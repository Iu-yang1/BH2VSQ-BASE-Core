using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TeleportRequestManager : UdonSharpBehaviour
    {
        public const int Capacity = 16;
        public TeleportManager teleport;
        public RequestPanel panel;
        public float timeoutSeconds = 30f;
        [UdonSynced] public int[] requestIds = new int[Capacity];
        [UdonSynced] public int[] requesterIds = new int[Capacity];
        [UdonSynced] public int[] targetIds = new int[Capacity];
        [UdonSynced] public int[] requestTypes = new int[Capacity];
        [UdonSynced] public int[] states = new int[Capacity]; // 1 pending, 2 accepted, 3 rejected.
        [UdonSynced] public long[] expiryTicks = new long[Capacity];
        [UdonSynced] private int nextRequestId;
        private int[] seenIds = new int[Capacity];
        private int[] seenStates = new int[Capacity];

        private void Start() { SendCustomEventDelayedSeconds("Tick", 1f); }

        public void Tick()
        {
            Refresh();
            SendCustomEventDelayedSeconds("Tick", 1f);
        }

        public bool Send(int playerId, int type)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!Utilities.IsValid(local) || playerId == local.playerId || !Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId))) return false;
            long now = Networking.GetNetworkDateTime().Ticks;
            int slot = -1;
            for (int i = 0; i < Capacity; i++)
            {
                if (states[i] == 1 && expiryTicks[i] > now && requesterIds[i] == local.playerId && targetIds[i] == playerId && requestTypes[i] == type) return false;
                if (slot < 0 && (states[i] != 1 || expiryTicks[i] <= now)) slot = i;
            }
            if (slot < 0) return false;
            Networking.SetOwner(local, gameObject);
            nextRequestId++;
            if (nextRequestId <= 0) nextRequestId = 1;
            requestIds[slot] = nextRequestId;
            requesterIds[slot] = local.playerId;
            targetIds[slot] = playerId;
            requestTypes[slot] = type;
            states[slot] = 1;
            expiryTicks[slot] = now + (long)(timeoutSeconds * 10000000L);
            seenIds[slot] = nextRequestId;
            seenStates[slot] = 1;
            RequestSerialization();
            if (panel != null) panel.ShowNotice("传送请求已发送");
            return true;
        }

        public override void OnDeserialization() { Refresh(); }

        public void Refresh()
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!Utilities.IsValid(local)) return;
            long now = Networking.GetNetworkDateTime().Ticks;
            for (int i = 0; i < Capacity; i++)
            {
                int id = requestIds[i];
                if (id == 0 || (requesterIds[i] != local.playerId && targetIds[i] != local.playerId)) continue;
                int state = states[i];
                if (state == 1 && expiryTicks[i] <= now) state = 4;
                if (seenIds[i] == id && seenStates[i] == state) continue;
                seenIds[i] = id;
                seenStates[i] = state;
                if (state == 1 && targetIds[i] == local.playerId)
                {
                    if (panel != null) panel.ShowNotice("收到传送请求 · 按住 Tab 前往请求页处理");
                }
                else if (state == 2)
                {
                    if (panel != null) panel.ShowNotice("传送请求已同意");
                    if (teleport != null)
                    {
                        bool moved = false;
                        if (requestTypes[i] == 0 && requesterIds[i] == local.playerId)
                            moved = teleport.ToPlayer(targetIds[i]);
                        if (requestTypes[i] == 1 && targetIds[i] == local.playerId)
                            moved = teleport.ToPlayer(requesterIds[i], true);
                        if (!moved && panel != null) panel.ShowAccessResult(teleport.lastResult);
                    }
                }
                else if (state == 3 && panel != null) panel.ShowNotice("传送请求已拒绝");
                else if (state == 4 && requesterIds[i] == local.playerId && panel != null) panel.ShowNotice("传送请求已过期");
            }
            if (panel != null) panel.Refresh();
        }

        public void Accept(int id) { Complete(id, 2); }
        public void Reject(int id) { Complete(id, 3); }

        private void Complete(int id, int state)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!Utilities.IsValid(local)) return;
            long now = Networking.GetNetworkDateTime().Ticks;
            for (int i = 0; i < Capacity; i++)
            {
                if (requestIds[i] != id || targetIds[i] != local.playerId || states[i] != 1 || expiryTicks[i] <= now) continue;
                Networking.SetOwner(local, gameObject);
                states[i] = state;
                RequestSerialization();
                Refresh();
                return;
            }
        }
    }
}
