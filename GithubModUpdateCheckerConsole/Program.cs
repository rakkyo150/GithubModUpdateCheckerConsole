using GithubModUpdateCheckerConsole;
using GithubModUpdateCheckerConsole.Interfaces;
using GithubModUpdateCheckerConsole.Utils;
using System;
using System.Diagnostics;
using System.IO;


string downloadModsTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
bool update;

IMainManager mainManager = new MainManager();
DataManager dataManager = new DataManager();
ConfigManager configManager = new ConfigManager();
GithubManager githubManager = new GithubManager();

if (!File.Exists(configFile))
{
    Console.WriteLine("There is not config.json");
    Console.WriteLine("Start Initialize");
    await mainManager.InitializeAsync();
}

configManager.LoadConfigFile(configFile);

Console.WriteLine("Tokenの確認をします");
await githubManager.CheckCredential();

Console.WriteLine("自分自身の更新バージョンがないか確認します");
update=await githubManager.CheckNewVersionAndDowonload();

if (update)
{
    Console.WriteLine("Updaterに更新を反映します");
    dataManager.UpdateUpdater();

    Console.WriteLine("Enterで本体の更新を反映");
    Console.ReadLine();

    ProcessStartInfo processStartInfo = new ProcessStartInfo();
    processStartInfo.Arguments = DataContainer.latestCheckerVersion.ToString();
    processStartInfo.FileName = Path.Combine(Environment.CurrentDirectory, "Updater.exe");
    Process process = Process.Start(processStartInfo);
    Environment.Exit(0);
}

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



