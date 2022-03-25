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
        private string oldBSVersion;
        private bool passInputGithubModInformation = false;

        private string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private string importCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImportGithubCsv");
        private string nowPluginsPath = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, "Plugins");
        private string downloadModsTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
        private string githubModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GithubModData.csv");
        private string mAModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ModAssistantModData.csv");
        private string backupFodlerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
        
        Dictionary<string, Version> nowLocalFilesInfoDictionary;
        Dictionary<string, Version> oldLocalFilesInfoDictionary;

        private Dictionary<string, Tuple<bool, string>> installedGithubModAndOriginalBoolAndUrl = new Dictionary<string, Tuple<bool, string>>();
        private List<string> installedMAMod=new List<string>();

        private ModAssistantModInformation[] modAssistantAllMods;

        private List<GithubModInformationCsv> initializeGithubModInformationCsv = new List<GithubModInformationCsv>();
        private List<GithubModInformationCsv> secondGithubModInformationCsv = new List<GithubModInformationCsv>();
        private List<MAModInformationCsv> detectedModAssistantModCsvList = new List<MAModInformationCsv>();
        private List<MAModInformationCsv> updateModAssistantModCsvList = new List<MAModInformationCsv>();

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

            nowLocalFilesInfoDictionary = dataManager.GetLocalModFilesInfo(nowPluginsPath);

            foreach (KeyValuePair<string, Version> fileAndVersion in nowLocalFilesInfoDictionary)
            {
                Console.WriteLine("****************************************************");

                foreach (var item in modAssistantAllMods)
                {
                    passInputGithubModInformation = dataManager.DetectMAModAndRemoveFromManagementForInitialize(item, fileAndVersion, ref detectedModAssistantModCsvList, out bool localFileSearchLoopBreak);
                    if (localFileSearchLoopBreak)
                    {
                        break;
                    }
                }

                if (!passInputGithubModInformation)
                {
                    dataManager.InputGithubModInformation(githubManager, fileAndVersion, ref initializeGithubModInformationCsv);
                }
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
            }

            if (detectedModAssistantModCsvList.Count > 0)
            {
                using var writer2 = new StreamWriter(mAModCsvPath, false);
                using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                csv2.WriteRecords(detectedModAssistantModCsvList);
            }

            using var writer1 = new StreamWriter(githubModCsvPath, false);
            using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
            csv1.WriteRecords(initializeGithubModInformationCsv);
        }

        public async Task UpdateGithubModForUsualBSVersionAsync()
        {
            if (!File.Exists(githubModCsvPath))
            {
                Console.WriteLine($"{githubModCsvPath}がありません");
                Console.WriteLine("イニシャライズします");
                await Initialize();
            }

            

            // Console.WriteLine("Start GetAllModAssistantMods");
            modAssistantAllMods = await modAssistantManager.GetAllModAssistantMods();

            
            using (var reader = new StreamReader(githubModCsvPath))
            using (var csv = new CsvReader(reader, new CultureInfo("ja-JP", false)))
            {
                IEnumerable<GithubModInformationCsv> githubModInformationEnum = csv.GetRecords<GithubModInformationCsv>();

                using var reader2 = new StreamReader(mAModCsvPath);
                using var csv2 = new CsvReader(reader2, new CultureInfo("ja-JP", false));
                IEnumerable<MAModInformationCsv> mAModInformationEnum = csv2.GetRecords<MAModInformationCsv>();

                foreach (var githubModInformation in githubModInformationEnum)
                {
                    secondGithubModInformationCsv.Add(githubModInformation);
                    Tuple<bool, string> originalBoolAndUrl = new Tuple<bool, string>(githubModInformation.OriginalMod, githubModInformation.GithubUrl);
                    installedGithubModAndOriginalBoolAndUrl.Add(githubModInformation.GithubMod, originalBoolAndUrl);
                }
                foreach (var mAModInformation in mAModInformationEnum)
                {
                    installedMAMod.Add(mAModInformation.ModAssistantMod);
                }

                nowLocalFilesInfoDictionary = dataManager.GetLocalModFilesInfo(nowPluginsPath);
            }

            Console.WriteLine("前回実行時との差分を取得");

            // ローカルの差分を反映
            dataManager.ManageLocalPluginsDiff(nowLocalFilesInfoDictionary, modAssistantAllMods, githubManager,
                ref installedGithubModAndOriginalBoolAndUrl, ref secondGithubModInformationCsv);

            // MAの更新を反映,ローカル増加分でMAにあるModの処理
            foreach (var item in modAssistantAllMods)
            {
                if (!installedGithubModAndOriginalBoolAndUrl.ContainsKey(item.name) && nowLocalFilesInfoDictionary.ContainsKey(item.name))
                {
                    KeyValuePair<string,Version> fileAndVersion=new KeyValuePair<string,Version>(item.name,nowLocalFilesInfoDictionary[item.name]);

                    passInputGithubModInformation=dataManager.DetectMAModAndRemoveFromManagementForUpdate(item, fileAndVersion,installedMAMod);

                    if (!passInputGithubModInformation)
                    {
                        dataManager.InputGithubModInformation(githubManager, fileAndVersion, ref secondGithubModInformationCsv);
                        GithubModInformationCsv newGithubModNotManageInMA = secondGithubModInformationCsv[secondGithubModInformationCsv.Count - 1];
                        Tuple<bool, string> tempGithubModInformation = new Tuple<bool, string>(newGithubModNotManageInMA.OriginalMod, newGithubModNotManageInMA.GithubUrl);
                        installedGithubModAndOriginalBoolAndUrl[newGithubModNotManageInMA.GithubMod] = tempGithubModInformation;
                    }
                }
                dataManager.DetectAddedMAModForUpdate(item, ref installedGithubModAndOriginalBoolAndUrl, ref secondGithubModInformationCsv);
            }

            foreach (var fileNameAndOriginalBoolAndUrl in installedGithubModAndOriginalBoolAndUrl)
            {
                string pluginPath = Path.Combine(nowPluginsPath, fileNameAndOriginalBoolAndUrl.Key);

                var latestVersion = githubManager.GetGithubModLatestVersion(fileNameAndOriginalBoolAndUrl.Value.Item2).Result;

                if (latestVersion > nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key])
                {
                    await githubManager.DownloadGithubModAsync(installedGithubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndUrl.Key].Item2,
                        nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key],downloadModsTemp);
                }
            }

            await dataManager.OrganizeDownloadFileStructure(downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);

            using var writer1 = new StreamWriter(githubModCsvPath, false);
            using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
            csv1.WriteRecords(secondGithubModInformationCsv);

            UpdateModAssistantModCsv();
        }

        public void UpdateModAssistantModCsv()
        {
            if (modAssistantAllMods != null)
            {
                foreach (var a in modAssistantAllMods)
                {
                    if (!installedGithubModAndOriginalBoolAndUrl.ContainsKey(a.name) && nowLocalFilesInfoDictionary.ContainsKey(a.name))
                    {
                        MAModInformationCsv modAssistantCsvInstance = new MAModInformationCsv()
                        {
                            ModAssistantMod = a.name,
                            LocalVersion = nowLocalFilesInfoDictionary[a.name].ToString(),
                            ModAssistantVersion = a.version,
                        };

                        updateModAssistantModCsvList.Add(modAssistantCsvInstance);
                    }
                }

                using var writer2 = new StreamWriter(mAModCsvPath, false);
                using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                csv2.WriteRecords(updateModAssistantModCsvList);
            }
        }

        public async Task UpdateGithubModForNewBSVersionAsync()
        {
            bool existOldPluginsFolder = false;
            string oldPluginsPath = "";

            if (!File.Exists(githubModCsvPath))
            {
                Console.WriteLine($"{githubModCsvPath}がありません");
                Console.WriteLine("イニシャライズします");
                await Initialize();
            }
            else
            {
                while (!existOldPluginsFolder)
                {
                    Console.WriteLine("参照するOld PluginsフォルダのBeat Saberのバージョンを入力してください");
                    Console.WriteLine("例 : Old 1.19.0 Pluginsなら\"1.19.0\"と入力してください");
                    oldBSVersion = Console.ReadLine();
                    oldPluginsPath = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, $"Old {oldBSVersion} Plugins");

                    if (Directory.Exists(oldPluginsPath))
                    {
                        existOldPluginsFolder = true;
                    }
                    else
                    {
                        Console.WriteLine(oldPluginsPath + "は存在しません");
                        Console.WriteLine("もう一度入力をお願いします");
                    }
                }

                using var reader = new StreamReader(githubModCsvPath);
                using var csv = new CsvReader(reader, new CultureInfo("ja-JP", false));
                IEnumerable<GithubModInformationCsv> githubModInformationEnum = csv.GetRecords<GithubModInformationCsv>();

                foreach (var githubModInformation in githubModInformationEnum)
                {
                    secondGithubModInformationCsv.Add(githubModInformation);
                    Tuple<bool, string> originalBoolAndUrl = new Tuple<bool, string>(githubModInformation.OriginalMod, githubModInformation.GithubUrl);
                    installedGithubModAndOriginalBoolAndUrl.Add(githubModInformation.GithubMod, originalBoolAndUrl);
                }

                nowLocalFilesInfoDictionary = dataManager.GetLocalModFilesInfo(nowPluginsPath);
                oldLocalFilesInfoDictionary = dataManager.GetLocalModFilesInfo(oldPluginsPath);

                foreach (var fileNameAndOriginalBoolAndUrl in installedGithubModAndOriginalBoolAndUrl)
                {
                    string fileName = fileNameAndOriginalBoolAndUrl.Key;

                    if (!nowLocalFilesInfoDictionary.ContainsKey(fileName) && oldLocalFilesInfoDictionary.ContainsKey(fileName))
                    {
                        await githubManager.DownloadGithubModAsync(installedGithubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndUrl.Key].Item2,
                            oldLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key], downloadModsTemp);
                    }
                    if (nowLocalFilesInfoDictionary.ContainsKey(fileName) && oldLocalFilesInfoDictionary.ContainsKey(fileName))
                    {
                        string pluginPath = Path.Combine(oldPluginsPath, fileNameAndOriginalBoolAndUrl.Key);

                        var latestVersion = githubManager.GetGithubModLatestVersion(fileNameAndOriginalBoolAndUrl.Value.Item2).Result;

                        if (latestVersion > nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key])
                        {
                            await githubManager.DownloadGithubModAsync(installedGithubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndUrl.Key].Item2,
                            nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key], downloadModsTemp);
                        }
                    }
                }

                await dataManager.OrganizeDownloadFileStructure(downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);

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

            dataManager.DirectoryCopy(nowPluginsPath, Path.Combine(zipPath,"Plugins"), true);
            dataManager.DirectoryCopy(importCsv, Path.Combine(zipPath, "Data"), true);
            File.Copy(configFile, Path.Combine(zipPath, "config.json"), true);

            ZipFile.CreateFromDirectory(zipPath, Path.Combine(backupFodlerPath, $"BS{gameVersion}-{now}.zip"));
            Directory.Delete(zipPath, true);
        }

        public void CleanModsTemp(string path)
        {
            if (!Directory.Exists(downloadModsTemp))
            {
                Console.WriteLine($"{downloadModsTemp}がありません");
                Console.WriteLine($"{downloadModsTemp}を作成します");
                Directory.CreateDirectory(downloadModsTemp);
            }
            DirectoryInfo dir = new DirectoryInfo(downloadModsTemp);

            //ディレクトリ以外の全ファイルを削除
            string[] filePaths = Directory.GetFiles(path);
            foreach (string filePath in filePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }

            //ディレクトリの中のディレクトリも再帰的に削除
            string[] directiryPaths = Directory.GetDirectories(path);
            foreach (string directoryPath in directiryPaths)
            {
                CleanModsTemp(directoryPath);
            }

            if (path != downloadModsTemp)
            {
                Directory.Delete(path, false);
            }
        }
    }
}

