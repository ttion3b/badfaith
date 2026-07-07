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

        private void Start()
        {
            StartCoroutine(PlayIntro());
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

            yield return WaitOrSkip(0.7f);

            // Écriture manuscrite lettre à lettre, avec le petit tremblement du stylo.
            for (int i = 0; i <= StudioName.Length && !_skipped; i++)
            {
                _studioText.text = StudioName.Substring(0, i);
                _studioText.transform.localRotation = Quaternion.Euler(0f, 0f, -3.5f + Random.Range(-0.6f, 0.6f));
                yield return WaitOrSkip(Random.Range(0.09f, 0.22f));
            }
            _studioText.text = StudioName;

            yield return WaitOrSkip(0.5f);
            _taglineText.text = Tagline;
            var taglineColor = _taglineText.color;
            for (float t = 0f; t < 1f && !_skipped; t += Time.deltaTime * 2f)
            {
                _taglineText.color = new Color(taglineColor.r, taglineColor.g, taglineColor.b, t);
                yield return null;
            }
            _taglineText.color = new Color(taglineColor.r, taglineColor.g, taglineColor.b, 1f);

            yield return WaitOrSkip(1.6f);

            // Fondu vers le menu.
            for (float t = 1f; t > 0f; t -= Time.deltaTime * 2.5f)
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
