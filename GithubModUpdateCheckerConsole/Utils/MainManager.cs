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
    internal class MainManager:IMainManager
    {
        IDataManager dataManager;
        IModAssistantManager modAssistantManager;
        IGithubManager githubManager;
        IConfigManager configManager;

        internal static ModAssistantModInformation[] modAssistantMod { get; set; }
        internal static Dictionary<string, string> ModAndUrl { get; set; } = new Dictionary<string, string>();

        private string gameVersion = "1.11.0";

        private string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        private string pluginsPath = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, "Plugins");

        private string githubModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "GithubModData.csv");
        private string modAssistantModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "ModAssistantModData.csv");

        private List<GithubModInformationCsv> githubModInformationCsv = new List<GithubModInformationCsv>();
        private List<ModAssistantModInformationCsv> modAssistantModCsv = new List<ModAssistantModInformationCsv>();

        internal MainManager()
        {
            dataManager = new DataManager();
            modAssistantManager = new ModAssistantManager();
            githubManager = new GithubManager();
            configManager=new ConfigManager();
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

            // 以下、Modの振り分け準備
            Console.WriteLine("Start GetVersion");
            gameVersion = dataManager.GetGameVersion();

            string modAssistantModInformationUrl = $"https://beatmods.com/api/v1/mod?status=approved&gameVersion={gameVersion}";

            Console.WriteLine("Start GetAllModAssistantMods");
            await modAssistantManager.GetAllModAssistantMods(modAssistantModInformationUrl);

            Dictionary<string,Version> localFilesInfoDictionary=dataManager.GetLocalFilesInfo(pluginsPath);

            // 以下、Modの振り分け、GithubModの情報入力とcsvへの登録
            foreach (KeyValuePair<string,Version> fileAndVersion in localFilesInfoDictionary)
            {
                int modAssistantModCount = modAssistantModCsv.Count;
                foreach (var item in modAssistantMod)
                {
                    if (item.name == fileAndVersion.Key)
                    {
                        Version modAssistantModVersion = new Version(item.version);
                        if (modAssistantModVersion >= fileAndVersion.Value)
                        {
                            Console.WriteLine(item.name + "はModAssistantにあるので無視します");

                            // GUIアプリ作る時、ここの操作のUIは必要そう
                            Console.WriteLine("オリジナルを使用していない場合、手動で追加してください");

                            Console.WriteLine("ModAssistantModData.csvに追加します");

                            var modAssistantCsvInstance = new ModAssistantModInformationCsv()
                            {
                                ModAssistantMod = fileAndVersion.Key,
                                LocalVersion = fileAndVersion.Value.ToString(),
                                ModAssistantVersion = item.version,
                            };
                            modAssistantModCsv.Add(modAssistantCsvInstance);
                        }

                        break;
                    }
                }
                if(modAssistantModCount > modAssistantModCsv.Count)
                {
                    using var writer2 = new StreamWriter(modAssistantModCsvPath, false);
                    using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                    csv2.WriteRecords(modAssistantModCsv);
                    break;
                }


                Console.WriteLine("オリジナルModですか？ [y/n]");
                var ok = Console.ReadLine();
                Console.WriteLine(ok);
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
                    OriginalMod = true,
                    GithubUrl = githubUrl,
                };
                githubModInformationCsv.Add(githubModInstance);
            }

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"));
            }

            using var writer1 = new StreamWriter(githubModCsvPath, false);
            using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
            csv1.WriteRecords(githubModInformationCsv);
        }

        public async Task UpdateAsync()
        {
            Console.WriteLine("Start GetVersion");
            gameVersion = dataManager.GetGameVersion();
            string modAssistantModInformationUrl = $"https://beatmods.com/api/v1/mod?status=approved&gameVersion={gameVersion}";

            Console.WriteLine("Start GetAllModAssistantMods");
            await modAssistantManager.GetAllModAssistantMods(modAssistantModInformationUrl);

            Dictionary<string, Version> localFilesInfoDictionary = dataManager.GetLocalFilesInfo(pluginsPath);

            if (File.Exists(githubModCsvPath))
            {
                using var reader = new StreamReader(githubModCsvPath);
                using var csv = new CsvReader(reader, new CultureInfo("ja-JP", false));
                var githubModInformationEnum = csv.GetRecords<GithubModInformationCsv>();

                foreach (var githubModInformation in githubModInformationEnum)
                {
                    MainManager.ModAndUrl.Add(githubModInformation.GithubMod, githubModInformation.GithubUrl);
                }

                // 以下、前回のデータとの差分を取得して必要事項を補充
                foreach (KeyValuePair<string,Version> fileAndVersion in localFilesInfoDictionary)
                {
                    if(!MainManager.ModAndUrl.ContainsKey(fileAndVersion.Key))
                    {
                        int modAssistantModCount = modAssistantModCsv.Count;
                        foreach (var item in modAssistantMod)
                        {
                            if (item.name == fileAndVersion.Key)
                            {
                                Version modAssistantModVersion = new Version(item.version);
                                if (modAssistantModVersion >= fileAndVersion.Value)
                                {
                                    Console.WriteLine(item.name + "はModAssistantにあるので無視します");

                                    // GUIアプリ作る時、ここの操作のUIは必要そう
                                    Console.WriteLine("オリジナルを使用していない場合、手動で追加してください");

                                    Console.WriteLine("ModAssistantModData.csvに追加します");

                                    var modAssistantCsvInstance = new ModAssistantModInformationCsv()
                                    {
                                        ModAssistantMod = fileAndVersion.Key,
                                        LocalVersion = fileAndVersion.Value.ToString(),
                                        ModAssistantVersion = item.version,
                                    };
                                    modAssistantModCsv.Add(modAssistantCsvInstance);
                                }

                                break;
                            }
                        }
                        if (modAssistantModCount > modAssistantModCsv.Count)
                        {
                            using var writer2 = new StreamWriter(modAssistantModCsvPath, false);
                            using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                            csv2.WriteRecords(modAssistantModCsv);
                            break;
                        }


                        Console.WriteLine("オリジナルModですか？ [y/n]");
                        var ok = Console.ReadLine();
                        Console.WriteLine(ok);
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
                            OriginalMod = true,
                            GithubUrl = githubUrl,
                        };
                        githubModInformationCsv.Add(githubModInstance);

                        MainManager.ModAndUrl.Add(githubModInstance.GithubMod, githubModInstance.GithubUrl);

                        foreach(var mod in githubModInformationEnum)
                        {
                            githubModInformationCsv.Add(githubModInstance);
                        }

                        using var writer1 = new StreamWriter(githubModCsvPath, false);
                        using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
                        csv1.WriteRecords(githubModInformationCsv);
                    }
                }
            }
            else
            {
                Dictionary<string, Version> localFilesInfoDictionary2 = dataManager.GetLocalFilesInfo(pluginsPath);
                // 以下、Modの振り分け、GithubModの情報入力とcsvへの登録
                foreach (KeyValuePair<string,Version> fileAndVersion in localFilesInfoDictionary2)
                {
                    int modAssistantModCount = modAssistantModCsv.Count;
                    foreach (var item in modAssistantMod)
                    {
                        if (item.name == fileAndVersion.Key)
                        {
                            Version modAssistantModVersion = new Version(item.version);
                            if (modAssistantModVersion >= fileAndVersion.Value)
                            {
                                Console.WriteLine(item.name + "はModAssistantにあるので無視します");

                                // GUIアプリ作る時、ここの操作のUIは必要そう
                                Console.WriteLine("オリジナルを使用していない場合、手動で追加してください");

                                Console.WriteLine("ModAssistantModData.csvに追加します");

                                var modAssistantCsvInstance = new ModAssistantModInformationCsv()
                                {
                                    ModAssistantMod = fileAndVersion.Key,
                                    LocalVersion = fileAndVersion.Value.ToString(),
                                    ModAssistantVersion = item.version,
                                };
                                modAssistantModCsv.Add(modAssistantCsvInstance);
                            }

                            break;
                        }
                    }
                    if (modAssistantModCount > modAssistantModCsv.Count)
                    {
                        using var writer2 = new StreamWriter(modAssistantModCsvPath, false);
                        using var csv2 = new CsvWriter(writer2, new CultureInfo("ja-JP", false));
                        csv2.WriteRecords(modAssistantModCsv);
                        break;
                    }


                    Console.WriteLine("オリジナルModですか？ [y/n]");
                    var ok = Console.ReadLine();
                    Console.WriteLine(ok);
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
                        OriginalMod = true,
                        GithubUrl = githubUrl,
                    };
                    githubModInformationCsv.Add(githubModInstance);

                    MainManager.ModAndUrl.Add(githubModInstance.GithubMod, githubModInstance.GithubUrl);
                }

                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"));
                }

                using var writer1 = new StreamWriter(githubModCsvPath, false);
                using var csv1 = new CsvWriter(writer1, new CultureInfo("ja-JP", false));
                csv1.WriteRecords(githubModInformationCsv);
            }

            // ModAssistanに登録されているようになっている場合
            foreach(var a in modAssistantMod)
            {
                if (MainManager.ModAndUrl.ContainsKey(a.name))
                {
                    if (localFilesInfoDictionary[a.name]<=new Version(a.version))
                    {
                        Console.WriteLine($"{a.name}がModAssistantにあります");
                        Console.WriteLine($"{a.name}をModAssistantで管理しますか? [y/n]");
                        string? response=Console.ReadLine();
                        if (response == "y")
                        {
                            GithubModInformationCsv removeItem=null;
                            
                            using var reader = new StreamReader(githubModCsvPath);
                            using var csv = new CsvReader(reader, new CultureInfo("ja-JP", false));
                            var githubModInformationEnum = csv.GetRecords<GithubModInformationCsv>();
                            foreach (var githubModInformation in githubModInformationEnum)
                            {
                                if (githubModInformation.GithubMod == a.name)
                                {
                                    removeItem = githubModInformation;
                                }
                            }
                            if(removeItem != null)
                            {
                                githubModInformationCsv.Remove(removeItem);
                                var modAssistantModInstance = new ModAssistantModInformationCsv()
                                {
                                    ModAssistantMod=a.name,
                                    LocalVersion=localFilesInfoDictionary[a.name].ToString(),
                                    ModAssistantVersion=a.version,
                                };
                                modAssistantModCsv.Add(modAssistantModInstance);
                            }
                            MainManager.ModAndUrl.Remove(a.name);
                        }
                    }
                }
            }

            foreach(KeyValuePair<string,Version> fileAndVersion in localFilesInfoDictionary)
            {
                string pluginPath = Path.Combine(pluginsPath, fileAndVersion.Key);

                System.Diagnostics.FileVersionInfo vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(pluginPath);

                var latestVersion = githubManager.GetGithubModLatestVersion(MainManager.ModAndUrl[fileAndVersion.Key]).Result;
                
                if (latestVersion > new Version(vi.FileVersion))
                {
                    await githubManager.GithubModDownloadAsync(MainManager.ModAndUrl[fileAndVersion.Key]);
                }
            }

            using var writer3 = new StreamWriter(githubModCsvPath, false);
            using var csv3 = new CsvWriter(writer3, new CultureInfo("ja-JP", false));
            csv3.WriteRecords(githubModInformationCsv);
            using var writer4 = new StreamWriter(modAssistantModCsvPath, false);
            using var csv4 = new CsvWriter(writer4, new CultureInfo("ja-JP", false));
            csv4.WriteRecords(modAssistantModCsv);
        }

        public void ImportCsv()
        {

        }
    }
}

