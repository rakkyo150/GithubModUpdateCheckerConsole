using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    internal interface IMainManager
    {
        Task Initialize();
        Task UpdateGithubModAsync();
        void UpdateModAssistantModCsv();
        Task ImportCsv();

        void Backup();

        void CleanModsTemp(string downloadModsTemp);
    }
}
