#nullable enable

using System.Collections.Generic;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// MatchRoomViewのUI要素定義と取得
    /// </summary>
    public partial class MatchRoomView
    {
        #region UI Elements

        // Header
        private Label? _joinCodeLabel;

        // Left Panel - Player List
        private VisualElement? _playerListContainer;
        private ScrollView? _playerListScrollView;

        // Right Panel - Room Settings
        private Label? _roomNameLabel;
        private TextField? _roomNameField;
        private Label? _passwordLabel;
        private VisualElement? _passwordInputContainer;
        private TextField? _passwordField;
        private Label? _passwordAsterisksLabel;
        private Label? _publicLabel;
        private Toggle? _publicToggle;
        private Label? _maxPlayersLabel;
        private DropdownField? _maxPlayersDropdown;
        private Label? _gameModeLabel;
        private Label? _mapLabel;
        private Label? _playersLabel;
        private Label? _statusLabel;

        // Room Info Dropdowns (Host Only)
        private DropdownField? _gameModeDropdown;
        private DropdownField? _mapDropdown;

        // Game Settings (Host Only)
        private VisualElement? _gameSettingsSection;
        private Label? _timeLimitLabel;
        private DropdownField? _timeLimitDropdown;
        private Label? _arrowLimitLabel;
        private DropdownField? _arrowLimitDropdown;

        // Change/Apply Settings Button (Host Only)
        private VisualElement? _changeSettingsButtonSection;
        private Button? _changeSettingsButton;

        // Footer Buttons
        private Button? _leaveRoomButton;
        private Button? _startGameButton;
        private Button? _readyButton;
        private Button? _cancelReadyButton;

        // Countdown Dialog
        private VisualElement? _countdownDialog;
        private Label? _countdownLabel;
        private Button? _cancelCountdownButton;

        #endregion

        #region UI Element Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            if (RootVisualElement == null)
            {
                return;
            }

            // Header
            _joinCodeLabel = Q<Label>("JoinCodeLabel");

            // Left Panel - Player List
            _playerListContainer = Q<VisualElement>("PlayerListContainer");
            _playerListScrollView = Q<ScrollView>("PlayerListScrollView");

            // Right Panel - Room Settings
            _roomNameLabel = Q<Label>("RoomNameLabel");
            _roomNameField = Q<TextField>("RoomNameField");
            _passwordLabel = Q<Label>("PasswordLabel");
            _passwordInputContainer = Q<VisualElement>("PasswordInputContainer");
            _passwordField = Q<TextField>("PasswordField");
            _passwordAsterisksLabel = Q<Label>("PasswordAsterisksLabel");
            _publicLabel = Q<Label>("PublicLabel");
            _publicToggle = Q<Toggle>("PublicToggle");
            _maxPlayersLabel = Q<Label>("MaxPlayersLabel");
            _maxPlayersDropdown = Q<DropdownField>("MaxPlayersDropdown");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _mapLabel = Q<Label>("MapLabel");
            _playersLabel = Q<Label>("PlayersLabel");
            _statusLabel = Q<Label>("StatusLabel");

            // Room Info Dropdowns (Host Only)
            _gameModeDropdown = Q<DropdownField>("GameModeDropdown");
            _mapDropdown = Q<DropdownField>("MapDropdown");

            // Game Settings (Host Only)
            _gameSettingsSection = Q<VisualElement>("GameSettingsSection");
            _timeLimitLabel = Q<Label>("TimeLimitLabel");
            _timeLimitDropdown = Q<DropdownField>("TimeLimitDropdown");
            _arrowLimitLabel = Q<Label>("ArrowLimitLabel");
            _arrowLimitDropdown = Q<DropdownField>("ArrowLimitDropdown");

            // Change/Apply Settings Button (Host Only)
            _changeSettingsButtonSection = Q<VisualElement>("ChangeSettingsButtonSection");
            _changeSettingsButton = Q<Button>("ChangeSettingsButton");

            // Footer Buttons
            _leaveRoomButton = Q<Button>("LeaveRoomButton");
            _startGameButton = Q<Button>("StartGameButton");
            _readyButton = Q<Button>("ReadyButton");
            _cancelReadyButton = Q<Button>("CancelReadyButton");

            // Countdown Dialog
            _countdownDialog = Q<VisualElement>("CountdownDialog");
            _countdownLabel = Q<Label>("CountdownLabel");
            _cancelCountdownButton = Q<Button>("CancelCountdownButton");
        }

        /// <summary>
        /// UI要素が正しく取得できているか検証します
        /// </summary>
        private void ValidateUIElements()
        {
            if (_playerListContainer == null)
            {
                Debug.LogWarning("[MatchRoomView] PlayerListContainer not found!", this);
            }

            if (_joinCodeLabel == null)
            {
                Debug.LogWarning("[MatchRoomView] JoinCodeLabel not found!", this);
            }

            if (_statusLabel == null)
            {
                Debug.LogWarning("[MatchRoomView] StatusLabel not found!", this);
            }
        }

        /// <summary>
        /// ドロップダウンの選択肢を設定します
        /// </summary>
        private void SetupDropdowns()
        {
            // Game Mode dropdown (Room Info - Host only)
            if (_gameModeDropdown != null)
            {
                _gameModeDropdown.choices = new List<string>
                {
                    "Arena", "ScoreMatch", "TeamFight", "Deathmatch", "Hunting"
                };
                _gameModeDropdown.value = "Arena";
            }

            // Map dropdown (Room Info - Host only)
            // フィールドプレハブと一致（TrainingRoomを除く）
            if (_mapDropdown != null)
            {
                _mapDropdown.choices = new List<string>
                {
                    "Arena", "Forest", "Nature", "PlayGround"
                };
                _mapDropdown.value = "Arena";
            }

            // Max Players dropdown (Room Info - Host only)
            // 最大プレイヤー数は2〜8の範囲（RoomSettingsと一致）
            if (_maxPlayersDropdown != null)
            {
                _maxPlayersDropdown.choices = new List<string>
                {
                    "2", "3", "4", "5", "6", "7", "8"
                };
                _maxPlayersDropdown.value = "8";
            }

            // Time Limit dropdown
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.choices = new List<string>
                {
                    "3:00", "5:00", "10:00", "15:00", "No Limit"
                };
                _timeLimitDropdown.value = "5:00";
            }

            // Arrow Limit dropdown
            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.choices = new List<string>
                {
                    "5", "10", "15", "20", "No Limit"
                };
                _arrowLimitDropdown.value = "No Limit";
            }

            // NPC Difficulty dropdowns are now created dynamically in player cells
        }

        /// <summary>
        /// ゲームモードに応じてArrow Limitの選択肢を更新します
        /// </summary>
        /// <param name="gameMode">ゲームモード</param>
        private void UpdateArrowLimitChoices(string gameMode)
        {
            if (_arrowLimitDropdown == null)
            {
                return;
            }

            // プログラムからUI更新中であることをマーク（コールバック連鎖防止）
            bool wasUpdatingUI = _isUpdatingUI;
            _isUpdatingUI = true;

            try
            {
                // 現在の値を保存
                string currentValue = _arrowLimitDropdown.value;

                // ゲームモードに応じて選択肢を設定
                if (gameMode == "ScoreMatch")
                {
                    // ScoreMatchの場合は"No Limit"を除外
                    _arrowLimitDropdown.choices = new List<string>
                    {
                        "5", "10", "15", "20"
                    };

                    // 現在の値が"No Limit"の場合、デフォルト値に変更
                    if (currentValue == "No Limit")
                    {
                        _arrowLimitDropdown.value = "10";
                        if (ViewModel != null)
                        {
                            ViewModel.ArrowLimit = 10;
                        }
                    }
                    else if (_arrowLimitDropdown.choices.Contains(currentValue))
                    {
                        _arrowLimitDropdown.value = currentValue;
                    }
                    else
                    {
                        _arrowLimitDropdown.value = "10";
                        if (ViewModel != null)
                        {
                            ViewModel.ArrowLimit = 10;
                        }
                    }
                }
                else
                {
                    // その他のモードでは全選択肢を表示
                    _arrowLimitDropdown.choices = new List<string>
                    {
                        "5", "10", "15", "20", "No Limit"
                    };

                    // 現在の値が選択肢に含まれていれば復元
                    if (_arrowLimitDropdown.choices.Contains(currentValue))
                    {
                        _arrowLimitDropdown.value = currentValue;
                    }
                    else
                    {
                        _arrowLimitDropdown.value = "No Limit";
                        if (ViewModel != null)
                        {
                            ViewModel.ArrowLimit = 0; // 0 = No Limit
                        }
                    }
                }

                Debug.Log($"[MatchRoomView] Arrow limit choices updated for game mode: {gameMode}, current value: {_arrowLimitDropdown.value}");
            }
            finally
            {
                // 元の状態に戻す
                _isUpdatingUI = wasUpdatingUI;
            }
        }

        #endregion
    }
}
