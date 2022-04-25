using GithubModUpdateCheckerConsole;
using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.IO;


string downloadModsTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

IMainManager mainManager = new MainManager();
DataManager dataManager = new DataManager();
GithubManager githubManager = new GithubManager();

ConfigManager configManager = new ConfigManager();

if (!File.Exists(configFile))
{
    Console.WriteLine("There is not config.json");
    Console.WriteLine("Start Initialize");
    await mainManager.InitializeAsync();
}

configManager.LoadConfigFile(configFile);

Console.WriteLine("Tokenの確認をします");
await githubManager.CheckCredential();

Console.WriteLine("バックアップを作成します");
dataManager.Backup();


dataManager.CleanModsTemp(downloadModsTemp);
Console.WriteLine("モードを選んでください");
Console.WriteLine("[1] 通常アップデートチェック");
Console.WriteLine("[2] BSアップデート後のアップデートチェック");
Console.WriteLine("[3] csvからダウンロード");
string? mode = Console.ReadLine();

if (mode == "1")
{
    Console.WriteLine("通常アップデートチェックスタート");
    await mainManager.UpdateGithubModForUsualBSVersionAsync();
}
else if (mode == "2")
{
    Console.WriteLine("BSアップデート後のアップデートチェックスタート");
    await mainManager.UpdateGithubModForNewBSVersionAsync();
}
else
{
    Console.WriteLine("csvからダウンロードをスタート");
    await mainManager.ImportCsvAsync();
}

Console.WriteLine("returnで終了します");
Console.ReadLine();



