#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using CavalryFight.Services.Lobby;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ再生を管理するコンポーネント
    /// </summary>
    /// <remarks>
    /// ReplayDataからフィールドとエンティティをスポーンし、
    /// フレームデータに基づいて位置・回転を更新します。
    /// 実際のプレハブとカスタマイズシステムを使用してリプレイを再現します。
    /// </remarks>
    public class ReplayPlaybackManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Prefabs")]
        [Tooltip("馬のプレハブ")]
        [SerializeField] private GameObject? _mountPrefab;

        [Tooltip("騎手のプレハブ")]
        [SerializeField] private GameObject? _riderPrefab;

        [Tooltip("ウルフのプレハブ（ハンティングモード用、サードパーティアセット）")]
        [SerializeField] private GameObject? _wolfPrefab;

        [Header("Settings")]
        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private IReplayService? _replayService;
        private ICustomizationService? _customizationService;
        private P09CharacterApplier? _characterApplier;
        private MalbersHorseApplier? _mountApplier;
        private ReplayData? _currentReplay;
        private readonly Dictionary<string, SpawnedEntity> _spawnedEntities = new();
        private readonly HashSet<int> _invalidAnimationHashes = new();
        private readonly HashSet<string> _invalidAnimatorParams = new();
        private Transform? _entityContainer;
        private bool _isInitialized;

        #endregion

        #region Nested Types

        /// <summary>
        /// スポーンされたエンティティの情報
        /// </summary>
        private class SpawnedEntity
        {
            public GameObject? Root;
            public GameObject? Rider;
            public GameObject? Mount;
            public GameObject? Wolf;
            public Animator? RiderAnimator;
            public Animator? MountAnimator;
            public Animator? WolfAnimator;
            public EntityType EntityType = EntityType.Unknown;
        }

        #endregion

        #region Properties

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static ReplayPlaybackManager? Instance { get; private set; }

        /// <summary>
        /// 現在のリプレイデータ
        /// </summary>
        public ReplayData? CurrentReplay => _currentReplay;

        /// <summary>
        /// 初期化済みかどうか
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ReplayPlaybackManager] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _replayService = ServiceLocator.Instance.Get<IReplayService>();
            _customizationService = ServiceLocator.Instance.Get<ICustomizationService>();

            // Applierを取得
            _characterApplier = _customizationService?.GetP09CharacterApplier();
            _mountApplier = _customizationService?.GetMalbersHorseApplier();

            if (_characterApplier == null && _debugLog)
            {
                Debug.LogWarning("[ReplayPlaybackManager] P09CharacterApplier が取得できませんでした。");
            }
            if (_mountApplier == null && _debugLog)
            {
                Debug.LogWarning("[ReplayPlaybackManager] MalbersHorseApplier が取得できませんでした。");
            }

            // エンティティコンテナを作成
            var containerObj = new GameObject("ReplayEntities");
            _entityContainer = containerObj.transform;
            _entityContainer.SetParent(transform);
        }

        private void Start()
        {
            LoadReplayData();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            CleanupEntities();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// リプレイデータを読み込んでエンティティをスポーンします
        /// </summary>
        public void LoadReplayData()
        {
            if (_replayService == null)
            {
                Debug.LogError("[ReplayPlaybackManager] ReplayService が取得できませんでした。");
                return;
            }

            _currentReplay = _replayService.CurrentReplay;

            if (_currentReplay == null)
            {
                Debug.LogWarning("[ReplayPlaybackManager] CurrentReplay が null です。リプレイが選択されていません。");
                return;
            }

            if (_debugLog)
            {
                Debug.Log($"[ReplayPlaybackManager] リプレイを読み込みました: {_currentReplay.MapName} - {_currentReplay.GameMode}");
                Debug.Log($"  フレーム数: {_currentReplay.Frames.Count}, イベント数: {_currentReplay.Events.Count}");
            }

            // フィールドをロード
            LoadField(_currentReplay.MapName);

            // エンティティをスポーン
            SpawnAllEntities();

            _isInitialized = true;
        }

        /// <summary>
        /// 指定した時間のフレームを適用します
        /// </summary>
        /// <param name="time">再生時間（秒）</param>
        public void ApplyFrameAtTime(float time)
        {
            if (_currentReplay == null || !_isInitialized)
            {
                return;
            }

            // 補間されたフレームを取得
            ReplayFrame? frame = _currentReplay.GetInterpolatedFrame(time);

            if (frame == null)
            {
                return;
            }

            ApplyFrame(frame);
        }

        /// <summary>
        /// フレームを適用してエンティティの位置・回転を更新します
        /// </summary>
        /// <param name="frame">適用するフレーム</param>
        public void ApplyFrame(ReplayFrame frame)
        {
            foreach (var snapshot in frame.Entities)
            {
                if (_spawnedEntities.TryGetValue(snapshot.EntityId, out SpawnedEntity entity))
                {
                    ApplySnapshotToEntity(snapshot, entity);
                }
            }
        }

        /// <summary>
        /// すべてのエンティティをクリーンアップします
        /// </summary>
        public void CleanupEntities()
        {
            foreach (var kvp in _spawnedEntities)
            {
                if (kvp.Value.Root != null)
                {
                    Destroy(kvp.Value.Root);
                }
            }

            _spawnedEntities.Clear();
            _invalidAnimationHashes.Clear();
            _invalidAnimatorParams.Clear();
            _isInitialized = false;

            if (_debugLog)
            {
                Debug.Log("[ReplayPlaybackManager] エンティティをクリーンアップしました。");
            }
        }

        /// <summary>
        /// リプレイをリロードします
        /// </summary>
        public void ReloadReplay()
        {
            CleanupEntities();
            LoadReplayData();
        }

        #endregion

        #region Events

        /// <summary>
        /// フィールドのロードが要求された時に発生します
        /// </summary>
        /// <remarks>
        /// FieldLoaderなどの外部コンポーネントがこのイベントを購読して
        /// 実際のフィールドロードを行います。
        /// </remarks>
        public static event Action<MapName>? FieldLoadRequested;

        #endregion

        #region Private Methods - Field Loading

        /// <summary>
        /// フィールドのロードを要求します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        private void LoadField(MapName mapName)
        {
            if (FieldLoadRequested != null)
            {
                FieldLoadRequested.Invoke(mapName);

                if (_debugLog)
                {
                    Debug.Log($"[ReplayPlaybackManager] フィールドロードを要求しました: {mapName}");
                }
            }
            else
            {
                Debug.LogWarning("[ReplayPlaybackManager] FieldLoadRequested イベントの購読者がいません。フィールドはロードされません。");
            }
        }

        #endregion

        #region Private Methods - Entity Spawning

        /// <summary>
        /// すべてのエンティティをスポーンします
        /// </summary>
        private void SpawnAllEntities()
        {
            if (_currentReplay == null)
            {
                return;
            }

            // プレイヤーをスポーン
            SpawnPlayerEntity();

            // 敵をスポーン
            foreach (var enemyCustomization in _currentReplay.Enemies)
            {
                SpawnEnemyEntity(enemyCustomization);
            }

            // ウルフをスポーン（ハンティングモード）
            foreach (var wolfCustomization in _currentReplay.Wolves)
            {
                SpawnWolfEntity(wolfCustomization);
            }

            // その他のエンティティをスポーン
            foreach (var otherCustomization in _currentReplay.OtherEntities)
            {
                SpawnOtherEntity(otherCustomization);
            }

            if (_debugLog)
            {
                Debug.Log($"[ReplayPlaybackManager] {_spawnedEntities.Count} 個のエンティティをスポーンしました。");
            }
        }

        /// <summary>
        /// プレイヤーエンティティをスポーンします
        /// </summary>
        private void SpawnPlayerEntity()
        {
            if (_currentReplay == null || _mountPrefab == null || _riderPrefab == null)
            {
                Debug.LogWarning("[ReplayPlaybackManager] プレハブが設定されていないため、プレイヤーをスポーンできません。");
                return;
            }

            // 初期位置を取得（最初のフレームから）
            Vector3 playerPos = Vector3.zero;
            Vector3 mountPos = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            if (_currentReplay.Frames.Count > 0)
            {
                var firstFrame = _currentReplay.Frames[0];
                foreach (var snapshot in firstFrame.Entities)
                {
                    if (snapshot.EntityId == "player_0")
                    {
                        playerPos = snapshot.Position;
                        rotation = snapshot.Rotation;
                    }
                    else if (snapshot.EntityId == "mount_0")
                    {
                        mountPos = snapshot.Position;
                    }
                }
            }

            // 馬をスポーン
            var mount = SpawnMount(mountPos, rotation, _currentReplay.PlayerMount);

            // 騎手をスポーン
            var rider = SpawnRider(playerPos, rotation, mount, _currentReplay.PlayerCharacter);

            // エンティティを登録
            var playerEntity = new SpawnedEntity
            {
                Root = mount,
                Mount = mount,
                Rider = rider,
                RiderAnimator = rider?.GetComponentInChildren<Animator>(),
                MountAnimator = mount?.GetComponentInChildren<Animator>(),
                EntityType = EntityType.Player
            };

            _spawnedEntities["player_0"] = playerEntity;

            // 馬も別途登録（位置更新用）
            var mountEntity = new SpawnedEntity
            {
                Root = mount,
                Mount = mount,
                MountAnimator = mount?.GetComponentInChildren<Animator>(),
                EntityType = EntityType.Mount
            };
            _spawnedEntities["mount_0"] = mountEntity;
        }

        /// <summary>
        /// 敵エンティティをスポーンします
        /// </summary>
        /// <param name="customization">敵のカスタマイズデータ</param>
        private void SpawnEnemyEntity(ReplayEntityCustomization customization)
        {
            if (_mountPrefab == null || _riderPrefab == null)
            {
                Debug.LogWarning("[ReplayPlaybackManager] プレハブが設定されていないため、敵をスポーンできません。");
                return;
            }

            if (_currentReplay == null)
            {
                return;
            }

            // 初期位置を取得
            Vector3 riderPos = Vector3.zero;
            Vector3 mountPos = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            string mountEntityId = $"mount_{customization.EntityId.Replace("enemy_", "")}";

            if (_currentReplay.Frames.Count > 0)
            {
                var firstFrame = _currentReplay.Frames[0];
                foreach (var snapshot in firstFrame.Entities)
                {
                    if (snapshot.EntityId == customization.EntityId)
                    {
                        riderPos = snapshot.Position;
                        rotation = snapshot.Rotation;
                    }
                    else if (snapshot.EntityId == mountEntityId || snapshot.EntityId == "mount_enemy_0")
                    {
                        mountPos = snapshot.Position;
                        mountEntityId = snapshot.EntityId; // 実際のIDを使用
                    }
                }
            }

            // 馬をスポーン
            var mount = SpawnMount(mountPos, rotation, customization.Mount);

            // 騎手をスポーン
            var rider = SpawnRider(riderPos, rotation, mount, customization.Character);

            // エンティティを登録
            var enemyEntity = new SpawnedEntity
            {
                Root = mount,
                Mount = mount,
                Rider = rider,
                RiderAnimator = rider?.GetComponentInChildren<Animator>(),
                MountAnimator = mount?.GetComponentInChildren<Animator>(),
                EntityType = EntityType.Enemy
            };

            _spawnedEntities[customization.EntityId] = enemyEntity;

            // 馬も別途登録
            var mountEntity = new SpawnedEntity
            {
                Root = mount,
                Mount = mount,
                MountAnimator = mount?.GetComponentInChildren<Animator>(),
                EntityType = EntityType.Mount
            };
            _spawnedEntities[mountEntityId] = mountEntity;
        }

        /// <summary>
        /// ウルフエンティティをスポーンします（ハンティングモード用）
        /// </summary>
        /// <param name="customization">ウルフのカスタマイズデータ</param>
        private void SpawnWolfEntity(ReplayEntityCustomization customization)
        {
            if (_wolfPrefab == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning("[ReplayPlaybackManager] ウルフプレハブが設定されていないため、ウルフをスポーンできません。");
                }
                return;
            }

            if (_currentReplay == null)
            {
                return;
            }

            // 初期位置を取得
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            if (_currentReplay.Frames.Count > 0)
            {
                var firstFrame = _currentReplay.Frames[0];
                foreach (var snapshot in firstFrame.Entities)
                {
                    if (snapshot.EntityId == customization.EntityId)
                    {
                        position = snapshot.Position;
                        rotation = snapshot.Rotation;
                        break;
                    }
                }
            }

            // ウルフをスポーン
            var wolf = SpawnWolf(position, rotation, customization.TeamIndex);

            // エンティティを登録
            var wolfEntity = new SpawnedEntity
            {
                Root = wolf,
                Wolf = wolf,
                WolfAnimator = wolf?.GetComponentInChildren<Animator>(),
                EntityType = EntityType.Wolf
            };

            _spawnedEntities[customization.EntityId] = wolfEntity;

            if (_debugLog)
            {
                Debug.Log($"[ReplayPlaybackManager] ウルフをスポーンしました: {customization.EntityId} (Team: {customization.TeamIndex})");
            }
        }

        /// <summary>
        /// その他のエンティティをスポーンします（獲物、NPC等）
        /// </summary>
        /// <param name="customization">エンティティのカスタマイズデータ</param>
        private void SpawnOtherEntity(ReplayEntityCustomization customization)
        {
            // TODO: 獲物やNPCのスポーン処理を実装
            // 現時点ではスキップ
            if (_debugLog)
            {
                Debug.Log($"[ReplayPlaybackManager] その他のエンティティをスキップしました: {customization.EntityId} (Type: {customization.EntityType})");
            }
        }

        /// <summary>
        /// ウルフをスポーンします
        /// </summary>
        /// <param name="position">スポーン位置</param>
        /// <param name="rotation">スポーン回転</param>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>スポーンされたウルフのGameObject</returns>
        private GameObject? SpawnWolf(Vector3 position, Quaternion rotation, int teamIndex)
        {
            if (_wolfPrefab == null)
            {
                return null;
            }

            var wolf = Instantiate(_wolfPrefab, position, rotation, _entityContainer);
            wolf.name = $"ReplayWolf_Team{teamIndex}";

            // ゲームプレイコンポーネントを無効化
            DisableGameplayComponents(wolf);

            // WolfControllerを無効化
            DisableComponentByName(wolf, "WolfController");

            // チームマテリアルの適用はHuntingRulesHandlerに依存するため、
            // リプレイでは簡易的な着色のみ対応（または将来的にマテリアル設定を追加）

            return wolf;
        }

        /// <summary>
        /// 馬をスポーンします
        /// </summary>
        private GameObject? SpawnMount(Vector3 position, Quaternion rotation, MountCustomization customization)
        {
            if (_mountPrefab == null)
            {
                return null;
            }

            var mount = Instantiate(_mountPrefab, position, rotation, _entityContainer);
            mount.name = "ReplayMount";

            // ゲームプレイコンポーネントを無効化
            DisableGameplayComponents(mount);

            // カスタマイズを適用
            if (_customizationService != null)
            {
                // 一時的にカスタマイズを設定して適用
                var originalMount = _customizationService.CurrentMount;
                // カスタマイズサービスに直接適用する方法がないため、Applierを直接使用
                ApplyMountCustomization(mount, customization);
            }

            return mount;
        }

        /// <summary>
        /// 騎手をスポーンします
        /// </summary>
        private GameObject? SpawnRider(Vector3 position, Quaternion rotation, GameObject? mount, CharacterCustomization customization)
        {
            if (_riderPrefab == null)
            {
                return null;
            }

            // プレハブを非アクティブでスポーン
            bool prefabWasActive = _riderPrefab.activeSelf;
            _riderPrefab.SetActive(false);

            var rider = Instantiate(_riderPrefab, position, rotation, _entityContainer);
            rider.name = "ReplayRider";

            _riderPrefab.SetActive(prefabWasActive);

            // Rigidbodyを追加（MRiderが必要とする場合）
            var rb = rider.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = rider.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            rider.SetActive(true);

            // ゲームプレイコンポーネントを無効化
            DisableGameplayComponents(rider);

            // 馬に騎乗（親子関係は設定しない、ApplySnapshotToEntityで位置を更新する）
            // 親子関係を設定すると馬の動きに追従するが、リプレイでは毎フレーム位置を明示的に設定する
            // デフォルトの騎乗位置を設定（最初のフレームで上書きされる）
            if (mount != null)
            {
                // 騎乗ポイントを探す（Malbers Mountの場合はMountPointを探す）
                Transform? mountPoint = FindMountPoint(mount);
                if (mountPoint != null)
                {
                    rider.transform.position = mountPoint.position;
                    rider.transform.rotation = mountPoint.rotation;
                }
                else
                {
                    // マウントポイントが見つからない場合はデフォルト位置
                    rider.transform.position = mount.transform.position + Vector3.up * 1.5f;
                    rider.transform.rotation = mount.transform.rotation;
                }
            }

            // カスタマイズを適用
            ApplyCharacterCustomization(rider, customization);

            return rider;
        }

        /// <summary>
        /// 馬にカスタマイズを適用します
        /// </summary>
        private void ApplyMountCustomization(GameObject mount, MountCustomization customization)
        {
            // CustomizationServiceから取得したMalbersHorseApplierを使用
            if (_mountApplier != null)
            {
                _mountApplier.Apply(mount, customization);
            }
            else if (_debugLog)
            {
                Debug.LogWarning("[ReplayPlaybackManager] MalbersHorseApplier が設定されていません。カスタマイズをスキップします。");
            }
        }

        /// <summary>
        /// キャラクターにカスタマイズを適用します
        /// </summary>
        private void ApplyCharacterCustomization(GameObject rider, CharacterCustomization customization)
        {
            // CustomizationServiceから取得したP09CharacterApplierを使用
            if (_characterApplier != null)
            {
                _characterApplier.Apply(rider, customization);
            }
            else if (_debugLog)
            {
                Debug.LogWarning("[ReplayPlaybackManager] P09CharacterApplier が設定されていません。カスタマイズをスキップします。");
            }
        }

        #endregion

        #region Private Methods - Frame Application

        /// <summary>
        /// スナップショットをエンティティに適用します
        /// </summary>
        private void ApplySnapshotToEntity(EntitySnapshot snapshot, SpawnedEntity entity)
        {
            // ウルフの場合
            if (entity.EntityType == EntityType.Wolf && entity.Wolf != null)
            {
                entity.Wolf.transform.position = snapshot.Position;
                entity.Wolf.transform.rotation = snapshot.Rotation;

                // アニメーションを適用
                if (entity.WolfAnimator != null)
                {
                    ApplyAnimationState(entity.WolfAnimator, snapshot);
                }

                // 表示/非表示
                if (entity.Wolf.activeSelf != snapshot.IsAlive)
                {
                    entity.Wolf.SetActive(snapshot.IsAlive);
                }
            }
            // 馬の場合はRootを移動
            else if (snapshot.EntityId.Contains("mount") && entity.Mount != null)
            {
                entity.Mount.transform.position = snapshot.Position;
                entity.Mount.transform.rotation = snapshot.Rotation;

                // アニメーションを適用
                if (entity.MountAnimator != null)
                {
                    ApplyAnimationState(entity.MountAnimator, snapshot);
                }

                // 表示/非表示
                if (entity.Mount.activeSelf != snapshot.IsAlive)
                {
                    entity.Mount.SetActive(snapshot.IsAlive);
                }
            }
            // 騎手の場合
            else if (entity.Rider != null)
            {
                // 騎乗状態に応じて位置を設定
                if (snapshot.IsMounted && !string.IsNullOrEmpty(snapshot.MountedOnEntityId))
                {
                    // 騎乗中: 馬のTransformを基準にローカル位置を適用
                    if (_spawnedEntities.TryGetValue(snapshot.MountedOnEntityId, out var mountEntity) && mountEntity.Mount != null)
                    {
                        // 騎乗ポイント（馬のTransform）を基準にローカル位置を適用
                        var mountTransform = mountEntity.Mount.transform;
                        entity.Rider.transform.position = mountTransform.TransformPoint(snapshot.LocalPositionOnMount);
                        entity.Rider.transform.rotation = mountTransform.rotation * snapshot.LocalRotationOnMount;
                    }
                }
                else
                {
                    // 騎乗していない場合はワールド位置を適用
                    entity.Rider.transform.position = snapshot.Position;
                    entity.Rider.transform.rotation = snapshot.Rotation;
                }

                // アニメーションを適用
                if (entity.RiderAnimator != null)
                {
                    ApplyAnimationState(entity.RiderAnimator, snapshot);
                }

                // 表示/非表示
                if (entity.Rider.activeSelf != snapshot.IsAlive)
                {
                    entity.Rider.SetActive(snapshot.IsAlive);
                }
            }
        }

        /// <summary>
        /// アニメーション状態を適用します
        /// </summary>
        private void ApplyAnimationState(Animator animator, EntitySnapshot snapshot)
        {
            // メインレイヤーのアニメーション
            if (snapshot.AnimationStateHash != 0)
            {
                TryPlayAnimation(animator, snapshot.AnimationStateHash, snapshot.AnimationNormalizedTime);
            }

            // アニメーションパラメータを設定
            TrySetAnimatorFloat(animator, "Speed", snapshot.AnimSpeedParam);
            TrySetAnimatorFloat(animator, "StateTime", snapshot.AnimStateTimeParam);

            // 追加レイヤーのアニメーション
            if (snapshot.AdditionalLayerStates != null)
            {
                foreach (var layerState in snapshot.AdditionalLayerStates)
                {
                    if (layerState.StateHash != 0)
                    {
                        // レイヤーウェイトを設定
                        if (layerState.LayerIndex < animator.layerCount)
                        {
                            animator.SetLayerWeight(layerState.LayerIndex, layerState.Weight);
                            TryPlayAnimation(animator, layerState.StateHash, layerState.NormalizedTime, layerState.LayerIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Animatorのfloatパラメータを安全に設定します
        /// </summary>
        private void TrySetAnimatorFloat(Animator animator, string paramName, float value)
        {
            // Animatorのインスタンスとパラメータ名でキャッシュキーを作成
            string cacheKey = $"{animator.GetInstanceID()}_{paramName}";

            // 既に無効と判明しているパラメータはスキップ
            if (_invalidAnimatorParams.Contains(cacheKey))
            {
                return;
            }

            // パラメータが存在するか確認
            bool paramExists = false;
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Float)
                {
                    paramExists = true;
                    break;
                }
            }

            if (paramExists)
            {
                animator.SetFloat(paramName, value);
            }
            else
            {
                // 無効なパラメータをキャッシュ
                _invalidAnimatorParams.Add(cacheKey);
            }
        }

        /// <summary>
        /// アニメーションを再生します（存在しないステートはスキップ）
        /// </summary>
        /// <param name="animator">アニメーター</param>
        /// <param name="stateHash">ステートハッシュ</param>
        /// <param name="normalizedTime">正規化された時間</param>
        /// <param name="layer">アニメーションレイヤー（デフォルト: 0）</param>
        private void TryPlayAnimation(Animator animator, int stateHash, float normalizedTime, int layer = 0)
        {
            // レイヤーとハッシュを組み合わせたキーを作成
            int cacheKey = stateHash ^ (layer << 16);

            // 既に無効と判明しているハッシュはスキップ
            if (_invalidAnimationHashes.Contains(cacheKey))
            {
                return;
            }

            // ステートが存在するか確認
            if (animator.HasState(layer, stateHash))
            {
                animator.Play(stateHash, layer, normalizedTime);
            }
            else
            {
                // 無効なハッシュを記録して次回からスキップ
                _invalidAnimationHashes.Add(cacheKey);

                if (_debugLog)
                {
                    Debug.LogWarning($"[ReplayPlaybackManager] アニメーションステート (hash: {stateHash}, layer: {layer}) が見つかりません。スキップします。");
                }
            }
        }

        #endregion

        #region Private Methods - Component Management

        /// <summary>
        /// ゲームプレイ用コンポーネントを無効化します
        /// </summary>
        /// <param name="entity">対象のGameObject</param>
        private void DisableGameplayComponents(GameObject entity)
        {
            // Rigidbodyを無効化
            var rigidbodies = entity.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Colliderを無効化
            var colliders = entity.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // AudioSourceを無効化
            var audioSources = entity.GetComponentsInChildren<AudioSource>(true);
            foreach (var audio in audioSources)
            {
                audio.enabled = false;
            }

            // NavMeshAgentを無効化
            var navMeshAgents = entity.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
            foreach (var agent in navMeshAgents)
            {
                agent.enabled = false;
            }

            // ゲームプレイ関連のMonoBehaviourを無効化
            DisableComponentByName(entity, "MAnimal");
            DisableComponentByName(entity, "MRider");
            DisableComponentByName(entity, "BlazeAI");
            DisableComponentByName(entity, "PlayerController");
            DisableComponentByName(entity, "RiderController");
            DisableComponentByName(entity, "MountController");
            DisableComponentByName(entity, "RiderArcherController");
        }

        /// <summary>
        /// 名前でコンポーネントを無効化します
        /// </summary>
        private void DisableComponentByName(GameObject entity, string componentName)
        {
            var components = entity.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in components)
            {
                if (component != null && component.GetType().Name.Contains(componentName))
                {
                    component.enabled = false;
                }
            }
        }

        /// <summary>
        /// 馬の騎乗ポイントを探します
        /// </summary>
        /// <param name="mount">馬のGameObject</param>
        /// <returns>騎乗ポイントのTransform（見つからない場合はnull）</returns>
        private Transform? FindMountPoint(GameObject mount)
        {
            // Malbersの標準的な騎乗ポイント名を探す
            string[] mountPointNames = { "MountPoint", "Sit Point", "SitPoint", "RiderPoint", "Rider Point" };

            foreach (var name in mountPointNames)
            {
                var point = mount.transform.Find(name);
                if (point != null)
                {
                    return point;
                }

                // 再帰的に検索
                point = FindChildRecursive(mount.transform, name);
                if (point != null)
                {
                    return point;
                }
            }

            return null;
        }

        /// <summary>
        /// 子Transform を再帰的に検索します
        /// </summary>
        private Transform? FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name || child.name.Contains(name))
                {
                    return child;
                }

                var found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        #endregion
    }
}
