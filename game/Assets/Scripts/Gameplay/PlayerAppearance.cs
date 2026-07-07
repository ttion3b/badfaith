using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// L'apparence du joueur : une des 15 silhouettes Synty (l'identité visuelle
    /// qui rend les accusations possibles : « c'est le VIKING ! »), les
    /// animations de locomotion, la montre au poignet gauche, et les poses
    /// procédurales publiques : lever le bras pour consulter la montre,
    /// braquer avec le Juge.
    /// </summary>
    public class PlayerAppearance : NetworkBehaviour
    {
        [SerializeField] private GameObject[] _characterVariants;
        [SerializeField] private RuntimeAnimatorController _animatorController;

        private readonly SyncVar<int> _variantIndex = new SyncVar<int>();

        private Animator _animator;
        private PlayerWatch _watch;
        private PlayerHealth _health;
        private Vector3 _lastPosition;
        private float _smoothedSpeed;
        private float _consultBlend;
        private float _aimBlend;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _variantIndex.Value = _characterVariants.Length > 0 ? Mathf.Abs(OwnerId) % _characterVariants.Length : 0;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _watch = GetComponent<PlayerWatch>();
            _health = GetComponent<PlayerHealth>();
            BuildVisual();
        }

        private void BuildVisual()
        {
            if (_characterVariants == null || _characterVariants.Length == 0)
                return;
            var prefab = _characterVariants[Mathf.Clamp(_variantIndex.Value, 0, _characterVariants.Length - 1)];
            if (prefab == null)
                return;

            var visual = Instantiate(prefab, transform);
            visual.name = "CharacterVisual";
            // Le CharacterController fait 2 m centré sur le pivot : les pieds sont à -1.
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;

            _animator = visual.GetComponentInChildren<Animator>();
            if (_animator == null)
                _animator = visual.AddComponent<Animator>();
            _animator.runtimeAnimatorController = _animatorController;
            _animator.applyRootMotion = false;

            // En vue première personne, on ne voit pas son propre corps —
            // mais on garde son ombre (et les autres nous voient, eux).
            if (IsOwner)
            {
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            // La montre, physiquement au poignet gauche.
            var hand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (hand != null)
            {
                var watchCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                watchCube.name = "WristWatch";
                Destroy(watchCube.GetComponent<BoxCollider>());
                watchCube.transform.SetParent(hand, false);
                watchCube.transform.localPosition = new Vector3(0f, 0.04f, 0.01f);
                watchCube.transform.localScale = new Vector3(0.055f, 0.025f, 0.055f);
                if (_watch != null)
                    _watch.SetWristIndicator(watchCube.GetComponent<Renderer>());
            }
        }

        private void Update()
        {
            if (_animator == null)
                return;

            // Vitesse horizontale mesurée sur le transform : identique pour le
            // propriétaire et les copies distantes (interpolées par le réseau).
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            delta.y = 0f;
            float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, speed, 10f * Time.deltaTime);
            _animator.SetFloat("Speed", _smoothedSpeed);
        }

        private void LateUpdate()
        {
            if (_animator == null || (_health != null && _health.IsDead))
                return;

            // Consultation de la montre : le bras gauche se lève — GESTE PUBLIC,
            // c'est le tell social du GDD, désormais en langage corporel.
            bool consulting = _watch != null && _watch.Consulting;
            _consultBlend = Mathf.MoveTowards(_consultBlend, consulting ? 1f : 0f, Time.deltaTime * 5f);
            if (_consultBlend > 0.001f)
            {
                ApplyBoneOffset(HumanBodyBones.LeftUpperArm, new Vector3(-35f, -25f, -20f), _consultBlend);
                ApplyBoneOffset(HumanBodyBones.LeftLowerArm, new Vector3(-65f, -20f, 0f), _consultBlend);
                ApplyBoneOffset(HumanBodyBones.Head, new Vector3(20f, -15f, 0f), _consultBlend);
            }

            // Le Juge en main : bras droit tendu. La menace est un choix public.
            bool aiming = TheJudge.Instance != null && TheJudge.Instance.HolderId == OwnerId;
            _aimBlend = Mathf.MoveTowards(_aimBlend, aiming ? 1f : 0f, Time.deltaTime * 5f);
            if (_aimBlend > 0.001f)
            {
                ApplyBoneOffset(HumanBodyBones.RightUpperArm, new Vector3(-70f, 0f, 0f), _aimBlend);
                ApplyBoneOffset(HumanBodyBones.RightLowerArm, new Vector3(-15f, 0f, 0f), _aimBlend);
            }
        }

        private void ApplyBoneOffset(HumanBodyBones bone, Vector3 eulerOffset, float weight)
        {
            var t = _animator.GetBoneTransform(bone);
            if (t != null)
                t.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(eulerOffset), weight);
        }
    }
}
