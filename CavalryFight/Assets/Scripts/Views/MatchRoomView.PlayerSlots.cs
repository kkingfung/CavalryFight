#nullable enable

using System.Collections.Generic;
using System.Linq;
using CavalryFight.Services.Lobby;
using CavalryFight.ViewModels;
using CavalryFight.ViewModels.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// MatchRoomViewのプレイヤースロット管理
    /// </summary>
    public partial class MatchRoomView
    {
        #region Player List Population

        /// <summary>
        /// プレイヤーリストを生成します
        /// </summary>
        private void PopulatePlayerList()
        {
            if (ViewModel == null || _playerListContainer == null)
            {
                return;
            }

            // 既存のリストをクリア
            _playerListContainer.Clear();
            _playerItemElements.Clear();

            // 固定スロット数を作成（MaxPlayers分）
            int maxPlayers = ViewModel.MaxPlayers;

            // プレイヤーをSlotIndexでマップに変換（重複がある場合は最新のものを使用）
            var playersBySlot = new Dictionary<int, PlayerInfo>();
            foreach (var player in ViewModel.Players)
            {
                playersBySlot[player.SlotIndex] = player;
            }

            for (int i = 0; i < maxPlayers; i++)
            {
                VisualElement playerItem;

                if (playersBySlot.ContainsKey(i))
                {
                    // このスロットにプレイヤーが存在する場合
                    var player = playersBySlot[i];
                    playerItem = CreatePlayerListItem(player);
                    _playerItemElements[player.PlayerId] = playerItem;
                }
                else
                {
                    // 空スロットの場合
                    playerItem = CreateEmptySlot(i);
                }

                _playerListContainer.Add(playerItem);
            }
        }

        /// <summary>
        /// 空スロットのUI要素を作成します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>作成されたVisualElement</returns>
        private VisualElement CreateEmptySlot(int slotIndex)
        {
            var container = new VisualElement();
            container.AddToClassList("player-item");
            container.AddToClassList("empty");
            container.name = $"EmptySlot_{slotIndex}";

            // プレイヤー情報セクション
            var infoSection = new VisualElement();
            infoSection.AddToClassList("player-item-info");

            // "Empty" ラベル
            var nameLabel = new Label("Empty");
            nameLabel.AddToClassList("player-item-name");
            nameLabel.AddToClassList("empty");
            infoSection.Add(nameLabel);

            container.Add(infoSection);

            // ホストの場合: Add NPC ボタンを表示
            if (ViewModel != null && ViewModel.IsHost)
            {
                var actionsSection = new VisualElement();
                actionsSection.AddToClassList("player-item-actions");

                var addNpcButton = new Button(() => OnAddNPCToSlot(slotIndex));
                addNpcButton.text = "Add CPU";
                addNpcButton.AddToClassList("add-npc-button");
                actionsSection.Add(addNpcButton);

                container.Add(actionsSection);
            }

            return container;
        }

        /// <summary>
        /// プレイヤーリストアイテムのUI要素を作成します
        /// </summary>
        /// <param name="player">プレイヤー情報</param>
        /// <returns>作成されたVisualElement</returns>
        private VisualElement CreatePlayerListItem(PlayerInfo player)
        {
            var container = new VisualElement();
            container.AddToClassList("player-item");
            container.name = $"PlayerItem_{player.PlayerId}";

            // プレイヤー情報セクション
            var infoSection = new VisualElement();
            infoSection.AddToClassList("player-item-info");

            // プレイヤー名（ローカルプレイヤーの場合は "(YOU)" を追加）
            var displayName = player.IsLocalPlayer ? $"{player.PlayerName} (YOU)" : player.PlayerName;
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("player-item-name");
            infoSection.Add(nameLabel);

            // ローカルプレイヤーの場合はコンテナの背景色を変更
            if (player.IsLocalPlayer)
            {
                container.AddToClassList("local-player");
            }

            // ステータス行（Team, HOST/Ready, FPS/Difficulty）
            var statsRow = new VisualElement();
            statsRow.AddToClassList("player-item-stats");

            // 1. チームバッジ（名前の直後）
            var teamBadge = new Label(GetTeamLabel(player.Team));
            teamBadge.AddToClassList("team-badge");
            teamBadge.AddToClassList(GetTeamClass(player.Team));
            statsRow.Add(teamBadge);

            // 2. HOST/Ready バッジ
            if (player.IsHost)
            {
                // ホストプレイヤー: HOSTバッジ
                var hostBadge = new Label("HOST");
                hostBadge.AddToClassList("host-badge");
                statsRow.Add(hostBadge);
            }
            else if (!player.IsNPC)
            {
                // 非ホスト・非NPC: Readyバッジ
                var readyBadge = new Label(player.IsReady ? "READY" : "NOT READY");
                readyBadge.AddToClassList("ready-badge");
                readyBadge.AddToClassList(player.IsReady ? "ready-true" : "ready-false");
                statsRow.Add(readyBadge);
            }
            else
            {
                // NPC: スペーサー（HOST/Readyの幅分）
                var spacer = new VisualElement();
                spacer.AddToClassList("stats-spacer");
                statsRow.Add(spacer);
            }

            // 3. FPS（通常プレイヤー）or Difficulty（NPC）
            if (player.IsNPC)
            {
                // NPC難易度ドロップダウン（ホストのみ編集可能）
                if (ViewModel != null && ViewModel.IsHost)
                {
                    var difficultyDropdown = new DropdownField();
                    difficultyDropdown.choices = new List<string> { "Easy", "Normal", "Hard", "Expert" };
                    difficultyDropdown.value = player.Difficulty;
                    difficultyDropdown.AddToClassList("npc-difficulty-dropdown");
                    difficultyDropdown.RegisterValueChangedCallback(evt => OnNPCDifficultyChanged(player.PlayerId, evt.newValue));
                    statsRow.Add(difficultyDropdown);
                }
                else
                {
                    var difficultyLabel = new Label($"CPU ({player.Difficulty})");
                    difficultyLabel.AddToClassList("player-item-fps");
                    statsRow.Add(difficultyLabel);
                }
            }
            else
            {
                // 通常プレイヤー: FPS
                var fpsLabel = new Label($"{player.Fps} FPS");
                fpsLabel.AddToClassList("player-item-fps");
                statsRow.Add(fpsLabel);
            }

            infoSection.Add(statsRow);
            container.Add(infoSection);

            // アクションセクション（チーム変更、キック、Remove NPC）
            if (ViewModel != null)
            {
                var actionsSection = new VisualElement();
                actionsSection.AddToClassList("player-item-actions");

                if (player.IsNPC)
                {
                    // NPCの場合: チーム変更ボタン（ホストのみ）
                    if (ViewModel.IsHost)
                    {
                        var teamButton = new Button(() => OnTeamButtonClicked(player.PlayerId));
                        teamButton.text = "Team";
                        teamButton.AddToClassList("team-button");
                        actionsSection.Add(teamButton);

                        var removeNpcButton = new Button(() => OnRemoveNPCClicked(player.PlayerId));
                        removeNpcButton.text = "Remove";
                        removeNpcButton.AddToClassList("kick-button");
                        actionsSection.Add(removeNpcButton);
                    }
                }
                else
                {
                    // 通常プレイヤーの場合: チーム変更ボタン
                    // ホストは全員のチームを変更可能、非ホストは自分のチームのみ変更可能
                    if (ViewModel.IsHost || player.IsLocalPlayer)
                    {
                        var teamButton = new Button(() => OnTeamButtonClicked(player.PlayerId));
                        teamButton.text = "Team";
                        teamButton.AddToClassList("team-button");
                        actionsSection.Add(teamButton);
                    }

                    // キックボタン（ホストのみ、自分以外）
                    if (ViewModel.IsHost && !player.IsHost)
                    {
                        var kickButton = new Button(() => OnKickButtonClicked(player.PlayerId));
                        kickButton.text = "Kick";
                        kickButton.AddToClassList("kick-button");
                        actionsSection.Add(kickButton);
                    }
                }

                container.Add(actionsSection);
            }

            return container;
        }

        /// <summary>
        /// チームラベルを取得します
        /// </summary>
        private string GetTeamLabel(PlayerTeam team)
        {
            return team switch
            {
                PlayerTeam.TeamA => "Team A",
                PlayerTeam.TeamB => "Team B",
                _ => "No Team"
            };
        }

        /// <summary>
        /// チームクラスを取得します
        /// </summary>
        private string GetTeamClass(PlayerTeam team)
        {
            return team switch
            {
                PlayerTeam.TeamA => "team-a",
                PlayerTeam.TeamB => "team-b",
                _ => "team-none"
            };
        }

        #endregion
    }
}
