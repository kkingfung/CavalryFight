#nullable enable

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CavalryFight.Core.Services;

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
        /// デフォルトでは右クリックに割り当てられます。
        /// </remarks>
        public event EventHandler? CancelAttackButtonPressed;

        /// <summary>
        /// ブーストボタンが押された時に発生します。
        /// </summary>
        /// <remarks>
        /// 馬の突然のスピードブーストに使用します。
        /// 攻撃チャージ中でない場合に右クリックで発動します。
        /// </remarks>
        public event EventHandler? BoostButtonPressed;

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

        #endregion

        #region Fields

        private bool _inputEnabled = true;
        private float _cameraSensitivity = 1.0f;
        private bool _invertYAxis = false;
        private GameInputActions? _inputActions;

        /// <summary>
        /// 攻撃がチャージ中かどうかを示すフラグ
        /// </summary>
        private bool _isAttackCharging = false;

        #endregion

        #region Properties

        /// <summary>
        /// 入力が有効かどうかを取得または設定します。
        /// </summary>
        /// <remarks>
        /// ゲームプレイ入力の有効/無効を制御します。
        /// UI入力（メニューボタン等）は常に有効です。
        /// </remarks>
        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (_inputActions != null)
                {
                    if (_inputEnabled)
                    {
                        // ゲームプレイ入力を有効化
                        _inputActions.Gameplay.Enable();
                    }
                    else
                    {
                        // 入力無効化時に攻撃チャージ状態をリセット
                        _isAttackCharging = false;
                        // ゲームプレイ入力のみ無効化（UI入力は有効のまま）
                        _inputActions.Gameplay.Disable();
                    }
                    // UI入力は常に有効
                    _inputActions.UI.Enable();
                }
            }
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
        /// Input Actionsを作成し、イベントを購読します。
        /// </remarks>
        public void Initialize()
        {
            Debug.LogWarning("[InputService] ========== INITIALIZING ==========");

            // GameInputActionsを作成
            _inputActions = new GameInputActions();
            Debug.Log("[InputService] GameInputActions created.");

            // 保存されたキーバインディングを読み込む
            LoadBindingOverrides();

            // イベントを購読
            _inputActions.Gameplay.Attack.started += OnAttackStarted;
            _inputActions.Gameplay.Attack.canceled += OnAttackCanceled;
            _inputActions.Gameplay.CancelAttack.performed += OnCancelAttackPerformed;
            _inputActions.Gameplay.Boost.performed += OnBoostPerformed;
            _inputActions.Gameplay.Mount.performed += OnMountPerformed;
            _inputActions.Gameplay.Jump.performed += OnJumpPerformed;
            _inputActions.UI.Menu.performed += OnMenuPerformed;
            Debug.Log("[InputService] Event handlers subscribed.");

            // 入力を有効化
            _inputActions.Enable();

            Debug.LogWarning($"[InputService] ========== INITIALIZED (InputEnabled={_inputEnabled}) ==========");
        }

        /// <summary>
        /// 保存されたキーバインディングオーバーライドを読み込みます。
        /// </summary>
        /// <remarks>
        /// KeyBindingViewModelで保存されたカスタムバインディングを読み込んで適用します。
        /// PlayerPrefsの "InputBindings" キーからJSON形式で読み込みます。
        /// </remarks>
        private void LoadBindingOverrides()
        {
            if (_inputActions == null)
            {
                return;
            }

            string rebinds = PlayerPrefs.GetString("InputBindings", string.Empty);

            if (!string.IsNullOrEmpty(rebinds))
            {
                _inputActions.asset.LoadBindingOverridesFromJson(rebinds);
                Debug.Log("[InputService] Custom key bindings loaded from PlayerPrefs.");
            }
            else
            {
                Debug.Log("[InputService] No custom key bindings found, using defaults.");
            }
        }

        /// <summary>
        /// キーバインディングを再読み込みします。
        /// </summary>
        /// <remarks>
        /// 設定画面でバインディングが変更された後に呼び出して、
        /// 変更を即座に反映させます。
        /// </remarks>
        public void ReloadBindingOverrides()
        {
            LoadBindingOverrides();
            Debug.Log("[InputService] Key bindings reloaded.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        /// <remarks>
        /// イベントハンドラをクリアし、Input Actionsを破棄します。
        /// </remarks>
        public void Dispose()
        {
            Debug.Log("[InputService] Disposing...");

            // イベントハンドラをクリア
            AttackButtonPressed = null;
            AttackButtonReleased = null;
            CancelAttackButtonPressed = null;
            BoostButtonPressed = null;
            MountButtonPressed = null;
            JumpButtonPressed = null;
            MenuButtonPressed = null;

            // Input Actionsのイベント購読を解除
            if (_inputActions != null)
            {
                _inputActions.Gameplay.Attack.started -= OnAttackStarted;
                _inputActions.Gameplay.Attack.canceled -= OnAttackCanceled;
                _inputActions.Gameplay.CancelAttack.performed -= OnCancelAttackPerformed;
                _inputActions.Gameplay.Boost.performed -= OnBoostPerformed;
                _inputActions.Gameplay.Mount.performed -= OnMountPerformed;
                _inputActions.Gameplay.Jump.performed -= OnJumpPerformed;
                _inputActions.UI.Menu.performed -= OnMenuPerformed;

                _inputActions.Dispose();
                _inputActions = null;
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
            if (_inputActions == null)
            {
                Debug.LogWarning("[InputService] GetMovementInput: _inputActions is null!");
                return Vector2.zero;
            }

            if (!_inputEnabled)
            {
                // 入力無効時は静かにゼロを返す（毎フレームログは出さない）
                return Vector2.zero;
            }

            Vector2 input = _inputActions.Gameplay.Move.ReadValue<Vector2>();

            // デバッグ: 入力があった時のみログ出力
            if (input.magnitude > 0.01f)
            {
                Debug.Log($"[InputService] Move input detected: {input}");
            }

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
            if (!_inputEnabled || _inputActions == null)
            {
                return Vector2.zero;
            }

            return _inputActions.Gameplay.Move.ReadValue<Vector2>();
        }

        #endregion

        #region Camera Input

        /// <summary>
        /// カメラ入力ベクトルを取得します。
        /// </summary>
        /// <returns>カメラベクトル（X: 水平、Y: 垂直）</returns>
        public Vector2 GetCameraInput()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return Vector2.zero;
            }

            Vector2 input = _inputActions.Gameplay.Camera.ReadValue<Vector2>();

            // マウスデルタの場合はスケーリングが必要
            var activeControl = _inputActions.Gameplay.Camera.activeControl;
            if (activeControl != null && activeControl.device is Mouse)
            {
                input *= 0.01f; // マウスデルタをスケーリング
            }

            // Y軸反転
            if (_invertYAxis)
            {
                input.y = -input.y;
            }

            return input * _cameraSensitivity;
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
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Attack.IsPressed();
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
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Attack.WasPressedThisFrame();
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
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Attack.WasReleasedThisFrame();
        }

        /// <summary>
        /// 攻撃キャンセルボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ中の攻撃をキャンセルする際に使用します。
        /// デフォルトでは右クリックに割り当てられます。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetCancelAttackButtonDown()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.CancelAttack.WasPressedThisFrame();
        }

        /// <summary>
        /// ブーストボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// 馬の突然のスピードブーストに使用します。
        /// 攻撃チャージ中でない場合に右クリックで発動します。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetBoostButtonDown()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Boost.WasPressedThisFrame();
        }

        /// <summary>
        /// 騎乗/降馬ボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetMountButtonDown()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Mount.WasPressedThisFrame();
        }

        /// <summary>
        /// ジャンプボタンが押されているかを取得します。
        /// </summary>
        /// <returns>押されている場合true</returns>
        public bool GetJumpButton()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Jump.IsPressed();
        }

        /// <summary>
        /// ジャンプボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetJumpButtonDown()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Jump.WasPressedThisFrame();
        }

        /// <summary>
        /// スプリントボタンが押されているかを取得します。
        /// </summary>
        /// <remarks>
        /// 継続的な高速移動に使用します。
        /// ボタンを押し続けている間、スプリント状態が維持されます。
        /// デフォルトではShiftキーに割り当てられます。
        /// </remarks>
        /// <returns>押されている場合true</returns>
        public bool GetSprintButton()
        {
            if (!_inputEnabled || _inputActions == null)
            {
                return false;
            }

            return _inputActions.Gameplay.Sprint.IsPressed();
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
            // メニューボタンは常に有効（ポーズ中でも操作可能にするため）
            if (_inputActions == null)
            {
                return false;
            }

            return _inputActions.UI.Menu.WasPressedThisFrame();
        }

        /// <summary>
        /// スコアボードボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// マッチ中のスコアボードを表示する際に使用します。
        /// デフォルトではTabキーに割り当てられます。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetScoreboardButtonDown()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            // Tabキーを直接チェック（Input Actionsに追加されていない場合のフォールバック）
            return UnityEngine.Input.GetKeyDown(KeyCode.Tab);
        }

        /// <summary>
        /// スコアボードボタンが離された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// スコアボードを閉じる際に使用します。
        /// デフォルトではTabキーに割り当てられます。
        /// </remarks>
        /// <returns>離された瞬間の場合true</returns>
        public bool GetScoreboardButtonUp()
        {
            if (!_inputEnabled)
            {
                return false;
            }

            // Tabキーを直接チェック（Input Actionsに追加されていない場合のフォールバック）
            return UnityEngine.Input.GetKeyUp(KeyCode.Tab);
        }

        #endregion

        #region Utility

        /// <summary>
        /// すべての入力をリセットします。
        /// </summary>
        public void ResetInput()
        {
            // 攻撃チャージ状態をリセット
            _isAttackCharging = false;

            // 新しいInput Systemでは他の状態は自動的にリセットされる
            Debug.Log("[InputService] Input reset.");
        }

        /// <summary>
        /// 入力を一時的に無効化します。
        /// </summary>
        public void DisableInput()
        {
            InputEnabled = false;
            Debug.Log("[InputService] Input disabled.");
        }

        /// <summary>
        /// 入力を有効化します。
        /// </summary>
        public void EnableInput()
        {
            InputEnabled = true;
            Debug.Log("[InputService] Input enabled.");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 攻撃ボタンが押された時のハンドラ
        /// </summary>
        private void OnAttackStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _isAttackCharging = true;
            AttackButtonPressed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 攻撃ボタンが離された時のハンドラ
        /// </summary>
        private void OnAttackCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _isAttackCharging = false;
            AttackButtonReleased?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 攻撃キャンセルボタンが押された時のハンドラ
        /// </summary>
        /// <remarks>
        /// 攻撃チャージ中の場合のみ、CancelAttackButtonPressedイベントを発火します。
        /// それ以外の場合は無視され、Boostイベントが処理されます。
        /// </remarks>
        private void OnCancelAttackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // 攻撃チャージ中の場合のみキャンセルイベントを発火
            if (_isAttackCharging)
            {
                _isAttackCharging = false;
                CancelAttackButtonPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// ブーストボタンが押された時のハンドラ
        /// </summary>
        /// <remarks>
        /// 攻撃チャージ中でない場合のみ、BoostButtonPressedイベントを発火します。
        /// 攻撃チャージ中の場合は無視され、CancelAttackイベントが処理されます。
        /// </remarks>
        private void OnBoostPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // 攻撃チャージ中でない場合のみブーストイベントを発火
            if (!_isAttackCharging)
            {
                BoostButtonPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 騎乗/降馬ボタンが押された時のハンドラ
        /// </summary>
        private void OnMountPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            MountButtonPressed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ジャンプボタンが押された時のハンドラ
        /// </summary>
        private void OnJumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            JumpButtonPressed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// メニューボタンが押された時のハンドラ
        /// </summary>
        private void OnMenuPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // メニューを開く際に攻撃チャージ状態をリセット
            _isAttackCharging = false;
            MenuButtonPressed?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
