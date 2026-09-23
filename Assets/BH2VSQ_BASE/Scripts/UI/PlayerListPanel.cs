using UdonSharp;
using TMPro;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Persistence;

namespace BH2VSQ.Base
{
    public class PlayerListPanel : UdonSharpBehaviour
    {
        public PlayerRegistry registry;
        public PlayerAreaTracker tracker;
        public PlayerDataManager data;
        public RadioDutyManager radio;
        public FloorManager floors;
        public AreaManager areas;
        public TMP_Text output;
        public PlayerDetailPanel detail;
        public TMP_Text[] rowTexts;
        public UIButtonAction[] rowActions;
        public GameObject[] rowObjects;
        public GameObject previousButton;
        public GameObject nextButton;
        public TMP_Text pageText;
        public LocalizationManager localization;
        private int page;

        public void Refresh()
        {
            if (registry == null || output == null) return;
            registry.Refresh();
            output.text = localization.Get(BaseText.Online) + ": " + registry.count + "  /  " + localization.Get(BaseText.Radio) + ": " + localization.Get(radio != null && radio.OnDuty() ? BaseText.OnDuty : BaseText.NoDuty);
            if (rowTexts == null || rowActions == null || rowObjects == null) return;
            int size = rowObjects.Length;
            if (size == 0) return;
            int pages = (registry.count + size - 1) / size;
            if (page >= pages) page = pages > 0 ? pages - 1 : 0;
            for (int i = 0; i < rowObjects.Length; i++)
            {
                int playerIndex = page * size + i;
                bool visible = playerIndex < registry.count && Utilities.IsValid(registry.players[playerIndex]);
                rowObjects[i].SetActive(visible);
                if (!visible) continue;
                VRCPlayerApi player = registry.players[playerIndex];
                int areaId = tracker == null ? -1 : tracker.AreaForPlayer(player.playerId);
                int areaIndex = areas == null ? -1 : areas.IndexOf(areaId);
                string areaName = areaIndex < 0 ? localization.Get(BaseText.Unknown) : areas.DisplayName(areaIndex, localization.Language());
                int floorId = areaIndex < 0 ? BaseConstants.InvalidId : areas.FloorIdAt(areaIndex);
                string floorName = floors != null && floors.Valid(floorId) ? floors.GetFloor(floorId, localization.Language()) : localization.Get(BaseText.Unknown);
                int xp = PlayerData.GetInt(player, "bh2vsq.xp");
                int level = 1 + (int)UnityEngine.Mathf.Sqrt(xp / 100f);
                rowTexts[i].text = player.displayName + " | " + localization.RankName(registry.RankForPlayer(player.playerId)) + " | " + localization.Get(BaseText.Level) + level + "\n" + floorName + " / " + areaName + (areas != null && areas.IsRadioLocation(areaId) ? " | " + localization.Get(BaseText.OnDuty) : "");
                rowActions[i].value = player.playerId;
            }
            if (previousButton != null) previousButton.SetActive(page > 0);
            if (nextButton != null) nextButton.SetActive(page + 1 < pages);
            if (pageText != null) pageText.text = (page + 1) + " / " + (pages > 0 ? pages : 1);
            if (detail != null && detail.selectedPlayerId != 0) detail.ShowPlayer(detail.selectedPlayerId);
        }

        public void NextPage() { page++; Refresh(); }
        public void PreviousPage() { if (page > 0) page--; Refresh(); }
    }
}
