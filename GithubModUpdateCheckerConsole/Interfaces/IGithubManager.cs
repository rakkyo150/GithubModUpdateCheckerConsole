﻿using System;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IGithubManager
    {
        Task DownloadGithubModAsync(string url,Version currentVersion, string destDirName);
        Task<Version> GetGithubModLatestVersion(string url);
    }
}
