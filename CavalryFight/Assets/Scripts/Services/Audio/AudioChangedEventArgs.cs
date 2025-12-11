#nullable enable

using System;

namespace CavalryFight.Services.Audio
{
    /// <summary>
    /// オーディオ変更イベントの引数
    /// </summary>
    public class AudioChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 変更されたオーディオクリップ名
        /// </summary>
        public string ClipName { get; }

        /// <summary>
        /// AudioChangedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="clipName">変更されたオーディオクリップ名</param>
        public AudioChangedEventArgs(string clipName)
        {
            ClipName = clipName;
        }
    }
}
