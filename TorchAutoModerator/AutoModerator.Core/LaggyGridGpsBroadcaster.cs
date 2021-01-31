using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;

namespace AutoModerator.Core
{
    /// <summary>
    /// Broadcast GPS entities to online players.
    /// Clean up old GPS entities.
    /// </summary>
    public sealed class LaggyGridGpsBroadcaster : LaggyGridBroadcasterBase
    {
        readonly DeprecationObserver<long> _gpsTimestamps;
        readonly EntityIdGpsCollection _gpsCollection;
        readonly LaggyGridGpsMaker _gridMaker;
        readonly LaggyGridGpsDescriptionMaker _descriptionMaker;


        public LaggyGridGpsBroadcaster(IConfig config, AutoModeratorPlugin plugin) : base(config, plugin)
        {
            _gpsTimestamps = new DeprecationObserver<long>();
            _gpsCollection = new EntityIdGpsCollection();

            _descriptionMaker = new LaggyGridGpsDescriptionMaker(plugin.Config);
            _gridMaker = new LaggyGridGpsMaker(_descriptionMaker);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void BroadcastGpsToOnlinePlayers(IEnumerable<MyGps> gpss)
        {
            var identityIds = GetDestinationIdentityIds().ToArray();

            foreach (var gps in gpss)
            {
                // Update this grid's last broadcast time
                _gpsTimestamps.Add(gps.EntityId);

                // actually send GPS to players
                _gpsCollection.SendAddOrModifyGps(identityIds, gps);
            }

            SaveGpsHashesToDisk();

            _log.Debug($"Broadcasting to {identityIds.Length} players: {gpss.Select(g => $"\"{g.Name}\"")}");
        }

        public override async Task BroadcastToOnlinePlayers(IEnumerable<LaggyGridReport> gridReports, CancellationToken canceller = default)
        {
            // MyGps can be created in the game loop only (idk why)
            await GameLoopObserver.MoveToGameLoop(canceller);

            // create GPS entities of laggy grids
            var gpsCollection = new List<MyGps>();
            foreach (var (gridReport, i) in gridReports.Select((r, i) => (r, i)))
            {
                var lagRank = i + 1;
                if (_gridMaker.TryMakeGps(gridReport, lagRank, out var gps))
                {
                    gpsCollection.Add(gps);
                }
            }

            await TaskUtils.MoveToThreadPool(canceller);

            // broadcast to players
            BroadcastGpsToOnlinePlayers(gpsCollection);
        }

        IEnumerable<long> GetDestinationIdentityIds()
        {
            var targetPlayers = new List<long>();
            var mutedPlayerIds = new HashSet<ulong>(_config.MutedPlayers);
            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers();
            foreach (var onlinePlayer in onlinePlayers)
            {
                if (mutedPlayerIds.Contains(onlinePlayer.SteamId())) continue;
                if (_config.AdminsOnly && !onlinePlayer.IsAdmin()) continue;

                targetPlayers.Add(onlinePlayer.Identity.IdentityId);
            }

            return targetPlayers;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DeleteAllCustomGpss()
        {
            var removedGridIds = _gpsTimestamps.RemoveAll();
            _gpsCollection.SendDeleteGpss(removedGridIds);

            SaveGpsHashesToDisk();
        }

        public async Task LoopCleaning(CancellationToken canceller)
        {
            _log.Trace("Started cleaner loop");

            while (!canceller.IsCancellationRequested)
            {
                DeleteExpiredGpss();
                await Task.Delay(5.Seconds(), canceller);
            }
        }

        void DeleteExpiredGpss()
        {
            var removedGridIds = _gpsTimestamps.RemoveDeprecated(_config.GpsLifespan);
            _gpsCollection.SendDeleteGpss(removedGridIds);

            SaveGpsHashesToDisk();

            if (removedGridIds.Any())
            {
                _log.Debug($"Cleaned grids gps: {removedGridIds.ToStringSeq()}");
            }
        }

        void SaveGpsHashesToDisk()
        {
            var allTrackedGpsHashes = _gpsCollection.GetAllTrackedGpsHashes();
            _plugin.GpsHashStore.UpdateTrackedGpsHashes(allTrackedGpsHashes);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<MyGps> GetAllCustomGpsEntities()
        {
            return _gpsCollection.GetAllTrackedGpss();
        }
    }
}