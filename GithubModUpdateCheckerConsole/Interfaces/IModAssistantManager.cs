using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IModAssistantManager
    {
        Task GetAllModAssistantMods(string modAssistantModInformationUrl);
    }
}
