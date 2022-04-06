using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IGithubManager
    {
        void InputGithubModInformation(KeyValuePair<string, Version> fileAndVersion);

        /// <summary>
        /// 最新バージョンを取得、現在のバージョンよりも高いの場合はダウンロード
        /// </summary>
        /// <param name="url"></param>
        /// <param name="currentVersion"></param>
        /// <param name="destDirFullPath"></param>
        /// <returns></returns>
        Task DownloadGithubModAsync(string url, Version currentVersion, string destDirName);
        
        Task<Version> GetGithubModLatestVersion(string url);
    }
}
