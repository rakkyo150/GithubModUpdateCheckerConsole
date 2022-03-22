using CsvHelper;
using GithubModUpdateCheckerConsole;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using GithubModUpdateCheckerConsole.Interfaces;


string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
bool initialize = false;

IMainManager mainManager = new MainManager();

ConfigManager configManager = new ConfigManager();

if (!File.Exists(configFile))
{
    Console.WriteLine("There is not config.json");
    Console.WriteLine("Start Initialize");
    await mainManager.Initialize();
    initialize = true;
}

configManager.LoadConfigFile(configFile);

Console.WriteLine("Choose mode : [1] Check Update [2] Import csv");
string? mode=Console.ReadLine();

if (mode == "2")
{
    Console.WriteLine("Start importing csv");
    await mainManager.ImportCsv();
}
else
{
    Console.WriteLine("Start checking update");
    await mainManager.UpdateGithubModAsync();
}

Console.WriteLine("returnで終了します");
Console.ReadLine();



