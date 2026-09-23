using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace BH2VSQ.Base
{
    public class BaseWorldSystem : UdonSharpBehaviour
    {
        public PlayerRegistry registry;
        public PlayerDataManager playerData;
        public PlayerAreaTracker tracker;
        public AuthenticationSession session;
        public FloorManager floors;
        public AccessManager access;
        public TeleportManager teleport;
        public TabMenuController menu;
        public GameObject menuCanvas;
        public LocalCanvasFollower menuFollower;
        public CanvasGroup menuCanvasGroup;
        [Range(0f, 1f)] public float menuOpacity = .86f;
        public bool returnUnauthorizedPlayersToSafePoint;
        public bool ready;
        private bool vrMenuOpen;
        private float nextUiRefresh;

        private void Start()
        {
            if (menuCanvas != null) menuCanvas.SetActive(false);
            if (menuCanvasGroup != null) menuCanvasGroup.alpha = Mathf.Clamp01(menuOpacity);
            ready = registry != null && playerData != null && tracker != null && session != null && floors != null && access != null && teleport != null;
            if (!ready) Debug.LogError("BH2VSQ BASE：核心引用缺失，请运行配置验证。");
            else
            {
                registry.Refresh();
                session.ResetSession();
                if (menu != null) menu.Refresh();
            }
        }

        private void Update()
        {
            bool visible = Input.GetKey(KeyCode.Tab) || vrMenuOpen;
            if (menuCanvas != null && menuCanvas.activeSelf != visible) SetMenuVisible(visible);
            if (visible && Time.time >= nextUiRefresh)
            {
                nextUiRefresh = Time.time + 2f;
                if (menu != null) menu.RefreshVisible();
            }
        }

        public void ToggleMenu()
        {
            vrMenuOpen = !vrMenuOpen;
            SetMenuVisible(Input.GetKey(KeyCode.Tab) || vrMenuOpen);
        }

        private void SetMenuVisible(bool show)
        {
            if (menuCanvas == null) return;
            menuCanvas.SetActive(show);
            if (!show) return;
            if (menuFollower != null) menuFollower.Place();
            if (menu != null) menu.RefreshVisible();
            nextUiRefresh = Time.time + 2f;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (registry != null) registry.Refresh();
            if (menu != null) menu.Refresh();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (registry != null) registry.Refresh();
            if (menu != null) menu.Refresh();
        }
    }
}
