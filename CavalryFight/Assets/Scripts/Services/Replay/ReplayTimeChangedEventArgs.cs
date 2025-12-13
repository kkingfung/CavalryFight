#nullable enable

using System;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// 再生時刻変更イベントの引数
    /// </summary>
    public class ReplayTimeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 現在の再生時刻（秒）
        /// </summary>
        public float CurrentTime { get; }

        /// <summary>
        /// ReplayTimeChangedEventArgsの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="currentTime">現在の再生時刻（秒）</param>
        public ReplayTimeChangedEventArgs(float currentTime)
        {
            CurrentTime = currentTime;
        }
    }
}
