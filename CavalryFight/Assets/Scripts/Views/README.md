# Views

## 概要
MVVMパターンのView層。UI表示とユーザー入力の受け取りを担当します。

## 責務
- MonoBehaviourコンポーネントの実装
- UIの表示と更新
- ユーザー入力の受け取り
- ViewModelとのバインディング

## 命名規則
- クラス名: `{機能名}View` (例: `PlayerView`, `CombatHUDView`)
- Namespace: `CavalryFight.Views.{機能名}`

## 例
```csharp
#nullable enable

using UnityEngine;
using UnityEngine.UI;
using CavalryFight.ViewModels.Player;

namespace CavalryFight.Views.Player
{
    /// <summary>
    /// プレイヤーUIビュー
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region SerializeField
        [SerializeField] private Slider? _healthSlider;
        [SerializeField] private Text? _playerNameText;
        #endregion

        #region Fields
        private PlayerViewModel? _viewModel;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ValidateComponents();
        }
        #endregion

        #region Public Methods
        public void BindViewModel(PlayerViewModel viewModel)
        {
            _viewModel = viewModel;
            UpdateView();
        }
        #endregion

        #region Private Methods
        private void UpdateView()
        {
            if (_viewModel == null) return;

            if (_healthSlider != null)
                _healthSlider.value = _viewModel.HealthRatio;
        }
        #endregion
    }
}
```

## 注意事項
- Viewは**MonoBehaviour**を継承
- ビジネスロジックは含めない（ViewModelに委譲）
- SerializeFieldで必要なUIコンポーネントを参照
