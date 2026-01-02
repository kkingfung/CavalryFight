#nullable enable

#if UNITY_EDITOR

using System;
using System.Threading.Tasks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Core;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.Development.MockData.MockServices
{
    /// <summary>
    /// モックのシーン管理サービス
    /// </summary>
    /// <remarks>
    /// 開発時に個別シーンをテストする際に使用します。
    /// 実際のシーン遷移は行わず、デバッグログを出力します。
    /// </remarks>
    public class MockSceneManagementService : ISceneManagementService
    {
        #region Events

        /// <summary>
        /// シーンロード開始時に発生します
        /// </summary>
        public event EventHandler<SceneLoadEventArgs>? SceneLoadStarted;

        /// <summary>
        /// シーンロード完了時に発生します
        /// </summary>
        public event EventHandler<SceneLoadEventArgs>? SceneLoadCompleted;

        /// <summary>
        /// シーンロード失敗時に発生します
        /// </summary>
        public event EventHandler<SceneLoadErrorEventArgs>? SceneLoadFailed;

        #endregion

        #region Properties

        /// <summary>
        /// 現在ロード中かどうかを取得します
        /// </summary>
        public bool IsLoading => false;

        /// <summary>
        /// 現在のロード進捗を取得します（0.0～1.0）
        /// </summary>
        public float LoadProgress => 1f;

        /// <summary>
        /// 設定画面からの戻り先を取得します
        /// </summary>
        public ReturnDestination CurrentReturnDestination { get; private set; } = ReturnDestination.MainMenu;

        #endregion

        #region Methods

        /// <summary>
        /// シーンコレクションを登録します
        /// </summary>
        /// <param name="startup">Startupシーンコレクション</param>
        /// <param name="mainMenu">MainMenuシーンコレクション</param>
        /// <param name="lobby">Lobbyシーンコレクション</param>
        /// <param name="matchRoom">MatchRoomシーンコレクション</param>
        /// <param name="settings">Settingsシーンコレクション</param>
        /// <param name="customization">Customizationシーンコレクション</param>
        /// <param name="match">Matchシーンコレクション</param>
        /// <param name="training">Trainingシーンコレクション</param>
        /// <param name="results">Resultsシーンコレクション</param>
        /// <param name="replay">Replayシーンコレクション</param>
        /// <param name="history">Historyシーンコレクション</param>
        /// <param name="hunting">Huntingシーンコレクション</param>
        public void RegisterSceneCollections(
            SceneCollection? startup,
            SceneCollection? mainMenu,
            SceneCollection? lobby,
            SceneCollection? matchRoom,
            SceneCollection? settings,
            SceneCollection? customization,
            SceneCollection? match,
            SceneCollection? training,
            SceneCollection? results,
            SceneCollection? replay,
            SceneCollection? history,
            SceneCollection? hunting)
        {
            Debug.Log("[MockSceneManagementService] RegisterSceneCollections called (no-op in mock)");
        }

        /// <summary>
        /// メインメニューシーンをロードします
        /// </summary>
        public void LoadMainMenu()
        {
            Debug.Log("[MockSceneManagementService] LoadMainMenu called");
        }

        /// <summary>
        /// ロビーシーンをロードします
        /// </summary>
        public void LoadLobby()
        {
            Debug.Log("[MockSceneManagementService] LoadLobby called");
        }

        /// <summary>
        /// マッチルームシーンをロードします
        /// </summary>
        public void LoadMatchRoom()
        {
            Debug.Log("[MockSceneManagementService] LoadMatchRoom called");
        }

        /// <summary>
        /// 設定シーンをロードします
        /// </summary>
        public void LoadSettings()
        {
            Debug.Log("[MockSceneManagementService] LoadSettings called");
        }

        /// <summary>
        /// 設定シーンをロードします（戻り先を指定）
        /// </summary>
        /// <param name="returnDestination">設定画面を閉じた時の戻り先</param>
        public void LoadSettingsWithReturn(ReturnDestination returnDestination)
        {
            CurrentReturnDestination = returnDestination;
            Debug.Log($"[MockSceneManagementService] LoadSettingsWithReturn called (return to: {returnDestination})");
        }

        /// <summary>
        /// 設定画面からの戻り先に遷移します
        /// </summary>
        public void ReturnFromSettings()
        {
            Debug.Log($"[MockSceneManagementService] ReturnFromSettings called (destination: {CurrentReturnDestination})");
        }

        /// <summary>
        /// カスタマイゼーションシーンをロードします
        /// </summary>
        public void LoadCustomization()
        {
            Debug.Log("[MockSceneManagementService] LoadCustomization called");
        }

        /// <summary>
        /// マッチシーンをロードします
        /// </summary>
        public void LoadMatch()
        {
            Debug.Log("[MockSceneManagementService] LoadMatch called");
        }

        /// <summary>
        /// トレーニングシーンをロードします
        /// </summary>
        public void LoadTraining()
        {
            Debug.Log("[MockSceneManagementService] LoadTraining called");
        }

        /// <summary>
        /// 結果シーンをロードします
        /// </summary>
        public void LoadResults()
        {
            Debug.Log("[MockSceneManagementService] LoadResults called");
        }

        /// <summary>
        /// リプレイシーンをロードします
        /// </summary>
        public void LoadReplay()
        {
            Debug.Log("[MockSceneManagementService] LoadReplay called");
        }

        /// <summary>
        /// リプレイ履歴シーンをロードします
        /// </summary>
        public void LoadHistory()
        {
            Debug.Log("[MockSceneManagementService] LoadHistory called");
        }

        /// <summary>
        /// ハンティングシーンをロードします
        /// </summary>
        public void LoadHunting()
        {
            Debug.Log("[MockSceneManagementService] LoadHunting called");
        }

        /// <summary>
        /// シーンを開きます
        /// </summary>
        /// <param name="scene">開くシーン</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>シーン操作</returns>
        public SceneOperation OpenScene(Scene scene, bool useLoadingScreen = true)
        {
            Debug.Log($"[MockSceneManagementService] OpenScene called: {scene?.name ?? "null"}");
            return null!;
        }

        /// <summary>
        /// シーンコレクションを開きます
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <param name="openAll">すべてのシーンを開くか</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>シーン操作</returns>
        public SceneOperation OpenCollection(SceneCollection collection, bool openAll = false, bool useLoadingScreen = true)
        {
            Debug.Log($"[MockSceneManagementService] OpenCollection called: {collection?.name ?? "null"}");
            return null!;
        }

        /// <summary>
        /// シーンを非同期で開きます
        /// </summary>
        /// <param name="scene">開くシーン</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>完了を待機するTask</returns>
        public Task OpenSceneAsync(Scene scene, bool useLoadingScreen = true)
        {
            Debug.Log($"[MockSceneManagementService] OpenSceneAsync called: {scene?.name ?? "null"}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// シーンコレクションを非同期で開きます
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <param name="openAll">すべてのシーンを開くか</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>完了を待機するTask</returns>
        public Task OpenCollectionAsync(SceneCollection collection, bool openAll = false, bool useLoadingScreen = true)
        {
            Debug.Log($"[MockSceneManagementService] OpenCollectionAsync called: {collection?.name ?? "null"}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// すべてのシーンを閉じます
        /// </summary>
        /// <returns>シーン操作</returns>
        public SceneOperation CloseAll()
        {
            Debug.Log("[MockSceneManagementService] CloseAll called");
            return null!;
        }

        /// <summary>
        /// シーンをプリロードします
        /// </summary>
        /// <param name="scene">プリロードするシーン</param>
        public void PreloadScene(Scene scene)
        {
            Debug.Log($"[MockSceneManagementService] PreloadScene called: {scene?.name ?? "null"}");
        }

        /// <summary>
        /// プリロードされたシーンを破棄します
        /// </summary>
        /// <param name="scene">破棄するシーン</param>
        public void DiscardPreload(Scene scene)
        {
            Debug.Log($"[MockSceneManagementService] DiscardPreload called: {scene?.name ?? "null"}");
        }

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[MockSceneManagementService] Initialize");
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[MockSceneManagementService] Dispose");
        }

        #endregion
    }
}

#endif
