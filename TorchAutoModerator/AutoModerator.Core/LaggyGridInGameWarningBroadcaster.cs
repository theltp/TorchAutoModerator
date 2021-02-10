using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.Torch;
using static Sandbox.Game.SessionComponents.MySessionComponentWarningSystem;

namespace AutoModerator.Core
{
    public class LaggyGridInGameWarningBroadcaster : LaggyGridBroadcasterBase
    {
        public LaggyGridInGameWarningBroadcaster(IConfig config, AutoModeratorPlugin plugin) : base(config, plugin)
        {
            PerformanceWarningApi.Enabled = true;
        }

        public override Task BroadcastToOnlinePlayers(IEnumerable<LaggyGridReport> gridReports, CancellationToken canceller = default)
        {
            PerformanceWarningApi.Broadcast(gridReports.Select(b => new WarningData($"Grid", $"Name {b.GridName} {(b.FactionTagOrNull == null ? $"Player '{b.PlayerNameOrNull ?? "nobody"} '" : $"Faction '{b.FactionTagOrNull}' {(_config.ShowFactionMember ? $"Player '{b.PlayerNameOrNull}'" ?? "'nobody'" : "")}")} current load {b.Mspf:N2} ms", Category.Performance)));
            return Task.CompletedTask;
        }
    }
}
