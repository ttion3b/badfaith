using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BadFaith.Menu
{
    /// <summary>
    /// Le menu principal : pseudo, héberger, rejoindre, quitter.
    /// Charge la scène de jeu avec l'intention réseau — NetworkLauncherHUD
    /// démarre la connexion tout seul à l'arrivée.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private InputField _pseudoField;
        [SerializeField] private InputField _addressField;

        private const string GameSceneName = "J0_GreyBox";

        private void Start()
        {
            if (_pseudoField != null)
                _pseudoField.text = PlayerPrefs.GetString("pseudo", "");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HostGame()
        {
            SavePseudo();
            MenuIntent.Mode = MenuMode.Host;
            SceneManager.LoadScene(GameSceneName);
        }

        public void JoinGame()
        {
            SavePseudo();
            MenuIntent.Mode = MenuMode.Join;
            MenuIntent.Address = _addressField != null && !string.IsNullOrWhiteSpace(_addressField.text)
                ? _addressField.text.Trim()
                : "localhost";
            SceneManager.LoadScene(GameSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SavePseudo()
        {
            string pseudo = _pseudoField != null ? _pseudoField.text.Trim() : "";
            if (string.IsNullOrEmpty(pseudo))
                pseudo = $"Agent{Random.Range(100, 999)}";
            MenuIntent.Pseudo = pseudo;
            PlayerPrefs.SetString("pseudo", pseudo);
            PlayerPrefs.Save();
        }
    }
}
