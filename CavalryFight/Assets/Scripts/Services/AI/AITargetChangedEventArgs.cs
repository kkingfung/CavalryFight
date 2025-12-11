#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI敵のターゲットが変更された時のイベント引数
    /// </summary>
    public class AITargetChangedEventArgs : EventArgs
    {
        /// <summary>
        /// AI敵
        /// </summary>
        public BlazeAI AI { get; }

        /// <summary>
        /// 新しいターゲット
        /// </summary>
        public GameObject? NewTarget { get; }

        /// <summary>
        /// 前のターゲット
        /// </summary>
        public GameObject? OldTarget { get; }

        /// <summary>
        /// AITargetChangedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="newTarget">新しいターゲット</param>
        /// <param name="oldTarget">前のターゲット</param>
        public AITargetChangedEventArgs(BlazeAI ai, GameObject? newTarget, GameObject? oldTarget = null)
        {
            AI = ai;
            NewTarget = newTarget;
            OldTarget = oldTarget;
        }
    }
}
