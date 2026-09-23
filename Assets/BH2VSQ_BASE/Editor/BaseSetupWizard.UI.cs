using System;
using System.Reflection;
using BH2VSQ.Base;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRC.SDK3.Components;

namespace BH2VSQ.Base.Editor
{
    public partial class BaseSetupWizard
    {
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float width, float height, int size = 22)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = new Color(.82f, .95f, 1f);
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/BH2VSQ_UI.asset");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Packages/com.unity.textmeshpro/Package Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font != null) text.font = font;
            return text;
        }

        private static TMP_InputField Input(Transform parent, string name, float x, float y, float width, float height, string hint)
        {
            RectTransform rect = Panel(parent, name, x, y, width, height, new Color(.02f, .15f, .26f, .95f));
            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            rect.GetComponent<Image>().raycastTarget = true;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            input.characterLimit = 6;
            RectTransform viewport = Rect(rect, "Text Area", 12, -5, width - 24, height - 10);
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text text = Text(viewport, "Text", "", 0, 0, width - 24, height - 10, 19);
            TMP_Text placeholder = Text(viewport, "Placeholder", hint, 0, 0, width - 24, height - 10, 19);
            placeholder.color = new Color(.42f, .69f, .78f);
            input.textViewport = viewport;
            input.textComponent = (TextMeshProUGUI)text;
            input.placeholder = (TextMeshProUGUI)placeholder;
            return input;
        }

        private static UIButtonAction Button(Transform parent, string name, string label, int action, int value, float x, float y, float width, float height)
        {
            RectTransform rect = Panel(parent, name, x, y, width, height, new Color(.02f, .27f, .43f, .9f));
            Button button = rect.gameObject.AddComponent<Button>();
            rect.GetComponent<Image>().raycastTarget = true;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(.12f, .72f, .9f, 1f);
            colors.pressedColor = new Color(.03f, .45f, .65f, 1f);
            button.colors = colors;
            TMP_Text text = Text(rect, "Label", label, 8, -2, width - 16, height - 4, 18);
            text.alignment = TextAlignmentOptions.Center;
            UIButtonAction handler = rect.gameObject.AddUdonSharpComponent<UIButtonAction>();
            handler.action = action;
            handler.value = value;
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(handler);
            UnityEventTools.AddStringPersistentListener(button.onClick, backing.SendCustomEvent, "Click");
            return handler;
        }

        private static Canvas Canvas(Transform parent, string name, Vector3 offset, int order, bool interactive)
        {
            RectTransform rect = Rect(parent, name, 0, 0, 900, 650);
            rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one * .0015f;
            rect.gameObject.layer = 0;
            Canvas canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            rect.gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 50;
            if (interactive)
            {
                rect.gameObject.AddComponent<GraphicRaycaster>();
                rect.gameObject.AddComponent<VRCUiShape>();
                BoxCollider collider = rect.gameObject.GetComponent<BoxCollider>();
                if (collider == null) collider = rect.gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(900f, 650f, 1f);
            }
            LocalCanvasFollower follower = rect.gameObject.AddUdonSharpComponent<LocalCanvasFollower>();
            follower.target = rect;
            follower.offset = offset;
            return canvas;
        }

        private static UIButtonAction TabButton(Transform parent, string name, string label, string icon, int action, float x, Image[] backgrounds, Image[] indicators, TMP_Text[] labels, int index)
        {
            UIButtonAction button = Button(parent, name, label, action, 0, x, -144, 200, 44);
            backgrounds[index] = button.GetComponent<Image>();
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(50, -2);
            textRect.sizeDelta = new Vector2(140, 40);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            labels[index] = text;
            RectTransform iconRoot = Panel(button.transform, "ActiveIcon", 14, -10, 28, 24, new Color(.18f, .39f, .51f));
            indicators[index] = iconRoot.GetComponent<Image>();
            Text(iconRoot, "Icon", icon, 0, 0, 28, 24, 19).alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static void BuildUI(Transform parent, BaseWorldSystem world, PlayerRegistry registry, PlayerDataManager data,
            PlayerAreaTracker tracker, FloorManager floors, AreaManager areas, PermissionManager permission,
            TOTPAuthManager auth, TeleportManager teleport, TeleportRequestManager requests, RadioDutyManager radio,
            AreaPopulationManager population, AdminManager admin, FloorAdminManager floorAdmin, LocalizationManager localization)
        {
            GameObject eventSystem = Group(parent, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Canvas mainCanvas = Canvas(parent, "MainCanvas", new Vector3(-.5f, -.05f, 1.45f), 1, true);
            world.menuCanvas = mainCanvas.gameObject;
            world.menuFollower = mainCanvas.GetComponent<LocalCanvasFollower>();
            world.menuCanvasGroup = mainCanvas.gameObject.AddComponent<CanvasGroup>();
            world.menuCanvasGroup.alpha = world.menuOpacity;
            RectTransform menuRoot = Panel(mainCanvas.transform, "TabMenu", 0, 0, 900, 650, new Color(.008f, .055f, .12f, .82f));
            Panel(menuRoot, "TopAccent", 0, 0, 900, 3, new Color(.12f, .83f, 1f));
            TabMenuController menu = menuRoot.gameObject.AddUdonSharpComponent<TabMenuController>();
            menu.permission = permission;
            world.menu = menu;

            RectTransform header = Panel(menuRoot, "Header", 18, -14, 864, 112, new Color(.025f, .13f, .23f, .96f));
            RectTransform infoRoot = Panel(header, "PersonalInfo", 10, -8, 515, 96, new Color(.025f, .18f, .28f, .9f));
            PersonalInfoPanel personal = infoRoot.gameObject.AddUdonSharpComponent<PersonalInfoPanel>();
            personal.nameText = Text(infoRoot, "PlayerName", "玩家", 14, -5, 250, 28, 24);
            personal.rankText = Text(infoRoot, "Rank", "访客", 272, -5, 85, 28, 19);
            personal.levelText = Text(infoRoot, "Level", "等级 1", 14, -38, 110, 27, 17);
            personal.xpText = Text(infoRoot, "Experience", "经验 0 / 100", 130, -38, 175, 27, 17);
            personal.timeText = Text(infoRoot, "PlayTime", "游戏时长 0小时 0分", 310, -38, 200, 27, 16);
            RectTransform xpTrack = Panel(infoRoot, "ExperienceTrack", 14, -82, 485, 7, new Color(.01f, .09f, .16f));
            personal.xpFill = Panel(xpTrack, "ExperienceFill", 0, 0, 485, 7, new Color(.14f, .9f, 1f));
            personal.data = data;
            personal.permission = permission;
            personal.tracker = tracker;
            personal.floors = floors;
            personal.areas = areas;
            personal.localization = localization;
            menu.header = personal;

            RectTransform loginRoot = Panel(header, "PermissionLogin", 535, -8, 315, 96, new Color(.018f, .1f, .2f, .94f));
            PermissionLoginPanel login = loginRoot.gameObject.AddUdonSharpComponent<PermissionLoginPanel>();
            login.codeInput = Input(loginRoot, "TOTP Code", 12, -27, 200, 42, "输入六位验证码");
            Button(loginRoot, "LoginButton", "确认", 10, 0, 222, -27, 80, 42);
            login.auth = auth;
            login.menu = menu;
            login.localization = localization;

            menu.tabBackgrounds = new Image[4];
            menu.tabIndicators = new Image[4];
            menu.tabLabels = new TMP_Text[4];
            TabButton(menuRoot, "TeleportTab", "传送", "↔", 2, 20, menu.tabBackgrounds, menu.tabIndicators, menu.tabLabels, 0);
            TabButton(menuRoot, "PlayersTab", "玩家", "●", 3, 240, menu.tabBackgrounds, menu.tabIndicators, menu.tabLabels, 1);
            TabButton(menuRoot, "RequestsTab", "申请", "◎", 5, 460, menu.tabBackgrounds, menu.tabIndicators, menu.tabLabels, 2);
            menu.adminTab = TabButton(menuRoot, "AdminTab", "管理", "▦", 4, 680, menu.tabBackgrounds, menu.tabIndicators, menu.tabLabels, 3).gameObject;

            RectTransform teleportRoot = Panel(menuRoot, "Teleport", 20, -198, 860, 430, new Color(.025f, .12f, .2f, .95f));
            TeleportPanel teleportPanel = teleportRoot.gameObject.AddUdonSharpComponent<TeleportPanel>();
            teleportPanel.teleport = teleport;
            teleportPanel.tracker = tracker;
            teleportPanel.population = population;
            teleportPanel.localization = localization;
            teleportPanel.feedback = Text(teleportRoot, "TeleportStatus", "请选择目的地", 20, -390, 390, 32, 17);
            Text(teleportRoot, "LocationTitle", "传送目的地  /  区域人数", 20, -10, 500, 33, 22);
            teleportPanel.locationActions = new UIButtonAction[14];
            teleportPanel.locationLabels = new TMP_Text[14];
            teleportPanel.locationObjects = new GameObject[14];
            for (int i = 0; i < 14; i++)
            {
                UIButtonAction item = Button(teleportRoot, "LocationItem_" + i, "", 20, 0,
                    20 + (i / 7) * 410, -52 - (i % 7) * 47, 390, 42);
                teleportPanel.locationActions[i] = item;
                teleportPanel.locationLabels[i] = item.GetComponentInChildren<TMP_Text>();
                teleportPanel.locationObjects[i] = item.gameObject;
                item.gameObject.SetActive(false);
            }
            teleportPanel.previousButton = Button(teleportRoot, "PreviousLocations", "上一页", 26, 0, 490, -390, 105, 32).gameObject;
            teleportPanel.pageLabel = Text(teleportRoot, "LocationPage", "1 / 1", 610, -390, 100, 32, 17);
            teleportPanel.nextButton = Button(teleportRoot, "NextLocations", "下一页", 27, 0, 730, -390, 105, 32).gameObject;
            menu.teleport = teleportRoot.gameObject;
            menu.teleportPanel = teleportPanel;

            RectTransform playersRoot = Panel(menuRoot, "PlayerList", 20, -198, 860, 430, new Color(.025f, .12f, .2f, .95f));
            PlayerListPanel playerList = playersRoot.gameObject.AddUdonSharpComponent<PlayerListPanel>();
            playerList.registry = registry;
            playerList.tracker = tracker;
            playerList.data = data;
            playerList.radio = radio;
            playerList.floors = floors;
            playerList.areas = areas;
            playerList.localization = localization;
            playerList.output = Text(playersRoot, "Count", "在线玩家", 20, -10, 790, 33, 21);
            RectTransform viewport = Panel(playersRoot, "Viewport", 20, -52, 490, 356, new Color(.012f, .08f, .14f));
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            RectTransform content = Rect(viewport, "Content", 0, 0, 470, 80 * 58);
            scroll.viewport = viewport;
            scroll.content = content;
            playerList.rowTexts = new TMP_Text[BaseConstants.MaxPlayers];
            playerList.rowActions = new UIButtonAction[BaseConstants.MaxPlayers];
            playerList.rowObjects = new GameObject[BaseConstants.MaxPlayers];
            for (int i = 0; i < BaseConstants.MaxPlayers; i++)
            {
                UIButtonAction row = Button(content, "PlayerItem_" + i, "", 31, 0, 2, -i * 58, 460, 54);
                playerList.rowActions[i] = row;
                playerList.rowObjects[i] = row.gameObject;
                playerList.rowTexts[i] = row.GetComponentInChildren<TMP_Text>();
                playerList.rowTexts[i].fontSize = 16;
                row.gameObject.SetActive(false);
            }
            RectTransform detailRoot = Panel(playersRoot, "PlayerDetail", 525, -52, 310, 356, new Color(.035f, .17f, .27f));
            PlayerDetailPanel detail = detailRoot.gameObject.AddUdonSharpComponent<PlayerDetailPanel>();
            detail.output = Text(detailRoot, "DetailText", "请选择玩家", 12, -12, 286, 155, 19);
            detail.tracker = tracker;
            detail.registry = registry;
            detail.floors = floors;
            detail.areas = areas;
            detail.localization = localization;
            playerList.detail = detail;
            Button(detailRoot, "GoToPlayer", "申请传送至玩家", 32, 0, 15, -220, 280, 45);
            Button(detailRoot, "InvitePlayer", "邀请玩家传送", 32, 1, 15, -280, 280, 45);
            menu.players = playersRoot.gameObject;
            menu.playerPanel = playerList;
            playersRoot.gameObject.SetActive(false);

            GameObject requestService = Group(parent, "RequestService");
            RequestPanel requestPanel = requestService.AddUdonSharpComponent<RequestPanel>();
            requestPanel.requests = requests;
            requestPanel.tracker = tracker;
            requestPanel.areas = areas;
            requestPanel.localization = localization;
            requests.panel = requestPanel;
            login.notice = requestPanel;
            Canvas noticeCanvas = Canvas(requestService.transform, "RequestNoticeCanvas", new Vector3(0f, .16f, 1f), 5, false);
            RectTransform noticeBox = Panel(noticeCanvas.transform, "RequestNotice", 100, -40, 700, 66, new Color(.015f, .2f, .3f, .98f));
            Panel(noticeBox, "NoticeAccent", 0, 0, 6, 66, new Color(.16f, .93f, 1f));
            requestPanel.noticeText = Text(noticeBox, "NoticeText", "", 20, -10, 660, 46, 20);
            requestPanel.noticeRoot = noticeCanvas.gameObject;
            requestPanel.noticeFollower = noticeCanvas.GetComponent<LocalCanvasFollower>();
            noticeCanvas.gameObject.SetActive(false);

            RectTransform requestsRoot = Panel(menuRoot, "Requests", 20, -198, 860, 430, new Color(.025f, .12f, .2f, .95f));
            requestPanel.root = requestsRoot.gameObject;
            requestPanel.countText = Text(requestsRoot, "RequestCount", "待处理申请  0", 20, -10, 500, 34, 22);
            requestPanel.emptyText = Text(requestsRoot, "Empty", "暂无待处理申请", 270, -185, 330, 44, 23);
            requestPanel.rowObjects = new GameObject[5];
            requestPanel.requesterTexts = new TMP_Text[5];
            requestPanel.locationTexts = new TMP_Text[5];
            requestPanel.acceptActions = new UIButtonAction[5];
            requestPanel.rejectActions = new UIButtonAction[5];
            for (int i = 0; i < 5; i++)
            {
                RectTransform row = Panel(requestsRoot, "RequestRow_" + i, 20, -57 - i * 61, 820, 54, new Color(.035f, .2f, .29f, .94f));
                Panel(row, "RowAccent", 0, 0, 4, 54, new Color(.13f, .74f, .92f));
                requestPanel.rowObjects[i] = row.gameObject;
                requestPanel.requesterTexts[i] = Text(row, "Requester", "", 16, -6, 270, 42, 18);
                requestPanel.locationTexts[i] = Text(row, "Location", "", 300, -6, 310, 42, 18);
                requestPanel.locationTexts[i].alignment = TextAlignmentOptions.Center;
                requestPanel.acceptActions[i] = Button(row, "Accept", "✓", 40, 0, 670, -6, 60, 42);
                requestPanel.rejectActions[i] = Button(row, "Reject", "×", 41, 0, 742, -6, 60, 42);
                requestPanel.acceptActions[i].GetComponent<Image>().color = new Color(.06f, .54f, .37f);
                requestPanel.rejectActions[i].GetComponent<Image>().color = new Color(.68f, .16f, .19f);
                row.gameObject.SetActive(false);
            }
            requestPanel.previousButton = Button(requestsRoot, "PreviousRequests", "上一页", 43, 0, 540, -386, 105, 33).gameObject;
            requestPanel.pageText = Text(requestsRoot, "RequestPage", "1 / 1", 665, -386, 70, 33, 17);
            requestPanel.nextButton = Button(requestsRoot, "NextRequests", "下一页", 44, 0, 735, -386, 105, 33).gameObject;
            menu.requests = requestsRoot.gameObject;
            menu.requestPanel = requestPanel;
            requestsRoot.gameObject.SetActive(false);

            RectTransform adminRoot = Panel(menuRoot, "AdminPanel", 20, -198, 860, 430, new Color(.025f, .12f, .2f, .95f));
            AdminPanel adminPanel = adminRoot.gameObject.AddUdonSharpComponent<AdminPanel>();
            adminPanel.admin = admin;
            adminPanel.floorAdmin = floorAdmin;
            adminPanel.floors = floors;
            adminPanel.population = population;
            adminPanel.radio = radio;
            adminPanel.areas = areas;
            adminPanel.localization = localization;
            adminPanel.status = Text(adminRoot, "AdminStatus", "仅管理员可用", 20, -8, 800, 36, 19);
            RectTransform floorAdminRoot = Panel(adminRoot, "FloorAdmin", 20, -50, 820, 124, new Color(.035f, .17f, .27f));
            adminPanel.floorActions = new UIButtonAction[6];
            adminPanel.floorLabels = new TMP_Text[6];
            adminPanel.floorObjects = new GameObject[6];
            adminPanel.previousFloorButton = Button(floorAdminRoot, "PreviousFloors", "◀", 66, 0, 5, -5, 85, 40).gameObject;
            for (int i = 0; i < 6; i++)
            {
                UIButtonAction selector = Button(floorAdminRoot, "SelectFloor_" + i, "", 60, 0, 95 + i * 102, -5, 98, 40);
                adminPanel.floorActions[i] = selector;
                adminPanel.floorLabels[i] = selector.GetComponentInChildren<TMP_Text>();
                adminPanel.floorObjects[i] = selector.gameObject;
                selector.gameObject.SetActive(false);
            }
            adminPanel.nextFloorButton = Button(floorAdminRoot, "NextFloors", "▶", 67, 0, 714, -5, 100, 40).gameObject;
            Button(floorAdminRoot, "Open", "开放", 61, 0, 5, -65, 130, 40);
            Button(floorAdminRoot, "Reserved", "包场", 61, 1, 145, -65, 130, 40);
            Button(floorAdminRoot, "Maintenance", "维护", 61, 2, 285, -65, 145, 40);
            Button(floorAdminRoot, "ApplyFloor", "应用状态", 62, 0, 450, -65, 145, 40);
            Button(adminRoot, "PlayerManagementTab", "玩家管理", 65, 0, 20, -186, 170, 35);
            Button(adminRoot, "PopulationTab", "人数统计", 63, 0, 205, -186, 170, 35);
            RectTransform populationRoot = Panel(adminRoot, "PopulationPanel", 20, -229, 820, 188, new Color(.035f, .17f, .27f));
            adminPanel.populationRoot = populationRoot.gameObject;
            adminPanel.populationLeft = Text(populationRoot, "PopulationLeft", "", 15, -12, 390, 130, 17);
            adminPanel.populationRight = Text(populationRoot, "PopulationRight", "", 420, -12, 390, 130, 17);
            adminPanel.previousPopulationButton = Button(populationRoot, "PreviousPopulation", "上一页", 68, 0, 15, -148, 125, 34).gameObject;
            adminPanel.nextPopulationButton = Button(populationRoot, "NextPopulation", "下一页", 69, 0, 680, -148, 125, 34).gameObject;
            menu.admin = adminRoot.gameObject;
            menu.adminPanel = adminPanel;
            adminRoot.gameObject.SetActive(false);

            foreach (UIButtonAction action in parent.GetComponentsInChildren<UIButtonAction>(true))
            {
                action.menu = menu;
                action.login = login;
                action.teleport = teleportPanel;
                action.players = playerList;
                action.detail = detail;
                action.requests = requests;
                action.requestPanel = requestPanel;
                action.admin = adminPanel;
            }
            mainCanvas.gameObject.SetActive(false);
        }

        private static void CreateComponentPrefabs(GameObject core)
        {
            string ui = Root + "/Prefabs/UI/";
            SavePart(core, "UI/MainCanvas/TabMenu", ui + "BH2VSQ_TabMenu.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/Header/PersonalInfo", ui + "BH2VSQ_PersonalInfo.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/Header/PermissionLogin", ui + "BH2VSQ_PermissionLogin.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/Teleport", ui + "BH2VSQ_Teleport.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/PlayerList", ui + "BH2VSQ_PlayerList.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/PlayerList/Viewport/Content/PlayerItem_0", ui + "BH2VSQ_PlayerListItem.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/PlayerList/PlayerDetail", ui + "BH2VSQ_PlayerDetail.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/Requests", ui + "BH2VSQ_TeleportRequest.prefab");
            SavePart(core, "UI/RequestService", ui + "BH2VSQ_RequestService.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/AdminPanel", ui + "BH2VSQ_AdminPanel.prefab");
            SavePart(core, "UI/MainCanvas/TabMenu/AdminPanel/FloorAdmin", ui + "BH2VSQ_FloorAdmin.prefab");
            SavePart(core, "Floor/FloorManager", Root + "/Prefabs/Floor/BH2VSQ_FloorController.prefab");
            CreateFloorBarrierPrefab(core);
            SavePart(core, "Floor/AreaManager", Root + "/Prefabs/Floor/BH2VSQ_AreaController.prefab");
            SavePart(core, "Teleport/Point_1F_Living", Root + "/Prefabs/Teleport/BH2VSQ_TeleportPoint.prefab");
            SavePart(core, "Teleport/Point_1F_Living/AreaTrigger", Root + "/Prefabs/Floor/BH2VSQ_AreaTrigger.prefab");
            PrefabUtility.SaveAsPrefabAsset(core, Root + "/Prefabs/Demo/BH2VSQ_DemoBase.prefab");
        }


        private static void CreateFloorBarrierPrefab(GameObject core)
        {
            string destination = Root + "/Prefabs/Floor/BH2VSQ_FloorBarrier.prefab";
            Transform source = core.transform.Find("Floor/FloorManager");
            if (source == null) throw new InvalidOperationException("Missing floor manager source for barrier prefab");

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = "BH2VSQ_FloorBarrier";
            clone.transform.SetParent(null, false);
            clone.SetActive(true);

            FloorManager floor = clone.GetComponent<FloorManager>();
            if (floor == null) throw new InvalidOperationException("Floor manager component missing on barrier prefab source");
            floor.teleport = null;
            floor.barrierController = true;
            floor.barrierFloorId = 1;

            foreach (UdonSharpBehaviour behaviour in clone.GetComponentsInChildren<UdonSharpBehaviour>(true))
                EditorUtility.SetDirty(behaviour);

            PrefabUtility.SaveAsPrefabAsset(clone, destination);
            UnityEngine.Object.DestroyImmediate(clone);
        }

        private static void SavePart(GameObject core, string path, string destination)
        {
            Transform source = core.transform.Find(path);
            if (source == null) throw new InvalidOperationException("Missing prefab part: " + path);
            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = source.name;
            clone.transform.SetParent(null, false);
            clone.SetActive(true);
            foreach (UdonSharpBehaviour behaviour in clone.GetComponentsInChildren<UdonSharpBehaviour>(true))
            {
                FieldInfo[] fields = behaviour.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)) continue;
                    UnityEngine.Object reference = field.GetValue(behaviour) as UnityEngine.Object;
                    Component component = reference as Component;
                    GameObject gameObject = reference as GameObject;
                    Transform transform = component != null ? component.transform : gameObject != null ? gameObject.transform : null;
                    if (transform != null && !transform.IsChildOf(clone.transform)) field.SetValue(behaviour, null);
                }
                EditorUtility.SetDirty(behaviour);
            }
            PrefabUtility.SaveAsPrefabAsset(clone, destination);
            UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}
