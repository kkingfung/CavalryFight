#nullable enable

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using CavalryFight.Services.Replay;
using UnityEngine;

namespace CavalryFight.Development.MockData.MockServices
{
    /// <summary>
    /// モックのリプレイサービス
    /// </summary>
    /// <remarks>
    /// 開発時に個別シーンをテストする際に使用します。
    /// モック設定に基づいてリプレイデータを提供します。
    /// </remarks>
    public class MockReplayService : IReplayService
    {
        #region Fields

        /// <summary>モック設定への参照</summary>
        private readonly MockSceneConfig? _config;

        /// <summary>現在選択中のリプレイデータ</summary>
        private ReplayData? _currentReplay;

        /// <summary>モックリプレイメタデータのリスト</summary>
        private readonly List<ReplayMetadata> _mockReplayList;

        #endregion

        #region Constructor

        /// <summary>
        /// MockReplayServiceの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="config">モック設定（nullの場合はデフォルト値を使用）</param>
        public MockReplayService(MockSceneConfig? config)
        {
            _config = config;
            _mockReplayList = CreateMockReplayList();
        }

        #endregion

        #region Events

        /// <summary>
        /// リプレイリストが更新された時に発生します
        /// </summary>
        public event Action? ReplayListUpdated;

        /// <summary>
        /// リプレイが選択された時に発生します
        /// </summary>
        public event Action<ReplayData>? ReplaySelected;

        #endregion

        #region Properties

        /// <summary>
        /// 現在選択されているリプレイデータ
        /// </summary>
        public ReplayData? CurrentReplay => _currentReplay;

        #endregion

        #region Replay List Management

        /// <summary>
        /// すべてのリプレイメタデータのリストを取得します
        /// </summary>
        /// <returns>リプレイメタデータのリスト</returns>
        public List<ReplayMetadata> GetAllReplays()
        {
            return new List<ReplayMetadata>(_mockReplayList);
        }

        /// <summary>
        /// リプレイメタデータを日付順にソートして取得します
        /// </summary>
        /// <param name="descending">降順の場合true（デフォルト）</param>
        /// <returns>ソートされたリプレイメタデータのリスト</returns>
        public List<ReplayMetadata> GetReplaysSortedByDate(bool descending = true)
        {
            var sorted = new List<ReplayMetadata>(_mockReplayList);
            if (descending)
            {
                sorted.Sort((a, b) => b.RecordedAt.CompareTo(a.RecordedAt));
            }
            else
            {
                sorted.Sort((a, b) => a.RecordedAt.CompareTo(b.RecordedAt));
            }
            return sorted;
        }

        /// <summary>
        /// リプレイリストをリフレッシュします（ファイルシステムから再読み込み）
        /// </summary>
        public void RefreshReplayList()
        {
            Debug.Log("[MockReplayService] RefreshReplayList called (no-op in mock)");
            ReplayListUpdated?.Invoke();
        }

        #endregion

        #region Replay Operations

        /// <summary>
        /// リプレイIDからリプレイデータを読み込みます
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>読み込まれたリプレイデータ、失敗した場合はnull</returns>
        public ReplayData? LoadReplay(string replayId)
        {
            Debug.Log($"[MockReplayService] LoadReplay: {replayId}");

            // モック設定からリプレイデータを取得
            if (_config?.ReplaySceneMockData != null)
            {
                return _config.ReplaySceneMockData.CreateReplayData();
            }

            // デフォルトのモックリプレイデータを作成
            return CreateDefaultMockReplayData(replayId);
        }

        /// <summary>
        /// リプレイを選択して CurrentReplay に設定します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>選択に成功した場合true</returns>
        public bool SelectReplay(string replayId)
        {
            Debug.Log($"[MockReplayService] SelectReplay: {replayId}");

            _currentReplay = LoadReplay(replayId);
            if (_currentReplay != null)
            {
                ReplaySelected?.Invoke(_currentReplay);
                return true;
            }
            return false;
        }

        /// <summary>
        /// リプレイを保存します
        /// </summary>
        /// <param name="replayData">保存するリプレイデータ</param>
        /// <returns>保存に成功した場合true</returns>
        public bool SaveReplay(ReplayData replayData)
        {
            Debug.Log($"[MockReplayService] SaveReplay: {replayData.ReplayId} (no-op in mock)");
            return true;
        }

        /// <summary>
        /// リプレイを削除します
        /// </summary>
        /// <param name="replayId">削除するリプレイID</param>
        /// <returns>削除に成功した場合true</returns>
        public bool DeleteReplay(string replayId)
        {
            Debug.Log($"[MockReplayService] DeleteReplay: {replayId} (no-op in mock)");
            return true;
        }

        #endregion

        #region Utility

        /// <summary>
        /// リプレイの総数を取得します
        /// </summary>
        /// <returns>リプレイの総数</returns>
        public int GetReplayCount()
        {
            return _mockReplayList.Count;
        }

        /// <summary>
        /// リプレイが存在するかどうかを確認します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>存在する場合true</returns>
        public bool ReplayExists(string replayId)
        {
            return _mockReplayList.Exists(r => r.ReplayId == replayId);
        }

        #endregion

        #region Mock Data Creation

        /// <summary>
        /// モックのリプレイリストを作成します
        /// </summary>
        /// <returns>モックリプレイメタデータのリスト</returns>
        private List<ReplayMetadata> CreateMockReplayList()
        {
            var list = new List<ReplayMetadata>();

            // 複数のモックリプレイを作成
            for (int i = 0; i < 10; i++)
            {
                list.Add(new ReplayMetadata
                {
                    ReplayId = $"mock-replay-{i:D3}",
                    RecordedAt = DateTime.Now.AddDays(-i).AddHours(-i * 2),
                    MapName = GetMockMapName(i),
                    GameMode = GetMockGameMode(i),
                    PlayerName = "You",
                    MatchDuration = 180f + (i * 30f),
                    FinalPlayerScore = 1000 + (i % 3 == 0 ? 500 : -200),
                    FinalEnemyScore = 1000 - (i % 3 == 0 ? 200 : 300)
                });
            }

            return list;
        }

        /// <summary>
        /// デフォルトのモックリプレイデータを作成します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>生成されたリプレイデータ</returns>
        private ReplayData CreateDefaultMockReplayData(string replayId)
        {
            var replay = new ReplayData
            {
                ReplayId = replayId,
                RecordedAt = DateTime.UtcNow.ToString("o"),
                MatchDuration = 300f,
                FinalPlayerScore = 1500,
                FinalEnemyScore = 1200,
                MapName = "Training Grounds",
                GameMode = "Arena",
                PlayerName = "You"
            };

            // モックフレームを追加（再生テスト用）
            for (int i = 0; i <= 300; i++)
            {
                replay.AddFrame(new ReplayFrame
                {
                    Timestamp = i,
                    // その他のフレームデータは初期値のまま
                });
            }

            // モックイベントを追加
            replay.AddEvent(new ReplayEvent
            {
                Timestamp = 30f,
                EventType = ReplayEventType.Score,
                SubjectEntityId = "player-local",
                Description = "Headshot! +100"
            });

            replay.AddEvent(new ReplayEvent
            {
                Timestamp = 75f,
                EventType = ReplayEventType.Death,
                SubjectEntityId = "enemy-1",
                TargetEntityId = "player-local",
                Description = "Double Kill!"
            });

            replay.AddEvent(new ReplayEvent
            {
                Timestamp = 150f,
                EventType = ReplayEventType.Score,
                SubjectEntityId = "player-local",
                Description = "Heart Shot! +200"
            });

            // ハイライトを生成
            replay.GenerateHighlightsFromScoreEvents();

            return replay;
        }

        /// <summary>
        /// インデックスに基づいてモックのマップ名を取得します
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>マップ名</returns>
        private string GetMockMapName(int index)
        {
            string[] maps = { "Training Grounds", "Castle Arena", "Forest Valley", "Desert Outpost", "Mountain Pass" };
            return maps[index % maps.Length];
        }

        /// <summary>
        /// インデックスに基づいてモックのゲームモードを取得します
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>ゲームモード名</returns>
        private string GetMockGameMode(int index)
        {
            string[] modes = { "Arena", "Team Battle", "Hunting", "Training" };
            return modes[index % modes.Length];
        }

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[MockReplayService] Initialize");
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[MockReplayService] Dispose");
        }

        #endregion
    }
}

#endif
