#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// オーディオ設定
    /// </summary>
    [Serializable]
    public class AudioSettings
    {
        /// <summary>
        /// マスターボリューム（0.0～1.0）
        /// </summary>
        [Range(0f, 1f)]
        public float MasterVolume = 1.0f;

        /// <summary>
        /// BGMボリューム（0.0～1.0）
        /// </summary>
        [Range(0f, 1f)]
        public float BgmVolume = 0.8f;

        /// <summary>
        /// SEボリューム（0.0～1.0）
        /// </summary>
        [Range(0f, 1f)]
        public float SfxVolume = 0.8f;

        /// <summary>
        /// デフォルトのオーディオ設定を作成します。
        /// </summary>
        /// <returns>デフォルト設定</returns>
        public static AudioSettings CreateDefault()
        {
            return new AudioSettings
            {
                MasterVolume = 1.0f,
                BgmVolume = 0.8f,
                SfxVolume = 0.8f
            };
        }

        /// <summary>
        /// 設定をコピーします。
        /// </summary>
        /// <returns>コピーされた設定</returns>
        public AudioSettings Clone()
        {
            return new AudioSettings
            {
                MasterVolume = this.MasterVolume,
                BgmVolume = this.BgmVolume,
                SfxVolume = this.SfxVolume
            };
        }
    }
}
