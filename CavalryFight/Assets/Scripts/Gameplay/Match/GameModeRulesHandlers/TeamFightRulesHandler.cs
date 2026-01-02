#nullable enable

using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// チームファイトモードのルールハンドラー
    /// </summary>
    /// <remarks>
    /// チームファイトモードのルール:
    /// - 時間制限必須（デフォルト10分）
    /// - 矢は無制限
    /// - プレイヤーは死なない
    /// - チーム対チーム（チーム0 vs チーム1）
    /// - 勝者: 時間終了時に合計スコアが高いチーム
    /// </remarks>
    public class TeamFightRulesHandler : GameModeRulesHandlerBase
    {
        #region Properties

        public override GameMode Mode => GameMode.TeamFight;

        public override bool IsTeamMode => true;

        public override bool CanPlayersDie => false;

        public override bool IsUnlimitedArrows => true;

        public override bool HasTimeLimit => true;

        #endregion

        #region Win Condition

        public override bool CheckWinCondition(out ulong winnerId)
        {
            winnerId = 0;

            // 時間制限に達した場合のみ勝者を決定
            if (!IsTimeUp)
            {
                return false;
            }

            // 最高スコアのチームが勝者
            int winningTeam = GetHighestScoringTeam();
            if (winningTeam < 0)
            {
                // 引き分け
                return false;
            }

            // チームインデックスをwinnerIdとして返す（0 or 1）
            winnerId = (ulong)winningTeam;
            return true;
        }

        #endregion

        #region Lifecycle

        public override void Initialize(MatchManager manager, RoomSettings settings)
        {
            base.Initialize(manager, settings);

            // チームファイトでは時間制限が必須
            if (settings.TimeLimit <= 0)
            {
                Debug.LogWarning("[TeamFightRulesHandler] TeamFight mode requires a time limit. Using default 600 seconds.");
                _remainingTime = 600f;
            }

            // チーム構成を検証
            int team0Count = 0;
            int team1Count = 0;
            foreach (var slot in manager.PlayerSlots)
            {
                if (slot.TeamIndex == 0) team0Count++;
                else if (slot.TeamIndex == 1) team1Count++;
            }

            Debug.Log($"[TeamFightRulesHandler] Team composition: Team 0 = {team0Count}, Team 1 = {team1Count}");

            if (team0Count == 0 || team1Count == 0)
            {
                Debug.LogWarning("[TeamFightRulesHandler] One team has no players!");
            }
        }

        public override void OnPlayerScored(ulong clientId, int score, HitLocation hitLocation)
        {
            base.OnPlayerScored(clientId, score, hitLocation);

            // チームスコアをログに出力
            if (_manager != null)
            {
                Debug.Log($"[TeamFightRulesHandler] Team scores - Team 0: {GetTeamScore(0)}, Team 1: {GetTeamScore(1)}");
            }
        }

        #endregion
    }
}
