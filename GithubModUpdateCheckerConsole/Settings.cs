namespace GithubModUpdateCheckerConsole
{
    internal class Settings
    {
        // internalだとconfig.jsonでなにも入力されない

        public static Settings Instance { get; set; } = new Settings();
        public string BeatSaberExeFolderPath { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Beat Saber";

        public string OAuthToken { get; set; } = "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";
    }
}
