using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Profiler.Core;
using Utils.General;
using Utils.TimeSerieses;

namespace AutoModerator.Core
{
    internal sealed class GridLagTimeSeries
    {
        public interface IConfig
        {
            double LongLaggyWindow { get; }
            double ProfileTime { get; }
            double ProfileResultsExpireTime { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly TaggedTimeSeries<long, double> _taggedTimeSeries;
        readonly Dictionary<long, GridLagProfileResult> _lastProfileResults;

        public GridLagTimeSeries(IConfig config)
        {
            _config = config;
            _taggedTimeSeries = new TaggedTimeSeries<long, double>();
            _lastProfileResults = new Dictionary<long, GridLagProfileResult>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Clear()
        {
            _taggedTimeSeries.Clear();
            _lastProfileResults.Clear();
        }

        public async Task Profile(GridLagProfiler profiler, CancellationToken canceller)
        {
            using (profiler)
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                Log.Debug("Auto-profiling...");

                var profilingTime = _config.ProfileTime.Seconds();
                await Task.Delay(profilingTime, canceller);

                var newProfiledGrids = profiler.GetProfileResults(50).ToArray();
                AddToTimeSeries(newProfiledGrids);
                Log.Debug($"Auto-profiled {newProfiledGrids.Length} laggiest grids");
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void AddToTimeSeries(IEnumerable<GridLagProfileResult> profileResults)
        {
            var timestamp = DateTime.UtcNow;
            foreach (var newProfiledGrid in profileResults)
            {
                var tag = newProfiledGrid.GridId;
                _taggedTimeSeries.Add(tag, timestamp, newProfiledGrid.ThresholdNormal);
                _lastProfileResults[tag] = newProfiledGrid;
            }

            // keep the time series small
            var removeFrom = timestamp - _config.ProfileResultsExpireTime.Seconds();
            var removableTags = _taggedTimeSeries.RemoveOlderThan(removeFrom);
            _lastProfileResults.RemoveKeys(removableTags);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<GridLagProfileResult> GetLaggyGrids()
        {
            foreach (var (gridId, lastProfileResult) in _lastProfileResults)
            {
                if (!_taggedTimeSeries.TryGetTimeSeries(gridId, out var timeSeries))
                {
                    throw new Exception($"Time series not found for a profiled grid: {lastProfileResult}");
                }

                if (IsLongLaggy(timeSeries))
                {
                    yield return lastProfileResult;
                }
            }
        }

        bool IsLongLaggy(ITimeSeries<double> timeSeries)
        {
            if (timeSeries.Count == 0) return false;

            var now = DateTime.UtcNow;
            var sumNormal = 0d;
            var validPointCount = 0;
            for (var i = timeSeries.Count - 1; i >= 0; i--)
            {
                var (timestamp, normal) = timeSeries.GetPointAt(i);
                if (timestamp < now - _config.LongLaggyWindow.Seconds())
                {
                    break;
                }

                sumNormal += normal;
                validPointCount += 1;
            }

            var avgNormal = sumNormal / validPointCount;
            return avgNormal >= 1f;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string SprintTimeSeries(long gridId, string timestampFormat, int width)
        {
            if (!_taggedTimeSeries.TryGetTimeSeries(gridId, out var timeSeries))
            {
                throw new Exception($"time series not found: {gridId}");
            }

            var sb = new StringBuilder();
            for (var i = 0; i < timeSeries.Count; i++)
            {
                var (timestamp, lag) = timeSeries.GetPointAt(i);
                sb.Append(timestamp.ToString(timestampFormat));
                sb.Append(' ');

                var normal = Math.Min(1, lag);
                var starIndex = (int) (width * normal);
                for (var j = 0; j < width; j++)
                {
                    var c = j == starIndex ? '+' : '-';
                    sb.Append(c);
                }

                sb.Append(' ');
                sb.Append($"{lag * 100:0}%");
            }

            return sb.ToString();
        }
    }
}