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
