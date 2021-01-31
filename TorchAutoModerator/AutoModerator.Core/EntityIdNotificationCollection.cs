using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Torch;
using Torch.Collections;
using Utils.Torch;
using Utils.General;
using VRage.Game.Entity;
using NLog;
using System.Threading;

namespace AutoModerator.Core
{
    public class EntityIdNotificationCollection : MtObservableList<EntityIdNotificationCollection.EntityNotification>
    {
        public class EntityNotification
        {
            public LaggyGridReport Report { get; set; }

            public TimeSpan Lifespan { get; set; }

            [XmlIgnore]
            public List<MyIdentity> Identities { get; set; }

            [XmlIgnore]
            public MyEntity Entity { get; set; }

            public ConcurrentDictionary<string, double> MspfStats { get; set; }

            public List<Quest> Quests { get; set; } = new List<Quest>();
        }

        private readonly static ILogger _log = LogManager.GetCurrentClassLogger();

        public async Task BroadcastAll(CancellationToken canceller = default)
        {
            foreach (var notifycation in this)
                await Broadcast(notifycation, canceller);
        }

        public static Task Broadcast(EntityNotification entityNotification, CancellationToken canceller = default)
        {
            entityNotification.Quests.Clear();
            entityNotification.Quests.AddRange(entityNotification.Identities.Select(b => new Quest("Perfomance Warning", b.IdentityId)
            {
                Description = $"Your grid [{entityNotification.Report.GridName}] is dropping server simulation speed! Top 5 of perfomance issues:",
                DisposeTime = entityNotification.Lifespan
            }));
            var objectives = entityNotification.MspfStats.OrderByDescending(b => b.Value)
                                                             .Take(5)
                                                             .Select(b => new KeyValuePair<string, bool>($"{b.Key.Substring(2).HumanizeCamelCase()}: {b.Value:0.00}ms", false));
            objectives.ForEach(b => entityNotification.Quests.ForEach(c => c.AddObjective(b)));
            return Task.CompletedTask;
        }

        public void Clear(bool sync)
        {
            if (sync)
                this.ForEach(b => b.Quests.ForEach(b => b.Dispose()));
            else
                this.ForEach(b => b.Quests.ForEach(b => b.DisposeTimer.Dispose()));
            Clear();
        }
    }
}
