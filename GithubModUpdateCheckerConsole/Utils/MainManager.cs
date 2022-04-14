using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Structure;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Collections.Generic;
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

            DataContainer.nowLocalFilesInfoDictionary = GetLocalModFilesInfo(DataContainer.nowPluginsPath, null);

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
                    githubManager.InputGithubModInformation(fileAndVersion, DataContainer.installedGithubModInformationToCsvForInitialize);
                }
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
            }

            if (DataContainer.detectedModAssistantModCsvListForInitialize.Count > 0)
            {
                WriteCsv(DataContainer.mAModCsvPath, DataContainer.detectedModAssistantModCsvListForInitialize);
            }

            WriteCsv(DataContainer.githubModCsvPath, DataContainer.installedGithubModInformationToCsvForInitialize);
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

            ReadCsv(DataContainer.githubModCsvPath, out List<GithubModInformationCsv> previousGithubModInformationList);

            ReadCsv(DataContainer.mAModCsvPath, out List<MAModInformationCsv> previousMAModInformationList);

            foreach (var previousGithubModInformation in previousGithubModInformationList)
            {
                DataContainer.installedGithubModInformationToCsvForUpdate.Add(previousGithubModInformation);
                Tuple<Version, bool, string> versionAndOriginalBoolAndUrl = new Tuple<Version, bool, string>(
                    new Version(previousGithubModInformation.LocalVersion), previousGithubModInformation.OriginalMod, previousGithubModInformation.GithubUrl
                );
                DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl.Add(previousGithubModInformation.GithubMod, versionAndOriginalBoolAndUrl);
            }
            foreach (var previousMAModInformation in previousMAModInformationList)
            {
                DataContainer.installedMAMod.Add(previousMAModInformation.ModAssistantMod);
            }

            DataContainer.nowLocalFilesInfoDictionary = GetLocalModFilesInfo(DataContainer.nowPluginsPath, DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl);

            Console.WriteLine("前回実行時との差分を取得");

            // ローカルの差分を反映
            ManageLocalPluginsDiff(githubManager);

            // ローカル増加分でMAにあるModの処理、MAの更新を反映
            foreach (var item in DataContainer.modAssistantAllMods)
            {
                if (!DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl.ContainsKey(item.name) && DataContainer.nowLocalFilesInfoDictionary.ContainsKey(item.name))
                {
                    KeyValuePair<string, Version> fileAndVersion = new KeyValuePair<string, Version>(item.name, DataContainer.nowLocalFilesInfoDictionary[item.name]);

                    Console.WriteLine("****************************************************");

                    passInputGithubModInformation = DetectMAModAndRemoveFromManagementForUpdate(item, fileAndVersion);

                    if (!passInputGithubModInformation)
                    {
                        githubManager.InputGithubModInformation(fileAndVersion, DataContainer.installedGithubModInformationToCsvForUpdate);
                        GithubModInformationCsv newGithubModNotManageInMA = DataContainer.installedGithubModInformationToCsvForUpdate[DataContainer.installedGithubModInformationToCsvForUpdate.Count - 1];
                        Tuple<Version, bool, string> tempGithubModInformation = new Tuple<Version, bool, string>(
                            new Version(newGithubModNotManageInMA.LocalVersion), newGithubModNotManageInMA.OriginalMod, newGithubModNotManageInMA.GithubUrl);
                        DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl[newGithubModNotManageInMA.GithubMod] = tempGithubModInformation;
                    }
                }
                DetectAddedMAModForUpdate(item);
            }

            foreach (var fileNameAndVersioinAndOriginalBoolAndUrl in DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl)
            {
                string pluginPath = Path.Combine(DataContainer.nowPluginsPath, fileNameAndVersioinAndOriginalBoolAndUrl.Key);

                var latestVersion = githubManager.GetGithubModLatestVersion(fileNameAndVersioinAndOriginalBoolAndUrl.Value.Item3).Result;

                if (latestVersion > fileNameAndVersioinAndOriginalBoolAndUrl.Value.Item1)
                {
                    await githubManager.DownloadGithubModAsync(DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl[fileNameAndVersioinAndOriginalBoolAndUrl.Key].Item3,
                        DataContainer.nowLocalFilesInfoDictionary[fileNameAndVersioinAndOriginalBoolAndUrl.Key], DataContainer.downloadModsTemp,
                        DataContainer.installedGithubModInformationToCsvForUpdate, fileNameAndVersioinAndOriginalBoolAndUrl.Key);
                }
            }

            OrganizeDownloadFileStructure(DataContainer.downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);

            WriteCsv(DataContainer.githubModCsvPath, DataContainer.installedGithubModInformationToCsvForUpdate);

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
                    DataContainer.installedGithubModInformationToCsvForUpdate.Add(githubModInformation);
                    Tuple<Version, bool, string> versionAndOriginalBoolAndUrl = new Tuple<Version, bool, string>(
                        new Version(githubModInformation.LocalVersion), githubModInformation.OriginalMod, githubModInformation.GithubUrl
                    );
                    // 初回に関してはnowではなく前回実行時のデータを用いているのでold
                    DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl.Add(githubModInformation.GithubMod, versionAndOriginalBoolAndUrl);
                }

                DataContainer.nowLocalFilesInfoDictionary = GetLocalModFilesInfo(DataContainer.nowPluginsPath, DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl);
                DataContainer.oldLocalFilesInfoDictionary = GetLocalModFilesInfo(oldPluginsPath, null);

                foreach (var fileNameAndVersionAndOriginalBoolAndUrl in DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl)
                {
                    string fileName = fileNameAndVersionAndOriginalBoolAndUrl.Key;

                    if (!DataContainer.nowLocalFilesInfoDictionary.ContainsKey(fileName) && DataContainer.oldLocalFilesInfoDictionary.ContainsKey(fileName))
                    {
                        await githubManager.DownloadGithubModAsync(DataContainer.nowLocalGithubModAndVersionAndOriginalBoolAndUrl[fileNameAndVersionAndOriginalBoolAndUrl.Key].Item3,
                            new Version(DataContainer.installedGithubModInformationToCsvForUpdate.Find(n => n.GithubMod == fileName).LocalVersion),
                            DataContainer.downloadModsTemp, DataContainer.installedGithubModInformationToCsvForUpdate, fileName);
                    }
                }

                OrganizeDownloadFileStructure(DataContainer.downloadModsTemp, Settings.Instance.BeatSaberExeFolderPath);

                WriteCsv(DataContainer.githubModCsvPath, DataContainer.installedGithubModInformationToCsvForUpdate);
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
                await githubManager.DownloadGithubModAsync(a.GithubUrl, new Version("0.0.0"), DataContainer.downloadModsTemp, null, null);
            }

            OrganizeDownloadFileStructure(DataContainer.downloadModsTemp, DataContainer.downloadModsTemp);
        }
    }
}

