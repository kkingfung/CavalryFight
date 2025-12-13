#nullable enable

using CavalryFight.Core.MVVM;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CavalryFight.Examples.PlayerHealth
{
    /// <summary>
    /// プレイヤー体力のView
    /// </summary>
    /// <remarks>
    /// ViewはUI要素の表示と更新を担当します。
    /// ViewModelからのデータをUIに反映し、ユーザー操作をViewModelに伝えます。
    /// </remarks>
    [RequireComponent(typeof(Canvas))]
    public class PlayerHealthView : ViewBase<PlayerHealthViewModel>
    {
        #region Inspector Fields

        [Header("UI References")]
        [SerializeField] private Slider? healthSlider;
        [SerializeField] private TextMeshProUGUI? healthText;
        [SerializeField] private TextMeshProUGUI? statusText;

        [Header("Buttons")]
        [SerializeField] private Button? takeDamageButton;
        [SerializeField] private Button? healButton;
        [SerializeField] private Button? restoreButton;

        [Header("Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private int healAmount = 20;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // ViewModelの作成
            var model = new PlayerHealthModel(maxHealth);
            ViewModel = new PlayerHealthViewModel(model);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();

            // UI要素の検証
            ValidateUIReferences();
        }

        /// <summary>
        /// ViewModelとのバインディング設定
        /// </summary>
        protected override void BindViewModel(PlayerHealthViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // プロパティバインディング
            if (healthSlider != null)
            {
                AddBinding(viewModel.Bind(
                    nameof(viewModel.HealthRatio),
                    () => viewModel.HealthRatio,
                    value => healthSlider.value = value
                ));
            }

            if (healthText != null)
            {
                AddBinding(viewModel.Bind(
                    nameof(viewModel.CurrentHealth),
                    () => viewModel.CurrentHealth,
                    _ => UpdateHealthText(viewModel)
                ));

                AddBinding(viewModel.Bind(
                    nameof(viewModel.MaxHealth),
                    () => viewModel.MaxHealth,
                    _ => UpdateHealthText(viewModel)
                ));
            }

            if (statusText != null)
            {
                AddBinding(viewModel.Bind(
                    nameof(viewModel.StatusMessage),
                    () => viewModel.StatusMessage,
                    value => statusText.text = value
                ));
            }

            // コマンドバインディング
            if (takeDamageButton != null)
            {
                takeDamageButton.onClick.AddListener(() =>
                {
                    if (viewModel.TakeDamageCommand.CanExecute(damageAmount))
                    {
                        viewModel.TakeDamageCommand.Execute(damageAmount);
                    }
                });

                AddBinding(viewModel.Bind(
                    nameof(viewModel.IsAlive),
                    () => viewModel.IsAlive,
                    value => UpdateButtonInteractable(takeDamageButton, value)
                ));
            }

            if (healButton != null)
            {
                healButton.onClick.AddListener(() =>
                {
                    if (viewModel.HealCommand.CanExecute(healAmount))
                    {
                        viewModel.HealCommand.Execute(healAmount);
                    }
                });

                AddBinding(viewModel.Bind(
                    nameof(viewModel.CurrentHealth),
                    () => viewModel.CurrentHealth,
                    _ => UpdateHealButtonState(viewModel)
                ));
            }

            if (restoreButton != null)
            {
                restoreButton.onClick.AddListener(() =>
                {
                    if (viewModel.RestoreToFullCommand.CanExecute(null))
                    {
                        viewModel.RestoreToFullCommand.Execute(null);
                    }
                });

                AddBinding(viewModel.Bind(
                    nameof(viewModel.CurrentHealth),
                    () => viewModel.CurrentHealth,
                    _ => UpdateRestoreButtonState(viewModel)
                ));
            }

            Debug.Log("[PlayerHealthView] ViewModel bound successfully.");
        }

        /// <summary>
        /// ViewModelバインド解除処理
        /// </summary>
        protected override void OnViewModelUnbound()
        {
            base.OnViewModelUnbound();

            // ボタンのリスナーをクリア
            takeDamageButton?.onClick.RemoveAllListeners();
            healButton?.onClick.RemoveAllListeners();
            restoreButton?.onClick.RemoveAllListeners();

            Debug.Log("[PlayerHealthView] ViewModel unbound.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// UI参照の検証
        /// </summary>
        private void ValidateUIReferences()
        {
            if (healthSlider == null)
            {
                Debug.LogWarning("[PlayerHealthView] Health slider is not assigned!");
            }

            if (healthText == null)
            {
                Debug.LogWarning("[PlayerHealthView] Health text is not assigned!");
            }

            if (statusText == null)
            {
                Debug.LogWarning("[PlayerHealthView] Status text is not assigned!");
            }
        }

        /// <summary>
        /// 体力テキストを更新
        /// </summary>
        private void UpdateHealthText(PlayerHealthViewModel viewModel)
        {
            if (healthText != null)
            {
                healthText.text = $"{viewModel.CurrentHealth} / {viewModel.MaxHealth}";
            }
        }

        /// <summary>
        /// 回復ボタンの状態を更新
        /// </summary>
        private void UpdateHealButtonState(PlayerHealthViewModel viewModel)
        {
            if (healButton != null)
            {
                bool canHeal = viewModel.IsAlive && viewModel.CurrentHealth < viewModel.MaxHealth;
                UpdateButtonInteractable(healButton, canHeal);
            }
        }

        /// <summary>
        /// 完全回復ボタンの状態を更新
        /// </summary>
        private void UpdateRestoreButtonState(PlayerHealthViewModel viewModel)
        {
            if (restoreButton != null)
            {
                bool canRestore = viewModel.IsAlive && viewModel.CurrentHealth < viewModel.MaxHealth;
                UpdateButtonInteractable(restoreButton, canRestore);
            }
        }

        /// <summary>
        /// ボタンのインタラクティブ状態を更新
        /// </summary>
        private void UpdateButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        #endregion
    }
}
