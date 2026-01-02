#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Training
{
    /// <summary>
    /// スコア獲得イベントの引数
    /// </summary>
    public class ScoreEarnedEventArgs : EventArgs
    {
        /// <summary>
        /// 獲得したスコアを取得します
        /// </summary>
        public int Score { get; }

        /// <summary>
        /// 命中位置を取得します
        /// </summary>
        public Vector3 HitPosition { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="score">獲得したスコア</param>
        /// <param name="hitPosition">命中位置</param>
        public ScoreEarnedEventArgs(int score, Vector3 hitPosition)
        {
            Score = score;
            HitPosition = hitPosition;
        }
    }
}
