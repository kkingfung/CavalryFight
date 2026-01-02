#nullable enable

using System;
using CavalryFight.Core.MVVM;
using CavalryFight.Services.SceneManagement;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// トレーニングシーンのViewModel
    /// </summary>
    /// <remarks>
    /// ゲーム状態の管理、ポーズメニューの制御、
    /// HUD用のデータ提供を行います。
    /// </remarks>
    public class TrainingViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService _sceneService;
        private bool _isPaused;
        private int _arrowsFired;
        private int _hits;
        private int _score;

        #endregion

        #region Properties

        /// <summary>
        /// ポーズ中かどうかを取得または設定します
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    OnPauseStateChanged();
                }
            }
        }

        /// <summary>
        /// 発射した矢の数を取得または設定します
        /// </summary>
        public int ArrowsFired
        {
            get => _arrowsFired;
            set => SetProperty(ref _arrowsFired, value);
        }

        /// <summary>
        /// ヒット数を取得または設定します
        /// </summary>
        public int Hits
        {
            get => _hits;
            set => SetProperty(ref _hits, value);
        }

        /// <summary>
        /// スコアを取得または設定します
        /// </summary>
        public int Score
        {
            get => _score;
            set => SetProperty(ref _score, value);
        }

        /// <summary>
        /// 命中率を取得します
        /// </summary>
        public float Accuracy
        {
            get
            {
                if (_arrowsFired == 0)
                {
                    return 0f;
                }
                return (float)_hits / _arrowsFired * 100f;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// ポーズ状態が変化した時に発生します
        /// </summary>
        public event EventHandler? PauseStateChanged;

        /// <summary>
        /// メインメニューへ戻る要求が発生した時に発生します
        /// </summary>
        public event EventHandler? BackToMenuRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sceneService">シーン管理サービス</param>
        public TrainingViewModel(ISceneManagementService sceneService)
        {
            _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ポーズ/再開を切り替えます
        /// </summary>
        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        /// <summary>
        /// ゲームを再開します
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// メインメニューに戻ります
        /// </summary>
        public void BackToMainMenu()
        {
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
            _sceneService.LoadMainMenu();
        }

        /// <summary>
        /// 矢を発射した記録を追加します
        /// </summary>
        public void RecordArrowFired()
        {
            ArrowsFired++;
            OnPropertyChanged(nameof(Accuracy));
        }

        /// <summary>
        /// ヒットの記録を追加します
        /// </summary>
        public void RecordHit()
        {
            Hits++;
            OnPropertyChanged(nameof(Accuracy));
        }

        /// <summary>
        /// スコアを追加します
        /// </summary>
        /// <param name="score">追加するスコア</param>
        public void RecordScore(int score)
        {
            Score += score;
        }

        /// <summary>
        /// 統計をリセットします
        /// </summary>
        public void ResetStatistics()
        {
            ArrowsFired = 0;
            Hits = 0;
            Score = 0;
            OnPropertyChanged(nameof(Accuracy));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ポーズ状態変化時の処理
        /// </summary>
        private void OnPauseStateChanged()
        {
            PauseStateChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
