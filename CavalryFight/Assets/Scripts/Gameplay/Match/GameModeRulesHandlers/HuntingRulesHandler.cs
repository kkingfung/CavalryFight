#nullable enable

using System.Collections.Generic;
using System.Linq;
using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// ハンティングモードのルールハンドラー
    /// </summary>
    /// <remarks>
    /// ハンティングモードのルール:
    /// - チーム対抗（最低4人必要）
    /// - 各チームに1人のハンター（騎馬弓兵）と複数のウルフ
    /// - ハンターは相手チームのウルフを狩る（頭:高得点、体:低得点）
    /// - ハンターは相手チームのハンターをスタンさせる（スコアなし）
    /// - ウルフはハンターをスタンさせる（死亡しない）
    /// - 時間制限必須、無制限の矢
    /// - 勝者: 時間終了時の最高チームスコア
    /// </remarks>
    public class HuntingRulesHandler : GameModeRulesHandlerBase
    {
        #region Constants

        /// <summary>
        /// 最低プレイヤー数
        /// </summary>
        public const int MinimumPlayerCount = 4;

        /// <summary>
        /// チームあたりのハンター数
        /// </summary>
        public const int HuntersPerTeam = 1;

        #endregion

        #region Serialized Fields

        [Header("Hunting Settings")]
        [SerializeField] private float _hunterStunDuration = 3f;

        [Header("Wolf Materials")]
        [Tooltip("使用可能なウルフマテリアル（Red, White, Blue, Green, Black, AI, Enemy）")]
        [SerializeField] private Material[]? _wolfMaterials;

        #endregion

        #region Fields

        private Dictionary<int, Material> _teamWolfMaterials = new Dictionary<int, Material>();
        private HashSet<ulong> _hunters = new HashSet<ulong>();
        private HashSet<ulong> _wolves = new HashSet<ulong>();

        #endregion

        #region Properties

        public override GameMode Mode => GameMode.Hunting;

        public override bool IsTeamMode => true;

        public override bool CanPlayersDie => false;

        /// <summary>
        /// ハンターのスタン時間
        /// </summary>
        public float HunterStunDuration => _hunterStunDuration;

        /// <summary>
        /// チーム0のウルフマテリアル
        /// </summary>
        public Material? Team0WolfMaterial => _teamWolfMaterials.TryGetValue(0, out var mat) ? mat : null;

        /// <summary>
        /// チーム1のウルフマテリアル
        /// </summary>
        public Material? Team1WolfMaterial => _teamWolfMaterials.TryGetValue(1, out var mat) ? mat : null;

        #endregion

        #region Lifecycle

        public override void Initialize(MatchManager manager, RoomSettings settings)
        {
            base.Initialize(manager, settings);

            _hunters.Clear();
            _wolves.Clear();
            _teamWolfMaterials.Clear();

            // プレイヤー数チェック
            if (manager.PlayerSlots.Count < MinimumPlayerCount)
            {
                Debug.LogWarning($"[HuntingRulesHandler] Not enough players. Required: {MinimumPlayerCount}, Current: {manager.PlayerSlots.Count}");
            }

            // 各チームの役割を割り当て
            AssignRoles();

            // ウルフマテリアルをランダムに選択
            SelectWolfMaterials();

            Debug.Log($"[HuntingRulesHandler] Initialized. Hunters: {_hunters.Count}, Wolves: {_wolves.Count}");
        }

        public override void Cleanup()
        {
            _hunters.Clear();
            _wolves.Clear();
            _teamWolfMaterials.Clear();
            base.Cleanup();
        }

        #endregion

        #region Role Assignment

        private void AssignRoles()
        {
            if (_manager == null)
            {
                return;
            }

            // チームごとにプレイヤーを分類
            var team0Players = new List<ulong>();
            var team1Players = new List<ulong>();

            foreach (var slot in _manager.PlayerSlots)
            {
                if (slot.TeamIndex == 0)
                {
                    team0Players.Add(slot.PlayerId);
                }
                else
                {
                    team1Players.Add(slot.PlayerId);
                }
            }

            // 各チームから1人をハンターとして選択（ランダム）
            AssignHunterForTeam(team0Players);
            AssignHunterForTeam(team1Players);

            Debug.Log($"[HuntingRulesHandler] Roles assigned - Team0: Hunter {(team0Players.Count > 0 ? team0Players[0].ToString() : "none")}, Team1: Hunter {(team1Players.Count > 0 ? team1Players[0].ToString() : "none")}");
        }

        private void AssignHunterForTeam(List<ulong> teamPlayers)
        {
            if (teamPlayers.Count == 0)
            {
                return;
            }

            // ランダムにハンターを選択
            int hunterIndex = Random.Range(0, teamPlayers.Count);
            ulong hunterId = teamPlayers[hunterIndex];
            _hunters.Add(hunterId);

            // 残りはウルフ
            for (int i = 0; i < teamPlayers.Count; i++)
            {
                if (i != hunterIndex)
                {
                    _wolves.Add(teamPlayers[i]);
                }
            }
        }

        private void SelectWolfMaterials()
        {
            if (_wolfMaterials == null || _wolfMaterials.Length < 2)
            {
                Debug.LogWarning("[HuntingRulesHandler] Not enough wolf materials assigned!");
                return;
            }

            // ランダムに2つのマテリアルを選択
            var shuffledMaterials = _wolfMaterials.OrderBy(_ => Random.value).Take(2).ToArray();
            _teamWolfMaterials[0] = shuffledMaterials[0];
            _teamWolfMaterials[1] = shuffledMaterials[1];

            Debug.Log($"[HuntingRulesHandler] Wolf materials selected - Team0: {shuffledMaterials[0].name}, Team1: {shuffledMaterials[1].name}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定されたプレイヤーがハンターかどうかを取得します
        /// </summary>
        public bool IsHunter(ulong clientId)
        {
            return _hunters.Contains(clientId);
        }

        /// <summary>
        /// 指定されたプレイヤーがウルフかどうかを取得します
        /// </summary>
        public bool IsWolf(ulong clientId)
        {
            return _wolves.Contains(clientId);
        }

        /// <summary>
        /// プレイヤーをスタンさせます（HunterStunHandlerに委譲）
        /// </summary>
        /// <remarks>
        /// スタン状態の管理はHunterStunHandlerで一元化されています。
        /// このメソッドはログ出力のみを行い、実際のスタン適用は呼び出し元で行います。
        /// </remarks>
        public void StunPlayer(ulong clientId, float duration)
        {
            Debug.Log($"[HuntingRulesHandler] Player {clientId} stunned for {duration}s");
        }

        /// <summary>
        /// 指定されたチームのウルフマテリアルを取得します
        /// </summary>
        public Material? GetWolfMaterialForTeam(int teamIndex)
        {
            return _teamWolfMaterials.TryGetValue(teamIndex, out var mat) ? mat : null;
        }

        #endregion

        #region Win Condition

        public override bool CheckWinCondition(out ulong winnerId)
        {
            winnerId = 0;

            // 時間切れで最高チームスコアが勝者
            if (!IsTimeUp)
            {
                return false;
            }

            int team0Score = GetTeamScore(0);
            int team1Score = GetTeamScore(1);

            if (team0Score > team1Score)
            {
                winnerId = 0; // チーム0の勝利
            }
            else if (team1Score > team0Score)
            {
                winnerId = 1; // チーム1の勝利
            }
            else
            {
                winnerId = ulong.MaxValue; // 引き分け
            }

            return true;
        }

        #endregion

        #region Scoring

        public override void OnPlayerScored(ulong clientId, int score, HitLocation location)
        {
            // ハンターのみがスコアを獲得できる
            if (!IsHunter(clientId))
            {
                return;
            }

            base.OnPlayerScored(clientId, score, location);
        }

        /// <summary>
        /// ハンターがウルフにヒットした時のスコアを計算します
        /// </summary>
        public int CalculateWolfHitScore(HitLocation location)
        {
            return location switch
            {
                HitLocation.Heart => 100, // 心臓は最高得点
                HitLocation.Head => 50,   // 頭は高得点
                HitLocation.Torso => 20,  // 胴体は中程度
                HitLocation.Arm => 10,    // 腕は低得点
                HitLocation.Leg => 10,    // 足は低得点
                _ => 0
            };
        }

        #endregion

        #region Player Events

        public override void OnPlayerDeath(ulong clientId, ulong killerId)
        {
            // ハンティングモードでは死亡しない
            // 代わりにスタンを適用
            if (IsHunter(clientId))
            {
                StunPlayer(clientId, _hunterStunDuration);
            }
        }

        public override bool CanPlayerRespawn(ulong clientId)
        {
            // 死亡しないのでリスポーンは不要
            return false;
        }

        #endregion
    }
}
