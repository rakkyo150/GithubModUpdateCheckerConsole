using System;
using System.Collections.Generic;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IDataManager
    {
        string GetGameVersion();

        public Dictionary<string, Version> GetLocalFilesInfo(string pluginsFolderPath);


    }
}
