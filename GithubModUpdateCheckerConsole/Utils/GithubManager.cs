using GithubModUpdateCheckerConsole.Interfaces;
using Octokit;
using System;
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

            var latestVersion = new Version(response.Result.TagName[..].Replace("v", ""));
            if (latestVersion > currentVersion)
            {
                Console.WriteLine($"{owner}/{name}の最新バージョン:{latestVersion}が見つかりました");
                Console.WriteLine("ダウンロードしますか？ [y/n]");
                string download = Console.ReadLine();

                if (download == "y")
                {
                    foreach (var item in response.Result.Assets)
                    {
                        Console.WriteLine("ダウンロード中");
                        await DownloadModHelperAsync(item.BrowserDownloadUrl, item.Name, destDirFullPath);
                        Console.WriteLine("ダウンロード成功！");
                    }
                }
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
                Console.WriteLine("URLにミスがあります");
                Console.WriteLine($"対象のURL : {url}");
                return new Version("0.0.0");
            }
            
            string owner = temp.Substring(0, nextSlashPosition);
            string name = temp.Substring(nextSlashPosition + 1);

            Version latestVersion;

            try
            {
                var response = github.Repository.Release.GetLatest(owner, name);
                latestVersion = new Version(response.Result.TagName.Replace("v", ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
