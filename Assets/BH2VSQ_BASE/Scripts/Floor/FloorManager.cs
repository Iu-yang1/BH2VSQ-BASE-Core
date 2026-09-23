using UnityEngine;
using UdonSharp;

namespace BH2VSQ.Base
{
    // Floor IDs and names come from TeleportPoint instances. State is synced on each point.
    public class FloorManager : UdonSharpBehaviour
    {
        public TeleportManager teleport;

        // Barrier-controller mode is used by BH2VSQ_FloorBarrier.prefab.
        // The barrier reuses this existing FloorManager U# program asset so the
        // prefab does not need a second UdonSharp program type.
        [HideInInspector] public bool barrierController;
        public int barrierFloorId = BaseConstants.InvalidId;

        private bool barrierInitialized;
        private bool barrierVisible;

        private void Start()
        {
            ResolveTeleportReference();

            // A valid Barrier Floor Id is the authoritative indicator that this
            // FloorManager is being used as a floor_barrier controller. The old
            // hidden barrierController flag is kept only for prefab backward
            // compatibility and is deliberately not required anymore.
            if (IsBarrierController())
            {
                RefreshBarrier();
                return;
            }

            // The normal FloorManager watches descendant barrier controllers so
            // network-synced floor-state changes are reflected locally as well.
            RefreshManagedBarriers();
        }

        private bool IsBarrierController()
        {
            return barrierFloorId != BaseConstants.InvalidId || barrierController;
        }

        private void ResolveTeleportReference()
        {
            if (teleport != null) return;

            // Prefer the nearest ancestor FloorManager. The floor barrier is
            // designed to live under the normal floor controller, whose teleport
            // reference is already authoritative for this world.
            Transform cursor = transform.parent;
            while (cursor != null)
            {
                FloorManager manager = cursor.GetComponent<FloorManager>();
                if (manager != null && manager != this && manager.teleport != null)
                {
                    teleport = manager.teleport;
                    return;
                }
                cursor = cursor.parent;
            }

            // Fallback for barriers that are not parented under FloorManager.
            Transform root = transform.root;
            if (root != null)
            {
                TeleportManager found = root.GetComponent<TeleportManager>();
                if (found == null) found = root.GetComponentInChildren<TeleportManager>(true);
                if (found != null) teleport = found;
            }
        }

        private FloorManager FindParentFloorManager()
        {
            Transform cursor = transform.parent;
            while (cursor != null)
            {
                FloorManager manager = cursor.GetComponent<FloorManager>();
                if (manager != null && manager != this) return manager;
                cursor = cursor.parent;
            }
            return null;
        }

        public void RefreshBarrier()
        {
            if (!IsBarrierController()) return;

            // Invalid ID remains a safe failure: the physical barrier stays shown.
            bool visible = true;
            if (barrierFloorId != BaseConstants.InvalidId)
            {
                // Prefer the parent FloorManager because it already points at the
                // world's authoritative TeleportManager. This avoids a barrier
                // accidentally resolving a different TeleportManager in a scene.
                FloorManager parentManager = FindParentFloorManager();
                FloorState state;
                if (parentManager != null)
                    state = parentManager.GetState(barrierFloorId);
                else
                {
                    ResolveTeleportReference();
                    state = GetState(barrierFloorId);
                }

                // Open => hide children. Reserved/Maintenance => show children.
                visible = state != FloorState.Open;
            }

            if (!barrierInitialized || visible != barrierVisible)
            {
                SetBarrierChildrenActive(visible);
                barrierVisible = visible;
                barrierInitialized = true;
            }

            // Keep checking so initialisation order and later UdonSynced state
            // changes are both handled without relying only on the admin UI path.
            SendCustomEventDelayedSeconds("RefreshBarrier", 0.25f);
        }

        public void RefreshBarrierForFloor(int floorId)
        {
            if (!IsBarrierController() || barrierFloorId != floorId) return;
            RefreshBarrier();
        }

        public void RefreshManagedBarriers()
        {
            if (IsBarrierController()) return;

            ResolveTeleportReference();
            FloorManager[] barriers = GetComponentsInChildren<FloorManager>(true);
            if (barriers != null)
            {
                for (int i = 0; i < barriers.Length; i++)
                {
                    FloorManager barrier = barriers[i];
                    if (barrier != null && barrier != this && barrier.IsBarrierController())
                    {
                        barrier.RefreshBarrier();
                    }
                }
            }

            SendCustomEventDelayedSeconds("RefreshManagedBarriers", 0.25f);
        }

        private void RefreshChildBarriers(int floorId)
        {
            FloorManager[] barriers = GetComponentsInChildren<FloorManager>(true);
            if (barriers == null) return;
            for (int i = 0; i < barriers.Length; i++)
            {
                FloorManager barrier = barriers[i];
                if (barrier != null && barrier != this && barrier.IsBarrierController())
                    barrier.RefreshBarrierForFloor(floorId);
            }
        }

        private void SetBarrierChildrenActive(bool active)
        {
            // The controller root stays active. Only the physical barrier
            // objects below it are toggled, so this Udon behaviour keeps running.
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null) child.gameObject.SetActive(active);
            }
        }

        public bool Valid(int floorId) { return teleport != null && teleport.ByFloor(floorId) != null; }

        public string GetFloor(int floorId, int language)
        {
            TeleportPoint point = teleport == null ? null : teleport.ByFloor(floorId);
            return point == null ? "?" : point.DisplayFloor(language);
        }

        public FloorState GetState(int floorId)
        {
            // 1F is always open and is not managed by the floor-state system.
            if (floorId == 1) return FloorState.Open;
            ResolveTeleportReference();
            TeleportPoint point = teleport == null ? null : teleport.ByFloor(floorId);
            return point == null ? FloorState.Maintenance : (FloorState)point.floorState;
        }

        public bool IsFloorAvailable(int floorId) { return Valid(floorId) && GetState(floorId) == FloorState.Open; }

        public bool SetState(int floorId, FloorState state, PermissionManager permission)
        {
            ResolveTeleportReference();
            if (floorId == 1 || !IsGuestFloor(floorId) || permission == null || !permission.IsAdmin() || teleport == null || teleport.points == null) return false;
            for (int i = 0; i < teleport.points.Length; i++)
                if (teleport.points[i] != null && teleport.points[i].floorId == floorId)
                    teleport.points[i].SetFloorState(state);
            RefreshChildBarriers(floorId);
            return true;
        }

        public bool IsGuestFloor(int floorId)
        {
            ResolveTeleportReference();
            if (floorId == 1 || teleport == null || teleport.points == null) return false;
            bool found = false;
            for (int i = 0; i < teleport.points.Length; i++)
            {
                TeleportPoint point = teleport.points[i];
                if (point == null || point.floorId != floorId) continue;
                found = true;
                if (point.requiredRank != BaseRank.Visitor) return false;
            }
            return found;
        }

        public int ManageableFloorCount()
        {
            ResolveTeleportReference();
            if (teleport == null || teleport.points == null) return 0;
            int count = 0;
            for (int i = 0; i < teleport.points.Length; i++)
            {
                TeleportPoint point = teleport.points[i];
                if (point == null || !IsGuestFloor(point.floorId)) continue;
                bool seen = false;
                for (int j = 0; j < i; j++)
                    if (teleport.points[j] != null && teleport.points[j].floorId == point.floorId) { seen = true; break; }
                if (!seen) count++;
            }
            return count;
        }

        public int ManageableFloorIdAt(int index)
        {
            ResolveTeleportReference();
            if (teleport == null || teleport.points == null) return BaseConstants.InvalidId;
            int position = 0;
            for (int i = 0; i < teleport.points.Length; i++)
            {
                TeleportPoint point = teleport.points[i];
                if (point == null || !IsGuestFloor(point.floorId)) continue;
                bool seen = false;
                for (int j = 0; j < i; j++)
                    if (teleport.points[j] != null && teleport.points[j].floorId == point.floorId) { seen = true; break; }
                if (seen) continue;
                if (position == index) return point.floorId;
                position++;
            }
            return BaseConstants.InvalidId;
        }

        public int UniqueFloorCount()
        {
            ResolveTeleportReference();
            if (teleport == null || teleport.points == null) return 0;
            int count = 0;
            for (int i = 0; i < teleport.points.Length; i++)
            {
                if (teleport.points[i] == null) continue;
                bool seen = false;
                for (int j = 0; j < i; j++)
                    if (teleport.points[j] != null && teleport.points[j].floorId == teleport.points[i].floorId) { seen = true; break; }
                if (!seen) count++;
            }
            return count;
        }

        public int FloorIdAt(int index)
        {
            ResolveTeleportReference();
            if (teleport == null || teleport.points == null) return BaseConstants.InvalidId;
            int position = 0;
            for (int i = 0; i < teleport.points.Length; i++)
            {
                if (teleport.points[i] == null) continue;
                bool seen = false;
                for (int j = 0; j < i; j++)
                    if (teleport.points[j] != null && teleport.points[j].floorId == teleport.points[i].floorId) { seen = true; break; }
                if (seen) continue;
                if (position == index) return teleport.points[i].floorId;
                position++;
            }
            return BaseConstants.InvalidId;
        }
    }
}
