using GithubModUpdateCheckerConsole.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GithubModUpdateCheckerConsole.Utils
{
    internal class ConfigManager : IConfigManager
    {
        public void LoadConfigFile(string path)
        {
            StreamReader re = new StreamReader(path);
            string _jsonStr = re.ReadToEnd();
            re.Close();
            dynamic _jsonDyn = JsonConvert.DeserializeObject<dynamic>(_jsonStr);

            if (_jsonDyn == null)
            {
                Console.WriteLine("There is something wrong with Config.json");
                Console.WriteLine("Remake Config.json");
                MakeConfigFile(path);
                LoadConfigFile(path);
                return;
            }

            if (_jsonDyn != null)
            {
                Settings.Instance.BeatSaberExeFolderPath = _jsonDyn.BeatSaberExeFolderPath;
                Settings.Instance.OAuthToken = _jsonDyn.OAuthToken;
            }
        }

        public void MakeConfigFile(string path)
        {
            Console.WriteLine("Input folder path where thre is Beat Saber.exe(ドラッグ＆ドロップでも可)");
            string? rawBsPath = Console.ReadLine();
            if (rawBsPath.Substring(0, 1) == "\"" && rawBsPath.Substring(rawBsPath.Length - 1) == "\"")
            {
                rawBsPath = rawBsPath.Trim('"');
            }
            Settings.Instance.BeatSaberExeFolderPath = rawBsPath;

            Console.WriteLine("Input Github OAuthToken");
            string? oAuthToken = Console.ReadLine();
            Settings.Instance.OAuthToken = oAuthToken;

            string _jsonFinish = JsonConvert.SerializeObject(Settings.Instance, Formatting.Indented);

            StreamWriter wr = new StreamWriter(new FileStream(path, FileMode.Create));
            wr.WriteLine(_jsonFinish);
            wr.Close();
        }
    }
}
