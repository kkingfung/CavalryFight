#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// リプレイの一覧取得、読み込み、保存、削除を管理します。
    /// HistoryシーンとReplayシーンで使用されます。
    /// </remarks>
    public interface IReplayService : IService
    {
        #region Events

        /// <summary>
        /// リプレイリストが更新された時に発生します
        /// </summary>
        event Action? ReplayListUpdated;

        /// <summary>
        /// リプレイが選択された時に発生します
        /// </summary>
        event Action<ReplayData>? ReplaySelected;

        #endregion

        #region Properties

        /// <summary>
        /// 現在選択されているリプレイデータ
        /// </summary>
        ReplayData? CurrentReplay { get; }

        #endregion

        #region Replay List Management

        /// <summary>
        /// すべてのリプレイメタデータのリストを取得します
        /// </summary>
        /// <returns>リプレイメタデータのリスト</returns>
        List<ReplayMetadata> GetAllReplays();

        /// <summary>
        /// リプレイメタデータを日付順にソートして取得します
        /// </summary>
        /// <param name="descending">降順の場合true（デフォルト）</param>
        /// <returns>ソートされたリプレイメタデータのリスト</returns>
        List<ReplayMetadata> GetReplaysSortedByDate(bool descending = true);

        /// <summary>
        /// リプレイリストをリフレッシュします（ファイルシステムから再読み込み）
        /// </summary>
        void RefreshReplayList();

        #endregion

        #region Replay Operations

        /// <summary>
        /// リプレイIDからリプレイデータを読み込みます
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>読み込まれたリプレイデータ、失敗した場合はnull</returns>
        ReplayData? LoadReplay(string replayId);

        /// <summary>
        /// リプレイを選択して CurrentReplay に設定します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>選択に成功した場合true</returns>
        bool SelectReplay(string replayId);

        /// <summary>
        /// リプレイを保存します
        /// </summary>
        /// <param name="replayData">保存するリプレイデータ</param>
        /// <returns>保存に成功した場合true</returns>
        bool SaveReplay(ReplayData replayData);

        /// <summary>
        /// リプレイを削除します
        /// </summary>
        /// <param name="replayId">削除するリプレイID</param>
        /// <returns>削除に成功した場合true</returns>
        bool DeleteReplay(string replayId);

        #endregion

        #region Utility

        /// <summary>
        /// リプレイの総数を取得します
        /// </summary>
        /// <returns>リプレイの総数</returns>
        int GetReplayCount();

        /// <summary>
        /// リプレイが存在するかどうかを確認します
        /// </summary>
        /// <param name="replayId">リプレイID</param>
        /// <returns>存在する場合true</returns>
        bool ReplayExists(string replayId);

        #endregion
    }
}
