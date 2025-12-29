#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Gameplay.Training
{
    /// <summary>
    /// トレーニングシーンのゲームプレイイベント管理
    /// </summary>
    /// <remarks>
    /// シングルトンパターンでトレーニング中のイベントを管理します。
    /// ArrowProjectileなどのゲームプレイオブジェクトとTrainingViewModelを繋ぎます。
    /// </remarks>
    public class TrainingManager : MonoBehaviour
    {
        #region Singleton

        private static TrainingManager? _instance;

        public static TrainingManager? Instance => _instance;

        #endregion

        #region Events

        /// <summary>
        /// 矢が発射された時に発生します
        /// </summary>
        public event EventHandler? ArrowFired;

        /// <summary>
        /// 矢がターゲットに命中した時に発生します
        /// </summary>
        public event EventHandler? TargetHit;

        /// <summary>
        /// スコアが獲得された時に発生します
        /// </summary>
        public event EventHandler<ScoreEarnedEventArgs>? ScoreEarned;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 矢が発射されたことを通知します
        /// </summary>
        public void NotifyArrowFired()
        {
            ArrowFired?.Invoke(this, EventArgs.Empty);
            Debug.Log("[TrainingManager] Arrow fired");
        }

        /// <summary>
        /// ターゲットに命中したことを通知します
        /// </summary>
        public void NotifyTargetHit()
        {
            TargetHit?.Invoke(this, EventArgs.Empty);
            Debug.Log("[TrainingManager] Target hit");
        }

        /// <summary>
        /// スコアを獲得したことを通知します
        /// </summary>
        /// <param name="score">獲得したスコア</param>
        /// <param name="hitPosition">命中位置</param>
        public void NotifyScoreEarned(int score, Vector3 hitPosition)
        {
            var args = new ScoreEarnedEventArgs(score, hitPosition);
            ScoreEarned?.Invoke(this, args);
            Debug.Log($"[TrainingManager] Score earned: {score} at {hitPosition}");
        }

        #endregion
    }

    /// <summary>
    /// スコア獲得イベントの引数
    /// </summary>
    public class ScoreEarnedEventArgs : EventArgs
    {
        /// <summary>獲得したスコア</summary>
        public int Score { get; }

        /// <summary>命中位置</summary>
        public Vector3 HitPosition { get; }

        public ScoreEarnedEventArgs(int score, Vector3 hitPosition)
        {
            Score = score;
            HitPosition = hitPosition;
        }
    }
}
