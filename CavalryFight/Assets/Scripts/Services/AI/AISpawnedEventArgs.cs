#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI敵がスポーンされた時のイベント引数
    /// </summary>
    public class AISpawnedEventArgs : EventArgs
    {
        /// <summary>
        /// スポーンされたAI敵
        /// </summary>
        public BlazeAI AI { get; }

        /// <summary>
        /// スポーン位置
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// AISpawnedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="ai">スポーンされたAI敵</param>
        /// <param name="position">スポーン位置</param>
        public AISpawnedEventArgs(BlazeAI ai, Vector3 position)
        {
            AI = ai;
            Position = position;
        }
    }
}
