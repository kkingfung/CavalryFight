#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力管理サービスの実装
    /// </summary>
    /// <remarks>
    /// Unity Input Systemをラップし、MVVMパターンでの入力処理を簡素化します。
    /// キーボード、マウス、ゲームパッドの入力に対応します。
    /// </remarks>
    public class InputService : IInputService
    {
        #region Events

        /// <summary>
        /// 攻撃ボタンが押された時に発生します。
        /// </summary>
        public event EventHandler? AttackButtonPressed;

        /// <summary>
        /// 攻撃ボタンが離された時に発生します。
        /// </summary>
        /// <remarks>
        /// チャージ攻撃の発動タイミングに使用します。
        /// </remarks>
        public event EventHandler? AttackButtonReleased;

        /// <summary>
        /// 攻撃キャンセルボタンが押された時に発生します。
        /// </summary>
        /// <remarks>
        /// チャージ中の攻撃をキャンセルする際に使用します。
        /// デフォルトでは右クリック（Fire2）に割り当てられます。
        /// </remarks>
        public event EventHandler? CancelAttackButtonPressed;

        /// <summary>
        /// 騎乗/降馬ボタンが押された時に発生します。
        /// </summary>
        public event EventHandler? MountButtonPressed;

        /// <summary>
        /// ジャンプボタンが押された時に発生します。
        /// </summary>
        public event EventHandler? JumpButtonPressed;

        /// <summary>
        /// メニューボタンが押された時に発生します。
        /// </summary>
        /// <remarks>
        /// ゲーム中のオプションメニュー（設定変更、リタイア等）を開く際に使用します。
        /// デフォルトではESCキーに割り当てられます。
        /// </remarks>
        public event EventHandler? MenuButtonPressed;

        /// <summary>
        /// ポーズボタンが押された時に発生します。
        /// </summary>
        public event EventHandler? PauseButtonPressed;

        #endregion

        #region Fields

        private bool _inputEnabled = true;
        private float _movementSensitivity = 1.0f;
        private float _cameraSensitivity = 1.0f;
        private bool _invertYAxis = false;
        private InputUpdater? _inputUpdater;

        #endregion

        #region Properties

        /// <summary>
        /// 入力が有効かどうかを取得または設定します。
        /// </summary>
        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        /// <summary>
        /// 移動入力の感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float MovementSensitivity
        {
            get => _movementSensitivity;
            set => _movementSensitivity = Mathf.Clamp01(value);
        }

        /// <summary>
        /// カメラ入力の感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float CameraSensitivity
        {
            get => _cameraSensitivity;
            set => _cameraSensitivity = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Y軸を反転するかどうかを取得または設定します。
        /// </summary>
        public bool InvertYAxis
        {
            get => _invertYAxis;
            set => _invertYAxis = value;
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        /// <remarks>
        /// ServiceLocatorに登録された直後に呼び出されます。
        /// 入力更新用のMonoBehaviourを作成します。
        /// </remarks>
        public void Initialize()
        {
            Debug.Log("[InputService] Initializing...");

            // 入力更新用のGameObjectを作成
            var updaterObject = new GameObject("InputUpdater");
            GameObject.DontDestroyOnLoad(updaterObject);
            _inputUpdater = updaterObject.AddComponent<InputUpdater>();
            _inputUpdater.Initialize(this);

            Debug.Log("[InputService] Initialized.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        /// <remarks>
        /// イベントハンドラをクリアし、入力更新用のGameObjectを破棄します。
        /// </remarks>
        public void Dispose()
        {
            Debug.Log("[InputService] Disposing...");

            // イベントハンドラをクリア
            AttackButtonPressed = null;
            AttackButtonReleased = null;
            CancelAttackButtonPressed = null;
            MountButtonPressed = null;
            JumpButtonPressed = null;
            MenuButtonPressed = null;
            PauseButtonPressed = null;

            // InputUpdaterを破棄
            if (_inputUpdater != null)
            {
                GameObject.Destroy(_inputUpdater.gameObject);
                _inputUpdater = null;
            }
        }

        #endregion

        #region Movement Input

        /// <summary>
        /// 移動入力ベクトルを取得します（正規化済み）
        /// </summary>
        /// <returns>移動ベクトル（X: 水平、Y: 垂直）</returns>
        public Vector2 GetMovementInput()
        {
            if (!_inputEnabled)
            {
                return Vector2.zero;
            }

            Vector2 input = GetRawMovementInput();
            input *= _movementSensitivity;

            // 正規化（斜め移動が速くならないように）
            if (input.magnitude > 1.0f)
            {
                input.Normalize();
            }

            return input;
        }

        /// <summary>
        /// 生の移動入力ベクトルを取得します（正規化なし）
        /// </summary>
        /// <returns>生の移動ベクトル</returns>
        public Vector2 GetRawMovementInput()
        {
            if (!_inputEnabled)
            {
                return Vector2.zero;
            }

            float horizontal = UnityEngine.Input.GetAxisRaw("Horizontal");
            float vertical = UnityEngine.Input.GetAxisRaw("Vertical");

            return new Vector2(horizontal, vertical);
        }

        #endregion

        #region Camera Input

        /// <summary>
        /// カメラ入力ベクトルを取得します。
        /// </summary>
        /// <returns>カメラベクトル（X: 水平、Y: 垂直）</returns>
        public Vector2 GetCameraInput()
        {
            if (!_inputEnabled)
            {
                return Vector2.zero;
            }

            float horizontal = UnityEngine.Input.GetAxis("Mouse X");
            float vertical = UnityEngine.Input.GetAxis("Mouse Y");

            // Y軸反転
            if (_invertYAxis)
            {
                vertical = -vertical;
            }

            return new Vector2(horizontal, vertical) * _cameraSensitivity;
        }

        #endregion

        #region Action Input

        /// <summary>
        /// 攻撃ボタンが押されているかを取得します。
        /// </summary>
        /// <remarks>
        /// ボタンを押し続けることで攻撃をチャージできます。
        /// 押されている時間に応じてチャージ量を増やす処理に使用します。
        /// </remarks>
        /// <returns>押されている場合true</returns>
        public bool GetAttackButton()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetButton("Fire1");
        }

        /// <summary>
        /// 攻撃ボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ攻撃の開始判定に使用します。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetAttackButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetButtonDown("Fire1");
        }

        /// <summary>
        /// 攻撃ボタンが離された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ攻撃を発動するタイミングの判定に使用します。
        /// ボタンを押している時間に応じてチャージされた攻撃を、ボタンを離した時に発動します。
        /// </remarks>
        /// <returns>離された瞬間の場合true</returns>
        public bool GetAttackButtonUp()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetButtonUp("Fire1");
        }

        /// <summary>
        /// 攻撃キャンセルボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ中の攻撃をキャンセルする際に使用します。
        /// デフォルトでは右クリック（Fire2）に割り当てられます。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetCancelAttackButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetButtonDown("Fire2");
        }

        /// <summary>
        /// 騎乗/降馬ボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetMountButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            // Eキーまたはゲームパッドのボタン
            return UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetButtonDown("Fire3");
        }

        /// <summary>
        /// ジャンプボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetJumpButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetButtonDown("Jump");
        }

        #endregion

        #region UI Input

        /// <summary>
        /// メニューボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// ゲーム中のオプションメニュー（設定変更、リタイア等）を開く際に使用します。
        /// デフォルトではESCキーに割り当てられます。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetMenuButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }

        /// <summary>
        /// ポーズボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetPauseButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            return UnityEngine.Input.GetButtonDown("Cancel");
        }

        #endregion

        #region Utility

        /// <summary>
        /// すべての入力をリセットします。
        /// </summary>
        public void ResetInput()
        {
            UnityEngine.Input.ResetInputAxes();
        }

        /// <summary>
        /// 入力を一時的に無効化します。
        /// </summary>
        public void DisableInput()
        {
            _inputEnabled = false;
            Debug.Log("[InputService] Input disabled.");
        }

        /// <summary>
        /// 入力を有効化します。
        /// </summary>
        public void EnableInput()
        {
            _inputEnabled = true;
            Debug.Log("[InputService] Input enabled.");
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 入力イベントをチェックして発火します（InputUpdaterから呼ばれます）
        /// </summary>
        internal void CheckAndFireEvents()
        {
            if (!_inputEnabled)
            {
                return;
            }

            // 攻撃ボタン押下
            if (GetAttackButtonDown())
            {
                AttackButtonPressed?.Invoke(this, EventArgs.Empty);
            }

            // 攻撃ボタン解放
            if (GetAttackButtonUp())
            {
                AttackButtonReleased?.Invoke(this, EventArgs.Empty);
            }

            // 攻撃キャンセルボタン
            if (GetCancelAttackButtonDown())
            {
                CancelAttackButtonPressed?.Invoke(this, EventArgs.Empty);
            }

            // 騎乗/降馬ボタン
            if (GetMountButtonDown())
            {
                MountButtonPressed?.Invoke(this, EventArgs.Empty);
            }

            // ジャンプボタン
            if (GetJumpButtonDown())
            {
                JumpButtonPressed?.Invoke(this, EventArgs.Empty);
            }

            // メニューボタン
            if (GetMenuButtonDown())
            {
                MenuButtonPressed?.Invoke(this, EventArgs.Empty);
            }

            // ポーズボタン
            if (GetPauseButtonDown())
            {
                PauseButtonPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region InputUpdater (Nested MonoBehaviour)

        /// <summary>
        /// 入力更新用のMonoBehaviour
        /// </summary>
        /// <remarks>
        /// InputServiceの内部クラスとして定義し、毎フレーム入力イベントをチェックします。
        /// このクラスは外部からアクセスされないため、ネストクラスとして定義されています。
        /// </remarks>
        private class InputUpdater : MonoBehaviour
        {
            private InputService? _service;

            /// <summary>
            /// InputUpdaterを初期化します。
            /// </summary>
            /// <param name="service">親となるInputService</param>
            public void Initialize(InputService service)
            {
                _service = service;
            }

            private void Update()
            {
                _service?.CheckAndFireEvents();
            }
        }

        #endregion
    }
}
