using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Screens.Helpers;
using TorchEntityGpsBroadcaster.Core;
using Utils.General;

namespace AutoModerator.Core
{
    public sealed class LaggyGridGpsBroadcaster
    {
        readonly BroadcastReceiverCollector _receivers;
        readonly EntityIdGpsCollection _gpsCollection;

        public LaggyGridGpsBroadcaster(string prefix, BroadcastReceiverCollector receivers)
        {
            _receivers = receivers;
            _gpsCollection = new EntityIdGpsCollection(prefix);
        }

        public void SendDeleteUntrackedGpss()
        {
            _gpsCollection.SendDeleteUntrackedGpss();
        }

        public void SendDeleteAllTrackedGpss()
        {
            _gpsCollection.SendDeleteAllTrackedGpss();
        }

        public IEnumerable<MyGps> GetAllTrackedGpss()
        {
            return _gpsCollection.GetAllTrackedGpss();
        }

        public void SendDeleteGpss(IEnumerable<long> entityIds)
        {
            foreach (var identityId in _gpsCollection.GetAllTrackedIdentityIds())
            foreach (var entityId in entityIds)
            {
                _gpsCollection.SendDeleteGps(identityId, entityId);
            }
        }

        public void SendReplaceAllTrackedGpss(IEnumerable<MyGps> newGpss)
        {
            // delete existing GPSs whose entity ID is not listed in `gpss`
            var newGpsEntityIds = newGpss.Select(g => g.EntityId).ToSet();
            foreach (var (identityId, gps) in _gpsCollection.GetAllTrackedPairs())
            {
                if (!newGpsEntityIds.Contains(gps.EntityId))
                {
                    _gpsCollection.SendDeleteGps(identityId, gps.Hash);
                }
            }

            // add/modify other existing GPSs
            foreach (var targetIdentityId in _receivers.GetReceiverIds())
            foreach (var gps in newGpss)
            {
                _gpsCollection.SendAddOrModifyGps(targetIdentityId, gps);
            }
        }
    }
}