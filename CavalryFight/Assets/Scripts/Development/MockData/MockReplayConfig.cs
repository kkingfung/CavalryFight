#nullable enable

using System;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Replay;
using UnityEngine;

namespace CavalryFight.Development.MockData
{
    /// <summary>
    /// モックリプレイ設定
    /// </summary>
    /// <remarks>
    /// リプレイ画面のテスト用モックデータを提供します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockReplay", menuName = "CavalryFight/Mock/Replay Config")]
    public class MockReplayConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Replay Info")]
        /// <summary>リプレイの一意識別子</summary>
        [SerializeField] private string _replayId = "mock-replay-001";

        /// <summary>リプレイの表示名</summary>
        [SerializeField] private string _replayName = "Test Replay";

        /// <summary>リプレイの持続時間（秒）</summary>
        [SerializeField] private float _duration = 300f;

        [Header("Match Data")]
        /// <summary>関連するマッチ結果設定</summary>
        [SerializeField] private MockMatchResultConfig? _matchResult;

        #endregion

        #region Properties

        /// <summary>
        /// リプレイID
        /// </summary>
        public string ReplayId => _replayId;

        /// <summary>
        /// リプレイ名
        /// </summary>
        public string ReplayName => _replayName;

        /// <summary>
        /// 持続時間（秒）
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// マッチ結果設定
        /// </summary>
        public MockMatchResultConfig? MatchResult => _matchResult;

        #endregion

        #region Methods

        /// <summary>
        /// ReplayDataを作成します
        /// </summary>
        /// <returns>生成されたリプレイデータ</returns>
        public ReplayData CreateReplayData()
        {
            var replay = new ReplayData
            {
                ReplayId = _replayId,
                RecordedAt = DateTime.UtcNow.ToString("o"),
                MatchDuration = _duration,
                MapName = ParseMapName(_matchResult?.MapName),
                GameMode = _matchResult?.GameMode ?? "Arena",
                PlayerName = "You"
            };

            if (_matchResult != null)
            {
                replay.FinalPlayerScore = _matchResult.PlayerScore;
                replay.FinalEnemyScore = _matchResult.EnemyScore;
            }

            // モックフレームを追加（再生テスト用）
            int frameCount = Mathf.CeilToInt(_duration);
            for (int i = 0; i <= frameCount; i++)
            {
                replay.AddFrame(new ReplayFrame
                {
                    Timestamp = i
                });
            }

            // モックイベントを追加
            replay.AddEvent(new ReplayEvent
            {
                Timestamp = _duration * 0.1f,
                EventType = ReplayEventType.Score,
                SubjectEntityId = "player-local",
                Description = "Headshot! +100"
            });

            replay.AddEvent(new ReplayEvent
            {
                Timestamp = _duration * 0.25f,
                EventType = ReplayEventType.Death,
                SubjectEntityId = "enemy-1",
                TargetEntityId = "player-local",
                Description = "First Blood!"
            });

            replay.AddEvent(new ReplayEvent
            {
                Timestamp = _duration * 0.5f,
                EventType = ReplayEventType.Score,
                SubjectEntityId = "player-local",
                Description = "Heart Shot! +200"
            });

            // ハイライトを生成
            replay.GenerateHighlightsFromScoreEvents();

            return replay;
        }

        /// <summary>
        /// 文字列からMapNameを解析します
        /// </summary>
        private static MapName ParseMapName(string? mapNameString)
        {
            if (string.IsNullOrEmpty(mapNameString))
            {
                return MapName.Arena;
            }

            if (Enum.TryParse<MapName>(mapNameString, true, out var result))
            {
                return result;
            }

            return MapName.Arena;
        }

        #endregion
    }
}
