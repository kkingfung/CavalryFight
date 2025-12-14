#nullable enable

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイイベントのタイプ
    /// </summary>
    public enum ReplayEventType
    {
        /// <summary>
        /// 不明
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// マッチ開始
        /// </summary>
        MatchStart = 1,

        /// <summary>
        /// マッチ終了
        /// </summary>
        MatchEnd = 2,

        /// <summary>
        /// スコア獲得
        /// </summary>
        Score = 10,

        /// <summary>
        /// エンティティ死亡
        /// </summary>
        Death = 20,

        /// <summary>
        /// 攻撃ヒット
        /// </summary>
        AttackHit = 30,

        /// <summary>
        /// チャージ攻撃発動
        /// </summary>
        ChargedAttack = 31,

        /// <summary>
        /// プレイヤースポーン
        /// </summary>
        PlayerSpawn = 40,

        /// <summary>
        /// 敵スポーン
        /// </summary>
        EnemySpawn = 41,

        /// <summary>
        /// カスタムイベント
        /// </summary>
        Custom = 100
    }
}
