using GithubModUpdateCheckerConsole.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GithubModUpdateCheckerConsole.Utils
{
    internal class DataManager : IDataManager
    {
        public string GetGameVersion()
        {
            string filename = Path.Combine(Settings.Instance.BeatSaberExeFolderPath, "Beat Saber_Data", "globalgamemanagers");
            using (var stream = File.OpenRead(filename))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                const string key = "public.app-category.games";
                int pos = 0;

                while (stream.Position < stream.Length && pos < key.Length)
                {
                    if (reader.ReadByte() == key[pos]) pos++;
                    else pos = 0;
                }

                if (stream.Position == stream.Length) // we went through the entire stream without finding the key
                    return null;

                while (stream.Position < stream.Length)
                {
                    var current = (char)reader.ReadByte();
                    if (char.IsDigit(current))
                        break;
                }

                var rewind = -sizeof(int) - sizeof(byte);
                stream.Seek(rewind, SeekOrigin.Current); // rewind to the string length

                var strlen = reader.ReadInt32();
                var strbytes = reader.ReadBytes(strlen);

                return Encoding.UTF8.GetString(strbytes);
            }
        }

        public Dictionary<string, Version> GetLocalFilesInfo(string pluginsFolderPath)
        {
            Console.WriteLine("Start Getting FileInfo");

            Dictionary<string, Version> filesInfo = new Dictionary<string, Version>();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(pluginsFolderPath);
            IEnumerable<System.IO.FileInfo> filesName = di.EnumerateFiles("*.dll", System.IO.SearchOption.AllDirectories);
            foreach (System.IO.FileInfo f in filesName)
            {
                string pluginPath = Path.Combine(pluginsFolderPath, f.Name);
                System.Diagnostics.FileVersionInfo vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(pluginPath);
                Version installedModVersion = new Version(vi.FileVersion);

                filesInfo.Add(f.Name, installedModVersion);
            }

            return filesInfo;
        }
    }
}
