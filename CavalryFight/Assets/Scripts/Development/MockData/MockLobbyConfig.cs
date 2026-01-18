#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Development.MockData
{
    /// <summary>
    /// モックロビー設定
    /// </summary>
    /// <remarks>
    /// ロビー画面のテスト用モックデータを提供します。
    /// 利用可能なルーム一覧を表示するためのモックデータです。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockLobby", menuName = "CavalryFight/Mock/Lobby Config")]
    public class MockLobbyConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Player Info")]
        /// <summary>ローカルプレイヤー名</summary>
        [SerializeField] private string _localPlayerName = "TestPlayer";

        [Header("Available Rooms")]
        /// <summary>利用可能なルーム一覧（スクロールテスト用に多数配置）</summary>
        [SerializeField] private List<MockRoomInfo> _availableRooms = new List<MockRoomInfo>
        {
            new MockRoomInfo
            {
                RoomId = "room-001",
                RoomName = "Beginners Welcome!",
                JoinCode = "ABC123",
                HostName = "Archer_Master",
                GameMode = "Arena",
                MapName = "Arena",
                CurrentPlayers = 3,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-002",
                RoomName = "Pro Players Only",
                JoinCode = "XYZ789",
                HostName = "HorseKnight99",
                GameMode = "Deathmatch",
                MapName = "Arena",
                CurrentPlayers = 6,
                MaxPlayers = 8,
                HasPassword = true,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-003",
                RoomName = "Team Practice",
                JoinCode = "TEAM01",
                HostName = "CaptainBow",
                GameMode = "TeamFight",
                MapName = "Forest",
                CurrentPlayers = 4,
                MaxPlayers = 6,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-004",
                RoomName = "Hunt the Wolves",
                JoinCode = "HUNT99",
                HostName = "WolfHunter",
                GameMode = "Hunting",
                MapName = "Forest",
                CurrentPlayers = 5,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-005",
                RoomName = "Quick Match",
                JoinCode = "QUICK1",
                HostName = "SpeedyArrow",
                GameMode = "ScoreMatch",
                MapName = "Nature",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-006",
                RoomName = "Epic Arena Battle",
                JoinCode = "ARENA1",
                HostName = "Knight_Alex",
                GameMode = "Arena",
                MapName = "PlayGround",
                CurrentPlayers = 3,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-007",
                RoomName = "Tournament Finals",
                JoinCode = "TOUR01",
                HostName = "Champion_Max",
                GameMode = "Arena",
                MapName = "Nature",
                CurrentPlayers = 8,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-008",
                RoomName = "Night Hunters",
                JoinCode = "NIGHT1",
                HostName = "Shadow_Jin",
                GameMode = "Hunting",
                MapName = "Forest",
                CurrentPlayers = 3,
                MaxPlayers = 4,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-009",
                RoomName = "Clan Battle Room",
                JoinCode = "CLAN01",
                HostName = "Leader_Hana",
                GameMode = "TeamFight",
                MapName = "PlayGround",
                CurrentPlayers = 8,
                MaxPlayers = 8,
                HasPassword = true,
                IsPublic = false
            },
            new MockRoomInfo
            {
                RoomId = "room-010",
                RoomName = "Score Attack",
                JoinCode = "SCORE1",
                HostName = "Scorer_Mai",
                GameMode = "ScoreMatch",
                MapName = "Nature",
                CurrentPlayers = 5,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-011",
                RoomName = "Weekend Warriors",
                JoinCode = "WEEK01",
                HostName = "Casual_Yui",
                GameMode = "Arena",
                MapName = "Forest",
                CurrentPlayers = 4,
                MaxPlayers = 6,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-012",
                RoomName = "Hunting Party",
                JoinCode = "PARTY1",
                HostName = "Pack_Kota",
                GameMode = "Hunting",
                MapName = "Nature",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-013",
                RoomName = "Championship Room",
                JoinCode = "CHAMP1",
                HostName = "Champ_Ryu",
                GameMode = "Arena",
                MapName = "PlayGround",
                CurrentPlayers = 6,
                MaxPlayers = 8,
                HasPassword = true,
                IsPublic = false
            },
            new MockRoomInfo
            {
                RoomId = "room-014",
                RoomName = "Practice Grounds",
                JoinCode = "PRAC01",
                HostName = "Coach_Mei",
                GameMode = "Arena",
                MapName = "TrainingRoom",
                CurrentPlayers = 1,
                MaxPlayers = 4,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-015",
                RoomName = "Late Night Battles",
                JoinCode = "LATE01",
                HostName = "Owl_Shin",
                GameMode = "Arena",
                MapName = "Nature",
                CurrentPlayers = 3,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-016",
                RoomName = "Friends Only",
                JoinCode = "FRND01",
                HostName = "Social_Aoi",
                GameMode = "TeamFight",
                MapName = "Forest",
                CurrentPlayers = 4,
                MaxPlayers = 8,
                HasPassword = true,
                IsPublic = false
            },
            new MockRoomInfo
            {
                RoomId = "room-017",
                RoomName = "Veteran Clash",
                JoinCode = "VET001",
                HostName = "Old_Guard",
                GameMode = "Arena",
                MapName = "PlayGround",
                CurrentPlayers = 7,
                MaxPlayers = 8,
                HasPassword = false,
                IsPublic = true
            },
            new MockRoomInfo
            {
                RoomId = "room-018",
                RoomName = "New Player Training",
                JoinCode = "TRAIN1",
                HostName = "Teacher_Ken",
                GameMode = "Arena",
                MapName = "TrainingRoom",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                HasPassword = false,
                IsPublic = true
            }
        };

        #endregion

        #region Properties

        /// <summary>
        /// ローカルプレイヤー名
        /// </summary>
        public string LocalPlayerName => _localPlayerName;

        /// <summary>
        /// 利用可能なルーム一覧
        /// </summary>
        public IReadOnlyList<MockRoomInfo> AvailableRooms => _availableRooms;

        #endregion
    }

    /// <summary>
    /// モックルーム情報
    /// </summary>
    [Serializable]
    public class MockRoomInfo
    {
        [Header("Room Identity")]
        /// <summary>ルームID</summary>
        public string RoomId = "room-001";

        /// <summary>ルーム名</summary>
        public string RoomName = "Test Room";

        /// <summary>参加コード</summary>
        public string JoinCode = "ABC123";

        [Header("Room Settings")]
        /// <summary>ホスト名</summary>
        public string HostName = "HostPlayer";

        /// <summary>ゲームモード</summary>
        public string GameMode = "Arena";

        /// <summary>マップ名</summary>
        public string MapName = "Arena";

        /// <summary>現在のプレイヤー数</summary>
        public int CurrentPlayers = 2;

        /// <summary>最大プレイヤー数</summary>
        public int MaxPlayers = 8;

        /// <summary>パスワード保護されているか</summary>
        public bool HasPassword = false;

        /// <summary>公開ルームかどうか</summary>
        public bool IsPublic = true;
    }
}
