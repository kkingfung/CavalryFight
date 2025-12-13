#nullable enable

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力アクションの種類
    /// </summary>
    /// <remarks>
    /// プレイヤーが実行できるすべてのアクションを定義します。
    /// これらのアクションはキーバインディングシステムで使用され、
    /// プレイヤーが好みのキー/ボタンに再割り当てできます。
    /// </remarks>
    public enum InputAction
    {
        /// <summary>
        /// 前進
        /// </summary>
        MoveForward,

        /// <summary>
        /// 後退
        /// </summary>
        MoveBackward,

        /// <summary>
        /// 左移動
        /// </summary>
        MoveLeft,

        /// <summary>
        /// 右移動
        /// </summary>
        MoveRight,

        /// <summary>
        /// カメラ水平回転
        /// </summary>
        CameraHorizontal,

        /// <summary>
        /// カメラ垂直回転
        /// </summary>
        CameraVertical,

        /// <summary>
        /// 攻撃（チャージ可能）
        /// </summary>
        Attack,

        /// <summary>
        /// 攻撃キャンセル
        /// </summary>
        CancelAttack,

        /// <summary>
        /// 騎乗/降馬
        /// </summary>
        Mount,

        /// <summary>
        /// ジャンプ
        /// </summary>
        Jump,

        /// <summary>
        /// メニュー（設定、リタイア等）
        /// </summary>
        Menu,

        /// <summary>
        /// ポーズ
        /// </summary>
        Pause
    }
}
