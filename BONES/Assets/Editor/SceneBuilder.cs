using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Bones;
using Bones.Data;
using Bones.Juice;
using Bones.Presentation;
using Bones.UI;

namespace Bones.Editor
{
    /// <summary>
    /// Builds a playable scene wired end-to-end so you don't hand-place objects. Run AFTER
    /// "BONES ▸ Generate MVP Data". Menu: BONES ▸ Build Playable Scene.
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Bones.unity";
        private const string PanelPath = "Assets/Settings/BonesPanelSettings.asset";
        private const string ThemePath = "Assets/Settings/BonesTheme.tss";

        [MenuItem("BONES/Build Playable Scene")]
        public static void Build()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/GameDatabase.asset")
                  ?? AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            if (db == null)
            {
                EditorUtility.DisplayDialog("BONES", "Run 'BONES ▸ Generate MVP Data' first.", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera looking down at the pavement where dice land.
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.065f, 0.075f);
            camGo.transform.position = new Vector3(0f, 3.9f, -4.4f);
            camGo.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            cam.fieldOfView = 56f;
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Key Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            // Pavement plate.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Pavement";
            floor.transform.localScale = Vector3.one * 2f;
            var floorRend = floor.GetComponent<Renderer>();
            floorRend.sharedMaterial = Bones.Presentation.MaterialUtil.Opaque(new Color(0.16f, 0.155f, 0.16f));

            // Two dice spaces: yours (front, larger) and the mark's (back, smaller — depth).
            var bankerAnchor = new GameObject("BankerAnchor");
            bankerAnchor.transform.position = new Vector3(0f, 0.35f, -0.6f);
            bankerAnchor.transform.localScale = Vector3.one * 1.0f;

            var markAnchor = new GameObject("MarkAnchor");
            markAnchor.transform.position = new Vector3(0f, 0.35f, 1.5f);
            markAnchor.transform.localScale = Vector3.one * 0.62f;

            // Controllers.
            var gameGo = new GameObject("GameController");
            var game = gameGo.AddComponent<GameController>();
            SetPrivate(game, "database", db);

            var choreoGo = new GameObject("Choreographer");
            var choreo = choreoGo.AddComponent<DiceRollChoreographer>();
            SetPrivate(choreo, "bankerAnchor", bankerAnchor.transform);
            SetPrivate(choreo, "markAnchor", markAnchor.transform);
            SetPrivate(choreo, "database", db);

            var juiceGo = new GameObject("JuiceDirector");
            var juice = juiceGo.AddComponent<JuiceDirector>();
            SetPrivate(juice, "config", db.juice);
            SetPrivate(juice, "shakeCamera", cam);

            var audioGo = new GameObject("AudioDirector");
            var audio = audioGo.AddComponent<AudioDirector>();

            // UI.
            var panel = GetOrCreatePanelSettings();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/UI/SpotUI.uxml");
            var uiGo = new GameObject("UI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = uxml;
            var ui = uiGo.AddComponent<GameUI>();
            SetPrivate(ui, "game", game);
            SetPrivate(ui, "choreographer", choreo);
            SetPrivate(ui, "juice", juice);
            SetPrivate(ui, "audioDirector", audio);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[BONES] Playable scene built at " + ScenePath + ". Press Play.");
            EditorUtility.DisplayDialog("BONES",
                "Scene built. Press Play.\n\nIf the UI is blank, open " + PanelPath +
                " and assign a Theme Style Sheet (Assets ▸ Create ▸ UI Toolkit ▸ TSS Theme File).", "OK");
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (existing != null) return existing;

            Directory.CreateDirectory("Assets/Settings");
            var panel = ScriptableObject.CreateInstance<PanelSettings>();

            // Try to find any theme in the project; else write a default-importing one.
            var theme = FindOrCreateTheme();
            if (theme != null) panel.themeStyleSheet = theme;

            AssetDatabase.CreateAsset(panel, PanelPath);
            AssetDatabase.SaveAssets();
            return panel;
        }

        private static ThemeStyleSheet FindOrCreateTheme()
        {
            var guids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));

            // Author a minimal runtime theme that imports Unity's defaults.
            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(ThemePath);
            }
            return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Assign a [SerializeField] private field via SerializedObject so it persists in the scene.</summary>
        private static void SetPrivate(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[BONES] No serialized field '{field}' on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);

            // Verify the reference actually took.
            var check = new SerializedObject(target).FindProperty(field);
            if (check == null || check.objectReferenceValue == null)
                Debug.LogError($"[BONES] Failed to wire '{field}' on {target.GetType().Name} to '{(value != null ? value.name : "null")}'.");
            else
                Debug.Log($"[BONES] Wired {target.GetType().Name}.{field} → {check.objectReferenceValue.name}");
        }
    }
}
