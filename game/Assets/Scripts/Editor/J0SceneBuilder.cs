using BadFaith.Gameplay;
using BadFaith.UI;
using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Editing.PrefabCollectionGenerator;
using FishNet.Managing;
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
            // les prefabs réseau existent — équivalent du menu Refresh Default Prefabs.
            Generator.GenerateFull(null, true);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildArena();
            Transform[] spawns = BuildSpawnPoints();
            BuildNetworkManager(playerPrefab, spawns);
            ScatterCubes(cubePrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
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
            var nmSo = new SerializedObject(nm);
            nmSo.FindProperty("_spawnablePrefabs").objectReferenceValue = Generator.GetDefaultPrefabObjects();
            nmSo.ApplyModifiedPropertiesWithoutUndo();

            var spawner = go.AddComponent<PlayerSpawner>();
            spawner.Spawns = spawns;
            var so = new SerializedObject(spawner);
            so.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab.GetComponent<NetworkObject>();
            so.ApplyModifiedPropertiesWithoutUndo();
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
