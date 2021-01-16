using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using AutoModerator.Core;
using NLog;
using Profiler.Basics;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Utils.General;
using Utils.Torch;

namespace AutoModerator
{
    public sealed class AutoModeratorPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<AutoModeratorConfig> _config;
        UserControl _userControl;
        CancellationTokenSource _canceller;
        FileLoggingConfigurator _fileLoggingConfigurator;
        GridLagTimeSeries _lagTimeSeries;
        GridLagTimeline _autoBroadcastableGrids;
        GridLagTimeline _manualBroadcastableGrids;
        GridLagReportGpsFactory _gpsFactory;
        GridLagReportDescriber _gpsDescriber;
        BroadcastReceiverCollector _gpsReceivers;
        LaggyGridGpsBroadcaster _gpsBroadcaster;
        ServerLagObserver _lagObserver;

        public AutoModeratorConfig Config => _config.Data;

        UserControl IWpfPlugin.GetControl() => _config.GetOrCreateUserControl(ref _userControl);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            GameLoopObserverManager.Add(torch);

            var configFilePath = this.MakeConfigFilePath();
            _config = Persistent<AutoModeratorConfig>.Load(configFilePath);
            Config.PropertyChanged += OnConfigChanged;

            _fileLoggingConfigurator = new FileLoggingConfigurator(
                "AutoModerator",
                new[] {"AutoModerator.*", "Utils.EntityGps.*"},
                AutoModeratorConfig.DefaultLogFilePath);

            _fileLoggingConfigurator.Initialize();
            _fileLoggingConfigurator.Configure(Config);

            _canceller = new CancellationTokenSource();

            _lagTimeSeries = new GridLagTimeSeries(Config);
            _gpsDescriber = new GridLagReportDescriber(Config);
            _gpsFactory = new GridLagReportGpsFactory(_gpsDescriber, new AntiGetaway());
            _gpsReceivers = new BroadcastReceiverCollector(Config);
            _gpsBroadcaster = new LaggyGridGpsBroadcaster("! ", _gpsReceivers);
            _lagObserver = new ServerLagObserver(5.Seconds());
            _manualBroadcastableGrids = new GridLagTimeline();
            _autoBroadcastableGrids = new GridLagTimeline();
        }

        void OnGameLoaded()
        {
            Config.PropertyChanged += OnConfigChangedInSession;

            var canceller = _canceller.Token;
            TaskUtils.RunUntilCancelledAsync(MainLoop, canceller).Forget(Log);
            TaskUtils.RunUntilCancelledAsync(_lagObserver.Observe, canceller).Forget(Log);
        }

        void OnGameUnloading()
        {
            Config.PropertyChanged -= OnConfigChanged;
            Config.PropertyChanged -= OnConfigChangedInSession;
            _config?.Dispose();
            _canceller?.Cancel();
            _canceller?.Dispose();
        }

        void OnConfigChanged(object _, PropertyChangedEventArgs args)
        {
            _fileLoggingConfigurator.Configure(Config);
        }

        void OnConfigChangedInSession(object _, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(Config.EnableAutoBroadcasting) &&
                !Config.EnableBroadcasting)
            {
                _gpsBroadcaster.SendDeleteAllTrackedGpss();
            }

            if (args.PropertyName == nameof(Config.EnableAutoBroadcasting) &&
                !Config.EnableAutoBroadcasting)
            {
                // delete auto-broadcasted GPSs
                _gpsBroadcaster.SendDeleteGpss(_autoBroadcastableGrids.GridIds);
                _autoBroadcastableGrids.Clear();
            }
        }

        async Task MainLoop(CancellationToken canceller)
        {
            Log.Info("Started collector loop");

            // clear all GPS entities from the last session
            _gpsBroadcaster.SendDeleteUntrackedGpss();

            // Wait for some time during the session startup
            await Task.Delay(Config.FirstIdle.Seconds(), canceller);

            while (!canceller.IsCancellationRequested)
            {
                var stopwatch = Stopwatch.StartNew();

                // auto profile
                var mask = new GameEntityMask(null, null, null);
                var profiler = new GridLagProfiler(Config, mask);
                await _lagTimeSeries.Profile(profiler, canceller);

                // remember laggy grids
                var laggyGrids = _lagTimeSeries.GetLaggyGrids();
                _autoBroadcastableGrids.AddProfileResults(laggyGrids, Config.MinLifespan.Seconds());

                // check if the server is laggy
                var simSpeed = _lagObserver.SimSpeed;
                var isLaggy = simSpeed < Config.SimSpeedThreshold;
                Log.Debug($"laggy: {isLaggy} ({simSpeed:0.0}ss)");

                if (Config.EnableBroadcasting && isLaggy)
                {
                    var allBroadcastableGpss = new Dictionary<long, MyGps>();

                    // auto broadcasting
                    if (Config.EnableAutoBroadcasting)
                    {
                        // manually-broadcasted GPSs should disappear in specified time
                        _autoBroadcastableGrids.RemoveExpired();

                        var gridReports = _autoBroadcastableGrids.MakeGridLagReports().ToArray();
                        var gpss = await _gpsFactory.CreateGpss(gridReports, canceller);
                        allBroadcastableGpss.AddRange(gpss.Select(g => (g.EntityId, g)));

                        Log.Debug($"Auto-broadcasted {gridReports.Length} grids");
                    }

                    // manual broadcasting (added from command)
                    {
                        // manually-broadcasted GPSs should disappear in specified time
                        _manualBroadcastableGrids.RemoveExpired();

                        var gridReports = _manualBroadcastableGrids.MakeGridLagReports().ToArray();
                        var gpss = await _gpsFactory.CreateGpss(gridReports, canceller);
                        allBroadcastableGpss.AddRange(gpss.Select(g => (g.EntityId, g)));

                        Log.Debug($"Manual-broadcasted {gridReports.Length} grids");
                    }

                    // broadcast
                    _gpsBroadcaster.SendReplaceAllTrackedGpss(allBroadcastableGpss.Values);
                }

                await TaskUtils.DelayMin(stopwatch, 1.Seconds(), canceller);
            }
        }

        public CancellationToken GetCancellationToken()
        {
            return _canceller.Token;
        }

        public GridLagProfiler GetProfiler(GameEntityMask mask)
        {
            return new GridLagProfiler(Config, mask);
        }

        public void Broadcast(IEnumerable<GridLagProfileResult> profileResults, TimeSpan remainingTime)
        {
            // remember the list so we can schedule countdown
            _manualBroadcastableGrids.AddProfileResults(profileResults, remainingTime);
        }

        public bool CheckPlayerReceivesBroadcast(MyPlayer player)
        {
            return _gpsReceivers.CheckReceive(player);
        }

        public void DeleteAllBroadcasts()
        {
            _gpsBroadcaster.SendDeleteAllTrackedGpss();
        }

        public IEnumerable<MyGps> GetAllBroadcasts()
        {
            return _gpsBroadcaster.GetAllTrackedGpss();
        }
    }
}