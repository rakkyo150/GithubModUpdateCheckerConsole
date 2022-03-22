using CsvHelper;
using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Structure;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole
{
    internal class MainManager : IMainManager
    {
        IDataManager dataManager;
        IModAssistantManager modAssistantManager;
        IGithubManager githubManager;
        IConfigManager configManager;

        private string gameVersion = "1.11.0";
        private bool passInputGithubModInformation = false;

        private string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private string importCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImportGithubCsv");
        private string pluginsPath = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, "Plugins");
        private string githubModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "GithubModData.csv");
        private string modAssistantModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "ModAssistantModData.csv");

        Dictionary<string, Version> localFilesInfoDictionary;

        private Dictionary<string, Tuple<bool, string>> githubModAndOriginalBoolAndUrl = new Dictionary<string, Tuple<bool, string>>();

        private ModAssistantModInformation[] modAssistantAllMods;

        private List<GithubModInformationCsv> githubModInformationCsv = new List<GithubModInformationCsv>();
        private List<ModAssistantModInformationCsv> detectedModAssistantModCsvList = new List<ModAssistantModInformationCsv>();
        private List<ModAssistantModInformationCsv> updateModAssistantModCsvList = new List<ModAssistantModInformationCsv>();

        internal MainManager()
        {
            dataManager = new DataManager();
            modAssistantManager = new ModAssistantManager();
            githubManager = new GithubManager();
            configManager = new ConfigManager();
        }

        // Based on https://dobon.net/vb/dotnet/file/getfiles.html and https://dobon.net/vb/dotnet/file/fileversion.html
        // OAuthも追加
        // なければconfig.jsonの作成、Modの振り分け、GithubのURL入力とcsvの作成
        public async Task Initialize()
        {
            // config.jsonの作成
            if (!File.Exists(configFile))
            {
                Console.WriteLine("Make a Config File");
                configManager.MakeConfigFile(configFile);
                Directory.CreateDirectory(importCsv);
            }

            // 以下、Modの振り分け準備
            Console.WriteLine("Start GetAllModAssistantMods");
            modAssistantAllMods = await modAssistantManager.GetAllModAssistantMods();

            localFilesInfoDictionary = dataManager.GetLocalFilesInfo(pluginsPath);

            // 以下、Modの振り分け、GithubModの情報入力とcsvへの登録
            foreach (KeyValuePair<string, Version> fileAndVersion in localFilesInfoDictionary)
            {
                foreach (var item in modAssistantAllMods)
                {
                    passInputGithubModInformation = dataManager.DetectModAssistantModAndRemoveFromManagement(item, fileAndVersion, ref detectedModAssistantModCsvList);
                }

                if (!passInputGithubModInformation)
                {
                    dataManager.InputGithubModInformation(githubManager, fileAndVersion, ref githubModInformationCsv);
                }
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"));
            }

            if (detectedModAssistantModCsvList.Count > 0)
            {
                using var writer2 = new StreamWriter(modAssistantModCsvPath, false);
                using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                csv2.WriteRecords(detectedModAssistantModCsvList);
            }

            using var writer1 = new StreamWriter(githubModCsvPath, false);
            using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
            csv1.WriteRecords(githubModInformationCsv);
        }

        public async Task UpdateGithubModAsync()
        {
            if (!File.Exists(githubModCsvPath))
            {
                Console.WriteLine($"{githubModCsvPath}がありません");
                Console.WriteLine("イニシャライズします");
                await Initialize();
            }
            else
            {
                Console.WriteLine("Start GetAllModAssistantMods");
                modAssistantAllMods = await modAssistantManager.GetAllModAssistantMods();

                using var reader = new StreamReader(githubModCsvPath);
                using var csv = new CsvReader(reader, new CultureInfo("ja-JP", false));
                IEnumerable<GithubModInformationCsv> githubModInformationEnum = csv.GetRecords<GithubModInformationCsv>();

                foreach (var githubModInformation in githubModInformationEnum)
                {
                    githubModInformationCsv.Add(githubModInformation);
                    Tuple<bool, string> originalBoolAndUrl = new Tuple<bool, string>(githubModInformation.OriginalMod, githubModInformation.GithubUrl);
                    githubModAndOriginalBoolAndUrl.Add(githubModInformation.GithubMod, originalBoolAndUrl);
                }

                localFilesInfoDictionary = dataManager.GetLocalFilesInfo(pluginsPath);

                Console.WriteLine("前回実行時との差分を取得");

                // MAの更新を反映
                foreach (var item in modAssistantAllMods)
                {
                    dataManager.DetectModAssistantModForUpdate(item, ref githubModAndOriginalBoolAndUrl, ref githubModInformationCsv);
                }
                // ローカルの差分を反映
                dataManager.ManageLocalPluginsDiff(localFilesInfoDictionary, modAssistantAllMods, githubManager,
                    ref githubModAndOriginalBoolAndUrl, ref githubModInformationCsv);
            }
            foreach (var fileAndOriginalBoolAndVersion in githubModAndOriginalBoolAndUrl)
            {
                string pluginPath = Path.Combine(pluginsPath, fileAndOriginalBoolAndVersion.Key);

                var latestVersion = githubManager.GetGithubModLatestVersion(fileAndOriginalBoolAndVersion.Value.Item2).Result;

                if (latestVersion > localFilesInfoDictionary[fileAndOriginalBoolAndVersion.Key])
                {
                    await githubManager.GithubModDownloadAsync(githubModAndOriginalBoolAndUrl[fileAndOriginalBoolAndVersion.Key].Item2);
                }
            }

            using var writer1 = new StreamWriter(githubModCsvPath, false);
            using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
            csv1.WriteRecords(githubModInformationCsv);

            UpdateModAssistantModCsv();
        }

        public void UpdateModAssistantModCsv()
        {
            if (modAssistantAllMods != null)
            {
                foreach (var a in modAssistantAllMods)
                {
                    if (!githubModAndOriginalBoolAndUrl.ContainsKey(a.name))
                    {
                        ModAssistantModInformationCsv modAssistantCsvInstance;
                        if (localFilesInfoDictionary.ContainsKey(a.name))
                        {
                            modAssistantCsvInstance = new ModAssistantModInformationCsv()
                            {
                                ModAssistantMod = a.name,
                                LocalVersion = localFilesInfoDictionary[a.name].ToString(),
                                ModAssistantVersion = a.version,
                            };
                        }
                        else
                        {
                            modAssistantCsvInstance = new ModAssistantModInformationCsv()
                            {
                                ModAssistantMod = a.name,
                                LocalVersion = a.version,
                                ModAssistantVersion = a.version,
                            };
                        }

                        updateModAssistantModCsvList.Add(modAssistantCsvInstance);
                    }
                }
                using var writer2 = new StreamWriter(modAssistantModCsvPath, false);
                using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                csv2.WriteRecords(updateModAssistantModCsvList);
            }
        }

        public async Task ImportCsv()
        {
            if (!Directory.Exists(importCsv))
            {
                Console.WriteLine($"{importCsv}がありません");
                Console.WriteLine($"{importCsv}を作成します");
                Directory.CreateDirectory(importCsv);
            }
            if (!File.Exists(Path.Combine(importCsv, "GithubModData.csv")))
            {
                Console.WriteLine($"{Path.Combine(importCsv, "GithubModData.csv")}がありません");
                Console.WriteLine("終了します");
                return;
            }

            using var reader = new StreamReader(Path.Combine(importCsv, "GithubModData.csv"));
            using var csv = new CsvReader(reader, new CultureInfo("ja-JP", false));
            IEnumerable<GithubModInformationCsv> githubModInformationEnum = csv.GetRecords<GithubModInformationCsv>();

            foreach (var a in githubModInformationEnum)
            {
                await githubManager.GithubModDownloadAsync(a.GithubUrl);
            }
        }
    }
}

