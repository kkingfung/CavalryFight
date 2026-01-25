#nullable enable

using System;
using System.Threading.Tasks;
using AdvancedSceneManager;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Loading;
using UnityEngine;

namespace CavalryFight.Services.SceneManagement
{
    /// <summary>
    /// シーン管理サービスの実装
    /// </summary>
    /// <remarks>
    /// Advanced Scene Manager (ASM)を使用してシーンを管理します。
    /// ViewModelから簡単にシーン遷移を実行できるようにします。
    /// </remarks>
    public class SceneManagementService : ISceneManagementService
    {
        #region Events

        /// <summary>
        /// シーンロード開始時に発生します。
        /// </summary>
        public event EventHandler<SceneLoadEventArgs>? SceneLoadStarted;

        /// <summary>
        /// シーンロード完了時に発生します。
        /// </summary>
        public event EventHandler<SceneLoadEventArgs>? SceneLoadCompleted;

        /// <summary>
        /// シーンロード失敗時に発生します。
        /// </summary>
        public event EventHandler<SceneLoadErrorEventArgs>? SceneLoadFailed;

        #endregion

        #region Fields

        private bool _isLoading;
        private float _loadProgress;
        private SceneOperation? _currentOperation;
        private float _loadStartTime;
        private string _currentSceneName = string.Empty;
        private ReturnDestination _returnDestination = ReturnDestination.MainMenu;

        // ゲームプレイシーンの遅延完了通知用
        private bool _waitingForGameplayReady;
        private bool _sceneLoadFinished;
        private float _sceneLoadDuration;
        private LoadingScreenBase? _activeLoadingScreen;

        // シーンコレクション参照
        private SceneCollection? _startupCollection;
        private SceneCollection? _mainMenuCollection;
        private SceneCollection? _lobbyCollection;
        private SceneCollection? _matchRoomCollection;
        private SceneCollection? _settingsCollection;
        private SceneCollection? _customizationCollection;
        private SceneCollection? _matchCollection;
        private SceneCollection? _trainingCollection;
        private SceneCollection? _resultsCollection;
        private SceneCollection? _replayCollection;
        private SceneCollection? _historyCollection;
        private SceneCollection? _huntingCollection;

        #endregion

        #region Properties

        /// <summary>
        /// 現在ロード中かどうかを取得します。
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// 現在のロード進捗を取得します（0.0～1.0）
        /// </summary>
        public float LoadProgress => _loadProgress;

        /// <summary>
        /// 設定画面からの戻り先を取得します。
        /// </summary>
        public ReturnDestination CurrentReturnDestination => _returnDestination;

        /// <summary>
        /// ゲームプレイシーンの準備完了待ちかどうかを取得します。
        /// </summary>
        public bool IsWaitingForGameplayReady => _waitingForGameplayReady;

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        /// <remarks>
        /// ServiceLocatorに登録された直後に呼び出されます。
        /// ASMのコールバックを登録し、シーン管理の準備を行います。
        /// </remarks>
        public void Initialize()
        {
            Debug.Log("[SceneManagementService] Initializing...");

            // ASMのコールバックを登録
            RegisterCallbacks();

            Debug.Log("[SceneManagementService] Initialized.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        /// <remarks>
        /// イベントハンドラをクリアし、現在のシーン操作を破棄します。
        /// </remarks>
        public void Dispose()
        {
            Debug.Log("[SceneManagementService] Disposing...");

            // ASMのコールバックを登録解除
            SceneManager.events.UnregisterCallback<SceneOpenPhaseEvent>(OnSceneOpenStarted, When.Before);
            SceneManager.events.UnregisterCallback<SceneOpenPhaseEvent>(OnSceneOpenCompleted, When.After);

            // イベントハンドラをクリア
            SceneLoadStarted = null;
            SceneLoadCompleted = null;
            SceneLoadFailed = null;

            _currentOperation = null;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// シーンコレクションを登録します。
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
            _startupCollection = startup;
            _mainMenuCollection = mainMenu;
            _lobbyCollection = lobby;
            _matchRoomCollection = matchRoom;
            _settingsCollection = settings;
            _customizationCollection = customization;
            _matchCollection = match;
            _trainingCollection = training;
            _resultsCollection = results;
            _replayCollection = replay;
            _historyCollection = history;
            _huntingCollection = hunting;

            Debug.Log("[SceneManagementService] Scene collections registered.");
        }

        #endregion

        #region High-Level Scene Operations

        /// <summary>
        /// メインメニューシーンをロードします。
        /// </summary>
        public void LoadMainMenu()
        {
            if (_mainMenuCollection == null)
            {
                Debug.LogError("[SceneManagementService] MainMenu collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("MainMenu", "MainMenu collection is not registered"));
                return;
            }

            try
            {
                OpenCollection(_mainMenuCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open MainMenu collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("MainMenu", ex.Message, ex));
            }
        }

        /// <summary>
        /// ロビーシーンをロードします。
        /// </summary>
        public void LoadLobby()
        {
            if (_lobbyCollection == null)
            {
                Debug.LogError("[SceneManagementService] Lobby collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Lobby", "Lobby collection is not registered"));
                return;
            }

            try
            {
                OpenCollection(_lobbyCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Lobby collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Lobby", ex.Message, ex));
            }
        }

        /// <summary>
        /// マッチルームシーンをロードします。
        /// </summary>
        public void LoadMatchRoom()
        {
            if (_matchRoomCollection == null)
            {
                Debug.LogError("[SceneManagementService] MatchRoom collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("MatchRoom", "MatchRoom collection is not registered"));
                return;
            }

            try
            {
                OpenCollection(_matchRoomCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open MatchRoom collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("MatchRoom", ex.Message, ex));
            }
        }

        /// <summary>
        /// 設定シーンをロードします。
        /// </summary>
        public void LoadSettings()
        {
            // デフォルトはメインメニューに戻る
            LoadSettingsWithReturn(ReturnDestination.MainMenu);
        }

        /// <summary>
        /// 設定シーンをロードします（戻り先を指定）。
        /// </summary>
        /// <param name="returnDestination">設定画面を閉じた時の戻り先</param>
        public void LoadSettingsWithReturn(ReturnDestination returnDestination)
        {
            if (_settingsCollection == null)
            {
                Debug.LogError("[SceneManagementService] Settings collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Settings", "Settings collection is not registered"));
                return;
            }

            _returnDestination = returnDestination;
            Debug.Log($"[SceneManagementService] Loading Settings with return destination: {returnDestination}");

            try
            {
                OpenCollection(_settingsCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Settings collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Settings", ex.Message, ex));
            }
        }

        /// <summary>
        /// 設定画面からの戻り先に遷移します。
        /// </summary>
        public void ReturnFromSettings()
        {
            Debug.Log($"[SceneManagementService] Returning from Settings to: {_returnDestination}");

            switch (_returnDestination)
            {
                case ReturnDestination.MainMenu:
                    LoadMainMenu();
                    break;
                case ReturnDestination.Training:
                    LoadTraining();
                    break;
                case ReturnDestination.Match:
                    LoadMatch();
                    break;
                case ReturnDestination.Lobby:
                    LoadLobby();
                    break;
                case ReturnDestination.Hunting:
                    LoadHunting();
                    break;
                default:
                    Debug.LogWarning($"[SceneManagementService] Unknown return destination: {_returnDestination}. Defaulting to MainMenu.");
                    LoadMainMenu();
                    break;
            }
        }

        /// <summary>
        /// カスタマイゼーションシーンをロードします。
        /// </summary>
        public void LoadCustomization()
        {
            if (_customizationCollection == null)
            {
                Debug.LogError("[SceneManagementService] Customization collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Customization", "Customization collection is not registered"));
                return;
            }

            OpenCollection(_customizationCollection);
        }

        /// <summary>
        /// マッチシーンをロードします。
        /// </summary>
        public void LoadMatch()
        {
            if (_matchCollection == null)
            {
                Debug.LogError("[SceneManagementService] Match collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Match", "Match collection is not registered"));
                return;
            }

            try
            {
                OpenCollectionWithGameplayWait(_matchCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Match collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Match", ex.Message, ex));
            }
        }

        /// <summary>
        /// トレーニングシーンをロードします。
        /// </summary>
        public void LoadTraining()
        {
            if (_trainingCollection == null)
            {
                Debug.LogError("[SceneManagementService] Training collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Training", "Training collection is not registered"));
                return;
            }

            try
            {
                OpenCollectionWithGameplayWait(_trainingCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Training collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Training", ex.Message, ex));
            }
        }

        /// <summary>
        /// 結果シーンをロードします。
        /// </summary>
        public void LoadResults()
        {
            if (_resultsCollection == null)
            {
                Debug.LogError("[SceneManagementService] Results collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Results", "Results collection is not registered"));
                return;
            }

            try
            {
                OpenCollection(_resultsCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Results collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Results", ex.Message, ex));
            }
        }

        /// <summary>
        /// リプレイシーンをロードします。
        /// </summary>
        public void LoadReplay()
        {
            if (_replayCollection == null)
            {
                Debug.LogError("[SceneManagementService] Replay collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Replay", "Replay collection is not registered"));
                return;
            }

            try
            {
                OpenCollection(_replayCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Replay collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Replay", ex.Message, ex));
            }
        }

        /// <summary>
        /// リプレイ履歴シーンをロードします。
        /// </summary>
        public void LoadHistory()
        {
            if (_historyCollection == null)
            {
                Debug.LogError("[SceneManagementService] History collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("History", "History collection is not registered"));
                return;
            }

            try
            {
                OpenCollection(_historyCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open History collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("History", ex.Message, ex));
            }
        }

        /// <summary>
        /// ハンティングシーンをロードします。
        /// </summary>
        public void LoadHunting()
        {
            if (_huntingCollection == null)
            {
                Debug.LogError("[SceneManagementService] Hunting collection is not registered!");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Hunting", "Hunting collection is not registered"));
                return;
            }

            try
            {
                OpenCollectionWithGameplayWait(_huntingCollection);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Failed to open Hunting collection: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs("Hunting", ex.Message, ex));
            }
        }

        /// <summary>
        /// ゲームプレイシーンの準備完了を通知します。
        /// </summary>
        /// <remarks>
        /// Match、Training、Huntingシーンで使用します。
        /// このメソッドが呼ばれるまでローディング画面は閉じません。
        /// </remarks>
        public void SignalGameplayReady()
        {
            if (!_waitingForGameplayReady)
            {
                Debug.LogWarning("[SceneManagementService] SignalGameplayReady called but not waiting for gameplay ready.");
                return;
            }

            Debug.Log("[SceneManagementService] Gameplay ready signal received. Closing loading screen...");

            _waitingForGameplayReady = false;

            // ローディング画面をフェードイン（閉じる）
            if (_activeLoadingScreen != null)
            {
                // コルーチンを開始してフェードイン
                LoadingScreenUtility.FadeIn(_activeLoadingScreen).StartCoroutine();
                Debug.Log("[SceneManagementService] Loading screen fade in (close) initiated.");
                _activeLoadingScreen = null;
            }
            else
            {
                Debug.LogWarning("[SceneManagementService] No active loading screen to close.");
            }

            // SceneLoadCompletedイベントを発火（保存していた値を使用）
            SceneLoadCompleted?.Invoke(this, new SceneLoadEventArgs(_currentSceneName, _sceneLoadDuration));
            Debug.Log($"[SceneManagementService] SceneLoadCompleted event fired for: {_currentSceneName}");
        }

        #endregion

        #region Low-Level Scene Operations

        /// <summary>
        /// シーンを開きます。
        /// </summary>
        /// <param name="scene">開くシーン</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>シーン操作</returns>
        public SceneOperation OpenScene(Scene scene, bool useLoadingScreen = true)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            _currentSceneName = scene.name;
            _loadStartTime = Time.realtimeSinceStartup;

            Debug.Log($"[SceneManagementService] Opening scene: {scene.name}");

            var operation = scene.Open();

            if (useLoadingScreen && LoadingScreenUtility.fade != null)
            {
                operation.With(LoadingScreenUtility.fade);
            }

            _currentOperation = operation;
            return operation;
        }

        /// <summary>
        /// シーンコレクションを開きます。
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <param name="openAll">すべてのシーンを開くか</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>シーン操作</returns>
        public SceneOperation OpenCollection(SceneCollection collection, bool openAll = false, bool useLoadingScreen = true)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _currentSceneName = collection.name;
            _loadStartTime = Time.realtimeSinceStartup;

            Debug.Log($"[SceneManagementService] Opening collection: {collection.name}");

            var operation = collection.Open(openAll);

            if (operation == null)
            {
                throw new InvalidOperationException($"Failed to open collection: {collection.name}");
            }

            // 進捗更新のコールバックを登録（operationが"frozen"になる前に登録する必要がある）
            operation.OnProgressChanged(OnProgressUpdated);

            if (useLoadingScreen && LoadingScreenUtility.fade != null)
            {
                operation.With(LoadingScreenUtility.fade);
            }

            _currentOperation = operation;
            return operation;
        }

        /// <summary>
        /// ゲームプレイシーン用にシーンコレクションを開きます。
        /// ローディング画面はSignalGameplayReady()が呼ばれるまで閉じません。
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <returns>シーン操作</returns>
        private SceneOperation OpenCollectionWithGameplayWait(SceneCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _currentSceneName = collection.name;
            _loadStartTime = Time.realtimeSinceStartup;
            _waitingForGameplayReady = true;
            _sceneLoadFinished = false;
            _activeLoadingScreen = null;

            Debug.Log($"[SceneManagementService] Opening gameplay collection with wait: {collection.name}");

            // ローディング画面をフェードアウト（画面を黒くする = 表示する）
            if (LoadingScreenUtility.fade != null)
            {
                var fadeAsync = LoadingScreenUtility.FadeOut();
                fadeAsync.OnComplete(loadingScreen =>
                {
                    _activeLoadingScreen = loadingScreen;
                    Debug.Log("[SceneManagementService] Loading screen fade out completed, stored reference");
                });
                Debug.Log("[SceneManagementService] Loading screen fade out initiated (manual)");
            }

            var operation = collection.Open(false);

            if (operation == null)
            {
                throw new InvalidOperationException($"Failed to open collection: {collection.name}");
            }

            // 進捗更新のコールバックを登録
            operation.OnProgressChanged(OnProgressUpdated);

            // ★重要: ローディング画面は手動制御するため、operation.With()は使わない
            // これにより、シーンロード完了時にASMが自動でローディング画面を閉じなくなる

            // シーンロード開始を通知
            _isLoading = true;
            SceneLoadStarted?.Invoke(this, new SceneLoadEventArgs(_currentSceneName, 0f));

            _currentOperation = operation;
            return operation;
        }

        /// <summary>
        /// シーンを非同期で開きます。
        /// </summary>
        /// <param name="scene">開くシーン</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>完了を待機するTask</returns>
        public async Task OpenSceneAsync(Scene scene, bool useLoadingScreen = true)
        {
            var operation = OpenScene(scene, useLoadingScreen);

            // 完了まで待機
            while (operation != null && operation.keepWaiting)
            {
                await Task.Yield();
            }

            Debug.Log($"[SceneManagementService] Scene opened: {scene.name}");
        }

        /// <summary>
        /// シーンコレクションを非同期で開きます。
        /// </summary>
        /// <param name="collection">開くシーンコレクション</param>
        /// <param name="openAll">すべてのシーンを開くか</param>
        /// <param name="useLoadingScreen">ローディング画面を使用するか</param>
        /// <returns>完了を待機するTask</returns>
        public async Task OpenCollectionAsync(SceneCollection collection, bool openAll = false, bool useLoadingScreen = true)
        {
            var operation = OpenCollection(collection, openAll, useLoadingScreen);

            // 完了まで待機
            while (operation != null && operation.keepWaiting)
            {
                await Task.Yield();
            }

            Debug.Log($"[SceneManagementService] Collection opened: {collection.name}");
        }

        /// <summary>
        /// すべてのシーンを閉じます。
        /// </summary>
        /// <returns>シーン操作</returns>
        public SceneOperation CloseAll()
        {
            Debug.Log("[SceneManagementService] Closing all scenes.");
            return SceneManager.runtime.CloseAll();
        }

        /// <summary>
        /// シーンをプリロードします。
        /// </summary>
        /// <param name="scene">プリロードするシーン</param>
        public void PreloadScene(Scene scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            Debug.Log($"[SceneManagementService] Preloading scene: {scene.name}");
            SceneManager.runtime.Preload(scene);
        }

        /// <summary>
        /// プリロードされたシーンを破棄します。
        /// </summary>
        /// <param name="scene">破棄するシーン</param>
        public void DiscardPreload(Scene scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            Debug.Log($"[SceneManagementService] Discarding preload: {scene.name}");
            scene.CancelPreload();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ASMのコールバックを登録します。
        /// </summary>
        private void RegisterCallbacks()
        {
            // シーンオープン開始時
            SceneManager.events.RegisterCallback<SceneOpenPhaseEvent>(OnSceneOpenStarted, When.Before);

            // シーンオープン完了時
            SceneManager.events.RegisterCallback<SceneOpenPhaseEvent>(OnSceneOpenCompleted, When.After);
        }

        /// <summary>
        /// シーンオープン開始時のコールバック
        /// </summary>
        /// <param name="e">シーンオープンフェーズイベントの引数</param>
        private void OnSceneOpenStarted(SceneOpenPhaseEvent e)
        {
            try
            {
                _isLoading = true;
                _loadProgress = 0f;

                Debug.Log($"[SceneManagementService] Scene open started: {_currentSceneName}");

                SceneLoadStarted?.Invoke(this, new SceneLoadEventArgs(_currentSceneName, 0f));

                // NOTE: e.operationのOnProgressChangedは、このタイミングでは"frozen"状態のため登録できません
                // 代わりにUpdate内でポーリングするか、他のコールバックで進捗を追跡します
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Error in scene open started callback: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs(_currentSceneName, ex.Message, ex));
            }
        }

        /// <summary>
        /// シーンオープン完了時のコールバック
        /// </summary>
        /// <param name="e">シーンオープンフェーズイベントの引数</param>
        private void OnSceneOpenCompleted(SceneOpenPhaseEvent e)
        {
            try
            {
                _isLoading = false;
                _loadProgress = 1f;

                float duration = Time.realtimeSinceStartup - _loadStartTime;

                // 操作がキャンセルされた場合はエラーとして扱う
                if (e.operation != null && e.operation.wasCancelled)
                {
                    Debug.LogWarning($"[SceneManagementService] Scene open cancelled: {_currentSceneName}");
                    _waitingForGameplayReady = false;
                    SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs(_currentSceneName, "Operation was cancelled"));
                    return;
                }

                Debug.Log($"[SceneManagementService] Scene open completed: {_currentSceneName} ({duration:F2}s)");

                // ゲームプレイシーンの準備完了待ちの場合は、SceneLoadCompletedを発火しない
                // ローディング画面も閉じない（SignalGameplayReady()が呼ばれるまで待つ）
                if (_waitingForGameplayReady)
                {
                    _sceneLoadFinished = true;
                    _sceneLoadDuration = duration;
                    Debug.Log($"[SceneManagementService] Gameplay scene loaded, waiting for SignalGameplayReady()...");
                    // ★ローディング画面は閉じない - SignalGameplayReady()で閉じる
                    return;
                }

                SceneLoadCompleted?.Invoke(this, new SceneLoadEventArgs(_currentSceneName, duration));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Error in scene open completed callback: {ex.Message}");
                SceneLoadFailed?.Invoke(this, new SceneLoadErrorEventArgs(_currentSceneName, ex.Message, ex));
            }
        }

        /// <summary>
        /// 進捗更新時のコールバック
        /// </summary>
        /// <param name="progress">現在の進捗（0.0～1.0）</param>
        private void OnProgressUpdated(float progress)
        {
            try
            {
                _loadProgress = progress;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManagementService] Error updating progress: {ex.Message}");
            }
        }

        #endregion
    }
}
