using GithubModUpdateCheckerConsole.Interfaces;
using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FileMode = System.IO.FileMode;

namespace GithubModUpdateCheckerConsole
{
    internal class GithubManager : IGithubManager
    {
        public async Task DownloadGithubModAsync(string url,Version currentVersion,string destDirFullPath)
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

                string latestVersionRaw = response.Result.TagName;

                int position = 0;
                foreach (char item in latestVersionRaw)
                {
                    if (item >= '0' && item <= '9')
                    {
                        break;
                    }
                    position++;
                }
                Version latestVersion = new Version(latestVersionRaw.Substring(position));

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
            catch(Exception ex)
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

            if(nextSlashPosition == -1)
            {
                Console.WriteLine("URLにミスがあるかもしれません");
                Console.WriteLine($"対象のURL : {url}");
                return new Version("0.0.0");
            }
            
            string owner = temp.Substring(0, nextSlashPosition);
            string name = temp.Substring(nextSlashPosition + 1);

            Version latestVersion;

            try
            {
                var response = github.Repository.Release.GetLatest(owner, name);
                string latestVersionRaw = response.Result.TagName;

                int position=0;
                foreach(char item in latestVersionRaw)
                {
                    if (item >= '0' && item <= '9')
                    {
                        break;
                    }
                    position++;
                }
                latestVersion = new Version(latestVersionRaw.Substring(position));
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
                    if(!Directory.Exists(destDirFullPath))
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
    }
}
