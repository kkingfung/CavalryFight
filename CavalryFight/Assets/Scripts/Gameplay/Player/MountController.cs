#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 馬（マウント）を制御するコンポーネント
    /// </summary>
    /// <remarks>
    /// Malbers Horse AnimSet Proのラッパーとして機能し、
    /// 移動制御とカスタマイズ適用のインターフェースを提供します。
    /// IInputServiceから直接入力を受け取り、馬の移動を制御します。
    /// </remarks>
    public class MountController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Horse Model")]
        [Tooltip("Malbers馬モデル（子オブジェクト）")]
        [SerializeField] private GameObject? _horseModel;

        [Tooltip("馬のAnimator")]
        [SerializeField] private Animator? _animator;

        [Header("Mount Point")]
        [Tooltip("騎手が座る位置")]
        [SerializeField] private Transform? _mountPoint;

        [Header("Movement Settings")]
        [Tooltip("歩行速度")]
        [SerializeField] private float _walkSpeed = 4f;

        [Tooltip("走行速度")]
        [SerializeField] private float _runSpeed = 8f;

        [Tooltip("ダッシュ速度")]
        [SerializeField] private float _sprintSpeed = 12f;

        [Tooltip("回転速度")]
        [SerializeField] private float _rotationSpeed = 120f;

        [Header("Physics")]
        [Tooltip("Rigidbodyを使用するか（falseの場合はTransform移動）")]
        [SerializeField] private bool _useRigidbody = false;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        #endregion

        #region Private Fields

        private IInputService? _inputService;
        private Rigidbody? _rigidbody;
        private bool _hasRider = false;
        private float _currentSpeed = 0f;

        // Animatorパラメータ
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int StateParam = Animator.StringToHash("State");
        private static readonly int SpeedMultiplierParam = Animator.StringToHash("SpeedMultiplier");

        #endregion

        #region Properties

        /// <summary>
        /// 馬モデルのGameObjectを取得します
        /// </summary>
        /// <remarks>
        /// カスタマイズ適用時にこのオブジェクトを対象とします
        /// </remarks>
        public GameObject? HorseModel => _horseModel;

        /// <summary>
        /// MountPoint（騎手の座る位置）を取得します
        /// </summary>
        public Transform? MountPoint => _mountPoint;

        /// <summary>
        /// 騎手がいるかどうかを取得します
        /// </summary>
        public bool HasRider => _hasRider;

        /// <summary>
        /// 現在の速度を取得します
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// Animatorを取得します
        /// </summary>
        public Animator? Animator => _animator;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _rigidbody = GetComponent<Rigidbody>();

            ValidateReferences();
            ConfigurePhysics();
        }

        /// <summary>
        /// 物理設定を調整します
        /// </summary>
        private void ConfigurePhysics()
        {
            // Rigidbodyの設定を調整して、予期しない動きを防ぐ
            if (_rigidbody != null)
            {
                // キネマティックでない場合、制約を設定
                if (!_rigidbody.isKinematic)
                {
                    // 回転の制限（X,Zは固定、Yのみ回転可能）
                    _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                    // ドラッグを増やして滑りを防ぐ
                    _rigidbody.linearDamping = 5f;
                    _rigidbody.angularDamping = 5f;
                }
            }
        }

        private void Update()
        {
            // 騎手がいる場合のみ入力を処理
            if (_hasRider && _inputService != null && _inputService.InputEnabled)
            {
                HandleMovementInput();
            }
        }

        private void FixedUpdate()
        {
            // Rigidbody移動の場合はFixedUpdateで処理
            if (_useRigidbody && _hasRider)
            {
                // Rigidbody移動はここで実装
            }
        }

        #endregion

        #region Public Methods - Rider Management

        /// <summary>
        /// 騎手が乗ったことを通知します
        /// </summary>
        public void OnRiderMounted()
        {
            _hasRider = true;

            if (_debugLog)
            {
                Debug.Log("[MountController] Rider mounted");
            }
        }

        /// <summary>
        /// 騎手が降りたことを通知します
        /// </summary>
        public void OnRiderDismounted()
        {
            _hasRider = false;
            _currentSpeed = 0f;

            // 停止アニメーション
            UpdateAnimator(0f);

            if (_debugLog)
            {
                Debug.Log("[MountController] Rider dismounted");
            }
        }

        #endregion

        #region Public Methods - Movement

        /// <summary>
        /// 外部から移動入力を設定します
        /// </summary>
        /// <param name="moveInput">移動入力（-1 to 1）</param>
        /// <param name="isSprinting">ダッシュ中か</param>
        public void SetMovementInput(Vector2 moveInput, bool isSprinting)
        {
            if (!_hasRider)
            {
                return;
            }

            // 速度計算
            float targetSpeed = 0f;
            if (moveInput.magnitude > 0.1f)
            {
                if (isSprinting)
                {
                    targetSpeed = _sprintSpeed;
                }
                else if (moveInput.y > 0.5f)
                {
                    targetSpeed = _runSpeed;
                }
                else
                {
                    targetSpeed = _walkSpeed;
                }
            }

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * 5f);

            // 移動適用
            ApplyMovement(moveInput);

            // アニメーション更新
            UpdateAnimator(_currentSpeed);
        }

        #endregion

        #region Public Methods - Customization

        /// <summary>
        /// カスタマイズ適用対象のGameObjectを取得します
        /// </summary>
        /// <returns>馬モデルのGameObject、またはこのGameObject</returns>
        public GameObject GetCustomizationTarget()
        {
            return _horseModel != null ? _horseModel : gameObject;
        }

        #endregion

        #region Private Methods - Movement

        /// <summary>
        /// 入力から移動を処理します
        /// </summary>
        private void HandleMovementInput()
        {
            if (_inputService == null)
            {
                return;
            }

            Vector2 moveInput = _inputService.GetMovementInput();
            bool isSprinting = _inputService.GetSprintButton();

            SetMovementInput(moveInput, isSprinting);
        }

        /// <summary>
        /// 移動を適用します
        /// </summary>
        /// <param name="moveInput">移動入力</param>
        private void ApplyMovement(Vector2 moveInput)
        {
            if (moveInput.magnitude < 0.1f)
            {
                return;
            }

            // 回転（左右入力）
            float rotationAmount = moveInput.x * _rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, rotationAmount, 0f);

            // 前進/後退
            if (Mathf.Abs(moveInput.y) > 0.1f)
            {
                Vector3 moveDirection = transform.forward * moveInput.y;
                float speed = moveInput.y > 0 ? _currentSpeed : _walkSpeed * 0.5f; // 後退は遅い

                if (_useRigidbody && _rigidbody != null)
                {
                    _rigidbody.MovePosition(transform.position + moveDirection * speed * Time.deltaTime);
                }
                else
                {
                    transform.position += moveDirection * speed * Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Animatorを更新します
        /// </summary>
        /// <param name="speed">現在の速度</param>
        /// <remarks>
        /// TODO: Malbers馬のAnimatorパラメータを調査して正しいパラメータ名を設定
        /// 現在はAnimatorパラメータが不明なため、スキップ
        /// </remarks>
        private void UpdateAnimator(float speed)
        {
            // TODO: Malbers馬のAnimatorパラメータを調査して実装
            // 現在はパラメータが存在しないためスキップ
            // if (_animator == null)
            // {
            //     return;
            // }
            //
            // _animator.SetFloat(SpeedParam, speed);
            //
            // int state = 0;
            // if (speed > _runSpeed * 0.9f)
            // {
            //     state = 4; // Sprint
            // }
            // else if (speed > _walkSpeed * 1.5f)
            // {
            //     state = 3; // Run
            // }
            // else if (speed > _walkSpeed * 0.5f)
            // {
            //     state = 1; // Walk
            // }
            //
            // _animator.SetInteger(StateParam, state);
        }

        #endregion

        #region Private Methods - Validation

        /// <summary>
        /// 参照の検証を行います
        /// </summary>
        private void ValidateReferences()
        {
            if (_horseModel == null)
            {
                // 子オブジェクトから馬モデルを自動検出
                _horseModel = FindHorseModelInChildren();

                if (_horseModel == null && _debugLog)
                {
                    Debug.LogWarning("[MountController] HorseModel が設定されていません");
                }
            }

            if (_animator == null && _horseModel != null)
            {
                _animator = _horseModel.GetComponent<Animator>();

                if (_animator == null)
                {
                    _animator = _horseModel.GetComponentInChildren<Animator>();
                }
            }

            if (_mountPoint == null)
            {
                // MountPointを検索
                _mountPoint = FindMountPoint();

                if (_mountPoint == null && _debugLog)
                {
                    Debug.LogWarning("[MountController] MountPoint が見つかりません");
                }
            }

            if (_animator == null && _debugLog)
            {
                Debug.LogWarning("[MountController] Animator が見つかりません");
            }
        }

        /// <summary>
        /// 子オブジェクトから馬モデルを検索します
        /// </summary>
        /// <returns>見つかった馬モデル</returns>
        private GameObject? FindHorseModelInChildren()
        {
            foreach (Transform child in transform)
            {
                // Horseを含む、またはMalbersコンポーネントを持つオブジェクトを検索
                if (child.name.Contains("Horse") ||
                    child.GetComponent<Animator>() != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// MountPointを検索します
        /// </summary>
        /// <returns>見つかったMountPoint</returns>
        private Transform? FindMountPoint()
        {
            // 一般的なMountPoint名を検索
            string[] mountPointNames = { "MountPoint", "Mount Point", "Seat", "RiderSeat", "Rider" };

            // 直接の子を検索
            foreach (string name in mountPointNames)
            {
                Transform? point = transform.Find(name);
                if (point != null)
                {
                    return point;
                }
            }

            // 再帰的に検索
            foreach (string name in mountPointNames)
            {
                Transform? point = FindChildRecursive(transform, name);
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

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// エディタでのギズモ描画
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // MountPointを可視化
            if (_mountPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_mountPoint.position, 0.2f);
                Gizmos.DrawLine(transform.position, _mountPoint.position);
            }

            // 進行方向を表示
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
        }
#endif

        #endregion
    }
}
