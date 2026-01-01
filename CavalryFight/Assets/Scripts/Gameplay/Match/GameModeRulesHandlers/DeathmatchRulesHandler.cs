#nullable enable

using System.Collections.Generic;
using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// デスマッチモードのルールハンドラー
    /// </summary>
    /// <remarks>
    /// デスマッチモードのルール:
    /// - 時間制限なし（固定）
    /// - 矢は無制限
    /// - プレイヤーは死亡する（リスポーンなし）
    /// - 勝者: 最後に生き残ったプレイヤー
    /// </remarks>
    public class DeathmatchRulesHandler : GameModeRulesHandlerBase
    {
        #region Fields

        private HashSet<ulong> _eliminatedPlayers = new HashSet<ulong>();

        #endregion

        #region Properties

        public override GameMode Mode => GameMode.Deathmatch;

        public override bool IsTeamMode => false;

        public override bool CanPlayersDie => true;

        public override bool IsUnlimitedArrows => true;

        public override bool HasTimeLimit => false;

        /// <summary>
        /// 生存しているプレイヤー数
        /// </summary>
        public int AlivePlayerCount
        {
            get
            {
                if (_manager == null) return 0;
                return _manager.PlayerSlots.Count - _eliminatedPlayers.Count;
            }
        }

        #endregion

        #region Lifecycle

        public override void Initialize(MatchManager manager, RoomSettings settings)
        {
            base.Initialize(manager, settings);
            _eliminatedPlayers.Clear();
        }

        public override void Cleanup()
        {
            _eliminatedPlayers.Clear();
            base.Cleanup();
        }

        #endregion

        #region Win Condition

        public override bool CheckWinCondition(out ulong winnerId)
        {
            winnerId = 0;

            // 生存者が1人以下になった場合
            if (AlivePlayerCount > 1) return false;

            // 最後の生存者を見つける
            if (_manager != null)
            {
                foreach (var slot in _manager.PlayerSlots)
                {
                    if (!_eliminatedPlayers.Contains(slot.PlayerId))
                    {
                        winnerId = slot.PlayerId;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Player Events

        public override void OnPlayerDeath(ulong clientId, ulong killerId)
        {
            base.OnPlayerDeath(clientId, killerId);

            // プレイヤーを脱落者リストに追加
            _eliminatedPlayers.Add(clientId);

            Debug.Log($"[DeathmatchRulesHandler] Player {clientId} eliminated. Remaining: {AlivePlayerCount}");

            // 勝利条件をチェック
            if (CheckWinCondition(out ulong winnerId))
            {
                OnWinConditionMet(winnerId);
            }
        }

        public override bool CanPlayerRespawn(ulong clientId)
        {
            // デスマッチではリスポーン不可
            return false;
        }

        #endregion
    }
}
