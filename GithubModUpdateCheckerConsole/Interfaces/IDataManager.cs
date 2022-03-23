using GithubModUpdateCheckerConsole.Structure;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    public interface IDataManager
    {
        Dictionary<string, Version> GetLocalFilesInfo(string pluginsFolderPath);

        bool DetectModAssistantModAndRemoveFromManagement(ModAssistantModInformation item, KeyValuePair<string, Version> fileAndVersion, ref List<ModAssistantModInformationCsv> detectedModAssistantModCsvList, out bool loopBreak);

        void InputGithubModInformation(IGithubManager githubManager, KeyValuePair<string, Version> fileAndVersion, ref List<GithubModInformationCsv> githubModInformationCsv);

        void DetectModAssistantModForUpdate(ModAssistantModInformation item, ref Dictionary<string, Tuple<bool, string>> githubModAndOriginalBoolAndUrl, ref List<GithubModInformationCsv> githubModInformationCsv);

        void ManageLocalPluginsDiff(Dictionary<string, Version> fileAndVersion, ModAssistantModInformation[] modAssistantAllMods, IGithubManager githubManager,
            ref Dictionary<string, Tuple<bool, string>> githubModAndOriginalBoolAndUrl, ref List<GithubModInformationCsv> githubModInformationCsv);

        void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs);

        Task OrganizeDownloadFileStructure(string sourceDirFullPath, string destDirFullPath);
    }
}
