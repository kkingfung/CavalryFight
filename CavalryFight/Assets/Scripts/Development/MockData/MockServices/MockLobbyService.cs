#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Services.Lobby;
using Unity.Collections;
using UnityEngine;

namespace CavalryFight.Development.MockData.MockServices
{
    /// <summary>
    /// モックロビーサービス
    /// </summary>
    /// <remarks>
    /// 開発用のモックロビーサービス。
    /// 実際のネットワーク接続なしでロビー/ルーム画面をテストできます。
    /// </remarks>
    public class MockLobbyService : ILobbyService
    {
        #region Fields

        /// <summary>モックシーン設定</summary>
        private readonly MockSceneConfig? _mockConfig;

        /// <summary>現在のルーム設定</summary>
        private RoomSettings _currentRoomSettings;

        /// <summary>プレイヤースロットリスト</summary>
        private readonly List<PlayerSlot> _playerSlots;

        /// <summary>ローカルプレイヤー情報</summary>
        private LobbyPlayerInfo? _localPlayerInfo;

        /// <summary>ホストかどうか</summary>
        private bool _isHost;

        /// <summary>ルームに参加しているかどうか</summary>
        private bool _isInRoom;

        /// <summary>現在のジョインコード</summary>
        private string? _currentJoinCode;

        /// <summary>利用可能なルームリスト</summary>
        private readonly List<RoomInfo> _availableRooms;

        /// <summary>ホストプレイヤーのID</summary>
        private ulong _hostPlayerId;

        #endregion

        #region Events

        /// <inheritdoc />
        public event Action<string>? RoomCreated;

        /// <inheritdoc />
        public event Action? RoomJoined;

        /// <inheritdoc />
        public event Action? RoomLeft;

        /// <inheritdoc />
        public event Action<RoomSettings>? RoomSettingsChanged;

        /// <inheritdoc />
        public event Action<ulong>? PlayerJoined;

        /// <inheritdoc />
        public event Action<ulong>? PlayerLeft;

        /// <inheritdoc />
        public event Action<int, PlayerSlot>? PlayerSlotChanged;

        /// <inheritdoc />
        public event Action<ulong, bool>? PlayerReadyChanged;

        /// <inheritdoc />
        public event Action? MatchStarting;

        /// <inheritdoc />
        public event Action<int>? CountdownStarted;

        /// <inheritdoc />
        public event Action<int>? CountdownUpdated;

        /// <inheritdoc />
        public event Action? CountdownCancelled;

        /// <inheritdoc />
        public event Action<string>? ErrorOccurred;

        /// <inheritdoc />
        public event Action? HostDisconnected;

        /// <inheritdoc />
        public event Action<IReadOnlyList<RoomInfo>>? AvailableRoomsUpdated;

        #endregion

        #region Properties

        /// <inheritdoc />
        public RoomSettings CurrentRoomSettings => _currentRoomSettings;

        /// <inheritdoc />
        public IReadOnlyList<PlayerSlot> PlayerSlots => _playerSlots;

        /// <inheritdoc />
        public LobbyPlayerInfo? LocalPlayerInfo => _localPlayerInfo;

        /// <inheritdoc />
        public bool IsHost => _isHost;

        /// <inheritdoc />
        public bool IsInRoom => _isInRoom;

        /// <inheritdoc />
        public string? CurrentJoinCode => _currentJoinCode;

        /// <inheritdoc />
        public IReadOnlyList<RoomInfo> AvailableRooms => _availableRooms;

        /// <summary>
        /// ホストプレイヤーのID（モック用）
        /// </summary>
        public ulong HostPlayerId => _hostPlayerId;

        #endregion

        #region Constructors

        /// <summary>
        /// MockLobbyServiceの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="mockConfig">モック設定</param>
        public MockLobbyService(MockSceneConfig? mockConfig)
        {
            _mockConfig = mockConfig;
            _playerSlots = new List<PlayerSlot>();
            _availableRooms = new List<RoomInfo>();

            // 8つの空スロットを初期化
            for (int i = 0; i < 8; i++)
            {
                _playerSlots.Add(new PlayerSlot(i));
            }

            // モック設定からデータを読み込み
            InitializeFromMockConfig();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// モック設定からデータを初期化します
        /// </summary>
        private void InitializeFromMockConfig()
        {
            // ロビー設定からルームリストを読み込み
            if (_mockConfig?.LobbySceneMockData != null)
            {
                var lobbyConfig = _mockConfig.LobbySceneMockData;

                // 利用可能なルームリストを設定
                foreach (var mockRoom in lobbyConfig.AvailableRooms)
                {
                    var roomInfo = new RoomInfo
                    {
                        RoomId = mockRoom.RoomId,
                        RoomName = mockRoom.RoomName,
                        HostName = mockRoom.HostName,
                        GameMode = ParseGameMode(mockRoom.GameMode),
                        MapName = mockRoom.MapName,
                        CurrentPlayers = mockRoom.CurrentPlayers,
                        MaxPlayers = mockRoom.MaxPlayers,
                        HasPassword = mockRoom.HasPassword,
                        JoinCode = mockRoom.JoinCode,
                        IsPublic = mockRoom.IsPublic
                    };
                    _availableRooms.Add(roomInfo);
                }

                Debug.Log($"[MockLobbyService] Loaded {_availableRooms.Count} mock rooms from lobby config");
            }
            else
            {
                // デフォルトのモックルームを作成（テスト用）
                CreateDefaultMockRooms();
            }

            // ルーム設定からデータを読み込み
            if (_mockConfig?.RoomSceneMockData != null)
            {
                var roomConfig = _mockConfig.RoomSceneMockData;

                // ホストかゲストかを設定
                _isHost = roomConfig.IsHost;
                _isInRoom = true;
                _currentJoinCode = roomConfig.JoinCode;

                // ルーム設定を作成
                _currentRoomSettings = new RoomSettings
                {
                    RoomName = new FixedString64Bytes(roomConfig.RoomName),
                    GameMode = ParseGameMode(roomConfig.GameMode),
                    MaxPlayers = roomConfig.MaxPlayers,
                    Password = new FixedString64Bytes(roomConfig.Password),
                    IsPublic = roomConfig.IsPublic,
                    TimeLimit = roomConfig.TimeLimit,
                    ArrowLimit = roomConfig.ArrowLimit,
                    MapName = new FixedString64Bytes(roomConfig.MapName)
                };

                // プレイヤースロットをクリア
                for (int i = 0; i < _playerSlots.Count; i++)
                {
                    _playerSlots[i] = new PlayerSlot(i);
                }

                // モック設定からプレイヤーを追加
                int slotIndex = 0;
                ulong localPlayerSlotId = 0;
                bool localPlayerSlotFound = false;

                foreach (var mockPlayer in roomConfig.Players)
                {
                    if (slotIndex >= 8) break;

                    // ホストプレイヤーを追跡
                    if (mockPlayer.IsHost)
                    {
                        _hostPlayerId = (ulong)slotIndex;
                    }

                    // ローカルプレイヤーのスロットを決定
                    bool isLocalPlayerSlot = false;
                    if (roomConfig.IsHost && mockPlayer.IsHost)
                    {
                        // ホストモード: ホストプレイヤーがローカル
                        isLocalPlayerSlot = true;
                        localPlayerSlotId = (ulong)slotIndex;
                        localPlayerSlotFound = true;
                    }
                    else if (!roomConfig.IsHost && !mockPlayer.IsHost && !mockPlayer.IsNPC && !localPlayerSlotFound)
                    {
                        // ゲストモード: 最初の非ホスト・非NPCプレイヤーがローカル
                        isLocalPlayerSlot = true;
                        localPlayerSlotId = (ulong)slotIndex;
                        localPlayerSlotFound = true;
                    }

                    var slot = new PlayerSlot
                    {
                        SlotIndex = slotIndex,
                        PlayerId = (ulong)slotIndex,
                        PlayerName = new FixedString64Bytes(isLocalPlayerSlot ? roomConfig.LocalPlayerName : mockPlayer.PlayerName),
                        IsAI = mockPlayer.IsNPC,
                        AIDifficulty = ParseAIDifficulty(mockPlayer.NPCDifficulty),
                        IsReady = mockPlayer.IsNPC || (isLocalPlayerSlot ? roomConfig.IsLocalPlayerReady : mockPlayer.IsReady),
                        TeamIndex = ParseTeamIndex(mockPlayer.Team)
                    };

                    _playerSlots[slotIndex] = slot;
                    slotIndex++;
                }

                // ローカルプレイヤー情報を設定
                _localPlayerInfo = new LobbyPlayerInfo
                {
                    PlayerId = localPlayerSlotId,
                    PlayerName = roomConfig.LocalPlayerName,
                    IsHost = roomConfig.IsHost,
                    IsLocalPlayer = true
                };

                Debug.Log($"[MockLobbyService] Initialized room with {slotIndex} players, IsHost: {_isHost}, LocalPlayerSlot: {localPlayerSlotId}, HostPlayerId: {_hostPlayerId}");
            }
            else
            {
                // デフォルト設定
                _currentRoomSettings = RoomSettings.CreateDefault();
                _isHost = true;
                _isInRoom = false;
            }
        }

        /// <summary>
        /// デフォルトのモックルームを作成します（スクロールテスト用に多数作成）
        /// </summary>
        private void CreateDefaultMockRooms()
        {
            // Arena - 空きあり
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-001",
                RoomName = "Epic Arena Battle",
                HostName = "Knight_Alex",
                GameMode = GameMode.Arena,
                MapName = "Castle Arena",
                CurrentPlayers = 3,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "ARENA1",
                IsPublic = true
            });

            // Hunting - 空きあり
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-002",
                RoomName = "Wolf Hunt",
                HostName = "Hunter_Rin",
                GameMode = GameMode.Hunting,
                MapName = "Forest Valley",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                HasPassword = false,
                JoinCode = "HUNT01",
                IsPublic = true
            });

            // Arena - 満員
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-003",
                RoomName = "Tournament Finals",
                HostName = "Champion_Max",
                GameMode = GameMode.Arena,
                MapName = "Desert Outpost",
                CurrentPlayers = 8,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "TOUR01",
                IsPublic = true
            });

            // パスワード付きルーム
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-004",
                RoomName = "Private Match",
                HostName = "Warrior_Yuki",
                GameMode = GameMode.ScoreMatch,
                MapName = "Mountain Pass",
                CurrentPlayers = 4,
                MaxPlayers = 8,
                HasPassword = true,
                JoinCode = "PRIV01",
                IsPublic = false
            });

            // TeamFight - 空きあり
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-005",
                RoomName = "Team vs Team",
                HostName = "Captain_Dan",
                GameMode = GameMode.TeamFight,
                MapName = "Castle Arena",
                CurrentPlayers = 6,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "TEAM01",
                IsPublic = true
            });

            // Arena - パスワード付き、ほぼ満員
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-006",
                RoomName = "Pro Players Only",
                HostName = "Elite_Sora",
                GameMode = GameMode.Arena,
                MapName = "Training Grounds",
                CurrentPlayers = 7,
                MaxPlayers = 8,
                HasPassword = true,
                JoinCode = "PRO123",
                IsPublic = false
            });

            // 追加のモックルーム（スクロールテスト用）
            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-007",
                RoomName = "Beginners Welcome",
                HostName = "Teacher_Ken",
                GameMode = GameMode.Arena,
                MapName = "Training Grounds",
                CurrentPlayers = 1,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "BEGIN1",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-008",
                RoomName = "Night Hunters",
                HostName = "Shadow_Jin",
                GameMode = GameMode.Hunting,
                MapName = "Dark Forest",
                CurrentPlayers = 3,
                MaxPlayers = 4,
                HasPassword = false,
                JoinCode = "NIGHT1",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-009",
                RoomName = "Clan Battle Room",
                HostName = "Leader_Hana",
                GameMode = GameMode.TeamFight,
                MapName = "Castle Arena",
                CurrentPlayers = 8,
                MaxPlayers = 8,
                HasPassword = true,
                JoinCode = "CLAN01",
                IsPublic = false
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-010",
                RoomName = "Quick Match",
                HostName = "Speedy_Taro",
                GameMode = GameMode.Arena,
                MapName = "Small Arena",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                HasPassword = false,
                JoinCode = "QUICK1",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-011",
                RoomName = "Score Attack",
                HostName = "Scorer_Mai",
                GameMode = GameMode.ScoreMatch,
                MapName = "Desert Outpost",
                CurrentPlayers = 5,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "SCORE1",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-012",
                RoomName = "Weekend Warriors",
                HostName = "Casual_Yui",
                GameMode = GameMode.Arena,
                MapName = "Forest Valley",
                CurrentPlayers = 4,
                MaxPlayers = 6,
                HasPassword = false,
                JoinCode = "WEEK01",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-013",
                RoomName = "Hunting Party",
                HostName = "Pack_Kota",
                GameMode = GameMode.Hunting,
                MapName = "Mountain Pass",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                HasPassword = false,
                JoinCode = "PARTY1",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-014",
                RoomName = "Championship Room",
                HostName = "Champ_Ryu",
                GameMode = GameMode.Arena,
                MapName = "Castle Arena",
                CurrentPlayers = 6,
                MaxPlayers = 8,
                HasPassword = true,
                JoinCode = "CHAMP1",
                IsPublic = false
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-015",
                RoomName = "Practice Grounds",
                HostName = "Coach_Mei",
                GameMode = GameMode.Arena,
                MapName = "Training Grounds",
                CurrentPlayers = 1,
                MaxPlayers = 4,
                HasPassword = false,
                JoinCode = "PRAC01",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-016",
                RoomName = "Late Night Battles",
                HostName = "Owl_Shin",
                GameMode = GameMode.Arena,
                MapName = "Desert Outpost",
                CurrentPlayers = 3,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "LATE01",
                IsPublic = true
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-017",
                RoomName = "Friends Only",
                HostName = "Social_Aoi",
                GameMode = GameMode.TeamFight,
                MapName = "Forest Valley",
                CurrentPlayers = 4,
                MaxPlayers = 8,
                HasPassword = true,
                JoinCode = "FRND01",
                IsPublic = false
            });

            _availableRooms.Add(new RoomInfo
            {
                RoomId = "mock-room-018",
                RoomName = "Veteran Clash",
                HostName = "Old_Guard",
                GameMode = GameMode.Arena,
                MapName = "Castle Arena",
                CurrentPlayers = 7,
                MaxPlayers = 8,
                HasPassword = false,
                JoinCode = "VET001",
                IsPublic = true
            });

            Debug.Log($"[MockLobbyService] Created {_availableRooms.Count} default mock rooms");
        }

        /// <summary>
        /// ゲームモード文字列をパースします
        /// </summary>
        private static GameMode ParseGameMode(string modeName)
        {
            return modeName.ToLower() switch
            {
                "arena" => GameMode.Arena,
                "scorematch" => GameMode.ScoreMatch,
                "teamfight" => GameMode.TeamFight,
                "deathmatch" => GameMode.Deathmatch,
                "hunting" => GameMode.Hunting,
                _ => GameMode.Arena
            };
        }

        /// <summary>
        /// AI難易度文字列をパースします
        /// </summary>
        private static AIDifficulty ParseAIDifficulty(string difficulty)
        {
            return difficulty.ToLower() switch
            {
                "easy" => AIDifficulty.Easy,
                "normal" => AIDifficulty.Normal,
                "hard" => AIDifficulty.Hard,
                "expert" => AIDifficulty.Expert,
                _ => AIDifficulty.Normal
            };
        }

        /// <summary>
        /// チーム名からチームインデックスをパースします
        /// </summary>
        private static int ParseTeamIndex(string teamName)
        {
            return teamName.ToLower() switch
            {
                "teama" => 0,
                "teamb" => 1,
                "none" => -1,
                _ => -1
            };
        }

        #endregion

        #region IService Implementation

        /// <inheritdoc />
        public void Initialize()
        {
            Debug.Log("[MockLobbyService] Initialized");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Debug.Log("[MockLobbyService] Disposed");
        }

        #endregion

        #region Host Methods

        /// <inheritdoc />
        public bool CreateRoom(RoomSettings roomSettings, string playerName)
        {
            Debug.Log($"[MockLobbyService] CreateRoom called: {roomSettings.RoomName}, Player: {playerName}");

            _currentRoomSettings = roomSettings;
            _isHost = true;
            _isInRoom = true;
            _currentJoinCode = "MOCK123";

            // ローカルプレイヤーをスロット0に追加
            _localPlayerInfo = new LobbyPlayerInfo(0, playerName, true) { IsLocalPlayer = true };
            _playerSlots[0] = new PlayerSlot(0, 0, playerName);

            RoomCreated?.Invoke(_currentJoinCode);
            return true;
        }

        /// <inheritdoc />
        public bool UpdateRoomSettings(RoomSettings roomSettings)
        {
            if (!_isHost)
            {
                Debug.LogWarning("[MockLobbyService] Only host can update room settings");
                return false;
            }

            Debug.Log($"[MockLobbyService] UpdateRoomSettings: GameMode={roomSettings.GameMode}, Map={roomSettings.MapName}");
            _currentRoomSettings = roomSettings;
            RoomSettingsChanged?.Invoke(roomSettings);
            return true;
        }

        /// <inheritdoc />
        public bool AddCPUPlayer(AIDifficulty difficulty, int teamIndex = -1)
        {
            if (!_isHost)
            {
                Debug.LogWarning("[MockLobbyService] Only host can add CPU players");
                return false;
            }

            // 空きスロットを探す
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].IsEmpty())
                {
                    var slot = new PlayerSlot(i, -i - 1, difficulty) { TeamIndex = teamIndex };
                    _playerSlots[i] = slot;
                    Debug.Log($"[MockLobbyService] Added CPU player to slot {i}");
                    PlayerSlotChanged?.Invoke(i, slot);
                    return true;
                }
            }

            Debug.LogWarning("[MockLobbyService] No empty slots available");
            return false;
        }

        /// <inheritdoc />
        public bool RemoveCPUPlayer(int slotIndex)
        {
            if (!_isHost)
            {
                Debug.LogWarning("[MockLobbyService] Only host can remove CPU players");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                return false;
            }

            var emptySlot = new PlayerSlot(slotIndex);
            _playerSlots[slotIndex] = emptySlot;
            Debug.Log($"[MockLobbyService] Removed CPU from slot {slotIndex}");
            PlayerSlotChanged?.Invoke(slotIndex, emptySlot);
            return true;
        }

        /// <inheritdoc />
        public bool ChangeCPUDifficulty(int slotIndex, AIDifficulty difficulty)
        {
            if (!_isHost || slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                return false;
            }

            var slot = _playerSlots[slotIndex];
            slot.AIDifficulty = difficulty;
            _playerSlots[slotIndex] = slot;
            Debug.Log($"[MockLobbyService] Changed CPU difficulty at slot {slotIndex} to {difficulty}");
            PlayerSlotChanged?.Invoke(slotIndex, slot);
            return true;
        }

        /// <inheritdoc />
        public bool SetPlayerTeam(ulong playerId, int teamIndex)
        {
            // ホストは全員のチームを変更可能、非ホストは自分のみ変更可能
            bool isLocalPlayer = _localPlayerInfo != null && _localPlayerInfo.PlayerId == playerId;
            if (!_isHost && !isLocalPlayer)
            {
                Debug.LogWarning($"[MockLobbyService] SetPlayerTeam denied: IsHost={_isHost}, IsLocalPlayer={isLocalPlayer}, playerId={playerId}");
                return false;
            }

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == playerId)
                {
                    var slot = _playerSlots[i];
                    slot.TeamIndex = teamIndex;
                    _playerSlots[i] = slot;
                    Debug.Log($"[MockLobbyService] Set player {playerId} to team {teamIndex}");
                    PlayerSlotChanged?.Invoke(i, slot);
                    return true;
                }
            }

            Debug.LogWarning($"[MockLobbyService] SetPlayerTeam: Player {playerId} not found in slots");
            return false;
        }

        /// <inheritdoc />
        public bool KickPlayer(ulong playerId)
        {
            if (!_isHost)
            {
                return false;
            }

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == playerId)
                {
                    var emptySlot = new PlayerSlot(i);
                    _playerSlots[i] = emptySlot;
                    Debug.Log($"[MockLobbyService] Kicked player {playerId}");
                    PlayerLeft?.Invoke(playerId);
                    PlayerSlotChanged?.Invoke(i, emptySlot);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool StartMatch()
        {
            if (!_isHost)
            {
                Debug.LogWarning("[MockLobbyService] Only host can start match");
                return false;
            }

            Debug.Log("[MockLobbyService] Starting match...");
            MatchStarting?.Invoke();
            return true;
        }

        /// <inheritdoc />
        public bool StartGameCountdown(int initialSeconds)
        {
            if (!_isHost)
            {
                return false;
            }

            Debug.Log($"[MockLobbyService] Starting countdown: {initialSeconds} seconds");
            CountdownStarted?.Invoke(initialSeconds);
            return true;
        }

        /// <inheritdoc />
        public bool CancelGameCountdown()
        {
            if (!_isHost)
            {
                return false;
            }

            Debug.Log("[MockLobbyService] Countdown cancelled");
            CountdownCancelled?.Invoke();
            return true;
        }

        #endregion

        #region Guest Methods

        /// <inheritdoc />
        public bool JoinRoom(string joinCode, string playerName, string password = "")
        {
            Debug.Log($"[MockLobbyService] JoinRoom called: JoinCode={joinCode}, Player={playerName}");

            _isHost = false;
            _isInRoom = true;
            _currentJoinCode = joinCode;

            // ローカルプレイヤーを追加
            _localPlayerInfo = new LobbyPlayerInfo(1, playerName, false) { IsLocalPlayer = true };

            // 空きスロットに追加
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].IsEmpty())
                {
                    _playerSlots[i] = new PlayerSlot(i, 1, playerName);
                    break;
                }
            }

            RoomJoined?.Invoke();
            return true;
        }

        #endregion

        #region Common Methods

        /// <inheritdoc />
        public void LeaveRoom()
        {
            Debug.Log("[MockLobbyService] LeaveRoom called");
            _isInRoom = false;
            _isHost = false;
            _currentJoinCode = null;

            // スロットをクリア
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                _playerSlots[i] = new PlayerSlot(i);
            }

            RoomLeft?.Invoke();
        }

        /// <inheritdoc />
        public void SetReady(bool isReady)
        {
            if (_localPlayerInfo == null) return;

            var playerId = _localPlayerInfo.PlayerId;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == playerId)
                {
                    var slot = _playerSlots[i];
                    slot.IsReady = isReady;
                    _playerSlots[i] = slot;
                    Debug.Log($"[MockLobbyService] SetReady: {isReady}");
                    PlayerReadyChanged?.Invoke(playerId, isReady);
                    PlayerSlotChanged?.Invoke(i, slot);
                    return;
                }
            }
        }

        /// <inheritdoc />
        public void SetCustomizationPreset(string presetName)
        {
            if (_localPlayerInfo == null) return;

            _localPlayerInfo.CustomizationPresetName = presetName;
            Debug.Log($"[MockLobbyService] SetCustomizationPreset: {presetName}");
        }

        /// <inheritdoc />
        public void SetPlayerName(string playerName)
        {
            if (_localPlayerInfo == null) return;

            _localPlayerInfo.PlayerName = playerName;
            Debug.Log($"[MockLobbyService] SetPlayerName: {playerName}");
        }

        /// <inheritdoc />
        public PlayerSlot? GetPlayerSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                return null;
            }

            return _playerSlots[slotIndex];
        }

        /// <inheritdoc />
        public int GetSlotIndexByPlayerId(ulong playerId)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <inheritdoc />
        public void RefreshAvailableRooms()
        {
            Debug.Log("[MockLobbyService] RefreshAvailableRooms called");
            AvailableRoomsUpdated?.Invoke(_availableRooms);
        }

        /// <inheritdoc />
        public void SavePlayerName(string playerName)
        {
            PlayerPrefs.SetString("MockPlayerName", playerName);
            Debug.Log($"[MockLobbyService] SavePlayerName: {playerName}");
        }

        /// <inheritdoc />
        public string? LoadPlayerName()
        {
            if (PlayerPrefs.HasKey("MockPlayerName"))
            {
                return PlayerPrefs.GetString("MockPlayerName");
            }

            return null;
        }

        #endregion

        #region Mock Event Triggers

        /// <summary>
        /// PlayerJoinedイベントを発火します（テスト用）
        /// </summary>
        /// <param name="playerId">参加したプレイヤーID</param>
        public void TriggerPlayerJoined(ulong playerId)
        {
            PlayerJoined?.Invoke(playerId);
        }

        /// <summary>
        /// CountdownUpdatedイベントを発火します（テスト用）
        /// </summary>
        /// <param name="secondsRemaining">残り秒数</param>
        public void TriggerCountdownUpdated(int secondsRemaining)
        {
            CountdownUpdated?.Invoke(secondsRemaining);
        }

        /// <summary>
        /// ErrorOccurredイベントを発火します（テスト用）
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ</param>
        public void TriggerErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(errorMessage);
        }

        /// <summary>
        /// HostDisconnectedイベントを発火します（テスト用）
        /// </summary>
        public void TriggerHostDisconnected()
        {
            HostDisconnected?.Invoke();
        }

        #endregion
    }
}
