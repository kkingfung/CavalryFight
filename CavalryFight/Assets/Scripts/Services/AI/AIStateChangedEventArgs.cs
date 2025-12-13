#nullable enable

using System;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI敵の状態が変更された時のイベント引数
    /// </summary>
    public class AIStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// AI敵
        /// </summary>
        public BlazeAI AI { get; }

        /// <summary>
        /// 新しい状態名
        /// </summary>
        public string NewState { get; }

        /// <summary>
        /// 前の状態名
        /// </summary>
        public string? OldState { get; }

        /// <summary>
        /// AIStateChangedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="newState">新しい状態名</param>
        /// <param name="oldState">前の状態名</param>
        public AIStateChangedEventArgs(BlazeAI ai, string newState, string? oldState = null)
        {
            AI = ai;
            NewState = newState;
            OldState = oldState;
        }
    }
}
