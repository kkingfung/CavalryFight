#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイカメラコントローラーのインターフェース
    /// </summary>
    /// <remarks>
    /// リプレイ中のカメラ制御を抽象化します。
    /// フリーカメラ、シネマティックカメラ等の実装に使用します。
    /// </remarks>
    public interface IReplayCameraController
    {
        /// <summary>
        /// カメラが有効かどうかを取得または設定します
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// カメラを初期化します
        /// </summary>
        /// <param name="camera">制御するカメラ</param>
        void Initialize(Camera camera);

        /// <summary>
        /// カメラの更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        /// <param name="currentFrame">現在のリプレイフレーム</param>
        void UpdateCamera(float deltaTime, ReplayFrame? currentFrame);

        /// <summary>
        /// カメラをリセットします（初期位置に戻す）
        /// </summary>
        void Reset();

        /// <summary>
        /// カメラを破棄します
        /// </summary>
        void Dispose();
    }
}
