using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    internal interface IMainManager
    {
        /// <summary>
        /// config.jsonの作成、ModAssistantとGithubのModの振り分け、GithubのModの情報を入力、csvの作成
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// 前回実行時のデータがなければイニシャライズ、前回実行時との差分を取得、ModAssistantとGithubのModを振り分け、GithubのModの情報を入力、最新バージョンのModをインストール
        /// </summary>
        /// <returns></returns>
        Task UpdateGithubModForUsualBSVersionAsync();

        /// <summary>
        /// <para>前回実行時のデータがなければイニシャライズ、旧プラグインフォルダを選択、更新されていたらダウンロード</para>
        /// <para>ModAssistantの情報は取得しないしcsvの更新もしないので注意</para>
        /// </summary>
        /// <returns></returns>
        Task UpdateGithubModForNewBSVersionAsync();


        /// <summary>
        /// csvに書かれているModをModsTempにダウンロード
        /// </summary>
        /// <returns></returns>
        Task ImportCsvAsync();
    }
}
