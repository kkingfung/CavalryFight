#nullable enable

using System;
using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using MalbersAnimations.Controller;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// プレイヤーのスポーンを管理するコンポーネント
    /// </summary>
    /// <remarks>
    /// シーン開始時にPlayerMountとPlayerRiderをスポーンし、
    /// ICustomizationServiceからカスタマイズを適用します。
    /// トレーニングシーンとマッチシーンで使用します。
    /// FieldLoaderがある場合は、フィールドのロード完了を待ってからスポーンします。
    /// </remarks>
    public class PlayerSpawner : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Prefabs")]
        [Tooltip("馬のプレハブ（PlayerMount）")]
        [SerializeField] private GameObject? _mountPrefab;

        [Tooltip("騎手のプレハブ（PlayerRider）")]
        [SerializeField] private GameObject? _riderPrefab;

        [Header("Spawn Settings")]
        [Tooltip("スポーンポイント（未設定の場合はSpawnManagerから取得）")]
        [SerializeField] private SpawnPoint? _spawnPoint;

        [Tooltip("シーン開始時に自動スポーンするか")]
        [SerializeField] private bool _autoSpawnOnStart = true;

        [Tooltip("FieldLoaderのロード完了を待つか")]
        [SerializeField] private bool _waitForFieldLoader = true;

        [Header("Tracker")]
        [Tooltip("AnimalTrackerのプレハブ")]
        [SerializeField] private GameObject? _animalTrackerPrefab;

        [Header("Debug")]
        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private ICustomizationService? _customizationService;
        private GameObject? _spawnedMount;
        private GameObject? _spawnedRider;
        private GameObject? _spawnedTracker;

        #endregion

        #region Events

        /// <summary>
        /// プレイヤーのスポーンが完了した時に発生します
        /// </summary>
        public event EventHandler? PlayerSpawned;

        #endregion

        #region Properties

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static PlayerSpawner? Instance { get; private set; }

        /// <summary>
        /// スポーンされた馬を取得します
        /// </summary>
        public GameObject? SpawnedMount => _spawnedMount;

        /// <summary>
        /// スポーンされた騎手を取得します
        /// </summary>
        public GameObject? SpawnedRider => _spawnedRider;

        /// <summary>
        /// プレイヤーがスポーン済みかどうかを取得します
        /// </summary>
        public bool IsSpawned => _spawnedMount != null && _spawnedRider != null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PlayerSpawner] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _customizationService = ServiceLocator.Instance.Get<ICustomizationService>();

            if (_customizationService == null)
            {
                Debug.LogWarning("[PlayerSpawner] ICustomizationService が取得できませんでした。カスタマイズは適用されません。");
            }
        }

        private void Start()
        {
            if (_autoSpawnOnStart)
            {
                TrySpawnPlayer();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // FieldLoaderのイベント購読を解除
            if (FieldLoader.Instance != null)
            {
                FieldLoader.Instance.FieldLoaded -= OnFieldLoaded;
            }

            // スポーンしたオブジェクトをクリーンアップ
            if (_spawnedMount != null)
            {
                Destroy(_spawnedMount);
            }

            if (_spawnedRider != null)
            {
                Destroy(_spawnedRider);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// フィールドのロード状態を確認してスポーンを試みます
        /// </summary>
        private void TrySpawnPlayer()
        {
            // FieldLoaderがない、または待機しない設定の場合は即座にスポーン
            if (!_waitForFieldLoader || FieldLoader.Instance == null)
            {
                SpawnPlayer();
                return;
            }

            // フィールドが既にロード済みの場合
            if (FieldLoader.Instance.IsLoaded)
            {
                SpawnPlayer();
                return;
            }

            // フィールドのロード完了を待つ
            FieldLoader.Instance.FieldLoaded += OnFieldLoaded;

            if (_debugLog)
            {
                Debug.Log("[PlayerSpawner] FieldLoader のロード完了を待機しています...");
            }
        }

        /// <summary>
        /// フィールドロード完了時のコールバック
        /// </summary>
        private void OnFieldLoaded(object? sender, EventArgs e)
        {
            if (FieldLoader.Instance != null)
            {
                FieldLoader.Instance.FieldLoaded -= OnFieldLoaded;
            }

            if (_debugLog)
            {
                Debug.Log("[PlayerSpawner] FieldLoader のロードが完了しました。プレイヤーをスポーンします。");
            }

            SpawnPlayer();
        }

        /// <summary>
        /// プレイヤー（馬と騎手）をスポーンします
        /// </summary>
        /// <returns>スポーンに成功したかどうか</returns>
        public bool SpawnPlayer()
        {
            return SpawnPlayer(null);
        }

        /// <summary>
        /// 指定したスポーンポイントにプレイヤーをスポーンします
        /// </summary>
        /// <param name="spawnPoint">スポーンポイント（nullの場合はデフォルトを使用）</param>
        /// <returns>スポーンに成功したかどうか</returns>
        public bool SpawnPlayer(SpawnPoint? spawnPoint)
        {
            // プレハブのチェック
            if (_mountPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] MountPrefab が設定されていません！");
                return false;
            }

            if (_riderPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] RiderPrefab が設定されていません！");
                return false;
            }

            // スポーンポイントを取得
            SpawnPoint? targetSpawnPoint = spawnPoint ?? _spawnPoint ?? GetSpawnPointFromManager();

            if (targetSpawnPoint == null)
            {
                Debug.LogError("[PlayerSpawner] スポーンポイントが見つかりません！");
                return false;
            }

            // 既存のスポーンオブジェクトを削除
            CleanupSpawnedObjects();

            // 馬をスポーン
            _spawnedMount = SpawnMount(targetSpawnPoint);
            if (_spawnedMount == null)
            {
                return false;
            }

            // 騎手をスポーン
            _spawnedRider = SpawnRider(targetSpawnPoint, _spawnedMount);
            if (_spawnedRider == null)
            {
                Destroy(_spawnedMount);
                _spawnedMount = null;
                return false;
            }

            // カスタマイズを適用
            ApplyCustomization();

            if (_debugLog)
            {
                Debug.Log($"[PlayerSpawner] プレイヤーをスポーンしました: {targetSpawnPoint.name}");
            }

            // スポーン完了イベントを発火
            PlayerSpawned?.Invoke(this, EventArgs.Empty);

            // CameraManagerにターゲットを設定
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetTargetFromSpawner(this);
            }

            // AnimalTrackerにライダーのMAnimalを設定
            AssignAnimalTracker();

            return true;
        }

        /// <summary>
        /// スポーンしたオブジェクトを削除して再スポーンします
        /// </summary>
        /// <returns>スポーンに成功したかどうか</returns>
        public bool Respawn()
        {
            CleanupSpawnedObjects();
            return SpawnPlayer();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// SpawnManagerからスポーンポイントを取得します
        /// </summary>
        /// <returns>スポーンポイント</returns>
        private SpawnPoint? GetSpawnPointFromManager()
        {
            if (SpawnManager.Instance == null)
            {
                Debug.LogWarning("[PlayerSpawner] SpawnManager が見つかりません。");
                return null;
            }

            // トレーニングシーンの場合はトレーニング用スポーンポイントを使用
            SpawnPoint? spawnPoint = SpawnManager.Instance.GetTrainingSpawnPoint();

            if (spawnPoint == null)
            {
                // トレーニング用がなければランダムに取得
                spawnPoint = SpawnManager.Instance.GetRandomSpawnPoint();
            }

            return spawnPoint;
        }

        /// <summary>
        /// 馬をスポーンします
        /// </summary>
        /// <param name="spawnPoint">スポーンポイント</param>
        /// <returns>スポーンした馬のGameObject</returns>
        private GameObject? SpawnMount(SpawnPoint spawnPoint)
        {
            if (_mountPrefab == null)
            {
                return null;
            }

            Vector3 position = spawnPoint.Position;
            Quaternion rotation = spawnPoint.Rotation;

            GameObject mount = Instantiate(_mountPrefab, position, rotation);
            mount.name = "PlayerMount";

            // スポーンしたオブジェクトをPlayerSpawnerと同じシーンに移動
            // （LoadingScreen等の一時シーンでスポーンされた場合の対策）
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mount, gameObject.scene);

            if (_debugLog)
            {
                Debug.Log($"[PlayerSpawner] 馬をスポーン: {position} (Scene: {gameObject.scene.name})");
            }

            return mount;
        }

        /// <summary>
        /// 騎手をスポーンします
        /// </summary>
        /// <param name="spawnPoint">スポーンポイント</param>
        /// <param name="mount">騎乗する馬</param>
        /// <returns>スポーンした騎手のGameObject</returns>
        private GameObject? SpawnRider(SpawnPoint spawnPoint, GameObject mount)
        {
            if (_riderPrefab == null)
            {
                return null;
            }

            // MountControllerからMountPointを取得
            var mountController = mount.GetComponent<MountController>();
            Transform? mountPoint = mountController?.MountPoint ?? FindMountPoint(mount);

            Vector3 position;
            Quaternion rotation;

            if (mountPoint != null)
            {
                position = mountPoint.position;
                rotation = mountPoint.rotation;
            }
            else
            {
                // MountPointがない場合は馬の上に配置
                position = mount.transform.position + Vector3.up * 1.5f;
                rotation = mount.transform.rotation;
            }

            GameObject rider = Instantiate(_riderPrefab, position, rotation);
            rider.name = "PlayerRider";

            // スポーンしたオブジェクトをPlayerSpawnerと同じシーンに移動
            // （LoadingScreen等の一時シーンでスポーンされた場合の対策）
            // 注意: RiderはMountの子になるので、Mountが正しいシーンにあれば自動的に移動される
            // ただし、MountTo()前に明示的に移動しておく
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rider, gameObject.scene);

            // RiderControllerを使用して騎乗
            var riderController = rider.GetComponent<RiderController>();
            if (riderController != null && mountPoint != null)
            {
                riderController.MountTo(mountPoint);
            }
            else
            {
                // RiderControllerがない場合は手動で親子関係を設定
                rider.transform.SetParent(mount.transform);
                rider.transform.localPosition = mountPoint != null ? Vector3.zero : Vector3.up * 1.5f;
                rider.transform.localRotation = Quaternion.identity;
            }

            // MountControllerに騎手が乗ったことを通知
            mountController?.OnRiderMounted();

            if (_debugLog)
            {
                Debug.Log($"[PlayerSpawner] 騎手をスポーン＆騎乗: {position}");
            }

            return rider;
        }

        /// <summary>
        /// 馬のMountPointを探します
        /// </summary>
        /// <param name="mount">馬のGameObject</param>
        /// <returns>MountPointのTransform</returns>
        private Transform? FindMountPoint(GameObject mount)
        {
            // 一般的なMountPoint名を検索
            string[] mountPointNames = { "MountPoint", "Mount Point", "Seat", "RiderSeat" };

            foreach (string name in mountPointNames)
            {
                Transform? point = mount.transform.Find(name);
                if (point != null)
                {
                    return point;
                }
            }

            // 子オブジェクトを再帰的に検索
            foreach (string name in mountPointNames)
            {
                Transform? point = FindChildRecursive(mount.transform, name);
                if (point != null)
                {
                    return point;
                }
            }

            return null;
        }

        /// <summary>
        /// 子オブジェクトを再帰的に検索します
        /// </summary>
        /// <param name="parent">親Transform</param>
        /// <param name="name">検索する名前</param>
        /// <returns>見つかったTransform</returns>
        private Transform? FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                {
                    return child;
                }

                Transform? found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// カスタマイズを適用します
        /// </summary>
        private void ApplyCustomization()
        {
            if (_customizationService == null)
            {
                if (_debugLog)
                {
                    Debug.Log("[PlayerSpawner] CustomizationService がないため、カスタマイズをスキップします。");
                }
                return;
            }

            // 馬にカスタマイズを適用
            if (_spawnedMount != null)
            {
                // MountControllerからカスタマイズ対象を取得
                var mountController = _spawnedMount.GetComponent<MountController>();
                GameObject mountTarget = mountController != null
                    ? mountController.GetCustomizationTarget()
                    : _spawnedMount;

                bool mountSuccess = _customizationService.ApplyMountCustomization(mountTarget);
                if (_debugLog)
                {
                    Debug.Log($"[PlayerSpawner] 馬のカスタマイズ適用: {(mountSuccess ? "成功" : "失敗")} (対象: {mountTarget.name})");
                }
            }

            // 騎手にカスタマイズを適用
            if (_spawnedRider != null)
            {
                // RiderControllerからカスタマイズ対象を取得
                var riderController = _spawnedRider.GetComponent<RiderController>();
                GameObject riderTarget = riderController != null
                    ? riderController.GetCustomizationTarget()
                    : FindP09Human(_spawnedRider.transform)?.gameObject ?? _spawnedRider;

                bool riderSuccess = _customizationService.ApplyCharacterCustomization(riderTarget);
                if (_debugLog)
                {
                    Debug.Log($"[PlayerSpawner] 騎手のカスタマイズ適用: {(riderSuccess ? "成功" : "失敗")} (対象: {riderTarget.name})");
                }
            }
        }

        /// <summary>
        /// P09_Humanオブジェクトを探します
        /// </summary>
        /// <param name="parent">親Transform</param>
        /// <returns>P09_HumanのTransform</returns>
        private Transform? FindP09Human(Transform parent)
        {
            // P09を含むオブジェクトを検索（Track等のMalbersエフェクトを除外）
            return FindP09ChildRecursive(parent);
        }

        /// <summary>
        /// P09キャラクターを再帰的に検索します
        /// </summary>
        /// <param name="parent">親Transform</param>
        /// <returns>見つかったTransform</returns>
        private Transform? FindP09ChildRecursive(Transform parent)
        {
            // 直接の子を検索
            foreach (Transform child in parent)
            {
                // P09を含み、Track/Effect/Particleを含まないオブジェクトを探す
                if (child.name.Contains("P09") &&
                    !child.name.Contains("Track") &&
                    !child.name.Contains("Effect") &&
                    !child.name.Contains("Particle"))
                {
                    return child;
                }
            }

            // 再帰的に検索
            foreach (Transform child in parent)
            {
                // Track/Effect/Particleを含む子はスキップ
                if (child.name.Contains("Track") ||
                    child.name.Contains("Effect") ||
                    child.name.Contains("Particle"))
                {
                    continue;
                }

                Transform? found = FindP09ChildRecursive(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// AnimalTrackerをスポーンしてライダーのMAnimalを設定します
        /// </summary>
        private void AssignAnimalTracker()
        {
            if (_animalTrackerPrefab == null)
            {
                if (_debugLog)
                {
                    Debug.Log("[PlayerSpawner] AnimalTrackerプレハブ が設定されていません。スキップします。");
                }
                return;
            }

            if (_spawnedRider == null)
            {
                Debug.LogWarning("[PlayerSpawner] ライダーがスポーンされていないため、AnimalTracker を設定できません。");
                return;
            }

            // ライダーからMAnimalコンポーネントを取得
            var mAnimal = _spawnedRider.GetComponent<MAnimal>();
            if (mAnimal == null)
            {
                // 子オブジェクトから検索
                mAnimal = _spawnedRider.GetComponentInChildren<MAnimal>();
            }

            if (mAnimal == null)
            {
                Debug.LogWarning("[PlayerSpawner] ライダーに MAnimal コンポーネントが見つかりません。");
                return;
            }

            // 既存のトラッカーを削除
            if (_spawnedTracker != null)
            {
                Destroy(_spawnedTracker);
            }

            // プレハブを非アクティブ状態でインスタンス化（OnEnableが先に走るのを防ぐ）
            _animalTrackerPrefab.SetActive(false);
            _spawnedTracker = Instantiate(_animalTrackerPrefab);
            _animalTrackerPrefab.SetActive(true); // プレハブを元に戻す

            _spawnedTracker.name = "AnimalTracker";

            // 同じシーンに移動（ルートオブジェクトとして配置し、トラッキングで追従させる）
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_spawnedTracker, gameObject.scene);

            // ライダーの位置に初期配置
            _spawnedTracker.transform.position = _spawnedRider.transform.position;
            _spawnedTracker.transform.rotation = _spawnedRider.transform.rotation;

            // AnimalTrackerコンポーネントを取得してMAnimalを設定
            var animalTracker = _spawnedTracker.GetComponent<AnimalTracker>();
            if (animalTracker != null)
            {
                animalTracker.animal = mAnimal;

                // Trackerフィールドがnullの場合、子オブジェクトから探すか新規作成
                if (animalTracker.Tracker == null)
                {
                    // 既存の子Transformを探す（Tracker, TrackerPoint等の名前）
                    Transform? trackerTransform = _spawnedTracker.transform.Find("Tracker");
                    if (trackerTransform == null)
                    {
                        trackerTransform = _spawnedTracker.transform.Find("TrackerPoint");
                    }

                    // 見つからない場合は新規作成
                    if (trackerTransform == null)
                    {
                        var trackerObj = new GameObject("Tracker");
                        trackerObj.transform.SetParent(_spawnedTracker.transform);
                        trackerObj.transform.localPosition = Vector3.zero;
                        trackerObj.transform.localRotation = Quaternion.identity;
                        trackerTransform = trackerObj.transform;

                        if (_debugLog)
                        {
                            Debug.Log("[PlayerSpawner] AnimalTracker 用の Tracker Transform を新規作成しました。");
                        }
                    }

                    animalTracker.Tracker = trackerTransform;
                }

                // Trackersリスト内のRelativeToがnullの場合、ライダーのTransformを設定
                foreach (var tracker in animalTracker.Trackers)
                {
                    if (tracker.RelativeTo == null)
                    {
                        tracker.RelativeTo = _spawnedRider.transform;
                    }
                }

                if (_debugLog)
                {
                    Debug.Log($"[PlayerSpawner] AnimalTracker をスポーンし、MAnimal を設定しました: {mAnimal.name}");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerSpawner] スポーンしたプレハブに AnimalTracker コンポーネントがありません。");
            }

            // MAnimal設定後にアクティブ化
            _spawnedTracker.SetActive(true);
        }

        /// <summary>
        /// スポーンしたオブジェクトを削除します
        /// </summary>
        private void CleanupSpawnedObjects()
        {
            if (_spawnedMount != null)
            {
                Destroy(_spawnedMount);
                _spawnedMount = null;
            }

            if (_spawnedRider != null)
            {
                Destroy(_spawnedRider);
                _spawnedRider = null;
            }

            if (_spawnedTracker != null)
            {
                Destroy(_spawnedTracker);
                _spawnedTracker = null;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// エディタでのギズモ描画
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_spawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_spawnPoint.Position, 1f);
                Gizmos.DrawLine(transform.position, _spawnPoint.Position);
            }
        }
#endif

        #endregion
    }
}
