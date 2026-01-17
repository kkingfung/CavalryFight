#nullable enable

using UnityEngine;
using UnityEngine.Animations;
using System.Collections;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 騎手のアーチャーシステムを制御するコンポーネント
    /// </summary>
    /// <remarks>
    /// Kevin IglesiasのHumanArcherControllerを騎乗用にアダプトしたもの。
    /// P09の弓はParentConstraintで手と背中を切り替えます。
    /// 弓の位置管理、エイム状態の管理、上半身の回転を担当します。
    /// 矢の生成はPlayerControllerが担当（カスタマイズ対応のため）。
    /// StateMachineBehavioursと連携してAnimatorイベントを処理します。
    /// </remarks>
    public class RiderArcherController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Animator")]
        [Tooltip("Animator（自動取得）")]
        [SerializeField] private Animator? _animator;

        [Header("BOW")]
        [Tooltip("弓オブジェクト（ParentConstraintを持つ）")]
        [SerializeField] private GameObject? _bowObject;

        [Header("BOW POSITIONS (Legacy - ParentConstraint使用時は不要)")]
        [Tooltip("弓オブジェクト（手に持っている状態）- ParentConstraint使用時は_bowObjectを使用")]
        [SerializeField] private GameObject? _bowInHand;

        [Tooltip("収納時の弓オブジェクト（背中）- ParentConstraint使用時は不要")]
        [SerializeField] private GameObject? _bowSheathed;

        [Header("QUIVER")]
        [Tooltip("矢筒オブジェクト - オプション")]
        [SerializeField] private Transform? _quiver;

        [Header("Aim Rotation")]
        [Tooltip("エイム時に回転させる脊椎ボーン（Spine または Spine1）")]
        [SerializeField] private Transform? _spineTransform;

        [Tooltip("カメラのTransform（エイム方向の参照）")]
        [SerializeField] private Transform? _cameraTransform;

        [Tooltip("上半身の水平回転制限（度）")]
        [SerializeField] private float _maxHorizontalRotation = 70f;

        [Tooltip("上半身の垂直回転制限（度）")]
        [SerializeField] private float _maxVerticalRotation = 45f;

        [Tooltip("上半身回転のスムージング速度")]
        [SerializeField] private float _aimRotationSpeed = 10f;

        [Header("Timing Settings")]
        [Tooltip("弦を引く時間")]
        [SerializeField] private float _stringPullDuration = 0.3f;

        [Tooltip("弦を離す時間")]
        [SerializeField] private float _stringReleaseDuration = 0.15f;

        [Tooltip("ParentConstraintの重み変更速度")]
        [SerializeField] private float _constraintTransitionSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        #endregion

        #region Private Fields

        private Coroutine? _bowAnimation;
        private Coroutine? _bowSheathAnimation;
        private Coroutine? _getArrowAnimation;

        private bool _isBowLoaded = false;
        private bool _isAiming = false;

        // ParentConstraint関連
        private ParentConstraint? _bowParentConstraint;
        private float _targetHandWeight = 0f;    // 手の重み（目標値）
        private float _currentHandWeight = 0f;   // 手の重み（現在値）

        // 弓が直接手にアタッチされているか（ParentConstraintを使用しない場合）
        private bool _bowDirectlyAttachedToHand = false;

        // エイム回転関連
        private Quaternion _targetSpineRotation = Quaternion.identity;
        private Quaternion _originalSpineRotation = Quaternion.identity;

        #endregion

        #region Properties

        /// <summary>
        /// Animatorを取得します
        /// </summary>
        public Animator? Animator => _animator;

        /// <summary>
        /// 弓が手にあるかどうか
        /// </summary>
        public bool IsBowInHand => _bowInHand != null && _bowInHand.activeSelf;

        /// <summary>
        /// 弓がロード（引いた）状態かどうか
        /// </summary>
        public bool IsBowLoaded => _isBowLoaded;

        /// <summary>
        /// エイム中かどうか
        /// </summary>
        public bool IsAiming => _isAiming;

        /// <summary>
        /// 弦を引く時間
        /// </summary>
        public float StringPullDuration => _stringPullDuration;

        /// <summary>
        /// 弦を離す時間
        /// </summary>
        public float StringReleaseDuration => _stringReleaseDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Animatorを自動取得
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }
            }

            // カメラを自動取得
            if (_cameraTransform == null)
            {
                var mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }

            // 弓のParentConstraintを取得
            InitializeBowConstraint();

            // 脊椎ボーンを自動取得
            InitializeSpineTransform();
        }

        private void Update()
        {
            // ParentConstraintの重みをスムーズに更新
            UpdateBowConstraintWeight();
        }

        private void LateUpdate()
        {
            // エイム中は上半身をカメラ方向に回転
            if (_isAiming)
            {
                UpdateAimRotation();
            }
        }

        /// <summary>
        /// 弓のParentConstraintを初期化します
        /// </summary>
        private void InitializeBowConstraint()
        {
            // _bowObjectが設定されている場合はそれを使用
            GameObject? bowObj = _bowObject ?? _bowInHand;
            if (bowObj != null)
            {
                _bowParentConstraint = bowObj.GetComponent<ParentConstraint>();
                if (_bowParentConstraint != null && _debugLog)
                {
                    Debug.Log($"[RiderArcherController] ParentConstraint を検出: {bowObj.name}, Sources: {_bowParentConstraint.sourceCount}");
                }
            }
        }

        /// <summary>
        /// 脊椎ボーンを自動取得します
        /// </summary>
        private void InitializeSpineTransform()
        {
            if (_spineTransform != null)
            {
                return;
            }

            if (_animator == null)
            {
                return;
            }

            // HumanoidアバターからSpineボーンを取得
            _spineTransform = _animator.GetBoneTransform(HumanBodyBones.Spine);
            if (_spineTransform == null)
            {
                _spineTransform = _animator.GetBoneTransform(HumanBodyBones.Chest);
            }

            if (_spineTransform != null && _debugLog)
            {
                Debug.Log($"[RiderArcherController] Spineボーンを自動取得: {_spineTransform.name}");
            }
        }

        /// <summary>
        /// ParentConstraintの重みをスムーズに更新します
        /// </summary>
        private void UpdateBowConstraintWeight()
        {
            // 弓が直接手にアタッチされている場合はParentConstraintを使用しない
            if (_bowDirectlyAttachedToHand)
            {
                return;
            }

            if (_bowParentConstraint == null || _bowParentConstraint.sourceCount < 2)
            {
                return;
            }

            // 目標値に向かってスムーズに補間
            _currentHandWeight = Mathf.MoveTowards(
                _currentHandWeight,
                _targetHandWeight,
                _constraintTransitionSpeed * Time.deltaTime
            );

            // Source 0: 手（Weapon_Target_Hand_L）
            // Source 1: 背中（Bow_Target_Back）
            var handSource = _bowParentConstraint.GetSource(0);
            var backSource = _bowParentConstraint.GetSource(1);

            handSource.weight = _currentHandWeight;
            backSource.weight = 1f - _currentHandWeight;

            _bowParentConstraint.SetSource(0, handSource);
            _bowParentConstraint.SetSource(1, backSource);
        }

        /// <summary>
        /// エイム時の上半身回転を更新します
        /// </summary>
        private void UpdateAimRotation()
        {
            if (_spineTransform == null || _cameraTransform == null)
            {
                return;
            }

            // カメラの向きを取得
            Vector3 cameraForward = _cameraTransform.forward;
            cameraForward.y = 0; // 水平方向のみ
            cameraForward.Normalize();

            // 馬の向き（親の親がPlayerRider、その親が馬）
            Transform? mountTransform = transform.parent?.parent;
            if (mountTransform == null)
            {
                return;
            }

            Vector3 mountForward = mountTransform.forward;
            mountForward.y = 0;
            mountForward.Normalize();

            // 馬の向きとカメラの向きの差分を計算
            float horizontalAngle = Vector3.SignedAngle(mountForward, cameraForward, Vector3.up);
            horizontalAngle = Mathf.Clamp(horizontalAngle, -_maxHorizontalRotation, _maxHorizontalRotation);

            // 垂直方向のエイム角度
            float verticalAngle = -_cameraTransform.eulerAngles.x;
            if (verticalAngle > 180f) verticalAngle -= 360f;
            verticalAngle = Mathf.Clamp(verticalAngle, -_maxVerticalRotation, _maxVerticalRotation);

            // 目標回転を計算（脊椎のローカル空間で）
            Quaternion horizontalRot = Quaternion.AngleAxis(horizontalAngle * 0.5f, Vector3.up);
            Quaternion verticalRot = Quaternion.AngleAxis(verticalAngle * 0.3f, Vector3.right);
            _targetSpineRotation = horizontalRot * verticalRot;

            // スムーズに回転を適用
            _spineTransform.localRotation = Quaternion.Slerp(
                _spineTransform.localRotation,
                _originalSpineRotation * _targetSpineRotation,
                _aimRotationSpeed * Time.deltaTime
            );
        }

        #endregion

        #region Bow Pull/Release (State Tracking Only)

        /// <summary>
        /// 弓を引きます（チャージ開始）
        /// </summary>
        /// <param name="delay">開始までの遅延</param>
        /// <param name="duration">引く動作の時間</param>
        /// <remarks>
        /// P09の弓は骨格構造を持たないため、状態管理のみ行います。
        /// アニメーションはAnimator Controllerが担当します。
        /// </remarks>
        public void LoadBow(float delay, float duration)
        {
            if (_bowAnimation != null)
            {
                StopCoroutine(_bowAnimation);
            }

            _bowAnimation = StartCoroutine(LoadBowCoroutine(delay, duration));

            if (_debugLog)
            {
                Debug.Log($"[RiderArcherController] LoadBow: delay={delay}, duration={duration}");
            }
        }

        /// <summary>
        /// 矢を発射します（リリース）
        /// </summary>
        /// <param name="delay">開始までの遅延</param>
        /// <param name="duration">リリース動作の時間</param>
        /// <remarks>
        /// 矢の実際の生成はPlayerControllerが担当します（カスタマイズ対応）。
        /// ここではイベント通知と状態管理のみ行います。
        /// </remarks>
        public void ShootArrow(float delay, float duration)
        {
            if (_bowAnimation != null)
            {
                StopCoroutine(_bowAnimation);
            }

            _bowAnimation = StartCoroutine(ShootArrowCoroutine(delay, duration));

            if (_debugLog)
            {
                Debug.Log($"[RiderArcherController] ShootArrow: delay={delay}, duration={duration}");
            }
        }

        /// <summary>
        /// 弓を引くのをキャンセルします
        /// </summary>
        /// <param name="delay">開始までの遅延</param>
        /// <param name="cancelDuration">キャンセル動作の時間</param>
        public void CancelLoadBow(float delay, float cancelDuration)
        {
            if (_bowAnimation != null)
            {
                StopCoroutine(_bowAnimation);
            }

            _bowAnimation = StartCoroutine(CancelLoadBowCoroutine(delay, cancelDuration));

            if (_debugLog)
            {
                Debug.Log($"[RiderArcherController] CancelLoadBow: delay={delay}, duration={cancelDuration}");
            }
        }

        private IEnumerator LoadBowCoroutine(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);

            _isBowLoaded = true;
            _isAiming = true;

            // P09の弓は骨格構造がないため、視覚的な弦アニメーションは省略
            // Animator Controllerのアニメーションが弓を引く動作を担当

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator ShootArrowCoroutine(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);

            _isBowLoaded = false;

            // 矢のリリースイベントを通知（PlayerControllerが矢を生成）
            OnArrowReleased?.Invoke();

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator CancelLoadBowCoroutine(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);

            _isBowLoaded = false;

            yield return new WaitForSeconds(duration);
        }

        #endregion

        #region Arrow Management

        /// <summary>
        /// 矢を取り出します（発射後に次の矢を準備）
        /// </summary>
        /// <param name="delay">遅延時間</param>
        /// <remarks>
        /// 矢の表示はカスタマイズプレハブによって異なるため、
        /// ここでは状態管理とイベント通知のみ行います。
        /// </remarks>
        public void GetArrow(float delay)
        {
            if (_getArrowAnimation != null)
            {
                StopCoroutine(_getArrowAnimation);
            }

            _getArrowAnimation = StartCoroutine(GetArrowCoroutine(delay));

            if (_debugLog)
            {
                Debug.Log($"[RiderArcherController] GetArrow: delay={delay}");
            }
        }

        private IEnumerator GetArrowCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 矢を取り出したイベントを通知
            OnArrowReady?.Invoke();
        }

        #endregion

        #region Bow Unsheathe/Sheathe

        /// <summary>
        /// 弓を抜きます（背中から手に）
        /// </summary>
        /// <param name="delay">遅延時間</param>
        public void UnsheatheBow(float delay)
        {
            if (_bowSheathAnimation != null)
            {
                StopCoroutine(_bowSheathAnimation);
            }

            _bowSheathAnimation = StartCoroutine(UnsheatheBowCoroutine(delay));

            if (_debugLog)
            {
                Debug.Log($"[RiderArcherController] UnsheatheBow: delay={delay}");
            }
        }

        private IEnumerator UnsheatheBowCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 脊椎の初期回転を保存
            if (_spineTransform != null)
            {
                _originalSpineRotation = _spineTransform.localRotation;
            }

            // 弓が直接手にアタッチされている場合は位置変更不要
            if (_bowDirectlyAttachedToHand)
            {
                if (_debugLog)
                {
                    Debug.Log("[RiderArcherController] 弓は直接手にアタッチ済み - 位置変更スキップ");
                }
            }
            // ParentConstraintがある場合は重みを変更（手に移動）
            else if (_bowParentConstraint != null)
            {
                _targetHandWeight = 1f;
                if (_debugLog)
                {
                    Debug.Log("[RiderArcherController] 弓を手に移動（ParentConstraint weight -> hand）");
                }
            }
            else
            {
                // Legacy: ParentConstraintがない場合は表示/非表示で切り替え
                if (_bowSheathed != null)
                {
                    _bowSheathed.SetActive(false);
                }

                if (_bowInHand != null)
                {
                    _bowInHand.SetActive(true);
                }
            }

            _isAiming = true;
            OnBowUnsheathed?.Invoke();
        }

        /// <summary>
        /// 弓を収納します（手から背中に）
        /// </summary>
        /// <param name="delay">遅延時間</param>
        public void SheatheBow(float delay)
        {
            if (_bowSheathAnimation != null)
            {
                StopCoroutine(_bowSheathAnimation);
            }

            _bowSheathAnimation = StartCoroutine(SheatheBowCoroutine(delay));

            if (_debugLog)
            {
                Debug.Log($"[RiderArcherController] SheatheBow: delay={delay}");
            }
        }

        private IEnumerator SheatheBowCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 弓が直接手にアタッチされている場合は位置変更不要（常に手に保持）
            if (_bowDirectlyAttachedToHand)
            {
                if (_debugLog)
                {
                    Debug.Log("[RiderArcherController] 弓は直接手にアタッチ済み - 収納スキップ（常に手に保持）");
                }
            }
            // ParentConstraintがある場合は重みを変更（背中に移動）
            else if (_bowParentConstraint != null)
            {
                _targetHandWeight = 0f;
                if (_debugLog)
                {
                    Debug.Log("[RiderArcherController] 弓を背中に移動（ParentConstraint weight -> back）");
                }
            }
            else
            {
                // Legacy: ParentConstraintがない場合は表示/非表示で切り替え
                if (_bowSheathed != null)
                {
                    _bowSheathed.SetActive(true);
                }

                if (_bowInHand != null)
                {
                    _bowInHand.SetActive(false);
                }
            }

            // 脊椎の回転を元に戻す
            if (_spineTransform != null)
            {
                _spineTransform.localRotation = _originalSpineRotation;
            }

            _isAiming = false;
            _isBowLoaded = false;
            OnBowSheathed?.Invoke();
        }

        #endregion

        #region Events

        /// <summary>
        /// 矢がリリースされた時に発火するイベント
        /// </summary>
        /// <remarks>
        /// PlayerControllerはこのイベントを購読せず、
        /// 直接入力に応じて矢を生成します（タイミング制御のため）。
        /// 他のシステム（サウンド、VFX等）が使用可能です。
        /// </remarks>
        public event System.Action? OnArrowReleased;

        /// <summary>
        /// 矢が準備完了した時に発火するイベント
        /// </summary>
        public event System.Action? OnArrowReady;

        /// <summary>
        /// 弓が抜かれた時に発火するイベント
        /// </summary>
        public event System.Action? OnBowUnsheathed;

        /// <summary>
        /// 弓が収納された時に発火するイベント
        /// </summary>
        public event System.Action? OnBowSheathed;

        #endregion

        #region Public Methods

        /// <summary>
        /// 弓のGameObject参照を設定します
        /// </summary>
        public void SetBowObjects(GameObject? bowInHand, GameObject? bowSheathed)
        {
            _bowInHand = bowInHand;
            _bowSheathed = bowSheathed;
        }

        /// <summary>
        /// すべてのコルーチンを停止し、弓を初期状態に戻します
        /// </summary>
        public void ResetBowState()
        {
            if (_bowAnimation != null)
            {
                StopCoroutine(_bowAnimation);
                _bowAnimation = null;
            }

            if (_bowSheathAnimation != null)
            {
                StopCoroutine(_bowSheathAnimation);
                _bowSheathAnimation = null;
            }

            if (_getArrowAnimation != null)
            {
                StopCoroutine(_getArrowAnimation);
                _getArrowAnimation = null;
            }

            _isBowLoaded = false;

            // 弓が直接手にアタッチされている場合は、位置をリセットしない（常に手に保持）
            if (_bowDirectlyAttachedToHand)
            {
                // _isAimingはtrueのまま維持（弓は常に手にある）
                if (_debugLog)
                {
                    Debug.Log("[RiderArcherController] 弓は直接アタッチ済み - 位置リセットスキップ");
                }
            }
            else
            {
                _isAiming = false;

                // ParentConstraintを背中の位置に戻す
                _targetHandWeight = 0f;
                _currentHandWeight = 0f;
                UpdateBowConstraintWeight();
            }

            // 脊椎の回転を元に戻す
            if (_spineTransform != null)
            {
                _spineTransform.localRotation = _originalSpineRotation;
            }

            if (_debugLog)
            {
                Debug.Log("[RiderArcherController] 弓を初期状態にリセットしました");
            }
        }

        /// <summary>
        /// カメラのTransformを設定します
        /// </summary>
        /// <param name="cameraTransform">カメラのTransform</param>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// 弓オブジェクトを設定します（ParentConstraint対応）
        /// </summary>
        /// <param name="bowObject">弓オブジェクト</param>
        /// <param name="immediatelyInHand">trueの場合、即座に手に配置</param>
        public void SetBowObject(GameObject bowObject, bool immediatelyInHand = false)
        {
            _bowObject = bowObject;
            _bowInHand = bowObject;
            InitializeBowConstraint();

            // 即座に手に配置する場合はParentConstraintを無効化して直接アタッチ
            if (immediatelyInHand)
            {
                Debug.Log($"[RiderArcherController] SetBowObject: immediatelyInHand=true, 弓を直接手に配置します");

                // ParentConstraintを無効化
                if (_bowParentConstraint != null)
                {
                    _bowParentConstraint.constraintActive = false;
                    Debug.Log("[RiderArcherController] ParentConstraintを無効化しました");
                }

                // 直接アタッチフラグを設定
                _bowDirectlyAttachedToHand = true;
                _isAiming = true;

                // 脊椎の初期回転を保存
                if (_spineTransform != null)
                {
                    _originalSpineRotation = _spineTransform.localRotation;
                }

                // 左手ボーンを取得して直接アタッチ
                if (_animator != null)
                {
                    Transform? leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    if (leftHand != null)
                    {
                        bowObject.transform.SetParent(leftHand);
                        // 位置と回転はPlayerController.ForceBowToLeftHandで毎フレーム設定するため、ここでは設定しない
                        Debug.Log($"[RiderArcherController] 弓を左手に直接アタッチ: parent={leftHand.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[RiderArcherController] 左手ボーンが見つかりません");
                    }
                }

                return;
            }

            // ParentConstraintが見つかった場合、制約を有効化（通常モード）
            if (_bowParentConstraint != null)
            {
                _bowParentConstraint.constraintActive = true;

                if (_debugLog)
                {
                    Debug.Log($"[RiderArcherController] 弓オブジェクトを設定: {bowObject.name}, ParentConstraint有効化");
                }
            }
            else
            {
                if (_debugLog)
                {
                    Debug.Log($"[RiderArcherController] 弓オブジェクトを設定: {bowObject.name} (ParentConstraintなし)");
                }
            }
        }

        /// <summary>
        /// 弓を指定されたボーンに直接アタッチします（ParentConstraintがない場合のフォールバック）
        /// </summary>
        /// <param name="handBone">手のボーン</param>
        /// <param name="localPosition">ローカル位置</param>
        /// <param name="localRotation">ローカル回転</param>
        public void AttachBowToHand(Transform handBone, Vector3 localPosition, Quaternion localRotation)
        {
            if (_bowObject == null)
            {
                Debug.LogWarning("[RiderArcherController] 弓オブジェクトが設定されていません");
                return;
            }

            // ParentConstraintがない場合は直接親子関係を設定
            if (_bowParentConstraint == null)
            {
                _bowObject.transform.SetParent(handBone);
                _bowObject.transform.localPosition = localPosition;
                _bowObject.transform.localRotation = localRotation;

                // 直接アタッチフラグを設定（Sheathe/Unsheatheで位置変更しない）
                _bowDirectlyAttachedToHand = true;

                if (_debugLog)
                {
                    Debug.Log($"[RiderArcherController] 弓を手に直接アタッチ: {handBone.name}");
                }
            }
        }

        /// <summary>
        /// 弓が直接手にアタッチされていることをマークします
        /// </summary>
        public void MarkBowAsDirectlyAttached()
        {
            _bowDirectlyAttachedToHand = true;
            _isAiming = true;  // 常にエイム状態とみなす

            // ParentConstraintを無効化（これが弓の位置を上書きするのを防ぐ）
            if (_bowParentConstraint != null)
            {
                _bowParentConstraint.constraintActive = false;
                Debug.Log("[RiderArcherController] ParentConstraintを無効化しました");
            }

            Debug.Log("[RiderArcherController] 弓を直接アタッチ済みとしてマーク");
        }

        #endregion
    }
}
