#nullable enable

using System;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// フレーム記録イベントの引数
    /// </summary>
    public class ReplayFrameRecordedEventArgs : EventArgs
    {
        /// <summary>
        /// 記録されたフレーム
        /// </summary>
        public ReplayFrame Frame { get; }

        /// <summary>
        /// ReplayFrameRecordedEventArgsの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="frame">記録されたフレーム</param>
        public ReplayFrameRecordedEventArgs(ReplayFrame frame)
        {
            Frame = frame;
        }
    }
}
