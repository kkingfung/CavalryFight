#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;
using MalbersAnimations.Controller;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 馬（マウント）を制御するコンポーネント
    /// </summary>
    /// <remarks>
    /// Malbers MAnimalコンポーネントをラップし、IInputServiceからの入力を
    /// MAnimalのAPIに変換して馬の移動とアニメーションを制御します。
    /// MAnimalが物理とアニメーションを処理するため、直接的なTransform操作は行いません。
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

        [Header("Boost Settings")]
        [Tooltip("ブースト時の速度倍率")]
        [SerializeField] private float _boostSpeedMultiplier = 1.5f;

        [Tooltip("ブースト持続時間（秒）")]
        [SerializeField] private float _boostDuration = 2f;

        [Tooltip("ブーストのクールダウン時間（秒）")]
        [SerializeField] private float _boostCooldown = 5f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        #endregion

        #region Private Fields

        private IInputService? _inputService;
        private MAnimal? _animal;
        private bool _hasRider = false;
        private bool _isSprinting = false;

        // ブースト関連
        private bool _isBoosting = false;
        private float _boostEndTime = 0f;
        private float _boostCooldownEndTime = 0f;
        private int _originalSpeedIndex = 1;
        private float _originalAnimatorSpeed = 1f;

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
        /// MAnimalコンポーネントを取得します
        /// </summary>
        public MAnimal? Animal => _animal;

        /// <summary>
        /// Animatorを取得します
        /// </summary>
        public Animator? Animator => _animator;

        /// <summary>
        /// ブースト中かどうかを取得します
        /// </summary>
        public bool IsBoosting => _isBoosting;

        /// <summary>
        /// ブーストがクールダウン中かどうかを取得します
        /// </summary>
        public bool IsBoostOnCooldown => Time.time < _boostCooldownEndTime;

        /// <summary>
        /// ブーストのクールダウン残り時間を取得します（秒）
        /// </summary>
        public float BoostCooldownRemaining => Mathf.Max(0f, _boostCooldownEndTime - Time.time);

        /// <summary>
        /// ブーストの残り時間を取得します（秒）
        /// </summary>
        public float BoostTimeRemaining => _isBoosting ? Mathf.Max(0f, _boostEndTime - Time.time) : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _inputService = ServiceLocator.Instance.Get<IInputService>();

            // MAnimalコンポーネントを取得
            _animal = GetComponent<MAnimal>();
            if (_animal == null)
            {
                _animal = GetComponentInChildren<MAnimal>();
            }

            // ブーストイベントを購読
            if (_inputService != null)
            {
                _inputService.BoostButtonPressed += OnBoostButtonPressed;
            }

            ValidateReferences();
        }

        private void OnDestroy()
        {
            // イベント購読を解除
            if (_inputService != null)
            {
                _inputService.BoostButtonPressed -= OnBoostButtonPressed;
            }
        }

        private void Update()
        {
            // 騎手がいる場合のみ入力を処理
            if (_hasRider && _inputService != null && _inputService.InputEnabled && _animal != null)
            {
                HandleMovementInput();
                UpdateBoostState();
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
            _isSprinting = false;
            _isBoosting = false;

            // MAnimalの移動を停止
            if (_animal != null)
            {
                _animal.StopMoving();
                _animal.Sprint_Set(false);
            }

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
            if (!_hasRider || _animal == null)
            {
                return;
            }

            // MAnimalに入力を渡す（MAnimalが物理とアニメーションを処理）
            _animal.SetInputAxis(moveInput);

            // ブースト中はスプリント状態を上書きしない
            if (_isBoosting)
            {
                return;
            }

            // スプリント状態の更新
            if (_isSprinting != isSprinting)
            {
                _isSprinting = isSprinting;
                _animal.Sprint_Set(isSprinting);

                if (_debugLog)
                {
                    Debug.Log($"[MountController] Sprint: {isSprinting}");
                }
            }
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

            // ジャンプ入力を処理
            HandleJumpInput();
        }

        /// <summary>
        /// ジャンプ入力を処理します
        /// </summary>
        private void HandleJumpInput()
        {
            if (_inputService == null || _animal == null)
            {
                return;
            }

            // ジャンプボタンの状態を取得
            bool jumpPressed = _inputService.GetJumpButton();

            // Jump StateのInputValueを設定（MAnimalの標準的な入力処理方式）
            var jumpState = _animal.State_Get(2); // Jump StateID = 2
            if (jumpState != null)
            {
                jumpState.SetInput(jumpPressed);
            }

            if (_debugLog && jumpPressed)
            {
                Debug.Log("[MountController] Jump input sent to MAnimal");
            }
        }

        /// <summary>
        /// ブーストボタンが押されたときのイベントハンドラ
        /// </summary>
        /// <remarks>
        /// InputServiceのBoostButtonPressedイベントは、攻撃チャージ中でない場合のみ発火します。
        /// これにより、右クリックがチャージキャンセルとブーストで適切に使い分けられます。
        /// </remarks>
        private void OnBoostButtonPressed(object? sender, System.EventArgs e)
        {
            // 騎手がいない場合やMAnimalがない場合はスキップ
            if (!_hasRider || _animal == null)
            {
                return;
            }

            TryStartBoost();
        }

        /// <summary>
        /// ブーストを開始しようとします
        /// </summary>
        private void TryStartBoost()
        {
            // 既にブースト中またはクールダウン中の場合はスキップ
            if (_isBoosting)
            {
                if (_debugLog)
                {
                    Debug.Log("[MountController] Boost skipped: already boosting");
                }
                return;
            }

            if (Time.time < _boostCooldownEndTime)
            {
                if (_debugLog)
                {
                    float remaining = _boostCooldownEndTime - Time.time;
                    Debug.Log($"[MountController] Boost on cooldown: {remaining:F1}s remaining");
                }
                return;
            }

            // ブースト開始
            StartBoost();
        }

        /// <summary>
        /// ブーストを開始します
        /// </summary>
        private void StartBoost()
        {
            if (_animal == null)
            {
                return;
            }

            _isBoosting = true;
            _boostEndTime = Time.time + _boostDuration;

            // MAnimalの現在のスピードインデックスとアニメーター速度を保存
            _originalSpeedIndex = _animal.CurrentSpeedIndex;
            _originalAnimatorSpeed = _animal.AnimatorSpeed;

            // スピードインデックスを最大に設定（ギャロップ）
            // MAnimalは Speed_CurrentIndex_Set でスピードを切り替える
            // 通常: 1=Walk, 2=Trot, 3=Gallop
            _animal.Speed_CurrentIndex_Set(3); // ギャロップにセット

            // さらにスプリントも有効化
            _animal.Sprint_Set(true);

            // アニメーター速度を上げる（これが実際の移動速度に影響）
            _animal.SetAnimatorSpeed(_boostSpeedMultiplier);

            // ブースト開始ログ（常に出力）
            Debug.Log($"[MountController] BOOST ACTIVATED! Duration: {_boostDuration}s, AnimatorSpeed: {_boostSpeedMultiplier}");
        }

        /// <summary>
        /// ブースト状態を更新します
        /// </summary>
        private void UpdateBoostState()
        {
            if (!_isBoosting)
            {
                return;
            }

            // ブースト終了判定
            if (Time.time >= _boostEndTime)
            {
                EndBoost();
            }
        }

        /// <summary>
        /// ブーストを終了します
        /// </summary>
        private void EndBoost()
        {
            if (_animal == null)
            {
                return;
            }

            _isBoosting = false;
            _boostCooldownEndTime = Time.time + _boostCooldown;

            // アニメーター速度を元に戻す
            _animal.SetAnimatorSpeed(_originalAnimatorSpeed);

            // スピードインデックスを元に戻す
            _animal.Speed_CurrentIndex_Set(_originalSpeedIndex);

            // スプリントをユーザーの現在の入力状態に戻す
            bool currentSprint = _inputService?.GetSprintButton() ?? false;
            _animal.Sprint_Set(currentSprint);

            // ブースト終了ログ（常に出力）
            Debug.Log($"[MountController] BOOST ENDED. Cooldown: {_boostCooldown}s");
        }

        #endregion

        #region Private Methods - Validation

        /// <summary>
        /// 参照の検証を行います
        /// </summary>
        private void ValidateReferences()
        {
            if (_animal == null)
            {
                Debug.LogError("[MountController] MAnimal コンポーネントが見つかりません。Malbers Animal Controllerが設定されていることを確認してください。");
            }

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
