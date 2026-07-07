using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Récepteur des AnimationEvents des clips Starter Assets (OnFootstep,
    /// OnLand) : joue les sons de pas en 3D spatial. Purement local — chaque
    /// client anime les personnages, donc chaque client entend les pas des
    /// autres s'ils sont assez proches. S'approcher en courant s'entend.
    /// </summary>
    public class CharacterAudioReceiver : MonoBehaviour
    {
        private AudioClip[] _footstepClips;
        private AudioClip _landClip;
        private AudioSource _source;

        public void Initialize(AudioClip[] footstepClips, AudioClip landClip)
        {
            _footstepClips = footstepClips;
            _landClip = landClip;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 1f;
            _source.minDistance = 1f;
            _source.maxDistance = 14f;
            _source.rolloffMode = AudioRolloffMode.Linear;
        }

        // Signatures attendues par les AnimationEvents des clips Starter Assets.
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (_source == null || _footstepClips == null || _footstepClips.Length == 0)
                return;
            if (animationEvent.animatorClipInfo.weight < 0.5f)
                return; // pendant les blends, seule l'anim dominante fait du bruit
            var clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            _source.PlayOneShot(clip, 0.35f);
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (_source == null || _landClip == null)
                return;
            if (animationEvent.animatorClipInfo.weight < 0.5f)
                return;
            _source.PlayOneShot(_landClip, 0.5f);
        }
    }
}
