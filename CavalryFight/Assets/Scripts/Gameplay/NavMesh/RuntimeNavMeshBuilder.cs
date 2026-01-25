#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// ランタイムでNavMeshを構築するコンポーネント
    /// </summary>
    /// <remarks>
    /// フィールドプレハブに追加して、ロード時に自動的にNavMeshを構築します。
    /// Unity 2022.2以降ではNavMeshSurface（AI Navigationパッケージ）を使用し、
    /// それ以前のバージョンではNavMesh.CalculateTriangulation()を使用します。
    ///
    /// 使用方法:
    /// 1. フィールドプレハブ（Arena.prefab等）にこのコンポーネントを追加
    /// 2. NavMeshSurfaceコンポーネントを同じGameObjectに追加（推奨）
    /// 3. NavMeshSurfaceのAgent Type、Include Layers、Use Geometryを設定
    /// 4. フィールドがロードされると自動的にNavMeshが構築されます
    /// </remarks>
    public class RuntimeNavMeshBuilder : MonoBehaviour
    {
        #region Serialized Fields

        [Header("NavMesh Settings")]
        [Tooltip("NavMeshを構築するまでの遅延時間（秒）")]
        [SerializeField] private float _buildDelay = 0.1f;

        [Tooltip("自動的にNavMeshを構築するかどうか")]
        [SerializeField] private bool _autoBuild = true;

        [Tooltip("NavMesh構築完了後にAIを再初期化するかどうか")]
        [SerializeField] private bool _reinitializeAIAfterBuild = true;

        [Header("Debug")]
        [Tooltip("デバッグログを出力するかどうか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private NavMeshSurface? _navMeshSurface;
        private bool _isBuilt = false;

        #endregion

        #region Properties

        /// <summary>
        /// NavMeshが構築済みかどうか
        /// </summary>
        public bool IsBuilt => _isBuilt;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_autoBuild)
            {
                StartCoroutine(BuildNavMeshDelayed());
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// NavMeshを構築します
        /// </summary>
        public void BuildNavMesh()
        {
            StartCoroutine(BuildNavMeshDelayed());
        }

        /// <summary>
        /// NavMeshを即座に構築します
        /// </summary>
        public void BuildNavMeshImmediate()
        {
            DoBuildNavMesh();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 遅延してNavMeshを構築します
        /// </summary>
        private IEnumerator BuildNavMeshDelayed()
        {
            // 少し待ってからビルド（シーンが完全にロードされるのを待つ）
            yield return new WaitForSeconds(_buildDelay);

            DoBuildNavMesh();
        }

        /// <summary>
        /// NavMeshを構築する実際の処理
        /// </summary>
        private void DoBuildNavMesh()
        {
            if (_isBuilt)
            {
                if (_debugLog)
                {
                    Debug.Log("[RuntimeNavMeshBuilder] NavMesh is already built, skipping...");
                }
                return;
            }

            // NavMeshSurfaceを使用してビルド
            BuildWithNavMeshSurface();

            _isBuilt = true;

            if (_debugLog)
            {
                Debug.Log("[RuntimeNavMeshBuilder] NavMesh build completed!");
            }

            // AIの再初期化
            if (_reinitializeAIAfterBuild)
            {
                ReinitializeAIAgents();
            }
        }

        /// <summary>
        /// NavMeshSurfaceを使用してNavMeshを構築します
        /// </summary>
        private void BuildWithNavMeshSurface()
        {
            try
            {
                // NavMeshSurfaceを取得または追加
                _navMeshSurface = GetComponent<NavMeshSurface>();

                if (_navMeshSurface == null)
                {
                    if (_debugLog)
                    {
                        Debug.Log("[RuntimeNavMeshBuilder] NavMeshSurface not found, adding one...");
                    }

                    _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();

                    // デフォルト設定 - Physics Collidersを使用（メッシュのRead/Write問題を回避）
                    _navMeshSurface.collectObjects = CollectObjects.Children;
                    _navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                }

                // NavMeshをビルド
                if (_debugLog)
                {
                    Debug.Log($"[RuntimeNavMeshBuilder] Building NavMesh with NavMeshSurface on '{gameObject.name}'...");
                    Debug.Log($"[RuntimeNavMeshBuilder] UseGeometry: {_navMeshSurface.useGeometry}, CollectObjects: {_navMeshSurface.collectObjects}");
                }

                _navMeshSurface.BuildNavMesh();

                if (_debugLog)
                {
                    Debug.Log($"[RuntimeNavMeshBuilder] NavMeshSurface build completed. Agent Type: {_navMeshSurface.agentTypeID}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RuntimeNavMeshBuilder] Exception during NavMesh build: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// シーン内のすべてのNavMeshAgentを再初期化します
        /// </summary>
        private void ReinitializeAIAgents()
        {
            if (_debugLog)
            {
                Debug.Log("[RuntimeNavMeshBuilder] Reinitializing AI agents...");
            }

            // シーン内のすべてのNavMeshAgentを取得
            var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            int reinitializedCount = 0;

            foreach (var agent in agents)
            {
                if (agent == null || !agent.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // エージェントを一度無効化して再有効化
                bool wasEnabled = agent.enabled;
                if (wasEnabled)
                {
                    agent.enabled = false;
                }

                // 現在の位置をNavMesh上に配置
                if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    if (_debugLog)
                    {
                        Debug.Log($"[RuntimeNavMeshBuilder] Agent '{agent.gameObject.name}' warped to NavMesh position: {hit.position}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[RuntimeNavMeshBuilder] Could not find NavMesh position near agent '{agent.gameObject.name}' at {agent.transform.position}");
                }

                if (wasEnabled)
                {
                    agent.enabled = true;
                }

                reinitializedCount++;
            }

            if (_debugLog)
            {
                Debug.Log($"[RuntimeNavMeshBuilder] Reinitialized {reinitializedCount} NavMeshAgents.");
            }
        }

        #endregion
    }
}
