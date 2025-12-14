#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// ReplayRecorderとReplayPlayerのUpdate処理を実行するMonoBehaviourヘルパー
    /// </summary>
    /// <remarks>
    /// ReplayRecorderとReplayPlayerは録画と再生のUpdate処理が必要ですが、
    /// IServiceはMonoBehaviourではないため、このヘルパーを使用して
    /// UnityのUpdate サイクルから処理を呼び出します。
    ///
    /// 使用方法:
    /// - Bootstrapスクリプトと同じGameObjectにアタッチ
    /// - DontDestroyOnLoadで永続化
    /// </remarks>
    [DisallowMultipleComponent]
    public class ReplayServiceUpdater : MonoBehaviour
    {
        private IReplayRecorder? _replayRecorder;
        private IReplayPlayer? _replayPlayer;

        private void Start()
        {
            // ReplayRecorderを取得
            _replayRecorder = ServiceLocator.Instance.Get<IReplayRecorder>();

            if (_replayRecorder == null)
            {
                Debug.LogWarning("[ReplayServiceUpdater] ReplayRecorder not found in ServiceLocator!");
            }

            // ReplayPlayerを取得
            _replayPlayer = ServiceLocator.Instance.Get<IReplayPlayer>();

            if (_replayPlayer == null)
            {
                Debug.LogWarning("[ReplayServiceUpdater] ReplayPlayer not found in ServiceLocator!");
            }

            if (_replayRecorder == null && _replayPlayer == null)
            {
                Debug.LogError("[ReplayServiceUpdater] Neither ReplayRecorder nor ReplayPlayer found in ServiceLocator!");
                enabled = false;
                return;
            }

            Debug.Log("[ReplayServiceUpdater] Started.");
        }

        private void Update()
        {
            // ReplayRecorderのUpdate処理
            if (_replayRecorder != null && _replayRecorder is ReplayRecorder recorder)
            {
                recorder.UpdateRecording(Time.deltaTime);
            }

            // ReplayPlayerのUpdate処理
            if (_replayPlayer != null && _replayPlayer is ReplayPlayer player)
            {
                player.UpdatePlayback(Time.deltaTime);
            }
        }
    }
}
