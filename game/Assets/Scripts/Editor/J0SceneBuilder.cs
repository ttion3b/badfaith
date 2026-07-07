using BadFaith.Gameplay;
using BadFaith.UI;
using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BadFaith.EditorTools
{
    /// <summary>
    /// Construit la scène de test J0 ("6 joueurs dans une boîte grise qui se
    /// lancent des cubes") en un clic : sol, murs, NetworkManager + Tugboat,
    /// prefabs Joueur et Cube, spawns. Relançable : écrase et reconstruit.
    /// </summary>
    public static class J0SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/J0_GreyBox.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string CubePrefabPath = "Assets/Prefabs/GrabCube.prefab";

        [MenuItem("MAUVAISE FOI/Construire la scène J0")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            System.IO.Directory.CreateDirectory("Assets/Prefabs");
            System.IO.Directory.CreateDirectory("Assets/Scenes");

            /* Le générateur de prefabs FishNet est coupé pendant nos sauvegardes :
             * son incrémental re-hashe des références périmées et hurle "same
             * assetPath hash of 0" (faux positif). Le refresh complet, forcé
             * juste après, reconstruit la collection proprement. */
            FishNet.Configuring.Configuration.Configurations.PrefabGenerator.Enabled = false;
            AssetDatabase.StartAssetEditing();
            try
            {
                BuildPlayerPrefab();
                BuildCubePrefab();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                FishNet.Configuring.Configuration.Configurations.PrefabGenerator.Enabled = true;
            }

            // Régénère DefaultPrefabObjects (AssetPathHash inclus). Le Generator
            // FishNet est internal : on passe par son menu, API publique stable.
            EditorApplication.ExecuteMenuItem("Tools/Fish-Networking/Utility/Refresh Default Prefabs");

            // Recharge les assets après l'import groupé (les références retournées
            // pendant StartAssetEditing ne sont pas fiables).
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CubePrefabPath);
            if (playerPrefab == null || cubePrefab == null)
            {
                Debug.LogError("[J0SceneBuilder] Prefabs introuvables après import — relance la construction.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            if (FacilityMapBuilder.AssetsAvailable)
                FacilityMapBuilder.Build(seed: System.Environment.TickCount);
            else
            {
                Debug.LogWarning("[J0SceneBuilder] Pack Synty absent — construction de l'arène grise de secours. (Package Manager > My Assets > POLYGON Sampler Pack)");
                BuildArena();
            }
            BuildTerminal();
            BuildCapsule();
            BuildJudge();
            Transform[] spawns = BuildSpawnPoints();
            BuildNetworkManager(playerPrefab, spawns);
            BuildGameServices();
            ScatterCubes(cubePrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);

            /* Les SceneIds FishNet ne peuvent pas être créés avant que la scène ait
             * un chemin (OnValidate des NetworkObject tourne à vide sur une scène
             * non sauvée). On rejoue donc l'équivalent exact du menu
             * Tools > Fish-Networking > Utility > Reserialize NetworkObjects,
             * puis on resauve. Les méthodes sont internal → réflexion (FishNet
             * vendoré, version figée). */
            ReserializeSceneNetworkObjects(scene);
            EditorSceneManager.SaveScene(scene);

            AddSceneToBuildSettings();

            Debug.Log("[J0SceneBuilder] Scène J0 construite : " + ScenePath + " — presse Play puis HÉBERGER.");
        }

        private static void BuildArena()
        {
            CreateBox("Sol", new Vector3(0, -0.5f, 0), new Vector3(40, 1, 40), new Color(0.35f, 0.35f, 0.38f));
            CreateBox("Mur_N", new Vector3(0, 2, 20.5f), new Vector3(41, 5, 1), new Color(0.45f, 0.45f, 0.5f));
            CreateBox("Mur_S", new Vector3(0, 2, -20.5f), new Vector3(41, 5, 1), new Color(0.45f, 0.45f, 0.5f));
            CreateBox("Mur_E", new Vector3(20.5f, 2, 0), new Vector3(1, 5, 41), new Color(0.45f, 0.45f, 0.5f));
            CreateBox("Mur_O", new Vector3(-20.5f, 2, 0), new Vector3(1, 5, 41), new Color(0.45f, 0.45f, 0.5f));
            // Quelques obstacles pour se cacher / viser.
            CreateBox("Caisse_A", new Vector3(5, 1, 3), new Vector3(2, 2, 2), new Color(0.55f, 0.5f, 0.4f));
            CreateBox("Caisse_B", new Vector3(-6, 1, -4), new Vector3(2, 2, 2), new Color(0.55f, 0.5f, 0.4f));
            CreateBox("Caisse_C", new Vector3(-2, 1, 8), new Vector3(4, 2, 2), new Color(0.55f, 0.5f, 0.4f));

            // La caméra de scène (vue d'attente avant connexion).
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 14, -24);
                cam.transform.rotation = Quaternion.Euler(30, 0, 0);
            }
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            var mat = new Material(renderer.sharedMaterial) { color = color };
            renderer.sharedMaterial = mat;
            return go;
        }

        private static Transform[] BuildSpawnPoints()
        {
            // En cercle dans le hub, autour du Terminal.
            var parent = new GameObject("SpawnPoints").transform;
            var spawns = new Transform[8];
            for (int i = 0; i < spawns.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / spawns.Length;
                var sp = new GameObject($"Spawn_{i}").transform;
                sp.SetParent(parent);
                sp.position = new Vector3(Mathf.Cos(angle) * 5f, 1.2f, Mathf.Sin(angle) * 5f);
                sp.LookAt(new Vector3(0, 1.2f, 0));
                spawns[i] = sp;
            }
            return spawns;
        }

        private static void BuildNetworkManager(GameObject playerPrefab, Transform[] spawns)
        {
            var go = new GameObject("NetworkManager");
            var nm = go.AddComponent<NetworkManager>();
            go.AddComponent<Tugboat>(); // TransportManager le détecte automatiquement sur le même GameObject.
            go.AddComponent<NetworkLauncherHUD>();

            // Assigne explicitement la collection de prefabs spawnables : l'auto-détection
            // de FishNet ne se déclenche pas sur un NetworkManager créé par AddComponent.
            var prefabCollection = AssetDatabase.LoadAssetAtPath<PrefabObjects>("Assets/DefaultPrefabObjects.asset");
            if (prefabCollection == null)
            {
                Debug.LogError("[J0SceneBuilder] DefaultPrefabObjects.asset introuvable — lance Tools > Fish-Networking > Utility > Refresh Default Prefabs puis reconstruis la scène.");
            }
            var nmSo = new SerializedObject(nm);
            nmSo.FindProperty("_spawnablePrefabs").objectReferenceValue = prefabCollection;
            nmSo.ApplyModifiedPropertiesWithoutUndo();

            var spawner = go.AddComponent<PlayerSpawner>();
            spawner.Spawns = spawns;
            var so = new SerializedObject(spawner);
            so.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab.GetComponent<NetworkObject>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Objet réseau de scène portant les services de gameplay côté hôte (Pactes, accidents, économie).</summary>
        private static void BuildGameServices()
        {
            var go = new GameObject("GameServices");
            go.AddComponent<NetworkObject>();
            go.AddComponent<HazardExecutor>();
            go.AddComponent<PacteNetworkService>();
            go.AddComponent<EconomyNetworkService>();
            go.AddComponent<RoundManager>();
            go.AddComponent<TribunalNetworkService>();
            go.AddComponent<LootRespawner>();
        }

        /// <summary>Le Juge : l'unique revolver. Objet de scène, il se téléporte sur un des 5 spots côté serveur.</summary>
        private static void BuildJudge()
        {
            // Parent neutre (échelle 1) : le visuel enfant ne sera pas déformé.
            var gun = new GameObject("TheJudge");
            gun.transform.position = new Vector3(0f, 0.6f, -5f);
            var grabCollider = gun.AddComponent<BoxCollider>();
            grabCollider.size = new Vector3(0.35f, 0.4f, 0.8f); // volume de prise généreux

            var pistolAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SyntyStudios/PolygonAdventure/Prefabs/Weapons/SM_Wep_MusketPistol_01.prefab");
            if (pistolAsset != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(pistolAsset);
                PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                visual.name = "Visual";
                visual.transform.SetParent(gun.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var c in visual.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(c);

                // Ajusté par bounds à ~0,7 m de long : bien visible dans une main.
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool first = true;
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                {
                    if (first) { bounds = r.bounds; first = false; }
                    else bounds.Encapsulate(r.bounds);
                }
                float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (longest > 0.01f)
                    visual.transform.localScale = Vector3.one * (0.7f / longest);
                // Recentrage : bounds recalculés après scale.
                var b2 = new Bounds(Vector3.zero, Vector3.zero);
                first = true;
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b2 = r.bounds; first = false; }
                    else b2.Encapsulate(r.bounds);
                }
                visual.transform.position += gun.transform.position - b2.center;
            }
            else
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = "Visual";
                Object.DestroyImmediate(fallback.GetComponent<BoxCollider>());
                fallback.transform.SetParent(gun.transform);
                fallback.transform.localPosition = Vector3.zero;
                fallback.transform.localScale = new Vector3(0.14f, 0.2f, 0.55f);
                var fbRenderer = fallback.GetComponent<Renderer>();
                fbRenderer.sharedMaterial = new Material(fbRenderer.sharedMaterial) { color = new Color(0.05f, 0.05f, 0.06f) };
            }
            var rb = gun.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            gun.AddComponent<NetworkObject>();
            var nt = gun.AddComponent<NetworkTransform>();
            var ntSo = new SerializedObject(nt);
            ntSo.FindProperty("_clientAuthoritative").boolValue = false;
            ntSo.ApplyModifiedPropertiesWithoutUndo();

            gun.AddComponent<NetworkGrabbable>();
            gun.AddComponent<TheJudge>();
        }

        /// <summary>La capsule d'extraction, au nord de l'arène. Position/rayon : RoundManager.CapsuleCenter.</summary>
        private static void BuildCapsule()
        {
            Vector3 c = RoundManager.CapsuleCenter;
            var parent = new GameObject("Capsule");
            parent.transform.position = c;

            CreateBox("CapsuleSol", new Vector3(c.x, 0.05f, c.z), new Vector3(6, 0.1f, 5), new Color(0.2f, 0.35f, 0.25f)).transform.SetParent(parent.transform);
            CreateBox("CapsuleMur_N", new Vector3(c.x, 1.5f, c.z + 2.4f), new Vector3(6, 3, 0.2f), new Color(0.3f, 0.45f, 0.35f)).transform.SetParent(parent.transform);
            CreateBox("CapsuleMur_E", new Vector3(c.x + 2.9f, 1.5f, c.z), new Vector3(0.2f, 3, 5), new Color(0.3f, 0.45f, 0.35f)).transform.SetParent(parent.transform);
            CreateBox("CapsuleMur_O", new Vector3(c.x - 2.9f, 1.5f, c.z), new Vector3(0.2f, 3, 5), new Color(0.3f, 0.45f, 0.35f)).transform.SetParent(parent.transform);
            CreateBox("CapsuleToit", new Vector3(c.x, 3.05f, c.z), new Vector3(6, 0.1f, 5), new Color(0.25f, 0.4f, 0.3f)).transform.SetParent(parent.transform);

            // Le bouton rouge, bien visible au fond.
            CreateBox("BoutonRouge", new Vector3(c.x, 1.2f, c.z + 2.2f), new Vector3(0.4f, 0.4f, 0.15f), Color.red).transform.SetParent(parent.transform);

            // Barrière d'entrée : visible tant que la capsule n'est pas "arrivée"
            // (RoundManager la masque en phase Extraction).
            var barrier = CreateBox("CapsuleBarrier", new Vector3(c.x, 1.5f, c.z - 2.4f), new Vector3(6, 3, 0.25f), new Color(0.8f, 0.2f, 0.2f));
            barrier.transform.SetParent(parent.transform);
        }

        /// <summary>Le Terminal central : kiosque + écran public des dépôts.</summary>
        private static void BuildTerminal()
        {
            var kiosk = CreateBox("Terminal", new Vector3(0, 0.75f, 0), new Vector3(1.4f, 1.5f, 0.7f), new Color(0.15f, 0.2f, 0.3f));

            var screenGo = new GameObject("TerminalScreen");
            screenGo.transform.SetParent(kiosk.transform);
            screenGo.transform.position = new Vector3(0, 2.4f, 0);
            var screen = screenGo.AddComponent<TextMesh>();
            screen.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screenGo.GetComponent<MeshRenderer>().material = screen.font.material;
            screen.fontSize = 56;
            screen.characterSize = 0.045f;
            screen.anchor = TextAnchor.MiddleCenter;
            screen.alignment = TextAlignment.Center;
            screen.color = new Color(0.4f, 1f, 0.6f);
            screen.text = "TERMINAL";

            // Deuxième face de l'écran (lisible des deux côtés).
            var backGo = Object.Instantiate(screenGo, kiosk.transform);
            backGo.name = "TerminalScreenBack";
            backGo.transform.position = screenGo.transform.position;
            backGo.transform.rotation = Quaternion.Euler(0, 180, 0);

            kiosk.AddComponent<NetworkObject>();
            kiosk.AddComponent<DepositTerminal>(); // trouve ses écrans (TextMesh enfants) tout seul
        }

        /// <summary>Les 15 silhouettes jouables (l'identité visuelle des accusations).</summary>
        private static readonly string[] CharacterVariantPaths =
        {
            "Character_Knight_White", "Character_Knight_Brown", "Character_Knight_Black",
            "Character_Peasant_White", "Character_Peasant_Brown", "Character_Peasant_Black",
            "Character_Shopkeeper_White", "Character_Shopkeeper_Brown", "Character_Shopkeeper_Black",
            "Character_Viking_White", "Character_Viking_Brown", "Character_Viking_Black",
            "Character_Warrior_White", "Character_Warrior_Brown", "Character_Warrior_Black",
        };

        private const string StarterAnimDir = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/";

        private static AnimationClip LoadClip(string fbxName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(StarterAnimDir + fbxName))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                    return clip;
            Debug.LogWarning($"[J0SceneBuilder] Clip introuvable dans {fbxName}");
            return null;
        }

        /// <summary>Contrôleur d'animation : blend 1D Idle → Marche → Course piloté par Speed.</summary>
        private static RuntimeAnimatorController BuildAnimatorController()
        {
            System.IO.Directory.CreateDirectory("Assets/Anim");
            const string path = "Assets/Anim/BadFaithCharacter.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            // API canonique : crée l'état + le blend tree DANS l'asset (persistance gérée).
            var state = controller.CreateBlendTreeInController("Locomotion", out UnityEditor.Animations.BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            tree.useAutomaticThresholds = false;
            tree.AddChild(LoadClip("Stand--Idle.anim.fbx"), 0f);
            tree.AddChild(LoadClip("Locomotion--Walk_N.anim.fbx"), 2.2f);
            tree.AddChild(LoadClip("Locomotion--Run_N.anim.fbx"), 6.5f);
            controller.layers[0].stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void BuildPlayerPrefab()
        {
            var root = new GameObject("Player");

            var cc = root.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;

            var cameraHolder = new GameObject("CameraHolder").transform;
            cameraHolder.SetParent(root.transform);
            cameraHolder.localPosition = new Vector3(0, 0.65f, 0);

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(cameraHolder);
            camGo.transform.localPosition = Vector3.zero;
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.SetActive(false); // activée par PlayerMotor pour le propriétaire uniquement

            var holdAnchor = new GameObject("HoldAnchor").transform;
            holdAnchor.SetParent(cameraHolder);
            holdAnchor.localPosition = new Vector3(0, -0.2f, 1.6f);

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>(); // _clientAuthoritative = true par défaut : le propriétaire pilote
            root.AddComponent<PlayerHealth>();

            var motor = root.AddComponent<PlayerMotor>();
            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("_cameraHolder").objectReferenceValue = cameraHolder;
            motorSo.ApplyModifiedPropertiesWithoutUndo();

            var grabber = root.AddComponent<PlayerGrabber>();
            var grabberSo = new SerializedObject(grabber);
            grabberSo.FindProperty("_cameraHolder").objectReferenceValue = cameraHolder;
            grabberSo.FindProperty("_holdAnchor").objectReferenceValue = holdAnchor;
            grabberSo.ApplyModifiedPropertiesWithoutUndo();

            // La Montre : l'indicateur de poignet est posé à runtime par
            // PlayerAppearance, sur la vraie main du modèle 3D.
            root.AddComponent<PlayerWatch>();

            // L'apparence : une des 15 silhouettes Synty + animations.
            var appearance = root.AddComponent<PlayerAppearance>();
            var appearanceSo = new SerializedObject(appearance);
            var variantsProp = appearanceSo.FindProperty("_characterVariants");
            variantsProp.arraySize = CharacterVariantPaths.Length;
            for (int i = 0; i < CharacterVariantPaths.Length; i++)
            {
                var characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/SyntyStudios/PolygonAdventure/Prefabs/Characters/{CharacterVariantPaths[i]}.prefab");
                variantsProp.GetArrayElementAtIndex(i).objectReferenceValue = characterPrefab;
            }
            appearanceSo.FindProperty("_animatorController").objectReferenceValue = BuildAnimatorController();

            // Sons de pas Starter Assets (joués par les AnimationEvents des clips).
            const string sfxDir = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/";
            var footsteps = appearanceSo.FindProperty("_footstepClips");
            footsteps.arraySize = 10;
            for (int i = 0; i < 10; i++)
                footsteps.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxDir}Player_Footstep_{i + 1:00}.wav");
            appearanceSo.FindProperty("_landClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(sfxDir + "Player_Land.wav");

            appearanceSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildCubePrefab()
        {
            GameObject go;
            var crateAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SyntyStudios/PolygonAdventure/Prefabs/Props/SM_Prop_Crate_01.prefab");
            if (crateAsset != null)
            {
                // Racine simple + visuel Synty : la physique reste sur un BoxCollider propre.
                go = new GameObject("GrabCube");
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(crateAsset);
                PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                visual.name = "Visual";
                visual.transform.SetParent(go.transform);
                foreach (var c in visual.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(c);

                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool first = true;
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                {
                    if (first) { bounds = r.bounds; first = false; }
                    else bounds.Encapsulate(r.bounds);
                }
                visual.transform.localPosition = -bounds.center;
                float scale = 0.7f / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                visual.transform.localScale = Vector3.one * scale;

                var col = go.AddComponent<BoxCollider>();
                col.size = bounds.size * scale;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "GrabCube";
                go.transform.localScale = Vector3.one * 0.6f;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            go.AddComponent<NetworkObject>();
            var nt = go.AddComponent<NetworkTransform>();
            // Autorité serveur pour la physique : l'hôte simule, les clients suivent.
            var ntSo = new SerializedObject(nt);
            ntSo.FindProperty("_clientAuthoritative").boolValue = false;
            ntSo.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<NetworkGrabbable>();
            go.AddComponent<LootItem>();

            PrefabUtility.SaveAsPrefabAsset(go, CubePrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void ScatterCubes(GameObject cubePrefab)
        {
            var parent = new GameObject("Cubes").transform;
            var rng = new System.Random(42);
            for (int i = 0; i < 14; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(cubePrefab);
                instance.transform.SetParent(parent);
                float x = (float)(rng.NextDouble() * 20.0 - 10.0);
                float z = (float)(rng.NextDouble() * 20.0 - 10.0);
                instance.transform.position = new Vector3(x, 0.5f + i * 0.05f, z);
            }
        }

        private static void ReserializeSceneNetworkObjects(Scene scene)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var createSceneId = typeof(NetworkObject).GetMethod(
                "CreateSceneId",
                flags | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(Scene), typeof(bool), typeof(int).MakeByRefType() },
                null);
            var reserialize = typeof(NetworkObject).GetMethod(
                "ReserializeEditorSetValues",
                flags | System.Reflection.BindingFlags.Instance);
            if (createSceneId == null || reserialize == null)
            {
                Debug.LogError("[J0SceneBuilder] API interne FishNet introuvable — lance manuellement Tools > Fish-Networking > Utility > Reserialize NetworkObjects (Scenes).");
                return;
            }

            object[] args = { scene, true, 0 };
            var nobs = (System.Collections.Generic.List<NetworkObject>)createSceneId.Invoke(null, args);
            foreach (var nob in nobs)
            {
                reserialize.Invoke(nob, new object[] { true, false });
                EditorUtility.SetDirty(nob);
            }
            Debug.Log($"[J0SceneBuilder] SceneIds FishNet régénérés pour {nobs.Count} NetworkObjects de scène.");
        }

        private static void AddSceneToBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == ScenePath)
                    return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true),
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
