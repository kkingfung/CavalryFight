#nullable enable

using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using System;
using UnityEngine;

namespace CavalryFight.Services.GameState
{
    /// <summary>
    /// ゲーム状態管理サービス実装
    /// </summary>
    public class GameStateService : IGameStateService
    {
        #region Fields

        private ISceneManagementService? _sceneService;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のゲーム状態
        /// </summary>
        public GameState CurrentState { get; private set; }

        /// <summary>
        /// 前のゲーム状態
        /// </summary>
        public GameState? PreviousState { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// ゲーム状態が変更されたときに発生
        /// </summary>
        public event EventHandler<GameStateChangedEventArgs>? StateChanged;

        #endregion

        #region IService Implementation

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[GameStateService] Initializing...");

            // SceneManagementServiceを取得
            _sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (_sceneService == null)
            {
                Debug.LogError("[GameStateService] SceneManagementService not found! Scene transitions will not work.");
            }

            // 初期状態を設定
            CurrentState = GameState.Initializing;
            PreviousState = null;

            Debug.Log("[GameStateService] Initialized.");
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[GameStateService] Disposing...");

            StateChanged = null;

            Debug.Log("[GameStateService] Disposed.");
        }

        #endregion

        #region State Transitions

        /// <summary>
        /// メインメニューへ遷移
        /// </summary>
        /// <remarks>
        /// 状態変更とシーンロードを行います。
        /// </remarks>
        public void TransitionToMainMenu()
        {
            if (!CanTransitionTo(GameState.MainMenu))
            {
                Debug.LogWarning($"[GameStateService] Cannot transition from {CurrentState} to MainMenu.");
                return;
            }

            ChangeState(GameState.MainMenu);
            _sceneService?.LoadMainMenu();
        }

        /// <summary>
        /// ロビーへ遷移
        /// </summary>
        /// <remarks>
        /// 状態変更とシーンロードを行います。
        /// </remarks>
        public void TransitionToLobby()
        {
            if (!CanTransitionTo(GameState.Lobby))
            {
                Debug.LogWarning($"[GameStateService] Cannot transition from {CurrentState} to Lobby.");
                return;
            }

            ChangeState(GameState.Lobby);
            _sceneService?.LoadLobby();
        }

        /// <summary>
        /// マッチへ遷移
        /// </summary>
        /// <remarks>
        /// 状態変更とシーンロードを行います。
        /// </remarks>
        public void TransitionToMatch()
        {
            if (!CanTransitionTo(GameState.Match))
            {
                Debug.LogWarning($"[GameStateService] Cannot transition from {CurrentState} to Match.");
                return;
            }

            ChangeState(GameState.Match);
            _sceneService?.LoadMatch();
        }

        /// <summary>
        /// 結果画面へ遷移
        /// </summary>
        /// <remarks>
        /// 状態変更とシーンロードを行います。
        /// </remarks>
        public void TransitionToResults()
        {
            if (!CanTransitionTo(GameState.Results))
            {
                Debug.LogWarning($"[GameStateService] Cannot transition from {CurrentState} to Results.");
                return;
            }

            ChangeState(GameState.Results);
            _sceneService?.LoadResults();
        }

        /// <summary>
        /// リプレイへ遷移
        /// </summary>
        /// <remarks>
        /// 状態変更とシーンロードを行います。
        /// </remarks>
        public void TransitionToReplay()
        {
            if (!CanTransitionTo(GameState.Replay))
            {
                Debug.LogWarning($"[GameStateService] Cannot transition from {CurrentState} to Replay.");
                return;
            }

            ChangeState(GameState.Replay);
            _sceneService?.LoadReplay();
        }

        #endregion

        #region State Queries

        /// <summary>
        /// 指定した状態への遷移が可能かどうかを確認
        /// </summary>
        /// <param name="targetState">遷移先の状態</param>
        /// <returns>遷移可能な場合true</returns>
        public bool CanTransitionTo(GameState targetState)
        {
            // 初期化中から遷移できるのはMainMenuのみ
            if (CurrentState == GameState.Initializing)
            {
                return targetState == GameState.MainMenu;
            }

            // 同じ状態への遷移は許可しない
            if (CurrentState == targetState)
            {
                return false;
            }

            // 有効な遷移パターン
            return targetState switch
            {
                // MainMenuへは常に戻れる(リセット)
                GameState.MainMenu => true,

                // Lobbyへは MainMenu から
                GameState.Lobby => CurrentState == GameState.MainMenu,

                // Matchへは Lobby から
                GameState.Match => CurrentState == GameState.Lobby,

                // Resultsへは Match から
                GameState.Results => CurrentState == GameState.Match,

                // Replayへは Results から、または MainMenu から
                GameState.Replay => CurrentState == GameState.Results || CurrentState == GameState.MainMenu,

                // Initializingへは遷移不可
                GameState.Initializing => false,

                _ => false
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 状態を変更
        /// </summary>
        /// <param name="newState">新しい状態</param>
        private void ChangeState(GameState newState)
        {
            var previousState = CurrentState;
            PreviousState = previousState;
            CurrentState = newState;

            Debug.Log($"[GameStateService] State changed: {previousState} -> {newState}");

            // イベント発火
            StateChanged?.Invoke(this, new GameStateChangedEventArgs(previousState, newState));
        }

        #endregion
    }
}
