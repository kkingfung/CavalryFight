#nullable enable

using System;
using System.IO;
using UnityEngine;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力バインディング管理サービスの実装
    /// </summary>
    /// <remarks>
    /// キーバインディングプロファイルをJSON形式でファイルに保存/読み込みします。
    /// Application.persistentDataPath を使用してユーザー設定を永続化します。
    /// </remarks>
    public class InputBindingService : IInputBindingService
    {
        #region Constants

        private const string PROFILE_FILE_NAME = "InputBindings.json";

        #endregion

        #region Events

        /// <summary>
        /// バインディングが変更された時に発生します。
        /// </summary>
        public event EventHandler<BindingChangedEventArgs>? BindingChanged;

        /// <summary>
        /// プロファイルが読み込まれた時に発生します。
        /// </summary>
        public event EventHandler? ProfileLoaded;

        #endregion

        #region Fields

        private InputBindingProfile _currentProfile;
        private string _profileFilePath;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のバインディングプロファイルを取得します。
        /// </summary>
        public InputBindingProfile CurrentProfile => _currentProfile;

        #endregion

        #region Constructors

        /// <summary>
        /// InputBindingServiceの新しいインスタンスを初期化します。
        /// </summary>
        public InputBindingService()
        {
            _profileFilePath = Path.Combine(Application.persistentDataPath, PROFILE_FILE_NAME);
            _currentProfile = InputBindingProfile.CreateDefault();
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        /// <remarks>
        /// 保存されたプロファイルがあれば読み込み、なければデフォルトを使用します。
        /// </remarks>
        public void Initialize()
        {
            Debug.Log("[InputBindingService] Initializing...");

            // 保存されたプロファイルを読み込む
            if (!LoadProfile())
            {
                Debug.Log("[InputBindingService] No saved profile found. Using default bindings.");
                _currentProfile = InputBindingProfile.CreateDefault();
            }

            // プロファイルを検証
            if (!ValidateProfile())
            {
                Debug.LogWarning("[InputBindingService] Profile validation failed. Resetting to default.");
                ResetToDefault();
            }

            Debug.Log($"[InputBindingService] Initialized. Profile: {_currentProfile.ProfileName}");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[InputBindingService] Disposing...");

            // 現在のプロファイルを保存
            SaveProfile();

            // イベントハンドラをクリア
            BindingChanged = null;
            ProfileLoaded = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定されたアクションのバインディングを取得します。
        /// </summary>
        /// <param name="action">取得するアクション</param>
        /// <returns>バインディング。存在しない場合はnull</returns>
        public InputBinding? GetBinding(InputAction action)
        {
            return _currentProfile.GetBinding(action);
        }

        /// <summary>
        /// 指定されたアクションのバインディングを設定します。
        /// </summary>
        /// <param name="binding">設定するバインディング</param>
        public void SetBinding(InputBinding binding)
        {
            _currentProfile.SetBinding(binding);

            // イベントを発火
            BindingChanged?.Invoke(this, new BindingChangedEventArgs(binding.Action, binding));

            Debug.Log($"[InputBindingService] Binding updated for action: {binding.Action}");
        }

        /// <summary>
        /// バインディングプロファイルをファイルに保存します。
        /// </summary>
        /// <returns>保存に成功した場合true</returns>
        public bool SaveProfile()
        {
            try
            {
                // プロファイルをJSON化
                string json = _currentProfile.ToJson();

                // ディレクトリが存在しない場合は作成
                string? directory = Path.GetDirectoryName(_profileFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // ファイルに書き込み
                File.WriteAllText(_profileFilePath, json);

                Debug.Log($"[InputBindingService] Profile saved to: {_profileFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputBindingService] Failed to save profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// バインディングプロファイルをファイルから読み込みます。
        /// </summary>
        /// <returns>読み込みに成功した場合true</returns>
        public bool LoadProfile()
        {
            try
            {
                // ファイルが存在しない場合は失敗
                if (!File.Exists(_profileFilePath))
                {
                    Debug.Log($"[InputBindingService] Profile file not found: {_profileFilePath}");
                    return false;
                }

                // ファイルから読み込み
                string json = File.ReadAllText(_profileFilePath);

                // JSONからプロファイルを作成
                var profile = InputBindingProfile.FromJson(json);
                if (profile == null)
                {
                    Debug.LogError("[InputBindingService] Failed to deserialize profile.");
                    return false;
                }

                _currentProfile = profile;

                // イベントを発火
                ProfileLoaded?.Invoke(this, EventArgs.Empty);

                Debug.Log($"[InputBindingService] Profile loaded: {_currentProfile.ProfileName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputBindingService] Failed to load profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// デフォルトのバインディングにリセットします。
        /// </summary>
        public void ResetToDefault()
        {
            Debug.Log("[InputBindingService] Resetting to default bindings...");

            _currentProfile = InputBindingProfile.CreateDefault();

            // プロファイルを保存
            SaveProfile();

            // イベントを発火
            ProfileLoaded?.Invoke(this, EventArgs.Empty);

            Debug.Log("[InputBindingService] Reset to default completed.");
        }

        /// <summary>
        /// 現在のプロファイルの妥当性を検証します。
        /// </summary>
        /// <returns>妥当な場合true</returns>
        public bool ValidateProfile()
        {
            return _currentProfile.Validate();
        }

        #endregion
    }
}
