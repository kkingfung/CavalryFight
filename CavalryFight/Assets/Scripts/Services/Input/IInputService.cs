#nullable enable

using System;
using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// プレイヤーの入力（キーボード、マウス、ゲームパッド）を管理します。
    /// MVVMパターンでの入力処理を容易にします。
    /// </remarks>
    public interface IInputService : IService
    {
        #region Events

        /// <summary>
        /// 攻撃ボタンが押された時に発生します。
        /// </summary>
        event EventHandler? AttackButtonPressed;

        /// <summary>
        /// 攻撃ボタンが離された時に発生します。
        /// </summary>
        /// <remarks>
        /// チャージ攻撃の発動タイミングに使用します。
        /// </remarks>
        event EventHandler? AttackButtonReleased;

        /// <summary>
        /// 攻撃キャンセルボタンが押された時に発生します。
        /// </summary>
        /// <remarks>
        /// チャージ中の攻撃をキャンセルする際に使用します。
        /// デフォルトでは右クリック（Fire2）に割り当てられます。
        /// </remarks>
        event EventHandler? CancelAttackButtonPressed;

        /// <summary>
        /// 騎乗/降馬ボタンが押された時に発生します。
        /// </summary>
        event EventHandler? MountButtonPressed;

        /// <summary>
        /// ジャンプボタンが押された時に発生します。
        /// </summary>
        event EventHandler? JumpButtonPressed;

        /// <summary>
        /// メニューボタンが押された時に発生します。
        /// </summary>
        /// <remarks>
        /// ゲーム中のオプションメニュー（設定変更、リタイア等）を開く際に使用します。
        /// デフォルトではESCキーに割り当てられます。
        /// </remarks>
        event EventHandler? MenuButtonPressed;

        /// <summary>
        /// ポーズボタンが押された時に発生します。
        /// </summary>
        event EventHandler? PauseButtonPressed;

        #endregion

        #region Properties

        /// <summary>
        /// 入力が有効かどうかを取得または設定します。
        /// </summary>
        bool InputEnabled { get; set; }

        /// <summary>
        /// 移動入力の感度を取得または設定します（0.0～1.0）
        /// </summary>
        float MovementSensitivity { get; set; }

        /// <summary>
        /// カメラ入力の感度を取得または設定します（0.0～1.0）
        /// </summary>
        float CameraSensitivity { get; set; }

        /// <summary>
        /// Y軸を反転するかどうかを取得または設定します。
        /// </summary>
        bool InvertYAxis { get; set; }

        #endregion

        #region Movement Input

        /// <summary>
        /// 移動入力ベクトルを取得します（正規化済み）
        /// </summary>
        /// <returns>移動ベクトル（X: 水平、Y: 垂直）</returns>
        Vector2 GetMovementInput();

        /// <summary>
        /// 生の移動入力ベクトルを取得します（正規化なし）
        /// </summary>
        /// <returns>生の移動ベクトル</returns>
        Vector2 GetRawMovementInput();

        #endregion

        #region Camera Input

        /// <summary>
        /// カメラ入力ベクトルを取得します。
        /// </summary>
        /// <returns>カメラベクトル（X: 水平、Y: 垂直）</returns>
        Vector2 GetCameraInput();

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
        bool GetAttackButton();

        /// <summary>
        /// 攻撃ボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ攻撃の開始判定に使用します。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        bool GetAttackButtonDown();

        /// <summary>
        /// 攻撃ボタンが離された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ攻撃を発動するタイミングの判定に使用します。
        /// ボタンを押している時間に応じてチャージされた攻撃を、ボタンを離した時に発動します。
        /// </remarks>
        /// <returns>離された瞬間の場合true</returns>
        bool GetAttackButtonUp();

        /// <summary>
        /// 攻撃キャンセルボタンが押された瞬間かを取得します。
        /// </summary>
        /// <remarks>
        /// チャージ中の攻撃をキャンセルする際に使用します。
        /// デフォルトでは右クリック（Fire2）に割り当てられます。
        /// </remarks>
        /// <returns>押された瞬間の場合true</returns>
        bool GetCancelAttackButtonDown();

        /// <summary>
        /// 騎乗/降馬ボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        bool GetMountButtonDown();

        /// <summary>
        /// ジャンプボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        bool GetJumpButtonDown();

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
        bool GetMenuButtonDown();

        /// <summary>
        /// ポーズボタンが押された瞬間かを取得します。
        /// </summary>
        /// <returns>押された瞬間の場合true</returns>
        bool GetPauseButtonDown();

        #endregion

        #region Utility

        /// <summary>
        /// すべての入力をリセットします。
        /// </summary>
        void ResetInput();

        /// <summary>
        /// 入力を一時的に無効化します。
        /// </summary>
        void DisableInput();

        /// <summary>
        /// 入力を有効化します。
        /// </summary>
        void EnableInput();

        #endregion
    }
}
