using GithubModUpdateCheckerConsole.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Utils
{
    internal class ModAssistantManager : IModAssistantManager
    {
        public async Task GetAllModAssistantMods(string modAssistantModInformationUrl)
        {
            Console.WriteLine(modAssistantModInformationUrl);
            using HttpClient httpClient = new HttpClient();
            try
            {
                var resp = await httpClient.GetStringAsync(modAssistantModInformationUrl);
                Console.WriteLine(resp);
                MainManager.modAssistantMod = JsonConvert.DeserializeObject<ModAssistantModInformation[]>(resp);

                Console.WriteLine("Fisnish GetAllMods");
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
