using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IGithubManager
    {
        void InputGithubModInformation(KeyValuePair<string, Version> fileAndVersion);

        /// <summary>
        /// リリースの情報を取得、リリースのバージョンが現在のバージョンよりも高い場合はダウンロード
        /// </summary>
        /// <param name="url"></param>
        /// <param name="currentVersion"></param>
        /// <param name="destDirFullPath"></param>
        /// <returns></returns>
        Task DownloadGithubModAsync(string url, Version currentVersion, string destDirName);
        
        Task<Version> GetGithubModLatestVersion(string url);
    }
}
