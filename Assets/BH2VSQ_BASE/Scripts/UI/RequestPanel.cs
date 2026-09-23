using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    public class RequestPanel : UdonSharpBehaviour
    {
        public GameObject root;
        public GameObject noticeRoot;
        public LocalCanvasFollower noticeFollower;
        public TMP_Text noticeText;
        public TMP_Text countText;
        public TMP_Text emptyText;
        public TMP_Text[] requesterTexts;
        public TMP_Text[] locationTexts;
        public GameObject[] rowObjects;
        public UIButtonAction[] acceptActions;
        public UIButtonAction[] rejectActions;
        public GameObject previousButton;
        public GameObject nextButton;
        public TMP_Text pageText;
        public TeleportRequestManager requests;
        public PlayerAreaTracker tracker;
        public AreaManager areas;
        public LocalizationManager localization;
        private int page;
        private float noticeHideAt;

        private void Update()
        {
            if (noticeRoot != null && noticeRoot.activeSelf && Time.time >= noticeHideAt) noticeRoot.SetActive(false);
        }

        public void ShowNotice(string message)
        {
            if (noticeText != null) noticeText.text = message;
            if (noticeRoot != null) noticeRoot.SetActive(true);
            if (noticeFollower != null) noticeFollower.Place();
            noticeHideAt = Time.time + 5f;
        }

        public void ShowAccessResult(AccessResult result)
        {
            if (localization == null) return;
            if (result == AccessResult.InsufficientRank) ShowNotice(localization.Get(BaseText.NoPermission));
            else if (result == AccessResult.FloorReserved) ShowNotice(localization.Get(BaseText.FloorReserved));
            else if (result == AccessResult.FloorMaintenance) ShowNotice(localization.Get(BaseText.FloorMaintenance));
            else if (result != AccessResult.Allowed) ShowNotice(localization.Get(BaseText.AccessDenied));
        }

        public void Refresh()
        {
            if (requests == null || rowObjects == null || !Utilities.IsValid(Networking.LocalPlayer)) return;
            int count = PendingCount();
            int size = rowObjects.Length;
            if (size == 0) return;
            int pages = (count + size - 1) / size;
            if (page >= pages) page = pages > 0 ? pages - 1 : 0;
            if (countText != null) countText.text = "待处理申请  " + count;
            if (emptyText != null) emptyText.gameObject.SetActive(count == 0);
            for (int i = 0; i < size; i++)
            {
                int slot = PendingSlot(page * size + i);
                rowObjects[i].SetActive(slot >= 0);
                if (slot < 0) continue;
                int requesterId = requests.requesterIds[slot];
                VRCPlayerApi requester = VRCPlayerApi.GetPlayerById(requesterId);
                requesterTexts[i].text = Utilities.IsValid(requester) ? requester.displayName : "离线玩家";
                int areaId = tracker == null ? BaseConstants.InvalidId : tracker.AreaForPlayer(requesterId);
                int areaIndex = areas == null ? -1 : areas.IndexOf(areaId);
                string place = areaIndex < 0 ? "未知位置" : areas.DisplayName(areaIndex, 1);
                locationTexts[i].text = place + (requests.requestTypes[slot] == 0 ? " · 申请前往" : " · 邀请前往");
                acceptActions[i].value = requests.requestIds[slot];
                rejectActions[i].value = requests.requestIds[slot];
            }
            if (previousButton != null) previousButton.SetActive(page > 0);
            if (nextButton != null) nextButton.SetActive(page + 1 < pages);
            if (pageText != null) pageText.text = (page + 1) + " / " + (pages > 0 ? pages : 1);
        }

        private int PendingCount()
        {
            int count = 0;
            long now = Networking.GetNetworkDateTime().Ticks;
            int localId = Networking.LocalPlayer.playerId;
            for (int i = 0; i < TeleportRequestManager.Capacity; i++)
                if (requests.states[i] == 1 && requests.targetIds[i] == localId && requests.expiryTicks[i] > now) count++;
            return count;
        }

        private int PendingSlot(int visibleIndex)
        {
            int count = 0;
            long now = Networking.GetNetworkDateTime().Ticks;
            int localId = Networking.LocalPlayer.playerId;
            for (int i = 0; i < TeleportRequestManager.Capacity; i++)
                if (requests.states[i] == 1 && requests.targetIds[i] == localId && requests.expiryTicks[i] > now)
                {
                    if (count == visibleIndex) return i;
                    count++;
                }
            return -1;
        }

        public void NextPage() { page++; Refresh(); }
        public void PreviousPage() { if (page > 0) page--; Refresh(); }
        public void Accept(int id) { if (requests != null) requests.Accept(id); }
        public void Reject(int id) { if (requests != null) requests.Reject(id); }
    }
}
