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

        private Light _sun;
        private float _sunIntensity;
        private Color _ambient;
        private Coroutine _blackout;
        /// <summary>Zones de gaz actives, côté serveur, pour les dégâts.</summary>
        private readonly System.Collections.Generic.List<(Vector3 pos, float radius, float until)> _serverGasZones = new System.Collections.Generic.List<(Vector3, float, float)>();

        /// <summary>Serveur : point d'entrée unique, Pacte ou naturel.</summary>
        public void ServerExecute(HazardEvent evt)
        {
            switch (evt.Type)
            {
                case HazardType.Blackout:
                    RpcBlackout(evt.DurationSeconds);
                    break;
                case HazardType.GasLeak:
                    Vector3 pos = new Vector3(Random.Range(-14f, 14f), 2.5f, Random.Range(-14f, 14f));
                    _serverGasZones.Add((pos, 5f, Time.time + evt.DurationSeconds));
                    RpcGasLeak(pos, 5f, evt.DurationSeconds);
                    break;
                // Porte grippée, brouillage, sol électrifié, panne du Terminal :
                // pas encore de support dans la boîte grise — no-op silencieux.
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
        private void RpcBlackout(float duration)
        {
            if (_blackout != null)
                StopCoroutine(_blackout);
            _blackout = StartCoroutine(BlackoutRoutine(duration));
        }

        private IEnumerator BlackoutRoutine(float duration)
        {
            if (_sun == null)
            {
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional)
                    {
                        _sun = light;
                        _sunIntensity = light.intensity;
                        _ambient = RenderSettings.ambientLight;
                        break;
                    }
                }
            }
            if (_sun == null)
                yield break;

            _sun.intensity = 0.02f;
            RenderSettings.ambientLight = new Color(0.01f, 0.01f, 0.02f);
            yield return new WaitForSeconds(duration);
            _sun.intensity = _sunIntensity;
            RenderSettings.ambientLight = _ambient;
            _blackout = null;
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
