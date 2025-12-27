#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ管理サービスの実装
    /// </summary>
    /// <remarks>
    /// リプレイファイルの保存・読み込み・削除を管理します。
    /// 現在はモックデータを返しますが、将来的にファイルシステムと連携します。
    /// </remarks>
    public class ReplayService : IReplayService
    {
        #region Constants

        private const string ReplayDirectory = "Replays";
        private const string ReplayFileExtension = ".replay.json";

        #endregion

        #region Fields

        private readonly List<ReplayMetadata> _replayMetadataList = new List<ReplayMetadata>();
        private ReplayData? _currentReplay;
        private readonly string _replayFolderPath;
        private bool _useMockData = true; // モックデータ使用フラグ

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

        #region Constructor

        /// <summary>
        /// ReplayServiceの新しいインスタンスを初期化します
        /// </summary>
        public ReplayService()
        {
            // リプレイ保存フォルダのパスを設定
            _replayFolderPath = Path.Combine(Application.persistentDataPath, ReplayDirectory);

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(_replayFolderPath))
            {
                Directory.CreateDirectory(_replayFolderPath);
                Debug.Log($"[ReplayService] Created replay folder: {_replayFolderPath}");
            }

            // モックデータを生成
            GenerateMockData();

            Debug.Log($"[ReplayService] Service initialized. Replay folder: {_replayFolderPath}");
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[ReplayService] Initialize called.");
            RefreshReplayList();
        }

        /// <summary>
        /// サービスを更新します（毎フレーム呼ばれる）
        /// </summary>
        public void Update()
        {
            // 必要に応じて実装
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            _replayMetadataList.Clear();
            _currentReplay = null;
            Debug.Log("[ReplayService] Service disposed.");
        }

        #endregion

        #region Replay List Management

        /// <summary>
        /// すべてのリプレイメタデータのリストを取得します
        /// </summary>
        /// <returns>リプレイメタデータのリスト</returns>
        public List<ReplayMetadata> GetAllReplays()
        {
            return new List<ReplayMetadata>(_replayMetadataList);
        }

        /// <summary>
        /// リプレイメタデータを日付順にソートして取得します
        /// </summary>
        /// <param name="descending">降順の場合true（デフォルト）</param>
        /// <returns>ソートされたリプレイメタデータのリスト</returns>
        public List<ReplayMetadata> GetReplaysSortedByDate(bool descending = true)
        {
            var sorted = descending
                ? _replayMetadataList.OrderByDescending(r => r.RecordedAt).ToList()
                : _replayMetadataList.OrderBy(r => r.RecordedAt).ToList();

            return sorted;
        }

        /// <summary>
        /// リプレイリストをリフレッシュします（ファイルシステムから再読み込み）
        /// </summary>
        public void RefreshReplayList()
        {
            _replayMetadataList.Clear();

            if (_useMockData)
            {
                // モックデータを再生成
                GenerateMockData();
            }
            else
            {
                // 実際のファイルシステムから読み込み（将来の実装）
                LoadReplaysFromFileSystem();
            }

            ReplayListUpdated?.Invoke();
            Debug.Log($"[ReplayService] Replay list refreshed. Count: {_replayMetadataList.Count}");
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
            if (_useMockData)
            {
                // モックデータを生成
                var metadata = _replayMetadataList.FirstOrDefault(r => r.ReplayId == replayId);
                if (metadata != null)
                {
                    return GenerateMockReplayData(metadata);
                }

                Debug.LogWarning($"[ReplayService] Replay not found: {replayId}");
                return null;
            }
            else
            {
                // ファイルから読み込み（将来の実装）
                string filePath = GetReplayFilePath(replayId);
                return ReplayData.LoadFromFile(filePath);
            }
        }

        /// <summary>
        /// リプレイを選択して CurrentReplay に設定します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>選択に成功した場合true</returns>
        public bool SelectReplay(string replayId)
        {
            var replayData = LoadReplay(replayId);
            if (replayData != null)
            {
                _currentReplay = replayData;
                ReplaySelected?.Invoke(replayData);
                Debug.Log($"[ReplayService] Replay selected: {replayId}");
                return true;
            }

            Debug.LogWarning($"[ReplayService] Failed to select replay: {replayId}");
            return false;
        }

        /// <summary>
        /// リプレイを保存します
        /// </summary>
        /// <param name="replayData">保存するリプレイデータ</param>
        /// <returns>保存に成功した場合true</returns>
        public bool SaveReplay(ReplayData replayData)
        {
            if (_useMockData)
            {
                // モックモードでは保存しない（ログのみ）
                Debug.Log($"[ReplayService] (Mock) Replay saved: {replayData.ReplayId}");
                return true;
            }

            string filePath = GetReplayFilePath(replayData.ReplayId);
            bool success = replayData.SaveToFile(filePath);

            if (success)
            {
                // メタデータリストを更新
                var metadata = ReplayMetadata.FromReplayData(replayData);
                _replayMetadataList.Add(metadata);
                ReplayListUpdated?.Invoke();
            }

            return success;
        }

        /// <summary>
        /// リプレイを削除します
        /// </summary>
        /// <param name="replayId">削除するリプレイID</param>
        /// <returns>削除に成功した場合true</returns>
        public bool DeleteReplay(string replayId)
        {
            // メタデータリストから削除
            int removedCount = _replayMetadataList.RemoveAll(r => r.ReplayId == replayId);

            if (removedCount > 0)
            {
                // 現在選択中のリプレイが削除された場合はクリア
                if (_currentReplay != null && _currentReplay.ReplayId == replayId)
                {
                    _currentReplay = null;
                }

                // ファイルシステムから削除（モックモードでは実行しない）
                if (!_useMockData)
                {
                    string filePath = GetReplayFilePath(replayId);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Debug.Log($"[ReplayService] Replay file deleted: {filePath}");
                    }
                }

                ReplayListUpdated?.Invoke();
                Debug.Log($"[ReplayService] Replay deleted: {replayId}");
                return true;
            }

            Debug.LogWarning($"[ReplayService] Replay not found for deletion: {replayId}");
            return false;
        }

        #endregion

        #region Utility

        /// <summary>
        /// リプレイの総数を取得します
        /// </summary>
        /// <returns>リプレイの総数</returns>
        public int GetReplayCount()
        {
            return _replayMetadataList.Count;
        }

        /// <summary>
        /// リプレイが存在するかどうかを確認します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>存在する場合true</returns>
        public bool ReplayExists(string replayId)
        {
            return _replayMetadataList.Any(r => r.ReplayId == replayId);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// リプレイのファイルパスを取得します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>ファイルパス</returns>
        private string GetReplayFilePath(string replayId)
        {
            return Path.Combine(_replayFolderPath, replayId + ReplayFileExtension);
        }

        /// <summary>
        /// ファイルシステムからリプレイを読み込みます（将来の実装）
        /// </summary>
        private void LoadReplaysFromFileSystem()
        {
            if (!Directory.Exists(_replayFolderPath))
            {
                return;
            }

            var files = Directory.GetFiles(_replayFolderPath, "*" + ReplayFileExtension);
            foreach (var filePath in files)
            {
                var replayData = ReplayData.LoadFromFile(filePath);
                if (replayData != null)
                {
                    var metadata = ReplayMetadata.FromReplayData(replayData);
                    _replayMetadataList.Add(metadata);
                }
            }

            Debug.Log($"[ReplayService] Loaded {_replayMetadataList.Count} replays from file system.");
        }

        /// <summary>
        /// モックデータを生成します（開発用）
        /// </summary>
        private void GenerateMockData()
        {
            _replayMetadataList.Clear();

            // サンプルリプレイを5つ生成
            string[] maps = { "Arena", "Forest", "Canyon", "Desert", "Tundra" };
            string[] modes = { "Deathmatch", "Team Battle", "Training", "Skirmish" };

            var random = new System.Random();
            var baseDate = DateTime.Now.AddDays(-30);

            for (int i = 0; i < 8; i++)
            {
                var metadata = new ReplayMetadata
                {
                    ReplayId = Guid.NewGuid().ToString(),
                    RecordedAt = baseDate.AddDays(i * 3).AddHours(random.Next(0, 24)),
                    MapName = maps[random.Next(maps.Length)],
                    GameMode = modes[random.Next(modes.Length)],
                    PlayerName = "Player",
                    MatchDuration = 120f + random.Next(0, 300),
                    FinalPlayerScore = random.Next(0, 20),
                    FinalEnemyScore = random.Next(0, 20)
                };

                _replayMetadataList.Add(metadata);
            }

            Debug.Log($"[ReplayService] Generated {_replayMetadataList.Count} mock replays.");
        }

        /// <summary>
        /// メタデータからモックリプレイデータを生成します
        /// </summary>
        /// <param name="metadata">メタデータ</param>
        /// <returns>生成されたリプレイデータ</returns>
        private ReplayData GenerateMockReplayData(ReplayMetadata metadata)
        {
            var replayData = new ReplayData
            {
                ReplayId = metadata.ReplayId,
                RecordedAt = metadata.RecordedAt.ToString("o"),
                MapName = metadata.MapName,
                GameMode = metadata.GameMode,
                PlayerName = metadata.PlayerName,
                MatchDuration = metadata.MatchDuration,
                FinalPlayerScore = metadata.FinalPlayerScore,
                FinalEnemyScore = metadata.FinalEnemyScore
            };

            return replayData;
        }

        #endregion
    }
}
