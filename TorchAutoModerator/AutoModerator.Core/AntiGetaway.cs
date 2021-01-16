using System.Collections.Generic;
using VRageMath;

namespace AutoModerator.Core
{
    public class AntiGetaway
    {
        readonly Dictionary<long, GridLagReport> _lastLagReports;
        readonly Dictionary<long, Vector3D> _lastKnownPositions;

        public AntiGetaway()
        {
            _lastLagReports = new Dictionary<long, GridLagReport>();
            _lastKnownPositions = new Dictionary<long, Vector3D>();
        }

        public void Record(GridLagReport report, Vector3D position)
        {
            
        }
    }
}