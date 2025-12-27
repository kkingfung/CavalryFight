#nullable enable

using AdvancedSceneManager.Models;
using UnityEngine;

namespace CavalryFight.Core.Bootstrap
{
    /// <summary>
    /// シーンコレクション設定
    /// </summary>
    /// <remarks>
    /// GameBootstrap が参照するシーンコレクション設定です。
    /// SceneManagementService に SceneCollection を登録する際に使用されます。
    /// </remarks>
    [CreateAssetMenu(fileName = "SceneCollectionConfig", menuName = "CavalryFight/Bootstrap/Scene Collection Config", order = 1)]
    public class SceneCollectionConfig : ScriptableObject
    {
        #region Scene Collections

        [Header("Core Scenes")]
        [Tooltip("Startup シーン（GameBootstrap が配置されている）")]
        [SerializeField] private SceneCollection? _startup;

        [Header("UI Scenes")]
        [Tooltip("メインメニューシーン")]
        [SerializeField] private SceneCollection? _mainMenu;

        [Tooltip("ロビーシーン（マルチプレイヤー待機）")]
        [SerializeField] private SceneCollection? _lobby;

        [Tooltip("設定画面シーン")]
        [SerializeField] private SceneCollection? _settings;

        [Header("Gameplay Scenes")]
        [Tooltip("マッチシーン（ゲームプレイ）")]
        [SerializeField] private SceneCollection? _match;

        [Tooltip("トレーニングシーン")]
        [SerializeField] private SceneCollection? _training;

        [Header("Result Scenes")]
        [Tooltip("結果表示シーン")]
        [SerializeField] private SceneCollection? _results;

        [Tooltip("リプレイ再生シーン")]
        [SerializeField] private SceneCollection? _replay;

        #endregion

        #region Properties

        /// <summary>
        /// Startup シーンを取得します
        /// </summary>
        public SceneCollection? Startup => _startup;

        /// <summary>
        /// メインメニューシーンを取得します
        /// </summary>
        public SceneCollection? MainMenu => _mainMenu;

        /// <summary>
        /// ロビーシーンを取得します
        /// </summary>
        public SceneCollection? Lobby => _lobby;

        /// <summary>
        /// 設定画面シーンを取得します
        /// </summary>
        public SceneCollection? Settings => _settings;

        /// <summary>
        /// マッチシーンを取得します
        /// </summary>
        public SceneCollection? Match => _match;

        /// <summary>
        /// トレーニングシーンを取得します
        /// </summary>
        public SceneCollection? Training => _training;

        /// <summary>
        /// 結果表示シーンを取得します
        /// </summary>
        public SceneCollection? Results => _results;

        /// <summary>
        /// リプレイ再生シーンを取得します
        /// </summary>
        public SceneCollection? Replay => _replay;

        #endregion

        #region Validation

#if UNITY_EDITOR
        /// <summary>
        /// エディタでの検証処理
        /// </summary>
        private void OnValidate()
        {
            // 必須のシーンコレクションが設定されているか確認
            if (_startup == null)
            {
                Debug.LogWarning("[SceneCollectionConfig] Startup scene collection is not assigned!", this);
            }

            if (_mainMenu == null)
            {
                Debug.LogWarning("[SceneCollectionConfig] MainMenu scene collection is not assigned!", this);
            }

            if (_match == null)
            {
                Debug.LogWarning("[SceneCollectionConfig] Match scene collection is not assigned!", this);
            }
        }
#endif

        #endregion
    }
}
