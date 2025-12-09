# Commands

## 概要
UIコマンドパターンの実装。ユーザーアクションを処理するコマンドクラスを配置します。

## 責務
- UI操作のコマンド化
- アクションの実行とUndo/Redo
- コマンドキューイング
- 操作履歴の管理

## 命名規則
- クラス名: `{アクション名}Command` (例: `AttackCommand`, `MoveCommand`)
- Namespace: `CavalryFight.Commands.{カテゴリ名}`

## 例
```csharp
#nullable enable

namespace CavalryFight.Commands.Combat
{
    /// <summary>
    /// コマンドの基底インターフェース
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// コマンドを実行
        /// </summary>
        void Execute();

        /// <summary>
        /// コマンドを元に戻す（オプション）
        /// </summary>
        void Undo();

        /// <summary>
        /// コマンドが実行可能か
        /// </summary>
        bool CanExecute();
    }

    /// <summary>
    /// 攻撃コマンド
    /// </summary>
    public class AttackCommand : ICommand
    {
        #region Fields
        private readonly PlayerModel _player;
        private readonly Vector3 _targetPosition;
        #endregion

        #region Constructor
        public AttackCommand(PlayerModel player, Vector3 targetPosition)
        {
            _player = player;
            _targetPosition = targetPosition;
        }
        #endregion

        #region Public Methods
        public void Execute()
        {
            // 攻撃実行ロジック
        }

        public void Undo()
        {
            // Undo実装（必要に応じて）
        }

        public bool CanExecute()
        {
            // 攻撃可能か判定
            return _player.CanAttack;
        }
        #endregion
    }
}
```

## 注意事項
- コマンドパターンを使用してアクションを分離
- Undo/Redo機能が必要な場合は履歴を保持
- `CanExecute()`で実行可能性をチェック
