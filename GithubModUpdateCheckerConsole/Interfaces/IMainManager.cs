using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    internal interface IMainManager
    {
        Task Initialize();

        Task UpdateGithubModForUsualBSVersionAsync();
        void UpdateModAssistantModCsv();
        Task UpdateGithubModForNewBSVersionAsync();
        Task ImportCsv();
        
        void Backup();
        void CleanModsTemp(string downloadModsTemp);
    }
}
