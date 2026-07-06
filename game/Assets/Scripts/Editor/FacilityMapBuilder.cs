using System.Collections.Generic;
using BadFaith.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BadFaith.EditorTools
{
    /// <summary>
    /// Construit LE COMPLEXE : une installation de La Direction (pièces
    /// POLYGON Prototype) bâtie sur des ruines médiévales (aile ouest en
    /// pierres POLYGON Knights). Plan en croix : hub central (Terminal),
    /// 4 ailes-zones. Décor semé par seed — relançable pour varier.
    /// </summary>
    public static class FacilityMapBuilder
    {
        // ---- Chemins des prefabs Synty (vérifiés à l'import) ----
        private const string Proto = "Assets/SyntyStudios/PolygonPrototype/Prefabs/Buildings/Polygon/";
        private const string Adv = "Assets/SyntyStudios/PolygonAdventure/Prefabs/";
        private const string Knights = "Assets/SyntyStudios/PolygonKnights/Prefabs/";

        private const string FloorPath = Proto + "SM_Buildings_Floor_5x5_01P.prefab";
        private const string WallPath = Proto + "SM_Buildings_Wall_5x3_01P.prefab";
        private const string WallDoorPath = Proto + "SM_Buildings_WallDoor_5x3_01P.prefab";
        private const string ColumnPath = Proto + "SM_Buildings_Column_1x3_01P.prefab";
        private const string CastleWallPath = Knights + "Buildings/SM_Bld_Castle_Wall_01.prefab";
        private const string CastlePillarPath = Knights + "Buildings/SM_Bld_Castle_Pillar_01.prefab";
        private const string LanternPath = Adv + "Items/SM_Item_Lantern_01.prefab";
        private const string CratePath = Adv + "Props/SM_Prop_Crate_01.prefab";
        private const string BarrelPath = Adv + "Props/SM_Prop_Barrel_01.prefab";
        private const string SackPath = Adv + "Props/SM_Prop_Sack_01.prefab";
        private const string ChestPath = Adv + "Props/SM_Prop_Chest_01.prefab";

        private struct Room
        {
            public int ZoneId;
            public string Name;
            public Rect Area;       // en mètres, plan XZ
            public bool CastleTheme;
            public Color LightColor;
        }

        // Le plan en croix : hub 15x15, ailes 15x15, empreinte 45x45.
        private static readonly Room[] Rooms =
        {
            new Room { ZoneId = 0, Name = "Hub",            Area = Rect.MinMaxRect(-7.5f, -7.5f, 7.5f, 7.5f),    CastleTheme = false, LightColor = new Color(1f, 0.95f, 0.85f) },
            new Room { ZoneId = 1, Name = "Aile Ruines",    Area = Rect.MinMaxRect(-22.5f, -7.5f, -7.5f, 7.5f),  CastleTheme = true,  LightColor = new Color(1f, 0.6f, 0.25f) },
            new Room { ZoneId = 2, Name = "Aile Est",       Area = Rect.MinMaxRect(7.5f, -7.5f, 22.5f, 7.5f),    CastleTheme = false, LightColor = new Color(0.75f, 0.85f, 1f) },
            new Room { ZoneId = 3, Name = "Entrepôt Sud",   Area = Rect.MinMaxRect(-7.5f, -22.5f, 7.5f, -7.5f),  CastleTheme = false, LightColor = new Color(1f, 0.85f, 0.6f) },
            new Room { ZoneId = 4, Name = "Quai Capsule",   Area = Rect.MinMaxRect(-7.5f, 7.5f, 7.5f, 22.5f),    CastleTheme = false, LightColor = new Color(0.7f, 1f, 0.8f) },
        };

        public static bool AssetsAvailable => AssetDatabase.LoadAssetAtPath<GameObject>(FloorPath) != null;

        /// <summary>Construit le complexe. Retourne le parent des zones (pour les spawns de loot).</summary>
        public static void Build(int seed)
        {
            var rng = new System.Random(seed);
            var mapRoot = new GameObject("Facility").transform;

            foreach (var room in Rooms)
                BuildRoom(room, mapRoot, rng);

            BuildInnerDoorWalls(mapRoot);
            SetupLightingAmbiance();
            Debug.Log($"[FacilityMapBuilder] Complexe construit (seed {seed}) : {Rooms.Length} zones.");
        }

        // ==================== SALLES ====================

        private static void BuildRoom(Room room, Transform mapRoot, System.Random rng)
        {
            var zoneGo = new GameObject($"Zone_{room.ZoneId}_{room.Name}");
            zoneGo.transform.SetParent(mapRoot);
            var zone = zoneGo.AddComponent<MapZone>();
            zone.ZoneId = room.ZoneId;
            zone.ZoneName = room.Name;
            zone.AreaCenter = new Vector3(room.Area.center.x, 2f, room.Area.center.y);
            zone.AreaSize = new Vector3(room.Area.width, 6f, room.Area.height);

            // Sols : dalles 5x5 sur la grille.
            for (float x = room.Area.xMin + 2.5f; x < room.Area.xMax; x += 5f)
                for (float z = room.Area.yMin + 2.5f; z < room.Area.yMax; z += 5f)
                    PlaceFitted(FloorPath, new Vector3(x, 0f, z), 0f, zoneGo.transform, floorTopAtZero: true);

            // Murs extérieurs (les murs intérieurs avec portes sont posés à part).
            BuildPerimeterWalls(room, zoneGo.transform);

            // Lumières : lanternes + point lights aux quarts de la salle.
            PlaceZoneLight(zone, new Vector3(room.Area.center.x - room.Area.width * 0.25f, 0f, room.Area.center.y - room.Area.height * 0.25f), room.LightColor);
            PlaceZoneLight(zone, new Vector3(room.Area.center.x + room.Area.width * 0.25f, 0f, room.Area.center.y + room.Area.height * 0.25f), room.LightColor);

            // Habillage thématique semé.
            DressRoom(room, zoneGo.transform, rng);
        }

        /// <summary>Murs d'enceinte : uniquement les bords qui donnent sur l'extérieur du complexe.</summary>
        private static void BuildPerimeterWalls(Room room, Transform parent)
        {
            const float ext = 22.49f; // bord extérieur de l'empreinte
            var r = room.Area;

            for (float x = r.xMin + 2.5f; x < r.xMax; x += 5f)
            {
                if (r.yMax >= ext) PlaceFitted(WallPath, new Vector3(x, 0f, r.yMax), 0f, parent);
                if (r.yMin <= -ext) PlaceFitted(WallPath, new Vector3(x, 0f, r.yMin), 180f, parent);
            }
            for (float z = r.yMin + 2.5f; z < r.yMax; z += 5f)
            {
                if (r.xMax >= ext) PlaceFitted(WallPath, new Vector3(r.xMax, 0f, z), -90f, parent);
                if (r.xMin <= -ext) PlaceFitted(WallPath, new Vector3(r.xMin, 0f, z), 90f, parent);
            }

            // Les ailes ont aussi des murs sur leurs flancs (les coins de la croix sont vides).
            bool isWing = room.ZoneId != 0;
            if (!isWing)
                return;
            if (Mathf.Approximately(r.center.y, 0f)) // ailes Est/Ouest : flancs nord et sud
            {
                for (float x = r.xMin + 2.5f; x < r.xMax; x += 5f)
                {
                    if (r.yMax < ext) PlaceFitted(WallPath, new Vector3(x, 0f, r.yMax), 0f, parent);
                    if (r.yMin > -ext) PlaceFitted(WallPath, new Vector3(x, 0f, r.yMin), 180f, parent);
                }
            }
            else // ailes Nord/Sud : flancs est et ouest
            {
                for (float z = r.yMin + 2.5f; z < r.yMax; z += 5f)
                {
                    if (r.xMax < ext) PlaceFitted(WallPath, new Vector3(r.xMax, 0f, z), -90f, parent);
                    if (r.xMin > -ext) PlaceFitted(WallPath, new Vector3(r.xMin, 0f, z), 90f, parent);
                }
            }
        }

        /// <summary>Les 4 murs hub/ailes : plein, PORTE au centre, plein.</summary>
        private static void BuildInnerDoorWalls(Transform mapRoot)
        {
            var parent = new GameObject("InnerWalls").transform;
            parent.SetParent(mapRoot);

            // Ouest (x = -7.5) et Est (x = +7.5)
            foreach (float sign in new[] { -1f, 1f })
            {
                float x = 7.5f * sign;
                float yRot = sign > 0 ? -90f : 90f;
                PlaceFitted(WallPath, new Vector3(x, 0f, -5f), yRot, parent);
                PlaceFitted(WallDoorPath, new Vector3(x, 0f, 0f), yRot, parent);
                PlaceFitted(WallPath, new Vector3(x, 0f, 5f), yRot, parent);
            }
            // Sud (z = -7.5) et Nord (z = +7.5)
            foreach (float sign in new[] { -1f, 1f })
            {
                float z = 7.5f * sign;
                float yRot = sign > 0 ? 0f : 180f;
                PlaceFitted(WallPath, new Vector3(-5f, 0f, z), yRot, parent);
                PlaceFitted(WallDoorPath, new Vector3(0f, 0f, z), yRot, parent);
                PlaceFitted(WallPath, new Vector3(5f, 0f, z), yRot, parent);
            }
        }

        // ==================== HABILLAGE ====================

        private static void DressRoom(Room room, Transform parent, System.Random rng)
        {
            if (room.CastleTheme)
            {
                // L'aile ruines : pierres anciennes que La Direction n'a pas fini d'effacer.
                PlaceFitted(CastlePillarPath, RandomIn(room, rng), Angle(rng), parent);
                PlaceFitted(CastlePillarPath, RandomIn(room, rng), Angle(rng), parent);
                PlaceFitted(CastleWallPath, RandomIn(room, rng), Angle(rng), parent);
                PlaceFitted(ChestPath, RandomIn(room, rng), Angle(rng), parent);
                return;
            }

            switch (room.ZoneId)
            {
                case 2: // Aile Est : colonnes propres, la façade corporate.
                    PlaceFitted(ColumnPath, new Vector3(12f, 0f, -4f), 0f, parent);
                    PlaceFitted(ColumnPath, new Vector3(12f, 0f, 4f), 0f, parent);
                    PlaceFitted(ColumnPath, new Vector3(18f, 0f, -4f), 0f, parent);
                    PlaceFitted(ColumnPath, new Vector3(18f, 0f, 4f), 0f, parent);
                    break;
                case 3: // Entrepôt Sud : le bazar logistique, des cachettes partout.
                    for (int i = 0; i < 5; i++)
                        PlaceFitted(rng.Next(2) == 0 ? BarrelPath : SackPath, RandomIn(room, rng), Angle(rng), parent);
                    PlaceFitted(CratePath, RandomIn(room, rng), Angle(rng), parent);
                    PlaceFitted(CratePath, RandomIn(room, rng), Angle(rng), parent);
                    break;
            }
        }

        private static Vector3 RandomIn(Room room, System.Random rng)
        {
            float x = room.Area.center.x + ((float)rng.NextDouble() - 0.5f) * room.Area.width * 0.6f;
            float z = room.Area.center.y + ((float)rng.NextDouble() - 0.5f) * room.Area.height * 0.6f;
            return new Vector3(x, 0f, z);
        }

        private static float Angle(System.Random rng) => rng.Next(0, 8) * 45f;

        private static void PlaceZoneLight(MapZone zone, Vector3 floorPosition, Color color)
        {
            var lantern = PlaceFitted(LanternPath, floorPosition, 0f, zone.transform);
            var lightGo = new GameObject("ZoneLight");
            lightGo.transform.SetParent(zone.transform);
            lightGo.transform.position = floorPosition + new Vector3(0f, 2.6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2.4f;
            light.range = 14f;
            if (lantern != null)
                lantern.transform.position = floorPosition + new Vector3(0f, 0.02f, 0f);
        }

        private static void SetupLightingAmbiance()
        {
            // Nuit industrielle : les zones ne vivent que par leurs lampes —
            // condition du Blackout par aile.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.09f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.04f);
            RenderSettings.fogDensity = 0.028f;

            var sun = Object.FindAnyObjectByType<Light>();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.12f; // clair de lune résiduel
                    light.color = new Color(0.6f, 0.7f, 1f);
                }
            }
        }

        // ==================== POSE ADAPTATIVE ====================

        /// <summary>
        /// Pose un prefab en se calant sur ses bounds réels (les pivots Synty
        /// varient) : centre XZ sur la cible, base au sol. Ajoute un collider
        /// si le prefab n'en a pas.
        /// </summary>
        private static GameObject PlaceFitted(string assetPath, Vector3 target, float yRotation, Transform parent, bool floorTopAtZero = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[FacilityMapBuilder] Prefab introuvable : {assetPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            instance.transform.position = target;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);

                Vector3 offset = target - bounds.center;
                offset.y = floorTopAtZero ? -bounds.max.y : -bounds.min.y;
                instance.transform.position += offset;

                if (instance.GetComponentInChildren<Collider>() == null)
                {
                    // Bounds recalculés après déplacement, convertis en local.
                    var newBounds = renderers[0].bounds;
                    foreach (var r in renderers)
                        newBounds.Encapsulate(r.bounds);
                    var box = instance.AddComponent<BoxCollider>();
                    box.center = instance.transform.InverseTransformPoint(newBounds.center);
                    var localSize = instance.transform.InverseTransformVector(newBounds.size);
                    box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                }
            }
            return instance;
        }
    }
}
