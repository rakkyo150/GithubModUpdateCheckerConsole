using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IGithubManager
    {
        void InputGithubModInformation(KeyValuePair<string, Version> fileAndVersion, List<GithubModInformationCsv> githubModInformationToCsv);

        /// <summary>
        /// <para>リリースの情報を取得、リリースのバージョンが現在のバージョンよりも高い場合はダウンロード</para>
        /// 第４引数にはダウンロードしたバージョン情報を渡します(nullなら渡しません)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="currentVersion"></param>
        /// <param name="destDirName"></param>
        /// <param name="githubModInformationToCsv"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task DownloadGithubModAsync(string url, Version currentVersion, string destDirName, List<GithubModInformationCsv> githubModInformationToCsv, string fileName);

        Task<Version> GetGithubModLatestVersion(string url);
    }
}
