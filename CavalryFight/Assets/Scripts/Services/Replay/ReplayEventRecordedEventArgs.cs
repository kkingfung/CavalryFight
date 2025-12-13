#nullable enable

using System;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// イベント記録イベントの引数
    /// </summary>
    public class ReplayEventRecordedEventArgs : EventArgs
    {
        /// <summary>
        /// 記録されたイベント
        /// </summary>
        public ReplayEvent ReplayEvent { get; }

        /// <summary>
        /// ReplayEventRecordedEventArgsの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="replayEvent">記録されたイベント</param>
        public ReplayEventRecordedEventArgs(ReplayEvent replayEvent)
        {
            ReplayEvent = replayEvent;
        }
    }
}
