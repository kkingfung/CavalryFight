#nullable enable

using System;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using CavalryFight.Services.Customization;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// カスタマイズ画面のViewModel
    /// </summary>
    /// <remarks>
    /// ICustomizationServiceを使用してキャラクターと馬のカスタマイズを管理します。
    /// 左パネルにカテゴリタブ、右パネルに3Dプレビューを配置します。
    /// </remarks>
    public class CustomizationViewModel : ViewModelBase
    {
        #region Fields

        private readonly ICustomizationService _customizationService;
        private CustomizationCategory _currentCategory;
        private CharacterCustomization _workingCharacter;
        private MountCustomization _workingMount;

        #endregion

        #region Properties

        /// <summary>
        /// 現在選択されているカテゴリ
        /// </summary>
        public CustomizationCategory CurrentCategory
        {
            get => _currentCategory;
            set
            {
                if (SetProperty(ref _currentCategory, value))
                {
                    OnPropertyChanged(nameof(IsCharacterCategory));
                    OnPropertyChanged(nameof(IsMountCategory));
                    CategoryChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>キャラクターカテゴリが選択されているか</summary>
        public bool IsCharacterCategory => CurrentCategory == CustomizationCategory.Character;

        /// <summary>馬カテゴリが選択されているか</summary>
        public bool IsMountCategory => CurrentCategory == CustomizationCategory.Mount;

        /// <summary>作業中のキャラクターカスタマイズ</summary>
        public CharacterCustomization WorkingCharacter => _workingCharacter;

        /// <summary>作業中の馬カスタマイズ</summary>
        public MountCustomization WorkingMount => _workingMount;

        #endregion

        #region Commands

        /// <summary>カテゴリを選択するコマンド</summary>
        public ICommand SelectCategoryCommand { get; }

        /// <summary>変更を適用して保存するコマンド（内部使用）</summary>
        public ICommand ApplyCommand { get; }

        /// <summary>変更を破棄して保存済みデータに戻すコマンド</summary>
        public ICommand ResetCommand { get; }

        /// <summary>メインメニューに戻るコマンド（自動保存）</summary>
        public ICommand BackToMenuCommand { get; }

        #endregion

        #region Events

        /// <summary>カテゴリが変更された時のイベント</summary>
        public event EventHandler<CustomizationCategory>? CategoryChanged;

        /// <summary>プレビューを更新する必要がある時のイベント</summary>
        public event EventHandler? PreviewUpdated;

        /// <summary>メインメニューに戻る要求イベント</summary>
        public event EventHandler? BackToMenuRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// CustomizationViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="customizationService">カスタマイズサービス</param>
        public CustomizationViewModel(ICustomizationService customizationService)
        {
            _customizationService = customizationService ?? throw new ArgumentNullException(nameof(customizationService));

            // 現在のカスタマイズをコピーして作業用データとする
            _workingCharacter = _customizationService.CurrentCharacter.Clone();
            _workingMount = _customizationService.CurrentMount.Clone();

            // サービスのイベントを購読
            _customizationService.CharacterCustomizationChanged += OnServiceCharacterChanged;
            _customizationService.MountCustomizationChanged += OnServiceMountChanged;

            // コマンドを初期化
            SelectCategoryCommand = new RelayCommand<CustomizationCategory>(ExecuteSelectCategory);
            ApplyCommand = new RelayCommand(ExecuteApply);
            ResetCommand = new RelayCommand(ExecuteReset);
            BackToMenuCommand = new RelayCommand(ExecuteBackToMenu);

            // デフォルトカテゴリ
            CurrentCategory = CustomizationCategory.Character;

            Debug.Log("[CustomizationViewModel] ViewModel initialized.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// キャラクターのプロパティ変更を通知してプレビューを更新します
        /// </summary>
        public void NotifyCharacterChanged()
        {
            OnPropertyChanged(nameof(WorkingCharacter));
            PreviewUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 馬のプロパティ変更を通知してプレビューを更新します
        /// </summary>
        public void NotifyMountChanged()
        {
            OnPropertyChanged(nameof(WorkingMount));
            PreviewUpdated?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// カテゴリを選択します
        /// </summary>
        private void ExecuteSelectCategory(CustomizationCategory category)
        {
            CurrentCategory = category;
            Debug.Log($"[CustomizationViewModel] Category changed to: {category}");
        }

        /// <summary>
        /// 変更を適用して保存します
        /// </summary>
        private void ExecuteApply()
        {
            // サービスに現在の作業データを設定
            _customizationService.SetCustomization(_workingCharacter, _workingMount);

            Debug.Log("[CustomizationViewModel] Customization applied.");
        }

        /// <summary>
        /// 変更を破棄して保存済みデータに戻します
        /// </summary>
        private void ExecuteReset()
        {
            // サービスから現在のデータを再取得
            _workingCharacter = _customizationService.CurrentCharacter.Clone();
            _workingMount = _customizationService.CurrentMount.Clone();

            // プロパティ変更を通知
            OnPropertyChanged(nameof(WorkingCharacter));
            OnPropertyChanged(nameof(WorkingMount));
            PreviewUpdated?.Invoke(this, EventArgs.Empty);

            Debug.Log("[CustomizationViewModel] Reset to saved data.");
        }

        /// <summary>
        /// メインメニューに戻ります（変更を自動保存）
        /// </summary>
        private void ExecuteBackToMenu()
        {
            // 変更を自動的に保存
            _customizationService.SetCustomization(_workingCharacter, _workingMount);

            Debug.Log("[CustomizationViewModel] Auto-saved customization and returning to menu.");
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// サービスのキャラクターカスタマイズ変更を処理します
        /// </summary>
        private void OnServiceCharacterChanged(CharacterCustomization customization)
        {
            // 外部から変更された場合、作業データを更新
            _workingCharacter = customization.Clone();
            OnPropertyChanged(nameof(WorkingCharacter));
            PreviewUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// サービスの馬カスタマイズ変更を処理します
        /// </summary>
        private void OnServiceMountChanged(MountCustomization customization)
        {
            // 外部から変更された場合、作業データを更新
            _workingMount = customization.Clone();
            OnPropertyChanged(nameof(WorkingMount));
            PreviewUpdated?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            // イベント購読解除
            _customizationService.CharacterCustomizationChanged -= OnServiceCharacterChanged;
            _customizationService.MountCustomizationChanged -= OnServiceMountChanged;

            base.OnDispose();
            Debug.Log("[CustomizationViewModel] ViewModel disposed.");
        }

        #endregion
    }

    /// <summary>
    /// カスタマイズ画面のカテゴリ
    /// </summary>
    public enum CustomizationCategory
    {
        Character,
        Mount
    }
}
