using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerAreaTracker : UdonSharpBehaviour
    {
        [UdonSynced] public int[] playerIds = new int[BaseConstants.MaxPlayers];
        [UdonSynced] public int[] areaIds = new int[BaseConstants.MaxPlayers];
        public AreaManager areas;
        public TeleportManager teleport;
        public AccessManager access;
        public BaseWorldSystem world;
        public int localAreaId = BaseConstants.InvalidId;
        public int localFloorId = BaseConstants.InvalidId;

        private void Start() { SendCustomEventDelayedSeconds("PollPosition", 1f); }

        public void PollPosition()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player) && teleport != null)
            {
                if (teleport.points == null) teleport.RefreshPoints();
                TeleportPoint found = null;
                float smallestVolume = float.MaxValue;
                Vector3 position = player.GetPosition();
                if (teleport.points != null)
                    for (int i = 0; i < teleport.points.Length; i++)
                    {
                        TeleportPoint point = teleport.points[i];
                        if (point == null) continue;
                        BoxCollider box = point.areaVolume;
                        if (box == null || !box.enabled || !box.gameObject.activeInHierarchy) continue;
                        Vector3 local = box.transform.InverseTransformPoint(position) - box.center;
                        Vector3 half = box.size * .5f;
                        if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z) continue;
                        float volume = half.x * half.y * half.z;
                        if (volume >= smallestVolume) continue;
                        found = point;
                        smallestVolume = volume;
                    }
                if (found == null) RecordArea(BaseConstants.InvalidId, BaseConstants.InvalidId);
                else if (world != null && world.returnUnauthorizedPlayersToSafePoint && access != null && access.CheckPointAccess(found) != AccessResult.Allowed)
                {
                    if (localAreaId != found.locationId) teleport.ToSafeFloor();
                }
                else EnterArea(found.locationId);
            }
            SendCustomEventDelayedSeconds("PollPosition", 1.5f);
        }

        public void EnterArea(int areaId)
        {
            if (areas == null || !Utilities.IsValid(Networking.LocalPlayer)) return;
            int area = areas.IndexOf(areaId);
            if (area < 0) return;
            RecordArea(areaId, areas.FloorIdAt(area));
        }

        private void RecordArea(int areaId, int floorId)
        {
            if (!Utilities.IsValid(Networking.LocalPlayer) || localAreaId == areaId) return;
            localAreaId = areaId;
            localFloorId = floorId;
            int id = Networking.LocalPlayer.playerId;
            int slot = -1;
            for (int i = 0; i < playerIds.Length; i++)
                if (playerIds[i] == id) { slot = i; break; }
                else if (slot < 0 && playerIds[i] == 0) slot = i;
            if (slot < 0) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            playerIds[slot] = id;
            areaIds[slot] = areaId;
            RequestSerialization();
        }

        public int AreaForPlayer(int playerId)
        {
            for (int i = 0; i < playerIds.Length; i++) if (playerIds[i] == playerId) return areaIds[i];
            return BaseConstants.InvalidId;
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(Networking.LocalPlayer) || !Networking.IsOwner(gameObject)) return;
            for (int i = 0; i < playerIds.Length; i++) if (playerIds[i] == player.playerId)
            {
                playerIds[i] = 0;
                areaIds[i] = 0;
                RequestSerialization();
                return;
            }
        }
    }
}
