using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    public class AreaTrigger : UdonSharpBehaviour
    {
        public PlayerAreaTracker tracker;
        public AccessManager access;
        public TeleportManager teleport;
        public TeleportPoint point;
        public BaseWorldSystem world;
        private void Start()
        {
            world = GetComponentInParent<BaseWorldSystem>();
            if (world == null) return;
            if (tracker == null) tracker = world.tracker;
            if (access == null) access = world.access;
            if (teleport == null) teleport = world.teleport;
            if (point == null) point = GetComponentInParent<TeleportPoint>();
            if (point != null && point.areaVolume == null) point.areaVolume = GetComponent<BoxCollider>();
        }
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            if (point == null) return;
            if (world != null && world.returnUnauthorizedPlayersToSafePoint && access != null && access.CheckPointAccess(point) != AccessResult.Allowed)
            {
                if (teleport != null) teleport.ToSafeFloor();
                return;
            }
            if (tracker != null) tracker.EnterArea(point.locationId);
        }
    }
}
