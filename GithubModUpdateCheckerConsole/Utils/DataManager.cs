using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Structure;
using System;
using System.Collections.Generic;
using System.IO;

namespace GithubModUpdateCheckerConsole.Utils
{
    internal class DataManager : IDataManager
    {
        public Dictionary<string, Version> GetLocalFilesInfo(string pluginsFolderPath)
        {
            // Console.WriteLine("Start Getting FileInfo");

            Dictionary<string, Version> filesInfo = new Dictionary<string, Version>();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(pluginsFolderPath);
            IEnumerable<System.IO.FileInfo> filesName = di.EnumerateFiles("*.dll", System.IO.SearchOption.AllDirectories);
            foreach (System.IO.FileInfo f in filesName)
            {
                string pluginPath = Path.Combine(pluginsFolderPath, f.Name);
                System.Diagnostics.FileVersionInfo vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(pluginPath);
                Version installedModVersion = new Version(vi.FileVersion);

                filesInfo.Add(f.Name.Replace(".dll", ""), installedModVersion);
            }

            return filesInfo;
        }

        public bool DetectModAssistantModAndRemoveFromManagement(ModAssistantModInformation item, KeyValuePair<string, Version> fileAndVersion, ref List<ModAssistantModInformationCsv> detectedModAssistantModCsvList)
        {
            bool pass = false;

            if (item.name == fileAndVersion.Key)
            {
                Version modAssistantModVersion = new Version(item.version);
                if (modAssistantModVersion >= fileAndVersion.Value)
                {
                    Console.WriteLine(item.name + "はModAssistantにあります");
                }
                else
                {
                    Console.WriteLine(item.name + "はModAssistantにありますが、ローカルにあるのは改造版の可能性が高いです");
                }

                Console.WriteLine(item.name + "をModAssistantで管理しますか？ [y/n]");
                string? manageInModAssistant = Console.ReadLine();

                if (manageInModAssistant == "y")
                {
                    Console.WriteLine("ModAssistantModData.csvに追加します");

                    var modAssistantCsvInstance = new ModAssistantModInformationCsv()
                    {
                        ModAssistantMod = fileAndVersion.Key,
                        LocalVersion = fileAndVersion.Value.ToString(),
                        ModAssistantVersion = item.version,
                    };
                    detectedModAssistantModCsvList.Add(modAssistantCsvInstance);

                    pass = true;
                }
            }

            return pass;
        }

        public void InputGithubModInformation(IGithubManager githubManager, KeyValuePair<string, Version> fileAndVersion, ref List<GithubModInformationCsv> githubModInformationCsv)
        {
            Console.WriteLine($"{fileAndVersion.Key} : {fileAndVersion.Value}");
            Console.WriteLine("オリジナルModですか？ [y/n]");
            var ok = Console.ReadLine();
            bool originalMod;
            if (ok == "y")
            {
                originalMod = true;
            }
            else
            {
                originalMod = false;
            }

            Console.WriteLine("GithubのリポジトリのUrlを入力してください");
            var githubUrl = Console.ReadLine();

            Console.WriteLine("Githubの最新のリリースのタグ情報を取得します");

            string githubModVersion = githubManager.GetGithubModLatestVersion(githubUrl).Result.ToString();

            Console.WriteLine("GithubModData.csvに追加します");

            var githubModInstance = new GithubModInformationCsv()
            {
                GithubMod = fileAndVersion.Key,
                LocalVersion = fileAndVersion.Value.ToString(),
                GithubVersion = githubModVersion,
                OriginalMod = originalMod,
                GithubUrl = githubUrl,
            };
            githubModInformationCsv.Add(githubModInstance);
        }

        public void DetectModAssistantModForUpdate(ModAssistantModInformation item, ref Dictionary<string, Tuple<bool, string>> githubModAndOriginalBoolAndUrl, ref List<GithubModInformationCsv> githubModInformationCsv)
        {
            if (githubModAndOriginalBoolAndUrl.ContainsKey(item.name))
            {
                if (githubModAndOriginalBoolAndUrl[item.name].Item1)
                {
                    Console.WriteLine(item.name + "はオリジナルModとして登録されており、かつModAssistantにあります");
                    Console.WriteLine($"よって、{ item.name} + を管理から外します");

                    githubModAndOriginalBoolAndUrl.Remove(item.name);
                    githubModInformationCsv.Remove(githubModInformationCsv.Find(n => n.GithubMod == item.name));
                }
            }
        }

        public void ManageLocalPluginsDiff(Dictionary<string, Version> localFilesInfoDictionary, ModAssistantModInformation[] modAssistantAllMods, IGithubManager githubManager,
            ref Dictionary<string, Tuple<bool, string>> githubModAndOriginalBoolAndUrl, ref List<GithubModInformationCsv> githubModInformationCsv)
        {
            // ローカルファイル減少分
            foreach (var a in githubModAndOriginalBoolAndUrl)
            {
                if (!localFilesInfoDictionary.ContainsKey(a.Key))
                {
                    githubModAndOriginalBoolAndUrl.Remove(a.Key);
                    githubModInformationCsv.Remove(githubModInformationCsv.Find(n => n.GithubMod == a.Key));
                }
            }
            // ローカルファイル増加分
            foreach (var a in localFilesInfoDictionary)
            {
                if (!githubModAndOriginalBoolAndUrl.ContainsKey(a.Key) && !Array.Exists(modAssistantAllMods, element => element.name == a.Key))
                {
                    InputGithubModInformation(githubManager, new KeyValuePair<string, Version>(a.Key, a.Value), ref githubModInformationCsv);
                    Tuple<bool, string> tempGithubModInformation = new Tuple<bool, string>(githubModInformationCsv.Find(n => n.GithubMod == a.Key).OriginalMod, githubModInformationCsv.Find(n => n.GithubMod == a.Key).GithubUrl);
                    githubModAndOriginalBoolAndUrl[a.Key] = tempGithubModInformation;
                }
            }
        }
    }
}
