using GithubModUpdateCheckerConsole.Utils;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IModAssistantManager
    {
        Task<ModAssistantModInformation[]> GetAllModAssistantMods();
        string GetGameVersion();
    }
}
