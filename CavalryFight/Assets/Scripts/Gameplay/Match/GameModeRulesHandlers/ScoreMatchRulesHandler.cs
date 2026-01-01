#nullable enable

using System.Linq;
using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// スコアマッチモードのルールハンドラー
    /// </summary>
    /// <remarks>
    /// スコアマッチモードのルール:
    /// - 時間制限必須（デフォルト3分/ラウンド）
    /// - 矢の制限あり（デフォルト5本）
    /// - プレイヤーは死なない
    /// - 勝者: 全員の矢が尽きるか時間終了時に最高スコアのプレイヤー
    /// </remarks>
    public class ScoreMatchRulesHandler : GameModeRulesHandlerBase
    {
        #region Properties

        public override GameMode Mode => GameMode.ScoreMatch;

        public override bool IsTeamMode => false;

        public override bool CanPlayersDie => false;

        public override bool IsUnlimitedArrows => false;

        public override bool HasTimeLimit => true;

        /// <summary>
        /// 全員の矢が尽きたかどうか
        /// </summary>
        public bool AllArrowsUsed
        {
            get
            {
                if (_manager == null) return false;

                foreach (var slot in _manager.PlayerSlots)
                {
                    if (GetRemainingArrows(slot.PlayerId) > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override bool IsMatchOver => base.IsMatchOver || AllArrowsUsed;

        #endregion

        #region Win Condition

        public override bool CheckWinCondition(out ulong winnerId)
        {
            winnerId = 0;

            // 時間切れまたは全員の矢が尽きた場合
            if (!IsTimeUp && !AllArrowsUsed) return false;

            // 最高スコアのプレイヤーが勝者
            winnerId = GetHighestScoringPlayer();
            return winnerId != 0;
        }

        #endregion

        #region Lifecycle

        public override void Initialize(MatchManager manager, RoomSettings settings)
        {
            base.Initialize(manager, settings);

            // スコアマッチでは矢の制限が必須
            if (settings.ArrowLimit <= 0)
            {
                Debug.LogWarning("[ScoreMatchRulesHandler] ScoreMatch mode requires an arrow limit. Using default 5 arrows.");
                foreach (var slot in manager.PlayerSlots)
                {
                    _playerArrows[slot.PlayerId] = 5;
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            // 全員の矢が尽きたかチェック
            if (AllArrowsUsed && !IsTimeUp)
            {
                Debug.Log("[ScoreMatchRulesHandler] All arrows used. Ending match.");
                if (CheckWinCondition(out ulong winnerId))
                {
                    OnWinConditionMet(winnerId);
                }
            }
        }

        #endregion
    }
}
