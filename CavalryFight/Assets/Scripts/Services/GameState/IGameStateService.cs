#nullable enable

using CavalryFight.Core.Services;
using System;

namespace CavalryFight.Services.GameState
{
    /// <summary>
    /// ゲーム状態
    /// </summary>
    public enum GameState
    {
        /// <summary>初期化中</summary>
        Initializing,

        /// <summary>メインメニュー</summary>
        MainMenu,

        /// <summary>ロビー(マルチプレイヤー待機)</summary>
        Lobby,

        /// <summary>マッチ中(ゲームプレイ)</summary>
        Match,

        /// <summary>結果表示</summary>
        Results,

        /// <summary>リプレイ再生</summary>
        Replay
    }

    /// <summary>
    /// ゲーム状態変更イベント引数
    /// </summary>
    public class GameStateChangedEventArgs : EventArgs
    {
        /// <summary>前の状態</summary>
        public GameState PreviousState { get; }

        /// <summary>新しい状態</summary>
        public GameState NewState { get; }

        public GameStateChangedEventArgs(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// ゲーム状態管理サービス
    /// </summary>
    /// <remarks>
    /// ゲームの全体的な状態遷移を管理します。
    /// MainMenu → Lobby → Match → Results → Replay の流れを制御します。
    /// </remarks>
    public interface IGameStateService : IService
    {
        #region Properties

        /// <summary>
        /// 現在のゲーム状態
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// 前のゲーム状態
        /// </summary>
        GameState? PreviousState { get; }

        #endregion

        #region Events

        /// <summary>
        /// ゲーム状態が変更されたときに発生
        /// </summary>
        event EventHandler<GameStateChangedEventArgs>? StateChanged;

        #endregion

        #region State Transitions

        /// <summary>
        /// メインメニューへ遷移
        /// </summary>
        void TransitionToMainMenu();

        /// <summary>
        /// ロビーへ遷移
        /// </summary>
        void TransitionToLobby();

        /// <summary>
        /// マッチへ遷移
        /// </summary>
        void TransitionToMatch();

        /// <summary>
        /// 結果画面へ遷移
        /// </summary>
        void TransitionToResults();

        /// <summary>
        /// リプレイへ遷移
        /// </summary>
        void TransitionToReplay();

        #endregion

        #region State Queries

        /// <summary>
        /// 指定した状態への遷移が可能かどうかを確認
        /// </summary>
        /// <param name="targetState">遷移先の状態</param>
        /// <returns>遷移可能な場合true</returns>
        bool CanTransitionTo(GameState targetState);

        #endregion
    }
}
