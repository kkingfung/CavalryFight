#nullable enable

using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// アリーナモードのルールハンドラー
    /// </summary>
    /// <remarks>
    /// アリーナモードのルール:
    /// - 時間制限必須（デフォルト5分）
    /// - 矢は無制限
    /// - プレイヤーは死なない
    /// - 勝者: 時間終了時に最高スコアのプレイヤー
    /// </remarks>
    public class ArenaRulesHandler : GameModeRulesHandlerBase
    {
        #region Properties

        public override GameMode Mode => GameMode.Arena;

        public override bool IsTeamMode => false;

        public override bool CanPlayersDie => false;

        public override bool IsUnlimitedArrows => true;

        public override bool HasTimeLimit => true;

        #endregion

        #region Win Condition

        public override bool CheckWinCondition(out ulong winnerId)
        {
            winnerId = 0;

            // 時間制限に達した場合のみ勝者を決定
            if (!IsTimeUp) return false;

            // 最高スコアのプレイヤーが勝者
            winnerId = GetHighestScoringPlayer();
            return winnerId != 0;
        }

        #endregion

        #region Lifecycle

        public override void Initialize(MatchManager manager, RoomSettings settings)
        {
            base.Initialize(manager, settings);

            // アリーナモードでは時間制限が必須
            if (settings.TimeLimit <= 0)
            {
                Debug.LogWarning("[ArenaRulesHandler] Arena mode requires a time limit. Using default 300 seconds.");
                _remainingTime = 300f;
            }
        }

        #endregion
    }
}
