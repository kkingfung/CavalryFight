#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using CavalryFight.Core.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using InputAction = UnityEngine.InputSystem.InputAction;

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
            _inputActions = new GameInputActions();
            _bindings = new List<KeyBindingEntry>();

            // コマンドを初期化
            StartRebindCommand = new RelayCommand<KeyBindingEntry>(ExecuteStartRebind, CanExecuteStartRebind);
            CancelRebindCommand = new RelayCommand(ExecuteCancelRebind, CanExecuteCancelRebind);
            ResetToDefaultCommand = new RelayCommand(ExecuteResetToDefault);
            CloseCommand = new RelayCommand(ExecuteClose);

            // バインディングを初期化
            InitializeBindings();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// バインディングエントリを初期化します
        /// </summary>
        private void InitializeBindings()
        {
            // Gameplayアクションマップのバインディングを追加

            // Move（コンポジット - 個別キーを表示）
            AddCompositeActionBindings(_inputActions.Gameplay.Move, "Move");

            // Camera（マウスデルタ - リバインド不要なので省略可）
            // AddActionBindings(_inputActions.Gameplay.Camera, "Camera");

            // 通常ボタンアクション
            AddActionBindings(_inputActions.Gameplay.Attack, "Attack / Aim");
            AddActionBindings(_inputActions.Gameplay.CancelAttack, "Cancel Attack (while aiming)");
            AddActionBindings(_inputActions.Gameplay.Boost, "Boost (when mounted, not aiming)");
            AddActionBindings(_inputActions.Gameplay.Mount, "Mount / Unmount");
            AddActionBindings(_inputActions.Gameplay.Jump, "Jump");
            AddActionBindings(_inputActions.Gameplay.Sprint, "Sprint (Hold)");

            // UIアクションマップのバインディングを追加
            AddActionBindings(_inputActions.UI.Menu, "Menu (Pause)");
        }

        /// <summary>
        /// コンポジットアクション（WASDなど）のバインディングをリストに追加します
        /// </summary>
        /// <remarks>
        /// コンポジットバインディングの個々のパーツ（Up, Down, Left, Right）を
        /// 個別のエントリとして表示します。
        /// </remarks>
        private void AddCompositeActionBindings(InputAction action, string displayName)
        {
            // Keyboard&Mouseスキームのコンポジットバインディングを検索
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                // コンポジットの開始を探す（例: "WASD"）
                if (!binding.isComposite)
                {
                    continue;
                }

                // Keyboard&Mouseグループのみ（Gamepadコンポジットはスキップ）
                // コンポジット自体にはgroupsが設定されていないため、最初のパートを確認
                int partIndex = i + 1;
                if (partIndex < action.bindings.Count)
                {
                    var firstPart = action.bindings[partIndex];
                    if (!string.IsNullOrEmpty(firstPart.groups) && !firstPart.groups.Contains("Keyboard&Mouse"))
                    {
                        continue;
                    }
                }

                // コンポジットの個々のパーツを追加
                for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                {
                    var partBinding = action.bindings[j];

                    // Keyboard&Mouseグループのみ
                    if (!string.IsNullOrEmpty(partBinding.groups) && !partBinding.groups.Contains("Keyboard&Mouse"))
                    {
                        continue;
                    }

                    // パーツ名を取得（up, down, left, right）
                    string partName = partBinding.name;
                    string partDisplayName = GetDirectionDisplayName(partName);

                    var entry = new KeyBindingEntry
                    {
                        Action = action,
                        BindingIndex = j,
                        ActionName = $"{displayName} {partDisplayName}",
                        CurrentBinding = GetBindingDisplayString(action, j)
                    };

                    _bindings.Add(entry);
                }
            }
        }

        /// <summary>
        /// 方向名を表示用の文字列に変換します
        /// </summary>
        private string GetDirectionDisplayName(string directionName)
        {
            return directionName.ToLower() switch
            {
                "up" => "(Forward)",
                "down" => "(Backward)",
                "left" => "(Left)",
                "right" => "(Right)",
                _ => $"({directionName})"
            };
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

                // コンポジットバインディング（WASDなど）はスキップ
                if (binding.isComposite)
                {
                    continue;
                }

                // コンポジットの一部もスキップ（個別のW,A,S,Dなど）
                if (binding.isPartOfComposite)
                {
                    continue;
                }

                // Keyboard&Mouseグループのみ、または空のグループ（デフォルト）
                if (!string.IsNullOrEmpty(binding.groups) && binding.groups != "Keyboard&Mouse")
                {
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
                return;
            }

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
            _rebindingOperation?.Cancel();
        }

        /// <summary>
        /// デフォルトにリセットします
        /// </summary>
        private void ExecuteResetToDefault()
        {
            // リバインディング中の場合はキャンセル
            if (IsRebinding)
            {
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
            // リバインディング中の場合はキャンセル
            if (IsRebinding)
            {
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
            CurrentRebindingEntry = null;
            IsRebinding = false;
            RebindingPrompt = string.Empty;

            // バインディング表示を更新
            entry.CurrentBinding = GetBindingDisplayString(entry.Action, entry.BindingIndex);
            BindingUpdated?.Invoke(this, entry);

            // クリーンアップ
            _rebindingOperation?.Dispose();
            _rebindingOperation = null;

            // アクションを再有効化
            entry.Action.Enable();
        }

        /// <summary>
        /// リバインディングキャンセル時に呼ばれます
        /// </summary>
        private void OnRebindCanceled()
        {
            // アクションを再有効化（キャンセル前に保存）
            var entry = CurrentRebindingEntry;

            CurrentRebindingEntry = null;
            IsRebinding = false;
            RebindingPrompt = string.Empty;

            // クリーンアップ
            _rebindingOperation?.Dispose();
            _rebindingOperation = null;

            // アクションを再有効化
            if (entry != null)
            {
                entry.Action.Enable();
            }
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

            // InputServiceに変更を即座に反映
            var inputService = ServiceLocator.Instance.Get<CavalryFight.Services.Input.IInputService>();
            if (inputService != null)
            {
                inputService.ReloadBindingOverrides();
            }
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
