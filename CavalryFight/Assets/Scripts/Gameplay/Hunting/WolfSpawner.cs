#nullable enable

using System.Collections.Generic;
using CavalryFight.Gameplay.Match;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Gameplay.Hunting
{
    /// <summary>
    /// ハンティングモードでウルフをスポーンするマネージャー
    /// </summary>
    /// <remarks>
    /// ウルフ役のプレイヤーにウルフプレハブをスポーンし、
    /// チームに応じたマテリアルを適用します。
    /// </remarks>
    public class WolfSpawner : NetworkBehaviour
    {
        #region Singleton

        private static WolfSpawner? _instance;

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static WolfSpawner? Instance => _instance;

        #endregion

        #region Serialized Fields

        [Header("Wolf Settings")]
        [SerializeField] private GameObject? _wolfPrefab;

        [Header("Spawn Points")]
        [SerializeField] private Transform[]? _team0SpawnPoints;
        [SerializeField] private Transform[]? _team1SpawnPoints;

        #endregion

        #region Private Fields

        private Dictionary<ulong, NetworkObject> _spawnedWolves = new Dictionary<ulong, NetworkObject>();
        private HuntingRulesHandler? _huntingHandler;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[WolfSpawner] Instance already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public override void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            base.OnDestroy();
        }

        #endregion

        #region Network Spawn

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // ハンティングルールハンドラーを取得
            if (MatchManager.Instance?.ActiveHandler is HuntingRulesHandler handler)
            {
                _huntingHandler = handler;
            }

            Debug.Log($"[WolfSpawner] Network spawned. IsServer: {IsServer}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// すべてのウルフプレイヤーをスポーンします
        /// </summary>
        public void SpawnAllWolves()
        {
            if (!IsServer || _huntingHandler == null)
            {
                return;
            }

            Debug.Log("[WolfSpawner] Spawning all wolves...");

            int team0SpawnIndex = 0;
            int team1SpawnIndex = 0;

            foreach (var slot in MatchManager.Instance!.PlayerSlots)
            {
                if (_huntingHandler.IsWolf(slot.PlayerId))
                {
                    Transform? spawnPoint = null;

                    if (slot.TeamIndex == 0 && _team0SpawnPoints != null && _team0SpawnPoints.Length > 0)
                    {
                        spawnPoint = _team0SpawnPoints[team0SpawnIndex % _team0SpawnPoints.Length];
                        team0SpawnIndex++;
                    }
                    else if (slot.TeamIndex == 1 && _team1SpawnPoints != null && _team1SpawnPoints.Length > 0)
                    {
                        spawnPoint = _team1SpawnPoints[team1SpawnIndex % _team1SpawnPoints.Length];
                        team1SpawnIndex++;
                    }

                    if (spawnPoint != null)
                    {
                        SpawnWolfForPlayer(slot.PlayerId, slot.TeamIndex, spawnPoint.position, spawnPoint.rotation);
                    }
                }
            }
        }

        /// <summary>
        /// 特定のプレイヤーのウルフをスポーンします
        /// </summary>
        public void SpawnWolfForPlayer(ulong clientId, int teamIndex, Vector3 position, Quaternion rotation)
        {
            if (!IsServer || _wolfPrefab == null)
            {
                return;
            }

            // 既存のウルフがあれば削除
            if (_spawnedWolves.TryGetValue(clientId, out var existingWolf))
            {
                existingWolf.Despawn();
                _spawnedWolves.Remove(clientId);
            }

            // ウルフをスポーン
            var wolfInstance = Instantiate(_wolfPrefab, position, rotation);
            var networkObject = wolfInstance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId);

                // WolfControllerの設定
                var wolfController = wolfInstance.GetComponent<WolfController>();
                if (wolfController != null)
                {
                    wolfController.SetTeamRpc(teamIndex);
                }

                _spawnedWolves[clientId] = networkObject;

                Debug.Log($"[WolfSpawner] Spawned wolf for player {clientId} at {position}");
            }
            else
            {
                Debug.LogError("[WolfSpawner] Wolf prefab does not have NetworkObject component!");
                Destroy(wolfInstance);
            }
        }

        /// <summary>
        /// 特定のプレイヤーのウルフをデスポーンします
        /// </summary>
        public void DespawnWolf(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            if (_spawnedWolves.TryGetValue(clientId, out var wolf))
            {
                wolf.Despawn();
                _spawnedWolves.Remove(clientId);
                Debug.Log($"[WolfSpawner] Despawned wolf for player {clientId}");
            }
        }

        /// <summary>
        /// すべてのウルフをデスポーンします
        /// </summary>
        public void DespawnAllWolves()
        {
            if (!IsServer)
            {
                return;
            }

            foreach (var kvp in _spawnedWolves)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Despawn();
                }
            }

            _spawnedWolves.Clear();
            Debug.Log("[WolfSpawner] All wolves despawned");
        }

        /// <summary>
        /// ウルフをリスポーンします
        /// </summary>
        public void RespawnWolf(ulong clientId)
        {
            if (!IsServer || _huntingHandler == null)
            {
                return;
            }

            // プレイヤーのチームを取得
            int teamIndex = 0;
            foreach (var slot in MatchManager.Instance!.PlayerSlots)
            {
                if (slot.PlayerId == clientId)
                {
                    teamIndex = slot.TeamIndex;
                    break;
                }
            }

            // スポーンポイントを取得
            Transform? spawnPoint = null;
            if (teamIndex == 0 && _team0SpawnPoints != null && _team0SpawnPoints.Length > 0)
            {
                spawnPoint = _team0SpawnPoints[Random.Range(0, _team0SpawnPoints.Length)];
            }
            else if (teamIndex == 1 && _team1SpawnPoints != null && _team1SpawnPoints.Length > 0)
            {
                spawnPoint = _team1SpawnPoints[Random.Range(0, _team1SpawnPoints.Length)];
            }

            if (spawnPoint != null)
            {
                // 既存のウルフを削除してから再スポーン
                DespawnWolf(clientId);
                SpawnWolfForPlayer(clientId, teamIndex, spawnPoint.position, spawnPoint.rotation);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// 指定されたプレイヤーのウルフを取得します
        /// </summary>
        public WolfController? GetWolfController(ulong clientId)
        {
            if (_spawnedWolves.TryGetValue(clientId, out var wolf))
            {
                return wolf.GetComponent<WolfController>();
            }
            return null;
        }

        #endregion
    }
}
