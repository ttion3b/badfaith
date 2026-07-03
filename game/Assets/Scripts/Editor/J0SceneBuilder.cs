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

            GameObject playerPrefab = BuildPlayerPrefab();
            GameObject cubePrefab = BuildCubePrefab();

            // Régénère DefaultPrefabObjects (AssetPathHash inclus) maintenant que
            // les prefabs réseau existent. Le Generator FishNet est internal :
            // on passe par son menu, API publique stable.
            // NB : une erreur "same assetPath hash of 0" peut apparaître PENDANT la
            // sauvegarde des prefabs ci-dessus — état transitoire, corrigé ici.
            EditorApplication.ExecuteMenuItem("Tools/Fish-Networking/Utility/Refresh Default Prefabs");
            Debug.Log("[J0SceneBuilder] Collection de prefabs régénérée — une éventuelle erreur de hash pendant la construction est transitoire et désormais résolue.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildArena();
            BuildTerminal();
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
            var parent = new GameObject("SpawnPoints").transform;
            var spawns = new Transform[8];
            for (int i = 0; i < spawns.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / spawns.Length;
                var sp = new GameObject($"Spawn_{i}").transform;
                sp.SetParent(parent);
                sp.position = new Vector3(Mathf.Cos(angle) * 12f, 1.2f, Mathf.Sin(angle) * 12f);
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

        private static GameObject BuildPlayerPrefab()
        {
            var root = new GameObject("Player");

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            capsule.transform.SetParent(root.transform);
            capsule.transform.localPosition = Vector3.zero;
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>()); // le CharacterController suffit

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

            var motor = root.AddComponent<PlayerMotor>();
            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("_cameraHolder").objectReferenceValue = cameraHolder;
            motorSo.ApplyModifiedPropertiesWithoutUndo();

            var grabber = root.AddComponent<PlayerGrabber>();
            var grabberSo = new SerializedObject(grabber);
            grabberSo.FindProperty("_cameraHolder").objectReferenceValue = cameraHolder;
            grabberSo.FindProperty("_holdAnchor").objectReferenceValue = holdAnchor;
            grabberSo.ApplyModifiedPropertiesWithoutUndo();

            // La Montre : indicateur de poignet visible par tous (le tell social du GDD).
            var wrist = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wrist.name = "WristWatch";
            Object.DestroyImmediate(wrist.GetComponent<BoxCollider>());
            wrist.transform.SetParent(root.transform);
            wrist.transform.localPosition = new Vector3(0.42f, -0.15f, 0.45f);
            wrist.transform.localScale = new Vector3(0.14f, 0.1f, 0.14f);

            var watch = root.AddComponent<PlayerWatch>();
            var watchSo = new SerializedObject(watch);
            watchSo.FindProperty("_wristIndicator").objectReferenceValue = wrist.GetComponent<Renderer>();
            watchSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildCubePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GrabCube";
            go.transform.localScale = Vector3.one * 0.6f;

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

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CubePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
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
