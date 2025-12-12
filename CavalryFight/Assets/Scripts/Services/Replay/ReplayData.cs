#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイデータ（マッチ全体の記録）
    /// </summary>
    /// <remarks>
    /// リプレイの完全なデータを保持します。
    /// フレーム、イベント、ハイライト、メタデータを含みます。
    /// JSON形式でファイルに保存/読み込みできます。
    /// </remarks>
    [Serializable]
    public class ReplayData
    {
        #region Metadata

        /// <summary>
        /// リプレイのユニークID
        /// </summary>
        public string ReplayId = Guid.NewGuid().ToString();

        /// <summary>
        /// リプレイ作成日時（ISO 8601形式）
        /// </summary>
        public string RecordedAt = DateTime.UtcNow.ToString("o");

        /// <summary>
        /// マッチの持続時間（秒）
        /// </summary>
        public float MatchDuration = 0f;

        /// <summary>
        /// 最終プレイヤースコア
        /// </summary>
        public int FinalPlayerScore = 0;

        /// <summary>
        /// 最終敵スコア
        /// </summary>
        public int FinalEnemyScore = 0;

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName = string.Empty;

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode = string.Empty;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName = "Player";

        #endregion

        #region Replay Content

        /// <summary>
        /// リプレイフレームのリスト
        /// </summary>
        /// <remarks>
        /// タイムスタンプ順にソートされている必要があります。
        /// </remarks>
        public List<ReplayFrame> Frames = new List<ReplayFrame>();

        /// <summary>
        /// リプレイイベントのリスト
        /// </summary>
        /// <remarks>
        /// タイムスタンプ順にソートされている必要があります。
        /// </remarks>
        public List<ReplayEvent> Events = new List<ReplayEvent>();

        /// <summary>
        /// リプレイハイライトのリスト
        /// </summary>
        /// <remarks>
        /// 重要度順にソートすることを推奨します。
        /// </remarks>
        public List<ReplayHighlight> Highlights = new List<ReplayHighlight>();

        #endregion

        #region Constructor

        /// <summary>
        /// ReplayDataの新しいインスタンスを初期化します
        /// </summary>
        public ReplayData()
        {
        }

        #endregion

        #region Frame Management

        /// <summary>
        /// フレームを追加します
        /// </summary>
        /// <param name="frame">追加するフレーム</param>
        public void AddFrame(ReplayFrame frame)
        {
            Frames.Add(frame);
        }

        /// <summary>
        /// 指定されたタイムスタンプに最も近いフレームを取得します
        /// </summary>
        /// <param name="timestamp">タイムスタンプ（秒）</param>
        /// <returns>最も近いフレーム、フレームがない場合はnull</returns>
        public ReplayFrame? GetFrameAtTime(float timestamp)
        {
            if (Frames.Count == 0)
            {
                return null;
            }

            // 二分探索で最も近いフレームを見つける
            int closestIndex = 0;
            float closestDistance = Mathf.Abs(Frames[0].Timestamp - timestamp);

            for (int i = 1; i < Frames.Count; i++)
            {
                float distance = Mathf.Abs(Frames[i].Timestamp - timestamp);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
                else if (Frames[i].Timestamp > timestamp)
                {
                    // タイムスタンプを超えたので終了
                    break;
                }
            }

            return Frames[closestIndex];
        }

        /// <summary>
        /// 指定されたタイムスタンプの前後のフレームを取得します
        /// </summary>
        /// <param name="timestamp">タイムスタンプ（秒）</param>
        /// <returns>前のフレームと後のフレームのタプル、見つからない場合はnull</returns>
        public (ReplayFrame? before, ReplayFrame? after) GetFramesAroundTime(float timestamp)
        {
            if (Frames.Count == 0)
            {
                return (null, null);
            }

            ReplayFrame? before = null;
            ReplayFrame? after = null;

            for (int i = 0; i < Frames.Count; i++)
            {
                if (Frames[i].Timestamp <= timestamp)
                {
                    before = Frames[i];
                }
                else
                {
                    after = Frames[i];
                    break;
                }
            }

            return (before, after);
        }

        /// <summary>
        /// 指定されたタイムスタンプで補間されたフレームを取得します
        /// </summary>
        /// <param name="timestamp">タイムスタンプ（秒）</param>
        /// <returns>補間されたフレーム、補間できない場合はnull</returns>
        public ReplayFrame? GetInterpolatedFrame(float timestamp)
        {
            var (before, after) = GetFramesAroundTime(timestamp);

            if (before == null && after == null)
            {
                return null;
            }

            if (before == null)
            {
                return after;
            }

            if (after == null)
            {
                return before;
            }

            // 補間係数を計算
            float duration = after.Timestamp - before.Timestamp;
            if (duration <= 0f)
            {
                return before;
            }

            float t = (timestamp - before.Timestamp) / duration;
            return ReplayFrame.Lerp(before, after, t);
        }

        #endregion

        #region Event Management

        /// <summary>
        /// イベントを追加します
        /// </summary>
        /// <param name="replayEvent">追加するイベント</param>
        public void AddEvent(ReplayEvent replayEvent)
        {
            Events.Add(replayEvent);
        }

        /// <summary>
        /// 指定された時間範囲内のイベントを取得します
        /// </summary>
        /// <param name="startTime">開始時刻（秒）</param>
        /// <param name="endTime">終了時刻（秒）</param>
        /// <returns>該当するイベントのリスト</returns>
        public List<ReplayEvent> GetEventsInRange(float startTime, float endTime)
        {
            var result = new List<ReplayEvent>();
            foreach (var replayEvent in Events)
            {
                if (replayEvent.Timestamp >= startTime && replayEvent.Timestamp <= endTime)
                {
                    result.Add(replayEvent);
                }
            }
            return result;
        }

        /// <summary>
        /// 指定されたタイプのイベントをすべて取得します
        /// </summary>
        /// <param name="eventType">イベントタイプ</param>
        /// <returns>該当するイベントのリスト</returns>
        public List<ReplayEvent> GetEventsByType(ReplayEventType eventType)
        {
            var result = new List<ReplayEvent>();
            foreach (var replayEvent in Events)
            {
                if (replayEvent.EventType == eventType)
                {
                    result.Add(replayEvent);
                }
            }
            return result;
        }

        #endregion

        #region Highlight Management

        /// <summary>
        /// ハイライトを追加します
        /// </summary>
        /// <param name="highlight">追加するハイライト</param>
        public void AddHighlight(ReplayHighlight highlight)
        {
            Highlights.Add(highlight);
        }

        /// <summary>
        /// ハイライトを重要度順にソートします
        /// </summary>
        public void SortHighlightsByImportance()
        {
            Highlights.Sort((a, b) => b.Importance.CompareTo(a.Importance));
        }

        /// <summary>
        /// スコアイベントから自動的にハイライトを生成します
        /// </summary>
        public void GenerateHighlightsFromScoreEvents()
        {
            var scoreEvents = GetEventsByType(ReplayEventType.Score);
            foreach (var scoreEvent in scoreEvents)
            {
                var highlight = ReplayHighlight.FromScoreEvent(scoreEvent);
                AddHighlight(highlight);
            }

            SortHighlightsByImportance();

            Debug.Log($"[ReplayData] Generated {Highlights.Count} highlights from {scoreEvents.Count} score events.");
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// リプレイデータをJSON文字列に変換します
        /// </summary>
        /// <returns>JSON文字列</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, prettyPrint: true);
        }

        /// <summary>
        /// リプレイデータをファイルに保存します
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>保存に成功した場合true</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                // ディレクトリが存在しない場合は作成
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // JSON化して保存
                string json = ToJson();
                File.WriteAllText(filePath, json);

                Debug.Log($"[ReplayData] Replay saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayData] Failed to save replay: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// JSON文字列からリプレイデータを作成します
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>デシリアライズされたリプレイデータ、失敗した場合はnull</returns>
        public static ReplayData? FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<ReplayData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayData] Failed to deserialize replay: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ファイルからリプレイデータを読み込みます
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>読み込まれたリプレイデータ、失敗した場合はnull</returns>
        public static ReplayData? LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[ReplayData] Replay file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var replay = FromJson(json);

                if (replay != null)
                {
                    Debug.Log($"[ReplayData] Replay loaded: {replay.ReplayId} ({replay.Frames.Count} frames)");
                }

                return replay;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayData] Failed to load replay: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// リプレイデータの概要を取得します
        /// </summary>
        /// <returns>概要文字列</returns>
        public string GetSummary()
        {
            return $"Replay {ReplayId}\n" +
                   $"Recorded: {RecordedAt}\n" +
                   $"Duration: {MatchDuration:F1}s\n" +
                   $"Score: {FinalPlayerScore} - {FinalEnemyScore}\n" +
                   $"Frames: {Frames.Count}\n" +
                   $"Events: {Events.Count}\n" +
                   $"Highlights: {Highlights.Count}";
        }

        #endregion
    }
}
