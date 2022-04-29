// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

String[] a = Environment.GetCommandLineArgs();


/*
foreach (string arg in a)
{
    Console.WriteLine(arg);
}
*/

string downloadPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,a[1]);
Console.WriteLine(downloadPath);
if (Directory.Exists(downloadPath))
{
    DirectoryInfo dir = new DirectoryInfo(downloadPath);

    FileInfo[] files = dir.GetFiles();
    foreach (FileInfo file in files)
    {
        string tempPath = Path.Combine(Environment.CurrentDirectory, file.Name);
        file.CopyTo(tempPath, true);
    }
    Console.WriteLine("本体のアップデート完了");
}
else
{
    Console.WriteLine("本体のアップデートができませんでした");
    Console.WriteLine("本体のアップデートは手動でお願いします");
}

Console.WriteLine("Enterで再起動します");
Console.ReadLine();
ProcessStartInfo processStartInfo = new ProcessStartInfo();
processStartInfo.FileName = Path.Combine(Environment.CurrentDirectory, "GithubModUpdateCheckerConsole.exe");
Process process=Process.Start(processStartInfo);
Environment.Exit(0);