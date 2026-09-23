using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TeleportPoint : UdonSharpBehaviour
    {
        public int locationId = 4000;
        public string locationName = "1F Living";
        public string chineseName = "1F 生活区";
        public int floorId = 1;
        public string floorName = "1F";
        public string chineseFloorName = "1F";
        public BaseRank requiredRank = BaseRank.Visitor;
        public bool tabVisible = true;
        public bool isSafeFallback;
        public bool radioDutyArea;
        public float xpMultiplier = 1f;
        public Transform destination;

        [UdonSynced] public int floorState;

        public string DisplayName(int language)
        {
            return language == 1 && !string.IsNullOrEmpty(chineseName) ? chineseName : locationName;
        }

        public string DisplayFloor(int language)
        {
            return language == 1 && !string.IsNullOrEmpty(chineseFloorName) ? chineseFloorName : floorName;
        }

        public Vector3 Position() { return destination != null ? destination.position : transform.position; }
        public Quaternion Rotation() { return destination != null ? destination.rotation : transform.rotation; }

        public void SetFloorState(FloorState state)
        {
            // 1F is permanently open and cannot be switched to Reserved/Maintenance.
            if (floorId == 1) return;
            if (!Utilities.IsValid(Networking.LocalPlayer)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            floorState = (int)state;
            RequestSerialization();
        }

    }
}
