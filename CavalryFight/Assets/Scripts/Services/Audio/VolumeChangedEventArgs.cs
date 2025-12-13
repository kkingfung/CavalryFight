#nullable enable

using System;

namespace CavalryFight.Services.Audio
{
    /// <summary>
    /// ボリューム変更イベントの引数
    /// </summary>
    public class VolumeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// ボリュームの種類
        /// </summary>
        public VolumeType Type { get; }

        /// <summary>
        /// 新しいボリューム値（0.0～1.0）
        /// </summary>
        public float Volume { get; }

        /// <summary>
        /// VolumeChangedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="type">ボリュームの種類</param>
        /// <param name="volume">新しいボリューム値</param>
        public VolumeChangedEventArgs(VolumeType type, float volume)
        {
            Type = type;
            Volume = volume;
        }
    }
}
