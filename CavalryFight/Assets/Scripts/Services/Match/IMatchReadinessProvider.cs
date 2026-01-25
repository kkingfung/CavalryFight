#nullable enable

using System;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチの準備状態を提供するインターフェース
    /// </summary>
    /// <remarks>
    /// ローディング画面がマッチの準備完了を待機するために使用します。
    /// すべてのエンティティ（プレイヤー、AI）がスポーンされた後に
    /// AllEntitiesReadyイベントが発火されます。
    /// </remarks>
    public interface IMatchReadinessProvider
    {
        /// <summary>
        /// すべてのエンティティの準備が完了した時に発生します
        /// </summary>
        /// <remarks>
        /// プレイヤーとAIがすべてスポーンされた後に発火されます。
        /// ローディング画面はこのイベントを待ってから非表示になります。
        /// </remarks>
        event Action? AllEntitiesReady;

        /// <summary>
        /// エンティティの準備が完了しているかどうかを取得します
        /// </summary>
        bool AreAllEntitiesReady { get; }

        /// <summary>
        /// 現在のロード進捗を取得します（0.0～1.0）
        /// </summary>
        /// <remarks>
        /// 0.0: 初期化開始
        /// 0.3: フィールドロード中
        /// 0.5: プレイヤースポーン中
        /// 0.7: AIスポーン中
        /// 1.0: 準備完了
        /// </remarks>
        float EntityLoadProgress { get; }

        /// <summary>
        /// 現在のロードステータスメッセージを取得します
        /// </summary>
        string LoadStatusMessage { get; }
    }
}
