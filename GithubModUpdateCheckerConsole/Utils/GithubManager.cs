using GithubModUpdateCheckerConsole.Interfaces;
using Octokit;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FileMode = System.IO.FileMode;

namespace GithubModUpdateCheckerConsole
{
    internal class GithubManager : IGithubManager
    {
        public async Task GithubModDownloadAsync(string url)
        {
            if (url == "p") return;

            var credential = new Credentials(Settings.Instance.OAuthToken);
            GitHubClient github = new GitHubClient(new ProductHeaderValue("GithubModUpdateChecker"));
            github.Credentials = credential;

            string tem = url.Replace("https://github.com/", "");
            int nextSlashPosition = tem.IndexOf('/');

            string owner = tem.Substring(0, nextSlashPosition);
            string name = tem.Substring(nextSlashPosition + 1);

            Version CurrentVersion = new Version("1.0.0");

            var response = github.Repository.Release.GetLatest(owner, name);

            var latestVersion = new Version(response.Result.TagName[..].Replace("v", ""));
            if (latestVersion > CurrentVersion)
            {
                Console.WriteLine($"{owner}/{name}の最新バージョン:{latestVersion}が見つかりました");
                Console.WriteLine("ダウンロードしますか？ [y/n]");
                string download = Console.ReadLine();

                if (download == "y")
                {
                    foreach (var item in response.Result.Assets)
                    {
                        Console.WriteLine("ダウンロード中");
                        await DownloadModAsync(item.BrowserDownloadUrl, item.Name);
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

            string tem = url.Replace("https://github.com/", "");
            int nextSlashPosition = tem.IndexOf('/');

            string owner = tem.Substring(0, nextSlashPosition);
            string name = tem.Substring(nextSlashPosition + 1);

            Version latestVersion;

            try
            {
                var response = github.Repository.Release.GetLatest(owner, name);
                latestVersion = new Version(response.Result.TagName.Replace("v", ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new Version("0.0.0");
            }

            return latestVersion;
        }

        // Based on https://qiita.com/thrzn41/items/2754bec8ebad97ecd7fd
        public async Task DownloadModAsync(string uri, string name)
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
                    string pluginDownloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", name);
                    using var fileStream = new FileStream(pluginDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
