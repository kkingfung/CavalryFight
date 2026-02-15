#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// フィールドプレハブをロードするコンポーネント
    /// </summary>
    /// <remarks>
    /// シーン開始時に指定されたフィールドプレハブをインスタンス化します。
    /// ロード完了後、SpawnManagerにSpawnPointの再検索を通知します。
    /// MapConfigを使用してマップ情報を一元管理します。
    /// </remarks>
    public class FieldLoader : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// ResourcesフォルダからMapConfigを読み込むパス
        /// </summary>
        private const string MAP_CONFIG_RESOURCE_PATH = "Settings/MapConfig";

        #endregion

        #region Serialized Fields

        [Header("Map Configuration")]
        [Tooltip("マップ設定（MapConfig ScriptableObject）。未設定の場合はResources/Settings/MapConfigから自動読み込み")]
        [SerializeField] private MapConfig? _mapConfig;

        [Header("Field Settings")]
        [Tooltip("デフォルトでロードするマップ")]
        [SerializeField] private MapName _defaultMapName = MapName.Arena;

        [Tooltip("デフォルトロードを有効にする")]
        [SerializeField] private bool _enableDefaultLoad = false;

        [Tooltip("デフォルトロードをスキップする（リプレイシーンなど外部からロードする場合）")]
        [SerializeField] private bool _skipDefaultLoad = false;

        [Tooltip("フィールドをインスタンス化する親Transform（空の場合はルート）")]
        [SerializeField] private Transform? _fieldParent;

        [Tooltip("フィールドのインスタンス化位置")]
        [SerializeField] private Vector3 _spawnPosition = Vector3.zero;

        [Tooltip("フィールドのインスタンス化回転")]
        [SerializeField] private Vector3 _spawnRotation = Vector3.zero;

        [Header("Debug")]
        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private GameObject? _loadedField;
        private MapName? _currentMapName;
        private bool _isReady = false; // NavMesh構築完了後にtrue

        #endregion

        #region Events

        /// <summary>
        /// フィールドのロードが完了した時に発生します
        /// </summary>
        public event EventHandler? FieldLoaded;

        #endregion

        #region Properties

        /// <summary>
        /// ロードされたフィールドのGameObjectを取得します
        /// </summary>
        public GameObject? LoadedField => _loadedField;

        /// <summary>
        /// フィールドがロード済みかどうかを取得します
        /// </summary>
        /// <remarks>
        /// NavMesh構築が完了するまでfalseを返します。
        /// AIスポーンはこのプロパティがtrueになるまで待つ必要があります。
        /// </remarks>
        public bool IsLoaded => _loadedField != null && _isReady;

        /// <summary>
        /// 現在ロードされているマップ名を取得します
        /// </summary>
        public MapName? CurrentMapName => _currentMapName;

        /// <summary>
        /// マップ設定を取得します
        /// </summary>
        public MapConfig? MapConfig => _mapConfig;

        /// <summary>
        /// 利用可能なマップ名のリストを取得します
        /// </summary>
        public IReadOnlyList<MapName> AvailableMapNames
        {
            get
            {
                if (_mapConfig != null)
                {
                    return _mapConfig.GetAllMapNames();
                }
                return new List<MapName>();
            }
        }

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static FieldLoader? Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FieldLoader] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // MapConfigが未設定の場合はResourcesから読み込み
            if (_mapConfig == null)
            {
                _mapConfig = Resources.Load<MapConfig>(MAP_CONFIG_RESOURCE_PATH);

                if (_mapConfig != null)
                {
                    if (_debugLog)
                    {
                        Debug.Log($"[FieldLoader] MapConfig を Resources/{MAP_CONFIG_RESOURCE_PATH} から読み込みました。");
                    }
                }
                else
                {
                    Debug.LogError($"[FieldLoader] MapConfig が見つかりません！Inspector で割り当てるか、Resources/{MAP_CONFIG_RESOURCE_PATH} に配置してください。");
                }
            }
        }

        private void Start()
        {
            // スキップフラグが設定されている場合はデフォルトロードをスキップ
            if (_skipDefaultLoad)
            {
                if (_debugLog)
                {
                    Debug.Log("[FieldLoader] デフォルトロードをスキップしました（外部からのロードを待機）");
                }
                return;
            }

            // デフォルトフィールドをロード
            if (_enableDefaultLoad)
            {
                LoadField(_defaultMapName);
            }
            else
            {
                if (_debugLog)
                {
                    Debug.Log("[FieldLoader] デフォルトロードが無効です。");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // ロードしたフィールドをクリーンアップ
            if (_loadedField != null)
            {
                Destroy(_loadedField);
                _loadedField = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// マップ名でフィールドをロードします
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <returns>ロードに成功したかどうか</returns>
        public bool LoadField(MapName mapName)
        {
            if (_mapConfig == null)
            {
                Debug.LogError("[FieldLoader] MapConfig が設定されていません！");
                return false;
            }

            // MapConfigからプレハブを取得
            GameObject? prefab = _mapConfig.GetPrefab(mapName);

            if (prefab == null)
            {
                Debug.LogError($"[FieldLoader] マップ '{mapName}' が見つかりません！利用可能: {string.Join(", ", AvailableMapNames)}");
                return false;
            }

            _currentMapName = mapName;
            return LoadFieldInternal(prefab);
        }

        /// <summary>
        /// インデックスでフィールドをロードします
        /// </summary>
        /// <param name="index">マップエントリーのインデックス</param>
        /// <returns>ロードに成功したかどうか</returns>
        public bool LoadFieldByIndex(int index)
        {
            if (_mapConfig == null)
            {
                Debug.LogError("[FieldLoader] MapConfig が設定されていません！");
                return false;
            }

            if (index < 0 || index >= _mapConfig.Count)
            {
                Debug.LogError($"[FieldLoader] インデックス {index} が範囲外です！（0-{_mapConfig.Count - 1}）");
                return false;
            }

            var entry = _mapConfig.Maps[index];
            if (entry.Prefab == null)
            {
                Debug.LogError($"[FieldLoader] インデックス {index} のプレハブが null です！");
                return false;
            }

            _currentMapName = entry.MapName;
            return LoadFieldInternal(entry.Prefab);
        }

        /// <summary>
        /// 指定したプレハブでフィールドをロードします
        /// </summary>
        /// <param name="fieldPrefab">ロードするフィールドプレハブ</param>
        /// <returns>ロードに成功したかどうか</returns>
        public bool LoadField(GameObject fieldPrefab)
        {
            if (fieldPrefab == null)
            {
                Debug.LogError("[FieldLoader] フィールドプレハブが null です！");
                return false;
            }

            return LoadFieldInternal(fieldPrefab);
        }

        /// <summary>
        /// フィールドをアンロードします
        /// </summary>
        public void UnloadField()
        {
            if (_loadedField != null)
            {
                Destroy(_loadedField);
                _loadedField = null;
                _currentMapName = null;
                _isReady = false;

                if (_debugLog)
                {
                    Debug.Log("[FieldLoader] フィールドをアンロードしました");
                }
            }
        }

        /// <summary>
        /// マップ名から表示名を取得します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <returns>表示名</returns>
        public string GetDisplayName(MapName mapName)
        {
            if (_mapConfig != null)
            {
                return _mapConfig.GetDisplayName(mapName);
            }
            return mapName.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 内部でフィールドをロードします
        /// </summary>
        /// <param name="prefab">ロードするプレハブ</param>
        /// <returns>ロードに成功したかどうか</returns>
        private bool LoadFieldInternal(GameObject prefab)
        {
            // 既存のフィールドがあれば削除
            if (_loadedField != null)
            {
                Destroy(_loadedField);
                _loadedField = null;
            }

            // 新しいフィールドロード開始時にリセット
            _isReady = false;

            // フィールドをインスタンス化
            Quaternion rotation = Quaternion.Euler(_spawnRotation);
            _loadedField = Instantiate(prefab, _spawnPosition, rotation, _fieldParent);
            _loadedField.name = prefab.name; // "(Clone)"を除去

            if (_debugLog)
            {
                Debug.Log($"[FieldLoader] フィールドをロードしました: {_currentMapName} (prefab: {prefab.name})");
            }

            // RuntimeNavMeshBuilderがあればNavMesh構築を待つ
            var navMeshBuilder = _loadedField.GetComponent<RuntimeNavMeshBuilder>();
            if (navMeshBuilder != null)
            {
                StartCoroutine(WaitForNavMeshAndNotify(navMeshBuilder));
            }
            else
            {
                // NavMeshBuilderがない場合は即座に通知
                NotifyFieldLoadComplete();
            }

            return true;
        }

        /// <summary>
        /// NavMesh構築を待ってからフィールドロード完了を通知します
        /// </summary>
        /// <param name="navMeshBuilder">NavMeshビルダー</param>
        private IEnumerator WaitForNavMeshAndNotify(RuntimeNavMeshBuilder navMeshBuilder)
        {
            // NavMesh構築完了を待つ（最大5秒）
            float timeout = 5f;
            float elapsed = 0f;

            while (!navMeshBuilder.IsBuilt && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (!navMeshBuilder.IsBuilt)
            {
                Debug.LogWarning($"[FieldLoader] NavMesh build timeout after {timeout}s, notifying anyway...");
            }

            NotifyFieldLoadComplete();
        }

        /// <summary>
        /// フィールドロード完了を通知します
        /// </summary>
        private void NotifyFieldLoadComplete()
        {
            // NavMesh構築完了 - IsLoadedがtrueになる
            _isReady = true;

            // SpawnManagerにSpawnPointの再検索を通知
            NotifySpawnManager();

            // イベント発火
            FieldLoaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// SpawnManagerにSpawnPointの再検索を通知します
        /// </summary>
        private void NotifySpawnManager()
        {
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.FindAllSpawnPoints();

                if (_debugLog)
                {
                    Debug.Log($"[FieldLoader] SpawnManager に通知しました。SpawnPoint数: {SpawnManager.Instance.SpawnPointCount}");
                }
            }
            else
            {
                if (_debugLog)
                {
                    Debug.LogWarning("[FieldLoader] SpawnManager が見つかりません。後で手動で FindAllSpawnPoints() を呼び出してください。");
                }
            }
        }

        #endregion
    }
}
