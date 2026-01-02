#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Performance;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using CavalryFight.ViewModels.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// マッチルーム画面のView
    /// </summary>
    /// <remarks>
    /// プレイヤーリスト、ルーム設定、準備状態を表示します。
    /// MatchRoomViewModelと連携して動作します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public partial class MatchRoomView : UIToolkitViewBase<MatchRoomViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;
        [SerializeField] private AudioClip? _countdownTickSfx;

        #endregion

        #region Fields

        private readonly Dictionary<string, VisualElement> _playerItemElements = new Dictionary<string, VisualElement>();
        private bool _isCountdownActive = false;
        private bool _isEditMode = false; // ホストの設定編集モード

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // サービスを取得（例外を回避するためTryGetを使用）
            var lobbyService = ServiceLocator.Instance.TryGet<ILobbyService>();
            var sceneService = ServiceLocator.Instance.TryGet<ISceneManagementService>();
            var performanceMonitor = ServiceLocator.Instance.TryGet<IPerformanceMonitor>();

            if (lobbyService == null || sceneService == null)
            {
                Debug.LogError("[MatchRoomView] Required services not found! Disabling component.", this);
                enabled = false;
                return;
            }

            // ViewModelを作成して設定（PerformanceMonitorはオプション）
            ViewModel = new MatchRoomViewModel(lobbyService, sceneService, performanceMonitor);
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
            base.OnDisable();
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {
            // ViewModelのカウントダウンタイマーを更新
            // タイマーロジックはViewModel内で管理されます
            if (_isCountdownActive && ViewModel != null)
            {
                ViewModel.Tick(Time.deltaTime);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// ルートビジュアル要素が準備できたときの処理
        /// </summary>
        /// <param name="root">ルートビジュアル要素</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            GetUIElements();

            // UI要素の検証
            ValidateUIElements();

            // ドロップダウンの選択肢を設定
            SetupDropdowns();

            // イベントハンドラを登録
            RegisterEventHandlers();

            // プレイヤーリストを初期化（UI要素が準備できた後）
            PopulatePlayerList();

            // 初期状態を設定
            UpdateUI();

            // ゲーム設定の有効/無効を初期化
            UpdateGameSettingsAvailability();

            Debug.Log("[MatchRoomView] UI initialized.");
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(MatchRoomViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // ViewModelのイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.GameStarting += OnGameStarting;
            viewModel.CountdownUpdated += OnCountdownUpdated;
            viewModel.LoadingSceneRequested += OnLoadingSceneRequested;
            viewModel.ErrorOccurred += OnErrorOccurred;

            // プレイヤーリストの変更を購読
            viewModel.Players.CollectionChanged += OnPlayersCollectionChanged;

            // プレイヤーリストを初期化
            PopulatePlayerList();
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します
        /// </summary>
        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.GameStarting -= OnGameStarting;
                ViewModel.CountdownUpdated -= OnCountdownUpdated;
                ViewModel.LoadingSceneRequested -= OnLoadingSceneRequested;
                ViewModel.ErrorOccurred -= OnErrorOccurred;
                ViewModel.Players.CollectionChanged -= OnPlayersCollectionChanged;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
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
    }
}
