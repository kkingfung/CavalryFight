#nullable enable

using CavalryFight.Core.Services;
using CavalryFight.Services.AI;
using CavalryFight.Services.Match;
using CavalryFight.Services.Replay;
using UnityEngine;

namespace CavalryFight.Core.Bootstrap
{
    /// <summary>
    /// サービス更新管理
    /// </summary>
    /// <remarks>
    /// Update処理が必要なサービスを毎フレーム更新します。
    /// GameBootstrapと同じGameObjectにアタッチされます。
    /// </remarks>
    [DisallowMultipleComponent]
    public class ServiceUpdater : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// マッチサービス
        /// </summary>
        private IMatchService? _matchService;

        /// <summary>
        /// AIサービス
        /// </summary>
        private IBlazeAIService? _aiService;

        /// <summary>
        /// リプレイ録画サービス
        /// </summary>
        private IReplayRecorder? _replayRecorder;

        /// <summary>
        /// リプレイ再生サービス
        /// </summary>
        private IReplayPlayer? _replayPlayer;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 開始時の初期化
        /// </summary>
        private void Start()
        {
            // ServiceLocatorから各サービスを取得
            _matchService = ServiceLocator.Instance.Get<IMatchService>();
            _aiService = ServiceLocator.Instance.Get<IBlazeAIService>();
            _replayRecorder = ServiceLocator.Instance.Get<IReplayRecorder>();
            _replayPlayer = ServiceLocator.Instance.Get<IReplayPlayer>();

            // 警告出力
            if (_matchService == null)
            {
                Debug.LogWarning("[ServiceUpdater] MatchService not found in ServiceLocator.");
            }

            if (_aiService == null)
            {
                Debug.LogWarning("[ServiceUpdater] BlazeAIService not found in ServiceLocator.");
            }

            if (_replayRecorder == null)
            {
                Debug.LogWarning("[ServiceUpdater] ReplayRecorder not found in ServiceLocator.");
            }

            if (_replayPlayer == null)
            {
                Debug.LogWarning("[ServiceUpdater] ReplayPlayer not found in ServiceLocator.");
            }

            // すべてのサービスが見つからない場合はコンポーネントを無効化
            if (_matchService == null && _aiService == null && _replayRecorder == null && _replayPlayer == null)
            {
                Debug.LogError("[ServiceUpdater] No services found. Disabling ServiceUpdater.");
                enabled = false;
                return;
            }

            Debug.Log("[ServiceUpdater] Started. Updating services each frame.");
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        private void Update()
        {
            // MatchServiceの更新
            if (_matchService != null)
            {
                _matchService.Update();
            }


            // ReplayRecorderの更新
            if (_replayRecorder != null && _replayRecorder is ReplayRecorder recorder)
            {
                recorder.UpdateRecording(Time.deltaTime);
            }

            // ReplayPlayerの更新
            if (_replayPlayer != null && _replayPlayer is ReplayPlayer player)
            {
                player.UpdatePlayback(Time.deltaTime);
            }
        }

        #endregion
    }
}
