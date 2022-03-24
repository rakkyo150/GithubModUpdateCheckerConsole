using GithubModUpdateCheckerConsole.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Utils
{
    internal class ModAssistantManager : IModAssistantManager
    {
        public async Task<ModAssistantModInformation[]> GetAllModAssistantMods()
        {
            ModAssistantModInformation[] modAssistantMod = null;

            string gameVersion = GetGameVersion();

            string modAssistantModInformationUrl = $"https://beatmods.com/api/v1/mod?status=approved&gameVersion={gameVersion}";

            using HttpClient httpClient = new HttpClient();
            try
            {
                var resp = await httpClient.GetStringAsync(modAssistantModInformationUrl);
                modAssistantMod = JsonConvert.DeserializeObject<ModAssistantModInformation[]>(resp);

                // Console.WriteLine("Fisnish GetAllMods");

                foreach(var mod in modAssistantMod)
                {
                    // Mod名とファイル名が違う、よく使うModに対応
                    if (mod.name == "BeatSaberMarkupLanguage")
                    {
                        mod.name = "BSML";
                    }
                    else if (mod.name == "BS Utils")
                    {
                        mod.name = "BS_Utils";
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return modAssistantMod;
        }

        public string GetGameVersion()
        {
            string filename = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, "Beat Saber_Data", "globalgamemanagers");
            using (var stream = File.OpenRead(filename))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                const string key = "public.app-category.games";
                int pos = 0;

                while (stream.Position < stream.Length && pos < key.Length)
                {
                    if (reader.ReadByte() == key[pos]) pos++;
                    else pos = 0;
                }

                if (stream.Position == stream.Length) // we went through the entire stream without finding the key
                    return null;

                while (stream.Position < stream.Length)
                {
                    var current = (char)reader.ReadByte();
                    if (char.IsDigit(current))
                        break;
                }

                var rewind = -sizeof(int) - sizeof(byte);
                stream.Seek(rewind, SeekOrigin.Current); // rewind to the string length

                var strlen = reader.ReadInt32();
                var strbytes = reader.ReadBytes(strlen);

                return Encoding.UTF8.GetString(strbytes);
            }
        }
    }
}
