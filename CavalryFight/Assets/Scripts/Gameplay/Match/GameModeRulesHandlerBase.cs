#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// ゲームモードルールハンドラーの基底クラス
    /// </summary>
    /// <remarks>
    /// 共通のロジックを実装し、モード固有のロジックは派生クラスでオーバーライドします。
    /// </remarks>
    public abstract class GameModeRulesHandlerBase : MonoBehaviour, IGameModeRulesHandler
    {
        #region Fields

        protected MatchManager? _manager;
        protected RoomSettings _settings;
        protected float _remainingTime;
        protected bool _isMatchStarted;
        protected bool _matchEnded;
        protected Dictionary<ulong, int> _playerArrows = new Dictionary<ulong, int>();
        protected Dictionary<ulong, int> _playerDeaths = new Dictionary<ulong, int>();

        #endregion

        #region Abstract Properties

        /// <summary>
        /// このハンドラーが処理するゲームモード
        /// </summary>
        public abstract GameMode Mode { get; }

        /// <summary>
        /// チームモードかどうか
        /// </summary>
        public abstract bool IsTeamMode { get; }

        /// <summary>
        /// プレイヤーが死亡するモードかどうか
        /// </summary>
        public abstract bool CanPlayersDie { get; }

        #endregion

        #region Virtual Properties

        /// <summary>
        /// 矢が無制限かどうか
        /// </summary>
        public virtual bool IsUnlimitedArrows => _settings.ArrowLimit == 0;

        /// <summary>
        /// 時間制限があるかどうか
        /// </summary>
        public virtual bool HasTimeLimit => _settings.TimeLimit > 0;

        /// <summary>
        /// 制限時間に達したかどうか
        /// </summary>
        public virtual bool IsTimeUp => HasTimeLimit && _remainingTime <= 0f;

        /// <summary>
        /// マッチが終了条件を満たしているかどうか
        /// </summary>
        public virtual bool IsMatchOver => IsTimeUp || CheckWinCondition(out _);

        #endregion

        #region Events

        /// <summary>
        /// 残り時間が変更された時に発生します
        /// </summary>
        public event Action<float>? RemainingTimeChanged;

        /// <summary>
        /// マッチ終了時に発生します
        /// </summary>
        public event Action<ulong>? MatchEndTriggered;

        #endregion

        #region Lifecycle

        /// <summary>
        /// ハンドラーを初期化します
        /// </summary>
        public virtual void Initialize(MatchManager manager, RoomSettings settings)
        {
            _manager = manager;
            _settings = settings;
            _remainingTime = settings.TimeLimit;
            _isMatchStarted = false;
            _matchEnded = false;
            _playerArrows.Clear();
            _playerDeaths.Clear();

            Debug.Log($"[{GetType().Name}] Initialized with TimeLimit={settings.TimeLimit}, ArrowLimit={settings.ArrowLimit}");
        }

        /// <summary>
        /// マッチ開始時の処理
        /// </summary>
        public virtual void OnMatchStart()
        {
            _isMatchStarted = true;

            // 各プレイヤーの矢数を初期化
            if (!IsUnlimitedArrows && _manager != null)
            {
                foreach (var slot in _manager.PlayerSlots)
                {
                    _playerArrows[slot.PlayerId] = _settings.ArrowLimit;
                }
            }

            Debug.Log($"[{GetType().Name}] Match started");
        }

        /// <summary>
        /// 毎フレームの更新処理
        /// </summary>
        public virtual void OnUpdate(float deltaTime)
        {
            if (!_isMatchStarted)
            {
                return;
            }

            // 時間制限の更新
            if (HasTimeLimit && _remainingTime > 0)
            {
                float previousTime = _remainingTime;
                _remainingTime -= deltaTime;

                if (_remainingTime <= 0)
                {
                    _remainingTime = 0;
                }

                // 秒単位で変わった場合のみイベント発火
                if (Mathf.FloorToInt(previousTime) != Mathf.FloorToInt(_remainingTime))
                {
                    RemainingTimeChanged?.Invoke(_remainingTime);
                }

                // 時間切れチェック
                if (IsTimeUp)
                {
                    OnTimeUp();
                }
            }

            // 勝利条件チェック（まだマッチが終了していない場合のみ）
            if (!_matchEnded && CheckWinCondition(out ulong winnerId))
            {
                _matchEnded = true;
                OnWinConditionMet(winnerId);
            }
        }

        /// <summary>
        /// マッチ終了時の処理
        /// </summary>
        public virtual void OnMatchEnd()
        {
            _isMatchStarted = false;
            _matchEnded = true;
            Debug.Log($"[{GetType().Name}] Match ended");
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public virtual void Cleanup()
        {
            _playerArrows.Clear();
            _playerDeaths.Clear();
            _manager = null;
        }

        #endregion

        #region Win Conditions

        /// <summary>
        /// 勝利条件をチェックします
        /// </summary>
        public abstract bool CheckWinCondition(out ulong winnerId);

        /// <summary>
        /// チームスコアを取得します
        /// </summary>
        public virtual int GetTeamScore(int teamIndex)
        {
            if (!IsTeamMode || _manager == null)
            {
                return 0;
            }

            int totalScore = 0;
            foreach (var slot in _manager.PlayerSlots)
            {
                if (slot.TeamIndex == teamIndex)
                {
                    var playerScore = _manager.GetPlayerScore(slot.PlayerId);
                    if (playerScore.HasValue)
                    {
                        totalScore += playerScore.Value.Score;
                    }
                }
            }
            return totalScore;
        }

        #endregion

        #region Player Events

        /// <summary>
        /// プレイヤーが死亡した時の処理
        /// </summary>
        public virtual void OnPlayerDeath(ulong clientId, ulong killerId)
        {
            if (!CanPlayersDie)
            {
                return;
            }

            if (!_playerDeaths.ContainsKey(clientId))
            {
                _playerDeaths[clientId] = 0;
            }
            _playerDeaths[clientId]++;

            Debug.Log($"[{GetType().Name}] Player {clientId} died (killed by {killerId}). Deaths: {_playerDeaths[clientId]}");
        }

        /// <summary>
        /// プレイヤーがスコアを獲得した時の処理
        /// </summary>
        public virtual void OnPlayerScored(ulong clientId, int score, HitLocation hitLocation)
        {
            Debug.Log($"[{GetType().Name}] Player {clientId} scored {score} points (hit: {hitLocation})");
        }

        /// <summary>
        /// プレイヤーが矢を発射した時の処理
        /// </summary>
        public virtual void OnArrowFired(ulong clientId)
        {
            if (IsUnlimitedArrows)
            {
                return;
            }

            if (_playerArrows.ContainsKey(clientId) && _playerArrows[clientId] > 0)
            {
                _playerArrows[clientId]--;
                Debug.Log($"[{GetType().Name}] Player {clientId} fired arrow. Remaining: {_playerArrows[clientId]}");
            }
        }

        /// <summary>
        /// プレイヤーがリスポーン可能かどうかを取得します
        /// </summary>
        public virtual bool CanPlayerRespawn(ulong clientId)
        {
            // デフォルトではリスポーン可能（Deathmatchなど派生クラスでオーバーライド）
            return true;
        }

        /// <summary>
        /// プレイヤーの残り矢数を取得します
        /// </summary>
        public virtual int GetRemainingArrows(ulong clientId)
        {
            if (IsUnlimitedArrows)
            {
                return -1;
            }

            return _playerArrows.TryGetValue(clientId, out int arrows) ? arrows : 0;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 時間切れ時の処理
        /// </summary>
        protected virtual void OnTimeUp()
        {
            Debug.Log($"[{GetType().Name}] Time is up!");

            // スコアが最も高いプレイヤーを勝者とする
            if (CheckWinCondition(out ulong winnerId))
            {
                MatchEndTriggered?.Invoke(winnerId);
            }
            else
            {
                // 引き分けの場合は0を返す
                MatchEndTriggered?.Invoke(0);
            }
        }

        /// <summary>
        /// 勝利条件が満たされた時の処理
        /// </summary>
        protected virtual void OnWinConditionMet(ulong winnerId)
        {
            Debug.Log($"[{GetType().Name}] Win condition met! Winner: {winnerId}");
            MatchEndTriggered?.Invoke(winnerId);
        }

        /// <summary>
        /// 最高スコアのプレイヤーIDを取得します
        /// </summary>
        public ulong GetHighestScoringPlayer()
        {
            if (_manager == null)
            {
                Debug.LogWarning($"[{GetType().Name}] GetHighestScoringPlayer: Manager is null!");
                return 0;
            }

            var allScores = _manager.GetAllPlayerScores();
            if (allScores.Length == 0)
            {
                Debug.LogWarning($"[{GetType().Name}] GetHighestScoringPlayer: No scores available!");
                return 0;
            }

            // スコア順にソート
            var sortedScores = allScores.OrderByDescending(s => s.Score).ToArray();

            int highestScore = sortedScores[0].Score;

            // 同じスコアのプレイヤーが複数いる場合（引き分け）
            int playersWithHighestScore = sortedScores.Count(s => s.Score == highestScore);
            if (playersWithHighestScore > 1)
            {
                // 引き分け
                Debug.Log($"[{GetType().Name}] DRAW - {playersWithHighestScore} players have the same highest score");
                return ulong.MaxValue;
            }

            return sortedScores[0].ClientId;
        }

        /// <summary>
        /// 最高スコアのチームインデックスを取得します
        /// </summary>
        protected int GetHighestScoringTeam()
        {
            int team0Score = GetTeamScore(0);
            int team1Score = GetTeamScore(1);

            if (team0Score > team1Score)
            {
                return 0;
            }
            if (team1Score > team0Score)
            {
                return 1;
            }
            return -1; // 引き分け
        }

        #endregion
    }
}
