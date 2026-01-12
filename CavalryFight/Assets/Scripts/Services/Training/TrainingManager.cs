#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Training
{
    /// <summary>
    /// トレーニングシーンのゲームロジックを管理します
    /// </summary>
    /// <remarks>
    /// シングルトンパターンで実装され、トレーニング中のスコアやイベントを管理します。
    /// </remarks>
    public class TrainingManager : MonoBehaviour
    {
        #region Singleton

        private static TrainingManager? _instance;

        /// <summary>
        /// シングルトンインスタンスを取得します
        /// </summary>
        public static TrainingManager? Instance => _instance;

        #endregion

        #region Events

        /// <summary>
        /// 矢が発射された時に発生します
        /// </summary>
        public event EventHandler? ArrowFired;

        /// <summary>
        /// ターゲットに命中した時に発生します
        /// </summary>
        public event EventHandler? TargetHit;

        /// <summary>
        /// スコアを獲得した時に発生します
        /// </summary>
        public event EventHandler<ScoreEarnedEventArgs>? ScoreEarned;

        /// <summary>
        /// チャージ（エイミング）が開始された時に発生します
        /// </summary>
        public event EventHandler? ChargingStarted;

        /// <summary>
        /// チャージ（エイミング）が終了した時に発生します
        /// </summary>
        public event EventHandler? ChargingEnded;

        #endregion

        #region Private Fields

        private int _totalScore;
        private int _arrowsFired;
        private int _hits;

        #endregion

        #region Properties

        /// <summary>
        /// 合計スコアを取得します
        /// </summary>
        public int TotalScore => _totalScore;

        /// <summary>
        /// 発射した矢の数を取得します
        /// </summary>
        public int ArrowsFired => _arrowsFired;

        /// <summary>
        /// 命中数を取得します
        /// </summary>
        public int Hits => _hits;

        /// <summary>
        /// 命中率を取得します（パーセンテージ）
        /// </summary>
        public float Accuracy => _arrowsFired > 0 ? (_hits / (float)_arrowsFired) * 100f : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[TrainingManager] Instance already exists. Destroying duplicate.");
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
        /// 矢が発射されたことを記録します
        /// </summary>
        public void RecordArrowFired()
        {
            _arrowsFired++;
            ArrowFired?.Invoke(this, EventArgs.Empty);
            Debug.Log($"[TrainingManager] Arrow fired. Total: {_arrowsFired}");
        }

        /// <summary>
        /// ターゲットへの命中を記録します
        /// </summary>
        /// <param name="score">獲得スコア</param>
        /// <param name="hitPosition">命中位置</param>
        public void RecordHit(int score, Vector3 hitPosition)
        {
            _hits++;
            _totalScore += score;

            TargetHit?.Invoke(this, EventArgs.Empty);
            ScoreEarned?.Invoke(this, new ScoreEarnedEventArgs(score, hitPosition));

            Debug.Log($"[TrainingManager] Hit! Score: {score}, Total: {_totalScore}, Accuracy: {Accuracy:F1}%");
        }

        /// <summary>
        /// トレーニングをリセットします
        /// </summary>
        public void Reset()
        {
            _totalScore = 0;
            _arrowsFired = 0;
            _hits = 0;
            Debug.Log("[TrainingManager] Training reset.");
        }

        /// <summary>
        /// チャージ開始を通知します
        /// </summary>
        public void NotifyChargingStarted()
        {
            Debug.Log("[TrainingManager] NotifyChargingStarted called");
            ChargingStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// チャージ終了を通知します
        /// </summary>
        public void NotifyChargingEnded()
        {
            Debug.Log("[TrainingManager] NotifyChargingEnded called");
            ChargingEnded?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
