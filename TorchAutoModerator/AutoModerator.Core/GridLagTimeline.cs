using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AutoModerator.Core
{
    public class GridLagTimeline
    {
        readonly IDictionary<long, (GridLagProfileResult ProfileResult, DateTime EndTimestamp)> _self;

        public GridLagTimeline()
        {
            _self = new ConcurrentDictionary<long, (GridLagProfileResult, DateTime)>();
        }

        public IEnumerable<long> GridIds => _self.Keys;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddProfileResults(IEnumerable<GridLagProfileResult> profileResults, TimeSpan remainingTime)
        {
            var endTime = DateTime.UtcNow + remainingTime;
            foreach (var profileResult in profileResults)
            {
                _self[profileResult.GridId] = (profileResult, endTime);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RemoveExpired()
        {
            foreach (var (gridId, (_, endTime)) in _self.ToArray())
            {
                if (endTime < DateTime.UtcNow)
                {
                    _self.Remove(gridId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<GridLagReport> MakeGridLagReports()
        {
            foreach (var (profileResult, endTime) in _self.Values)
            {
                var remainingTime = endTime - DateTime.UtcNow;
                var gridReport = new GridLagReport(profileResult, remainingTime);
                yield return gridReport;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Clear()
        {
            _self.Clear();
        }
    }
}