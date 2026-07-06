using BadFaith.Core.Hazards;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Le Juge (docs/gdd/05-le-juge.md) : l'unique revolver de la map, une
    /// seule balle. Son vrai usage est le braquage au micro. Le tir est entendu
    /// par TOUTE la map (sans localisation), tue net, et n'est jamais déniable
    /// au Tribunal. Le barillet se vérifie en privé : le bluff survit au tir.
    /// </summary>
    public class TheJudge : NetworkBehaviour
    {
        public static TheJudge Instance { get; private set; }

        /// <summary>Les 5 spots de spawn connus (semi-aléatoire, jamais annoncé).</summary>
        private static readonly Vector3[] SpawnSpots =
        {
            new Vector3(12f, 0.6f, 12f),
            new Vector3(-13f, 0.6f, -11f),
            new Vector3(14f, 0.6f, -9f),
            new Vector3(-11f, 0.6f, 13f),
            new Vector3(3f, 0.6f, -14f),
        };

        private readonly SyncVar<bool> _loaded = new SyncVar<bool>(true);
        private readonly SyncVar<int> _holderId = new SyncVar<int>(-1);

        private AudioSource _audio;
        private AudioClip _bangClip;
        private AudioClip _clickClip;

        /// <summary>NB boîte grise : l'état du barillet est répliqué (lobbies entre amis).
        /// L'ASYMÉTRIE d'info est appliquée au niveau UI (seul le porteur, touche R).</summary>
        public bool Loaded => _loaded.Value;
        public int HolderId => _holderId.Value;

        private void Awake()
        {
            Instance = this;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _bangClip = CreateBangClip();
            _clickClip = CreateClickClip();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            transform.position = SpawnSpots[Random.Range(0, SpawnSpots.Length)];
        }

        /// <summary>Serveur : appelé par PlayerGrabber quand l'arme change de main (-1 = posée).</summary>
        public void ServerSetHolder(int ownerId) => _holderId.Value = ownerId;

        /// <summary>Serveur : rechargée et redéplacée pour la manche suivante.</summary>
        public void ServerResetRound()
        {
            _loaded.Value = true;
            _holderId.Value = -1;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            transform.position = SpawnSpots[Random.Range(0, SpawnSpots.Length)];
        }

        /// <summary>Serveur : le tir. Une balle, une vie, zéro déni.</summary>
        public void ServerShoot(int shooterId, Vector3 eyePosition, Vector3 direction)
        {
            if (!_loaded.Value)
            {
                // Chien sur chambre vide : petit clic 3D — croustillant à 2 m.
                RpcEmptyClick();
                return;
            }

            _loaded.Value = false;
            RpcBang();
            if (RoundManager.Instance != null)
                RoundManager.Instance.ServerAnnounce("UN COUP DE FEU A ÉTÉ TIRÉ.", 5f);

            // RaycastAll : on ignore l'arme elle-même (portée devant la caméra) et le tireur.
            int victimId = -1;
            var hits = Physics.RaycastAll(eyePosition, direction, 100f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<TheJudge>() == this)
                    continue;
                var player = hit.collider.GetComponentInParent<PlayerHealth>();
                if (player != null && player.OwnerId == shooterId)
                    continue;

                if (player != null && !player.IsDead)
                {
                    player.ServerKill();
                    victimId = player.OwnerId;
                }
                break; // premier obstacle légitime : joueur ou mur, la balle s'arrête.
            }

            // Au Tribunal, le tir est l'acte le moins déniable du jeu : loggé
            // comme événement d'auteur connu, toujours sélectionné en priorité.
            if (PacteNetworkService.Instance != null)
            {
                PacteNetworkService.Instance.ServerLogEvent(new HazardEvent
                {
                    Type = HazardType.GunShot,
                    GameTime = Time.time,
                    ZoneId = -1,
                    TargetPlayerId = victimId,
                    DurationSeconds = 0f,
                    Origin = HazardOrigin.Pacte,
                    AuthorPlayerId = shooterId,
                    SourcePacteId = -1,
                });
            }
        }

        [ObserversRpc]
        private void RpcBang()
        {
            // 2D, plein volume : toute la map l'entend, personne ne sait d'où.
            _audio.spatialBlend = 0f;
            _audio.PlayOneShot(_bangClip, 1f);
        }

        [ObserversRpc]
        private void RpcEmptyClick()
        {
            // 3D discret : seuls les proches entendent le clic de la chambre vide.
            _audio.spatialBlend = 1f;
            _audio.minDistance = 0.5f;
            _audio.maxDistance = 4f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.PlayOneShot(_clickClip, 0.7f);
        }

        private static AudioClip CreateBangClip()
        {
            const int rate = 44100;
            const float duration = 0.45f;
            int samples = (int)(rate * duration);
            var data = new float[samples];
            var rng = new System.Random(7);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-6f * t);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * envelope;
            }
            var clip = AudioClip.Create("JudgeBang", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateClickClip()
        {
            const int rate = 44100;
            int samples = (int)(rate * 0.06f);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-30f * t) * 0.8f;
            }
            var clip = AudioClip.Create("JudgeClick", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
