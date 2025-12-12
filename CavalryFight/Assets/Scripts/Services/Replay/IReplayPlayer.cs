#nullable enable

using System;
using System.Collections.Generic;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ再生サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// 録画されたリプレイの再生を管理します。
    /// ファイルからの読込、再生制御、シーク、ハイライトジャンプ等を提供します。
    /// </remarks>
    public interface IReplayPlayer
    {
        #region Properties

        /// <summary>
        /// 現在再生中かどうかを取得します
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 再生が一時停止中かどうかを取得します
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 現在再生中のリプレイデータを取得します
        /// </summary>
        /// <remarks>
        /// 再生中でない場合はnull
        /// </remarks>
        ReplayData? CurrentPlayback { get; }

        /// <summary>
        /// 現在の再生時刻（秒）を取得します
        /// </summary>
        float PlaybackTime { get; }

        /// <summary>
        /// 再生速度を取得または設定します（1.0が通常速度）
        /// </summary>
        /// <remarks>
        /// 0.5で0.5倍速、2.0で2倍速になります。
        /// </remarks>
        float PlaybackSpeed { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// 再生が開始された時に発生します
        /// </summary>
        event EventHandler<ReplayPlaybackStartedEventArgs>? PlaybackStarted;

        /// <summary>
        /// 再生が停止された時に発生します
        /// </summary>
        event EventHandler? PlaybackStopped;

        /// <summary>
        /// 再生が一時停止された時に発生します
        /// </summary>
        event EventHandler? PlaybackPaused;

        /// <summary>
        /// 再生が再開された時に発生します
        /// </summary>
        event EventHandler? PlaybackResumed;

        /// <summary>
        /// 再生時刻が変更された時に発生します（シークを含む）
        /// </summary>
        event EventHandler<ReplayTimeChangedEventArgs>? PlaybackTimeChanged;

        #endregion

        #region Methods

        /// <summary>
        /// リプレイの再生を開始します
        /// </summary>
        /// <param name="replay">再生するリプレイデータ</param>
        /// <param name="startTime">開始時刻（秒、デフォルトは0）</param>
        void StartPlayback(ReplayData replay, float startTime = 0f);

        /// <summary>
        /// リプレイの再生を停止します
        /// </summary>
        void StopPlayback();

        /// <summary>
        /// リプレイの再生を一時停止します
        /// </summary>
        void PausePlayback();

        /// <summary>
        /// リプレイの再生を再開します
        /// </summary>
        void ResumePlayback();

        /// <summary>
        /// 指定した時刻にシークします
        /// </summary>
        /// <param name="time">シーク先の時刻（秒）</param>
        void SeekTo(float time);

        /// <summary>
        /// ハイライトの開始位置にジャンプします
        /// </summary>
        /// <param name="highlight">ジャンプ先のハイライト</param>
        void JumpToHighlight(ReplayHighlight highlight);

        /// <summary>
        /// 現在の再生フレームを取得します
        /// </summary>
        /// <returns>現在のフレーム（再生中でない場合はnull）</returns>
        ReplayFrame? GetCurrentFrame();

        /// <summary>
        /// 再生の更新処理（MonoBehaviourのUpdateから呼ぶ必要があります）
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        void UpdatePlayback(float deltaTime);

        /// <summary>
        /// ファイルからリプレイを読み込みます
        /// </summary>
        /// <param name="fileName">ファイル名（拡張子なし）</param>
        /// <returns>読み込まれたリプレイデータ（失敗時はnull）</returns>
        ReplayData? LoadReplay(string fileName);

        /// <summary>
        /// 保存されているリプレイのリストを取得します
        /// </summary>
        /// <returns>リプレイファイル名のリスト（拡張子なし）</returns>
        List<string> GetSavedReplays();

        /// <summary>
        /// リプレイファイルを削除します
        /// </summary>
        /// <param name="fileName">ファイル名（拡張子なし）</param>
        /// <returns>削除に成功したかどうか</returns>
        bool DeleteReplay(string fileName);

        #endregion
    }
}
