using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoModerator.Core
{
    public abstract class LaggyGridBroadcasterBase
    {
        public interface IConfig
        {
            /// <summary>
            /// Length of time to keep no-longer-laggy grids in HUD.
            /// </summary>
            TimeSpan GpsLifespan { get; }

            /// <summary>
            /// Steam IDs of players who have muted this GPS broadcaster.
            /// </summary>
            IEnumerable<ulong> MutedPlayers { get; }

            /// <summary>
            /// Broadcast to admin players only.
            /// </summary>
            bool AdminsOnly { get; }
        }

        public enum BroadcasterType
        {
            GPS,
            QuestLog,
            Notification,
            Chat
        }
        
        protected IConfig _config;
        protected readonly AutoModeratorPlugin _plugin;
        protected readonly static ILogger _log = LogManager.GetLogger("LaggyGridBroadcaster");

        public LaggyGridBroadcasterBase(IConfig config, AutoModeratorPlugin plugin)
        {
            _config = config;
            _plugin = plugin;
        }

        public abstract Task BroadcastToOnlinePlayers(IEnumerable<LaggyGridReport> gridReports, CancellationToken canceller = default);

    }
}
