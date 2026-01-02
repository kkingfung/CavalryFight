#nullable enable

using System;
using System.Collections.Specialized;
using System.Linq;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Lobby;
using CavalryFight.ViewModels;
using CavalryFight.ViewModels.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// MatchRoomViewのイベントハンドラ
    /// </summary>
    public partial class MatchRoomView
    {
        #region Event Handlers Registration

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Player Name Update Button
            if (_updatePlayerNameButton != null)
            {
                _updatePlayerNameButton.clicked += OnUpdatePlayerNameButtonClicked;
            }

            // Footer buttons
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.clicked += OnLeaveRoomButtonClicked;
            }

            if (_startGameButton != null)
            {
                _startGameButton.clicked += OnStartGameButtonClicked;
            }

            if (_readyButton != null)
            {
                _readyButton.clicked += OnReadyButtonClicked;
            }

            if (_cancelReadyButton != null)
            {
                _cancelReadyButton.clicked += OnCancelReadyButtonClicked;
            }

            // Countdown dialog
            if (_cancelCountdownButton != null)
            {
                _cancelCountdownButton.clicked += OnCancelCountdownButtonClicked;
            }

            // Change Settings button (Host only)
            if (_changeSettingsButton != null)
            {
                _changeSettingsButton.clicked += OnChangeSettingsButtonClicked;
            }

            // Room Info dropdown change events (Host only)
            if (_gameModeDropdown != null)
            {
                _gameModeDropdown.RegisterValueChangedCallback(OnGameModeChanged);
            }

            if (_mapDropdown != null)
            {
                _mapDropdown.RegisterValueChangedCallback(OnMapChanged);
            }

            // Password field change event (for asterisks display)
            if (_passwordField != null)
            {
                _passwordField.RegisterValueChangedCallback(OnPasswordChanged);
            }

            // Dropdown change events
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.RegisterValueChangedCallback(OnTimeLimitChanged);
            }

            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.RegisterValueChangedCallback(OnArrowLimitChanged);
            }
        }

        /// <summary>
        /// イベントハンドラを解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // Player Name Update Button
            if (_updatePlayerNameButton != null)
            {
                _updatePlayerNameButton.clicked -= OnUpdatePlayerNameButtonClicked;
            }

            // Footer buttons
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.clicked -= OnLeaveRoomButtonClicked;
            }

            if (_startGameButton != null)
            {
                _startGameButton.clicked -= OnStartGameButtonClicked;
            }

            if (_readyButton != null)
            {
                _readyButton.clicked -= OnReadyButtonClicked;
            }

            if (_cancelReadyButton != null)
            {
                _cancelReadyButton.clicked -= OnCancelReadyButtonClicked;
            }

            // Countdown dialog
            if (_cancelCountdownButton != null)
            {
                _cancelCountdownButton.clicked -= OnCancelCountdownButtonClicked;
            }

            // Change Settings button
            if (_changeSettingsButton != null)
            {
                _changeSettingsButton.clicked -= OnChangeSettingsButtonClicked;
            }

            // Room Info dropdown change events
            if (_gameModeDropdown != null)
            {
                _gameModeDropdown.UnregisterValueChangedCallback(OnGameModeChanged);
            }

            if (_mapDropdown != null)
            {
                _mapDropdown.UnregisterValueChangedCallback(OnMapChanged);
            }

            // Password field change event
            if (_passwordField != null)
            {
                _passwordField.UnregisterValueChangedCallback(OnPasswordChanged);
            }

            // Dropdown change events
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.UnregisterValueChangedCallback(OnTimeLimitChanged);
            }

            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.UnregisterValueChangedCallback(OnArrowLimitChanged);
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
                case nameof(MatchRoomViewModel.PlayerName):
                    // プレイヤー名が変更された場合、TextFieldを更新
                    if (_playerNameInput != null && _playerNameInput.value != ViewModel.PlayerName)
                    {
                        _playerNameInput.value = ViewModel.PlayerName;
                    }
                    break;

                case nameof(MatchRoomViewModel.IsCountingDown):
                    UpdateCountdownDialog();
                    break;

                case nameof(MatchRoomViewModel.IsReady):
                case nameof(MatchRoomViewModel.IsHost):
                case nameof(MatchRoomViewModel.CanStartGame):
                    UpdateButtonVisibility();
                    break;

                case nameof(MatchRoomViewModel.Players):
                    // プレイヤー情報の変更（チーム変更など）
                    PopulatePlayerList();
                    UpdateUI();
                    break;

                case nameof(MatchRoomViewModel.CurrentPlayers):
                    // プレイヤー数の変更
                    UpdateUI();
                    break;

                case nameof(MatchRoomViewModel.MaxPlayers):
                    // 最大プレイヤー数の変更 - スロット数を再生成
                    PopulatePlayerList();
                    UpdateUI();
                    break;

                case nameof(MatchRoomViewModel.GameMode):
                    // ゲームモード変更時にドロップダウンの有効/無効を更新
                    UpdateArrowLimitChoices(ViewModel.GameMode);
                    UpdateGameSettingsAvailability();
                    UpdateUI();
                    break;

                case nameof(MatchRoomViewModel.TimeLimit):
                case nameof(MatchRoomViewModel.ArrowLimit):
                    // 設定値が変更された場合にドロップダウン表示を更新
                    UpdateGameSettingsDropdownValues();
                    break;

                default:
                    UpdateUI();
                    break;
            }
        }

        /// <summary>
        /// プレイヤーリストの変更イベントを処理します
        /// </summary>
        private void OnPlayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PopulatePlayerList();
            UpdateUI();
        }

        /// <summary>
        /// ゲーム開始イベントを処理します
        /// </summary>
        private void OnGameStarting(object? sender, EventArgs e)
        {
            _isCountdownActive = true;

            UpdateCountdownDialog();
        }

        /// <summary>
        /// カウントダウン更新イベントを処理します
        /// </summary>
        private void OnCountdownUpdated(object? sender, int secondsRemaining)
        {
            if (_countdownLabel != null)
            {
                _countdownLabel.text = secondsRemaining.ToString();
            }

            // カウントダウンティックSE
            if (secondsRemaining > 0 && _countdownTickSfx != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                audioService?.PlaySfx(_countdownTickSfx);
            }

            if (secondsRemaining <= 0)
            {
                _isCountdownActive = false;
            }
        }

        /// <summary>
        /// ローディングシーン遷移要求イベントを処理します
        /// </summary>
        private void OnLoadingSceneRequested(object? sender, EventArgs e)
        {
            Debug.Log("[MatchRoomView] Loading scene requested. (Handled by ASM automatically)");
            // Loading screens are handled automatically by Advanced Scene Manager
        }

        /// <summary>
        /// エラー発生イベントを処理します
        /// </summary>
        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            Debug.LogError($"[MatchRoomView] Error: {errorMessage}");
        }

        /// <summary>
        /// 退出ボタンがクリックされた時の処理
        /// </summary>
        private void OnLeaveRoomButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.LeaveRoom();
        }

        /// <summary>
        /// ゲーム開始ボタンがクリックされた時の処理
        /// </summary>
        private void OnStartGameButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.StartGame();
        }

        /// <summary>
        /// 準備完了ボタンがクリックされた時の処理
        /// </summary>
        private void OnReadyButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.ToggleReady();
        }

        /// <summary>
        /// 準備キャンセルボタンがクリックされた時の処理
        /// </summary>
        private void OnCancelReadyButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.ToggleReady();
        }

        /// <summary>
        /// カウントダウンキャンセルボタンがクリックされた時の処理
        /// </summary>
        private void OnCancelCountdownButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.CancelCountdown();
            _isCountdownActive = false;
        }

        /// <summary>
        /// チーム変更ボタンがクリックされた時の処理
        /// </summary>
        private void OnTeamButtonClicked(string playerId)
        {
            if (ViewModel == null)
            {
                return;
            }

            PlayButtonClickSfx();

            var player = ViewModel.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                // TeamFightモードかどうかを確認
                bool isTeamFightMode = ViewModel.GameMode == "TeamFight";

                // チームをローテーション
                PlayerTeam newTeam;
                if (isTeamFightMode)
                {
                    // TeamFightモードではNoneをスキップ（TeamA ↔ TeamB のみ）
                    newTeam = player.Team switch
                    {
                        PlayerTeam.None => PlayerTeam.TeamA,
                        PlayerTeam.TeamA => PlayerTeam.TeamB,
                        PlayerTeam.TeamB => PlayerTeam.TeamA,
                        _ => PlayerTeam.TeamA
                    };
                }
                else
                {
                    // 通常モード: None → TeamA → TeamB → None
                    newTeam = player.Team switch
                    {
                        PlayerTeam.None => PlayerTeam.TeamA,
                        PlayerTeam.TeamA => PlayerTeam.TeamB,
                        PlayerTeam.TeamB => PlayerTeam.None,
                        _ => PlayerTeam.None
                    };
                }

                ViewModel.ChangePlayerTeam(playerId, newTeam);
            }
        }

        /// <summary>
        /// キックボタンがクリックされた時の処理
        /// </summary>
        private void OnKickButtonClicked(string playerId)
        {
            PlayButtonClickSfx();
            ViewModel?.KickPlayer(playerId);
        }

        /// <summary>
        /// タイムリミット変更イベント
        /// </summary>
        private void OnTimeLimitChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            // タイムリミット文字列を秒数に変換
            int seconds = evt.newValue switch
            {
                "3:00" => 180,
                "5:00" => 300,
                "10:00" => 600,
                "15:00" => 900,
                "No Limit" => 0,
                _ => 300
            };

            ViewModel.TimeLimit = seconds;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Time limit changed to: {evt.newValue} ({seconds} seconds)");
        }

        /// <summary>
        /// 矢の制限変更イベント
        /// </summary>
        private void OnArrowLimitChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            // 矢の制限文字列を数値に変換
            int arrows = evt.newValue switch
            {
                "5" => 5,
                "10" => 10,
                "15" => 15,
                "20" => 20,
                "No Limit" => 0,
                _ => 0
            };

            ViewModel.ArrowLimit = arrows;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Arrow limit changed to: {evt.newValue} ({arrows} arrows)");
        }

        /// <summary>
        /// プレイヤー名更新ボタンクリックイベント
        /// </summary>
        private void OnUpdatePlayerNameButtonClicked()
        {
            if (ViewModel == null || _playerNameInput == null)
            {
                return;
            }

            PlayButtonClickSfx();

            var newName = _playerNameInput.value;

            if (string.IsNullOrWhiteSpace(newName))
            {
                Debug.LogWarning("[MatchRoomView] Player name cannot be empty.");
                return;
            }

            // ViewModelのPlayerNameプロパティを更新
            ViewModel.PlayerName = newName;

            // LobbyServiceを通じてネットワーク上のプレイヤー名を更新
            // （LobbyService.SetPlayerName内でPlayerPrefsに保存されます）
            var lobbyService = ServiceLocator.Instance.TryGet<ILobbyService>();
            if (lobbyService != null && lobbyService.IsInRoom)
            {
                lobbyService.SetPlayerName(newName);
                Debug.Log($"[MatchRoomView] Player name updated in network: {newName}");
            }
            else
            {
                Debug.Log($"[MatchRoomView] Player name updated locally (not in room): {newName}");
            }
        }

        /// <summary>
        /// ゲームモード変更イベント
        /// </summary>
        private void OnGameModeChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.GameMode = evt.newValue;

            // ゲームモードに応じてArrow Limitの選択肢を更新
            UpdateArrowLimitChoices(evt.newValue);

            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Game mode changed to: {evt.newValue}");
        }

        /// <summary>
        /// マップ変更イベント
        /// </summary>
        private void OnMapChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.MapName = evt.newValue;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Map changed to: {evt.newValue}");
        }

        /// <summary>
        /// パスワードフィールド変更イベント（アスタリスク表示を更新）
        /// </summary>
        private void OnPasswordChanged(ChangeEvent<string> evt)
        {
            if (_passwordAsterisksLabel == null)
            {
                return;
            }

            // Update asterisks label to match password length
            if (!string.IsNullOrEmpty(evt.newValue))
            {
                _passwordAsterisksLabel.text = new string('*', evt.newValue.Length);
            }
            else
            {
                _passwordAsterisksLabel.text = "(empty)";
            }
        }

        /// <summary>
        /// スロットにNPCを追加する処理
        /// </summary>
        private void OnAddNPCToSlot(int slotIndex)
        {
            PlayButtonClickSfx();
            ViewModel?.AddNPC(slotIndex);
        }

        /// <summary>
        /// NPCを削除する処理
        /// </summary>
        private void OnRemoveNPCClicked(string npcId)
        {
            PlayButtonClickSfx();
            ViewModel?.RemoveNPC(npcId);
        }

        /// <summary>
        /// NPC難易度変更イベント
        /// </summary>
        private void OnNPCDifficultyChanged(string npcId, string difficulty)
        {
            ViewModel?.ChangeNPCDifficulty(npcId, difficulty);
        }

        /// <summary>
        /// Change Settings / Apply Settings ボタンがクリックされた時の処理
        /// </summary>
        private void OnChangeSettingsButtonClicked()
        {
            PlayButtonClickSfx();

            if (_isEditMode)
            {
                // Apply Settings: 設定を適用
                ApplySettings();
                _isEditMode = false;
                if (_changeSettingsButton != null)
                {
                    _changeSettingsButton.text = "Change Settings";
                }
                Debug.Log("[MatchRoomView] Settings applied, returning to read-only mode");
            }
            else
            {
                // Change Settings: 編集モードに切り替え
                _isEditMode = true;
                if (_changeSettingsButton != null)
                {
                    _changeSettingsButton.text = "Apply Settings";
                }
                Debug.Log("[MatchRoomView] Entering edit mode");
            }

            // ゲーム設定の有効/無効を更新
            UpdateGameSettingsAvailability();

            // UIを更新
            UpdateUI();
        }

        /// <summary>
        /// 設定を適用します
        /// </summary>
        private void ApplySettings()
        {
            if (ViewModel == null)
            {
                return;
            }

            bool hasChanges = false;

            // ルーム名の変更を検出
            if (_roomNameField != null && _roomNameField.value != ViewModel.RoomName)
            {
                ViewModel.RoomName = _roomNameField.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Room name changed to: {_roomNameField.value}");
            }

            // パスワードの変更を検出
            if (_passwordField != null && _passwordField.value != ViewModel.Password)
            {
                ViewModel.Password = _passwordField.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Password changed");
            }

            // 公開設定の変更を検出
            if (_publicToggle != null && _publicToggle.value != ViewModel.IsPublic)
            {
                ViewModel.IsPublic = _publicToggle.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Public setting changed to: {_publicToggle.value}");
            }

            // 最大プレイヤー数の変更を検出
            if (_maxPlayersDropdown != null && int.TryParse(_maxPlayersDropdown.value, out int maxPlayers) && maxPlayers != ViewModel.MaxPlayers)
            {
                ViewModel.MaxPlayers = maxPlayers;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Max players changed to: {maxPlayers}");
            }

            // ゲームモードの変更を検出
            if (_gameModeDropdown != null && _gameModeDropdown.value != ViewModel.GameMode)
            {
                ViewModel.GameMode = _gameModeDropdown.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Game mode changed to: {_gameModeDropdown.value}");
            }

            // マップの変更を検出
            if (_mapDropdown != null && _mapDropdown.value != ViewModel.MapName)
            {
                ViewModel.MapName = _mapDropdown.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Map changed to: {_mapDropdown.value}");
            }

            // タイムリミットの変更を検出
            if (_timeLimitDropdown != null)
            {
                int timeLimit = 0;
                if (_timeLimitDropdown.value == "No Limit")
                {
                    timeLimit = 0;
                }
                else if (_timeLimitDropdown.value.Contains(":"))
                {
                    // "5:00" -> 300 seconds
                    string[] parts = _timeLimitDropdown.value.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int minutes))
                    {
                        timeLimit = minutes * 60;
                    }
                }

                if (timeLimit != ViewModel.TimeLimit)
                {
                    ViewModel.TimeLimit = timeLimit;
                    hasChanges = true;
                    Debug.Log($"[MatchRoomView] Time limit changed to: {timeLimit} seconds");
                }
            }

            // 矢の制限の変更を検出
            if (_arrowLimitDropdown != null)
            {
                int arrowLimit = 0;
                if (_arrowLimitDropdown.value == "No Limit")
                {
                    arrowLimit = 0;
                }
                else if (int.TryParse(_arrowLimitDropdown.value, out int limit))
                {
                    arrowLimit = limit;
                }

                if (arrowLimit != ViewModel.ArrowLimit)
                {
                    ViewModel.ArrowLimit = arrowLimit;
                    hasChanges = true;
                    Debug.Log($"[MatchRoomView] Arrow limit changed to: {arrowLimit}");
                }
            }

            // 変更があった場合のみサーバーに通知
            if (hasChanges)
            {
                ViewModel.UpdateRoomSettings();
                Debug.Log("[MatchRoomView] Room settings applied and synced to server");
            }
            else
            {
                Debug.Log("[MatchRoomView] No settings changes detected");
            }
        }

        #endregion
    }
}
