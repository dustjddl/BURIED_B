using System.IO;
using System.Linq;
using CoffinEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;

namespace CoffinEscape.Editor
{
    public static class BuildPrototypeScene
    {
        private const string ScenePath = "Assets/_Project/Scenes/CoffinEscapePrototype.unity";
        private const string ConfigPath = "Assets/_Project/Settings/GameConfig.asset";
        private const string XrRigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/XR Interaction Simulator/XR Interaction Simulator.prefab";

        [MenuItem("CoffinEscape/Build Prototype Scene")]
        public static void Build()
        {
            try
            {
                BuildInternal(false);
                File.WriteAllText("Library/CoffinEscapeM2Build.log", "SUCCESS");
            }
            catch (System.Exception exception)
            {
                File.WriteAllText("Library/CoffinEscapeM2Build.log", exception.ToString());
                Debug.LogException(exception);
                throw;
            }
        }

        public static void RebuildGeneratedScene()
        {
            try
            {
                BuildInternal(true);
                File.WriteAllText("Library/CoffinEscapeM2Build.log", "SUCCESS");
            }
            catch (System.Exception exception)
            {
                File.WriteAllText("Library/CoffinEscapeM2Build.log", exception.ToString());
                Debug.LogException(exception);
                throw;
            }
        }

        private static void BuildInternal(bool allowOverwrite)
        {
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/Settings");
            EnsureFolder("Assets/_Project/Input");

            if (File.Exists(ScenePath) && !allowOverwrite && !EditorUtility.DisplayDialog(
                    "Rebuild Coffin Escape Prototype",
                    "The generated prototype scene already exists. Rebuild it from the current GameConfig?",
                    "Rebuild", "Cancel"))
                return;

            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPrefabPath);
            GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (rigPrefab == null || simulatorPrefab == null)
                throw new FileNotFoundException("The installed XRI 3.3.2 Starter Assets or simulator sample could not be found.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameConfig config = LoadOrCreateConfig();
            GameObject root = new GameObject("Coffin Escape Prototype");
            GameObject environment = Child(root.transform, "Coffin");
            Material gray = CreateGrayboxMaterial(config);
            BoxCollider movementBounds = BuildCoffin(environment.transform, config, gray);

            var managerObject = new GameObject("Game Systems");
            managerObject.transform.SetParent(root.transform, false);
            var interactionManager = managerObject.AddComponent<XRInteractionManager>();
            var resources = managerObject.AddComponent<ResourceManager>();
            resources.Initialize(config);

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.name = "XR Origin (Coffin Escape)";
            rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            XROrigin origin = rig.GetComponent<XROrigin>();
            if (origin == null)
                throw new MissingComponentException("The XRI Starter Assets rig has no XROrigin component.");
            origin.CameraYOffset = config.cameraAnchorHeightMeters;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            if (origin.Camera != null)
            {
                origin.Camera.clearFlags = CameraClearFlags.SolidColor;
                origin.Camera.backgroundColor = Color.black;
            }

            ConfigureHands(rig, interactionManager, config);
            var calibrator = rig.AddComponent<PostureCalibrator>();
            SetObjectReference(calibrator, "origin", origin);
            SetObjectReference(calibrator, "headAnchor", environment.transform.Find("Calibration Anchor"));

            GameObject simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab, scene);
            simulator.name = "XR Device Simulator (Keyboard and Mouse)";

            BuildGrabDemo(environment.transform, resources, config, gray, movementBounds);
            ConfigureLighting(config);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rig;
            Debug.Log($"Coffin Escape prototype scene created at {ScenePath}");
        }

        private static BoxCollider BuildCoffin(Transform parent, GameConfig config, Material material)
        {
            float length = config.coffinLengthMeters;
            float width = config.coffinWidthMeters;
            float height = config.coffinHeightMeters;
            float wall = config.coffinWallThicknessMeters;
            float outsideWidth = width + wall * 2f;

            Cube("Coffin Floor", parent, new Vector3(0f, -wall * 0.5f, length * 0.5f), new Vector3(outsideWidth, wall, length), material);
            Cube("Left Wall", parent, new Vector3(-width * 0.5f - wall * 0.5f, height * 0.5f, length * 0.5f), new Vector3(wall, height, length), material);
            Cube("Right Wall", parent, new Vector3(width * 0.5f + wall * 0.5f, height * 0.5f, length * 0.5f), new Vector3(wall, height, length), material);
            Cube("Head Wall", parent, new Vector3(0f, height * 0.5f, -wall * 0.5f), new Vector3(outsideWidth, height, wall), material);
            Cube("Foot Wall", parent, new Vector3(0f, height * 0.5f, length + wall * 0.5f), new Vector3(outsideWidth, height, wall), material);

            GameObject hinge = Child(parent, "Lid Hinge");
            hinge.transform.position = new Vector3(-outsideWidth * 0.5f, height + wall * 0.5f, length * 0.5f);
            Cube("Lid", hinge.transform, new Vector3(outsideWidth * 0.5f, 0f, 0f),
                new Vector3(outsideWidth, wall, length), material);

            GameObject anchor = Child(parent, "Calibration Anchor");
            anchor.transform.position = new Vector3(0f, config.cameraAnchorHeightMeters, config.headAnchorForwardMeters);
            anchor.transform.rotation = Quaternion.identity;

            GameObject boundsObject = Child(parent, "Grabbable Bounds");
            BoxCollider bounds = boundsObject.AddComponent<BoxCollider>();
            bounds.isTrigger = true;
            bounds.center = new Vector3(0f, height * 0.5f, length * 0.5f);
            bounds.size = new Vector3(width, height, length);
            return bounds;
        }

        private static void ConfigureHands(GameObject rig, XRInteractionManager manager, GameConfig config)
        {
            foreach (LocomotionProvider locomotion in rig.GetComponentsInChildren<LocomotionProvider>(true))
                locomotion.enabled = false;
            foreach (XRInteractionGroup group in rig.GetComponentsInChildren<XRInteractionGroup>(true))
                group.enabled = false;

            ConfigureHand(rig, "Left Controller", XRNode.LeftHand, Key.Digit1, manager, config);
            ConfigureHand(rig, "Right Controller", XRNode.RightHand, Key.Digit2, manager, config);
        }

        private static void ConfigureHand(GameObject rig, string handName, XRNode hand, Key debugKey,
            XRInteractionManager manager, GameConfig config)
        {
            Transform handTransform = rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == handName);
            if (handTransform == null)
                throw new MissingComponentException($"The XRI rig is missing its {handName}.");

            foreach (XRBaseInteractor oldInteractor in handTransform.GetComponentsInChildren<XRBaseInteractor>(true))
                oldInteractor.enabled = false;

            SphereCollider collider = handTransform.GetComponent<SphereCollider>();
            if (collider == null)
                collider = handTransform.gameObject.AddComponent<SphereCollider>();
            collider.radius = config.handInteractorRadiusMeters;
            collider.isTrigger = true;

            XRDirectInteractor direct = handTransform.GetComponent<XRDirectInteractor>();
            if (direct == null)
                direct = handTransform.gameObject.AddComponent<XRDirectInteractor>();
            direct.interactionManager = manager;

            XRInputButtonReader selectReader = direct.selectInput;
            selectReader.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            direct.selectInput = selectReader;
            direct.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.StateChange;

            HandSelectInput handInput = handTransform.GetComponent<HandSelectInput>();
            if (handInput == null)
                handInput = handTransform.gameObject.AddComponent<HandSelectInput>();
            handInput.Configure(direct, hand, config, debugKey);
        }

        private static void BuildGrabDemo(Transform parent, ResourceManager resources, GameConfig config,
            Material material, BoxCollider movementBounds)
        {
            GameObject lighter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lighter.name = "Lighter (M2 Interaction Demo)";
            lighter.transform.SetParent(parent, false);
            lighter.transform.localPosition = config.grabTestObjectPositionMeters;
            lighter.transform.localScale = config.lighterScaleMeters;
            lighter.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody lighterBody = lighter.AddComponent<Rigidbody>();
            lighterBody.isKinematic = true;
            lighterBody.useGravity = false;
            XRGrabInteractable lighterGrab = lighter.AddComponent<XRGrabInteractable>();
            lighterGrab.movementType = XRBaseInteractable.MovementType.Kinematic;
            GrabbableItem lighterHome = lighter.AddComponent<GrabbableItem>();

            GameObject flameObject = new GameObject("Flame");
            flameObject.transform.SetParent(lighter.transform, false);
            flameObject.transform.localPosition = config.lighterFlameLocalPositionMeters;
            Light flame = flameObject.AddComponent<Light>();
            flame.type = LightType.Point;
            flame.intensity = 0f;
            flame.range = config.lighterLightRangeMeters;

            LighterController lighterController = lighter.AddComponent<LighterController>();
            lighterController.Configure(flame, resources, config);

            Transform homeAnchor = Child(parent, "Lighter Return Anchor").transform;
            homeAnchor.localPosition = config.grabTestObjectPositionMeters;
            homeAnchor.localRotation = Quaternion.identity;
            lighterHome.SetReturnAnchor(homeAnchor);
            lighterHome.SetMovementBounds(movementBounds);
        }

        private static GameConfig LoadOrCreateConfig()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static Material CreateGrayboxMaterial(GameConfig config)
        {
            const string materialPath = "Assets/_Project/Settings/Graybox.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                material.color = config.grayboxColor;
                EditorUtility.SetDirty(material);
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            material = new Material(shader) { name = "Graybox" };
            material.color = config.grayboxColor;
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject Child(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ConfigureLighting(GameConfig config)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientIntensity = config.ambientIntensity;
            RenderSettings.reflectionIntensity = 0f;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            EditorBuildSettingsScene prototype = scenes.FirstOrDefault(existing => existing.path == scenePath);
            if (prototype != null && scenes.Length > 0 && scenes[0].path == scenePath)
                return;

            prototype ??= new EditorBuildSettingsScene(scenePath, true);
            prototype.enabled = true;
            EditorBuildSettings.scenes = new[] { prototype }
                .Concat(scenes.Where(existing => existing.path != scenePath))
                .ToArray();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
