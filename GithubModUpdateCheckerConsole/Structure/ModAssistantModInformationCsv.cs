using CsvHelper.Configuration.Attributes;
using System;

namespace GithubModUpdateCheckerConsole.Structure
{
    public class ModAssistantModInformationCsv
    {
        [Index(0)]
        public string ModAssistantMod { get; set; }
        [Index(1)]
        public string LocalVersion { get; set; }
        [Index(2)]
        public string ModAssistantVersion { get; set; }
    }
}
