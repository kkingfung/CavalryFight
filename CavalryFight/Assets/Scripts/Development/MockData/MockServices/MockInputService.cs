#nullable enable

#if UNITY_EDITOR

using System;
using CavalryFight.Services.Input;
using UnityEngine;

namespace CavalryFight.Development.MockData.MockServices
{
    /// <summary>
    /// モックの入力サービス
    /// </summary>
    /// <remarks>
    /// 開発時に個別シーンをテストする際に使用します。
    /// 実際のInput Systemをラップして動作します。
    /// </remarks>
    public class MockInputService : IInputService
    {
        #region Events

        /// <summary>
        /// 攻撃ボタンが押された時に発生します
        /// </summary>
        public event EventHandler? AttackButtonPressed;

        /// <summary>
        /// 攻撃ボタンが離された時に発生します
        /// </summary>
        public event EventHandler? AttackButtonReleased;

        /// <summary>
        /// 攻撃キャンセルボタンが押された時に発生します
        /// </summary>
        public event EventHandler? CancelAttackButtonPressed;

        /// <summary>
        /// ブーストボタンが押された時に発生します
        /// </summary>
        public event EventHandler? BoostButtonPressed;

        /// <summary>
        /// 騎乗/降馬ボタンが押された時に発生します
        /// </summary>
        public event EventHandler? MountButtonPressed;

        /// <summary>
        /// ジャンプボタンが押された時に発生します
        /// </summary>
        public event EventHandler? JumpButtonPressed;

        /// <summary>
        /// メニューボタンが押された時に発生します
        /// </summary>
        public event EventHandler? MenuButtonPressed;

        #endregion

        #region Properties

        /// <summary>
        /// 入力が有効かどうかを取得または設定します
        /// </summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>
        /// 移動入力の感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float MovementSensitivity { get; set; } = 1f;

        /// <summary>
        /// カメラ入力の感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float CameraSensitivity { get; set; } = 1f;

        /// <summary>
        /// Y軸を反転するかどうかを取得または設定します
        /// </summary>
        public bool InvertYAxis { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 移動入力ベクトルを取得します（正規化済み）
        /// </summary>
        /// <returns>移動ベクトル（X: 水平、Y: 垂直）</returns>
        public Vector2 GetMovementInput()
        {
            if (!InputEnabled)
            {
                return Vector2.zero;
            }
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        /// <summary>
        /// 生の移動入力ベクトルを取得します（正規化なし）
        /// </summary>
        /// <returns>生の移動ベクトル</returns>
        public Vector2 GetRawMovementInput()
        {
            if (!InputEnabled)
            {
                return Vector2.zero;
            }
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        /// <summary>
        /// カメラ入力ベクトルを取得します
        /// </summary>
        /// <returns>カメラベクトル（X: 水平、Y: 垂直）</returns>
        public Vector2 GetCameraInput()
        {
            if (!InputEnabled)
            {
                return Vector2.zero;
            }
            float x = Input.GetAxis("Mouse X") * CameraSensitivity;
            float y = Input.GetAxis("Mouse Y") * CameraSensitivity;
            if (InvertYAxis)
            {
                y = -y;
            }
            return new Vector2(x, y);
        }

        /// <summary>
        /// 攻撃ボタンが押されているかを取得します
        /// </summary>
        /// <returns>押されている場合true</returns>
        public bool GetAttackButton()
        {
            return InputEnabled && Input.GetButton("Fire1");
        }

        /// <summary>
        /// 攻撃ボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetAttackButtonDown()
        {
            if (InputEnabled && Input.GetButtonDown("Fire1"))
            {
                AttackButtonPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 攻撃ボタンが離された瞬間かを取得します
        /// </summary>
        /// <returns>離された瞬間の場合true</returns>
        public bool GetAttackButtonUp()
        {
            if (InputEnabled && Input.GetButtonUp("Fire1"))
            {
                AttackButtonReleased?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 攻撃キャンセルボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetCancelAttackButtonDown()
        {
            if (InputEnabled && Input.GetButtonDown("Fire2"))
            {
                CancelAttackButtonPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 騎乗/降馬ボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetMountButtonDown()
        {
            if (InputEnabled && Input.GetKeyDown(KeyCode.E))
            {
                MountButtonPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// ジャンプボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetJumpButtonDown()
        {
            if (InputEnabled && Input.GetButtonDown("Jump"))
            {
                JumpButtonPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// ブーストボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetBoostButtonDown()
        {
            if (InputEnabled && Input.GetButtonDown("Fire2"))
            {
                BoostButtonPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// スプリントボタンが押されているかを取得します
        /// </summary>
        /// <returns>押されている場合true</returns>
        public bool GetSprintButton()
        {
            return InputEnabled && Input.GetKey(KeyCode.LeftShift);
        }

        /// <summary>
        /// メニューボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetMenuButtonDown()
        {
            if (InputEnabled && Input.GetKeyDown(KeyCode.Escape))
            {
                MenuButtonPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// スコアボードボタンが押された瞬間かを取得します
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        public bool GetScoreboardButtonDown()
        {
            return InputEnabled && Input.GetKeyDown(KeyCode.Tab);
        }

        /// <summary>
        /// スコアボードボタンが離された瞬間かを取得します
        /// </summary>
        /// <returns>離された瞬間の場合true</returns>
        public bool GetScoreboardButtonUp()
        {
            return InputEnabled && Input.GetKeyUp(KeyCode.Tab);
        }

        /// <summary>
        /// すべての入力をリセットします
        /// </summary>
        public void ResetInput()
        {
            Debug.Log("[MockInputService] ResetInput");
        }

        /// <summary>
        /// 入力を一時的に無効化します
        /// </summary>
        public void DisableInput()
        {
            InputEnabled = false;
            Debug.Log("[MockInputService] DisableInput");
        }

        /// <summary>
        /// 入力を有効化します
        /// </summary>
        public void EnableInput()
        {
            InputEnabled = true;
            Debug.Log("[MockInputService] EnableInput");
        }

        /// <summary>
        /// キーバインディングを再読み込みします
        /// </summary>
        public void ReloadBindingOverrides()
        {
            Debug.Log("[MockInputService] ReloadBindingOverrides");
        }

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[MockInputService] Initialize");
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[MockInputService] Dispose");
        }

        #endregion
    }
}

#endif
