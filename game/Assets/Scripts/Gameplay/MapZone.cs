using System.Collections.Generic;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Une zone de la map (salle/aile). Porte ses lumières (enfants) : le
    /// Blackout éteint UNE zone, le gaz remplit UNE salle. Données de scène
    /// pures, identiques chez tous — pas de réseau nécessaire.
    /// </summary>
    public class MapZone : MonoBehaviour
    {
        public int ZoneId;
        public string ZoneName;
        public Vector3 AreaCenter;
        public Vector3 AreaSize;

        private static readonly List<MapZone> Registry = new List<MapZone>();
        public static IReadOnlyList<MapZone> All => Registry;

        private Light[] _lights;

        private void OnEnable()
        {
            Registry.Add(this);
            _lights = GetComponentsInChildren<Light>(true);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }

        public static MapZone Get(int zoneId)
        {
            foreach (var zone in Registry)
                if (zone.ZoneId == zoneId)
                    return zone;
            return null;
        }

        /// <summary>Zone contenant cette position (0 par défaut : le hub).</summary>
        public static int ZoneIdAt(Vector3 position)
        {
            foreach (var zone in Registry)
            {
                var bounds = new Bounds(zone.AreaCenter, zone.AreaSize);
                if (bounds.Contains(new Vector3(position.x, zone.AreaCenter.y, position.z)))
                    return zone.ZoneId;
            }
            return 0;
        }

        public void SetLights(bool enabled)
        {
            foreach (var light in _lights)
                if (light != null)
                    light.enabled = enabled;
        }

        public Vector3 RandomPointInside(System.Random rng, float shrink = 0.7f, float y = 1f)
        {
            float x = AreaCenter.x + ((float)rng.NextDouble() - 0.5f) * AreaSize.x * shrink;
            float z = AreaCenter.z + ((float)rng.NextDouble() - 0.5f) * AreaSize.z * shrink;
            return new Vector3(x, y, z);
        }
    }
}
