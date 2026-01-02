#nullable enable

using System;
using System.Collections.Generic;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチ結果データ
    /// </summary>
    /// <remarks>
    /// マッチ終了後に生成され、リザルト画面で表示されるデータです。
    /// スコア、統計情報、リプレイデータへの参照を含みます。
    /// </remarks>
    [Serializable]
    public class MatchResult
    {
        #region Match Info

        /// <summary>
        /// マッチのユニークID
        /// </summary>
        public string MatchId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName { get; set; } = string.Empty;

        /// <summary>
        /// ゲームモード（Arena, ScoreMatch, TeamFight, Deathmatch, Versus）
        /// </summary>
        public string GameMode { get; set; } = string.Empty;

        /// <summary>
        /// マッチ持続時間（秒）
        /// </summary>
        public float MatchDuration { get; set; }

        /// <summary>
        /// マッチ終了日時
        /// </summary>
        public DateTime FinishedAt { get; set; } = DateTime.Now;

        #endregion

        #region Game Settings

        /// <summary>
        /// 最大プレイヤー数
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// 制限時間（秒）、0は無制限
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// 矢の制限数、0は無制限
        /// </summary>
        public int ArrowLimit { get; set; }

        /// <summary>
        /// チーム戦かどうか
        /// </summary>
        public bool IsTeamMatch { get; set; }

        /// <summary>
        /// チームAの名前（チーム戦の場合）
        /// </summary>
        public string TeamAName { get; set; } = "Team A";

        /// <summary>
        /// チームBの名前（チーム戦の場合）
        /// </summary>
        public string TeamBName { get; set; } = "Team B";

        /// <summary>
        /// 制限時間の表示文字列
        /// </summary>
        public string TimeLimitText => TimeLimit > 0 ? $"{TimeLimit / 60}:{TimeLimit % 60:D2}" : "Unlimited";

        /// <summary>
        /// 矢制限の表示文字列
        /// </summary>
        public string ArrowLimitText => ArrowLimit > 0 ? ArrowLimit.ToString() : "Unlimited";

        #endregion

        #region Scores

        /// <summary>
        /// プレイヤー/チームAのスコア
        /// </summary>
        public int PlayerScore { get; set; }

        /// <summary>
        /// 敵/チームBのスコア
        /// </summary>
        public int EnemyScore { get; set; }

        /// <summary>
        /// 勝利したかどうか
        /// </summary>
        public bool IsVictory => PlayerScore > EnemyScore;

        /// <summary>
        /// 引き分けかどうか
        /// </summary>
        public bool IsDraw => PlayerScore == EnemyScore;

        /// <summary>
        /// 敗北したかどうか
        /// </summary>
        public bool IsDefeat => PlayerScore < EnemyScore;

        /// <summary>
        /// 結果テキスト（VICTORY/DEFEAT/DRAW）
        /// </summary>
        public string ResultText => IsDraw ? "DRAW" : (IsVictory ? "VICTORY" : "DEFEAT");

        /// <summary>
        /// スコア表示テキスト
        /// </summary>
        public string ScoreText => $"{PlayerScore} - {EnemyScore}";

        /// <summary>
        /// 持続時間の表示文字列（分:秒）
        /// </summary>
        public string DurationText
        {
            get
            {
                int minutes = (int)(MatchDuration / 60f);
                int seconds = (int)(MatchDuration % 60f);
                return $"{minutes}:{seconds:D2}";
            }
        }

        #endregion

        #region Player Statistics

        /// <summary>
        /// ローカルプレイヤーの統計
        /// </summary>
        public PlayerStatistics LocalPlayerStats { get; set; } = new PlayerStatistics();

        /// <summary>
        /// 全プレイヤーの統計リスト
        /// </summary>
        public List<PlayerStatistics> AllPlayerStats { get; set; } = new List<PlayerStatistics>();

        #endregion

        #region Replay

        /// <summary>
        /// リプレイデータがあるかどうか
        /// </summary>
        public bool HasReplayData { get; set; }

        /// <summary>
        /// 関連するリプレイID（保存済みの場合）
        /// </summary>
        public string? SavedReplayId { get; set; }

        #endregion

        #region Room/Lobby State

        /// <summary>
        /// 元のルームID（マルチプレイヤーの場合）
        /// </summary>
        public string? OriginalRoomId { get; set; }

        /// <summary>
        /// ルームがまだ存在するかどうか
        /// </summary>
        public bool IsRoomStillOpen { get; set; }

        /// <summary>
        /// マルチプレイヤーマッチかどうか
        /// </summary>
        public bool IsMultiplayerMatch { get; set; }

        /// <summary>
        /// 現在のプレイヤー数
        /// </summary>
        public int CurrentPlayerCount { get; set; }

        /// <summary>
        /// ルームに戻れるかどうか
        /// </summary>
        public bool CanReturnToRoom => IsMultiplayerMatch && IsRoomStillOpen;

        #endregion

        #region Constructor

        /// <summary>
        /// MatchResultの新しいインスタンスを初期化します
        /// </summary>
        public MatchResult()
        {
        }

        /// <summary>
        /// MatchResultの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="duration">持続時間（秒）</param>
        /// <param name="playerScore">プレイヤースコア</param>
        /// <param name="enemyScore">敵スコア</param>
        public MatchResult(string mapName, string gameMode, float duration, int playerScore, int enemyScore)
        {
            MapName = mapName;
            GameMode = gameMode;
            MatchDuration = duration;
            PlayerScore = playerScore;
            EnemyScore = enemyScore;
            FinishedAt = DateTime.Now;
        }

        #endregion
    }
}
