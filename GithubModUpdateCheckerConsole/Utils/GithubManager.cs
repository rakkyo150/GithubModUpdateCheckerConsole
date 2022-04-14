using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Utils;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FileMode = System.IO.FileMode;

namespace GithubModUpdateCheckerConsole
{
    internal class GithubManager : IGithubManager
    {
        // Initializeでも使うので第二引数が必要
        public void InputGithubModInformation(KeyValuePair<string, Version> fileAndVersion, List<GithubModInformationCsv> githubModInformationToCsv)
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

            string githubUrl = "p";
            string githubModVersion = "0.0.0";
            bool inputUrlFinish = false;
            while (!inputUrlFinish)
            {
                Console.WriteLine("GithubのリポジトリのURLを入力してください");
                Console.WriteLine("Google検索したい場合は\"s\"を、URLが無いような場合は\"p\"を入力してください");
                githubUrl = Console.ReadLine();
                if (githubUrl == "s")
                {
                    try
                    {
                        string searchUrl = $"https://www.google.com/search?q={fileAndVersion.Key}";
                        ProcessStartInfo pi = new ProcessStartInfo()
                        {
                            FileName = searchUrl,
                            UseShellExecute = true,
                        };
                        Process.Start(pi);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Google検索できませんでした");
                    }
                }
                else if (githubUrl == "p")
                {
                    Console.WriteLine("最新のリリース情報を取得しません");
                    inputUrlFinish = true;
                }
                else
                {
                    Console.WriteLine("Githubの最新のリリースのタグ情報を取得します");

                    githubModVersion = GetGithubModLatestVersion(githubUrl).Result.ToString();
                    if (githubModVersion == new Version("0.0.0").ToString())
                    {
                        Console.WriteLine("リリース情報が取得できませんでした");
                        Console.WriteLine("URLを修正しますか？ [y/n]");
                        var a = Console.ReadLine();
                        if (a != "y")
                        {
                            inputUrlFinish = true;
                        }
                    }
                    else
                    {
                        inputUrlFinish = true;
                    }
                }
            }

            Console.WriteLine("GithubModData.csvにデータを追加します");
            Console.WriteLine("データを書き換えたい場合、このcsvを直接書き換えてください");

            var githubModInstance = new GithubModInformationCsv()
            {
                GithubMod = fileAndVersion.Key,
                LocalVersion = fileAndVersion.Value.ToString(),
                GithubVersion = githubModVersion,
                OriginalMod = originalMod,
                GithubUrl = githubUrl,
            };
            githubModInformationToCsv.Add(githubModInstance);
        }

        public async Task DownloadGithubModAsync(string url, Version currentVersion, string destDirFullPath, List<GithubModInformationCsv> githubModInformationToCsv, string fileName)
        {
            if (url == "p") return;

            var credential = new Credentials(Settings.Instance.OAuthToken);
            GitHubClient github = new GitHubClient(new ProductHeaderValue("GithubModUpdateChecker"));
            github.Credentials = credential;

            string temp = url.Replace("https://github.com/", "");
            int nextSlashPosition = temp.IndexOf('/');

            if (nextSlashPosition == -1)
            {
                Console.WriteLine("URLにミスがあります");
                Console.WriteLine($"対象のURL : {url}");
                return;
            }

            string owner = temp.Substring(0, nextSlashPosition);
            string name = temp.Substring(nextSlashPosition + 1);

            var response = github.Repository.Release.GetLatest(owner, name);

            try
            {
                string releaseBody = response.Result.Body;
                var releaseCreatedAt = response.Result.CreatedAt;
                DateTimeOffset now = DateTimeOffset.UtcNow;

                Version latestVersion = DetectVersion(response.Result.TagName);

                if (latestVersion == null)
                {
                    throw new Exception("バージョン情報の取得に失敗");
                }

                if (latestVersion > currentVersion)
                {
                    Console.WriteLine("****************************************************");
                    Console.WriteLine($"{owner}/{name}の最新バージョン:{latestVersion}が見つかりました");

                    Console.WriteLine("----------------------------------------------------");
                    if ((now - releaseCreatedAt).Days >= 1)
                    {
                        Console.WriteLine((now - releaseCreatedAt).Days + "日前にリリース");
                    }
                    else
                    {
                        Console.WriteLine((now - releaseCreatedAt).Hours + "時間" + (now - releaseCreatedAt).Minutes + "分前にリリース");
                    }
                    Console.WriteLine("リリースの説明");
                    Console.WriteLine(releaseBody);
                    Console.WriteLine("----------------------------------------------------");

                    bool downloadChoiceFinish = false;
                    while (!downloadChoiceFinish)
                    {
                        Console.WriteLine("ダウンロードしますか？ [y/n]");
                        Console.WriteLine("リポジトリを確認したい場合は\"r\"を入力してください");
                        string download = Console.ReadLine();
                        if (download == "r")
                        {
                            try
                            {
                                string searchUrl = url;
                                ProcessStartInfo pi = new ProcessStartInfo()
                                {
                                    FileName = searchUrl,
                                    UseShellExecute = true,
                                };
                                Process.Start(pi);

                                downloadChoiceFinish = false;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine("リポジトリが開けませんでした");
                            }
                        }
                        else if (download == "y")
                        {
                            foreach (var item in response.Result.Assets)
                            {
                                Console.WriteLine("ダウンロード中");
                                await DownloadModHelperAsync(item.BrowserDownloadUrl, item.Name, destDirFullPath);
                                Console.WriteLine("ダウンロード成功！");
                            }

                            if (githubModInformationToCsv != null)
                            {
                                if (githubModInformationToCsv.Find(n => n.GithubMod == fileName) == null)
                                {
                                    Console.WriteLine("csvのModのバージョンを更新できませんでした");
                                }
                                else
                                {
                                    githubModInformationToCsv.Find(n => n.GithubMod == fileName).LocalVersion = latestVersion.ToString();
                                }
                            }
                            downloadChoiceFinish = true;
                        }
                        else
                        {
                            Console.WriteLine("ダウンロードしません");
                            downloadChoiceFinish = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("リリースが見つかりませんでした");
                Console.WriteLine($"対象のリポジトリのURL : {url}");
            }
        }

        public async Task<Version> GetGithubModLatestVersion(string url)
        {
            if (url == "p") return new Version("0.0.0");

            var credential = new Credentials(Settings.Instance.OAuthToken);
            GitHubClient github = new GitHubClient(new ProductHeaderValue("GithubModUpdateChecker"));
            github.Credentials = credential;

            string temp = url.Replace("https://github.com/", "");
            int nextSlashPosition = temp.IndexOf('/');

            if (nextSlashPosition == -1)
            {
                Console.WriteLine("URLにミスがあるかもしれません");
                Console.WriteLine($"対象のURL : {url}");
                return new Version("0.0.0");
            }

            string owner = temp.Substring(0, nextSlashPosition);
            string name = temp.Substring(nextSlashPosition + 1);

            Version latestVersion = null;

            try
            {
                // プレリリースを取得する場合はGetAllしかないが、効率が悪いのでプレリリースには対応しません
                var response = github.Repository.Release.GetLatest(owner, name);
                latestVersion = DetectVersion(response.Result.TagName);

                if (latestVersion == null)
                {
                    throw new Exception("バージョン情報の取得に失敗");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("URLにミスがあるかもしれません");
                Console.WriteLine($"対象のURL : {url}");
                return new Version("0.0.0");
            }

            return latestVersion;
        }

        // Based on https://qiita.com/thrzn41/items/2754bec8ebad97ecd7fd
        public async Task DownloadModHelperAsync(string uri, string name, string destDirFullPath)
        {
            using HttpClient httpClient = new();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri));
            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using var content = response.Content;
                    using var stream = await content.ReadAsStreamAsync();
                    if (!Directory.Exists(destDirFullPath))
                    {
                        Directory.CreateDirectory(destDirFullPath);
                    }
                    string pluginDownloadPath = Path.Combine(destDirFullPath, name);
                    using var fileStream = new FileStream(pluginDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("URLにミスがあるかもしれません");
                Console.WriteLine($"対象のURL : {uri}");
            }
        }

        public Version DetectVersion(string tagName)
        {
            Version version = null;

            // バージョン情報が始まる位置を特定
            int position = 0;
            foreach (char item in tagName)
            {
                if (item >= '0' && item <= '9')
                {
                    break;
                }
                position++;
            }

            //　バージョン情報が終わる位置を特定
            for (int i = 0; i <= tagName.Length - position - 1; i++)
            {
                char versionDetector = tagName[position + i];
                if (!(versionDetector >= '0' && versionDetector <= '9') && versionDetector != '.')
                {
                    version = new Version(tagName.Substring(position, i));
                    break;
                }
                if (i == tagName.Length - position - 1)
                {
                    version = new Version(tagName.Substring(position));
                }
            }

            return version;
        }
    }
}
