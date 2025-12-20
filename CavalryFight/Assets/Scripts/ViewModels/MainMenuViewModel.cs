#nullable enable

using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// メインメニュー画面のViewModel
    /// </summary>
    /// <remarks>
    /// メインメニューで表示するゲームモード選択とナビゲーション機能を提供します。
    /// </remarks>
    public class MainMenuViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService? _sceneManagementService;
        private string _title = "CavalryFight";
        private string _subtitle = "Main Menu";

        #endregion

        #region Properties

        /// <summary>
        /// タイトルテキストを取得または設定します。
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// サブタイトルテキストを取得または設定します。
        /// </summary>
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// トレーニングモードを開始するコマンド
        /// </summary>
        public ICommand StartTrainingCommand { get; }

        /// <summary>
        /// マッチロビーを開くコマンド
        /// </summary>
        public ICommand OpenMatchLobbyCommand { get; }

        /// <summary>
        /// カスタマイゼーション画面を開くコマンド
        /// </summary>
        public ICommand OpenCustomizationCommand { get; }

        /// <summary>
        /// リプレイ履歴画面を開くコマンド
        /// </summary>
        public ICommand OpenReplayHistoryCommand { get; }

        /// <summary>
        /// 設定画面を開くコマンド
        /// </summary>
        public ICommand OpenSettingsCommand { get; }

        /// <summary>
        /// ゲームを終了するコマンド
        /// </summary>
        public ICommand QuitGameCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// MainMenuViewModelの新しいインスタンスを初期化します。
        /// </summary>
        public MainMenuViewModel()
        {
            // サービスを取得
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // コマンドを初期化
            StartTrainingCommand = new RelayCommand(OnStartTraining, CanStartTraining);
            OpenMatchLobbyCommand = new RelayCommand(OnOpenMatchLobby, CanOpenMatchLobby);
            OpenCustomizationCommand = new RelayCommand(OnOpenCustomization, CanOpenCustomization);
            OpenReplayHistoryCommand = new RelayCommand(OnOpenReplayHistory, CanOpenReplayHistory);
            OpenSettingsCommand = new RelayCommand(OnOpenSettings, CanOpenSettings);
            QuitGameCommand = new RelayCommand(OnQuitGame);
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// トレーニングモードを開始できるかどうかを判定します。
        /// </summary>
        /// <returns>開始可能な場合はtrue</returns>
        private bool CanStartTraining()
        {
            return _sceneManagementService != null && !_sceneManagementService.IsLoading;
        }

        /// <summary>
        /// トレーニングモードを開始します。
        /// </summary>
        private void OnStartTraining()
        {
            Debug.Log("[MainMenuViewModel] Starting Training Mode...");
            _sceneManagementService?.LoadTraining();
        }

        /// <summary>
        /// マッチロビーを開けるかどうかを判定します。
        /// </summary>
        /// <returns>開ける場合はtrue</returns>
        private bool CanOpenMatchLobby()
        {
            return _sceneManagementService != null && !_sceneManagementService.IsLoading;
        }

        /// <summary>
        /// マッチロビーを開きます。
        /// </summary>
        private void OnOpenMatchLobby()
        {
            Debug.Log("[MainMenuViewModel] Opening Match Lobby...");
            _sceneManagementService?.LoadLobby();
        }

        /// <summary>
        /// カスタマイゼーション画面を開けるかどうかを判定します。
        /// </summary>
        /// <returns>開ける場合はtrue</returns>
        private bool CanOpenCustomization()
        {
            return _sceneManagementService != null && !_sceneManagementService.IsLoading;
        }

        /// <summary>
        /// カスタマイゼーション画面を開きます。
        /// </summary>
        private void OnOpenCustomization()
        {
            Debug.Log("[MainMenuViewModel] Opening Customization...");
            // TODO: カスタマイゼーション専用のシーンが必要な場合は実装
            // 現在はSettingsシーンでカスタマイゼーションタブを提供する想定
            _sceneManagementService?.LoadSettings();
        }

        /// <summary>
        /// リプレイ履歴画面を開けるかどうかを判定します。
        /// </summary>
        /// <returns>開ける場合はtrue</returns>
        private bool CanOpenReplayHistory()
        {
            return _sceneManagementService != null && !_sceneManagementService.IsLoading;
        }

        /// <summary>
        /// リプレイ履歴画面を開きます。
        /// </summary>
        private void OnOpenReplayHistory()
        {
            Debug.Log("[MainMenuViewModel] Opening Replay History...");
            _sceneManagementService?.LoadReplay();
        }

        /// <summary>
        /// 設定画面を開けるかどうかを判定します。
        /// </summary>
        /// <returns>開ける場合はtrue</returns>
        private bool CanOpenSettings()
        {
            return _sceneManagementService != null && !_sceneManagementService.IsLoading;
        }

        /// <summary>
        /// 設定画面を開きます。
        /// </summary>
        private void OnOpenSettings()
        {
            Debug.Log("[MainMenuViewModel] Opening Settings...");
            _sceneManagementService?.LoadSettings();
        }

        /// <summary>
        /// ゲームを終了します。
        /// </summary>
        private void OnQuitGame()
        {
            Debug.Log("[MainMenuViewModel] Quitting game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        protected override void OnDispose()
        {
            base.OnDispose();
            Debug.Log("[MainMenuViewModel] Disposed.");
        }

        #endregion
    }
}
