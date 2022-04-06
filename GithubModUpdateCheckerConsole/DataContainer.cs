﻿using GithubModUpdateCheckerConsole.Structure;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace GithubModUpdateCheckerConsole
{
    public static class DataContainer
    {
        public readonly static string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public readonly static string importCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImportGithubCsv");
        public readonly static string nowPluginsPath = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, "Plugins");
        public readonly static string downloadModsTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
        public readonly static string githubModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GithubModData.csv");
        public readonly static string mAModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ModAssistantModData.csv");
        public readonly static string backupFodlerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

        public static string nowGameVersion { get; set; } = "1.10.0";
        public static string oldGameVersion { get; set; }

        public static Dictionary<string, Version> nowLocalFilesInfoDictionary { get; set; }
        public static Dictionary<string, Version> oldLocalFilesInfoDictionary { get; set; }

        public static Dictionary<string, Tuple<bool, string>> nowLocalGithubModAndOriginalBoolAndUrl { get; set; } = new Dictionary<string, Tuple<bool, string>>();
        public static List<string> installedMAMod { get; set; } = new List<string>();

        public static ModAssistantModInformation[] modAssistantAllMods { get; set; }

        public static List<GithubModInformationCsv> initializeGithubModInformationCsv { get; set; } = new List<GithubModInformationCsv>();
        public static List<GithubModInformationCsv> updateGithubModInformationCsv { get; set; } = new List<GithubModInformationCsv>();
        public static List<MAModInformationCsv> detectedModAssistantModCsvList { get; set; } = new List<MAModInformationCsv>();
        public static List<MAModInformationCsv> updateModAssistantModCsvList { get; set; } = new List<MAModInformationCsv>();
    }
}
