using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BadFaith.Menu
{
    /// <summary>
    /// L'intro de lancement : « AP Studio » s'écrit à la main sur fond noir,
    /// une punchline tombe, puis le menu apparaît. N'importe quelle touche
    /// passe l'intro (on respecte le temps des gens — pas celui de La Direction).
    /// </summary>
    public class BootIntro : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _bootGroup;
        [SerializeField] private Text _studioText;
        [SerializeField] private Text _taglineText;
        [SerializeField] private CanvasGroup _menuGroup;

        private const string StudioName = "AP Studio";
        private const string Tagline = "des jeux faits avec une mauvaise foi certaine";
        private bool _skipped;
        private AudioSource _audio;

        private void Start()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _audio.clip = CreateJingle();
            StartCoroutine(PlayIntro());
        }

        /// <summary>
        /// Le sting du studio, généré par code : arpège de ré majeur qui monte,
        /// doux et corporate — le calme avant la mauvaise foi.
        /// </summary>
        private static AudioClip CreateJingle()
        {
            const int rate = 44100;
            const float duration = 5.5f;
            int samples = (int)(rate * duration);
            var data = new float[samples];

            // (fréquence Hz, départ s, tenue s, volume)
            var notes = new (float f, float start, float sustain, float vol)[]
            {
                (293.66f, 0.00f, 1.6f, 0.30f), // D4
                (440.00f, 0.55f, 1.6f, 0.28f), // A4
                (587.33f, 1.10f, 1.8f, 0.30f), // D5
                (739.99f, 1.65f, 2.0f, 0.26f), // F#5
                // L'accord final, tenu.
                (587.33f, 2.60f, 2.6f, 0.20f),
                (739.99f, 2.60f, 2.6f, 0.16f),
                (880.00f, 2.60f, 2.6f, 0.14f), // A5
            };

            foreach (var note in notes)
            {
                int start = (int)(note.start * rate);
                int length = (int)(note.sustain * rate);
                for (int i = 0; i < length && start + i < samples; i++)
                {
                    float t = (float)i / rate;
                    float attack = Mathf.Clamp01(t / 0.04f);
                    float decay = Mathf.Exp(-2.2f * t / note.sustain);
                    float envelope = attack * decay * note.vol;
                    float phase = 2f * Mathf.PI * note.f * t;
                    // Fondamentale + un peu d'octave : timbre cloche douce.
                    data[start + i] += (Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2f)) * envelope;
                }
            }

            var clip = AudioClip.Create("StudioJingle", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                _skipped = true;
        }

        private IEnumerator PlayIntro()
        {
            _bootGroup.alpha = 1f;
            _menuGroup.alpha = 0f;
            _menuGroup.interactable = false;
            _menuGroup.blocksRaycasts = false;
            _studioText.text = string.Empty;
            _taglineText.text = string.Empty;

            yield return WaitOrSkip(1.2f);
            _audio.Play();

            // Écriture manuscrite lettre à lettre, avec le petit tremblement du stylo.
            for (int i = 0; i <= StudioName.Length && !_skipped; i++)
            {
                _studioText.text = StudioName.Substring(0, i);
                _studioText.transform.localRotation = Quaternion.Euler(0f, 0f, -3.5f + Random.Range(-0.6f, 0.6f));
                yield return WaitOrSkip(Random.Range(0.2f, 0.38f));
            }
            _studioText.text = StudioName;

            yield return WaitOrSkip(1.0f);
            _taglineText.text = Tagline;
            var taglineColor = _taglineText.color;
            for (float t = 0f; t < 1f && !_skipped; t += Time.deltaTime * 0.8f)
            {
                _taglineText.color = new Color(taglineColor.r, taglineColor.g, taglineColor.b, t);
                yield return null;
            }
            _taglineText.color = new Color(taglineColor.r, taglineColor.g, taglineColor.b, 1f);

            yield return WaitOrSkip(2.8f);

            // Fondu vers le menu.
            for (float t = 1f; t > 0f; t -= Time.deltaTime * 1.2f)
            {
                _bootGroup.alpha = t;
                _menuGroup.alpha = 1f - t;
                yield return null;
            }
            _bootGroup.alpha = 0f;
            _bootGroup.blocksRaycasts = false;
            _menuGroup.alpha = 1f;
            _menuGroup.interactable = true;
            _menuGroup.blocksRaycasts = true;
        }

        private IEnumerator WaitOrSkip(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end && !_skipped)
                yield return null;
        }
    }
}
