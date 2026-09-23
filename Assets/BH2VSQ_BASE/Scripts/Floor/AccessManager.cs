using UdonSharp;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    public class AccessManager : UdonSharpBehaviour
    {
        public FloorManager floors;
        public TeleportManager teleport;
        public PermissionManager permission;

        public AccessResult CheckPointAccess(TeleportPoint point)
        {
            return CheckPointAccess(point, false);
        }

        // Accepted invitations bypass only the destination rank gate.
        // Reserved / maintenance floor state remains enforced for the invited player.
        public AccessResult CheckPointAccess(TeleportPoint point, bool bypassRank)
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
                return AccessResult.InvalidPlayer;

            if (point == null)
                return AccessResult.InvalidArea;

            if (permission == null)
                return AccessResult.InsufficientRank;

            BaseRank playerRank = permission.GetRank();

            // Explicit hierarchical permission:
            // Admin   >= Member >= Visitor
            // An accepted invitation can bypass this rank gate.
            if (!bypassRank && !HasRequiredRank(playerRank, point.requiredRank))
                return AccessResult.InsufficientRank;

            // Admin bypasses reserved / maintenance restrictions.
            if (playerRank == BaseRank.Admin)
                return AccessResult.Allowed;

            FloorState state =
                floors == null
                    ? FloorState.Maintenance
                    : floors.GetState(point.floorId);

            if (state == FloorState.Reserved)
                return AccessResult.FloorReserved;

            if (state == FloorState.Maintenance)
                return AccessResult.FloorMaintenance;

            return AccessResult.Allowed;
        }

        public AccessResult CheckFloorAccess(int floorId)
        {
            if (teleport == null || teleport.ByFloor(floorId) == null)
                return AccessResult.InvalidFloor;

            return CheckPointAccess(teleport.ByFloor(floorId));
        }

        public AccessResult CheckAreaAccess(int locationId)
        {
            return CheckPointAccess(
                teleport == null
                    ? null
                    : teleport.ById(locationId)
            );
        }

        private bool HasRequiredRank(
            BaseRank playerRank,
            BaseRank requiredRank)
        {
            int playerLevel = GetRankLevel(playerRank);
            int requiredLevel = GetRankLevel(requiredRank);

            return playerLevel >= requiredLevel;
        }

        private int GetRankLevel(BaseRank rank)
        {
            if (rank == BaseRank.Admin)
                return 2;

            if (rank == BaseRank.Member)
                return 1;

            return 0;
        }
    }
}