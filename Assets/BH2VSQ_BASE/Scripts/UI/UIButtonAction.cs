using UdonSharp;

namespace BH2VSQ.Base
{
    public class UIButtonAction : UdonSharpBehaviour
    {
        public int action;
        public int value;
        public TabMenuController menu;
        public PermissionLoginPanel login;
        public TeleportPanel teleport;
        public PlayerListPanel players;
        public PlayerDetailPanel detail;
        public TeleportRequestManager requests;
        public RequestPanel requestPanel;
        public AdminPanel admin;

        public void Click()
        {
            if (action == 2 && menu != null) menu.ShowTeleport();
            else if (action == 3 && menu != null) menu.ShowPlayers();
            else if (action == 4 && menu != null) menu.ShowAdmin();
            else if (action == 5 && menu != null) menu.ShowRequests();
            else if (action == 10 && login != null) login.Submit();
            else if (action == 20 && teleport != null) teleport.ToLocation(value);
            else if (action == 26 && teleport != null) teleport.PreviousPage();
            else if (action == 27 && teleport != null) teleport.NextPage();
            else if (action == 31 && detail != null) detail.ShowPlayer(value);
            else if (action == 33 && players != null) players.PreviousPage();
            else if (action == 34 && players != null) players.NextPage();
            else if (action == 32 && detail != null && requests != null)
            {
                if (!requests.Send(detail.selectedPlayerId, value) && requestPanel != null) requestPanel.ShowNotice("请求发送失败或已有相同申请");
            }
            else if (action == 40 && requestPanel != null) requestPanel.Accept(value);
            else if (action == 41 && requestPanel != null) requestPanel.Reject(value);
            else if (action == 43 && requestPanel != null) requestPanel.PreviousPage();
            else if (action == 44 && requestPanel != null) requestPanel.NextPage();
            else if (action == 60 && admin != null) { admin.selectedFloor = value; admin.Refresh(); }
            else if (action == 61 && admin != null) { admin.selectedState = value; admin.Refresh(); }
            else if (action == 62 && admin != null) admin.ApplyFloorState();
            else if (action == 63 && admin != null) admin.ShowPopulation();
            else if (action == 65 && admin != null && admin.CanOpenPlayerManagement() && menu != null)
            {
                menu.ShowPlayers();
                if (players != null) players.Refresh();
            }
            else if (action == 66 && admin != null) admin.PreviousFloorPage();
            else if (action == 67 && admin != null) admin.NextFloorPage();
            else if (action == 68 && admin != null) admin.PreviousPopulationPage();
            else if (action == 69 && admin != null) admin.NextPopulationPage();
        }
    }
}
