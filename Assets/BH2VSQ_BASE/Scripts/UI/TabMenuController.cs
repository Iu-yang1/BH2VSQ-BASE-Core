using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BH2VSQ.Base
{
    public class TabMenuController : UdonSharpBehaviour
    {
        public PermissionManager permission;
        public PersonalInfoPanel header;
        public GameObject teleport;
        public GameObject players;
        public GameObject requests;
        public GameObject admin;
        public GameObject adminTab;
        public TeleportPanel teleportPanel;
        public PlayerListPanel playerPanel;
        public RequestPanel requestPanel;
        public AdminPanel adminPanel;
        public Image[] tabBackgrounds;
        public Image[] tabIndicators;
        public TMP_Text[] tabLabels;
        private int currentTab;
        private int appliedTab = -1;

        public void Refresh() { RefreshVisible(); }
        public void RefreshVisible()
        {
            bool isAdmin = permission != null && permission.IsAdmin();
            if (adminTab != null) adminTab.SetActive(isAdmin);
            if (currentTab == 3 && !isAdmin) currentTab = 0;
            if (appliedTab != currentTab) Show(currentTab);
            if (header != null) header.Refresh();
            if (currentTab == 0 && teleportPanel != null) teleportPanel.Refresh();
            if (currentTab == 1 && playerPanel != null) playerPanel.Refresh();
            if (currentTab == 2 && requestPanel != null) requestPanel.Refresh();
            if (currentTab == 3 && adminPanel != null) adminPanel.Refresh();
        }

        public void ShowTeleport() { Show(0); RefreshVisible(); }
        public void ShowPlayers() { Show(1); RefreshVisible(); }
        public void ShowRequests() { Show(2); RefreshVisible(); }
        public void ShowAdmin() { if (permission != null && permission.IsAdmin()) { Show(3); RefreshVisible(); } }
        private void Show(int tab)
        {
            currentTab = tab;
            if (appliedTab == tab) return;
            appliedTab = tab;
            if (teleport != null) teleport.SetActive(tab == 0);
            if (players != null) players.SetActive(tab == 1);
            if (requests != null) requests.SetActive(tab == 2);
            if (admin != null) admin.SetActive(tab == 3 && permission != null && permission.IsAdmin());
            for (int i = 0; tabBackgrounds != null && i < tabBackgrounds.Length; i++)
            {
                bool active = i == tab;
                if (tabBackgrounds[i] != null) tabBackgrounds[i].color = active ? new Color(.04f, .46f, .65f, .95f) : new Color(.02f, .16f, .28f, .9f);
                if (tabIndicators != null && i < tabIndicators.Length && tabIndicators[i] != null) tabIndicators[i].color = active ? new Color(.26f, .94f, 1f, 1f) : new Color(.18f, .39f, .51f, .65f);
                if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null) tabLabels[i].color = active ? Color.white : new Color(.65f, .82f, .9f);
            }
        }
    }
}
