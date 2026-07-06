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

        private const string City = "Assets/SyntyStudios/PolygonCity/Prefabs/";

        private const string FloorPath = Proto + "SM_Buildings_Floor_5x5_01P.prefab";
        private const string WallPath = Proto + "SM_Buildings_Wall_5x3_01P.prefab";
        private const string WallDoorPath = Proto + "SM_Buildings_WallDoor_5x3_01P.prefab";
        private const string WallShortPath = Proto + "SM_Buildings_Wall_2x3_01P.prefab";
        private const string ColumnPath = Proto + "SM_Buildings_Column_1x3_01P.prefab";
        private const string CastleWallPath = Knights + "Buildings/SM_Bld_Castle_Wall_01.prefab";
        private const string CastlePillarPath = Knights + "Buildings/SM_Bld_Castle_Pillar_01.prefab";
        private const string LanternPath = Adv + "Items/SM_Item_Lantern_01.prefab";
        private const string CratePath = Adv + "Props/SM_Prop_Crate_01.prefab";
        private const string BarrelPath = Adv + "Props/SM_Prop_Barrel_01.prefab";
        private const string SackPath = Adv + "Props/SM_Prop_Sack_01.prefab";
        private const string ChestPath = Adv + "Props/SM_Prop_Chest_01.prefab";
        private const string FirePath = Adv + "FX/FX_Fire.prefab";
        private const string BrazierPath = Knights + "Props/SM_Prop_Brazier_01.prefab";
        private const string BannerPath = Knights + "Props/SM_Prop_Banner_01.prefab";
        private const string GravestonePath = Knights + "Props/SM_Prop_Gravestone_01.prefab";
        private const string GuillotinePath = Knights + "Props/SM_Prop_Guillotine_01.prefab";
        private const string StatuePath = Knights + "Props/SM_Prop_Statue_01.prefab";
        private const string PlinthPath = Knights + "Props/SM_Prop_Plinth_01.prefab";
        private const string RockPilePath = Knights + "Environments/SM_Env_RockPile_01.prefab";
        private const string LampostPath = Knights + "Props/SM_Prop_Lampost_01.prefab";
        private const string BeamPath = Knights + "Props/SM_Prop_Beam_01.prefab";

        private const string DeskPath = City + "Props/SM_Prop_ShopInterior_Desk_01.prefab";
        private const string ShelfPath = City + "Props/SM_Prop_ShopInterior_Shelf_01.prefab";
        private const string ChairPath = City + "Props/SM_Prop_ShopInterior_Chair_01.prefab";
        private const string TablePath = City + "Props/SM_Prop_ShopInterior_Table_01.prefab";
        private const string CouchPath = City + "Props/SM_Prop_Couch_01.prefab";
        private const string PotPlantPath = City + "Props/SM_Prop_PotPlant_01.prefab";
        private const string SecurityCamPath = City + "Props/SM_Prop_SecurityCamera_01.prefab";
        private const string PosterFramePath = City + "Props/SM_Prop_Poster_Frame_01.prefab";
        private const string AtmPath = City + "Props/SM_Prop_ATM_01.prefab";
        private const string PowerBoxPath = City + "Props/SM_Prop_PowerBox_01.prefab";
        private const string PalletPath = City + "Props/SM_Prop_Pallet_01.prefab";
        private const string CardboardPath = City + "Props/SM_Prop_CardboardBox_01.prefab";
        private const string SkipPath = City + "Props/SM_Prop_Skip_01.prefab";
        private const string BarrierPath = City + "Props/SM_Prop_Barrier_01.prefab";
        private const string ConePath = City + "Props/SM_Prop_Cone_01.prefab";
        private const string TrashBagPath = City + "Props/SM_Prop_TrashBag_01.prefab";
        private const string PaperPath = City + "Props/SM_Prop_Paper_01.prefab";
        private const string SignWarningPath = City + "Props/SM_Prop_Sign_Warning_01.prefab";
        private const string PipePresetPath = City + "Props/SM_Prop_Pipe_Preset_01.prefab";

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

        /// <summary>Position normalisée dans la salle : nx/nz dans [-1..1] (bords à ±1).</summary>
        private static Vector3 At(Room room, float nx, float nz)
            => new Vector3(room.Area.center.x + nx * room.Area.width * 0.5f, 0f, room.Area.center.y + nz * room.Area.height * 0.5f);

        private static void DressRoom(Room room, Transform parent, System.Random rng)
        {
            switch (room.ZoneId)
            {
                case 0: DressHub(room, parent, rng); break;
                case 1: DressRuins(room, parent, rng); break;
                case 2: DressOffices(room, parent, rng); break;
                case 3: DressWarehouse(room, parent, rng); break;
                case 4: DressDock(room, parent, rng); break;
            }
        }

        /// <summary>Hub : l'accueil corporate. Propre, surveillé, faussement accueillant.</summary>
        private static void DressHub(Room room, Transform parent, System.Random rng)
        {
            // La statue que La Direction s'est offerte, sur son socle, face à l'entrée.
            PlaceFitted(PlinthPath, At(room, 0.55f, 0.55f), 0f, parent);
            PlaceFitted(StatuePath, At(room, 0.55f, 0.55f), 200f, parent, yOverride: 1.1f);

            PlaceFitted(CouchPath, At(room, -0.6f, 0.65f), 160f, parent);
            PlaceFitted(PotPlantPath, At(room, -0.78f, 0.78f), 0f, parent);
            PlaceFitted(PotPlantPath, At(room, 0.78f, -0.78f), 0f, parent);
            PlaceFitted(DeskPath, At(room, -0.6f, -0.6f), 45f, parent);
            PlaceFitted(ChairPath, At(room, -0.68f, -0.68f), 225f, parent);
            PlaceFitted(AtmPath, At(room, 0.85f, 0.1f), -90f, parent);

            // Les caméras de La Direction, une par passage — le jeu vous regarde.
            foreach (var (nx, nz, rot) in new[] { (0.15f, 0.92f, 180f), (0.92f, 0.15f, -90f), (0.15f, -0.92f, 0f), (-0.92f, 0.15f, 90f) })
                PlaceFitted(SecurityCamPath, At(room, nx, nz), rot, parent, yOverride: 2.7f);

            // Affiches corporate encadrées.
            PlaceFitted(PosterFramePath, At(room, -0.4f, 0.96f), 180f, parent, yOverride: 1.7f);
            PlaceFitted(PosterFramePath, At(room, 0.96f, -0.4f), -90f, parent, yOverride: 1.7f);

            ScatterSmall(room, parent, rng, PaperPath, 4);
            PlaceFitted(SignWarningPath, At(room, 0.3f, -0.85f), 20f, parent);
        }

        /// <summary>Ruines : ce que La Direction a enterré — et qui dépasse encore.</summary>
        private static void DressRuins(Room room, Transform parent, System.Random rng)
        {
            // La guillotine. Personne ne pose de questions.
            PlaceFitted(GuillotinePath, At(room, -0.45f, 0.35f), 150f, parent);

            PlaceFitted(CastlePillarPath, At(room, -0.65f, -0.55f), 0f, parent);
            PlaceFitted(CastlePillarPath, At(room, 0.35f, 0.65f), 0f, parent);
            PlaceFitted(CastlePillarPath, At(room, -0.15f, -0.15f), 0f, parent);
            PlaceFitted(CastleWallPath, At(room, -0.55f, 0.75f), 15f, parent);
            PlaceFitted(CastleWallPath, At(room, 0.6f, -0.65f), 100f, parent);

            for (int i = 0; i < 3; i++)
                PlaceFitted(GravestonePath, RandomIn(room, rng), Angle(rng), parent);
            for (int i = 0; i < 3; i++)
                PlaceFitted(RockPilePath, RandomIn(room, rng), Angle(rng), parent);
            PlaceFitted(BeamPath, RandomIn(room, rng), Angle(rng), parent);
            PlaceFitted(ChestPath, At(room, 0.7f, 0.7f), 220f, parent);

            // Bannières délavées sur le mur du fond.
            PlaceFitted(BannerPath, At(room, -0.97f, -0.3f), 90f, parent, yOverride: 2.2f);
            PlaceFitted(BannerPath, At(room, -0.97f, 0.3f), 90f, parent, yOverride: 2.2f);
        }

        /// <summary>Aile Est : les bureaux. Open-space figé en pleine évacuation.</summary>
        private static void DressOffices(Room room, Transform parent, System.Random rng)
        {
            // Deux rangées de bureaux.
            foreach (float nz in new[] { -0.45f, 0.15f })
                for (int i = 0; i < 3; i++)
                {
                    float nx = -0.5f + i * 0.45f;
                    PlaceFitted(DeskPath, At(room, nx, nz), 0f, parent);
                    PlaceFitted(ChairPath, At(room, nx, nz - 0.12f), rng.Next(-30, 210), parent);
                }

            PlaceFitted(ShelfPath, At(room, 0.9f, 0.5f), -90f, parent);
            PlaceFitted(ShelfPath, At(room, 0.9f, 0.75f), -90f, parent);
            PlaceFitted(TablePath, At(room, -0.6f, 0.7f), 30f, parent);
            PlaceFitted(PotPlantPath, At(room, -0.85f, -0.85f), 0f, parent);
            PlaceFitted(PowerBoxPath, At(room, 0.55f, -0.9f), 180f, parent);
            PlaceFitted(PipePresetPath, At(room, -0.2f, 0.93f), 180f, parent);
            PlaceFitted(SecurityCamPath, At(room, -0.9f, 0.9f), 135f, parent, yOverride: 2.7f);
            PlaceFitted(PosterFramePath, At(room, 0.2f, -0.96f), 0f, parent, yOverride: 1.7f);

            ScatterSmall(room, parent, rng, PaperPath, 6);
            PlaceFitted(ConePath, RandomIn(room, rng), Angle(rng), parent);
        }

        /// <summary>Entrepôt Sud : la logistique. Rangées, cachettes, angles morts.</summary>
        private static void DressWarehouse(Room room, Transform parent, System.Random rng)
        {
            // Deux rangées d'étagères de stockage — les allées cachent les dépôts furtifs.
            foreach (float nx in new[] { -0.4f, 0.3f })
                for (int i = 0; i < 3; i++)
                    PlaceFitted(ShelfPath, At(room, nx, -0.55f + i * 0.45f), 90f, parent);

            // Piles de palettes et bennes.
            PlaceFitted(SkipPath, At(room, 0.75f, -0.75f), 70f, parent);
            for (int i = 0; i < 3; i++)
                PlaceFitted(PalletPath, At(room, -0.75f + i * 0.12f, 0.7f), Angle(rng), parent);

            for (int i = 0; i < 4; i++)
                PlaceFitted(CardboardPath, RandomIn(room, rng), Angle(rng), parent);
            for (int i = 0; i < 4; i++)
                PlaceFitted(rng.Next(2) == 0 ? BarrelPath : SackPath, RandomIn(room, rng), Angle(rng), parent);
            PlaceFitted(CratePath, RandomIn(room, rng), Angle(rng), parent);
            PlaceFitted(TrashBagPath, RandomIn(room, rng), Angle(rng), parent);
            PlaceFitted(SecurityCamPath, At(room, 0.9f, 0.9f), -135f, parent, yOverride: 2.7f);
        }

        /// <summary>Quai Capsule : zone d'embarquement balisée — l'endroit où l'on se trahit en dernier.</summary>
        private static void DressDock(Room room, Transform parent, System.Random rng)
        {
            // Couloir balisé vers la capsule.
            foreach (float nx in new[] { -0.35f, 0.35f })
                for (int i = 0; i < 3; i++)
                    PlaceFitted(BarrierPath, At(room, nx, -0.7f + i * 0.5f), 90f, parent);

            PlaceFitted(SignWarningPath, At(room, -0.55f, -0.3f), -30f, parent);
            PlaceFitted(ConePath, At(room, 0.5f, -0.5f), 0f, parent);
            PlaceFitted(ConePath, At(room, -0.55f, 0.2f), 0f, parent);
            PlaceFitted(PowerBoxPath, At(room, 0.9f, 0.3f), -90f, parent);
            PlaceFitted(PipePresetPath, At(room, -0.9f, 0.5f), 90f, parent);
            PlaceFitted(SecurityCamPath, At(room, 0.1f, 0.92f), 180f, parent, yOverride: 2.7f);
            PlaceFitted(CardboardPath, At(room, 0.7f, -0.8f), Angle(rng), parent);
        }

        private static void ScatterSmall(Room room, Transform parent, System.Random rng, string path, int count)
        {
            for (int i = 0; i < count; i++)
                PlaceFitted(path, RandomIn(room, rng), Angle(rng), parent, addCollider: false);
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
            bool ruins = zone.ZoneId == 1;
            if (ruins)
            {
                // Braseros enflammés : la seule lumière que les ruines tolèrent.
                PlaceFitted(BrazierPath, floorPosition, 0f, zone.transform);
                var fire = PlaceFitted(FirePath, floorPosition, 0f, zone.transform, yOverride: 1.05f, addCollider: false);
                if (fire != null)
                    fire.transform.localScale = Vector3.one * 0.6f;
            }
            else
            {
                PlaceFitted(LampostPath, floorPosition, 0f, zone.transform);
                PlaceFitted(LanternPath, floorPosition + new Vector3(0.6f, 0f, 0.3f), 0f, zone.transform);
            }

            var lightGo = new GameObject("ZoneLight");
            lightGo.transform.SetParent(zone.transform);
            lightGo.transform.position = floorPosition + new Vector3(0f, ruins ? 1.4f : 2.8f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = ruins ? 2.8f : 2.4f;
            light.range = ruins ? 12f : 15f;
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
        /// varient) : centre XZ sur la cible, base au sol — ou centre à
        /// yOverride pour les objets muraux (caméras, affiches, bannières).
        /// Ajoute un collider si le prefab n'en a pas.
        /// </summary>
        private static GameObject PlaceFitted(string assetPath, Vector3 target, float yRotation, Transform parent, bool floorTopAtZero = false, float? yOverride = null, bool addCollider = true)
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
                if (yOverride.HasValue)
                    offset.y = yOverride.Value - bounds.center.y;
                else
                    offset.y = floorTopAtZero ? -bounds.max.y : -bounds.min.y;
                instance.transform.position += offset;

                if (addCollider && instance.GetComponentInChildren<Collider>() == null)
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
