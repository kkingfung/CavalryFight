#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイハイライト（重要な瞬間のマーク）
    /// </summary>
    /// <remarks>
    /// スコア、エピックキル等の重要な瞬間を記録します。
    /// インスタントリプレイやハイライトリール作成に使用します。
    /// </remarks>
    [Serializable]
    public class ReplayHighlight
    {
        /// <summary>
        /// ハイライトの開始タイムスタンプ（秒）
        /// </summary>
        /// <remarks>
        /// マッチ開始からの経過時間です。
        /// 通常、イベント発生の数秒前から開始します。
        /// </remarks>
        public float StartTimestamp = 0f;

        /// <summary>
        /// ハイライトの終了タイムスタンプ（秒）
        /// </summary>
        /// <remarks>
        /// マッチ開始からの経過時間です。
        /// 通常、イベント発生の数秒後まで含みます。
        /// </remarks>
        public float EndTimestamp = 0f;

        /// <summary>
        /// ハイライトのタイプ
        /// </summary>
        public HighlightType HighlightType = HighlightType.Unknown;

        /// <summary>
        /// ハイライトのタイトル
        /// </summary>
        /// <remarks>
        /// 例：「First Score!」「Decisive Strike!」「Match Winner!」
        /// </remarks>
        public string Title = string.Empty;

        /// <summary>
        /// ハイライトの説明
        /// </summary>
        /// <remarks>
        /// 例：「Player scored at 1:23」「Enemy defeated 3 opponents」
        /// </remarks>
        public string Description = string.Empty;

        /// <summary>
        /// 関連するリプレイイベント
        /// </summary>
        /// <remarks>
        /// このハイライトのきっかけとなったイベントへの参照です。
        /// </remarks>
        public ReplayEvent? RelatedEvent = null;

        /// <summary>
        /// 重要度（0.0～1.0）
        /// </summary>
        /// <remarks>
        /// ハイライトの重要度を表します。
        /// 1.0が最も重要で、ハイライトリールの優先順位付けに使用します。
        /// </remarks>
        public float Importance = 0.5f;

        /// <summary>
        /// ReplayHighlightの新しいインスタンスを初期化します
        /// </summary>
        public ReplayHighlight()
        {
        }

        /// <summary>
        /// ReplayHighlightの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="startTimestamp">開始タイムスタンプ（秒）</param>
        /// <param name="endTimestamp">終了タイムスタンプ（秒）</param>
        /// <param name="highlightType">ハイライトタイプ</param>
        /// <param name="title">タイトル</param>
        public ReplayHighlight(float startTimestamp, float endTimestamp, HighlightType highlightType, string title)
        {
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            HighlightType = highlightType;
            Title = title;
        }

        /// <summary>
        /// ハイライトの持続時間を取得します（秒）
        /// </summary>
        public float Duration => EndTimestamp - StartTimestamp;

        /// <summary>
        /// イベントからハイライトを作成します（前後の余裕時間を含む）
        /// </summary>
        /// <param name="replayEvent">元となるイベント</param>
        /// <param name="beforeTime">イベント前の時間（秒）</param>
        /// <param name="afterTime">イベント後の時間（秒）</param>
        /// <param name="highlightType">ハイライトタイプ</param>
        /// <param name="title">タイトル</param>
        /// <param name="importance">重要度（0.0～1.0）</param>
        /// <returns>作成されたハイライト</returns>
        public static ReplayHighlight FromEvent(
            ReplayEvent replayEvent,
            float beforeTime,
            float afterTime,
            HighlightType highlightType,
            string title,
            float importance = 0.5f)
        {
            var startTime = Mathf.Max(0f, replayEvent.Timestamp - beforeTime);
            var endTime = replayEvent.Timestamp + afterTime;

            return new ReplayHighlight
            {
                StartTimestamp = startTime,
                EndTimestamp = endTime,
                HighlightType = highlightType,
                Title = title,
                Description = replayEvent.Description,
                RelatedEvent = replayEvent,
                Importance = importance
            };
        }

        /// <summary>
        /// スコアイベントからハイライトを自動作成します
        /// </summary>
        /// <param name="scoreEvent">スコアイベント</param>
        /// <returns>作成されたハイライト</returns>
        public static ReplayHighlight FromScoreEvent(ReplayEvent scoreEvent)
        {
            // スコアの3秒前から2秒後までを記録
            return FromEvent(
                scoreEvent,
                beforeTime: 3f,
                afterTime: 2f,
                HighlightType.Score,
                "Score!",
                importance: 1.0f
            );
        }

        /// <summary>
        /// ハイライトのコピーを作成します
        /// </summary>
        /// <returns>コピーされたハイライト</returns>
        public ReplayHighlight Clone()
        {
            return new ReplayHighlight
            {
                StartTimestamp = this.StartTimestamp,
                EndTimestamp = this.EndTimestamp,
                HighlightType = this.HighlightType,
                Title = this.Title,
                Description = this.Description,
                RelatedEvent = this.RelatedEvent?.Clone(),
                Importance = this.Importance
            };
        }
    }
}
