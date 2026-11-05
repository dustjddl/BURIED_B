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
        private const string VisualCatalogPath = "Assets/_Project/Settings/VisualCatalog.asset";
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
            VisualCatalog visualCatalog = LoadOrCreateVisualCatalog();
            EnsureTag("ControllerTip");
            EnsureTag("CoffinWall");
            GameObject root = new GameObject("Coffin Escape Prototype");
            GameObject environment = Child(root.transform, "Coffin");
            Material gray = CreateGrayboxMaterial(config);
            BoxCollider movementBounds = BuildCoffin(environment.transform, config, gray, visualCatalog);

            var managerObject = new GameObject("Game Systems");
            managerObject.transform.SetParent(root.transform, false);
            var interactionManager = managerObject.AddComponent<XRInteractionManager>();
            var resources = managerObject.AddComponent<ResourceManager>();
            resources.Initialize(config);

            GrabbableItem lighter = BuildGrabDemo(environment.transform, resources, config, gray, movementBounds, visualCatalog);

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

            ConfigureHands(rig, interactionManager, config, lighter, visualCatalog, gray);
            var calibrator = rig.AddComponent<PostureCalibrator>();
            calibrator.Configure(origin, environment.transform.Find("Calibration Anchor"), config);
            var boundaryConstraint = rig.AddComponent<CoffinBoundaryConstraint>();
            Transform leftController = rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Left Controller");
            Transform rightController = rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Right Controller");
            boundaryConstraint.Configure(origin, movementBounds, new[] { leftController, rightController }, config);

            GameObject simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab, scene);
            simulator.name = "XR Device Simulator (Keyboard and Mouse)";
            simulator.AddComponent<XrSimulatorHardwareGate>().Configure(config);

            ConfigureLighting(config);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rig;
            Debug.Log($"Coffin Escape prototype scene created at {ScenePath}");
        }

        private static BoxCollider BuildCoffin(Transform parent, GameConfig config, Material material, VisualCatalog visualCatalog)
        {
            float length = config.coffinLengthMeters;
            float width = config.coffinWidthMeters;
            float height = config.coffinHeightMeters;
            float wall = config.coffinWallThicknessMeters;
            float outsideWidth = width + wall * 2f;

            BoxPanel("Coffin Floor", parent, new Vector3(0f, -wall * 0.5f, length * 0.5f), new Vector3(outsideWidth, wall, length), material, visualCatalog.coffinPanelPrefab, config.coffinFloorColor);
            BoxPanel("Left Wall", parent, new Vector3(-width * 0.5f - wall * 0.5f, height * 0.5f, length * 0.5f), new Vector3(wall, height, length), material, visualCatalog.coffinPanelPrefab, config.coffinWallColor);
            BoxPanel("Right Wall", parent, new Vector3(width * 0.5f + wall * 0.5f, height * 0.5f, length * 0.5f), new Vector3(wall, height, length), material, visualCatalog.coffinPanelPrefab, config.coffinWallColor);
            BoxPanel("Head Wall", parent, new Vector3(0f, height * 0.5f, -wall * 0.5f), new Vector3(outsideWidth, height, wall), material, visualCatalog.coffinPanelPrefab, config.coffinWallColor);
            BoxPanel("Foot Wall", parent, new Vector3(0f, height * 0.5f, length + wall * 0.5f), new Vector3(outsideWidth, height, wall), material, visualCatalog.coffinPanelPrefab, config.coffinWallColor);

            GameObject hinge = Child(parent, "Lid Hinge");
            hinge.transform.position = new Vector3(-outsideWidth * 0.5f, height + wall * 0.5f, length * 0.5f);
            BoxPanel("Lid", hinge.transform, new Vector3(outsideWidth * 0.5f, 0f, 0f),
                new Vector3(outsideWidth, wall, length), material, visualCatalog.coffinPanelPrefab, config.coffinLidColor);

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

        private static void ConfigureHands(GameObject rig, XRInteractionManager manager, GameConfig config,
            GrabbableItem initialLighter, VisualCatalog visualCatalog, Material handMaterial)
        {
            foreach (LocomotionProvider locomotion in rig.GetComponentsInChildren<LocomotionProvider>(true))
                locomotion.enabled = false;
            foreach (XRInteractionGroup group in rig.GetComponentsInChildren<XRInteractionGroup>(true))
                group.enabled = false;

            ConfigureHand(rig, "Left Controller", XRNode.LeftHand, manager, config, visualCatalog, handMaterial,
                config.lighterStartHand == XRNode.LeftHand ? initialLighter : null);
            ConfigureHand(rig, "Right Controller", XRNode.RightHand, manager, config, visualCatalog, handMaterial,
                config.lighterStartHand == XRNode.RightHand ? initialLighter : null);
        }

        private static void ConfigureHand(GameObject rig, string handName, XRNode hand, XRInteractionManager manager,
            GameConfig config, VisualCatalog visualCatalog, Material handMaterial, GrabbableItem initialItem)
        {
            Transform handTransform = rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == handName);
            if (handTransform == null)
                throw new MissingComponentException($"The XRI rig is missing its {handName}.");

            NearFarInteractor existingNearFar = handTransform.GetComponentInChildren<NearFarInteractor>(true);
            InputActionReference gripAction = existingNearFar != null ? ReadActionReference(existingNearFar.selectInput) : null;
            InputActionReference triggerAction = existingNearFar != null ? ReadActionReference(existingNearFar.activateInput) : null;

            XRDirectInteractor direct = handTransform.GetComponent<XRDirectInteractor>();
            if (direct == null)
                direct = handTransform.gameObject.AddComponent<XRDirectInteractor>();

            foreach (XRBaseInteractor oldInteractor in handTransform.GetComponentsInChildren<XRBaseInteractor>(true))
            {
                if (oldInteractor != direct)
                    oldInteractor.enabled = false;
            }
            direct.enabled = true;

            SphereCollider collider = handTransform.GetComponent<SphereCollider>();
            if (collider == null)
                collider = handTransform.gameObject.AddComponent<SphereCollider>();
            collider.radius = config.handInteractorRadiusMeters;
            collider.isTrigger = true;

            Rigidbody handBody = handTransform.GetComponent<Rigidbody>();
            if (handBody == null)
                handBody = handTransform.gameObject.AddComponent<Rigidbody>();
            handBody.isKinematic = true;
            handBody.useGravity = false;

            direct.interactionManager = manager;
            XRInputButtonReader disabledSelect = direct.selectInput;
            disabledSelect.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            direct.selectInput = disabledSelect;
            XRInputButtonReader disabledActivate = direct.activateInput;
            disabledActivate.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            direct.activateInput = disabledActivate;

            direct.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.StateChange;

            Transform tip = handTransform.Find("ControllerTip");
            if (tip == null)
                tip = Child(handTransform, "ControllerTip").transform;
            tip.localPosition = config.controllerTipLocalPositionMeters;
            tip.localRotation = Quaternion.identity;
            tip.localScale = Vector3.one;
            tip.gameObject.tag = "ControllerTip";
            SphereCollider tipCollider = tip.GetComponent<SphereCollider>();
            if (tipCollider == null)
                tipCollider = tip.gameObject.AddComponent<SphereCollider>();
            tipCollider.radius = config.controllerTipRadiusMeters;
            tipCollider.isTrigger = true;

            ControllerWallHaptics wallHaptics = handTransform.GetComponent<ControllerWallHaptics>();
            if (wallHaptics == null)
                wallHaptics = handTransform.gameObject.AddComponent<ControllerWallHaptics>();
            wallHaptics.Configure(direct, config);

            HandGrabToggle grabToggle = handTransform.GetComponent<HandGrabToggle>();
            if (grabToggle == null)
                grabToggle = handTransform.gameObject.AddComponent<HandGrabToggle>();
            grabToggle.Configure(direct, hand, config, gripAction, triggerAction, initialItem);

            Transform controllerVisual = handTransform.Find(handName + " Visual");
            ControllerHandVisual handVisual = handTransform.GetComponent<ControllerHandVisual>();
            if (handVisual == null)
                handVisual = handTransform.gameObject.AddComponent<ControllerHandVisual>();
            handVisual.Configure(grabToggle, hand, config, handMaterial, visualCatalog.controllerHandPrefab, controllerVisual);
        }

        private static GrabbableItem BuildGrabDemo(Transform parent, ResourceManager resources, GameConfig config,
            Material material, BoxCollider movementBounds, VisualCatalog visualCatalog)
        {
            CreateGrabObject("Cube (M2 Grab Test)", parent, config.cubeTestObjectPositionMeters,
                Vector3.one * config.grabTestObjectSizeMeters, PrimitiveType.Cube, visualCatalog.cubePickupPrefab,
                material, config.testCubeColor, movementBounds, config.cubeTestObjectPositionMeters, "Cube Return Anchor");
            CreateGrabObject("Sphere (M2 Grab Test)", parent, config.sphereTestObjectPositionMeters,
                Vector3.one * config.grabTestObjectSizeMeters, PrimitiveType.Sphere, visualCatalog.spherePickupPrefab,
                material, config.testSphereColor, movementBounds, config.sphereTestObjectPositionMeters, "Sphere Return Anchor");

            GrabbableItem lighterHome = CreateGrabObject("Lighter (M2 Interaction Demo)", parent,
                config.grabTestObjectPositionMeters, config.lighterScaleMeters, PrimitiveType.Cube,
                visualCatalog.lighterPrefab, material, config.lighterBodyColor, movementBounds, config.lighterReturnAnchorLocalPositionMeters,
                "Lighter Return Anchor");
            GameObject lighter = lighterHome.gameObject;

            GameObject flameObject = new GameObject("Flame");
            flameObject.transform.SetParent(lighter.transform, false);
            flameObject.transform.localPosition = config.lighterFlameLocalPositionMeters;
            Light flame = flameObject.AddComponent<Light>();
            flame.type = LightType.Point;
            flame.intensity = 0f;
            flame.range = config.lighterLightRangeMeters;

            LighterController lighterController = lighter.AddComponent<LighterController>();
            lighterController.Configure(flame, resources, config);

            lighterHome.ReturnComponent.Configure(parent.Find("Lighter Return Anchor"), movementBounds);
            return lighterHome;
        }

        private static GrabbableItem CreateGrabObject(string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, PrimitiveType fallbackPrimitive, GameObject visualPrefab, Material fallbackMaterial,
            Color visualColor, BoxCollider movementBounds, Vector3 returnAnchorLocalPosition, string returnAnchorName)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.transform.SetParent(parent, false);
            itemObject.transform.localPosition = localPosition;
            itemObject.transform.localScale = localScale;

            if (fallbackPrimitive == PrimitiveType.Sphere)
                itemObject.AddComponent<SphereCollider>();
            else
                itemObject.AddComponent<BoxCollider>();

            Rigidbody body = itemObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            XRGrabInteractable interactable = itemObject.AddComponent<XRGrabInteractable>();
            interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
            interactable.selectMode = InteractableSelectMode.Single;

            ApplyVisualColor(CreateVisual(itemObject.transform, visualPrefab, fallbackPrimitive, fallbackMaterial), visualColor);

            Transform anchor = Child(parent, returnAnchorName).transform;
            anchor.localPosition = returnAnchorLocalPosition;
            anchor.localRotation = Quaternion.identity;

            ReturnToAnchor returnComponent = itemObject.AddComponent<ReturnToAnchor>();
            returnComponent.Configure(anchor, movementBounds);
            return itemObject.AddComponent<GrabbableItem>();
        }

        private static void ApplyVisualColor(GameObject visualRoot, Color color)
        {
            visualRoot.AddComponent<VisualTint>().Configure(color);
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

        private static GameObject BoxPanel(string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material fallbackMaterial, GameObject visualPrefab, Color visualColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localScale = localScale;
            BoxCollider collider = panel.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            panel.tag = "CoffinWall";
            ApplyVisualColor(CreateVisual(panel.transform, visualPrefab, PrimitiveType.Cube, fallbackMaterial), visualColor);
            return panel;
        }

        private static GameObject CreateVisual(Transform parent, GameObject visualPrefab,
            PrimitiveType fallbackPrimitive, Material fallbackMaterial)
        {
            GameObject visualRoot = Child(parent, "Visual");
            if (visualPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
                instance.transform.SetParent(visualRoot.transform, false);
                foreach (Collider visualCollider in instance.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(visualCollider);
                foreach (Rigidbody visualBody in instance.GetComponentsInChildren<Rigidbody>(true))
                    Object.DestroyImmediate(visualBody);
            }
            else
            {
                GameObject primitive = GameObject.CreatePrimitive(fallbackPrimitive);
                primitive.name = fallbackPrimitive.ToString();
                primitive.transform.SetParent(visualRoot.transform, false);
                Collider primitiveCollider = primitive.GetComponent<Collider>();
                if (primitiveCollider != null)
                    Object.DestroyImmediate(primitiveCollider);
                foreach (Renderer renderer in primitive.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = fallbackMaterial;
            }

            return visualRoot;
        }

        private static InputActionReference ReadActionReference(XRInputButtonReader reader)
        {
            return reader != null && reader.inputSourceMode == XRInputButtonReader.InputSourceMode.InputActionReference
                ? reader.inputActionReferencePerformed
                : null;
        }

        private static VisualCatalog LoadOrCreateVisualCatalog()
        {
            VisualCatalog catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(VisualCatalogPath);
            if (catalog != null)
                return catalog;

            catalog = ScriptableObject.CreateInstance<VisualCatalog>();
            AssetDatabase.CreateAsset(catalog, VisualCatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void EnsureTag(string tag)
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
            if (tagManagerAsset == null)
                throw new System.InvalidOperationException("Unity TagManager.asset could not be loaded.");

            SerializedObject serialized = new SerializedObject(tagManagerAsset);
            SerializedProperty tags = serialized.FindProperty("tags");
            for (int index = 0; index < tags.arraySize; index++)
            {
                if (tags.GetArrayElementAtIndex(index).stringValue == tag)
                    return;
            }

            int newIndex = tags.arraySize;
            tags.InsertArrayElementAtIndex(newIndex);
            tags.GetArrayElementAtIndex(newIndex).stringValue = tag;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

    }
}
