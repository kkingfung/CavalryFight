#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI敵が死亡した時のイベント引数
    /// </summary>
    public class AIDeathEventArgs : EventArgs
    {
        /// <summary>
        /// 死亡したAI敵
        /// </summary>
        public BlazeAI AI { get; }

        /// <summary>
        /// 死亡位置
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// キラー（AI敵を倒したオブジェクト）
        /// </summary>
        public GameObject? Killer { get; }

        /// <summary>
        /// AIDeathEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="ai">死亡したAI敵</param>
        /// <param name="position">死亡位置</param>
        /// <param name="killer">キラー</param>
        public AIDeathEventArgs(BlazeAI ai, Vector3 position, GameObject? killer = null)
        {
            AI = ai;
            Position = position;
            Killer = killer;
        }
    }
}
