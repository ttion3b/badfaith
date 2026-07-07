using BadFaith.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BadFaith.EditorTools
{
    /// <summary>
    /// Construit la scène MainMenu : intro « AP Studio » manuscrite sur fond
    /// noir, puis le menu (pseudo, héberger, rejoindre, quitter) aux couleurs
    /// de La Direction. Place le menu en scène 0 des build settings.
    /// </summary>
    public static class MenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontPath = "Assets/Fonts/PatrickHand-Regular.ttf";
        private const string GameScenePath = "Assets/Scenes/J0_GreyBox.unity";

        private static readonly Color Ink = new Color(0.92f, 0.92f, 0.88f);
        private static readonly Color DirectionYellow = new Color(0.98f, 0.78f, 0.12f);
        private static readonly Color PanelDark = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        private static Font Handwriting => AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        [MenuItem("MAUVAISE FOI/Construire le menu principal")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Caméra noire (le canvas overlay fait tout).
            var camGo = new GameObject("MenuCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            // Canvas + EventSystem (new Input System).
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // ---- Groupe MENU (dessous, révélé après l'intro) ----
            var menuGroup = CreateGroup(canvasGo.transform, "MenuGroup");
            CreateImage(menuGroup.transform, "Fond", new Color(0.05f, 0.05f, 0.07f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateText(menuGroup.transform, "Titre", "BAD FAITH", 130, DirectionYellow,
                new Vector2(0.5f, 0.78f), rotationZ: -2.5f);
            CreateText(menuGroup.transform, "SousTitre", "La Direction vous attend.", 34, Ink,
                new Vector2(0.5f, 0.66f));

            var pseudoField = CreateInputField(menuGroup.transform, "ChampPseudo", "ton pseudo…", new Vector2(0.5f, 0.52f));
            var addressField = CreateInputField(menuGroup.transform, "ChampAdresse", "adresse de l'hôte (localhost)", new Vector2(0.5f, 0.335f));

            var controller = canvasGo.AddComponent<MainMenuController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_pseudoField").objectReferenceValue = pseudoField;
            controllerSo.FindProperty("_addressField").objectReferenceValue = addressField;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            CreateButton(menuGroup.transform, "BoutonHeberger", "HÉBERGER UNE PARTIE", new Vector2(0.5f, 0.43f),
                () => { }, controller, nameof(MainMenuController.HostGame), DirectionYellow, Color.black);
            CreateButton(menuGroup.transform, "BoutonRejoindre", "REJOINDRE", new Vector2(0.5f, 0.245f),
                () => { }, controller, nameof(MainMenuController.JoinGame), new Color(0.25f, 0.25f, 0.3f), Ink);
            CreateButton(menuGroup.transform, "BoutonQuitter", "QUITTER", new Vector2(0.5f, 0.13f),
                () => { }, controller, nameof(MainMenuController.QuitGame), new Color(0.15f, 0.12f, 0.12f), new Color(0.8f, 0.5f, 0.4f));

            CreateText(menuGroup.transform, "Version", "prototype — le Tribunal jugera", 20, new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.045f));

            // ---- Groupe BOOT (au-dessus, l'intro) ----
            var bootGroup = CreateGroup(canvasGo.transform, "BootGroup");
            CreateImage(bootGroup.transform, "Noir", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var studioText = CreateText(bootGroup.transform, "Studio", "", 130, Ink, new Vector2(0.5f, 0.55f), rotationZ: -3.5f);
            var taglineText = CreateText(bootGroup.transform, "Tagline", "", 32, new Color(0.65f, 0.65f, 0.6f), new Vector2(0.5f, 0.4f), rotationZ: -1.5f);

            var intro = canvasGo.AddComponent<BootIntro>();
            var introSo = new SerializedObject(intro);
            introSo.FindProperty("_bootGroup").objectReferenceValue = bootGroup;
            introSo.FindProperty("_studioText").objectReferenceValue = studioText;
            introSo.FindProperty("_taglineText").objectReferenceValue = taglineText;
            introSo.FindProperty("_menuGroup").objectReferenceValue = menuGroup;
            introSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Build settings : menu en 0, jeu en 1 (exit la SampleScene).
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };

            Debug.Log("[MenuSceneBuilder] Menu principal construit : " + ScenePath);
        }

        // ==================== HELPERS UI ====================

        private static CanvasGroup CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<CanvasGroup>();
        }

        private static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int size, Color color, Vector2 anchorCenter, float rotationZ = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchorCenter;
            rect.sizeDelta = new Vector2(1600, size * 1.6f);
            rect.localRotation = Quaternion.Euler(0, 0, rotationZ);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Handwriting;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchorCenter)
        {
            var background = CreateImage(parent, name, PanelDark, anchorCenter, anchorCenter, Vector2.zero, Vector2.zero);
            var rect = (RectTransform)background.transform;
            rect.sizeDelta = new Vector2(520, 64);

            var textComp = CreateText(background.transform, "Text", "", 30, Ink, new Vector2(0.5f, 0.5f));
            ((RectTransform)textComp.transform).sizeDelta = new Vector2(480, 50);
            textComp.alignment = TextAnchor.MiddleLeft;

            var placeholderComp = CreateText(background.transform, "Placeholder", placeholder, 30, new Color(0.45f, 0.45f, 0.45f), new Vector2(0.5f, 0.5f));
            ((RectTransform)placeholderComp.transform).sizeDelta = new Vector2(480, 50);
            placeholderComp.alignment = TextAnchor.MiddleLeft;
            placeholderComp.fontStyle = FontStyle.Italic;

            var field = background.gameObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = textComp;
            field.placeholder = placeholderComp;
            return field;
        }

        private static void CreateButton(Transform parent, string name, string label, Vector2 anchorCenter,
            System.Action noop, MainMenuController controller, string methodName, Color background, Color textColor)
        {
            var image = CreateImage(parent, name, background, anchorCenter, anchorCenter, Vector2.zero, Vector2.zero);
            var rect = (RectTransform)image.transform;
            rect.sizeDelta = new Vector2(520, 78);
            rect.localRotation = Quaternion.Euler(0, 0, Random.Range(-1f, 1f)); // léger désaxé fait-main

            var text = CreateText(image.transform, "Label", label, 40, textColor, new Vector2(0.5f, 0.5f));
            ((RectTransform)text.transform).sizeDelta = new Vector2(500, 60);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // Câblage persistant de l'événement (sérialisé dans la scène).
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), controller, methodName));
        }
    }
}
