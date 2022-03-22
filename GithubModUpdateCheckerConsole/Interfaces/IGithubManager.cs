using System;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IGithubManager
    {
        Task GithubModDownloadAsync(string url,Version currentVersion);
        Task<Version> GetGithubModLatestVersion(string url);
    }
}
