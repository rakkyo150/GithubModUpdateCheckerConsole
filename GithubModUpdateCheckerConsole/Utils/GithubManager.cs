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

            var credential = new Credentials(Settings.Instance.OAuthToken);
            GitHubClient github = new GitHubClient(new ProductHeaderValue("GithubModUpdateChecker"));
            github.Credentials = credential;

            string tem = url.Replace("https://github.com/", "");
            int nextSlashPosition = tem.IndexOf('/');

            string owner = tem.Substring(0, nextSlashPosition);
            Console.WriteLine(owner);
            string name = tem.Substring(nextSlashPosition + 1);
            Console.WriteLine(name);

            Version CurrentVersion = new Version("1.0.0");

            var response = github.Repository.Release.GetLatest(owner, name);
            Console.WriteLine(response.Result.HtmlUrl);

            var latestVersion = new Version(response.Result.TagName[1..]); // remove `v` from `vW.X.Y.Z
            if (latestVersion > CurrentVersion)
            {
                foreach (var item in response.Result.Assets)
                {
                    await DownloadModAsync(item.BrowserDownloadUrl, item.Name);
                }
            }
        }

        public async Task<Version> GetGithubModLatestVersion(string url)
        {
            var credential = new Credentials(Settings.Instance.OAuthToken);
            GitHubClient github = new GitHubClient(new ProductHeaderValue("GithubModUpdateChecker"));
            github.Credentials = credential;

            string tem = url.Replace("https://github.com/", "");
            int nextSlashPosition = tem.IndexOf('/');

            string owner = tem.Substring(0, nextSlashPosition);
            Console.WriteLine(owner);
            string name = tem.Substring(nextSlashPosition + 1);
            Console.WriteLine(name);

            var response = github.Repository.Release.GetLatest(owner, name);
            var latestVersion = new Version(response.Result.TagName[1..]);

            return latestVersion;
        }

        // Based on https://qiita.com/thrzn41/items/2754bec8ebad97ecd7fd
        public async Task DownloadModAsync(string uri, string name)
        {
            using HttpClient httpClient = new();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri));
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var content = response.Content;
                using var stream = await content.ReadAsStreamAsync();
                using var fileStream = new FileStream($".\\{name}", FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}
