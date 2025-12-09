# Models

## 概要
MVVMパターンのModel層。ゲームロジック、データ構造、ビジネスルールを実装します。

## 責務
- ゲームデータの定義と管理
- ビジネスロジックの実装
- データの永続化
- データバリデーション

## 命名規則
- クラス名: `{機能名}Model` (例: `PlayerModel`, `ArrowModel`)
- Namespace: `CavalryFight.Models.{機能名}`

## 例
```csharp
#nullable enable

namespace CavalryFight.Models.Player
{
    /// <summary>
    /// プレイヤーデータモデル
    /// </summary>
    public class PlayerModel
    {
        #region Properties
        public int Health { get; set; }
        public int MaxHealth { get; private set; }
        public string PlayerName { get; set; }
        #endregion

        #region Constructor
        public PlayerModel(string playerName, int maxHealth)
        {
            PlayerName = playerName;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }
        #endregion
    }
}
```

## 注意事項
- Modelは**ViewやViewModelに依存しない**
- ピュアなC#クラスとして実装（MonoBehaviour非推奨）
- ビジネスロジックのユニットテストを記述
