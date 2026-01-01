#nullable enable

using System;
using CavalryFight.Services.Lobby;
using Unity.Netcode;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// ゲームモードごとのルール処理を定義するインターフェース
    /// </summary>
    /// <remarks>
    /// Strategyパターンを使用して、各ゲームモードの固有ロジックを分離します。
    /// MatchManagerは実行時に適切なハンドラーを選択して使用します。
    /// </remarks>
    public interface IGameModeRulesHandler
    {
        #region Properties

        /// <summary>
        /// このハンドラーが処理するゲームモード
        /// </summary>
        GameMode Mode { get; }

        /// <summary>
        /// チームモードかどうか
        /// </summary>
        bool IsTeamMode { get; }

        /// <summary>
        /// プレイヤーが死亡するモードかどうか
        /// </summary>
        bool CanPlayersDie { get; }

        /// <summary>
        /// 矢が無制限かどうか
        /// </summary>
        bool IsUnlimitedArrows { get; }

        /// <summary>
        /// 時間制限があるかどうか
        /// </summary>
        bool HasTimeLimit { get; }

        /// <summary>
        /// 制限時間に達したかどうか
        /// </summary>
        bool IsTimeUp { get; }

        /// <summary>
        /// マッチが終了条件を満たしているかどうか
        /// </summary>
        bool IsMatchOver { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// ハンドラーを初期化します
        /// </summary>
        /// <param name="manager">親のMatchManager</param>
        /// <param name="settings">ルーム設定</param>
        void Initialize(MatchManager manager, RoomSettings settings);

        /// <summary>
        /// マッチ開始時の処理
        /// </summary>
        void OnMatchStart();

        /// <summary>
        /// 毎フレームの更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// マッチ終了時の処理
        /// </summary>
        void OnMatchEnd();

        /// <summary>
        /// リソースの解放
        /// </summary>
        void Cleanup();

        #endregion

        #region Win Conditions

        /// <summary>
        /// 勝利条件をチェックします
        /// </summary>
        /// <param name="winnerId">勝者のクライアントID（チームモードの場合はチームインデックス）</param>
        /// <returns>勝者が決定した場合はtrue</returns>
        bool CheckWinCondition(out ulong winnerId);

        /// <summary>
        /// チームスコアを取得します（チームモードのみ）
        /// </summary>
        /// <param name="teamIndex">チームインデックス（0 or 1）</param>
        /// <returns>チームの合計スコア</returns>
        int GetTeamScore(int teamIndex);

        #endregion

        #region Player Events

        /// <summary>
        /// プレイヤーが死亡した時の処理
        /// </summary>
        /// <param name="clientId">死亡したプレイヤーのクライアントID</param>
        /// <param name="killerId">キルしたプレイヤーのクライアントID</param>
        void OnPlayerDeath(ulong clientId, ulong killerId);

        /// <summary>
        /// プレイヤーがスコアを獲得した時の処理
        /// </summary>
        /// <param name="clientId">スコアを獲得したプレイヤーのクライアントID</param>
        /// <param name="score">獲得したスコア</param>
        /// <param name="hitLocation">命中部位</param>
        void OnPlayerScored(ulong clientId, int score, HitLocation hitLocation);

        /// <summary>
        /// プレイヤーが矢を発射した時の処理
        /// </summary>
        /// <param name="clientId">発射したプレイヤーのクライアントID</param>
        void OnArrowFired(ulong clientId);

        /// <summary>
        /// プレイヤーがリスポーン可能かどうかを取得します
        /// </summary>
        /// <param name="clientId">プレイヤーのクライアントID</param>
        /// <returns>リスポーン可能な場合はtrue</returns>
        bool CanPlayerRespawn(ulong clientId);

        /// <summary>
        /// プレイヤーの残り矢数を取得します
        /// </summary>
        /// <param name="clientId">プレイヤーのクライアントID</param>
        /// <returns>残り矢数（無制限の場合は-1）</returns>
        int GetRemainingArrows(ulong clientId);

        #endregion

        #region Events

        /// <summary>
        /// 残り時間が変更された時に発生します
        /// </summary>
        event Action<float>? RemainingTimeChanged;

        /// <summary>
        /// マッチ終了時に発生します
        /// </summary>
        event Action<ulong>? MatchEndTriggered;

        #endregion
    }

    /// <summary>
    /// 命中部位
    /// </summary>
    public enum HitLocation
    {
        /// <summary>ミス</summary>
        Miss = 0,

        /// <summary>心臓（クリティカル）</summary>
        Heart = 1,

        /// <summary>頭部</summary>
        Head = 2,

        /// <summary>胴体</summary>
        Torso = 3,

        /// <summary>腕</summary>
        Arm = 4,

        /// <summary>脚</summary>
        Leg = 5,

        /// <summary>馬</summary>
        Mount = 6
    }
}
