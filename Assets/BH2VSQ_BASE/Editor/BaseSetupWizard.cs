using System;
using System.IO;
using BH2VSQ.Base;
using TMPro;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRC.Udon;

namespace BH2VSQ.Base.Editor
{
    public partial class BaseSetupWizard : EditorWindow
    {
        private const string Root = "Assets/BH2VSQ_BASE";
        private const string CorePath = Root + "/Prefabs/Core/BH2VSQ_BASE_Core.prefab";
        private const string ConfigPath = Root + "/Data/Default/DefaultBaseConfig.asset";
        private Vector2 scroll;

        [MenuItem("BH2VSQ BASE/配置向导")]
        public static void Open() { GetWindow<BaseSetupWizard>("BH2VSQ BASE 配置"); }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("BH2VSQ BASE Core", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("脚本编译后再构建资源包。不要将真实 TOTP 密钥提交到源码仓库；Udon 中的 TOTP 仅用于世界内分级。", MessageType.Warning);
            if (GUILayout.Button("1. 创建默认数据")) CreateData();
            if (GUILayout.Button("2. 构建预制体和示例场景")) BuildAll();
            if (GUILayout.Button("3. 验证配置")) BaseValidator.ValidateSelection(true);
            if (GUILayout.Button("4. 导出 Unity 资源包")) Export();
            EditorGUILayout.EndScrollView();
        }

        [MenuItem("BH2VSQ BASE/生成预制体与场景")]
        public static void BuildAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("场景保存已取消。");
            CreateFolders();
            CreateProgramAssets();
            CreateData();
            CreateFontAsset();
            GameObject root = BuildCore();
            PrefabUtility.SaveAsPrefabAsset(root, CorePath);
            CreateComponentPrefabs(root);
            UnityEngine.Object.DestroyImmediate(root);
            CreateDemoScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BH2VSQ BASE：已生成核心、界面组件、位置点、数据和示例场景。");
        }

        private static void CreateFolders()
        {
            string[] paths = { "Data", "Data/Default", "Data/Localization", "Fonts", "Prefabs", "Prefabs/Core", "Prefabs/UI", "Prefabs/Floor", "Prefabs/Teleport", "Prefabs/Demo", "Scenes" };
            foreach (string path in paths)
            {
                string current = Root;
                string[] segments = path.Split('/');
                foreach (string segment in segments)
                {
                    string next = current + "/" + segment;
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segment);
                    current = next;
                }
            }
        }

        private static void CreateProgramAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { Root + "/Scripts" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Type type = script != null ? script.GetClass() : null;
                if (type == null || !typeof(UdonSharpBehaviour).IsAssignableFrom(type)) continue;
                string assetPath = Path.ChangeExtension(path, ".asset");
                if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(assetPath) != null) continue;
                UdonSharpProgramAsset asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                asset.sourceCsScript = script;
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UdonSharpProgramAsset.CompileAllCsPrograms(true);
            if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError())
                throw new InvalidOperationException("UdonSharp 编译失败，请检查 Unity 控制台。");
        }

        [MenuItem("BH2VSQ BASE/创建默认数据")]
        public static void CreateData()
        {
            CreateFolders();
            string locationsPath = Root + "/Data/Default/DefaultLocationDatabase.asset";
            LocationDatabase locations = AssetDatabase.LoadAssetAtPath<LocationDatabase>(locationsPath);
            if (locations == null) { locations = ScriptableObject.CreateInstance<LocationDatabase>(); AssetDatabase.CreateAsset(locations, locationsPath); }
            BaseConfig config = AssetDatabase.LoadAssetAtPath<BaseConfig>(ConfigPath);
            if (config == null) { config = ScriptableObject.CreateInstance<BaseConfig>(); AssetDatabase.CreateAsset(config, ConfigPath); }
            config.locations = locations;
            string localizationPath = Root + "/Data/Localization/DefaultLocalization.asset";
            LocalizationDatabase translation = AssetDatabase.LoadAssetAtPath<LocalizationDatabase>(localizationPath);
            if (translation == null) { translation = ScriptableObject.CreateInstance<LocalizationDatabase>(); AssetDatabase.CreateAsset(translation, localizationPath); }
            if (translation.chinese == null || translation.chinese.Length != BaseText.EntryCount)
            {
                translation.chinese = LocalizationDatabase.DefaultChinese();
                EditorUtility.SetDirty(translation);
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static void CreateFontAsset()
        {
            EnsureTmpResources();
            const string path = Root + "/Fonts/BH2VSQ_UI.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) return;
            Font source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NotoSansSC-Regular.ttf");
            if (source == null) throw new InvalidOperationException("缺少 NotoSansSC-Regular.ttf 字体文件。");
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(font, path);
            foreach (Texture2D atlas in font.atlasTextures)
                if (atlas != null && !EditorUtility.IsPersistent(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            if (font.material != null && !EditorUtility.IsPersistent(font.material))
            {
                font.material.mainTexture = font.atlasTexture;
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureTmpResources()
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") != null) return;
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro");
            if (package == null) throw new InvalidOperationException("缺少 TextMesh Pro 包。");
            string essentials = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(essentials)) throw new FileNotFoundException("缺少 TextMesh Pro 必需资源包。", essentials);
            AssetDatabase.ImportPackage(essentials, false);
            AssetDatabase.Refresh();
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
                throw new InvalidOperationException("尚未导入 TextMesh Pro 必需资源。请在编辑器中使用 Window → TextMeshPro → Import TMP Essential Resources，然后重新构建。");
        }

        private static T Manager<T>(Transform parent, string name) where T : UdonSharpBehaviour
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddUdonSharpComponent<T>();
        }

        private static GameObject Group(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject BuildCore()
        {
            BaseConfig config = AssetDatabase.LoadAssetAtPath<BaseConfig>(ConfigPath);
            if (config == null || config.locations == null)
                throw new InvalidOperationException("缺少默认 BaseConfig，请先创建默认数据。");
            LocationDatabase locationData = config.locations;
            int locationCount = locationData.ids == null ? 0 : locationData.ids.Length;
            if (locationCount == 0 || locationData.names == null || locationData.chineseNames == null || locationData.floorIds == null || locationData.floorNames == null || locationData.chineseFloorNames == null || locationData.requiredRanks == null || locationData.tabVisible == null || locationData.xpMultipliers == null ||
                locationData.names.Length != locationCount || locationData.chineseNames.Length != locationCount || locationData.floorIds.Length != locationCount || locationData.floorNames.Length != locationCount || locationData.chineseFloorNames.Length != locationCount || locationData.requiredRanks.Length != locationCount || locationData.tabVisible.Length != locationCount || locationData.xpMultipliers.Length != locationCount)
                throw new InvalidOperationException("地点数据库数组长度必须相同且不为零。");
            GameObject root = new GameObject("BH2VSQ_BASE_Core");
            BaseWorldSystem world = root.AddUdonSharpComponent<BaseWorldSystem>();
            GameObject coreGroup = Group(root.transform, "Core");
            GameObject authGroup = Group(root.transform, "Authentication");
            GameObject floorGroup = Group(root.transform, "Floor");
            GameObject teleportGroup = Group(root.transform, "Teleport");
            GameObject adminGroup = Group(root.transform, "Admin");
            GameObject uiGroup = Group(root.transform, "UI");

            PlayerRegistry registry = Manager<PlayerRegistry>(coreGroup.transform, "PlayerRegistry");
            PlayerDataManager data = Manager<PlayerDataManager>(coreGroup.transform, "PlayerDataManager");
            PlayerAreaTracker tracker = Manager<PlayerAreaTracker>(coreGroup.transform, "PlayerAreaTracker");
            PlayerLevelSystem level = Manager<PlayerLevelSystem>(coreGroup.transform, "PlayerLevelSystem");
            LocalizationManager localization = Manager<LocalizationManager>(coreGroup.transform, "LocalizationManager");
            AuthenticationSession session = Manager<AuthenticationSession>(authGroup.transform, "AuthenticationSession");
            PermissionManager permission = Manager<PermissionManager>(authGroup.transform, "PermissionManager");
            TOTPAuthManager auth = Manager<TOTPAuthManager>(authGroup.transform, "TOTPAuthManager");
            FloorManager floors = Manager<FloorManager>(floorGroup.transform, "FloorManager");
            AreaManager areas = Manager<AreaManager>(floorGroup.transform, "AreaManager");
            AccessManager access = Manager<AccessManager>(floorGroup.transform, "AccessManager");
            AreaPopulationManager population = Manager<AreaPopulationManager>(floorGroup.transform, "AreaPopulationManager");
            RadioDutyManager radio = Manager<RadioDutyManager>(floorGroup.transform, "RadioDutyManager");
            TeleportManager teleport = Manager<TeleportManager>(teleportGroup.transform, "TeleportManager");
            TeleportRequestManager requests = Manager<TeleportRequestManager>(teleportGroup.transform, "TeleportRequestManager");
            AdminManager admin = Manager<AdminManager>(adminGroup.transform, "AdminManager");
            FloorAdminManager floorAdmin = Manager<FloorAdminManager>(adminGroup.transform, "FloorAdminManager");

            teleport.pointRoot = teleportGroup.transform;
            floors.teleport = teleport; areas.teleport = teleport;
            tracker.areas = areas; tracker.teleport = teleport; tracker.access = access; tracker.world = world;
            level.data = data; level.tracker = tracker; level.areas = areas;
            LocalizationDatabase translations = AssetDatabase.LoadAssetAtPath<LocalizationDatabase>(Root + "/Data/Localization/DefaultLocalization.asset");
            localization.chinese = (string[])translations.chinese.Clone();
            session.registry = registry; permission.session = session; permission.registry = registry; auth.session = session;
            access.floors = floors; access.teleport = teleport; access.permission = permission;
            population.tracker = tracker; radio.population = population; radio.teleport = teleport;
            teleport.access = access;
            teleport.tracker = tracker;
            requests.teleport = teleport;
            requests.timeoutSeconds = config.requestTimeoutSeconds;
            admin.permission = permission; floorAdmin.admin = admin; floorAdmin.floors = floors;
            world.registry = registry; world.playerData = data; world.tracker = tracker; world.session = session;
            world.floors = floors; world.access = access; world.teleport = teleport;

            TeleportPoint[] points = new TeleportPoint[locationCount];
            for (int i = 0; i < points.Length; i++)
            {
                TeleportPoint point = Manager<TeleportPoint>(teleportGroup.transform, "Point_" + locationData.names[i].Replace(' ', '_'));
                point.locationId = locationData.ids[i]; point.locationName = locationData.names[i]; point.chineseName = locationData.chineseNames[i];
                point.floorId = locationData.floorIds[i]; point.floorName = locationData.floorNames[i]; point.chineseFloorName = locationData.chineseFloorNames[i];
                point.requiredRank = locationData.requiredRanks[i]; point.tabVisible = locationData.tabVisible[i]; point.xpMultiplier = locationData.xpMultipliers[i];
                point.isSafeFallback = point.locationId == locationData.safeFallbackId; point.radioDutyArea = point.locationId == locationData.radioDutyId;
                point.transform.localPosition = new Vector3(i * 3f, 0f, 0f);
                point.destination = point.transform;
                points[i] = point;
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "Visual";
                visual.transform.SetParent(point.transform, false);
                visual.transform.localPosition = new Vector3(0f, .1f, 0f);
                visual.transform.localScale = new Vector3(.35f, .1f, .35f);
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                GameObject gizmo = Group(point.transform, "Gizmo");
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "PointMarker";
                marker.transform.SetParent(gizmo.transform, false);
                marker.transform.localPosition = new Vector3(0f, .35f, 0f);
                marker.transform.localScale = Vector3.one * .16f;
                UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                GameObject trigger = Group(point.transform, "AreaTrigger");
                BoxCollider collider = trigger.AddComponent<BoxCollider>();
                collider.isTrigger = true; collider.size = new Vector3(2f, 2.5f, 2f);
                point.areaVolume = collider;
                AreaTrigger areaTrigger = trigger.AddUdonSharpComponent<AreaTrigger>();
                areaTrigger.tracker = tracker; areaTrigger.access = access; areaTrigger.teleport = teleport; areaTrigger.point = point; areaTrigger.world = world;
            }
            teleport.points = points;

            BuildUI(uiGroup.transform, world, registry, data, tracker, floors, areas, permission, auth, teleport, requests, radio, population, admin, floorAdmin, localization);
            return root;
        }

        private static void CreateDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject core = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CorePath));
            core.transform.position = Vector3.zero;
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Demo Ground";
            ground.transform.localScale = new Vector3(15f, 1f, 5f);
            GameObject vrcWorld = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCWorld.prefab");
            if (vrcWorld != null) PrefabUtility.InstantiatePrefab(vrcWorld);
            EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null) { GameObject e = new GameObject("EventSystem"); e.AddComponent<EventSystem>(); e.AddComponent<StandaloneInputModule>(); }
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/BH2VSQ_BASE_Demo.unity");
        }

        [MenuItem("BH2VSQ BASE/导出 Unity 资源包")]
        public static void Export()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Releases");
            Directory.CreateDirectory(directory);
            AssetDatabase.ExportPackage(new[] { Root, "Assets/TextMesh Pro", "Assets/SerializedUdonPrograms" }, Path.Combine(directory, "BH2VSQ_BASE_Core.unitypackage"), ExportPackageOptions.Recurse);
            Debug.Log("BH2VSQ BASE 资源包已导出到 " + directory);
        }

        // UI and component prefab builders are defined in the other partial file.
    }
}
