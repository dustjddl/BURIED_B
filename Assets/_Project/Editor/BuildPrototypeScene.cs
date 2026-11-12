using System.IO;
using TMPro;
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
            if (visualCatalog.lighterPrefab == null)
            {
                visualCatalog.lighterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/devotid/Lighter/Prefabs/Lighter_Prefab.prefab");
                EditorUtility.SetDirty(visualCatalog);
            }
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

            BuildM3(environment.transform, resources, lighter, config, gray, movementBounds,
                origin.Camera != null ? origin.Camera.transform : rig.transform);

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

        private static void BuildM3(Transform parent, ResourceManager resources, GrabbableItem lighter,
            GameConfig config, Material material, BoxCollider bounds, Transform reader)
        {
            RunData run = RunGenerator.Generate(PlayerPrefs.GetInt("CoffinEscape.RunSeed", 1));
            Light lighterLight = lighter.GetComponentInChildren<Light>(true);
            Vector3[] pilePositions={config.pileAPositionMeters,config.pileBPositionMeters};
            PileContent[] pileContents={run.PileA,run.PileB};
            for(int i=0;i<2;i++)
            {
                GameObject pile=GameObject.CreatePrimitive(PrimitiveType.Cylinder); pile.name=$"P1 Pile {(i==0?"A":"B")}"; pile.transform.SetParent(parent,false); pile.transform.localPosition=pilePositions[i]; pile.transform.localScale=new Vector3(config.puzzleItemSizeMeters*1.5f,config.puzzleItemSizeMeters*.35f,config.puzzleItemSizeMeters*1.5f); pile.GetComponent<Renderer>().sharedMaterial=material; pile.GetComponent<Collider>().isTrigger=true; Rigidbody pileBody=pile.AddComponent<Rigidbody>(); pileBody.isKinematic=true;
                var touch=pile.AddComponent<PileInteractable>();
                Vector3 itemPos=pilePositions[i]+Vector3.up*config.puzzleItemSizeMeters;
                GrabbableItem item=CreateGrabObject(pileContents[i]==PileContent.Photo?"Photo Clue Pickup":"Spare Oil Pickup",parent,itemPos,Vector3.one*config.puzzleItemSizeMeters,PrimitiveType.Cube,null,material,pileContents[i]==PileContent.Photo?config.testCubeColor:config.lighterBodyColor,bounds,itemPos,pileContents[i]+" Return Anchor");
                touch.Configure(pileContents[i],item,resources,config.oilFuelBonusSeconds);
                if(pileContents[i]==PileContent.Photo) CreateClue(parent,"Photo Back Clue",ClueLocation.PhotoBack,run.TinLock,lighterLight,config,material,item.transform,reader);
            }
            Vector3 tinPos=config.tinCanPositionMeters;
            GameObject tin=GameObject.CreatePrimitive(PrimitiveType.Cube); tin.name="P2 Tin Can"; tin.transform.SetParent(parent,false); tin.transform.localPosition=tinPos; tin.transform.localScale=Vector3.one*config.tinCanSizeMeters; tin.GetComponent<Renderer>().sharedMaterial=material;
            var tinLock=tin.AddComponent<CodeLock>();
            Transform lid=new GameObject("Tin Can Lid Hinge").transform; lid.SetParent(tin.transform,false); lid.localPosition=Vector3.up*.5f;
            GameObject lidVisual=GameObject.CreatePrimitive(PrimitiveType.Cube); lidVisual.name="Lid"; lidVisual.transform.SetParent(lid,false); lidVisual.transform.localPosition=new Vector3(0,.05f,0); lidVisual.transform.localScale=new Vector3(1.02f,.12f,1.02f); lidVisual.GetComponent<Renderer>().sharedMaterial=material; Object.DestroyImmediate(lidVisual.GetComponent<Collider>());
            GameObject[] rewards=new GameObject[2];
            for(int i=0;i<2;i++){rewards[i]=GameObject.CreatePrimitive(i==0?PrimitiveType.Cube:PrimitiveType.Sphere); rewards[i].name=i==0?"P2 Tool":"P4 Talisman"; rewards[i].transform.SetParent(parent,false); rewards[i].transform.localPosition=tinPos+new Vector3((i==0?-.08f:.08f),.08f,0); rewards[i].transform.localScale=Vector3.one*config.puzzleItemSizeMeters; rewards[i].GetComponent<Renderer>().sharedMaterial=material; rewards[i].SetActive(false);}
            TextMeshPro status=CreateWorldText(tin.transform,"Lock Status",new Vector3(0,.8f,-.52f),config); status.text="LOCKED";
            TMP_Text[] numbers=new TMP_Text[3];
            for(int i=0;i<3;i++)
            {
                Vector3 slotPos=new Vector3((i-1)*.32f,.5f,-.52f);
                GameObject symbol=new GameObject("Dial Symbol"); symbol.transform.SetParent(tin.transform,false); symbol.transform.localPosition=slotPos; var view=symbol.AddComponent<SymbolView>(); view.Set(run.TinLock.SymbolAt(i),.16f,material);
                TextMeshPro number=CreateWorldText(tin.transform,"Dial Number",slotPos+Vector3.up*.18f,config); number.text="0"; numbers[i]=number;
                CreateDialButton(tin.transform,tinLock,i,1,false,new Vector3(slotPos.x,slotPos.y+.37f,slotPos.z),config);
                CreateDialButton(tin.transform,tinLock,i,-1,false,new Vector3(slotPos.x,slotPos.y-.27f,slotPos.z),config);
            }
            tinLock.Configure(run.TinLock,resources,lid,rewards,config.lockLidOpenSeconds,config.lockLidOpenAngleDegrees,status,numbers);
            CreateDialButton(tin.transform,tinLock,0,0,true,new Vector3(0,-.2f,-.52f),config);
            CreateClue(parent,"Right Graffiti 1",ClueLocation.RightGraffiti,run.TinLock,lighterLight,config,material,null,reader);
            CreateClue(parent,"Left Graffiti 2",ClueLocation.LeftGraffiti,run.TinLock,lighterLight,config,material,null,reader);
            parent.gameObject.AddComponent<RunDebugControls>().Configure(config,tinLock,run,status);
        }

        private static void CreateClue(Transform parent,string name,ClueLocation location,CodeRunData data,Light light,GameConfig config,Material symbolMaterial,Transform photo,Transform reader)
        {
            int slot=System.Array.IndexOf(data.ClueLocations,location); if(slot<0)return;
            GameObject clue=new GameObject(name); clue.transform.SetParent(photo!=null?photo:parent,false); clue.transform.localPosition=photo!=null?new Vector3(0,0,-.51f):location==ClueLocation.RightGraffiti?new Vector3(config.coffinWidthMeters*.47f,.26f,.9f):new Vector3(-config.coffinWidthMeters*.47f,.26f,.9f);
            GameObject mesh=new GameObject("Symbol Mesh"); mesh.transform.SetParent(clue.transform,false); mesh.transform.localPosition=Vector3.left*.07f; var symbol=mesh.AddComponent<SymbolView>(); symbol.Set(data.SymbolAt(slot),.09f,symbolMaterial);
            TextMeshPro label=CreateWorldText(clue.transform,"Clue Digit",Vector3.right*.07f,config); label.text=data.Digits[slot].ToString();
            ClueItem item=clue.AddComponent<ClueItem>(); item.Configure(data.SymbolAt(slot),data.Digits[slot],light,reader,config.clueReadDistanceMeters,label);
        }

        private static TextMeshPro CreateWorldText(Transform parent,string name,Vector3 localPosition,GameConfig config)
        {
            GameObject go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=localPosition; go.transform.localRotation=Quaternion.identity;
            var text=go.AddComponent<TextMeshPro>(); text.fontSize=.25f; text.alignment=TextAlignmentOptions.Center; text.color=Color.white; text.rectTransform.sizeDelta=new Vector2(.35f,.2f); return text;
        }

        private static void CreateDialButton(Transform parent,CodeLock codeLock,int slot,int delta,bool confirm,Vector3 position,GameConfig config)
        {
            GameObject button=GameObject.CreatePrimitive(PrimitiveType.Cube); button.name=confirm?"Confirm Button":delta>0?"Dial Up":"Dial Down"; button.transform.SetParent(parent,false); button.transform.localPosition=position; button.transform.localScale=new Vector3(.16f,.1f,.05f); button.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Settings/Graybox.mat");
            button.AddComponent<DialButton>().Configure(codeLock,slot,delta,confirm,config.dialButtonCooldownSeconds);
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
