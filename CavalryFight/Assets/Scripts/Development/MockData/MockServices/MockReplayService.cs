#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Services.Lobby;
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

            // MockHistoryConfigからデータを読み込み
            if (_config?.HistorySceneMockData != null)
            {
                var historyConfig = _config.HistorySceneMockData;
                foreach (var entry in historyConfig.MatchHistory)
                {
                    if (entry.HasReplay)
                    {
                        var metadata = new ReplayMetadata
                    {
                        ReplayId = entry.ReplayId,
                        RecordedAt = ParseMatchDate(entry.MatchDate),
                        MapName = entry.MapName,
                        GameMode = entry.GameMode,
                        PlayerName = "You",
                        MatchDuration = entry.MatchDuration,
                        FinalPlayerScore = entry.PlayerScore,
                        FinalEnemyScore = entry.EnemyScore,
                        PlayerCount = entry.TotalPlayers,
                        Kills = entry.Kills,
                        Deaths = entry.Deaths,
                        Accuracy = entry.Accuracy,
                        Rank = entry.Rank
                    };

                    // 他プレイヤーの統計情報を追加
                    if (entry.OtherPlayers != null && entry.OtherPlayers.Count > 0)
                    {
                        foreach (var otherPlayer in entry.OtherPlayers)
                        {
                            metadata.OtherPlayers.Add(new OtherPlayerStats
                            {
                                PlayerName = otherPlayer.PlayerName,
                                Kills = otherPlayer.Kills,
                                Deaths = otherPlayer.Deaths,
                                Score = otherPlayer.Score,
                                Rank = otherPlayer.Rank
                            });
                        }
                    }
                    else
                    {
                        // デフォルトの他プレイヤーを生成
                        metadata.OtherPlayers = GenerateMockOtherPlayers(entry.TotalPlayers - 1, entry.Rank);
                    }

                    list.Add(metadata);
                    }
                }

                if (list.Count > 0)
                {
                    Debug.Log($"[MockReplayService] Loaded {list.Count} replays from HistoryConfig");
                    return list;
                }
            }

            // デフォルトのモックリプレイを作成（勝利、敗北、引き分けを含む）
            var defaultEntries = new[]
            {
                // 勝利 - 2人対戦
                (playerScore: 1500, enemyScore: 1200, playerCount: 2, rank: 1, kills: 10, deaths: 5, map: "Arena", mode: "ScoreMatch"),
                // 敗北 - 2人対戦
                (playerScore: 900, enemyScore: 1500, playerCount: 2, rank: 2, kills: 4, deaths: 10, map: "Forest", mode: "ScoreMatch"),
                // 引き分け - 2人対戦
                (playerScore: 1200, enemyScore: 1200, playerCount: 2, rank: 1, kills: 8, deaths: 8, map: "PlayGround", mode: "ScoreMatch"),
                // 勝利 - 4人対戦
                (playerScore: 2000, enemyScore: 1500, playerCount: 4, rank: 1, kills: 15, deaths: 5, map: "Nature", mode: "Arena"),
                // 敗北 - 4人対戦
                (playerScore: 800, enemyScore: 1800, playerCount: 4, rank: 3, kills: 6, deaths: 12, map: "Arena", mode: "Arena"),
                // 勝利 - 8人対戦
                (playerScore: 3000, enemyScore: 2500, playerCount: 8, rank: 1, kills: 20, deaths: 4, map: "PlayGround", mode: "Team Battle"),
                // 敗北 - 8人対戦
                (playerScore: 1200, enemyScore: 2800, playerCount: 8, rank: 5, kills: 8, deaths: 15, map: "Forest", mode: "Team Battle"),
                // 引き分け - 8人対戦
                (playerScore: 2000, enemyScore: 2000, playerCount: 8, rank: 1, kills: 14, deaths: 14, map: "Nature", mode: "Team Battle"),
                // 勝利 - ハンティング 4人
                (playerScore: 1800, enemyScore: 1400, playerCount: 4, rank: 1, kills: 12, deaths: 3, map: "Forest", mode: "Hunting"),
                // 敗北 - ハンティング 8人
                (playerScore: 900, enemyScore: 2200, playerCount: 8, rank: 6, kills: 5, deaths: 11, map: "Nature", mode: "Hunting"),
            };

            for (int i = 0; i < defaultEntries.Length; i++)
            {
                var entry = defaultEntries[i];
                var metadata = new ReplayMetadata
                {
                    ReplayId = $"mock-replay-{i:D3}",
                    RecordedAt = DateTime.Now.AddDays(-i).AddHours(-i * 2),
                    MapName = entry.map,
                    GameMode = entry.mode,
                    PlayerName = "You",
                    MatchDuration = 180f + (i * 30f),
                    FinalPlayerScore = entry.playerScore,
                    FinalEnemyScore = entry.enemyScore,
                    PlayerCount = entry.playerCount,
                    Kills = entry.kills,
                    Deaths = entry.deaths,
                    Accuracy = 55f + (i * 2f),
                    Rank = entry.rank,
                    OtherPlayers = GenerateMockOtherPlayers(entry.playerCount - 1, entry.rank)
                };

                list.Add(metadata);
            }

            return list;
        }

        /// <summary>
        /// マッチ日時文字列をパースします
        /// </summary>
        /// <param name="dateText">日時文字列（例：2024-01-15 14:30）</param>
        /// <returns>パースされたDateTime</returns>
        private static DateTime ParseMatchDate(string dateText)
        {
            if (DateTime.TryParse(dateText, out var result))
            {
                return result;
            }
            return DateTime.Now;
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
                MapName = MapName.Arena,
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
            string[] maps = { "Arena", "Forest", "Nature", "PlayGround", "TrainingRoom" };
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
        /// モックの他プレイヤー統計情報を生成します
        /// </summary>
        /// <param name="count">生成するプレイヤー数</param>
        /// <param name="playerRank">自分の順位（他プレイヤーの順位計算に使用）</param>
        /// <returns>他プレイヤーの統計情報リスト</returns>
        private List<OtherPlayerStats> GenerateMockOtherPlayers(int count, int playerRank)
        {
            var players = new List<OtherPlayerStats>();
            string[] names = { "Knight_Alex", "Archer_Yuki", "Warrior_Max", "Hunter_Rin", "Rider_John", "Lancer_Sora", "Cavalry_Dan" };

            int currentRank = 1;
            for (int i = 0; i < count && i < names.Length; i++)
            {
                // 自分の順位をスキップ
                if (currentRank == playerRank)
                {
                    currentRank++;
                }

                players.Add(new OtherPlayerStats
                {
                    PlayerName = names[i],
                    Kills = 5 + (count - i) * 2,
                    Deaths = 3 + i,
                    Score = 1500 - (currentRank - 1) * 200,
                    Rank = currentRank
                });

                currentRank++;
            }

            return players;
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
