using System.Collections;
using BadFaith.Core.Hazards;
using FishNet.Object;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Exécute les HazardEvent côté clients. RÈGLE D'INDISTINGUABILITÉ
    /// (docs/gdd/04-deni-plausible.md) : ce code ne sait pas si l'événement
    /// vient d'un Pacte ou d'un accident naturel — même RPC, mêmes effets,
    /// mêmes timings dans les deux cas.
    /// </summary>
    public class HazardExecutor : NetworkBehaviour
    {
        private const float GasDamagePerSecond = 15f;

        /// <summary>Zones de gaz actives, côté serveur, pour les dégâts.</summary>
        private readonly System.Collections.Generic.List<(Vector3 pos, float radius, float until)> _serverGasZones = new System.Collections.Generic.List<(Vector3, float, float)>();

        /// <summary>Serveur : point d'entrée unique, Pacte ou naturel.</summary>
        public void ServerExecute(HazardEvent evt)
        {
            switch (evt.Type)
            {
                case HazardType.Blackout:
                    RpcBlackout(evt.ZoneId, evt.DurationSeconds);
                    break;
                case HazardType.GasLeak:
                    var zone = MapZone.Get(evt.ZoneId);
                    Vector3 pos = zone != null
                        ? zone.RandomPointInside(new System.Random(), shrink: 0.6f, y: 2f)
                        : new Vector3(Random.Range(-14f, 14f), 2.5f, Random.Range(-14f, 14f));
                    _serverGasZones.Add((pos, 5f, Time.time + evt.DurationSeconds));
                    RpcGasLeak(pos, 5f, evt.DurationSeconds);
                    break;
                // Porte grippée, brouillage, panne du Terminal :
                // pas encore de support — no-op silencieux.
            }
        }

        private void Update()
        {
            if (!IsServerInitialized || _serverGasZones.Count == 0)
                return;

            _serverGasZones.RemoveAll(z => Time.time > z.until);
            if (_serverGasZones.Count == 0)
                return;

            int damage = Mathf.CeilToInt(GasDamagePerSecond * Time.deltaTime);
            foreach (var health in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (health.IsDead)
                    continue;
                foreach (var zone in _serverGasZones)
                {
                    if (Vector3.Distance(health.transform.position, zone.pos) <= zone.radius)
                    {
                        health.ServerDamage(damage);
                        break;
                    }
                }
            }
        }

        [ObserversRpc]
        private void RpcBlackout(int zoneId, float duration)
        {
            StartCoroutine(BlackoutRoutine(zoneId, duration));
        }

        private IEnumerator BlackoutRoutine(int zoneId, float duration)
        {
            // Le Blackout éteint les lampes d'UNE zone (docs/gdd/03-pactes.md) —
            // même code qu'il soit naturel ou de Pacte.
            var zone = MapZone.Get(zoneId);
            if (zone == null)
                yield break;

            zone.SetLights(false);
            yield return new WaitForSeconds(duration);
            zone.SetLights(true);
        }

        [ObserversRpc]
        private void RpcGasLeak(Vector3 position, float radius, float duration)
        {
            StartCoroutine(GasRoutine(position, radius, duration));
        }

        private IEnumerator GasRoutine(Vector3 position, float radius, float duration)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "FuiteDeGaz";
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius * 2f;
            var renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = new Color(0.35f, 0.75f, 0.25f, 1f);
            yield return new WaitForSeconds(duration);
            Object.Destroy(sphere);
        }
    }
}
