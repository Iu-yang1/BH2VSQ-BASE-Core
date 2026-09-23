using UdonSharp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BH2VSQ.Base
{
    public class TeleportPanel : UdonSharpBehaviour
    {
        public TeleportManager teleport;
        public PlayerAreaTracker tracker;
        public AreaPopulationManager population;
        public LocalizationManager localization;
        public TMP_Text feedback;
        public UIButtonAction[] locationActions;
        public TMP_Text[] locationLabels;
        public GameObject[] locationObjects;
        public GameObject previousButton;
        public GameObject nextButton;
        public TMP_Text pageLabel;
        private int page;

        public void Refresh()
        {
            if (teleport == null || locationObjects == null) return;
            teleport.RefreshPoints();
            int visible = VisibleCount();
            int size = locationObjects.Length;
            if (size == 0) return;
            int pages = (visible + size - 1) / size;
            if (page >= pages) page = pages > 0 ? pages - 1 : 0;
            for (int i = 0; i < size; i++)
            {
                TeleportPoint point = VisibleAt(page * size + i);
                locationObjects[i].SetActive(point != null);
                if (point == null) continue;
                locationActions[i].value = point.locationId;
                bool current = tracker != null && tracker.localAreaId == point.locationId;
                int count = population == null ? 0 : population.Count(point.locationId);
                locationLabels[i].text = point.DisplayName(1) + "  ·  " + count + " 人" + (current ? "  ◆ 当前" : "");
                Image background = locationObjects[i].GetComponent<Image>();
                if (background != null) background.color = current ? new Color(.06f, .69f, .84f, .96f) : new Color(.02f, .23f, .38f, .83f);
            }
            if (previousButton != null) previousButton.SetActive(page > 0);
            if (nextButton != null) nextButton.SetActive(page + 1 < pages);
            if (pageLabel != null) pageLabel.text = localization.Get(BaseText.Page) + " " + (page + 1) + " / " + (pages > 0 ? pages : 1);
        }

        private int VisibleCount()
        {
            int count = 0;
            if (teleport.points != null)
                for (int i = 0; i < teleport.points.Length; i++)
                    if (teleport.points[i] != null && teleport.points[i].tabVisible) count++;
            return count;
        }

        private TeleportPoint VisibleAt(int visibleIndex)
        {
            int count = 0;
            if (teleport.points != null)
                for (int i = 0; i < teleport.points.Length; i++)
                    if (teleport.points[i] != null && teleport.points[i].tabVisible)
                    {
                        if (count == visibleIndex) return teleport.points[i];
                        count++;
                    }
            return null;
        }

        public void NextPage() { page++; Refresh(); }
        public void PreviousPage() { if (page > 0) page--; Refresh(); }

        public void ToLocation(int id)
        {
            if (teleport == null)
            {
                SetFeedback(AccessResult.InvalidArea);
                return;
            }
            teleport.ToLocation(id);
            SetFeedback(teleport.lastResult);
        }

        private void SetFeedback(AccessResult result)
        {
            if (feedback == null || localization == null) return;
            if (result == AccessResult.Allowed) feedback.text = localization.Get(BaseText.Teleporting);
            else if (result == AccessResult.InsufficientRank) feedback.text = localization.Get(BaseText.NoPermission);
            else if (result == AccessResult.FloorReserved) feedback.text = localization.Get(BaseText.FloorReserved);
            else if (result == AccessResult.FloorMaintenance) feedback.text = localization.Get(BaseText.FloorMaintenance);
            else feedback.text = localization.Get(BaseText.AccessDenied);
        }
    }
}
