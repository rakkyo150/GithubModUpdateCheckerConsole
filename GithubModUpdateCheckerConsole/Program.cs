using GithubModUpdateCheckerConsole;
using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.IO;


string downloadModsTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
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

Console.WriteLine("バックアップを作成します");
mainManager.Backup();

mainManager.CleanModsTemp(downloadModsTemp);
Console.WriteLine("モードを選んでください");
Console.WriteLine("[1] 通常アップデートチェック");
Console.WriteLine("[2] BSアップデート後のアップデートチェック");
Console.WriteLine("[3] csvからダウンロード");
string? mode = Console.ReadLine();

if(mode == "1")
{
    Console.WriteLine("通常アップデートチェックスタート");
    await mainManager.UpdateGithubModForUsualBSVersionAsync();
}
else if(mode == "2")
{
    Console.WriteLine("BSアップデート後のアップデートチェックスタート");
    await mainManager.UpdateGithubModForNewBSVersionAsync();
}
else
{
    Console.WriteLine("csvからダウンロードをスタート");
    await mainManager.ImportCsv();
}

Console.WriteLine("returnで終了します");
Console.ReadLine();



