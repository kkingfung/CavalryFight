#nullable enable

using CavalryFight.Services.Lobby;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// MatchRoomViewのUI更新処理
    /// </summary>
    public partial class MatchRoomView
    {
        #region UI Updates

        /// <summary>
        /// UIを更新します
        /// </summary>
        private void UpdateUI()
        {
            if (ViewModel == null)
            {
                return;
            }

            // プログラムからUI更新中であることをマーク（コールバック連鎖防止）
            _isUpdatingUI = true;

            try
            {
                // Join Code
                if (_joinCodeLabel != null)
                {
                    _joinCodeLabel.text = $"Join Code: {ViewModel.JoinCode}";
                }

                // Room Info
                if (_roomNameLabel != null)
                {
                    _roomNameLabel.text = ViewModel.RoomName;
                }

                if (_gameModeLabel != null)
                {
                    _gameModeLabel.text = ViewModel.GameMode;
                }

                // Arrow Limitの選択肢をゲームモードに応じて更新
                UpdateArrowLimitChoices(ViewModel.GameMode);

                if (_mapLabel != null)
                {
                    _mapLabel.text = ViewModel.MapName;
                }

                if (_playersLabel != null)
                {
                    _playersLabel.text = $"{ViewModel.CurrentPlayers} / {ViewModel.MaxPlayers}";
                }

                // Status
                if (_statusLabel != null)
                {
                    _statusLabel.text = ViewModel.StatusMessage;
                }

                // Game Settings Section (Always visible for host)
                if (_gameSettingsSection != null)
                {
                    _gameSettingsSection.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
                }

                // Game Settings - Time Limit (Toggle between label and dropdown)
                if (_timeLimitLabel != null && _timeLimitDropdown != null)
                {
                    _timeLimitLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _timeLimitDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update dropdown value when in edit mode
                    if (ViewModel.IsHost && _isEditMode)
                    {
                        string timeLimitValue = ViewModel.TimeLimit == 0 ? "No Limit" : $"{ViewModel.TimeLimit / 60}:00";
                        if (_timeLimitDropdown.value != timeLimitValue)
                        {
                            _timeLimitDropdown.value = timeLimitValue;
                        }
                    }
                }

                // Game Settings - Arrow Limit (Toggle between label and dropdown)
                if (_arrowLimitLabel != null && _arrowLimitDropdown != null)
                {
                    _arrowLimitLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _arrowLimitDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update dropdown value when in edit mode
                    if (ViewModel.IsHost && _isEditMode)
                    {
                        string arrowLimitValue = ViewModel.ArrowLimit == 0 ? "No Limit" : ViewModel.ArrowLimit.ToString();
                        if (_arrowLimitDropdown.value != arrowLimitValue)
                        {
                            _arrowLimitDropdown.value = arrowLimitValue;
                        }
                    }
                }

                // Change Settings Button Section (Host Only)
                if (_changeSettingsButtonSection != null)
                {
                    _changeSettingsButtonSection.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
                }

                // Room Info - Room Name (Edit Mode shows TextField, otherwise Label)
                if (_roomNameLabel != null && _roomNameField != null)
                {
                    _roomNameLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _roomNameField.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
                    if (ViewModel.IsHost && _isEditMode && _roomNameField.value != ViewModel.RoomName)
                    {
                        _roomNameField.value = ViewModel.RoomName;
                    }
                }

                // Room Info - Password (Edit Mode shows input container with invisible TextField and asterisks, otherwise normal Label)
                if (_passwordLabel != null && _passwordInputContainer != null && _passwordField != null && _passwordAsterisksLabel != null)
                {
                    bool isEditingPassword = ViewModel.IsHost && _isEditMode;

                    _passwordLabel.style.display = isEditingPassword ? DisplayStyle.None : DisplayStyle.Flex;
                    _passwordInputContainer.style.display = isEditingPassword ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update password label (visible when not editing)
                    if (_passwordField.value != null && !string.IsNullOrEmpty(_passwordField.value))
                    {
                        _passwordLabel.text = "******"; // Fixed asterisks when not editing
                    }
                    else
                    {
                        _passwordLabel.text = "None";
                    }

                    // Update asterisks label (visible when editing) - now updated in real-time via OnPasswordChanged callback
                    if (isEditingPassword)
                    {
                        if (!string.IsNullOrEmpty(_passwordField.value))
                        {
                            _passwordAsterisksLabel.text = new string('*', _passwordField.value.Length);
                        }
                        else
                        {
                            _passwordAsterisksLabel.text = "(empty)";
                        }
                    }
                }

                // Room Info - Public (Edit Mode shows Toggle, otherwise Label)
                if (_publicLabel != null && _publicToggle != null)
                {
                    _publicLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _publicToggle.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update label text
                    _publicLabel.text = ViewModel.IsPublic ? "Yes" : "No";

                    // Update toggle value when in edit mode
                    if (ViewModel.IsHost && _isEditMode && _publicToggle.value != ViewModel.IsPublic)
                    {
                        _publicToggle.value = ViewModel.IsPublic;
                    }
                }

                // Room Info - Max Players (Edit Mode shows Dropdown, otherwise Label)
                if (_maxPlayersLabel != null && _maxPlayersDropdown != null)
                {
                    _maxPlayersLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _maxPlayersDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update label text
                    _maxPlayersLabel.text = ViewModel.MaxPlayers.ToString();

                    // Update dropdown value when in edit mode
                    if (ViewModel.IsHost && _isEditMode && _maxPlayersDropdown.value != ViewModel.MaxPlayers.ToString())
                    {
                        _maxPlayersDropdown.value = ViewModel.MaxPlayers.ToString();
                    }
                }

                // Room Info Dropdowns (Edit Mode shows dropdowns, otherwise labels)
                if (_gameModeLabel != null && _gameModeDropdown != null)
                {
                    _gameModeLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _gameModeDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update dropdown value when in edit mode
                    if (ViewModel.IsHost && _isEditMode && _gameModeDropdown.value != ViewModel.GameMode)
                    {
                        _gameModeDropdown.value = ViewModel.GameMode;
                    }
                }

                if (_mapLabel != null && _mapDropdown != null)
                {
                    _mapLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                    _mapDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                    // Update dropdown value when in edit mode
                    if (ViewModel.IsHost && _isEditMode && _mapDropdown.value != ViewModel.MapName)
                    {
                        _mapDropdown.value = ViewModel.MapName;
                    }
                }

                // Buttons visibility
                UpdateButtonVisibility();
            }
            finally
            {
                // UI更新完了フラグをリセット
                _isUpdatingUI = false;
            }
        }

        /// <summary>
        /// ボタンの表示状態を更新します
        /// </summary>
        private void UpdateButtonVisibility()
        {
            if (ViewModel == null)
            {
                return;
            }

            // Leave Room Button - 準備中は無効
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.SetEnabled(!ViewModel.IsReady || ViewModel.IsHost);
            }

            if (ViewModel.IsHost)
            {
                // ホストの場合: Start Game ボタンのみ表示
                if (_startGameButton != null)
                {
                    _startGameButton.style.display = DisplayStyle.Flex;
                    _startGameButton.SetEnabled(ViewModel.CanStartGame);
                }

                if (_readyButton != null)
                {
                    _readyButton.style.display = DisplayStyle.None;
                }

                if (_cancelReadyButton != null)
                {
                    _cancelReadyButton.style.display = DisplayStyle.None;
                }
            }
            else
            {
                // ゲストの場合: Ready / Cancel Ready ボタンを切り替え
                if (_startGameButton != null)
                {
                    _startGameButton.style.display = DisplayStyle.None;
                }

                if (_readyButton != null)
                {
                    _readyButton.style.display = ViewModel.IsReady ? DisplayStyle.None : DisplayStyle.Flex;
                }

                if (_cancelReadyButton != null)
                {
                    _cancelReadyButton.style.display = ViewModel.IsReady ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// カウントダウンダイアログの表示状態を更新します
        /// </summary>
        private void UpdateCountdownDialog()
        {
            if (ViewModel == null || _countdownDialog == null)
            {
                return;
            }

            _countdownDialog.style.display = ViewModel.IsCountingDown ? DisplayStyle.Flex : DisplayStyle.None;

            if (ViewModel.IsCountingDown && _countdownLabel != null)
            {
                _countdownLabel.text = ViewModel.CountdownSeconds.ToString();
            }
        }

        /// <summary>
        /// ゲームモードに応じて設定の有効/無効を更新します
        /// </summary>
        private void UpdateGameSettingsAvailability()
        {
            if (ViewModel == null)
            {
                return;
            }

            // GameMode文字列をenumに変換
            if (!System.Enum.TryParse<GameMode>(ViewModel.GameMode, out var parsedMode))
            {
                return;
            }

            // タイムリミットの有効/無効
            bool isTimeLimitEditable = GameModeRules.IsTimeLimitEditable(parsedMode);
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.SetEnabled(isTimeLimitEditable && ViewModel.IsHost && _isEditMode);
            }

            // 矢の制限の有効/無効
            bool isArrowLimitEditable = GameModeRules.IsArrowLimitEditable(parsedMode);
            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.SetEnabled(isArrowLimitEditable && ViewModel.IsHost && _isEditMode);
            }

            // ドロップダウンの値を更新
            UpdateGameSettingsDropdownValues();

            Debug.Log($"[MatchRoomView] Game settings availability updated for mode {ViewModel.GameMode}: TimeLimitEditable={isTimeLimitEditable}, ArrowLimitEditable={isArrowLimitEditable}");
        }

        /// <summary>
        /// ゲーム設定ドロップダウンの表示値を更新します
        /// </summary>
        private void UpdateGameSettingsDropdownValues()
        {
            if (ViewModel == null)
            {
                return;
            }

            // プログラムからUI更新中であることをマーク（コールバック連鎖防止）
            bool wasUpdatingUI = _isUpdatingUI;
            _isUpdatingUI = true;

            try
            {
                // タイムリミットの表示を更新
                if (_timeLimitDropdown != null)
                {
                    string timeLimitValue = ViewModel.TimeLimit switch
                    {
                        180 => "3:00",
                        300 => "5:00",
                        600 => "10:00",
                        900 => "15:00",
                        0 => "No Limit",
                        _ => "No Limit"
                    };
                    _timeLimitDropdown.value = timeLimitValue;
                }

                // タイムリミットラベルの表示を更新
                if (_timeLimitLabel != null)
                {
                    _timeLimitLabel.text = ViewModel.TimeLimit == 0 ? "No Limit" : $"{ViewModel.TimeLimit / 60}:00";
                }

                // 矢の制限の表示を更新
                if (_arrowLimitDropdown != null)
                {
                    string arrowLimitValue = ViewModel.ArrowLimit switch
                    {
                        5 => "5",
                        10 => "10",
                        15 => "15",
                        20 => "20",
                        0 => "No Limit",
                        _ => "No Limit"
                    };
                    _arrowLimitDropdown.value = arrowLimitValue;
                }

                // 矢の制限ラベルの表示を更新
                if (_arrowLimitLabel != null)
                {
                    _arrowLimitLabel.text = ViewModel.ArrowLimit == 0 ? "No Limit" : ViewModel.ArrowLimit.ToString();
                }
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
