#nullable enable

using System;
using System.Threading.Tasks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Core;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.SceneManagement
{
    /// <summary>
    /// シーン管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// Advanced Scene Manager (ASM)をラップし、
    /// MVVMパターンでシーン遷移を管理します。
    /// </remarks>
    public interface ISceneManagementService : IService
    {
        #region Events

        /// <summary>
        /// シーンロード開始時に発生します。
        /// </summary>
        event EventHandler<SceneLoadEventArgs>? SceneLoadStarted;

        /// <summary>
        /// シーンロード完了時に発生します。
        /// </summary>
        event EventHandler<SceneLoadEventArgs>? SceneLoadCompleted;

        /// <summary>
        /// シーンロード失敗時に発生します。
        /// </summary>
        event EventHandler<SceneLoadErrorEventArgs>? SceneLoadFailed;

        #endregion

        #region Properties

        /// <summary>
        /// 現在ロード中かどうかを取得します。
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 現在のロード進捗を取得します（0.0～1.0）
        /// </summary>
        float LoadProgress { get; }

        #endregion

        #region Configuration

        /// <summary>
        /// シーンコレクションを登録します。
        /// </summary>
        /// <param name="startup">Startupシーンコレクション</param>
        /// <param name="mainMenu">MainMenuシーンコレクション</param>
        /// <param name="lobby">Lobbyシーンコレクション</param>
        /// <param name="settings">Settingsシーンコレクション</param>
        /// <param name="customization">Customizationシーンコレクション</param>
        /// <param name="match">Matchシーンコレクション</param>
        /// <param name="training">Trainingシーンコレクション</param>
        /// <param name="results">Resultsシーンコレクション</param>
        /// <param name="replay">Replayシーンコレクション</param>
        void RegisterSceneCollections(
            SceneCollection? startup,
            SceneCollection? mainMenu,
            SceneCollection? lobby,
            SceneCollection? settings,
            SceneCollection? customization,
            SceneCollection? match,
            SceneCollection? training,
            SceneCollection? results,
            SceneCollection? replay);

        #endregion

        #region High-Level Scene Operations

        /// <summary>
        /// メインメニューシーンをロードします。
        /// </summary>
        void LoadMainMenu();

        /// <summary>
        /// ロビーシーンをロードします。
        /// </summary>
        void LoadLobby();

        /// <summary>
        /// 設定シーンをロードします。
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// カスタマイゼーションシーンをロードします。
        /// </summary>
        void LoadCustomization();

        /// <summary>
        /// マッチシーンをロードします。
        /// </summary>
        void LoadMatch();

        /// <summary>
        /// トレーニングシーンをロードします。
        /// </summary>
        void LoadTraining();

        /// <summary>
        /// 結果シーンをロードします。
        /// </summary>
        void LoadResults();

        /// <summary>
        /// リプレイシーンをロードします。
        /// </summary>
        void LoadReplay();

        #endregion

        #region Low-Level Scene Operations

        /// <summary>
        /// シーンを開きます。
        /// </summary>
        /// <param name="scene">開くシーン</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>シーン操作</returns>
        SceneOperation OpenScene(Scene scene, bool useLoadingScreen = true);

        /// <summary>
        /// シーンコレクションを開きます。
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <param name="openAll">すべてのシーンを開くか</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>シーン操作</returns>
        SceneOperation OpenCollection(SceneCollection collection, bool openAll = false, bool useLoadingScreen = true);

        /// <summary>
        /// シーンを非同期で開きます。
        /// </summary>
        /// <param name="scene">開くシーン</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>完了を待機するTask</returns>
        Task OpenSceneAsync(Scene scene, bool useLoadingScreen = true);

        /// <summary>
        /// シーンコレクションを非同期で開きます。
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <param name="openAll">すべてのシーンを開くか</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>完了を待機するTask</returns>
        Task OpenCollectionAsync(SceneCollection collection, bool openAll = false, bool useLoadingScreen = true);

        /// <summary>
        /// すべてのシーンを閉じます。
        /// </summary>
        /// <returns>シーン操作</returns>
        SceneOperation CloseAll();

        /// <summary>
        /// シーンをプリロードします。
        /// </summary>
        /// <param name="scene">プリロードするシーン</param>
        void PreloadScene(Scene scene);

        /// <summary>
        /// プリロードされたシーンを破棄します。
        /// </summary>
        /// <param name="scene">破棄するシーン</param>
        void DiscardPreload(Scene scene);

        #endregion
    }

    /// <summary>
    /// シーンロードイベントの引数
    /// </summary>
    public class SceneLoadEventArgs : EventArgs
    {
        /// <summary>
        /// ロードされたシーン名
        /// </summary>
        public string SceneName { get; }

        /// <summary>
        /// ロードにかかった時間（秒）
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// SceneLoadEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="sceneName">ロードされたシーン名</param>
        /// <param name="duration">ロードにかかった時間（秒）</param>
        public SceneLoadEventArgs(string sceneName, float duration)
        {
            SceneName = sceneName;
            Duration = duration;
        }
    }

    /// <summary>
    /// シーンロードエラーイベントの引数
    /// </summary>
    public class SceneLoadErrorEventArgs : EventArgs
    {
        /// <summary>
        /// エラーが発生したシーン名
        /// </summary>
        public string SceneName { get; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 例外
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// SceneLoadErrorEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="sceneName">エラーが発生したシーン名</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <param name="exception">発生した例外（オプション）</param>
        public SceneLoadErrorEventArgs(string sceneName, string errorMessage, Exception? exception = null)
        {
            SceneName = sceneName;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}
