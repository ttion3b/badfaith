namespace BadFaith.Menu
{
    public enum MenuMode { None, Host, Join }

    /// <summary>
    /// Ce que le joueur a choisi au menu principal — lu par NetworkLauncherHUD
    /// au chargement de la scène de jeu pour démarrer la connexion tout seul.
    /// </summary>
    public static class MenuIntent
    {
        public static MenuMode Mode = MenuMode.None;
        public static string Address = "localhost";
        public static string Pseudo = "";

        public static void Clear() => Mode = MenuMode.None;
    }
}
