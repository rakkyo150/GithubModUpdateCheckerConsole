using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Utils
{
    internal class DataManager : IDataManager
    {
        public Dictionary<string, Version> GetLocalModFilesInfo(string pluginsFolderPath)
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

        public bool DetectMAModAndRemoveFromManagement(ModAssistantModInformation item, KeyValuePair<string, Version> fileAndVersion, ref List<ModAssistantModInformationCsv> detectedModAssistantModCsvList, out bool loopBreak)
        {
            loopBreak= false;
            bool pass = false;


            if (item.name == fileAndVersion.Key)
            {
                loopBreak = true;

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

            // プリセット機能が欲しい

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

            string githubUrl="p";
            bool finish = false;
            while (!finish)
            {
                Console.WriteLine("GithubのリポジトリのUrlを入力してください(検索したい場合は\"s\"を入力してください)");
                githubUrl = Console.ReadLine();
                if (githubUrl == "s")
                {
                    OpenUrl(githubUrl);
                }
                else
                {
                    finish = true;
                }
            }

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

        public void DetectMAModForUpdate(ModAssistantModInformation item, ref Dictionary<string, Tuple<bool, string>> githubModAndOriginalBoolAndUrl, ref List<GithubModInformationCsv> githubModInformationCsv)
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

        // https://docs.microsoft.com/ja-jp/dotnet/standard/io/how-to-copy-directories
        // 上書きする
        public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        // sourceDirFullPathはModが一時的に保存される場所、destDirFullPathはBeat Saberディレクトリと同じ構造のもの
        public async Task OrganizeDownloadFileStructure(string sourceDirFullPath,string destDirFullPath)
        {
            if (Directory.GetFiles(sourceDirFullPath).Length > 0)
            {
                // https://github.com/denpadokei/LocalModAssistant/blob/b0c119f7e32a35cd15ca2010f9dc50b8267183fe/LocalModAssistant/Models/MainViewDomain.cs
                foreach (var dllFileFullPath in Directory.EnumerateFiles(sourceDirFullPath, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    if (!Directory.Exists(Path.Combine(destDirFullPath, "Plugins")))
                    {
                        Directory.CreateDirectory(Path.Combine(destDirFullPath, "Plugins"));
                    }
                    try
                    {
                        var installPath = Path.Combine(destDirFullPath, "Plugins",Path.GetFileName(dllFileFullPath));
                        if (File.Exists(installPath))
                        {
                            File.Delete(installPath);
                        }
                        File.Move(dllFileFullPath, installPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                    }
                }
                foreach (var zipFileName in Directory.EnumerateFiles(sourceDirFullPath, "*.zip", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        using (var fs = File.Open(zipFileName, FileMode.Open))
                        using (var zip = new ZipArchive(fs))
                        {
                            foreach (var file in zip.Entries)
                            {
                                var installPath = Path.Combine(destDirFullPath, file.FullName);
                                if (File.Exists(installPath))
                                {
                                    File.Delete(installPath);
                                }
                            }
                            zip.ExtractToDirectory(destDirFullPath);
                        }
                        File.Delete(zipFileName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                    }
                }
            }
        }

        private Process OpenUrl(string url)
        {
            ProcessStartInfo pi = new ProcessStartInfo()
            {
                FileName = url,
                UseShellExecute = true,
            };

            return Process.Start(pi);
        }
    }
}
