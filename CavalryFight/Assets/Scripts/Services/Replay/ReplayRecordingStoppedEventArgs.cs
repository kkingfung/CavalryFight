#nullable enable

using System;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// 録画停止イベントの引数
    /// </summary>
    public class ReplayRecordingStoppedEventArgs : EventArgs
    {
        /// <summary>
        /// 録画されたリプレイデータ
        /// </summary>
        public ReplayData ReplayData { get; }

        /// <summary>
        /// ReplayRecordingStoppedEventArgsの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="replayData">録画されたリプレイデータ</param>
        public ReplayRecordingStoppedEventArgs(ReplayData replayData)
        {
            ReplayData = replayData;
        }
    }
}
