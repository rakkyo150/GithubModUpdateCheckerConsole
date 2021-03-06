using System.Collections.Generic;

namespace GithubModUpdateCheckerConsole.Utils
{

    public class ModAssistantModInformation
    {
        public string name;
        public string version;
        public string gameVersion;
        public string _id;
        public string status;
        public string authorId;
        public string uploadedDate;
        public string updatedDate;
        public Author author;
        public string description;
        public string link;
        public string category;
        public DownloadLink[] downloads;
        public bool required;
        public Dependency[] dependencies;
        public List<ModAssistantModInformation> Dependents = new List<ModAssistantModInformation>();

        public class Author
        {
            public string _id;
            public string username;
            public string lastLogin;
        }

        public class DownloadLink
        {
            public string type;
            public string url;
            public FileHashes[] hashMd5;
        }

        public class FileHashes
        {
            public string hash;
            public string file;
        }

        public class Dependency
        {
            public string name;
            public string _id;
            public ModAssistantModInformation Mod;
        }
    }
}
