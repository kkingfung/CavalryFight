#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// 設定プロファイル
    /// </summary>
    /// <remarks>
    /// すべてのゲーム設定を含むプロファイルです。
    /// JSON形式でファイルに保存/読み込みできます。
    /// </remarks>
    [Serializable]
    public class SettingsProfile
    {
        /// <summary>
        /// プロファイル名
        /// </summary>
        public string ProfileName = "Default";

        /// <summary>
        /// オーディオ設定
        /// </summary>
        public AudioSettings Audio = new AudioSettings();

        /// <summary>
        /// ビデオ設定
        /// </summary>
        public VideoSettings Video = new VideoSettings();

        /// <summary>
        /// ゲームプレイ設定
        /// </summary>
        public GameplaySettings Gameplay = new GameplaySettings();

        /// <summary>
        /// デフォルトの設定プロファイルを作成します。
        /// </summary>
        /// <returns>デフォルト設定</returns>
        public static SettingsProfile CreateDefault()
        {
            return new SettingsProfile
            {
                ProfileName = "Default",
                Audio = AudioSettings.CreateDefault(),
                Video = VideoSettings.CreateDefault(),
                Gameplay = GameplaySettings.CreateDefault()
            };
        }

        /// <summary>
        /// プロファイルをコピーします。
        /// </summary>
        /// <returns>コピーされたプロファイル</returns>
        public SettingsProfile Clone()
        {
            return new SettingsProfile
            {
                ProfileName = this.ProfileName,
                Audio = this.Audio.Clone(),
                Video = this.Video.Clone(),
                Gameplay = this.Gameplay.Clone()
            };
        }

        /// <summary>
        /// プロファイルをJSON文字列に変換します。
        /// </summary>
        /// <returns>JSON文字列</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, prettyPrint: true);
        }

        /// <summary>
        /// JSON文字列からプロファイルを作成します。
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>デシリアライズされたプロファイル</returns>
        public static SettingsProfile? FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<SettingsProfile>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsProfile] Failed to deserialize profile: {ex.Message}");
                return null;
            }
        }
    }
}
