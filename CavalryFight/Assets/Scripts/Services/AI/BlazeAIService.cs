#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// Blaze AI管理サービスの実装
    /// </summary>
    /// <remarks>
    /// Blaze AIエンジンをラップし、AI敵の一元管理を行います。
    /// スポーン、削除、ターゲット設定、状態管理などの機能を提供します。
    /// </remarks>
    public class BlazeAIService : IBlazeAIService
    {
        #region Events

        /// <summary>
        /// AI敵がスポーンされた時に発生します。
        /// </summary>
        public event EventHandler<AISpawnedEventArgs>? AISpawned;

        /// <summary>
        /// AI敵が死亡した時に発生します。
        /// </summary>
        public event EventHandler<AIDeathEventArgs>? AIDied;

        /// <summary>
        /// AI敵のターゲットが変更された時に発生します。
        /// </summary>
        public event EventHandler<AITargetChangedEventArgs>? AITargetChanged;

        /// <summary>
        /// AI敵の状態が変更された時に発生します。
        /// </summary>
        public event EventHandler<AIStateChangedEventArgs>? AIStateChanged;

        #endregion

        #region Fields

        private readonly List<BlazeAI> _activeAIs;
        private readonly Dictionary<BlazeAI, BlazeAI.State> _lastKnownStates;
        private Transform? _aiContainer;

        #endregion

        #region Properties

        /// <summary>
        /// 現在アクティブなAI敵のリストを取得します。
        /// </summary>
        public IReadOnlyList<BlazeAI> ActiveAIs => _activeAIs.AsReadOnly();

        /// <summary>
        /// アクティブなAI敵の数を取得します。
        /// </summary>
        public int ActiveAICount => _activeAIs.Count;

        #endregion

        #region Constructor

        /// <summary>
        /// BlazeAIServiceの新しいインスタンスを初期化します。
        /// </summary>
        public BlazeAIService()
        {
            _activeAIs = new List<BlazeAI>();
            _lastKnownStates = new Dictionary<BlazeAI, BlazeAI.State>();
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[BlazeAIService] Initializing...");

            // AI用のコンテナオブジェクトを作成
            var containerObject = new GameObject("AI Container");
            GameObject.DontDestroyOnLoad(containerObject);
            _aiContainer = containerObject.transform;

            Debug.Log("[BlazeAIService] Initialized.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[BlazeAIService] Disposing...");

            // すべてのAI敵を削除
            RemoveAllAIs();

            // イベントハンドラをクリア
            AISpawned = null;
            AIDied = null;
            AITargetChanged = null;
            AIStateChanged = null;

            // コンテナオブジェクトを破棄
            if (_aiContainer != null)
            {
                GameObject.Destroy(_aiContainer.gameObject);
                _aiContainer = null;
            }

            Debug.Log("[BlazeAIService] Disposed.");
        }

        #endregion

        #region Update

        /// <summary>
        /// サービスを更新します（MonoBehaviourのUpdateから呼び出す）
        /// </summary>
        /// <remarks>
        /// AI状態の変更を検出し、AIStateChangedイベントを発火します。
        /// </remarks>
        public void Update()
        {
            // 各AIの状態をチェック
            foreach (var ai in _activeAIs)
            {
                if (ai == null)
                {
                    continue;
                }

                BlazeAI.State currentState = ai.state;

                // 前回の状態を取得
                if (_lastKnownStates.TryGetValue(ai, out BlazeAI.State previousState))
                {
                    // 状態が変更されたかチェック
                    if (currentState != previousState)
                    {
                        // 状態変更イベントを発火
                        AIStateChanged?.Invoke(this, new AIStateChangedEventArgs(
                            ai,
                            currentState.ToString(),
                            previousState.ToString()
                        ));

                        Debug.Log($"[BlazeAIService] AI {ai.name} state changed: {previousState} -> {currentState}");
                    }
                }

                // 現在の状態を記録
                _lastKnownStates[ai] = currentState;
            }
        }

        #endregion

        #region Spawn Management

        /// <summary>
        /// AI敵をスポーンします。
        /// </summary>
        /// <param name="prefab">スポーンするAI敵のPrefab</param>
        /// <param name="position">スポーン位置</param>
        /// <param name="rotation">スポーン回転</param>
        /// <returns>スポーンされたAI敵</returns>
        public BlazeAI? SpawnAI(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[BlazeAIService] Cannot spawn AI: prefab is null.");
                return null;
            }

            // Prefabにblazeaiコンポーネントがあるか確認
            var blazeAIComponent = prefab.GetComponent<BlazeAI>();
            if (blazeAIComponent == null)
            {
                Debug.LogError("[BlazeAIService] Cannot spawn AI: prefab does not have BlazeAI component.");
                return null;
            }

            try
            {
                // AI敵をインスタンス化
                var aiObject = GameObject.Instantiate(prefab, position, rotation, _aiContainer);
                var ai = aiObject.GetComponent<BlazeAI>();

                if (ai != null)
                {
                    // リストに追加
                    _activeAIs.Add(ai);

                    // 初期状態を記録
                    _lastKnownStates[ai] = ai.state;

                    // イベントを発火
                    AISpawned?.Invoke(this, new AISpawnedEventArgs(ai, position));

                    Debug.Log($"[BlazeAIService] AI spawned at {position}. Active AIs: {_activeAIs.Count}");
                    return ai;
                }
                else
                {
                    Debug.LogError("[BlazeAIService] Failed to get BlazeAI component from instantiated object.");
                    GameObject.Destroy(aiObject);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to spawn AI: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 指定されたAI敵を削除します。
        /// </summary>
        /// <param name="ai">削除するAI敵</param>
        public void RemoveAI(BlazeAI ai)
        {
            if (ai == null)
            {
                return;
            }

            if (_activeAIs.Contains(ai))
            {
                _activeAIs.Remove(ai);

                // 状態トラッキングから削除
                _lastKnownStates.Remove(ai);

                // イベントを発火
                AIDied?.Invoke(this, new AIDeathEventArgs(ai, ai.transform.position));

                // GameObjectを破棄
                GameObject.Destroy(ai.gameObject);

                Debug.Log($"[BlazeAIService] AI removed. Active AIs: {_activeAIs.Count}");
            }
        }

        /// <summary>
        /// すべてのAI敵を削除します。
        /// </summary>
        public void RemoveAllAIs()
        {
            Debug.Log($"[BlazeAIService] Removing all AIs ({_activeAIs.Count})...");

            // リストのコピーを作成（削除中にリストが変更されるため）
            var aisCopy = _activeAIs.ToList();

            foreach (var ai in aisCopy)
            {
                if (ai != null)
                {
                    GameObject.Destroy(ai.gameObject);
                }
            }

            _activeAIs.Clear();
            _lastKnownStates.Clear();

            Debug.Log("[BlazeAIService] All AIs removed.");
        }

        #endregion

        #region Target Management

        /// <summary>
        /// 指定されたAI敵のターゲットを設定します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="target">ターゲット</param>
        /// <param name="turnToAttackState">攻撃状態に遷移するか</param>
        public void SetAITarget(BlazeAI ai, GameObject target, bool turnToAttackState = true)
        {
            if (ai == null)
            {
                Debug.LogWarning("[BlazeAIService] Cannot set target: AI is null.");
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("[BlazeAIService] Cannot set target: target is null.");
                return;
            }

            try
            {
                // Blaze AIのSetEnemyメソッドを呼び出す
                ai.SetEnemy(target, turnToAttackState);

                // イベントを発火
                AITargetChanged?.Invoke(this, new AITargetChangedEventArgs(ai, target));

                Debug.Log($"[BlazeAIService] Target set for AI: {ai.name} -> {target.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to set AI target: {ex.Message}");
            }
        }

        /// <summary>
        /// すべてのAI敵に同じターゲットを設定します。
        /// </summary>
        /// <param name="target">ターゲット</param>
        /// <param name="turnToAttackState">攻撃状態に遷移するか</param>
        public void SetAllAIsTarget(GameObject target, bool turnToAttackState = true)
        {
            if (target == null)
            {
                Debug.LogWarning("[BlazeAIService] Cannot set target: target is null.");
                return;
            }

            Debug.Log($"[BlazeAIService] Setting target for all AIs: {target.name}");

            foreach (var ai in _activeAIs)
            {
                if (ai != null)
                {
                    SetAITarget(ai, target, turnToAttackState);
                }
            }
        }

        /// <summary>
        /// 指定されたAI敵のターゲットをクリアします。
        /// </summary>
        /// <param name="ai">AI敵</param>
        public void ClearAITarget(BlazeAI ai)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                // Blaze AIのResetEnemyManagerメソッドを呼び出す
                ai.ResetEnemyManager();

                // イベントを発火
                AITargetChanged?.Invoke(this, new AITargetChangedEventArgs(ai, null));

                Debug.Log($"[BlazeAIService] Target cleared for AI: {ai.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to clear AI target: {ex.Message}");
            }
        }

        #endregion

        #region AI Control

        /// <summary>
        /// 指定されたAI敵をフレンドリーモードに設定します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="isFriendly">フレンドリーかどうか</param>
        public void SetAIFriendly(BlazeAI ai, bool isFriendly)
        {
            if (ai == null)
            {
                return;
            }

            ai.friendly = isFriendly;
            Debug.Log($"[BlazeAIService] AI {ai.name} friendly mode: {isFriendly}");
        }

        /// <summary>
        /// 指定されたAI敵を特定の位置に移動させます。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="position">目標位置</param>
        public void MoveAITo(BlazeAI ai, Vector3 position)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                // NavMeshAgentで移動
                var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.SetDestination(position);
                    Debug.Log($"[BlazeAIService] AI {ai.name} moving to {position}");
                }
                else
                {
                    Debug.LogWarning($"[BlazeAIService] AI {ai.name} has no NavMeshAgent.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to move AI: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定されたAI敵を特定の方向に回転させます。
        /// </summary>
        /// <param name="ai">AI敵</param>
        /// <param name="direction">回転方向</param>
        /// <param name="speed">回転速度</param>
        public void RotateAI(BlazeAI ai, Vector3 direction, float speed)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                ai.RotateTo(direction, speed);
                Debug.Log($"[BlazeAIService] AI {ai.name} rotating to {direction}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to rotate AI: {ex.Message}");
            }
        }

        #endregion

        #region Audio Control

        /// <summary>
        /// 指定されたAI敵のパトロールオーディオを再生します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        public void PlayAIPatrolAudio(BlazeAI ai)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                ai.PlayPatrolAudio();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to play AI patrol audio: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定されたAI敵のオーディオを停止します。
        /// </summary>
        /// <param name="ai">AI敵</param>
        public void StopAIAudio(BlazeAI ai)
        {
            if (ai == null)
            {
                return;
            }

            try
            {
                ai.StopAudio();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlazeAIService] Failed to stop AI audio: {ex.Message}");
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// 指定された範囲内のAI敵を取得します。
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>範囲内のAI敵のリスト</returns>
        public List<BlazeAI> GetAIsInRadius(Vector3 position, float radius)
        {
            var result = new List<BlazeAI>();

            foreach (var ai in _activeAIs)
            {
                if (ai != null && Vector3.Distance(ai.transform.position, position) <= radius)
                {
                    result.Add(ai);
                }
            }

            return result;
        }

        /// <summary>
        /// プレイヤーに最も近いAI敵を取得します。
        /// </summary>
        /// <param name="playerPosition">プレイヤーの位置</param>
        /// <returns>最も近いAI敵。存在しない場合はnull</returns>
        public BlazeAI? GetClosestAI(Vector3 playerPosition)
        {
            BlazeAI? closestAI = null;
            float closestDistance = float.MaxValue;

            foreach (var ai in _activeAIs)
            {
                if (ai != null)
                {
                    float distance = Vector3.Distance(ai.transform.position, playerPosition);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestAI = ai;
                    }
                }
            }

            return closestAI;
        }

        #endregion
    }
}
