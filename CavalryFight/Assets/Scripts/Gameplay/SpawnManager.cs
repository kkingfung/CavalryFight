#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// スポーンマネージャー
    /// </summary>
    /// <remarks>
    /// シーン内のスポーンポイントを管理し、適切なスポーン位置を提供します。
    /// シングルトンパターンで実装されています。
    /// </remarks>
    public class SpawnManager : MonoBehaviour
    {
        #region Singleton

        private static SpawnManager? _instance;

        /// <summary>
        /// SpawnManagerのインスタンス
        /// </summary>
        public static SpawnManager? Instance => _instance;

        #endregion

        #region Serialized Fields

        [Header("Settings")]
        [Tooltip("起動時に自動でスポーンポイントを検索するか")]
        [SerializeField] private bool _autoFindSpawnPoints = true;

        [Tooltip("スポーン時のY軸オフセット")]
        [SerializeField] private float _spawnHeightOffset = 0.1f;

        #endregion

        #region Private Fields

        private List<SpawnPoint> _allSpawnPoints = new List<SpawnPoint>();
        private Dictionary<int, List<SpawnPoint>> _teamSpawnPoints = new Dictionary<int, List<SpawnPoint>>();
        private int _lastUsedIndex = -1;

        #endregion

        #region Properties

        /// <summary>
        /// 全スポーンポイント
        /// </summary>
        public IReadOnlyList<SpawnPoint> AllSpawnPoints => _allSpawnPoints;

        /// <summary>
        /// スポーンポイントの数
        /// </summary>
        public int SpawnPointCount => _allSpawnPoints.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SpawnManager] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_autoFindSpawnPoints)
            {
                FindAllSpawnPoints();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// シーン内の全スポーンポイントを検索します
        /// </summary>
        public void FindAllSpawnPoints()
        {
            _allSpawnPoints.Clear();
            _teamSpawnPoints.Clear();

            var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            _allSpawnPoints.AddRange(spawnPoints.OrderByDescending(sp => sp.Priority));

            // チーム別に分類
            foreach (var sp in _allSpawnPoints)
            {
                if (!_teamSpawnPoints.ContainsKey(sp.TeamId))
                {
                    _teamSpawnPoints[sp.TeamId] = new List<SpawnPoint>();
                }
                _teamSpawnPoints[sp.TeamId].Add(sp);
            }

            Debug.Log($"[SpawnManager] Found {_allSpawnPoints.Count} spawn points. Teams: {string.Join(", ", _teamSpawnPoints.Keys)}");
        }

        /// <summary>
        /// スポーンポイントを登録します
        /// </summary>
        /// <param name="spawnPoint">登録するスポーンポイント</param>
        public void RegisterSpawnPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint == null || _allSpawnPoints.Contains(spawnPoint))
            {
                return;
            }

            _allSpawnPoints.Add(spawnPoint);
            _allSpawnPoints = _allSpawnPoints.OrderByDescending(sp => sp.Priority).ToList();

            if (!_teamSpawnPoints.ContainsKey(spawnPoint.TeamId))
            {
                _teamSpawnPoints[spawnPoint.TeamId] = new List<SpawnPoint>();
            }
            _teamSpawnPoints[spawnPoint.TeamId].Add(spawnPoint);

            Debug.Log($"[SpawnManager] Registered spawn point: {spawnPoint.name}");
        }

        /// <summary>
        /// スポーンポイントの登録を解除します
        /// </summary>
        /// <param name="spawnPoint">解除するスポーンポイント</param>
        public void UnregisterSpawnPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint == null)
            {
                return;
            }

            _allSpawnPoints.Remove(spawnPoint);

            if (_teamSpawnPoints.ContainsKey(spawnPoint.TeamId))
            {
                _teamSpawnPoints[spawnPoint.TeamId].Remove(spawnPoint);
            }

            Debug.Log($"[SpawnManager] Unregistered spawn point: {spawnPoint.name}");
        }

        /// <summary>
        /// ランダムなスポーンポイントを取得します
        /// </summary>
        /// <returns>スポーンポイント（見つからない場合はnull）</returns>
        public SpawnPoint? GetRandomSpawnPoint()
        {
            if (_allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] No spawn points available.");
                return null;
            }

            return _allSpawnPoints[Random.Range(0, _allSpawnPoints.Count)];
        }

        /// <summary>
        /// チーム用のスポーンポイントを取得します
        /// </summary>
        /// <param name="teamId">チームID</param>
        /// <returns>スポーンポイント（見つからない場合はnull）</returns>
        public SpawnPoint? GetSpawnPointForTeam(int teamId)
        {
            if (_teamSpawnPoints.TryGetValue(teamId, out var teamPoints) && teamPoints.Count > 0)
            {
                return teamPoints[Random.Range(0, teamPoints.Count)];
            }

            // チーム専用がなければ共通（teamId=0）から取得
            if (teamId != 0 && _teamSpawnPoints.TryGetValue(0, out var commonPoints) && commonPoints.Count > 0)
            {
                return commonPoints[Random.Range(0, commonPoints.Count)];
            }

            Debug.LogWarning($"[SpawnManager] No spawn point found for team {teamId}.");
            return GetRandomSpawnPoint();
        }

        /// <summary>
        /// 次のスポーンポイントを順番に取得します（ラウンドロビン）
        /// </summary>
        /// <returns>スポーンポイント（見つからない場合はnull）</returns>
        public SpawnPoint? GetNextSpawnPoint()
        {
            if (_allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[SpawnManager] No spawn points available.");
                return null;
            }

            _lastUsedIndex = (_lastUsedIndex + 1) % _allSpawnPoints.Count;
            return _allSpawnPoints[_lastUsedIndex];
        }

        /// <summary>
        /// トレーニング用のスポーンポイントを取得します
        /// </summary>
        /// <returns>スポーンポイント（見つからない場合はnull）</returns>
        public SpawnPoint? GetTrainingSpawnPoint()
        {
            var trainingPoints = _allSpawnPoints.Where(sp => sp.TrainingOnly).ToList();

            if (trainingPoints.Count > 0)
            {
                return trainingPoints[Random.Range(0, trainingPoints.Count)];
            }

            // トレーニング専用がなければ通常のスポーンポイントを使用
            return GetRandomSpawnPoint();
        }

        /// <summary>
        /// 指定位置から最も近いスポーンポイントを取得します
        /// </summary>
        /// <param name="position">基準位置</param>
        /// <returns>最も近いスポーンポイント（見つからない場合はnull）</returns>
        public SpawnPoint? GetNearestSpawnPoint(Vector3 position)
        {
            if (_allSpawnPoints.Count == 0)
            {
                return null;
            }

            return _allSpawnPoints
                .OrderBy(sp => Vector3.Distance(sp.Position, position))
                .First();
        }

        /// <summary>
        /// 指定位置から最も遠いスポーンポイントを取得します
        /// </summary>
        /// <param name="position">基準位置</param>
        /// <returns>最も遠いスポーンポイント（見つからない場合はnull）</returns>
        public SpawnPoint? GetFarthestSpawnPoint(Vector3 position)
        {
            if (_allSpawnPoints.Count == 0)
            {
                return null;
            }

            return _allSpawnPoints
                .OrderByDescending(sp => Vector3.Distance(sp.Position, position))
                .First();
        }

        /// <summary>
        /// スポーン位置を取得します（高さオフセット込み）
        /// </summary>
        /// <param name="spawnPoint">スポーンポイント</param>
        /// <returns>スポーン位置</returns>
        public Vector3 GetSpawnPosition(SpawnPoint spawnPoint)
        {
            if (spawnPoint == null)
            {
                return Vector3.zero;
            }

            return spawnPoint.Position + Vector3.up * _spawnHeightOffset;
        }

        #endregion
    }
}
