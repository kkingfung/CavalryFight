#nullable enable

using System;
using UnityEngine;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ録画サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// ゲームプレイの録画を管理します。
    /// キーフレーム、イベント、ハイライトを記録し、
    /// ReplayDataとして保存できます。
    /// </remarks>
    public interface IReplayRecorder : IService
    {
        #region Properties

        /// <summary>
        /// 現在録画中かどうかを取得します
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// 現在の録画データを取得します
        /// </summary>
        /// <remarks>
        /// 録画中でない場合はnull
        /// </remarks>
        ReplayData? CurrentRecording { get; }

        /// <summary>
        /// 録画開始からの経過時間（秒）を取得します
        /// </summary>
        float RecordingTime { get; }

        #endregion

        #region Events

        /// <summary>
        /// 録画が開始された時に発生します
        /// </summary>
        event EventHandler? RecordingStarted;

        /// <summary>
        /// 録画が停止された時に発生します
        /// </summary>
        event EventHandler<ReplayRecordingStoppedEventArgs>? RecordingStopped;

        /// <summary>
        /// フレームが記録された時に発生します
        /// </summary>
        event EventHandler<ReplayFrameRecordedEventArgs>? FrameRecorded;

        /// <summary>
        /// イベントが記録された時に発生します
        /// </summary>
        event EventHandler<ReplayEventRecordedEventArgs>? EventRecorded;

        #endregion

        #region Methods

        /// <summary>
        /// 録画を開始します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="playerName">プレイヤー名</param>
        void StartRecording(string mapName, string gameMode, string playerName = "Player");

        /// <summary>
        /// 録画を停止します
        /// </summary>
        /// <returns>録画されたリプレイデータ</returns>
        ReplayData? StopRecording();

        /// <summary>
        /// エンティティを録画対象として登録します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="entityType">エンティティタイプ</param>
        /// <param name="gameObject">エンティティのGameObject</param>
        void RegisterEntity(string entityId, EntityType entityType, GameObject gameObject);

        /// <summary>
        /// エンティティの登録を解除します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        void UnregisterEntity(string entityId);

        /// <summary>
        /// イベントを記録します
        /// </summary>
        /// <param name="eventType">イベントタイプ</param>
        /// <param name="subjectEntityId">主体エンティティID</param>
        /// <param name="description">イベントの説明</param>
        /// <param name="targetEntityId">対象エンティティID（オプション）</param>
        void RecordEvent(ReplayEventType eventType, string subjectEntityId, string description, string? targetEntityId = null);

        /// <summary>
        /// スコアを更新します（次のフレーム記録時に反映されます）
        /// </summary>
        /// <param name="playerScore">プレイヤースコア</param>
        /// <param name="enemyScore">敵スコア</param>
        void UpdateScore(int playerScore, int enemyScore);

        /// <summary>
        /// 録画の更新処理（MonoBehaviourのUpdateから呼ぶ必要があります）
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        void UpdateRecording(float deltaTime);

        /// <summary>
        /// リプレイをファイルに保存します
        /// </summary>
        /// <param name="replay">保存するリプレイデータ</param>
        /// <param name="fileName">ファイル名（拡張子なし、省略時は自動生成）</param>
        /// <returns>保存に成功したかどうか</returns>
        bool SaveReplay(ReplayData replay, string? fileName = null);

        #endregion
    }
}
