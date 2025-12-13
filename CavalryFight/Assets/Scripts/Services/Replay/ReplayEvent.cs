#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイイベント（特定時刻に発生したイベント）
    /// </summary>
    /// <remarks>
    /// スコア、死亡、攻撃ヒット等の重要なゲームイベントを記録します。
    /// ハイライト検出やイベントログに使用します。
    /// </remarks>
    [Serializable]
    public class ReplayEvent
    {
        /// <summary>
        /// イベントのタイムスタンプ（秒）
        /// </summary>
        /// <remarks>
        /// マッチ開始からの経過時間です。
        /// </remarks>
        public float Timestamp = 0f;

        /// <summary>
        /// イベントタイプ
        /// </summary>
        public ReplayEventType EventType = ReplayEventType.Unknown;

        /// <summary>
        /// イベントに関連する主体エンティティID
        /// </summary>
        /// <remarks>
        /// 例：攻撃者、スコアした人、死亡した人
        /// </remarks>
        public string SubjectEntityId = string.Empty;

        /// <summary>
        /// イベントに関連する対象エンティティID
        /// </summary>
        /// <remarks>
        /// 例：攻撃対象、アシストした人
        /// </remarks>
        public string? TargetEntityId = null;

        /// <summary>
        /// イベントの説明
        /// </summary>
        /// <remarks>
        /// 例：「Player scored」「Enemy was defeated」
        /// </remarks>
        public string Description = string.Empty;

        /// <summary>
        /// イベントのカスタムデータ（JSON形式）
        /// </summary>
        /// <remarks>
        /// イベント固有の追加情報を格納します。
        /// 例：攻撃力、スコア値、ダメージ量等
        /// </remarks>
        public Dictionary<string, string> CustomData = new Dictionary<string, string>();

        /// <summary>
        /// ReplayEventの新しいインスタンスを初期化します
        /// </summary>
        public ReplayEvent()
        {
        }

        /// <summary>
        /// ReplayEventの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="timestamp">タイムスタンプ（秒）</param>
        /// <param name="eventType">イベントタイプ</param>
        /// <param name="subjectEntityId">主体エンティティID</param>
        /// <param name="description">イベントの説明</param>
        public ReplayEvent(float timestamp, ReplayEventType eventType, string subjectEntityId, string description)
        {
            Timestamp = timestamp;
            EventType = eventType;
            SubjectEntityId = subjectEntityId;
            Description = description;
        }

        /// <summary>
        /// カスタムデータを追加します
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        public void AddCustomData(string key, string value)
        {
            CustomData[key] = value;
        }

        /// <summary>
        /// カスタムデータを取得します
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>値（見つからない場合はnull）</returns>
        public string? GetCustomData(string key)
        {
            if (CustomData.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// イベントのコピーを作成します
        /// </summary>
        /// <returns>コピーされたイベント</returns>
        public ReplayEvent Clone()
        {
            var cloned = new ReplayEvent
            {
                Timestamp = this.Timestamp,
                EventType = this.EventType,
                SubjectEntityId = this.SubjectEntityId,
                TargetEntityId = this.TargetEntityId,
                Description = this.Description,
                CustomData = new Dictionary<string, string>(this.CustomData)
            };

            return cloned;
        }
    }
}
