#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// ビデオ設定
    /// </summary>
    [Serializable]
    public class VideoSettings
    {
        /// <summary>
        /// 解像度の幅
        /// </summary>
        public int ResolutionWidth = 1920;

        /// <summary>
        /// 解像度の高さ
        /// </summary>
        public int ResolutionHeight = 1080;

        /// <summary>
        /// フルスクリーンモード
        /// </summary>
        public FullScreenMode FullScreenMode = FullScreenMode.FullScreenWindow;

        /// <summary>
        /// 画質レベル（0～UnityのQuality Settingsの最大値）
        /// </summary>
        public int QualityLevel = 2;

        /// <summary>
        /// VSync有効化
        /// </summary>
        public bool VSync = true;

        /// <summary>
        /// フレームレート制限（-1で無制限）
        /// </summary>
        public int TargetFrameRate = 60;

        /// <summary>
        /// アンチエイリアシング（0, 2, 4, 8）
        /// </summary>
        public int AntiAliasing = 4;

        /// <summary>
        /// デフォルトのビデオ設定を作成します。
        /// </summary>
        /// <returns>デフォルト設定</returns>
        public static VideoSettings CreateDefault()
        {
            return new VideoSettings
            {
                ResolutionWidth = Screen.currentResolution.width,
                ResolutionHeight = Screen.currentResolution.height,
                FullScreenMode = FullScreenMode.FullScreenWindow,
                QualityLevel = QualitySettings.GetQualityLevel(),
                VSync = true,
                TargetFrameRate = 60,
                AntiAliasing = 4
            };
        }

        /// <summary>
        /// 設定をコピーします。
        /// </summary>
        /// <returns>コピーされた設定</returns>
        public VideoSettings Clone()
        {
            return new VideoSettings
            {
                ResolutionWidth = this.ResolutionWidth,
                ResolutionHeight = this.ResolutionHeight,
                FullScreenMode = this.FullScreenMode,
                QualityLevel = this.QualityLevel,
                VSync = this.VSync,
                TargetFrameRate = this.TargetFrameRate,
                AntiAliasing = this.AntiAliasing
            };
        }
    }
}
