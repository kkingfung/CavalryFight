#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// フィールドプレハブをロードするコンポーネント
    /// </summary>
    /// <remarks>
    /// シーン開始時に指定されたフィールドプレハブをインスタンス化します。
    /// ロード完了後、SpawnManagerにSpawnPointの再検索を通知します。
    /// 複数のフィールドプレハブを登録し、名前で切り替えることができます。
    /// </remarks>
    public class FieldLoader : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Field Settings")]
        [Tooltip("利用可能なフィールドプレハブのリスト")]
        [SerializeField] private List<GameObject> _fieldPrefabs = new List<GameObject>();

        [Tooltip("デフォルトでロードするフィールドのインデックス（-1 = 自動ロードしない）")]
        [SerializeField] private int _defaultFieldIndex = 0;

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
        private string? _currentFieldName;

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
        public bool IsLoaded => _loadedField != null;

        /// <summary>
        /// 現在ロードされているフィールド名を取得します
        /// </summary>
        public string? CurrentFieldName => _currentFieldName;

        /// <summary>
        /// 利用可能なフィールド名のリストを取得します
        /// </summary>
        public IReadOnlyList<string> AvailableFieldNames
        {
            get
            {
                var names = new List<string>();
                foreach (var prefab in _fieldPrefabs)
                {
                    if (prefab != null)
                    {
                        names.Add(prefab.name);
                    }
                }
                return names;
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
        }

        private void Start()
        {
            // デフォルトフィールドをロード
            if (_defaultFieldIndex >= 0 && _defaultFieldIndex < _fieldPrefabs.Count)
            {
                LoadFieldByIndex(_defaultFieldIndex);
            }
            else if (_defaultFieldIndex >= 0)
            {
                Debug.LogWarning($"[FieldLoader] デフォルトフィールドインデックス {_defaultFieldIndex} が範囲外です。");
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
        /// 名前でフィールドをロードします
        /// </summary>
        /// <param name="fieldName">フィールドプレハブの名前</param>
        /// <returns>ロードに成功したかどうか</returns>
        public bool LoadFieldByName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                Debug.LogError("[FieldLoader] フィールド名が空です！");
                return false;
            }

            // 名前でプレハブを検索
            GameObject? prefab = null;
            foreach (var p in _fieldPrefabs)
            {
                if (p != null && p.name == fieldName)
                {
                    prefab = p;
                    break;
                }
            }

            if (prefab == null)
            {
                Debug.LogError($"[FieldLoader] フィールド '{fieldName}' が見つかりません！利用可能: {string.Join(", ", AvailableFieldNames)}");
                return false;
            }

            return LoadFieldInternal(prefab);
        }

        /// <summary>
        /// インデックスでフィールドをロードします
        /// </summary>
        /// <param name="index">フィールドプレハブのインデックス</param>
        /// <returns>ロードに成功したかどうか</returns>
        public bool LoadFieldByIndex(int index)
        {
            if (index < 0 || index >= _fieldPrefabs.Count)
            {
                Debug.LogError($"[FieldLoader] インデックス {index} が範囲外です！（0-{_fieldPrefabs.Count - 1}）");
                return false;
            }

            var prefab = _fieldPrefabs[index];
            if (prefab == null)
            {
                Debug.LogError($"[FieldLoader] インデックス {index} のプレハブが null です！");
                return false;
            }

            return LoadFieldInternal(prefab);
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
                _currentFieldName = null;

                if (_debugLog)
                {
                    Debug.Log("[FieldLoader] フィールドをアンロードしました");
                }
            }
        }

        /// <summary>
        /// フィールドプレハブを登録します
        /// </summary>
        /// <param name="prefab">登録するプレハブ</param>
        public void RegisterFieldPrefab(GameObject prefab)
        {
            if (prefab != null && !_fieldPrefabs.Contains(prefab))
            {
                _fieldPrefabs.Add(prefab);

                if (_debugLog)
                {
                    Debug.Log($"[FieldLoader] フィールドプレハブを登録しました: {prefab.name}");
                }
            }
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

            // フィールドをインスタンス化
            Quaternion rotation = Quaternion.Euler(_spawnRotation);
            _loadedField = Instantiate(prefab, _spawnPosition, rotation, _fieldParent);
            _loadedField.name = prefab.name; // "(Clone)"を除去
            _currentFieldName = prefab.name;

            if (_debugLog)
            {
                Debug.Log($"[FieldLoader] フィールドをロードしました: {_loadedField.name}");
            }

            // SpawnManagerにSpawnPointの再検索を通知
            NotifySpawnManager();

            // イベント発火
            FieldLoaded?.Invoke(this, EventArgs.Empty);

            return true;
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
