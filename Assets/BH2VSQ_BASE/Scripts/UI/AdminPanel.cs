using UdonSharp;
using TMPro;
using UnityEngine;

namespace BH2VSQ.Base
{
    public class AdminPanel : UdonSharpBehaviour
    {
        public AdminManager admin;
        public FloorAdminManager floorAdmin;
        public FloorManager floors;
        public AreaPopulationManager population;
        public RadioDutyManager radio;
        public AreaManager areas;
        public LocalizationManager localization;
        public TMP_Text status;
        public TMP_Text populationLeft;
        public TMP_Text populationRight;
        public GameObject populationRoot;
        public UIButtonAction[] floorActions;
        public TMP_Text[] floorLabels;
        public GameObject[] floorObjects;
        public GameObject previousFloorButton;
        public GameObject nextFloorButton;
        public GameObject previousPopulationButton;
        public GameObject nextPopulationButton;
        public int selectedFloor = 1;
        public int selectedState;
        private int floorPage;
        private int populationPage;

        public void Refresh()
        {
            RefreshFloorButtons();
            if (status != null && localization != null) status.text = admin != null && admin.CanManage() ?
                localization.Get(BaseText.Floor) + " " + floors.GetFloor(selectedFloor, localization.Language()) + ": " + localization.StateName(floors.GetState(selectedFloor)) +
                "\n" + localization.Get(BaseText.Radio) + ": " + localization.Get(radio.OnDuty() ? BaseText.OnDuty : BaseText.NoDuty) : localization.Get(BaseText.AdminOnly);
            if (admin == null || !admin.CanManage() || areas == null || population == null) return;
            int count = areas.Count();
            int pages = (count + 13) / 14;
            if (populationPage >= pages) populationPage = pages > 0 ? pages - 1 : 0;
            string left = "", right = "";
            for (int i = populationPage * 14; i < count && i < (populationPage + 1) * 14; i++)
            {
                string line = areas.DisplayName(i, localization.Language()) + ": " + population.Count(areas.IdAt(i)) + "\n";
                if (i - populationPage * 14 < 7) left += line; else right += line;
            }
            if (populationLeft != null) populationLeft.text = left;
            if (populationRight != null) populationRight.text = right;
            if (previousPopulationButton != null) previousPopulationButton.SetActive(populationPage > 0);
            if (nextPopulationButton != null) nextPopulationButton.SetActive(populationPage + 1 < pages);
        }

        private void RefreshFloorButtons()
        {
            if (floors == null || floorObjects == null || floorObjects.Length == 0) return;
            int count = floors.ManageableFloorCount();
            if (count > 0 && !floors.IsGuestFloor(selectedFloor)) selectedFloor = floors.ManageableFloorIdAt(0);
            if (count == 0) selectedFloor = BaseConstants.InvalidId;
            int pages = (count + floorObjects.Length - 1) / floorObjects.Length;
            if (floorPage >= pages) floorPage = pages > 0 ? pages - 1 : 0;
            for (int i = 0; i < floorObjects.Length; i++)
            {
                int position = floorPage * floorObjects.Length + i;
                bool visible = position < count;
                floorObjects[i].SetActive(visible);
                if (!visible) continue;
                int floorId = floors.ManageableFloorIdAt(position);
                floorActions[i].value = floorId;
                floorLabels[i].text = floors.GetFloor(floorId, localization.Language()) + " · " + localization.StateName(floors.GetState(floorId));
            }
            if (previousFloorButton != null) previousFloorButton.SetActive(floorPage > 0);
            if (nextFloorButton != null) nextFloorButton.SetActive(floorPage + 1 < pages);
        }

        public void NextFloorPage() { floorPage++; Refresh(); }
        public void PreviousFloorPage() { if (floorPage > 0) floorPage--; Refresh(); }
        public void NextPopulationPage() { populationPage++; Refresh(); }
        public void PreviousPopulationPage() { if (populationPage > 0) populationPage--; Refresh(); }
        public bool CanOpenPlayerManagement() { return admin != null && admin.CanManage(); }

        public void ShowPopulation()
        {
            if (admin == null || !admin.CanManage()) return;
            if (populationRoot != null) populationRoot.SetActive(true);
            Refresh();
        }

        public void ApplyFloorState()
        {
            if (floorAdmin != null && floors != null && floors.IsGuestFloor(selectedFloor))
                floorAdmin.SetFloor(selectedFloor, selectedState);
            Refresh();
        }
    }
}
