using CsvHelper;
using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Structure;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
        private string downloadModsTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
        private string githubModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GithubModData.csv");
        private string modAssistantModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ModAssistantModData.csv");
        private string backupFodlerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
        
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
            }

            // Console.WriteLine("Start GetAllModAssistantMods");
            modAssistantAllMods = await modAssistantManager.GetAllModAssistantMods();

            localFilesInfoDictionary = dataManager.GetLocalModFilesInfo(pluginsPath);

            foreach (KeyValuePair<string, Version> fileAndVersion in localFilesInfoDictionary)
            {
                foreach (var item in modAssistantAllMods)
                {
                    passInputGithubModInformation = dataManager.DetectMAModAndRemoveFromManagement(item, fileAndVersion, ref detectedModAssistantModCsvList, out bool loopBreak);
                    if (loopBreak)
                    {
                        break;
                    }
                }

                if (!passInputGithubModInformation)
                {
                    dataManager.InputGithubModInformation(githubManager, fileAndVersion, ref githubModInformationCsv);
                }
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
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
                // Console.WriteLine("Start GetAllModAssistantMods");
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

                localFilesInfoDictionary = dataManager.GetLocalModFilesInfo(pluginsPath);

                Console.WriteLine("前回実行時との差分を取得");

                // MAの更新を反映
                foreach (var item in modAssistantAllMods)
                {
                    dataManager.DetectMAModForUpdate(item, ref githubModAndOriginalBoolAndUrl, ref githubModInformationCsv);
                }
                // ローカルの差分を反映
                dataManager.ManageLocalPluginsDiff(localFilesInfoDictionary, modAssistantAllMods, githubManager,
                    ref githubModAndOriginalBoolAndUrl, ref githubModInformationCsv);
            }
            foreach (var fileNameAndOriginalBoolAndVersion in githubModAndOriginalBoolAndUrl)
            {
                string pluginPath = Path.Combine(pluginsPath, fileNameAndOriginalBoolAndVersion.Key);

                var latestVersion = githubManager.GetGithubModLatestVersion(fileNameAndOriginalBoolAndVersion.Value.Item2).Result;

                if (latestVersion > localFilesInfoDictionary[fileNameAndOriginalBoolAndVersion.Key])
                {
                    await githubManager.DownloadGithubModAsync(githubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndVersion.Key].Item2,
                        localFilesInfoDictionary[fileNameAndOriginalBoolAndVersion.Key],downloadModsTemp);
                }
            }

            await dataManager.OrganizeDownloadFileStructure(downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);

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
                    if (githubModAndOriginalBoolAndUrl.ContainsKey(a.name))
                    {
                        ModAssistantModInformationCsv modAssistantCsvInstance = new ModAssistantModInformationCsv()
                        {
                            ModAssistantMod = a.name,
                            LocalVersion = localFilesInfoDictionary[a.name].ToString(),
                            ModAssistantVersion = a.version,
                        };

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
                // すべてダウンロードしたいのでnew Version("0.0.0")を渡す
                // ダウンロードされるのは最新のもの
                await githubManager.DownloadGithubModAsync(a.GithubUrl,new Version("0.0.0"),downloadModsTemp);
            }

            await dataManager.OrganizeDownloadFileStructure(downloadModsTemp, downloadModsTemp);
        }

        public void Backup()
        { 
            if (!Directory.Exists(backupFodlerPath))
            {
                Console.WriteLine($"{backupFodlerPath}がありません");
                Console.WriteLine($"{backupFodlerPath}を作成します");
                Directory.CreateDirectory(backupFodlerPath);
            }
            if(!Directory.Exists(importCsv))
            {
                Console.WriteLine($"{importCsv}がありません");
                Console.WriteLine($"{importCsv}を作成します");
                Directory.CreateDirectory(importCsv);
            }

            gameVersion =modAssistantManager.GetGameVersion();
            string now=DateTime.Now.ToString("yyyyMMddHHmmss");
            string zipPath= Path.Combine(backupFodlerPath, $"BS{gameVersion}-{now}");
            Directory.CreateDirectory(zipPath);

            dataManager.DirectoryCopy(pluginsPath, Path.Combine(zipPath,"Plugins"), true);
            dataManager.DirectoryCopy(importCsv, Path.Combine(zipPath, "Data"), true);
            File.Copy(configFile, Path.Combine(zipPath, "config.json"), true);

            ZipFile.CreateFromDirectory(zipPath, Path.Combine(backupFodlerPath, $"BS{gameVersion}-{now}.zip"));
        }

        public void CleanModsTemp()
        {
            if (!Directory.Exists(downloadModsTemp))
            {
                Console.WriteLine($"{downloadModsTemp}がありません");
                Console.WriteLine($"{downloadModsTemp}を作成します");
                Directory.CreateDirectory(downloadModsTemp);
            }
            DirectoryInfo dir = new DirectoryInfo(downloadModsTemp);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                file.Delete();
            }
        }
    }
}

