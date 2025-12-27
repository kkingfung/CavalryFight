#nullable enable

using System;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイメタデータ（リスト表示用の軽量データ）
    /// </summary>
    /// <remarks>
    /// リプレイリストで表示するための基本情報のみを含みます。
    /// 完全なリプレイデータは必要に応じて ReplayData から読み込みます。
    /// </remarks>
    [Serializable]
    public class ReplayMetadata
    {
        #region Properties

        /// <summary>
        /// リプレイのユニークID
        /// </summary>
        public string ReplayId { get; set; } = string.Empty;

        /// <summary>
        /// リプレイ作成日時
        /// </summary>
        public DateTime RecordedAt { get; set; }

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName { get; set; } = string.Empty;

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode { get; set; } = string.Empty;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// マッチの持続時間（秒）
        /// </summary>
        public float MatchDuration { get; set; }

        /// <summary>
        /// 最終プレイヤースコア
        /// </summary>
        public int FinalPlayerScore { get; set; }

        /// <summary>
        /// 最終敵スコア
        /// </summary>
        public int FinalEnemyScore { get; set; }

        /// <summary>
        /// 勝敗結果（プレイヤーが勝った場合true）
        /// </summary>
        public bool IsPlayerVictory => FinalPlayerScore > FinalEnemyScore;

        /// <summary>
        /// 引き分けかどうか
        /// </summary>
        public bool IsDraw => FinalPlayerScore == FinalEnemyScore;

        /// <summary>
        /// 結果の表示文字列
        /// </summary>
        public string ResultText
        {
            get
            {
                if (IsDraw) return "Draw";
                return IsPlayerVictory ? "Victory" : "Defeat";
            }
        }

        /// <summary>
        /// スコア表示文字列
        /// </summary>
        public string ScoreText => $"{FinalPlayerScore} - {FinalEnemyScore}";

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

        /// <summary>
        /// 日時の表示文字列
        /// </summary>
        public string DateText => RecordedAt.ToString("yyyy/MM/dd HH:mm");

        #endregion

        #region Constructor

        /// <summary>
        /// ReplayMetadataの新しいインスタンスを初期化します
        /// </summary>
        public ReplayMetadata()
        {
        }

        /// <summary>
        /// ReplayDataからメタデータを作成します
        /// </summary>
        /// <param name="replayData">リプレイデータ</param>
        /// <returns>作成されたメタデータ</returns>
        public static ReplayMetadata FromReplayData(ReplayData replayData)
        {
            return new ReplayMetadata
            {
                ReplayId = replayData.ReplayId,
                RecordedAt = DateTime.Parse(replayData.RecordedAt),
                MapName = replayData.MapName,
                GameMode = replayData.GameMode,
                PlayerName = replayData.PlayerName,
                MatchDuration = replayData.MatchDuration,
                FinalPlayerScore = replayData.FinalPlayerScore,
                FinalEnemyScore = replayData.FinalEnemyScore
            };
        }

        #endregion
    }
}
