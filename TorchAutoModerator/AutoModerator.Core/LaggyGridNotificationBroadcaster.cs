using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.General;
using Utils.Torch;

namespace AutoModerator.Core
{
    public class LaggyGridNotificationBroadcaster : LaggyGridBroadcasterBase
    {
        readonly EntityIdNotificationCollection _notifications;

        public LaggyGridNotificationBroadcaster(IConfig config, AutoModeratorPlugin plugin) : base(config, plugin)
        {
            _notifications = new EntityIdNotificationCollection();
        }

        public override async Task BroadcastToOnlinePlayers(IEnumerable<LaggyGridReport> gridReports, CancellationToken canceller = default)
        {
            _notifications.Clear(false);
            gridReports.GroupBy(b => b.GridId).Select(b => new EntityIdNotificationCollection.EntityNotification()
            {
                Report = b.First(),
                Lifespan = _config.GpsLifespan,
                MspfStats = new System.Collections.Concurrent.ConcurrentDictionary<string, double>(new[] { new KeyValuePair<string, double>(nameof(MyCubeGrid), b.Average(b => b.Mspf)) })
            }).ForEach(b => _notifications.Add(b));

            await GameLoopObserver.MoveToGameLoop(canceller);

            foreach (var entityNotification in _notifications)
            {
                entityNotification.Entity = MyEntities.GetEntityByIdOrDefault<MyCubeGrid>(entityNotification.Report.GridId);
                entityNotification.Identities = new List<MyIdentity>();

                if (string.IsNullOrEmpty(entityNotification.Report.FactionTagOrNull) && !string.IsNullOrEmpty(entityNotification.Report.PlayerNameOrNull))
                {
                    if (!MySession.Static.Players.IsPlayerOnline(entityNotification.Report.PlayerIdentityId))
                    {
                        _log.Trace($"Can't send notification player '{entityNotification.Report.PlayerNameOrNull}' is offline");
                        return;
                    }
                    entityNotification.Identities.Add(MySession.Static.Players.TryGetIdentity(entityNotification.Report.PlayerIdentityId));
                }
                else if (!string.IsNullOrEmpty(entityNotification.Report.FactionTagOrNull))
                {
                    var faction = MySession.Static.Factions.TryGetFactionByTag(entityNotification.Report.FactionTagOrNull);
                    entityNotification.Identities.AddRange(faction.Members.Select(b => MySession.Static.Players.TryGetIdentity(b.Value.PlayerId)));
                }

                if (entityNotification.Entity == default || !entityNotification.Identities.Any(b => b != default))
                {
                    _log.Warn($"Can't find {(entityNotification.Entity == default ? "entity" : "players")} for notification");
                    return;
                }
            }

            await TaskUtils.MoveToThreadPool(canceller);
            await _notifications.BroadcastAll(canceller);
        }
    }
}
