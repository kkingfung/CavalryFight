#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Replay;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// 履歴/リプレイ一覧画面のViewModel
    /// </summary>
    /// <remarks>
    /// リプレイの一覧表示、選択、詳細表示、削除機能を提供します。
    /// 左側にリスト、右側に詳細情報を表示するレイアウトです。
    /// </remarks>
    public class HistoryViewModel : ViewModelBase
    {
        #region Fields

        private readonly IReplayService _replayService;
        private readonly ISceneManagementService? _sceneManagementService;
        private List<ReplayMetadata> _replayList = new List<ReplayMetadata>();
        private ReplayMetadata? _selectedReplay;
        private int _selectedIndex = -1;

        #endregion

        #region Properties

        /// <summary>
        /// リプレイメタデータのリスト
        /// </summary>
        public List<ReplayMetadata> ReplayList
        {
            get => _replayList;
            private set => SetProperty(ref _replayList, value);
        }

        /// <summary>
        /// 現在選択されているリプレイメタデータ
        /// </summary>
        public ReplayMetadata? SelectedReplay
        {
            get => _selectedReplay;
            private set
            {
                if (SetProperty(ref _selectedReplay, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(NoSelection));
                }
            }
        }

        /// <summary>
        /// 選択されているリプレイのインデックス
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetProperty(ref _selectedIndex, value);
        }

        /// <summary>
        /// リプレイが選択されているかどうか
        /// </summary>
        public bool HasSelection => SelectedReplay != null;

        /// <summary>
        /// リプレイが選択されていないかどうか
        /// </summary>
        public bool NoSelection => SelectedReplay == null;

        /// <summary>
        /// リプレイリストが空かどうか
        /// </summary>
        public bool IsEmpty => ReplayList.Count == 0;

        /// <summary>
        /// リプレイリストに項目があるかどうか
        /// </summary>
        public bool HasReplays => ReplayList.Count > 0;

        #endregion

        #region Commands

        /// <summary>
        /// リプレイを選択するコマンド
        /// </summary>
        public ICommand SelectReplayCommand { get; }

        /// <summary>
        /// 選択したリプレイを視聴するコマンド
        /// </summary>
        public ICommand WatchReplayCommand { get; }

        /// <summary>
        /// 選択したリプレイを削除するコマンド
        /// </summary>
        public ICommand DeleteReplayCommand { get; }

        /// <summary>
        /// リプレイリストをリフレッシュするコマンド
        /// </summary>
        public ICommand RefreshListCommand { get; }

        /// <summary>
        /// メインメニューに戻るコマンド
        /// </summary>
        public ICommand BackToMenuCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// リプレイが選択された時のイベント
        /// </summary>
        public event EventHandler<ReplayMetadata>? ReplaySelected;

        /// <summary>
        /// リプレイリストが更新された時のイベント
        /// </summary>
        public event EventHandler? ListUpdated;

        /// <summary>
        /// メインメニューに戻る要求イベント
        /// </summary>
        public event EventHandler? BackToMenuRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// HistoryViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="replayService">リプレイサービス</param>
        public HistoryViewModel(IReplayService replayService)
        {
            _replayService = replayService ?? throw new ArgumentNullException(nameof(replayService));
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // サービスのイベントを購読
            _replayService.ReplayListUpdated += OnReplayListUpdated;

            // コマンドを初期化
            SelectReplayCommand = new RelayCommand<string>(ExecuteSelectReplay, CanSelectReplay);
            WatchReplayCommand = new RelayCommand(ExecuteWatchReplay, CanWatchReplay);
            DeleteReplayCommand = new RelayCommand(ExecuteDeleteReplay, CanDeleteReplay);
            RefreshListCommand = new RelayCommand(ExecuteRefreshList);
            BackToMenuCommand = new RelayCommand(ExecuteBackToMenu);

            // リプレイリストを読み込み
            LoadReplayList();

            Debug.Log("[HistoryViewModel] ViewModel initialized.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// リプレイリストを読み込みます
        /// </summary>
        public void LoadReplayList()
        {
            ReplayList = _replayService.GetReplaysSortedByDate(descending: true);
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasReplays));

            Debug.Log($"[HistoryViewModel] Loaded {ReplayList.Count} replays.");
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// リプレイを選択できるかどうかを判定します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>選択可能な場合true</returns>
        private bool CanSelectReplay(string? replayId)
        {
            return !string.IsNullOrEmpty(replayId);
        }

        /// <summary>
        /// リプレイを選択します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        private void ExecuteSelectReplay(string? replayId)
        {
            if (string.IsNullOrEmpty(replayId))
            {
                return;
            }

            var replay = ReplayList.Find(r => r.ReplayId == replayId);
            if (replay != null)
            {
                SelectedReplay = replay;
                SelectedIndex = ReplayList.IndexOf(replay);
                ReplaySelected?.Invoke(this, replay);

                // サービスにも選択を通知
                _replayService.SelectReplay(replayId);

                Debug.Log($"[HistoryViewModel] Replay selected: {replay.MapName} ({replay.DateText})");
            }
        }

        /// <summary>
        /// リプレイを視聴できるかどうかを判定します
        /// </summary>
        /// <returns>視聴可能な場合true</returns>
        private bool CanWatchReplay()
        {
            return SelectedReplay != null && _sceneManagementService != null;
        }

        /// <summary>
        /// 選択したリプレイを視聴します
        /// </summary>
        private void ExecuteWatchReplay()
        {
            if (SelectedReplay == null || _sceneManagementService == null)
            {
                return;
            }

            Debug.Log($"[HistoryViewModel] Starting replay: {SelectedReplay.ReplayId}");

            // リプレイシーンに遷移
            _sceneManagementService.LoadReplay();
        }

        /// <summary>
        /// リプレイを削除できるかどうかを判定します
        /// </summary>
        /// <returns>削除可能な場合true</returns>
        private bool CanDeleteReplay()
        {
            return SelectedReplay != null;
        }

        /// <summary>
        /// 選択したリプレイを削除します
        /// </summary>
        private void ExecuteDeleteReplay()
        {
            if (SelectedReplay == null)
            {
                return;
            }

            string replayId = SelectedReplay.ReplayId;
            string mapName = SelectedReplay.MapName;

            // リプレイを削除
            bool success = _replayService.DeleteReplay(replayId);

            if (success)
            {
                Debug.Log($"[HistoryViewModel] Replay deleted: {mapName}");

                // 選択をクリア
                SelectedReplay = null;
                SelectedIndex = -1;

                // リストを再読み込み
                LoadReplayList();
            }
            else
            {
                Debug.LogWarning($"[HistoryViewModel] Failed to delete replay: {replayId}");
            }
        }

        /// <summary>
        /// リプレイリストをリフレッシュします
        /// </summary>
        private void ExecuteRefreshList()
        {
            Debug.Log("[HistoryViewModel] Refreshing replay list...");
            _replayService.RefreshReplayList();
            LoadReplayList();
        }

        /// <summary>
        /// メインメニューに戻ります
        /// </summary>
        private void ExecuteBackToMenu()
        {
            Debug.Log("[HistoryViewModel] Returning to main menu...");
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// リプレイリストが更新された時の処理
        /// </summary>
        private void OnReplayListUpdated()
        {
            LoadReplayList();
            ListUpdated?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            // イベント購読解除
            _replayService.ReplayListUpdated -= OnReplayListUpdated;

            base.OnDispose();
            Debug.Log("[HistoryViewModel] ViewModel disposed.");
        }

        #endregion
    }
}
