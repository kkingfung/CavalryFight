#nullable enable

using System;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// ゲーム設定管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// ゲーム設定の読み込み、保存、適用を管理します。
    /// オーディオ、ビデオ、ゲームプレイ設定を一元管理し、
    /// 他のサービスと連携して設定を適用します。
    /// </remarks>
    public interface IGameSettingsService : IService
    {
        #region Events

        /// <summary>
        /// 設定が変更された時に発生します。
        /// </summary>
        event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        /// <summary>
        /// 設定が適用された時に発生します。
        /// </summary>
        event EventHandler? SettingsApplied;

        /// <summary>
        /// 設定がリセットされた時に発生します。
        /// </summary>
        event EventHandler? SettingsReset;

        #endregion

        #region Properties

        /// <summary>
        /// 現在の設定プロファイルを取得します。
        /// </summary>
        SettingsProfile CurrentProfile { get; }

        /// <summary>
        /// 保留中の設定（まだ適用されていない設定）を取得します。
        /// </summary>
        SettingsProfile PendingProfile { get; }

        /// <summary>
        /// 保留中の変更があるかどうかを取得します。
        /// </summary>
        bool HasPendingChanges { get; }

        #endregion

        #region Settings Management

        /// <summary>
        /// 設定を保留します（まだ適用しない）
        /// </summary>
        /// <param name="profile">保留する設定</param>
        void SetPendingSettings(SettingsProfile profile);

        /// <summary>
        /// 保留中の設定を適用します。
        /// </summary>
        void ApplySettings();

        /// <summary>
        /// 保留中の変更を破棄します。
        /// </summary>
        void DiscardPendingChanges();

        /// <summary>
        /// 設定をデフォルトにリセットします。
        /// </summary>
        void ResetToDefault();

        /// <summary>
        /// 設定をファイルに保存します。
        /// </summary>
        /// <returns>保存に成功した場合true</returns>
        bool SaveSettings();

        /// <summary>
        /// 設定をファイルから読み込みます。
        /// </summary>
        /// <returns>読み込みに成功した場合true</returns>
        bool LoadSettings();

        #endregion

        #region Specific Settings Access

        /// <summary>
        /// オーディオ設定を取得します。
        /// </summary>
        /// <returns>オーディオ設定</returns>
        AudioSettings GetAudioSettings();

        /// <summary>
        /// ビデオ設定を取得します。
        /// </summary>
        /// <returns>ビデオ設定</returns>
        VideoSettings GetVideoSettings();

        /// <summary>
        /// ゲームプレイ設定を取得します。
        /// </summary>
        /// <returns>ゲームプレイ設定</returns>
        GameplaySettings GetGameplaySettings();

        #endregion
    }

    /// <summary>
    /// 設定変更イベントの引数
    /// </summary>
    public class SettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 変更された設定のカテゴリ
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// SettingsChangedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="category">変更された設定のカテゴリ</param>
        public SettingsChangedEventArgs(string category)
        {
            Category = category;
        }
    }
}
