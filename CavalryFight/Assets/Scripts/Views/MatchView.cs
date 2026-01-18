#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Input;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using CavalryFight.Services.Training;
using CavalryFight.ViewModels;
using CavalryFight.ViewModels.Data;
using CavalryFight.Views.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// マッチシーンのView
    /// </summary>
    /// <remarks>
    /// マッチ中のHUD、スコアボード、ポーズメニューを管理します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class MatchView : UIToolkitViewBase<MatchViewModel>
    {
        #region UI Elements

        // HUD
        private VisualElement? _hudPanel;
        private Label? _scoreLabel;
        private Label? _timerLabel;
        private Label? _arrowsLabel;
        private Label? _hitsLabel;
        private Label? _accuracyLabel;
        private Label? _gameModeBadge;

        // Team Scores (TeamFight mode)
        private VisualElement? _teamScorePanel;
        private Label? _team0ScoreLabel;
        private Label? _team1ScoreLabel;

        // Countdown
        private VisualElement? _countdownPanel;
        private Label? _countdownLabel;

        // Scoreboard
        private VisualElement? _scoreboardPanel;
        private VisualElement? _scoreboardContainer;

        // Settings Popup (shown when paused)
        private VisualElement? _settingsPopup;
        private SettingsPopupController? _settingsPopupController;
        private Button? _resumeButton;
        private Button? _leaveMatchButton;

        // Match End
        private VisualElement? _matchEndPanel;
        private Label? _matchResultLabel;
        private Label? _finalScoreLabel;
        private Button? _continueButton;

        // Kill Feed
        private VisualElement? _killFeedContainer;
        private readonly Queue<VisualElement> _killFeedEntries = new Queue<VisualElement>();
        private const int MaxKillFeedEntries = 2;
        private const float KillFeedEntryDuration = 5f;

        // Crosshair
        private VisualElement? _crosshair;

        #endregion

        #region Services

        private IAudioService? _audioService;
        private IInputService? _inputService;
        private ISceneManagementService? _sceneService;

        #endregion

        #region Crosshair State

        private GameObject? _localPlayerObject;
        private bool _wasCharging = false;

        #endregion

        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _buttonClickSound;

        [Header("Score Popup")]
        [SerializeField] private GameObject? _scorePopupPrefab;

        [Header("Key Bindings")]
        [SerializeField] private KeyBindingView? _keyBindingView;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

            if (_sceneService != null)
            {
                ViewModel = new MatchViewModel(_sceneService);
            }
        }

        private void Update()
        {
            // ViewModelの更新
            ViewModel?.Update();

            // 入力処理
            HandleInput();

            // クロスヘア更新
            UpdateCrosshair();
        }

        /// <summary>
        /// 入力を処理します
        /// </summary>
        private void HandleInput()
        {
            if (_inputService == null)
            {
                return;
            }

            // スコアボード表示切り替え（Tabキー）
            if (_inputService.GetScoreboardButtonDown())
            {
                ViewModel?.ShowScoreboard();
            }
            else if (_inputService.GetScoreboardButtonUp())
            {
                ViewModel?.HideScoreboard();
            }

            // ポーズ切り替え（Escapeキー）
            if (_inputService.GetMenuButtonDown())
            {
                ViewModel?.TogglePause();
            }
        }

        #endregion

        #region UI Setup

        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            GetUIElements();
            RegisterEventHandlers();
            UpdateUIFromViewModel();
        }

        private void GetUIElements()
        {
            // HUD
            _hudPanel = Q<VisualElement>("HUDPanel");
            _scoreLabel = Q<Label>("ScoreLabel");
            _timerLabel = Q<Label>("TimerLabel");
            _arrowsLabel = Q<Label>("ArrowsLabel");
            _hitsLabel = Q<Label>("HitsLabel");
            _accuracyLabel = Q<Label>("AccuracyLabel");
            _gameModeBadge = Q<Label>("GameModeBadge");

            // Team Scores
            _teamScorePanel = Q<VisualElement>("TeamScorePanel");
            _team0ScoreLabel = Q<Label>("Team0ScoreLabel");
            _team1ScoreLabel = Q<Label>("Team1ScoreLabel");

            // Countdown
            _countdownPanel = Q<VisualElement>("CountdownPanel");
            _countdownLabel = Q<Label>("CountdownLabel");

            // Scoreboard
            _scoreboardPanel = Q<VisualElement>("ScoreboardPanel");
            _scoreboardContainer = Q<VisualElement>("ScoreboardContainer");

            // Settings Popup
            _settingsPopup = Q<VisualElement>("SettingsPopup");
            if (_settingsPopup != null)
            {
                // SettingsPopup内からボタンを取得
                _resumeButton = _settingsPopup.Q<Button>("ResumeButton");
                _leaveMatchButton = _settingsPopup.Q<Button>("LeaveMatchButton");

                _settingsPopupController = new SettingsPopupController(
                    _settingsPopup,
                    _keyBindingView,
                    _buttonClickSound);
                _settingsPopupController.ResumeRequested += OnSettingsResumeRequested;
                _settingsPopupController.BackToMenuRequested += OnSettingsBackToMenuRequested;
            }

            // Match End
            _matchEndPanel = Q<VisualElement>("MatchEndPanel");
            _matchResultLabel = Q<Label>("MatchResultLabel");
            _finalScoreLabel = Q<Label>("FinalScoreLabel");
            _continueButton = Q<Button>("ContinueButton");

            // Kill Feed
            _killFeedContainer = Q<VisualElement>("KillFeedContainer");

            // Crosshair
            _crosshair = Q<VisualElement>("Crosshair");
            if (_crosshair != null)
            {
                Debug.Log("[MatchView] Crosshair element found.");
            }

            // CrosshairContainerはマウスイベントを無視（UIの下のゲームをクリック可能に）
            var crosshairContainer = Q<VisualElement>("CrosshairContainer");
            if (crosshairContainer != null)
            {
                crosshairContainer.pickingMode = PickingMode.Ignore;
            }
        }

        private void RegisterEventHandlers()
        {
            // Settings Popup buttons
            _resumeButton?.RegisterCallback<ClickEvent>(OnResumeClicked);
            _leaveMatchButton?.RegisterCallback<ClickEvent>(OnLeaveMatchClicked);

            // Match End
            _continueButton?.RegisterCallback<ClickEvent>(OnContinueClicked);
        }

        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            // ゲームモードバッジ
            if (_gameModeBadge != null)
            {
                _gameModeBadge.text = ViewModel.GameModeName;
            }

            // チームスコアパネルの表示/非表示
            if (_teamScorePanel != null)
            {
                _teamScorePanel.style.display = ViewModel.IsTeamMode
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            // 初期状態を反映
            UpdateHUD();
            UpdatePausePanel();
            UpdateCountdownPanel();
            UpdateMatchEndPanel();
        }

        #endregion

        #region UI Updates

        private void UpdateHUD()
        {
            if (ViewModel == null)
            {
                return;
            }

            if (_scoreLabel != null)
            {
                _scoreLabel.text = ViewModel.LocalPlayerScore.ToString();
            }

            if (_timerLabel != null)
            {
                _timerLabel.text = ViewModel.RemainingTimeText;
            }

            if (_arrowsLabel != null)
            {
                _arrowsLabel.text = ViewModel.ArrowsText;
            }

            if (_hitsLabel != null)
            {
                _hitsLabel.text = ViewModel.LocalPlayerHits.ToString();
            }

            if (_accuracyLabel != null)
            {
                _accuracyLabel.text = ViewModel.AccuracyText;
            }

            // チームスコア
            if (ViewModel.IsTeamMode)
            {
                if (_team0ScoreLabel != null)
                {
                    _team0ScoreLabel.text = ViewModel.Team0Score.ToString();
                }

                if (_team1ScoreLabel != null)
                {
                    _team1ScoreLabel.text = ViewModel.Team1Score.ToString();
                }
            }
        }

        private void UpdatePausePanel()
        {
            if (_settingsPopup == null || ViewModel == null)
            {
                return;
            }

            if (ViewModel.IsPaused)
            {
                _settingsPopupController?.Show();
            }
            else
            {
                _settingsPopupController?.Hide();
            }

            // ポーズ時はHUDを薄くする
            if (_hudPanel != null)
            {
                _hudPanel.style.opacity = ViewModel.IsPaused ? 0.5f : 1f;
            }
        }

        private void UpdateCountdownPanel()
        {
            if (_countdownPanel == null || ViewModel == null)
            {
                return;
            }

            bool showCountdown = ViewModel.IsCountingDown && ViewModel.CountdownValue > 0;
            _countdownPanel.style.display = showCountdown
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (_countdownLabel != null && showCountdown)
            {
                _countdownLabel.text = ViewModel.CountdownValue.ToString();
            }
        }

        private void UpdateScoreboard()
        {
            if (_scoreboardPanel == null || _scoreboardContainer == null || ViewModel == null)
            {
                return;
            }

            _scoreboardPanel.style.display = ViewModel.IsScoreboardVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!ViewModel.IsScoreboardVisible)
            {
                return;
            }

            // スコアボードをクリアして再構築
            _scoreboardContainer.Clear();

            foreach (var player in ViewModel.PlayerScores)
            {
                var row = CreateScoreboardRow(player);
                _scoreboardContainer.Add(row);
            }
        }

        private VisualElement CreateScoreboardRow(PlayerScoreInfo player)
        {
            var row = new VisualElement();
            row.AddToClassList("scoreboard-row");

            if (player.IsLocalPlayer)
            {
                row.AddToClassList("scoreboard-row--local");
            }

            // プレイヤー名
            var nameLabel = new Label(player.PlayerName);
            nameLabel.AddToClassList("scoreboard-name");
            row.Add(nameLabel);

            // スコア
            var scoreLabel = new Label(player.Score.ToString());
            scoreLabel.AddToClassList("scoreboard-score");
            row.Add(scoreLabel);

            // ヒット数
            var hitsLabel = new Label(player.Hits.ToString());
            hitsLabel.AddToClassList("scoreboard-hits");
            row.Add(hitsLabel);

            // 命中率
            var accuracyLabel = new Label(player.AccuracyText);
            accuracyLabel.AddToClassList("scoreboard-accuracy");
            row.Add(accuracyLabel);

            return row;
        }

        private void UpdateMatchEndPanel()
        {
            if (_matchEndPanel == null || ViewModel == null)
            {
                return;
            }

            _matchEndPanel.style.display = ViewModel.IsMatchEnded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void ShowMatchResult(MatchEndResult result)
        {
            if (_matchEndPanel == null || _matchResultLabel == null || _finalScoreLabel == null)
            {
                return;
            }

            _matchEndPanel.style.display = DisplayStyle.Flex;

            // 結果テキストを設定
            ulong localClientId = 0;
            if (ViewModel != null)
            {
                localClientId = ViewModel.LocalClientId;
            }
            bool isDraw = result.WinnerId == ulong.MaxValue;
            bool isWinner = !isDraw && result.WinnerId == localClientId;

            if (isDraw)
            {
                _matchResultLabel.text = "DRAW";
                _matchResultLabel.RemoveFromClassList("result-victory");
                _matchResultLabel.RemoveFromClassList("result-defeat");
                _matchResultLabel.AddToClassList("result-draw");
            }
            else
            {
                _matchResultLabel.text = isWinner ? "VICTORY" : "DEFEAT";
                _matchResultLabel.RemoveFromClassList("result-victory");
                _matchResultLabel.RemoveFromClassList("result-defeat");
                _matchResultLabel.RemoveFromClassList("result-draw");
                _matchResultLabel.AddToClassList(isWinner ? "result-victory" : "result-defeat");
            }

            _finalScoreLabel.text = $"Score: {ViewModel?.LocalPlayerScore ?? 0}";
        }

        #endregion

        #region Score Popup

        /// <summary>
        /// スコアポップアップを表示します（3D空間に表示）
        /// </summary>
        /// <param name="score">スコア</param>
        /// <param name="worldPosition">表示位置（ワールド座標）</param>
        private void ShowScorePopup(int score, Vector3 worldPosition)
        {
            if (_scorePopupPrefab == null)
            {
                return;
            }

            // 少し上にオフセット
            Vector3 popupPosition = worldPosition + Vector3.up * 0.5f;

            // スコアポップアップをインスタンス化
            GameObject popupObj = Instantiate(_scorePopupPrefab, popupPosition, Quaternion.identity);

            // スコアを設定
            ScorePopup? popup = popupObj.GetComponent<ScorePopup>();
            if (popup != null)
            {
                Color popupColor = GetScoreColor(score);
                popup.SetScore(score, popupColor);
            }
        }

        /// <summary>
        /// スコアに応じた色を取得します
        /// </summary>
        private Color GetScoreColor(int score)
        {
            if (score >= 50)
            {
                return new Color(1f, 0.2f, 0.2f); // 赤（クリティカル）
            }
            else if (score >= 30)
            {
                return new Color(1f, 0.84f, 0f); // ゴールド
            }
            else if (score >= 20)
            {
                return new Color(1f, 0.5f, 0f); // オレンジ
            }
            else
            {
                return Color.white;
            }
        }

        #endregion

        #region Event Handlers

        private void OnResumeClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.Resume();
        }

        private void OnSettingsResumeRequested(object? sender, EventArgs e)
        {
            ViewModel?.Resume();
        }

        private void OnSettingsBackToMenuRequested(object? sender, EventArgs e)
        {
            ViewModel?.LeaveMatch();
        }

        private void OnLeaveMatchClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.LeaveMatch();
        }

        private void OnContinueClicked(ClickEvent evt)
        {
            PlayButtonSound();
            _sceneService?.LoadResults();
        }

        #endregion

        #region ViewModel Binding

        protected override void BindViewModel(MatchViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.PauseStateChanged += OnPauseStateChanged;
            viewModel.MatchStarted += OnMatchStarted;
            viewModel.MatchEnded += OnMatchEnded;
            viewModel.ScoreGained += OnScoreGained;
            viewModel.PlayerKilled += OnPlayerKilled;
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.PauseStateChanged -= OnPauseStateChanged;
                ViewModel.MatchStarted -= OnMatchStarted;
                ViewModel.MatchEnded -= OnMatchEnded;
                ViewModel.ScoreGained -= OnScoreGained;
                ViewModel.PlayerKilled -= OnPlayerKilled;
            }
            base.UnbindViewModel();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MatchViewModel.LocalPlayerScore):
                case nameof(MatchViewModel.RemainingTimeText):
                case nameof(MatchViewModel.ArrowsText):
                case nameof(MatchViewModel.LocalPlayerHits):
                case nameof(MatchViewModel.AccuracyText):
                case nameof(MatchViewModel.Team0Score):
                case nameof(MatchViewModel.Team1Score):
                    UpdateHUD();
                    break;

                case nameof(MatchViewModel.CountdownValue):
                case nameof(MatchViewModel.IsCountingDown):
                    UpdateCountdownPanel();
                    break;

                case nameof(MatchViewModel.IsScoreboardVisible):
                case nameof(MatchViewModel.PlayerScores):
                    UpdateScoreboard();
                    break;

                case nameof(MatchViewModel.IsMatchEnded):
                    UpdateMatchEndPanel();
                    break;
            }
        }

        private void OnPauseStateChanged(object? sender, bool isPaused)
        {
            UpdatePausePanel();
            Time.timeScale = isPaused ? 0f : 1f;
        }

        private void OnMatchStarted(object? sender, EventArgs e)
        {
            // カウントダウンパネルを非表示
            if (_countdownPanel != null)
            {
                _countdownPanel.style.display = DisplayStyle.None;
            }
        }

        private void OnMatchEnded(object? sender, MatchEndResult result)
        {
            ShowMatchResult(result);
        }

        private void OnScoreGained(object? sender, ScoreGainedEventArgs e)
        {
            // ローカルプレイヤーのスコアのみポップアップ表示
            // ワールド座標からの変換は現在未実装。スコアポップアップは画面中央に表示。
            if (ViewModel != null)
            {
                ShowScorePopup(e.Score, Vector3.zero);
            }
        }

        private void OnPlayerKilled(object? sender, PlayerKilledEventArgs e)
        {
            ShowKillFeedEntry(e.KillerName, e.VictimName);
        }

        #endregion

        #region Kill Feed

        /// <summary>
        /// キルフィードにエントリを追加します
        /// </summary>
        /// <param name="killerName">キラー名</param>
        /// <param name="victimName">被害者名</param>
        private void ShowKillFeedEntry(string killerName, string victimName)
        {
            if (_killFeedContainer == null)
            {
                return;
            }

            // 最大数を超えている場合は古いエントリを削除
            while (_killFeedEntries.Count >= MaxKillFeedEntries)
            {
                var oldEntry = _killFeedEntries.Dequeue();
                oldEntry.RemoveFromHierarchy();
            }

            // 新しいエントリを作成
            var entry = CreateKillFeedEntry(killerName, victimName);
            _killFeedContainer.Add(entry);
            _killFeedEntries.Enqueue(entry);

            // 一定時間後に削除
            entry.schedule.Execute(() =>
            {
                if (_killFeedEntries.Count > 0 && _killFeedEntries.Peek() == entry)
                {
                    _killFeedEntries.Dequeue();
                }
                entry.RemoveFromHierarchy();
            }).StartingIn((long)(KillFeedEntryDuration * 1000));

            Debug.Log($"[MatchView] Kill feed: {killerName} killed {victimName}");
        }

        /// <summary>
        /// キルフィードエントリのVisualElementを作成します
        /// </summary>
        private VisualElement CreateKillFeedEntry(string killerName, string victimName)
        {
            var entry = new VisualElement();
            entry.AddToClassList("kill-feed-entry");

            var killerLabel = new Label(killerName);
            killerLabel.AddToClassList("kill-feed-killer");
            entry.Add(killerLabel);

            var actionLabel = new Label("killed");
            actionLabel.AddToClassList("kill-feed-action");
            entry.Add(actionLabel);

            var victimLabel = new Label(victimName);
            victimLabel.AddToClassList("kill-feed-victim");
            entry.Add(victimLabel);

            return entry;
        }

        #endregion

        #region Audio

        private void PlayButtonSound()
        {
            PlaySound(_buttonClickSound);
        }

        private void PlaySound(AudioClip? clip)
        {
            if (clip != null && _audioService != null)
            {
                _audioService.PlaySfx(clip);
            }
        }

        #endregion

        #region Crosshair

        /// <summary>
        /// クロスヘアの表示/非表示を更新します
        /// </summary>
        private void UpdateCrosshair()
        {
            // ローカルプレイヤーを検索（まだ取得していない場合）
            if (_localPlayerObject == null)
            {
                FindLocalPlayer();
            }

            // プレイヤーが見つからない場合は何もしない
            if (_localPlayerObject == null)
            {
                return;
            }

            // チャージ状態を取得（PlayerControllerのIsChargingプロパティを使用）
            bool isCharging = GetPlayerChargingState();
            if (isCharging != _wasCharging)
            {
                _wasCharging = isCharging;
                if (isCharging)
                {
                    ShowCrosshair();
                }
                else
                {
                    HideCrosshair();
                }
            }
        }

        /// <summary>
        /// ローカルプレイヤーを検索します
        /// </summary>
        private void FindLocalPlayer()
        {
            // "PlayerController"コンポーネントを持つオブジェクトを検索
            var playerControllers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var controller in playerControllers)
            {
                if (controller.GetType().Name == "PlayerController")
                {
                    _localPlayerObject = controller.gameObject;
                    Debug.Log($"[MatchView] Found PlayerController: {controller.gameObject.name}");
                    break;
                }
            }
        }

        /// <summary>
        /// プレイヤーのチャージ状態を取得します
        /// </summary>
        private bool GetPlayerChargingState()
        {
            if (_localPlayerObject == null)
            {
                return false;
            }

            // PlayerControllerのIsChargingプロパティをリフレクションで取得
            var controller = _localPlayerObject.GetComponent("PlayerController");
            if (controller != null)
            {
                var property = controller.GetType().GetProperty("IsCharging");
                if (property != null)
                {
                    return (bool)property.GetValue(controller);
                }
            }

            return false;
        }

        /// <summary>
        /// クロスヘアを表示します
        /// </summary>
        private void ShowCrosshair()
        {
            if (_crosshair != null)
            {
                _crosshair.RemoveFromClassList("hidden");
            }
        }

        /// <summary>
        /// クロスヘアを非表示にします
        /// </summary>
        private void HideCrosshair()
        {
            if (_crosshair != null)
            {
                _crosshair.AddToClassList("hidden");
            }
        }

        #endregion

        #region Cleanup

        protected override void OnDestroy()
        {
            // TimeScaleを元に戻す
            Time.timeScale = 1f;

            // SettingsPopupControllerのクリーンアップ
            if (_settingsPopupController != null)
            {
                _settingsPopupController.ResumeRequested -= OnSettingsResumeRequested;
                _settingsPopupController.BackToMenuRequested -= OnSettingsBackToMenuRequested;
                _settingsPopupController.Dispose();
                _settingsPopupController = null;
            }

            base.OnDestroy();
        }

        #endregion
    }
}
