using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API.Managers;
using VRageMath;

namespace AutoModerator.Core
{
    public class LaggyGridDirectChatBroadcaster : LaggyGridBroadcasterBase
    {
        IChatManagerServer _chatManager => _plugin.Torch.CurrentSession?.Managers.GetManager<IChatManagerServer>();

        public LaggyGridDirectChatBroadcaster(IConfig config, AutoModeratorPlugin plugin) : base(config, plugin)
        {

        }

        public override Task BroadcastToOnlinePlayers(IEnumerable<LaggyGridReport> gridReports, CancellationToken canceller = default)
        {
            foreach (var report in gridReports.GroupBy(b => b.PlayerIdentityId).Where(b => b.Key > 0))
            {
                var identities = new List<long>
                {
                    report.Key
                };

                var tag = report.FirstOrDefault(b => !string.IsNullOrEmpty(b.FactionTagOrNull))?.FactionTagOrNull;
                if (tag != default)
                {
                    var faction = MySession.Static.Factions.TryGetFactionByTag(tag);
                    if (faction != default)
                    {
                        identities.AddRange(faction.Members.Select(b => b.Value.PlayerId));
                    }
                }

                identities.ForEach(b => SendDMReports(report, b));
            }
            return Task.CompletedTask;
        }

        private void SendDMReports(IEnumerable<LaggyGridReport> gridReports, long identityId)
        {
            var player = MySession.Static.Players.GetOnlinePlayers().FirstOrDefault(b => b.Identity.IdentityId == identityId);
            if (player == null)
            {
                _log.Trace($"Can't send notification player '{player?.DisplayName}'({identityId}) is offline");
                return;
            }
            var sb = new StringBuilder().AppendLine("Your overloading grids:");
            gridReports.ForEach(b => sb.AppendLine($"> '{b.GridName}' {b.Mspf:N2} ms"));
            _chatManager?.SendMessageAsOther("[Performance waning]", sb.ToString(), Color.IndianRed, player.Id.SteamId);
        }
    }
}
