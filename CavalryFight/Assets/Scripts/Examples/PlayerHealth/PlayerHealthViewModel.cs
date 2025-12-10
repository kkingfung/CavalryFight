#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using UnityEngine;

namespace CavalryFight.Examples.PlayerHealth
{
    /// <summary>
    /// プレイヤー体力のViewModel
    /// </summary>
    /// <remarks>
    /// ViewModelはModelとViewの間の橋渡しをします。
    /// UIに表示するためのプロパティと、ユーザー操作のためのコマンドを提供します。
    /// </remarks>
    public class PlayerHealthViewModel : ViewModelBase
    {
        #region Fields

        private readonly PlayerHealthModel _model;
        private string _statusMessage;

        #endregion

        #region Properties

        /// <summary>
        /// 現在の体力を取得します。
        /// </summary>
        public int CurrentHealth => _model.CurrentHealth;

        /// <summary>
        /// 最大体力を取得します。
        /// </summary>
        public int MaxHealth => _model.MaxHealth;

        /// <summary>
        /// 体力の割合を取得します（0.0～1.0）
        /// </summary>
        public float HealthRatio => _model.HealthRatio;

        /// <summary>
        /// プレイヤーが生存しているかどうかを取得します。
        /// </summary>
        public bool IsAlive => _model.IsAlive;

        /// <summary>
        /// ステータスメッセージを取得します。
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// ダメージを受けるコマンド
        /// </summary>
        public ICommand TakeDamageCommand { get; }

        /// <summary>
        /// 体力を回復するコマンド
        /// </summary>
        public ICommand HealCommand { get; }

        /// <summary>
        /// 体力を完全回復するコマンド
        /// </summary>
        public ICommand RestoreToFullCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// PlayerHealthViewModelの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="model">体力データモデル</param>
        public PlayerHealthViewModel(PlayerHealthModel model)
        {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _statusMessage = "Ready";

            // コマンドの初期化
            TakeDamageCommand = new RelayCommand<int?>(
                execute: damage => OnTakeDamage(damage ?? 10),
                canExecute: _ => IsAlive
            );

            HealCommand = new RelayCommand<int?>(
                execute: amount => OnHeal(amount ?? 20),
                canExecute: _ => IsAlive && CurrentHealth < MaxHealth
            );

            RestoreToFullCommand = new RelayCommand(
                execute: OnRestoreToFull,
                canExecute: () => IsAlive && CurrentHealth < MaxHealth
            );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ダメージを受ける処理
        /// </summary>
        private void OnTakeDamage(int damage)
        {
            int actualDamage = _model.TakeDamage(damage);

            if (actualDamage > 0)
            {
                StatusMessage = $"Took {actualDamage} damage!";
                Debug.Log($"[PlayerHealth] Took {actualDamage} damage. Current: {CurrentHealth}/{MaxHealth}");
            }

            if (!IsAlive)
            {
                StatusMessage = "Player defeated!";
                Debug.Log("[PlayerHealth] Player defeated!");
            }

            // すべての関連プロパティの変更を通知
            NotifyHealthChanged();
        }

        /// <summary>
        /// 体力を回復する処理
        /// </summary>
        private void OnHeal(int amount)
        {
            int actualHeal = _model.Heal(amount);

            if (actualHeal > 0)
            {
                StatusMessage = $"Healed {actualHeal} HP!";
                Debug.Log($"[PlayerHealth] Healed {actualHeal} HP. Current: {CurrentHealth}/{MaxHealth}");
            }

            NotifyHealthChanged();
        }

        /// <summary>
        /// 体力を完全回復する処理
        /// </summary>
        private void OnRestoreToFull()
        {
            _model.RestoreToFull();
            StatusMessage = "Fully restored!";
            Debug.Log($"[PlayerHealth] Fully restored! Current: {CurrentHealth}/{MaxHealth}");

            NotifyHealthChanged();
        }

        /// <summary>
        /// 体力関連のプロパティ変更を通知します。
        /// </summary>
        private void NotifyHealthChanged()
        {
            OnPropertiesChanged(
                nameof(CurrentHealth),
                nameof(HealthRatio),
                nameof(IsAlive)
            );

            // コマンドの実行可能状態を更新
            (TakeDamageCommand as RelayCommand<int?>)?.RaiseCanExecuteChanged();
            (HealCommand as RelayCommand<int?>)?.RaiseCanExecuteChanged();
            (RestoreToFullCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// ViewModelの破棄処理
        /// </summary>
        protected override void OnDispose()
        {
            Debug.Log("[PlayerHealth] ViewModel disposed.");
            base.OnDispose();
        }

        #endregion
    }
}
