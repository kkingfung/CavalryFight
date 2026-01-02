#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Development.MockData
{
    /// <summary>
    /// モックルーム設定
    /// </summary>
    /// <remarks>
    /// ルーム画面のテスト用モックデータを提供します。
    /// ホストまたはゲストとしてルームに参加した状態をシミュレートします。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockRoom", menuName = "CavalryFight/Mock/Room Config")]
    public class MockRoomConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Local Player Settings")]
        /// <summary>ローカルプレイヤーがホストかどうか</summary>
        [SerializeField] private bool _isHost = false;

        /// <summary>ローカルプレイヤー名</summary>
        [SerializeField] private string _localPlayerName = "TestPlayer";

        /// <summary>ローカルプレイヤーが準備完了かどうか</summary>
        [SerializeField] private bool _isLocalPlayerReady = false;

        [Header("Room Identity")]
        /// <summary>ルームID</summary>
        [SerializeField] private string _roomId = "mock-room-001";

        /// <summary>ルーム名</summary>
        [SerializeField] private string _roomName = "Test Room";

        /// <summary>参加コード</summary>
        [SerializeField] private string _joinCode = "ABC123";

        [Header("Room Settings")]
        /// <summary>ゲームモード</summary>
        [SerializeField] private string _gameMode = "Arena";

        /// <summary>マップ名</summary>
        [SerializeField] private string _mapName = "Arena";

        /// <summary>最大プレイヤー数</summary>
        [SerializeField] private int _maxPlayers = 8;

        /// <summary>制限時間（秒）</summary>
        [SerializeField] private int _timeLimit = 300;

        /// <summary>矢の制限数</summary>
        [SerializeField] private int _arrowLimit = 0;

        /// <summary>パスワード（空の場合はなし）</summary>
        [SerializeField] private string _password = "";

        /// <summary>公開ルームかどうか</summary>
        [SerializeField] private bool _isPublic = true;

        [Header("Players in Room")]
        /// <summary>ルーム内のプレイヤー一覧</summary>
        [SerializeField] private List<MockRoomPlayer> _players = new List<MockRoomPlayer>
        {
            new MockRoomPlayer
            {
                PlayerId = "player-001",
                PlayerName = "HostPlayer",
                IsHost = true,
                IsReady = true,
                IsNPC = false,
                Team = "None",
                FPS = 60,
                Ping = 15
            },
            new MockRoomPlayer
            {
                PlayerId = "player-002",
                PlayerName = "GuestPlayer1",
                IsHost = false,
                IsReady = true,
                IsNPC = false,
                Team = "None",
                FPS = 55,
                Ping = 45
            },
            new MockRoomPlayer
            {
                PlayerId = "cpu-001",
                PlayerName = "CPU 1",
                IsHost = false,
                IsReady = true,
                IsNPC = true,
                NPCDifficulty = "Normal",
                Team = "None",
                FPS = 60,
                Ping = 0
            },
            new MockRoomPlayer
            {
                PlayerId = "cpu-002",
                PlayerName = "CPU 2",
                IsHost = false,
                IsReady = true,
                IsNPC = true,
                NPCDifficulty = "Hard",
                Team = "None",
                FPS = 60,
                Ping = 0
            }
        };

        #endregion

        #region Properties

        /// <summary>ローカルプレイヤーがホストかどうか</summary>
        public bool IsHost => _isHost;

        /// <summary>ローカルプレイヤー名</summary>
        public string LocalPlayerName => _localPlayerName;

        /// <summary>ローカルプレイヤーが準備完了かどうか</summary>
        public bool IsLocalPlayerReady => _isLocalPlayerReady;

        /// <summary>ルームID</summary>
        public string RoomId => _roomId;

        /// <summary>ルーム名</summary>
        public string RoomName => _roomName;

        /// <summary>参加コード</summary>
        public string JoinCode => _joinCode;

        /// <summary>ゲームモード</summary>
        public string GameMode => _gameMode;

        /// <summary>マップ名</summary>
        public string MapName => _mapName;

        /// <summary>最大プレイヤー数</summary>
        public int MaxPlayers => _maxPlayers;

        /// <summary>制限時間（秒）</summary>
        public int TimeLimit => _timeLimit;

        /// <summary>矢の制限数</summary>
        public int ArrowLimit => _arrowLimit;

        /// <summary>パスワード</summary>
        public string Password => _password;

        /// <summary>公開ルームかどうか</summary>
        public bool IsPublic => _isPublic;

        /// <summary>ルーム内のプレイヤー一覧</summary>
        public IReadOnlyList<MockRoomPlayer> Players => _players;

        #endregion
    }

    /// <summary>
    /// モックルームプレイヤー情報
    /// </summary>
    [Serializable]
    public class MockRoomPlayer
    {
        [Header("Player Identity")]
        /// <summary>プレイヤーID</summary>
        public string PlayerId = "player-001";

        /// <summary>プレイヤー名</summary>
        public string PlayerName = "Player";

        [Header("Player State")]
        /// <summary>ホストかどうか</summary>
        public bool IsHost = false;

        /// <summary>準備完了かどうか</summary>
        public bool IsReady = false;

        /// <summary>NPCかどうか</summary>
        public bool IsNPC = false;

        /// <summary>NPC難易度（NPCの場合）</summary>
        public string NPCDifficulty = "Normal";

        [Header("Team Settings")]
        /// <summary>チーム（None, TeamA, TeamB）</summary>
        public string Team = "None";

        [Header("Performance")]
        /// <summary>FPS（パフォーマンス表示用）</summary>
        public int FPS = 60;

        /// <summary>Ping（パフォーマンス表示用）</summary>
        public int Ping = 30;
    }
}
