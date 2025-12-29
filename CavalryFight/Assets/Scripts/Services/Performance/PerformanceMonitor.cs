#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Performance
{
    /// <summary>
    /// パフォーマンス監視サービスの実装
    /// </summary>
    /// <remarks>
    /// FPSを追跡し、定期的に更新イベントを発火します。
    /// </remarks>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        #region Fields

        /// <summary>
        /// FPS計算用のフレームカウント
        /// </summary>
        private int _frameCount;

        /// <summary>
        /// FPS計算用の経過時間
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// FPS更新間隔（秒）
        /// </summary>
        private const float UpdateInterval = 0.5f;

        /// <summary>
        /// 現在のFPS
        /// </summary>
        private int _currentFPS;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のFPS（フレーム/秒）を取得します
        /// </summary>
        public int CurrentFPS => _currentFPS;

        #endregion

        #region Events

        /// <summary>
        /// FPSが更新された時に発生します
        /// </summary>
        public event Action<int>? FPSUpdated;

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            _frameCount = 0;
            _elapsedTime = 0f;
            _currentFPS = 0;

            Debug.Log("[PerformanceMonitor] Initialized.");
        }

        /// <summary>
        /// サービスをシャットダウンします
        /// </summary>
        public void Shutdown()
        {
            Debug.Log("[PerformanceMonitor] Shutdown.");
        }

        /// <summary>
        /// フレーム更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        public void Tick(float deltaTime)
        {
            _frameCount++;
            _elapsedTime += deltaTime;

            if (_elapsedTime >= UpdateInterval)
            {
                // FPSを計算
                int newFPS = Mathf.RoundToInt(_frameCount / _elapsedTime);

                // 値が変更された場合のみイベント発火
                if (newFPS != _currentFPS)
                {
                    _currentFPS = newFPS;
                    FPSUpdated?.Invoke(_currentFPS);
                }

                // カウンターをリセット
                _frameCount = 0;
                _elapsedTime = 0f;
            }
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            // 特に解放するリソースはありませんが、インターフェース実装のため定義
            Debug.Log("[PerformanceMonitor] Disposed.");
        }

        #endregion
    }
}
