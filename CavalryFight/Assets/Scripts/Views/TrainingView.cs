#nullable enable

using UnityEngine;
using UnityEngine.UIElements;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Input;
using CavalryFight.Services.SceneManagement;
using CavalryFight.Services.Training;
using CavalryFight.ViewModels;

namespace CavalryFight.Views
{
    /// <summary>
    /// トレーニングシーンのView
    /// </summary>
    /// <remarks>
    /// HUDの表示とポーズメニューの制御を行います。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class TrainingView : UIToolkitViewBase<TrainingViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;

        [Header("Score Popup")]
        [SerializeField] private GameObject? _scorePopupPrefab;

        #endregion

        #region Private Fields

        private IAudioService? _audioService;
        private IInputService? _inputService;
        private Label? _scoreLabel;
        private Label? _arrowsFiredLabel;
        private Label? _hitsLabel;
        private Label? _accuracyLabel;
        private VisualElement? _pauseMenu;
        private Button? _resumeButton;
        private Button? _settingsButton;
        private Button? _backToMenuButton;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // サービス取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

            if (sceneService == null)
            {
                Debug.LogError("[TrainingView] ISceneManagementService が取得できませんでした！");
                return;
            }

            // ViewModel作成
            ViewModel = new TrainingViewModel(sceneService);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // BGMを再生
            if (_bgmClip != null && _audioService != null)
            {
                _audioService.PlayBgm(_bgmClip, loop: true, fadeInDuration: 2f);
            }

            // TrainingManagerイベント購読
            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.ArrowFired += OnArrowFired;
                TrainingManager.Instance.TargetHit += OnTargetHit;
                TrainingManager.Instance.ScoreEarned += OnScoreEarned;
            }
        }

        protected override void OnDisable()
        {
            // TrainingManagerイベント購読解除
            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.ArrowFired -= OnArrowFired;
                TrainingManager.Instance.TargetHit -= OnTargetHit;
                TrainingManager.Instance.ScoreEarned -= OnScoreEarned;
            }

            // BGMは停止しない（シーン遷移時の継続再生のため）
            // 次のシーンが異なるBGMを要求する場合は、そのシーンのOnEnable()で自動的に切り替わる
            base.OnDisable();
        }

        private void Update()
        {
            if (_inputService == null || ViewModel == null)
            {
                return;
            }

            // ポーズボタンチェック
            if (_inputService.GetMenuButtonDown())
            {
                ViewModel.TogglePause();
            }
        }

        #endregion

        #region UIToolkitViewBase Overrides

        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            GetUIElements();
            RegisterEventHandlers();
            UpdateUIFromViewModel();

            // 初期状態ではポーズメニューを非表示
            _pauseMenu?.AddToClassList("hidden");
        }

        protected override void BindViewModel(TrainingViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            if (viewModel != null)
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                viewModel.PauseStateChanged += OnPauseStateChanged;
            }
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.PauseStateChanged -= OnPauseStateChanged;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
        }

        #endregion

        #region UI Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            if (RootVisualElement == null)
            {
                return;
            }

            _scoreLabel = Q<Label>("ScoreLabel");
            _arrowsFiredLabel = Q<Label>("ArrowsFiredLabel");
            _hitsLabel = Q<Label>("HitsLabel");
            _accuracyLabel = Q<Label>("AccuracyLabel");
            _pauseMenu = Q<VisualElement>("PauseMenu");
            _resumeButton = Q<Button>("ResumeButton");
            _settingsButton = Q<Button>("SettingsButton");
            _backToMenuButton = Q<Button>("BackToMenuButton");
        }

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (_resumeButton != null)
            {
                _resumeButton.clicked += OnResumeClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked += OnSettingsClicked;
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.clicked += OnBackToMenuClicked;
            }
        }

        /// <summary>
        /// イベントハンドラを解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_resumeButton != null)
            {
                _resumeButton.clicked -= OnResumeClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked -= OnSettingsClicked;
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.clicked -= OnBackToMenuClicked;
            }
        }

        /// <summary>
        /// ViewModelの値でUIを初期化します
        /// </summary>
        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            UpdateStatistics();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更時の処理
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(TrainingViewModel.Score):
                case nameof(TrainingViewModel.ArrowsFired):
                case nameof(TrainingViewModel.Hits):
                case nameof(TrainingViewModel.Accuracy):
                    UpdateStatistics();
                    break;
            }
        }

        /// <summary>
        /// ポーズ状態変更時の処理
        /// </summary>
        private void OnPauseStateChanged(object? sender, System.EventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (ViewModel.IsPaused)
            {
                ShowPauseMenu();
            }
            else
            {
                HidePauseMenu();
            }
        }

        /// <summary>
        /// Resume ボタンクリック時の処理
        /// </summary>
        private void OnResumeClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.Resume();
        }

        /// <summary>
        /// Settings ボタンクリック時の処理
        /// </summary>
        private void OnSettingsClicked()
        {
            PlayButtonClickSfx();

            // 設定画面を開く
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (sceneService != null)
            {
                sceneService.LoadSettings();
                Debug.Log("[TrainingView] Opening Settings scene.");
            }
            else
            {
                Debug.LogWarning("[TrainingView] ISceneManagementService not available.");
            }
        }

        /// <summary>
        /// Back to Menu ボタンクリック時の処理
        /// </summary>
        private void OnBackToMenuClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.BackToMainMenu();
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// 統計表示を更新します
        /// </summary>
        private void UpdateStatistics()
        {
            if (ViewModel == null)
            {
                return;
            }

            if (_scoreLabel != null)
            {
                _scoreLabel.text = $"Score: {ViewModel.Score}";
            }

            if (_arrowsFiredLabel != null)
            {
                _arrowsFiredLabel.text = $"Arrows: {ViewModel.ArrowsFired}";
            }

            if (_hitsLabel != null)
            {
                _hitsLabel.text = $"Hits: {ViewModel.Hits}";
            }

            if (_accuracyLabel != null)
            {
                _accuracyLabel.text = $"Accuracy: {ViewModel.Accuracy:F1}%";
            }
        }

        /// <summary>
        /// ポーズメニューを表示します
        /// </summary>
        private void ShowPauseMenu()
        {
            _pauseMenu?.RemoveFromClassList("hidden");

            // ゲーム時間を停止
            Time.timeScale = 0f;

            // 入力を無効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = false;
            }

            Debug.Log("[TrainingView] Pause menu shown.");
        }

        /// <summary>
        /// ポーズメニューを非表示にします
        /// </summary>
        private void HidePauseMenu()
        {
            _pauseMenu?.AddToClassList("hidden");

            // ゲーム時間を再開
            Time.timeScale = 1f;

            // 入力を有効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = true;
            }

            Debug.Log("[TrainingView] Pause menu hidden.");
        }

        #endregion

        #region Audio

        /// <summary>
        /// ボタンクリック音を再生します
        /// </summary>
        private void PlayButtonClickSfx()
        {
            if (_buttonClickSfx != null && _audioService != null)
            {
                _audioService.PlaySfx(_buttonClickSfx);
            }
        }

        #endregion

        #region Training Events

        /// <summary>
        /// 矢が発射された時の処理
        /// </summary>
        private void OnArrowFired(object? sender, System.EventArgs e)
        {
            ViewModel?.RecordArrowFired();
        }

        /// <summary>
        /// ターゲットに命中した時の処理
        /// </summary>
        private void OnTargetHit(object? sender, System.EventArgs e)
        {
            ViewModel?.RecordHit();
        }

        /// <summary>
        /// スコアを獲得した時の処理
        /// </summary>
        private void OnScoreEarned(object? sender, ScoreEarnedEventArgs e)
        {
            ViewModel?.RecordScore(e.Score);

            // スコアポップアップ表示
            ShowScorePopup(e.Score, e.HitPosition);

            Debug.Log($"[TrainingView] Score earned: {e.Score} at {e.HitPosition}");
        }

        /// <summary>
        /// スコアポップアップを表示します
        /// </summary>
        /// <param name="score">スコア</param>
        /// <param name="position">表示位置</param>
        private void ShowScorePopup(int score, Vector3 position)
        {
            if (_scorePopupPrefab == null)
            {
                return;
            }

            // 少し上にオフセット
            Vector3 popupPosition = position + Vector3.up * 0.5f;

            // スコアポップアップをインスタンス化
            GameObject popupObj = Instantiate(_scorePopupPrefab, popupPosition, Quaternion.identity);

            // スコアを設定
            ScorePopup? popup = popupObj.GetComponent<ScorePopup>();
            if (popup != null)
            {
                // スコアに応じて色を変える
                Color popupColor = GetScoreColor(score);
                popup.SetScore(score, popupColor);
            }
        }

        /// <summary>
        /// スコアに応じた色を取得します
        /// </summary>
        /// <param name="score">スコア</param>
        /// <returns>色</returns>
        private Color GetScoreColor(int score)
        {
            // スコアに応じてグラデーション
            if (score >= 20) // フルチャージ
            {
                return new Color(1f, 0.84f, 0f); // ゴールド
            }
            else if (score >= 15)
            {
                return new Color(1f, 0.5f, 0f); // オレンジ
            }
            else if (score >= 10)
            {
                return Color.yellow;
            }
            else
            {
                return Color.white;
            }
        }

        #endregion
    }
}
