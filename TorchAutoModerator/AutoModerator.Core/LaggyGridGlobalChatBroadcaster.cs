using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API.Managers;
using VRage.Game.ModAPI;
using VRageMath;

namespace AutoModerator.Core
{
    public class LaggyGridGlobalChatBroadcaster : LaggyGridBroadcasterBase
    {
        IChatManagerServer _chatManager => _plugin.Torch.CurrentSession?.Managers.GetManager<IChatManagerServer>();

        public LaggyGridGlobalChatBroadcaster(IConfig config, AutoModeratorPlugin plugin) : base(config, plugin)
        {
            
        }

        public override Task BroadcastToOnlinePlayers(IEnumerable<LaggyGridReport> gridReports, CancellationToken canceller = default)
        {
            var sb = new StringBuilder().AppendLine("Overloading grids:");
            gridReports.ForEach(b => sb.AppendLine($"> {(b.FactionTagOrNull == null ? $"player '{b.PlayerNameOrNull ?? "nobody"} '" : $"faction '{b.FactionTagOrNull}' {(_config.ShowFactionMember ? $"player '{b.PlayerNameOrNull}'" ?? "'nobody'" : "")}")} grid '{b.GridName}' {b.Mspf:N2} ms"));

            if (_config.AdminsOnly)
                MySession.Static.Players.GetOnlinePlayers()
                    .Cast<IMyPlayer>()
                    .Where(b => b.PromoteLevel > MyPromoteLevel.Admin)
                    .ForEach(b => _chatManager?.SendMessageAsOther("[Performance warning]", sb.ToString(), Color.IndianRed, b.SteamUserId));
            else
                _chatManager?.SendMessageAsOther("[Performance warning]", sb.ToString(), Color.IndianRed);
            return Task.CompletedTask;
        }
    }
}
