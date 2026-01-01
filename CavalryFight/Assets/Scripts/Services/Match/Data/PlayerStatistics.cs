#nullable enable

using System;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// プレイヤー統計データ
    /// </summary>
    /// <remarks>
    /// マッチ中のプレイヤーのパフォーマンス統計を保持します。
    /// リザルト画面での表示やリプレイメタデータに使用されます。
    /// </remarks>
    [Serializable]
    public class PlayerStatistics
    {
        #region Identity

        /// <summary>
        /// プレイヤーID
        /// </summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// チーム（TeamA, TeamB, None）
        /// </summary>
        public string Team { get; set; } = "None";

        /// <summary>
        /// NPCかどうか
        /// </summary>
        public bool IsNPC { get; set; }

        /// <summary>
        /// ローカルプレイヤーかどうか
        /// </summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>
        /// 生存中かどうか
        /// </summary>
        public bool IsAlive { get; set; } = true;

        /// <summary>
        /// ランク順位（1位から）
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// ホストかどうか
        /// </summary>
        public bool IsHost { get; set; }

        /// <summary>
        /// ルームを退出したかどうか
        /// </summary>
        public bool HasLeft { get; set; }

        /// <summary>
        /// リマッチに投票したかどうか
        /// </summary>
        public bool HasVotedRematch { get; set; }

        #endregion

        #region Combat Stats

        /// <summary>
        /// スコア
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 発射した矢の数
        /// </summary>
        public int ArrowsFired { get; set; }

        /// <summary>
        /// 命中した回数
        /// </summary>
        public int Hits { get; set; }

        /// <summary>
        /// 命中率（%）
        /// </summary>
        public float Accuracy => ArrowsFired > 0 ? (float)Hits / ArrowsFired * 100f : 0f;

        /// <summary>
        /// 命中率テキスト
        /// </summary>
        public string AccuracyText => $"{Accuracy:F1}%";

        /// <summary>
        /// キル数
        /// </summary>
        public int Kills { get; set; }

        /// <summary>
        /// デス数
        /// </summary>
        public int Deaths { get; set; }

        /// <summary>
        /// K/D比
        /// </summary>
        public float KDRatio => Deaths > 0 ? (float)Kills / Deaths : Kills;

        /// <summary>
        /// K/D比テキスト
        /// </summary>
        public string KDRatioText => $"{KDRatio:F2}";

        #endregion

        #region Additional Stats

        /// <summary>
        /// アシスト数
        /// </summary>
        public int Assists { get; set; }

        /// <summary>
        /// ヘッドショット数
        /// </summary>
        public int Headshots { get; set; }

        /// <summary>
        /// 最長キルストリーク
        /// </summary>
        public int LongestKillStreak { get; set; }

        /// <summary>
        /// 与えたダメージ総量
        /// </summary>
        public float DamageDealt { get; set; }

        /// <summary>
        /// 受けたダメージ総量
        /// </summary>
        public float DamageTaken { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// PlayerStatisticsの新しいインスタンスを初期化します
        /// </summary>
        public PlayerStatistics()
        {
        }

        /// <summary>
        /// PlayerStatisticsの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        public PlayerStatistics(string playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 矢の発射を記録します
        /// </summary>
        public void RecordArrowFired()
        {
            ArrowsFired++;
        }

        /// <summary>
        /// ヒットを記録します
        /// </summary>
        /// <param name="isHeadshot">ヘッドショットかどうか</param>
        public void RecordHit(bool isHeadshot = false)
        {
            Hits++;
            if (isHeadshot)
            {
                Headshots++;
            }
        }

        /// <summary>
        /// キルを記録します
        /// </summary>
        /// <param name="scoreValue">得点</param>
        public void RecordKill(int scoreValue = 1)
        {
            Kills++;
            Score += scoreValue;
        }

        /// <summary>
        /// デスを記録します
        /// </summary>
        public void RecordDeath()
        {
            Deaths++;
        }

        /// <summary>
        /// ダメージを記録します
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="isDealt">与えたダメージか（falseの場合は受けたダメージ）</param>
        public void RecordDamage(float damage, bool isDealt)
        {
            if (isDealt)
            {
                DamageDealt += damage;
            }
            else
            {
                DamageTaken += damage;
            }
        }

        /// <summary>
        /// 統計をリセットします
        /// </summary>
        public void Reset()
        {
            Score = 0;
            ArrowsFired = 0;
            Hits = 0;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            Headshots = 0;
            LongestKillStreak = 0;
            DamageDealt = 0;
            DamageTaken = 0;
        }

        #endregion
    }
}
