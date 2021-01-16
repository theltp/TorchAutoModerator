using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.TimeSerieses
{
    public sealed class TaggedTimeSeries<T, E>
    {
        readonly Dictionary<T, TimeSeries<E>> _timeSeriesMap;

        public TaggedTimeSeries()
        {
            _timeSeriesMap = new Dictionary<T, TimeSeries<E>>();
        }

        public bool TryGetTimeSeries(T tag, out ITimeSeries<E> timeSeries)
        {
            if (_timeSeriesMap.TryGetValue(tag, out var t))
            {
                timeSeries = t;
                return true;
            }

            timeSeries = null;
            return false;
        }

        public void Add(T tag, DateTime timestamp, E element)
        {
            if (!_timeSeriesMap.TryGetValue(tag, out var timeSeries))
            {
                timeSeries = new TimeSeries<E>();
                _timeSeriesMap[tag] = timeSeries;
            }

            timeSeries.Add(timestamp, element);
        }

        public IEnumerable<T> RemoveOlderThan(DateTime thresholdTimestamp)
        {
            var removedTags = new List<T>();
            foreach (var p in _timeSeriesMap.ToArray())
            {
                var tag = p.Key;
                var timeSeries = p.Value;

                timeSeries.RemoveOlderThan(thresholdTimestamp);
                if (timeSeries.Count == 0)
                {
                    _timeSeriesMap.Remove(tag);
                    removedTags.Add(tag);
                }
            }

            return removedTags;
        }

        public void Clear()
        {
            foreach (var timeSeries in _timeSeriesMap.Values)
            {
                timeSeries.Clear();
            }

            _timeSeriesMap.Clear();
        }
    }
}