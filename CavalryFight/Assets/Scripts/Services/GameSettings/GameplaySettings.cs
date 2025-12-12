#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// ゲームプレイ設定
    /// </summary>
    [Serializable]
    public class GameplaySettings
    {
        /// <summary>
        /// 移動感度（0.0～1.0）
        /// </summary>
        [Range(0f, 1f)]
        public float MovementSensitivity = 1.0f;

        /// <summary>
        /// カメラ感度（0.0～1.0）
        /// </summary>
        [Range(0f, 1f)]
        public float CameraSensitivity = 0.5f;

        /// <summary>
        /// カメラY軸反転
        /// </summary>
        public bool InvertYAxis = false;

        /// <summary>
        /// 難易度（0=Easy, 1=Normal, 2=Hard）
        /// </summary>
        public int Difficulty = 1;

        /// <summary>
        /// 字幕表示
        /// </summary>
        public bool ShowSubtitles = true;

        /// <summary>
        /// ダメージ表示
        /// </summary>
        public bool ShowDamageNumbers = true;

        /// <summary>
        /// ミニマップ表示
        /// </summary>
        public bool ShowMinimap = true;

        /// <summary>
        /// デフォルトのゲームプレイ設定を作成します。
        /// </summary>
        /// <returns>デフォルト設定</returns>
        public static GameplaySettings CreateDefault()
        {
            return new GameplaySettings
            {
                MovementSensitivity = 1.0f,
                CameraSensitivity = 0.5f,
                InvertYAxis = false,
                Difficulty = 1,
                ShowSubtitles = true,
                ShowDamageNumbers = true,
                ShowMinimap = true
            };
        }

        /// <summary>
        /// 設定をコピーします。
        /// </summary>
        /// <returns>コピーされた設定</returns>
        public GameplaySettings Clone()
        {
            return new GameplaySettings
            {
                MovementSensitivity = this.MovementSensitivity,
                CameraSensitivity = this.CameraSensitivity,
                InvertYAxis = this.InvertYAxis,
                Difficulty = this.Difficulty,
                ShowSubtitles = this.ShowSubtitles,
                ShowDamageNumbers = this.ShowDamageNumbers,
                ShowMinimap = this.ShowMinimap
            };
        }
    }
}
