namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IConfigManager
    {
        void LoadConfigFile(string path);
        void MakeConfigFile(string path);
    }
}
