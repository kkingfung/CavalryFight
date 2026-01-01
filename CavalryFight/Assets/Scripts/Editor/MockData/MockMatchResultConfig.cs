#nullable enable

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using CavalryFight.Services.Match;
using UnityEngine;

namespace CavalryFight.Editor.MockData
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
        [SerializeField] private int _maxPlayers = 4;

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
        [SerializeField] private int _currentPlayerCount = 4;

        #endregion

        #region Players

        [Header("Players")]
        /// <summary>プレイヤー設定のリスト</summary>
        [SerializeField] private List<MockPlayerConfig> _players = new List<MockPlayerConfig>();

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
                IsAlive = true,
                HasLeft = false,
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

#endif
