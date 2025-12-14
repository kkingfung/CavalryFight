#nullable enable

using System;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// 再生開始イベントの引数
    /// </summary>
    public class ReplayPlaybackStartedEventArgs : EventArgs
    {
        /// <summary>
        /// 再生するリプレイデータ
        /// </summary>
        public ReplayData ReplayData { get; }

        /// <summary>
        /// 再生開始時刻（秒）
        /// </summary>
        public float StartTime { get; }

        /// <summary>
        /// ReplayPlaybackStartedEventArgsの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="replayData">再生するリプレイデータ</param>
        /// <param name="startTime">再生開始時刻（秒）</param>
        public ReplayPlaybackStartedEventArgs(ReplayData replayData, float startTime)
        {
            ReplayData = replayData;
            StartTime = startTime;
        }
    }
}
