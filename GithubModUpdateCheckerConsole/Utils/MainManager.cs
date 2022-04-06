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
    internal class MainManager : DataManager, IMainManager
    {
        IModAssistantManager modAssistantManager;
        IGithubManager githubManager;
        IConfigManager configManager;

        private bool passInputGithubModInformation = false;

        internal MainManager()
        {
            modAssistantManager = new ModAssistantManager();
            githubManager = new GithubManager();
            configManager = new ConfigManager();
        }

        // Based on https://dobon.net/vb/dotnet/file/getfiles.html and https://dobon.net/vb/dotnet/file/fileversion.html
        public async Task Initialize()
        {
            // config.jsonの作成
            if (!File.Exists(DataContainer.configFile))
            {
                Console.WriteLine("Make a Config File");
                configManager.MakeConfigFile(DataContainer.configFile);
            }

            // Console.WriteLine("Start GetAllModAssistantMods");
            DataContainer.modAssistantAllMods = await modAssistantManager.GetAllModAssistantMods();

            DataContainer.nowLocalFilesInfoDictionary = GetLocalModFilesInfo(DataContainer.nowPluginsPath);

            foreach (KeyValuePair<string, Version> fileAndVersion in DataContainer.nowLocalFilesInfoDictionary)
            {
                Console.WriteLine("****************************************************");

                foreach (var item in DataContainer.modAssistantAllMods)
                {
                    passInputGithubModInformation = DetectMAModAndRemoveFromManagementForInitialize(item, fileAndVersion, out bool localFileSearchLoopBreak);
                    if (localFileSearchLoopBreak)
                    {
                        break;
                    }
                }

                if (!passInputGithubModInformation)
                {
                    githubManager.InputGithubModInformation(fileAndVersion);
                }
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
            }

            if (DataContainer.detectedModAssistantModCsvList.Count > 0)
            {
                WriteCsv(DataContainer.mAModCsvPath, DataContainer.detectedModAssistantModCsvList);
            }

            WriteCsv(DataContainer.githubModCsvPath, DataContainer.initializeGithubModInformationCsv);
        }

        public async Task UpdateGithubModForUsualBSVersionAsync()
        {
            if (!File.Exists(DataContainer.githubModCsvPath))
            {
                Console.WriteLine($"{DataContainer.githubModCsvPath}がありません");
                Console.WriteLine("イニシャライズします");
                await Initialize();
            }

            DataContainer.modAssistantAllMods = await modAssistantManager.GetAllModAssistantMods();

            ReadCsv(DataContainer.githubModCsvPath, out List<GithubModInformationCsv> githubModInformationList);

            ReadCsv(DataContainer.mAModCsvPath, out List<MAModInformationCsv> mAModInformationList);

            foreach (var githubModInformation in githubModInformationList)
            {
                DataContainer.updateGithubModInformationCsv.Add(githubModInformation);
                Tuple<bool, string> originalBoolAndUrl = new Tuple<bool, string>(githubModInformation.OriginalMod, githubModInformation.GithubUrl);
                DataContainer.nowLocalGithubModAndOriginalBoolAndUrl.Add(githubModInformation.GithubMod, originalBoolAndUrl);
            }
            foreach (var mAModInformation in mAModInformationList)
            {
                DataContainer.installedMAMod.Add(mAModInformation.ModAssistantMod);
            }

            DataContainer.nowLocalFilesInfoDictionary = GetLocalModFilesInfo(DataContainer.nowPluginsPath);

            Console.WriteLine("前回実行時との差分を取得");

            // ローカルの差分を反映
            ManageLocalPluginsDiff(githubManager);

            // MAの更新を反映,ローカル増加分でMAにあるModの処理
            foreach (var item in DataContainer.modAssistantAllMods)
            {
                if (!DataContainer.nowLocalGithubModAndOriginalBoolAndUrl.ContainsKey(item.name) && DataContainer.nowLocalFilesInfoDictionary.ContainsKey(item.name))
                {
                    KeyValuePair<string, Version> fileAndVersion = new KeyValuePair<string, Version>(item.name, DataContainer.nowLocalFilesInfoDictionary[item.name]);

                    Console.WriteLine("****************************************************");

                    passInputGithubModInformation = DetectMAModAndRemoveFromManagementForUpdate(item, fileAndVersion);

                    if (!passInputGithubModInformation)
                    {
                        githubManager.InputGithubModInformation(fileAndVersion);
                        GithubModInformationCsv newGithubModNotManageInMA = DataContainer.updateGithubModInformationCsv[DataContainer.updateGithubModInformationCsv.Count - 1];
                        Tuple<bool, string> tempGithubModInformation = new Tuple<bool, string>(newGithubModNotManageInMA.OriginalMod, newGithubModNotManageInMA.GithubUrl);
                        DataContainer.nowLocalGithubModAndOriginalBoolAndUrl[newGithubModNotManageInMA.GithubMod] = tempGithubModInformation;
                    }
                }
                DetectAddedMAModForUpdate(item);
            }

            foreach (var fileNameAndOriginalBoolAndUrl in DataContainer.nowLocalGithubModAndOriginalBoolAndUrl)
            {
                string pluginPath = Path.Combine(DataContainer.nowPluginsPath, fileNameAndOriginalBoolAndUrl.Key);

                var latestVersion = githubManager.GetGithubModLatestVersion(fileNameAndOriginalBoolAndUrl.Value.Item2).Result;

                if (latestVersion > DataContainer.nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key])
                {
                    await githubManager.DownloadGithubModAsync(DataContainer.nowLocalGithubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndUrl.Key].Item2,
                        DataContainer.nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key], DataContainer.downloadModsTemp);
                }
            }

            OrganizeDownloadFileStructure(DataContainer.downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);

            WriteCsv(DataContainer.githubModCsvPath, DataContainer.updateGithubModInformationCsv);

            UpdateModAssistantModCsv();
        }

        public async Task UpdateGithubModForNewBSVersionAsync()
        {
            bool existOldPluginsFolder = false;
            string oldPluginsPath = "";

            if (!File.Exists(DataContainer.githubModCsvPath))
            {
                Console.WriteLine($"{DataContainer.githubModCsvPath}がありません");
                Console.WriteLine("イニシャライズします");
                await Initialize();
            }
            else
            {
                while (!existOldPluginsFolder)
                {
                    Console.WriteLine("参照するOld PluginsフォルダのBeat Saberのバージョンを入力してください");
                    Console.WriteLine("例 : Old 1.19.0 Pluginsなら\"1.19.0\"と入力してください");
                    DataContainer.oldGameVersion = Console.ReadLine();
                    oldPluginsPath = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, $"Old {DataContainer.oldGameVersion} Plugins");

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

                ReadCsv(DataContainer.githubModCsvPath, out List<GithubModInformationCsv> githubModInformationList);

                foreach (var githubModInformation in githubModInformationList)
                {
                    DataContainer.updateGithubModInformationCsv.Add(githubModInformation);
                    Tuple<bool, string> originalBoolAndUrl = new Tuple<bool, string>(githubModInformation.OriginalMod, githubModInformation.GithubUrl);
                    DataContainer.nowLocalGithubModAndOriginalBoolAndUrl.Add(githubModInformation.GithubMod, originalBoolAndUrl);
                }

                DataContainer.nowLocalFilesInfoDictionary = GetLocalModFilesInfo(DataContainer.nowPluginsPath);
                DataContainer.oldLocalFilesInfoDictionary = GetLocalModFilesInfo(oldPluginsPath);

                foreach (var fileNameAndOriginalBoolAndUrl in DataContainer.nowLocalGithubModAndOriginalBoolAndUrl)
                {
                    string fileName = fileNameAndOriginalBoolAndUrl.Key;

                    if (!DataContainer.nowLocalFilesInfoDictionary.ContainsKey(fileName) && DataContainer.oldLocalFilesInfoDictionary.ContainsKey(fileName))
                    {
                        await githubManager.DownloadGithubModAsync(DataContainer.nowLocalGithubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndUrl.Key].Item2,
                            DataContainer.oldLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key], DataContainer.downloadModsTemp);
                    }
                    if (DataContainer.nowLocalFilesInfoDictionary.ContainsKey(fileName) && DataContainer.oldLocalFilesInfoDictionary.ContainsKey(fileName))
                    {
                        await githubManager.DownloadGithubModAsync(DataContainer.nowLocalGithubModAndOriginalBoolAndUrl[fileNameAndOriginalBoolAndUrl.Key].Item2,
                            DataContainer.nowLocalFilesInfoDictionary[fileNameAndOriginalBoolAndUrl.Key], DataContainer.downloadModsTemp);
                    }
                }

                OrganizeDownloadFileStructure(DataContainer.downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);
            }
        }


        public async Task ImportCsv()
        {
            if (!Directory.Exists(DataContainer.importCsv))
            {
                Console.WriteLine($"{DataContainer.importCsv}がありません");
                Console.WriteLine($"{DataContainer.importCsv}を作成します");
                Directory.CreateDirectory(DataContainer.importCsv);
            }
            if (!File.Exists(Path.Combine(DataContainer.importCsv, "GithubModData.csv")))
            {
                Console.WriteLine($"{Path.Combine(DataContainer.importCsv, "GithubModData.csv")}がありません");
                Console.WriteLine("終了します");
                return;
            }

            ReadCsv(Path.Combine(DataContainer.importCsv, "GithubModData.csv"), out List<GithubModInformationCsv> githubModInformationList);

            foreach (var a in githubModInformationList)
            {
                // すべてダウンロードしたいのでnew Version("0.0.0")を渡す
                // ダウンロードされるのは最新のもの
                await githubManager.DownloadGithubModAsync(a.GithubUrl, new Version("0.0.0"), DataContainer.downloadModsTemp);
            }

            OrganizeDownloadFileStructure(DataContainer.downloadModsTemp, DataContainer.downloadModsTemp);
        }
    }
}

