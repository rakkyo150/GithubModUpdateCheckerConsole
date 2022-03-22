using CsvHelper;
using GithubModUpdateCheckerConsole;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using GithubModUpdateCheckerConsole.Interfaces;


string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
string githubModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "GithubModData.csv");
// string modAssistantModCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "ModAssistantModData.csv");

IMainManager mainManager = new MainManager();

ConfigManager configManager = new ConfigManager();

if (!File.Exists(configFile))
{
    Console.WriteLine("There is not config.json");
    Console.WriteLine("Start Initialize");
    await mainManager.Initialize();
}

configManager.LoadConfigFile(configFile);

Console.WriteLine("Choose mode : [1] Check Update [2] Import csv");
string? mode=Console.ReadLine();

if (mode == "2")
{
    Console.WriteLine("Start importing csv");
    mainManager.ImportCsv();
}
else
{
    Console.WriteLine("Start checking update");
    mainManager.UpdateAsync();
}

Console.ReadLine();


/*
if (File.Exists(githubModCsvPath))
{
    using var reader = new StreamReader(githubModCsvPath);
    using var csv = new CsvReader(reader, new CultureInfo("ja-JP", false));
    var githubModInformationCsv = csv.GetRecords<GithubModInformationCsv>();

    foreach (var githubModInformation in githubModInformationCsv)
    {
        MainManager.ModAndUrl.Add(githubModInformation.GithubMod, githubModInformation.GithubUrl);
    }
}
*/

/*
var githubTask = new GithubTask();
await githubTask.GithubModDownloadAsync();
*/



