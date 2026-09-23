using System.Collections.Generic;
using System.Text;
using BH2VSQ.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;

namespace BH2VSQ.Base.Editor
{
    public static class BaseValidator
    {
        [MenuItem("BH2VSQ BASE/验证配置 (Validate Setup)")]
        public static void ValidateMenu() { ValidateSelection(true); }

        public static bool ValidateSelection(bool showDialog)
        {
            GameObject root = Selection.activeGameObject;
            BaseWorldSystem selected = root == null ? null : root.GetComponentInParent<BaseWorldSystem>();
            if (selected != null) root = selected.gameObject;
            if (root == null || root.GetComponentInChildren<BaseWorldSystem>(true) == null)
                root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BH2VSQ_BASE/Prefabs/Core/BH2VSQ_BASE_Core.prefab");
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            if (root == null) errors.Add("缺少核心预制件，请先生成插件包。");
            else Validate(root, errors, warnings);
            StringBuilder result = new StringBuilder();
            result.AppendLine(errors.Count == 0 ? "静态引用验证通过。" : "配置验证失败：");
            foreach (string error in errors) result.AppendLine("错误：" + error);
            foreach (string warning in warnings) result.AppendLine("提示：" + warning);
            result.AppendLine("网络行为仍需在 VRChat 多客户端环境中验证。");
            if (errors.Count > 0) Debug.LogError(result.ToString()); else Debug.Log(result.ToString());
            if (showDialog) EditorUtility.DisplayDialog("BH2VSQ BASE 配置验证", result.ToString(), "确定");
            return errors.Count == 0;
        }

        private static void Validate(GameObject root, List<string> errors, List<string> warnings)
        {
            BaseWorldSystem world = root.GetComponentInChildren<BaseWorldSystem>(true);
            FloorManager floors = root.GetComponentInChildren<FloorManager>(true);
            AreaManager areas = root.GetComponentInChildren<AreaManager>(true);
            TOTPAuthManager auth = root.GetComponentInChildren<TOTPAuthManager>(true);
            PermissionManager permission = root.GetComponentInChildren<PermissionManager>(true);
            TeleportManager teleport = root.GetComponentInChildren<TeleportManager>(true);
            TabMenuController menu = root.GetComponentInChildren<TabMenuController>(true);
            TeleportPanel panel = root.GetComponentInChildren<TeleportPanel>(true);
            PlayerListPanel playerPanel = root.GetComponentInChildren<PlayerListPanel>(true);
            AdminPanel admin = root.GetComponentInChildren<AdminPanel>(true);
            RequestPanel requestPanel = root.GetComponentInChildren<RequestPanel>(true);
            TeleportRequestManager requests = root.GetComponentInChildren<TeleportRequestManager>(true);
            LocalizationManager localization = root.GetComponentInChildren<LocalizationManager>(true);
            if (world == null || world.registry == null || world.playerData == null || world.tracker == null || world.session == null || world.access == null || world.teleport == null)
                errors.Add("核心管理器引用不完整。");
            if (floors == null || floors.teleport != teleport || areas == null || areas.teleport != teleport)
                errors.Add("楼层与地点视图未连接到传送管理器。");
            if (permission == null || permission.session == null || auth == null || auth.session == null)
                errors.Add("认证组件引用不完整。");
            if (auth != null)
            {
                SerializedObject serialized = new SerializedObject(auth);
                SerializedProperty member = serialized.FindProperty("memberTotpSecret");
                SerializedProperty administrator = serialized.FindProperty("adminTotpSecret");
                if (member == null || administrator == null || string.IsNullOrWhiteSpace(member.stringValue) || string.IsNullOrWhiteSpace(administrator.stringValue))
                    warnings.Add("TOTP 密钥为空。请只在场景实例中配置，避免提交到公开仓库。");
            }
            if (teleport == null || teleport.access == null || teleport.pointRoot == null)
                errors.Add("传送管理器、权限管理器或地点根节点缺失。");
            else
            {
                TeleportPoint[] points = teleport.pointRoot.GetComponentsInChildren<TeleportPoint>(true);
                if (points.Length == 0) errors.Add("没有传送点。请将传送点预制件放在 Teleport 节点下。");
                HashSet<int> ids = new HashSet<int>();
                Dictionary<int, string> floorNames = new Dictionary<int, string>();
                int safeCount = 0;
                foreach (TeleportPoint point in points)
                {
                    if (!ids.Add(point.locationId)) errors.Add("地点 ID 重复：" + point.locationId);
                    if (string.IsNullOrWhiteSpace(point.chineseName)) errors.Add("地点缺少中文名称：" + point.name);
                    if (string.IsNullOrWhiteSpace(point.chineseFloorName)) errors.Add("楼层缺少中文名称：" + point.name);
                    if (point.requiredRank < BaseRank.Visitor || point.requiredRank > BaseRank.Admin) errors.Add("权限等级无效：" + point.name);
                    if (point.xpMultiplier <= 0) errors.Add("经验倍率必须大于零：" + point.name);
                    if (point.isSafeFallback)
                    {
                        safeCount++;
                        if (world != null && world.returnUnauthorizedPlayersToSafePoint && point.requiredRank != BaseRank.Visitor)
                            errors.Add("自动回退的安全点必须允许游客进入：" + point.name);
                    }
                    string previous;
                    if (floorNames.TryGetValue(point.floorId, out previous) && previous != point.floorName)
                        warnings.Add("同一楼层 ID 使用了不同名称：" + point.floorId);
                    else floorNames[point.floorId] = point.floorName;
                    AreaTrigger trigger = point.GetComponentInChildren<AreaTrigger>(true);
                    if (trigger == null || (trigger.point != null && trigger.point != point) || (trigger.world != null && trigger.world != world)) errors.Add("传送点缺少已连接的区域触发器：" + point.name);
                    else
                    {
                        BoxCollider collider = trigger.GetComponent<BoxCollider>();
                        if (collider == null || !collider.isTrigger) errors.Add("区域触发器的 BoxCollider 未设为 Trigger：" + point.name);
                        else if (point.areaVolume != collider) errors.Add("传送点未缓存对应的区域碰撞体：" + point.name);
                    }
                }
                if (safeCount > 1 || (world != null && world.returnUnauthorizedPlayersToSafePoint && safeCount != 1)) errors.Add("自动回退开启时必须且只能有一个安全传送点，当前数量：" + safeCount);
                if (teleport.points == null || teleport.points.Length != points.Length)
                    warnings.Add("传送点序列化数组与层级不一致；运行时会自动扫描 Teleport 根节点。");
            }
            if (localization == null || localization.chinese == null || localization.chinese.Length != BaseText.EntryCount)
                errors.Add("中文文本表长度不正确，请重新生成默认数据与预制件。");
            if (menu == null || menu.header == null || menu.teleport == null || menu.players == null || menu.requests == null || menu.admin == null) errors.Add("主菜单结构不完整。");
            if (menu != null && (menu.tabBackgrounds == null || menu.tabBackgrounds.Length != 4 || menu.tabIndicators == null || menu.tabIndicators.Length != 4))
                errors.Add("四个标签的当前页高亮组件不完整。");
            if (world != null && (world.menuCanvas == null || world.menuFollower == null || world.menuCanvasGroup == null || world.menuCanvas.GetComponent<Canvas>() == null))
                errors.Add("Tab 菜单画布或头部定位组件未连接。");
            if (world != null && world.menuCanvas != null && EditorUtility.IsPersistent(root) && world.menuCanvas.activeSelf)
                errors.Add("核心预制体中的 Tab 菜单应默认隐藏。");
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.name == "RequestNoticeCanvas") continue;
                if (canvas.GetComponent<VRCUiShape>() == null || canvas.GetComponent<BoxCollider>() == null || canvas.GetComponent<GraphicRaycaster>() == null)
                    errors.Add("交互画布缺少 VRC_UIShape、BoxCollider 或 GraphicRaycaster：" + canvas.name);
                if (canvas.gameObject.layer == 5) errors.Add("交互画布不能使用 UI 图层：" + canvas.name);
            }
            foreach (UIButtonAction action in root.GetComponentsInChildren<UIButtonAction>(true))
            {
                Button button = action.GetComponent<Button>();
                if (button == null || button.onClick.GetPersistentEventCount() == 0 || button.onClick.GetPersistentTarget(0) == null || button.onClick.GetPersistentMethodName(0) != "SendCustomEvent")
                    errors.Add("按钮未连接到 Udon Click 事件：" + action.name);
            }
            if (!EditorUtility.IsPersistent(root) && UnityEngine.Object.FindObjectsOfType<EventSystem>().Length != 1)
                errors.Add("场景需要且仅能有一个有效 EventSystem。");
            if (panel == null || panel.locationObjects == null || panel.locationActions == null || panel.locationLabels == null || panel.locationBackgrounds == null || panel.locationObjects.Length == 0 || panel.locationObjects.Length != panel.locationActions.Length || panel.locationObjects.Length != panel.locationLabels.Length || panel.locationObjects.Length != panel.locationBackgrounds.Length)
                errors.Add("地点菜单按钮池不完整。");
            if (playerPanel == null || playerPanel.rowObjects == null || playerPanel.rowActions == null || playerPanel.rowTexts == null ||
                playerPanel.rowObjects.Length != BaseConstants.PlayerRowsPerPage || playerPanel.rowActions.Length != playerPanel.rowObjects.Length || playerPanel.rowTexts.Length != playerPanel.rowObjects.Length ||
                playerPanel.previousButton == null || playerPanel.nextButton == null || playerPanel.pageText == null)
                errors.Add("玩家列表分页按钮池不完整或仍在一次性创建全部玩家行。");
            if (admin == null || admin.floorObjects == null || admin.floorActions == null || admin.floorLabels == null || admin.floorObjects.Length == 0 || admin.floorObjects.Length != admin.floorActions.Length || admin.floorObjects.Length != admin.floorLabels.Length)
                errors.Add("楼层管理按钮池不完整。");
            if (requests == null || requestPanel == null || requests.panel != requestPanel || requestPanel.noticeRoot == null || requestPanel.rowObjects == null || requestPanel.rowObjects.Length == 0 ||
                requestPanel.acceptActions == null || requestPanel.rejectActions == null || requestPanel.requesterTexts == null || requestPanel.locationTexts == null ||
                requestPanel.acceptActions.Length != requestPanel.rowObjects.Length || requestPanel.rejectActions.Length != requestPanel.rowObjects.Length ||
                requestPanel.requesterTexts.Length != requestPanel.rowObjects.Length || requestPanel.locationTexts.Length != requestPanel.rowObjects.Length)
                errors.Add("传送请求列表、结果提示或引用不完整。");
            if (requests != null && (requests.requestIds == null || requests.requestIds.Length != TeleportRequestManager.Capacity)) errors.Add("传送请求同步槽数量不正确。");
            if (world != null && (world.tracker == null || world.tracker.teleport != teleport || world.tracker.world != world)) errors.Add("自动位置检测未连接。");
            if (root.GetComponentInChildren<LocalCanvasFollower>(true) == null) errors.Add("本地界面跟随组件缺失。");
        }
    }
}
