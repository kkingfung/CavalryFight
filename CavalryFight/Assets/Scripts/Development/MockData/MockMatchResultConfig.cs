#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Services.Match;
using UnityEngine;

namespace CavalryFight.Development.MockData
{
    /// <summary>
    /// モックのマッチ結果設定
    /// </summary>
    /// <remarks>
    /// エディタでシーンを直接開いた時にテスト用のマッチ結果を提供します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockMatchResult", menuName = "CavalryFight/Mock/Match Result")]
    public class MockMatchResultConfig : ScriptableObject
    {
        #region Match Info

        [Header("Match Info")]
        /// <summary>マップ名</summary>
        [SerializeField] private string _mapName = "Training Grounds";

        /// <summary>ゲームモード名</summary>
        [SerializeField] private string _gameMode = "Arena";

        /// <summary>マッチの持続時間（秒）</summary>
        [SerializeField] private float _matchDuration = 300f;

        #endregion

        #region Game Settings

        [Header("Game Settings")]
        /// <summary>最大プレイヤー数</summary>
        [SerializeField] private int _maxPlayers = 8;

        /// <summary>制限時間（秒）</summary>
        [SerializeField] private int _timeLimit = 300;

        /// <summary>矢の制限数</summary>
        [SerializeField] private int _arrowLimit = 30;

        /// <summary>チーム戦かどうか</summary>
        [SerializeField] private bool _isTeamMatch = false;

        /// <summary>チームAの名前</summary>
        [SerializeField] private string _teamAName = "Team A";

        /// <summary>チームBの名前</summary>
        [SerializeField] private string _teamBName = "Team B";

        #endregion

        #region Scores

        [Header("Scores")]
        /// <summary>プレイヤー（またはチームA）のスコア</summary>
        [SerializeField] private int _playerScore = 1500;

        /// <summary>敵（またはチームB）のスコア</summary>
        [SerializeField] private int _enemyScore = 1200;

        #endregion

        #region Replay

        [Header("Replay")]
        /// <summary>リプレイデータがあるかどうか</summary>
        [SerializeField] private bool _hasReplayData = true;

        #endregion

        #region Room State

        [Header("Room State")]
        /// <summary>マルチプレイヤーマッチかどうか</summary>
        [SerializeField] private bool _isMultiplayerMatch = false;

        /// <summary>ルームがまだ開いているかどうか</summary>
        [SerializeField] private bool _isRoomStillOpen = false;

        /// <summary>現在のプレイヤー数</summary>
        [SerializeField] private int _currentPlayerCount = 8;

        #endregion

        #region Players

        [Header("Players")]
        /// <summary>プレイヤー設定のリスト</summary>
        [SerializeField] private List<MockPlayerConfig> _players = new List<MockPlayerConfig>
        {
            new MockPlayerConfig
            {
                PlayerName = "You",
                Team = "Team A",
                IsNPC = false,
                IsLocalPlayer = true,
                IsHost = true,
                IsAlive = true,
                HasLeft = false,
                Rank = 1,
                Score = 1500,
                ArrowsFired = 45,
                Hits = 32,
                Kills = 12,
                Deaths = 3,
                Assists = 2,
                Headshots = 6,
                LongestKillStreak = 5,
                DamageDealt = 1850f,
                DamageTaken = 420f
            },
            new MockPlayerConfig
            {
                PlayerName = "Archer_Master",
                Team = "Team A",
                IsNPC = false,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = true,
                HasLeft = false,
                Rank = 2,
                Score = 1200,
                ArrowsFired = 52,
                Hits = 28,
                Kills = 9,
                Deaths = 5,
                Assists = 3,
                Headshots = 4,
                LongestKillStreak = 3,
                DamageDealt = 1420f,
                DamageTaken = 680f
            },
            new MockPlayerConfig
            {
                PlayerName = "CPU Knight",
                Team = "Team B",
                IsNPC = true,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = false,
                HasLeft = false,
                Rank = 3,
                Score = 850,
                ArrowsFired = 38,
                Hits = 18,
                Kills = 6,
                Deaths = 7,
                Assists = 1,
                Headshots = 2,
                LongestKillStreak = 2,
                DamageDealt = 920f,
                DamageTaken = 950f
            },
            new MockPlayerConfig
            {
                PlayerName = "CPU Archer",
                Team = "Team B",
                IsNPC = true,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = true,
                HasLeft = false,
                Rank = 4,
                Score = 650,
                ArrowsFired = 42,
                Hits = 14,
                Kills = 4,
                Deaths = 9,
                Assists = 2,
                Headshots = 1,
                LongestKillStreak = 2,
                DamageDealt = 680f,
                DamageTaken = 1150f
            },
            new MockPlayerConfig
            {
                PlayerName = "Swift_Rider",
                Team = "Team A",
                IsNPC = false,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = false,
                HasLeft = true,
                Rank = 5,
                Score = 580,
                ArrowsFired = 35,
                Hits = 12,
                Kills = 5,
                Deaths = 8,
                Assists = 1,
                Headshots = 2,
                LongestKillStreak = 2,
                DamageDealt = 620f,
                DamageTaken = 980f
            },
            new MockPlayerConfig
            {
                PlayerName = "Dark_Hunter",
                Team = "None",
                IsNPC = false,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = true,
                HasLeft = false,
                Rank = 6,
                Score = 450,
                ArrowsFired = 48,
                Hits = 10,
                Kills = 3,
                Deaths = 11,
                Assists = 3,
                Headshots = 0,
                LongestKillStreak = 1,
                DamageDealt = 480f,
                DamageTaken = 1280f
            },
            new MockPlayerConfig
            {
                PlayerName = "CPU Cavalry",
                Team = "Team B",
                IsNPC = true,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = false,
                HasLeft = false,
                Rank = 7,
                Score = 320,
                ArrowsFired = 28,
                Hits = 8,
                Kills = 2,
                Deaths = 12,
                Assists = 1,
                Headshots = 0,
                LongestKillStreak = 1,
                DamageDealt = 350f,
                DamageTaken = 1400f
            },
            new MockPlayerConfig
            {
                PlayerName = "Novice_Bow",
                Team = "None",
                IsNPC = false,
                IsLocalPlayer = false,
                IsHost = false,
                IsAlive = false,
                HasLeft = true,
                Rank = 8,
                Score = 180,
                ArrowsFired = 55,
                Hits = 5,
                Kills = 1,
                Deaths = 14,
                Assists = 0,
                Headshots = 0,
                LongestKillStreak = 1,
                DamageDealt = 210f,
                DamageTaken = 1650f
            }
        };

        #endregion

        #region Properties

        /// <summary>マップ名</summary>
        public string MapName => _mapName;

        /// <summary>ゲームモード</summary>
        public string GameMode => _gameMode;

        /// <summary>プレイヤースコア</summary>
        public int PlayerScore => _playerScore;

        /// <summary>敵スコア</summary>
        public int EnemyScore => _enemyScore;

        #endregion

        #region Methods

        /// <summary>
        /// MatchResultを生成します
        /// </summary>
        /// <returns>生成されたマッチ結果</returns>
        public MatchResult CreateMatchResult()
        {
            var result = new MatchResult
            {
                MatchId = Guid.NewGuid().ToString(),
                MapName = _mapName,
                GameMode = _gameMode,
                MatchDuration = _matchDuration,
                FinishedAt = DateTime.Now,
                MaxPlayers = _maxPlayers,
                TimeLimit = _timeLimit,
                ArrowLimit = _arrowLimit,
                IsTeamMatch = _isTeamMatch,
                TeamAName = _teamAName,
                TeamBName = _teamBName,
                PlayerScore = _playerScore,
                EnemyScore = _enemyScore,
                HasReplayData = _hasReplayData,
                IsMultiplayerMatch = _isMultiplayerMatch,
                IsRoomStillOpen = _isRoomStillOpen,
                CurrentPlayerCount = _currentPlayerCount
            };

            // プレイヤー統計を生成
            PlayerStatistics? localStats = null;
            foreach (var playerConfig in _players)
            {
                var stats = playerConfig.CreatePlayerStatistics();
                result.AllPlayerStats.Add(stats);

                if (stats.IsLocalPlayer)
                {
                    localStats = stats;
                }
            }

            // ローカルプレイヤーがいない場合はデフォルトを作成
            if (localStats == null && result.AllPlayerStats.Count > 0)
            {
                result.AllPlayerStats[0].IsLocalPlayer = true;
                localStats = result.AllPlayerStats[0];
            }

            if (localStats != null)
            {
                result.LocalPlayerStats = localStats;
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// モックプレイヤー設定
    /// </summary>
    /// <remarks>
    /// プレイヤーの統計情報を設定するためのシリアライズ可能なクラスです。
    /// </remarks>
    [Serializable]
    public class MockPlayerConfig
    {
        #region Identity

        [Header("Identity")]
        /// <summary>プレイヤー名</summary>
        public string PlayerName = "Player";

        /// <summary>所属チーム</summary>
        public string Team = "None";

        /// <summary>NPCかどうか</summary>
        public bool IsNPC = false;

        /// <summary>ローカルプレイヤーかどうか</summary>
        public bool IsLocalPlayer = false;

        /// <summary>ホストかどうか</summary>
        public bool IsHost = false;

        /// <summary>生存しているかどうか</summary>
        public bool IsAlive = true;

        /// <summary>退出したかどうか</summary>
        public bool HasLeft = false;

        /// <summary>順位</summary>
        public int Rank = 1;

        #endregion

        #region Combat Stats

        [Header("Combat Stats")]
        /// <summary>スコア</summary>
        public int Score = 100;

        /// <summary>発射した矢の数</summary>
        public int ArrowsFired = 30;

        /// <summary>命中数</summary>
        public int Hits = 15;

        /// <summary>キル数</summary>
        public int Kills = 5;

        /// <summary>デス数</summary>
        public int Deaths = 3;

        #endregion

        #region Additional Stats

        [Header("Additional Stats")]
        /// <summary>アシスト数</summary>
        public int Assists = 2;

        /// <summary>ヘッドショット数</summary>
        public int Headshots = 3;

        /// <summary>最長キルストリーク</summary>
        public int LongestKillStreak = 3;

        /// <summary>与えたダメージ量</summary>
        public float DamageDealt = 500f;

        /// <summary>受けたダメージ量</summary>
        public float DamageTaken = 300f;

        #endregion

        #region Methods

        /// <summary>
        /// PlayerStatisticsを生成します
        /// </summary>
        /// <returns>生成されたプレイヤー統計</returns>
        public PlayerStatistics CreatePlayerStatistics()
        {
            return new PlayerStatistics
            {
                PlayerId = Guid.NewGuid().ToString(),
                PlayerName = PlayerName,
                Team = Team,
                IsNPC = IsNPC,
                IsLocalPlayer = IsLocalPlayer,
                IsHost = IsHost,
                Rank = Rank,
                IsAlive = IsAlive,
                HasLeft = HasLeft,
                HasVotedRematch = false,
                Score = Score,
                ArrowsFired = ArrowsFired,
                Hits = Hits,
                Kills = Kills,
                Deaths = Deaths,
                Assists = Assists,
                Headshots = Headshots,
                LongestKillStreak = LongestKillStreak,
                DamageDealt = DamageDealt,
                DamageTaken = DamageTaken
            };
        }

        #endregion
    }
}
