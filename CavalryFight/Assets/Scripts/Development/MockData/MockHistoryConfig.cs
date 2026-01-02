#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Development.MockData
{
    /// <summary>
    /// モック履歴設定
    /// </summary>
    /// <remarks>
    /// 履歴画面のテスト用モックデータを提供します。
    /// 過去のマッチ履歴とリプレイ一覧を表示するためのモックデータです。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockHistory", menuName = "CavalryFight/Mock/History Config")]
    public class MockHistoryConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Match History")]
        /// <summary>マッチ履歴一覧</summary>
        [SerializeField] private List<MockHistoryEntry> _matchHistory = new List<MockHistoryEntry>
        {
            // 勝利 - Arena
            new MockHistoryEntry
            {
                MatchId = "match-001",
                GameMode = "Arena",
                MapName = "Arena",
                MatchDate = "2025-12-30 14:30",
                MatchDuration = 300f,
                IsVictory = true,
                PlayerScore = 1850,
                EnemyScore = 1200,
                Rank = 1,
                TotalPlayers = 4,
                Kills = 12,
                Deaths = 3,
                Assists = 2,
                Headshots = 5,
                Accuracy = 72.5f,
                HasReplay = true,
                ReplayId = "replay-001"
            },
            // 敗北 - TeamFight
            new MockHistoryEntry
            {
                MatchId = "match-002",
                GameMode = "TeamFight",
                MapName = "Castle",
                MatchDate = "2025-12-29 20:15",
                MatchDuration = 420f,
                IsVictory = false,
                PlayerScore = 1100,
                EnemyScore = 1500,
                Rank = 2,
                TotalPlayers = 6,
                Kills = 8,
                Deaths = 6,
                Assists = 4,
                Headshots = 2,
                Accuracy = 58.3f,
                HasReplay = true,
                ReplayId = "replay-002"
            },
            // 引き分け - ScoreMatch
            new MockHistoryEntry
            {
                MatchId = "match-003",
                GameMode = "ScoreMatch",
                MapName = "Arena",
                MatchDate = "2025-12-28 18:00",
                MatchDuration = 300f,
                IsVictory = false, // IsDraw判定はスコアで行う
                PlayerScore = 1500,
                EnemyScore = 1500,
                Rank = 1,
                TotalPlayers = 2,
                Kills = 10,
                Deaths = 10,
                Assists = 0,
                Headshots = 4,
                Accuracy = 62.0f,
                HasReplay = true,
                ReplayId = "replay-003"
            },
            // 勝利 - Hunting
            new MockHistoryEntry
            {
                MatchId = "match-004",
                GameMode = "Hunting",
                MapName = "Forest",
                MatchDate = "2025-12-27 18:45",
                MatchDuration = 360f,
                IsVictory = true,
                PlayerScore = 2200,
                EnemyScore = 1800,
                Rank = 1,
                TotalPlayers = 8,
                Kills = 15,
                Deaths = 2,
                Assists = 3,
                Headshots = 8,
                Accuracy = 80.0f,
                HasReplay = true,
                ReplayId = "replay-004"
            },
            // 敗北 - Deathmatch (大差)
            new MockHistoryEntry
            {
                MatchId = "match-005",
                GameMode = "Deathmatch",
                MapName = "Desert",
                MatchDate = "2025-12-26 12:00",
                MatchDuration = 180f,
                IsVictory = false,
                PlayerScore = 450,
                EnemyScore = 1200,
                Rank = 4,
                TotalPlayers = 4,
                Kills = 3,
                Deaths = 12,
                Assists = 1,
                Headshots = 0,
                Accuracy = 35.0f,
                HasReplay = true,
                ReplayId = "replay-005"
            },
            // 勝利 - Arena (8人戦)
            new MockHistoryEntry
            {
                MatchId = "match-006",
                GameMode = "Arena",
                MapName = "Castle",
                MatchDate = "2025-12-25 21:30",
                MatchDuration = 480f,
                IsVictory = true,
                PlayerScore = 2500,
                EnemyScore = 2100,
                Rank = 1,
                TotalPlayers = 8,
                Kills = 18,
                Deaths = 5,
                Assists = 6,
                Headshots = 9,
                Accuracy = 68.5f,
                HasReplay = true,
                ReplayId = "replay-006"
            },
            // 敗北 - ScoreMatch (僅差)
            new MockHistoryEntry
            {
                MatchId = "match-007",
                GameMode = "ScoreMatch",
                MapName = "Forest",
                MatchDate = "2025-12-24 16:30",
                MatchDuration = 300f,
                IsVictory = false,
                PlayerScore = 1480,
                EnemyScore = 1500,
                Rank = 2,
                TotalPlayers = 2,
                Kills = 9,
                Deaths = 10,
                Assists = 0,
                Headshots = 3,
                Accuracy = 55.0f,
                HasReplay = true,
                ReplayId = "replay-007"
            },
            // 引き分け - TeamFight
            new MockHistoryEntry
            {
                MatchId = "match-008",
                GameMode = "TeamFight",
                MapName = "Desert",
                MatchDate = "2025-12-23 19:00",
                MatchDuration = 360f,
                IsVictory = false, // IsDraw判定はスコアで行う
                PlayerScore = 1800,
                EnemyScore = 1800,
                Rank = 1,
                TotalPlayers = 6,
                Kills = 11,
                Deaths = 11,
                Assists = 5,
                Headshots = 4,
                Accuracy = 59.0f,
                HasReplay = false,
                ReplayId = ""
            }
        };

        #endregion

        #region Properties

        /// <summary>
        /// マッチ履歴一覧
        /// </summary>
        public IReadOnlyList<MockHistoryEntry> MatchHistory => _matchHistory;

        #endregion
    }

    /// <summary>
    /// モック履歴エントリ
    /// </summary>
    [Serializable]
    public class MockHistoryEntry
    {
        [Header("Match Info")]
        /// <summary>マッチID</summary>
        public string MatchId = "match-001";

        /// <summary>ゲームモード</summary>
        public string GameMode = "Arena";

        /// <summary>マップ名</summary>
        public string MapName = "Arena";

        /// <summary>マッチ日時（表示用文字列）</summary>
        public string MatchDate = "2025-01-01 12:00";

        /// <summary>マッチ時間（秒）</summary>
        public float MatchDuration = 300f;

        [Header("Result")]
        /// <summary>勝利したかどうか</summary>
        public bool IsVictory = true;

        /// <summary>プレイヤースコア</summary>
        public int PlayerScore = 1500;

        /// <summary>敵スコア</summary>
        public int EnemyScore = 1200;

        /// <summary>順位（FFAの場合）</summary>
        public int Rank = 1;

        /// <summary>総プレイヤー数（FFAの場合）</summary>
        public int TotalPlayers = 8;

        [Header("Player Stats")]
        /// <summary>キル数</summary>
        public int Kills = 10;

        /// <summary>デス数</summary>
        public int Deaths = 3;

        /// <summary>アシスト数</summary>
        public int Assists = 5;

        /// <summary>ヘッドショット数</summary>
        public int Headshots = 4;

        /// <summary>命中率（%）</summary>
        public float Accuracy = 65.5f;

        [Header("Replay")]
        /// <summary>リプレイが利用可能かどうか</summary>
        public bool HasReplay = true;

        /// <summary>リプレイID</summary>
        public string ReplayId = "replay-001";

        [Header("Other Players")]
        /// <summary>他のプレイヤーの統計情報</summary>
        public List<MockOtherPlayerStats> OtherPlayers = new List<MockOtherPlayerStats>();
    }

    /// <summary>
    /// 他プレイヤーの統計情報
    /// </summary>
    [Serializable]
    public class MockOtherPlayerStats
    {
        /// <summary>プレイヤー名</summary>
        public string PlayerName = "Player";

        /// <summary>キル数</summary>
        public int Kills = 5;

        /// <summary>デス数</summary>
        public int Deaths = 5;

        /// <summary>スコア</summary>
        public int Score = 1000;

        /// <summary>順位</summary>
        public int Rank = 2;
    }
}
