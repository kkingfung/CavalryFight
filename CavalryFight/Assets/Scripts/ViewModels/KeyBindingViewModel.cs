#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// キーバインディング設定画面のViewModel
    /// </summary>
    /// <remarks>
    /// Unity Input Systemを使用してキーバインディングを管理します。
    /// ユーザーがキーを再バインドできる機能を提供します。
    /// </remarks>
    public class KeyBindingViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        /// リバインディング時に複数の入力が検出された場合、次の入力を待つ時間（秒）
        /// 例: キーを押した直後に別のキーが押された場合、0.1秒待ってから確定する
        /// </summary>
        private const float RebindingMatchWaitTime = 0.1f;

        #endregion

        #region Fields

        private readonly GameInputActions _inputActions;
        private readonly List<KeyBindingEntry> _bindings;
        private InputActionRebindingExtensions.RebindingOperation? _rebindingOperation;
        private bool _isRebinding;
        private string _rebindingPrompt = string.Empty;
        private KeyBindingEntry? _currentRebindingEntry;

        #endregion

        #region Properties

        /// <summary>
        /// キーバインディングエントリのリストを取得します
        /// </summary>
        public IReadOnlyList<KeyBindingEntry> Bindings => _bindings;

        /// <summary>
        /// 現在リバインディング中かどうかを取得します
        /// </summary>
        public bool IsRebinding
        {
            get => _isRebinding;
            private set => SetProperty(ref _isRebinding, value);
        }

        /// <summary>
        /// リバインディングプロンプトメッセージを取得します
        /// </summary>
        public string RebindingPrompt
        {
            get => _rebindingPrompt;
            private set => SetProperty(ref _rebindingPrompt, value);
        }

        /// <summary>
        /// 現在リバインディング中のエントリを取得します
        /// </summary>
        public KeyBindingEntry? CurrentRebindingEntry
        {
            get => _currentRebindingEntry;
            private set => SetProperty(ref _currentRebindingEntry, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// リバインディングを開始するコマンド
        /// </summary>
        public ICommand StartRebindCommand { get; }

        /// <summary>
        /// リバインディングをキャンセルするコマンド
        /// </summary>
        public ICommand CancelRebindCommand { get; }

        /// <summary>
        /// デフォルトにリセットするコマンド
        /// </summary>
        public ICommand ResetToDefaultCommand { get; }

        /// <summary>
        /// ポップアップを閉じるコマンド
        /// </summary>
        public ICommand CloseCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// ポップアップを閉じる要求が発生した時のイベント
        /// </summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// バインディングが更新された時のイベント
        /// </summary>
        public event EventHandler<KeyBindingEntry>? BindingUpdated;

        #endregion

        #region Constructor

        /// <summary>
        /// KeyBindingViewModelの新しいインスタンスを初期化します
        /// </summary>
        public KeyBindingViewModel()
        {
            Debug.LogWarning("=== [KeyBindingViewModel] CONSTRUCTOR STARTED ===");

            _inputActions = new GameInputActions();
            _bindings = new List<KeyBindingEntry>();

            // コマンドを初期化
            StartRebindCommand = new RelayCommand<KeyBindingEntry>(ExecuteStartRebind, CanExecuteStartRebind);
            CancelRebindCommand = new RelayCommand(ExecuteCancelRebind, CanExecuteCancelRebind);
            ResetToDefaultCommand = new RelayCommand(ExecuteResetToDefault);
            CloseCommand = new RelayCommand(ExecuteClose);

            // バインディングを初期化
            InitializeBindings();

            Debug.LogWarning($"=== [KeyBindingViewModel] ViewModel initialized with {_bindings.Count} bindings ===");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// バインディングエントリを初期化します
        /// </summary>
        private void InitializeBindings()
        {
            // Gameplayアクションマップのバインディングを追加
            AddActionBindings(_inputActions.Gameplay.Move, "Move");
            AddActionBindings(_inputActions.Gameplay.Camera, "Camera");
            AddActionBindings(_inputActions.Gameplay.Attack, "Attack / Aim");
            AddActionBindings(_inputActions.Gameplay.CancelAttack, "Cancel Attack (while aiming)");
            AddActionBindings(_inputActions.Gameplay.Boost, "Boost (when mounted, not aiming)");
            AddActionBindings(_inputActions.Gameplay.Mount, "Mount / Unmount");
            AddActionBindings(_inputActions.Gameplay.Jump, "Jump");

            // UIアクションマップのバインディングを追加
            AddActionBindings(_inputActions.UI.Menu, "Menu (Pause)");

            Debug.LogWarning($"=== [KeyBindingViewModel] InitializeBindings completed with {_bindings.Count} total bindings ===");
        }

        /// <summary>
        /// アクションのバインディングをリストに追加します
        /// </summary>
        private void AddActionBindings(InputAction action, string displayName)
        {
            // Keyboard&Mouseスキームのバインディングのみを表示
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                Debug.Log($"[KeyBindingViewModel] Checking binding {i} for {displayName}: path={binding.path}, groups={binding.groups}, isComposite={binding.isComposite}, isPartOfComposite={binding.isPartOfComposite}");

                // コンポジットバインディング（WASDなど）はスキップ
                if (binding.isComposite)
                {
                    Debug.Log($"[KeyBindingViewModel] Skipping composite binding for {displayName}");
                    continue;
                }

                // コンポジットの一部もスキップ（個別のW,A,S,Dなど）
                if (binding.isPartOfComposite)
                {
                    Debug.Log($"[KeyBindingViewModel] Skipping part of composite for {displayName}");
                    continue;
                }

                // Keyboard&Mouseグループのみ、または空のグループ（デフォルト）
                if (!string.IsNullOrEmpty(binding.groups) && binding.groups != "Keyboard&Mouse")
                {
                    Debug.Log($"[KeyBindingViewModel] Skipping non-Keyboard&Mouse binding: {binding.groups}");
                    continue;
                }

                var entry = new KeyBindingEntry
                {
                    Action = action,
                    BindingIndex = i,
                    ActionName = displayName,
                    CurrentBinding = GetBindingDisplayString(action, i)
                };

                _bindings.Add(entry);
                Debug.Log($"[KeyBindingViewModel] Added binding for {displayName}: {entry.CurrentBinding}");
            }
        }

        /// <summary>
        /// バインディングの表示文字列を取得します
        /// </summary>
        private string GetBindingDisplayString(InputAction action, int bindingIndex)
        {
            return action.GetBindingDisplayString(bindingIndex);
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// リバインディングを開始できるかどうかを判定します
        /// </summary>
        private bool CanExecuteStartRebind(KeyBindingEntry? entry)
        {
            return entry != null && !IsRebinding;
        }

        /// <summary>
        /// リバインディングを開始します
        /// </summary>
        private void ExecuteStartRebind(KeyBindingEntry? entry)
        {
            if (entry == null)
            {
                Debug.LogError("[KeyBindingViewModel] ExecuteStartRebind called with null entry!");
                return;
            }

            Debug.LogWarning($"=== [KeyBindingViewModel] STARTING REBIND for {entry.ActionName} at index {entry.BindingIndex} ===");

            // アクションを無効化（リバインディング中は入力を受け付けない）
            entry.Action.Disable();

            CurrentRebindingEntry = entry;
            IsRebinding = true;
            RebindingPrompt = $"Press a key to bind to '{entry.ActionName}'...";

            _rebindingOperation = entry.Action
                .PerformInteractiveRebinding(entry.BindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(RebindingMatchWaitTime)
                .OnComplete(operation => OnRebindComplete(entry))
                .OnCancel(operation => OnRebindCanceled())
                .Start();

            Debug.LogWarning($"=== [KeyBindingViewModel] Rebinding operation STARTED. Waiting for input... ===");
        }

        /// <summary>
        /// リバインディングをキャンセルできるかどうかを判定します
        /// </summary>
        private bool CanExecuteCancelRebind()
        {
            return IsRebinding;
        }

        /// <summary>
        /// リバインディングをキャンセルします
        /// </summary>
        private void ExecuteCancelRebind()
        {
            Debug.Log("[KeyBindingViewModel] Canceling rebind.");
            _rebindingOperation?.Cancel();
        }

        /// <summary>
        /// デフォルトにリセットします
        /// </summary>
        private void ExecuteResetToDefault()
        {
            Debug.Log("[KeyBindingViewModel] Resetting to default bindings.");

            // リバインディング中の場合はキャンセル
            if (IsRebinding)
            {
                Debug.Log("[KeyBindingViewModel] Canceling active rebinding before reset.");
                _rebindingOperation?.Cancel();
            }

            // すべてのバインディングをデフォルトに戻す
            _inputActions.asset.RemoveAllBindingOverrides();

            // UIを更新
            foreach (var entry in _bindings)
            {
                entry.CurrentBinding = GetBindingDisplayString(entry.Action, entry.BindingIndex);
                BindingUpdated?.Invoke(this, entry);
            }

            OnPropertyChanged(nameof(Bindings));
        }

        /// <summary>
        /// ポップアップを閉じます
        /// </summary>
        private void ExecuteClose()
        {
            Debug.Log("[KeyBindingViewModel] Closing popup.");

            // リバインディング中の場合はキャンセル
            if (IsRebinding)
            {
                Debug.Log("[KeyBindingViewModel] Canceling active rebinding before close.");
                _rebindingOperation?.Cancel();
            }

            // 変更を保存
            SaveBindings();

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Rebinding Callbacks

        /// <summary>
        /// リバインディング完了時に呼ばれます
        /// </summary>
        private void OnRebindComplete(KeyBindingEntry entry)
        {
            Debug.LogWarning($"=== [KeyBindingViewModel] REBIND COMPLETED for {entry.ActionName} ===");

            CurrentRebindingEntry = null;
            IsRebinding = false;
            RebindingPrompt = string.Empty;

            // バインディング表示を更新
            entry.CurrentBinding = GetBindingDisplayString(entry.Action, entry.BindingIndex);
            Debug.LogWarning($"=== [KeyBindingViewModel] New binding: {entry.CurrentBinding} ===");
            BindingUpdated?.Invoke(this, entry);

            // クリーンアップ
            _rebindingOperation?.Dispose();
            _rebindingOperation = null;
        }

        /// <summary>
        /// リバインディングキャンセル時に呼ばれます
        /// </summary>
        private void OnRebindCanceled()
        {
            Debug.LogWarning("=== [KeyBindingViewModel] REBIND CANCELED ===");

            CurrentRebindingEntry = null;
            IsRebinding = false;
            RebindingPrompt = string.Empty;

            // クリーンアップ
            _rebindingOperation?.Dispose();
            _rebindingOperation = null;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// バインディングを保存します
        /// </summary>
        private void SaveBindings()
        {
            string rebinds = _inputActions.asset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("InputBindings", rebinds);
            PlayerPrefs.Save();
            Debug.Log("[KeyBindingViewModel] Bindings saved.");
        }

        /// <summary>
        /// バインディングを読み込みます
        /// </summary>
        public void LoadBindings()
        {
            string rebinds = PlayerPrefs.GetString("InputBindings", string.Empty);

            if (!string.IsNullOrEmpty(rebinds))
            {
                _inputActions.asset.LoadBindingOverridesFromJson(rebinds);
                Debug.Log("[KeyBindingViewModel] Bindings loaded.");

                // UIを更新
                foreach (var entry in _bindings)
                {
                    entry.CurrentBinding = GetBindingDisplayString(entry.Action, entry.BindingIndex);
                    BindingUpdated?.Invoke(this, entry);
                }

                OnPropertyChanged(nameof(Bindings));
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            _rebindingOperation?.Dispose();
            _inputActions?.Dispose();

            base.OnDispose();
            Debug.Log("[KeyBindingViewModel] ViewModel disposed.");
        }

        #endregion
    }

    /// <summary>
    /// キーバインディングエントリ
    /// </summary>
    public class KeyBindingEntry : ViewModelBase
    {
        private string _currentBinding = string.Empty;

        /// <summary>
        /// アクション
        /// </summary>
        public InputAction Action { get; set; } = null!;

        /// <summary>
        /// バインディングインデックス
        /// </summary>
        public int BindingIndex { get; set; }

        /// <summary>
        /// アクション名
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// 現在のバインディング表示文字列
        /// </summary>
        public string CurrentBinding
        {
            get => _currentBinding;
            set => SetProperty(ref _currentBinding, value);
        }
    }
}
