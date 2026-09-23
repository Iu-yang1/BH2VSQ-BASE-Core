using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    public class TeleportManager : UdonSharpBehaviour
    {
        public AccessManager access;
        public PlayerAreaTracker tracker;
        public Transform pointRoot;
        public TeleportPoint[] points;
        public AccessResult lastResult;

        private void Start() { RefreshPoints(); }

        public void RefreshPoints()
        {
            if (pointRoot != null) points = pointRoot.GetComponentsInChildren<TeleportPoint>(true);
        }

        public TeleportPoint ById(int locationId)
        {
            if (points == null) RefreshPoints();
            if (points == null) return null;
            for (int i = 0; i < points.Length; i++)
                if (points[i] != null && points[i].locationId == locationId) return points[i];
            return null;
        }

        public TeleportPoint ByFloor(int floorId)
        {
            if (points == null) RefreshPoints();
            if (points == null) return null;
            for (int i = 0; i < points.Length; i++)
                if (points[i] != null && points[i].floorId == floorId) return points[i];
            return null;
        }

        public bool ToLocation(int locationId)
        {
            TeleportPoint point = ById(locationId);
            lastResult = access == null ? AccessResult.InvalidArea : access.CheckPointAccess(point);
            return lastResult == AccessResult.Allowed && Move(point);
        }

        public bool ToFloor(int floorId)
        {
            TeleportPoint point = ByFloor(floorId);
            lastResult = access == null ? AccessResult.InvalidFloor : access.CheckPointAccess(point);
            return lastResult == AccessResult.Allowed && Move(point);
        }

        public bool ToPlayer(int playerId)
        {
            return ToPlayer(playerId, false);
        }

        // Used by an accepted invitation: bypass the destination player's rank gate
        // while still enforcing floor state such as reserved or maintenance.
        public bool ToPlayer(int playerId, bool bypassRank)
        {
            lastResult = AccessResult.Allowed;
            VRCPlayerApi target = VRCPlayerApi.GetPlayerById(playerId);
            if (!Utilities.IsValid(Networking.LocalPlayer) || !Utilities.IsValid(target) || target.isLocal)
            {
                lastResult = AccessResult.InvalidPlayer;
                return false;
            }
            if (tracker != null && access != null)
            {
                TeleportPoint area = ById(tracker.AreaForPlayer(playerId));
                if (area != null)
                {
                    lastResult = access.CheckPointAccess(area, bypassRank);
                    if (lastResult != AccessResult.Allowed) return false;
                }
            }
            Vector3 position = target.GetPosition();
            Networking.LocalPlayer.TeleportTo(position + target.GetRotation() * Vector3.back, target.GetRotation());
            return true;
        }

        public void ToSafeFloor()
        {
            if (points == null) RefreshPoints();
            if (points != null)
                for (int i = 0; i < points.Length; i++)
                    if (points[i] != null && points[i].isSafeFallback && Move(points[i])) return;
            if (Utilities.IsValid(Networking.LocalPlayer)) Networking.LocalPlayer.Respawn();
        }

        private bool Move(TeleportPoint point)
        {
            if (point == null || !Utilities.IsValid(Networking.LocalPlayer)) return false;
            Networking.LocalPlayer.TeleportTo(point.Position(), point.Rotation());
            return true;
        }
    }
}
