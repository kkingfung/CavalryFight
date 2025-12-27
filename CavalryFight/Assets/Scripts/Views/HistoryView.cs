#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Replay;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// 履歴/リプレイ一覧画面のView
    /// </summary>
    /// <remarks>
    /// UI Toolkitを使用してリプレイの一覧と詳細を表示します。
    /// HistoryViewModelとバインドされ、ユーザー操作を処理します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class HistoryView : UIToolkitViewBase<HistoryViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;

        #endregion

        #region UI Elements

        // Header
        private Button? _refreshButton;

        // Left Panel - List
        private VisualElement? _replayListContainer;
        private VisualElement? _emptyState;
        private ScrollView? _replayListScrollView;

        // Right Panel - Details
        private VisualElement? _noSelectionState;
        private VisualElement? _detailsContent;
        private Label? _dateLabel;
        private Label? _mapLabel;
        private Label? _gameModeLabel;
        private Label? _durationLabel;
        private Label? _playerScoreLabel;
        private Label? _enemyScoreLabel;
        private Label? _resultLabel;
        private Button? _watchReplayButton;
        private Button? _deleteButton;

        // Footer
        private Button? _backButton;

        #endregion

        #region Fields

        private readonly Dictionary<string, VisualElement> _replayItemElements = new Dictionary<string, VisualElement>();
        private VisualElement? _currentSelectedElement;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // サービスを取得
            var replayService = ServiceLocator.Instance.Get<IReplayService>();
            if (replayService == null)
            {
                Debug.LogError("[HistoryView] IReplayService not found in ServiceLocator!", this);
                return;
            }

            // ViewModelを作成してバインド
            ViewModel = new HistoryViewModel(replayService);
        }

        /// <summary>
        /// 有効化時の処理
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // BGMを再生
            if (_bgmClip != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlayBgm(_bgmClip, loop: true, fadeInDuration: 2f);
                }
            }
        }

        /// <summary>
        /// 無効化時の処理
        /// </summary>
        protected override void OnDisable()
        {
            // BGMは停止しない（シーン遷移時の継続再生のため）
            // 次のシーンが異なるBGMを要求する場合は、そのシーンのOnEnable()で自動的に切り替わる
            base.OnDisable();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// RootVisualElementが準備できた時に呼び出されます
        /// </summary>
        /// <param name="root">ルートVisualElement</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            GetUIElements();

            // UI要素の検証
            ValidateUIElements();

            // イベントハンドラを登録
            RegisterEventHandlers();

            // 初期状態を設定
            UpdateEmptyState();
            UpdateDetailsVisibility();
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(HistoryViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // ViewModelのイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.ReplaySelected += OnReplaySelected;
            viewModel.ListUpdated += OnListUpdated;
            viewModel.BackToMenuRequested += OnBackToMenuRequested;

            // リプレイリストを初期化
            PopulateReplayList();
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します
        /// </summary>
        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.ReplaySelected -= OnReplaySelected;
                ViewModel.ListUpdated -= OnListUpdated;
                ViewModel.BackToMenuRequested -= OnBackToMenuRequested;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
        }

        #endregion

        #region UI Element Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            // Header
            _refreshButton = Q<Button>("RefreshButton");

            // Left Panel
            _replayListContainer = Q<VisualElement>("ReplayListContainer");
            _emptyState = Q<VisualElement>("EmptyState");
            _replayListScrollView = Q<ScrollView>("ReplayListScrollView");

            // Right Panel
            _noSelectionState = Q<VisualElement>("NoSelectionState");
            _detailsContent = Q<VisualElement>("DetailsContent");
            _dateLabel = Q<Label>("DateLabel");
            _mapLabel = Q<Label>("MapLabel");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _durationLabel = Q<Label>("DurationLabel");
            _playerScoreLabel = Q<Label>("PlayerScoreLabel");
            _enemyScoreLabel = Q<Label>("EnemyScoreLabel");
            _resultLabel = Q<Label>("ResultLabel");
            _watchReplayButton = Q<Button>("WatchReplayButton");
            _deleteButton = Q<Button>("DeleteButton");

            // Footer
            _backButton = Q<Button>("BackButton");
        }

        /// <summary>
        /// UI要素が正しく取得できているか検証します
        /// </summary>
        private void ValidateUIElements()
        {
            if (_refreshButton == null)
            {
                Debug.LogWarning("[HistoryView] RefreshButton not found!", this);
            }

            if (_replayListContainer == null)
            {
                Debug.LogWarning("[HistoryView] ReplayListContainer not found!", this);
            }

            if (_emptyState == null)
            {
                Debug.LogWarning("[HistoryView] EmptyState not found!", this);
            }

            if (_noSelectionState == null)
            {
                Debug.LogWarning("[HistoryView] NoSelectionState not found!", this);
            }

            if (_detailsContent == null)
            {
                Debug.LogWarning("[HistoryView] DetailsContent not found!", this);
            }

            if (_backButton == null)
            {
                Debug.LogWarning("[HistoryView] BackButton not found!", this);
            }
        }

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (_refreshButton != null)
            {
                _refreshButton.clicked += OnRefreshButtonClicked;
            }

            if (_watchReplayButton != null)
            {
                _watchReplayButton.clicked += OnWatchReplayButtonClicked;
            }

            if (_deleteButton != null)
            {
                _deleteButton.clicked += OnDeleteButtonClicked;
            }

            if (_backButton != null)
            {
                _backButton.clicked += OnBackButtonClicked;
            }
        }

        /// <summary>
        /// イベントハンドラを解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_refreshButton != null)
            {
                _refreshButton.clicked -= OnRefreshButtonClicked;
            }

            if (_watchReplayButton != null)
            {
                _watchReplayButton.clicked -= OnWatchReplayButtonClicked;
            }

            if (_deleteButton != null)
            {
                _deleteButton.clicked -= OnDeleteButtonClicked;
            }

            if (_backButton != null)
            {
                _backButton.clicked -= OnBackButtonClicked;
            }
        }

        #endregion

        #region Replay List Population

        /// <summary>
        /// リプレイリストを生成します
        /// </summary>
        private void PopulateReplayList()
        {
            if (ViewModel == null || _replayListContainer == null)
            {
                return;
            }

            // 既存のリストをクリア
            _replayListContainer.Clear();
            _replayItemElements.Clear();

            // リプレイアイテムを作成
            foreach (var replay in ViewModel.ReplayList)
            {
                var replayItem = CreateReplayListItem(replay);
                _replayListContainer.Add(replayItem);
                _replayItemElements[replay.ReplayId] = replayItem;
            }

            UpdateEmptyState();
        }

        /// <summary>
        /// リプレイリストアイテムのUI要素を作成します
        /// </summary>
        /// <param name="metadata">リプレイメタデータ</param>
        /// <returns>作成されたVisualElement</returns>
        private VisualElement CreateReplayListItem(ReplayMetadata metadata)
        {
            var container = new VisualElement();
            container.AddToClassList("replay-item");
            container.name = $"ReplayItem_{metadata.ReplayId}";

            // ヘッダー行（マップ名 + 結果）
            var header = new VisualElement();
            header.AddToClassList("replay-item-header");

            var mapLabel = new Label(metadata.MapName);
            mapLabel.AddToClassList("replay-item-map");
            header.Add(mapLabel);

            var resultLabel = new Label(metadata.ResultText);
            resultLabel.AddToClassList("replay-item-result");
            if (metadata.IsPlayerVictory)
            {
                resultLabel.AddToClassList("result-victory");
            }
            else if (metadata.IsDraw)
            {
                resultLabel.AddToClassList("result-draw");
            }
            else
            {
                resultLabel.AddToClassList("result-defeat");
            }
            header.Add(resultLabel);

            container.Add(header);

            // 情報行（日時 + スコア）
            var info = new VisualElement();
            info.AddToClassList("replay-item-info");

            var dateLabel = new Label(metadata.DateText);
            dateLabel.AddToClassList("replay-item-date");
            info.Add(dateLabel);

            var scoreLabel = new Label(metadata.ScoreText);
            scoreLabel.AddToClassList("replay-item-score");
            info.Add(scoreLabel);

            container.Add(info);

            // クリックイベント
            container.RegisterCallback<ClickEvent>(evt =>
            {
                OnReplayItemClicked(metadata.ReplayId);
            });

            return container;
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// 空リスト状態の表示/非表示を更新します
        /// </summary>
        private void UpdateEmptyState()
        {
            if (ViewModel == null || _emptyState == null || _replayListScrollView == null)
            {
                return;
            }

            if (ViewModel.IsEmpty)
            {
                _emptyState.style.display = DisplayStyle.Flex;
                _replayListScrollView.style.display = DisplayStyle.None;
            }
            else
            {
                _emptyState.style.display = DisplayStyle.None;
                _replayListScrollView.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 詳細パネルの表示/非表示を更新します
        /// </summary>
        private void UpdateDetailsVisibility()
        {
            if (_noSelectionState == null || _detailsContent == null)
            {
                return;
            }

            if (ViewModel == null || ViewModel.SelectedReplay == null)
            {
                _noSelectionState.style.display = DisplayStyle.Flex;
                _detailsContent.style.display = DisplayStyle.None;
            }
            else
            {
                _noSelectionState.style.display = DisplayStyle.None;
                _detailsContent.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 詳細パネルの内容を更新します
        /// </summary>
        private void UpdateDetailsContent()
        {
            if (ViewModel == null || ViewModel.SelectedReplay == null)
            {
                return;
            }

            var replay = ViewModel.SelectedReplay;

            // 基本情報
            if (_dateLabel != null)
            {
                _dateLabel.text = replay.DateText;
            }

            if (_mapLabel != null)
            {
                _mapLabel.text = replay.MapName;
            }

            if (_gameModeLabel != null)
            {
                _gameModeLabel.text = replay.GameMode;
            }

            if (_durationLabel != null)
            {
                _durationLabel.text = replay.DurationText;
            }

            // スコア
            if (_playerScoreLabel != null)
            {
                _playerScoreLabel.text = replay.FinalPlayerScore.ToString();
            }

            if (_enemyScoreLabel != null)
            {
                _enemyScoreLabel.text = replay.FinalEnemyScore.ToString();
            }

            // 結果
            if (_resultLabel != null)
            {
                _resultLabel.text = replay.ResultText;

                // スタイルをクリア
                _resultLabel.style.color = Color.white;

                // 結果に応じた色を設定
                if (replay.IsPlayerVictory)
                {
                    _resultLabel.style.color = new Color(0.4f, 1f, 0.4f); // 緑
                }
                else if (replay.IsDraw)
                {
                    _resultLabel.style.color = new Color(0.8f, 0.8f, 0.8f); // グレー
                }
                else
                {
                    _resultLabel.style.color = new Color(1f, 0.4f, 0.4f); // 赤
                }
            }
        }

        /// <summary>
        /// リストアイテムの選択状態を更新します
        /// </summary>
        /// <param name="replayId">選択されたリプレイID</param>
        private void UpdateListItemSelection(string replayId)
        {
            // 前回の選択を解除
            if (_currentSelectedElement != null)
            {
                _currentSelectedElement.RemoveFromClassList("replay-item-selected");
            }

            // 新しい選択を適用
            if (_replayItemElements.TryGetValue(replayId, out var element))
            {
                element.AddToClassList("replay-item-selected");
                _currentSelectedElement = element;

                // スクロールして表示
                element.ScrollToElement();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更イベントを処理します
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(HistoryViewModel.SelectedReplay):
                    UpdateDetailsVisibility();
                    UpdateDetailsContent();
                    break;

                case nameof(HistoryViewModel.ReplayList):
                    PopulateReplayList();
                    UpdateEmptyState();
                    break;

                case nameof(HistoryViewModel.IsEmpty):
                    UpdateEmptyState();
                    break;
            }
        }

        /// <summary>
        /// リプレイが選択された時の処理
        /// </summary>
        private void OnReplaySelected(object? sender, ReplayMetadata replay)
        {
            UpdateListItemSelection(replay.ReplayId);
            UpdateDetailsContent();
        }

        /// <summary>
        /// リストが更新された時の処理
        /// </summary>
        private void OnListUpdated(object? sender, EventArgs e)
        {
            PopulateReplayList();
        }

        /// <summary>
        /// メインメニューに戻る要求を処理します
        /// </summary>
        private void OnBackToMenuRequested(object? sender, EventArgs e)
        {
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            sceneService?.LoadMainMenu();
        }

        /// <summary>
        /// リプレイアイテムがクリックされた時の処理
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        private void OnReplayItemClicked(string replayId)
        {
            ViewModel?.SelectReplayCommand.Execute(replayId);
        }

        /// <summary>
        /// リフレッシュボタンがクリックされた時の処理
        /// </summary>
        private void OnRefreshButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.RefreshListCommand.Execute(null);
        }

        /// <summary>
        /// 視聴ボタンがクリックされた時の処理
        /// </summary>
        private void OnWatchReplayButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.WatchReplayCommand.Execute(null);
        }

        /// <summary>
        /// 削除ボタンがクリックされた時の処理
        /// </summary>
        private void OnDeleteButtonClicked()
        {
            if (ViewModel?.SelectedReplay == null)
            {
                return;
            }

            PlayButtonClickSfx();

            // 確認ダイアログ（将来的には実装）
            Debug.Log($"[HistoryView] Delete requested for: {ViewModel.SelectedReplay.MapName}");

            ViewModel.DeleteReplayCommand.Execute(null);
        }

        /// <summary>
        /// 戻るボタンがクリックされた時の処理
        /// </summary>
        private void OnBackButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.BackToMenuCommand.Execute(null);
        }

        #endregion

        #region Private Methods - Audio

        /// <summary>
        /// ボタンクリック効果音を再生します
        /// </summary>
        private void PlayButtonClickSfx()
        {
            if (_buttonClickSfx != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlaySfx(_buttonClickSfx);
                }
            }
        }

        #endregion

        #region Extensions

        /// <summary>
        /// ScrollToElement のためのヘルパーメソッド
        /// </summary>
        private static class VisualElementExtensions
        {
            public static void ScrollToElement(VisualElement element)
            {
                // 親のScrollViewを見つける
                var parent = element.parent;
                while (parent != null && !(parent is ScrollView))
                {
                    parent = parent.parent;
                }

                if (parent is ScrollView scrollView)
                {
                    // 要素が見えるようにスクロール
                    float offset = element.layout.y;
                    scrollView.scrollOffset = new Vector2(0, offset);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// VisualElement拡張メソッド
    /// </summary>
    internal static class VisualElementExtensions
    {
        /// <summary>
        /// 要素をスクロールビュー内で表示させます
        /// </summary>
        public static void ScrollToElement(this VisualElement element)
        {
            var parent = element.parent;
            while (parent != null && !(parent is ScrollView))
            {
                parent = parent.parent;
            }

            if (parent is ScrollView scrollView)
            {
                float offset = element.layout.y;
                scrollView.scrollOffset = new Vector2(0, offset);
            }
        }
    }
}
