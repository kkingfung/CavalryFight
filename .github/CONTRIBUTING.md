# 🏇 CavalryFightへの貢献ガイド

CavalryFightへようこそ！本ガイドでは、プロジェクトへの効果的な貢献方法を説明します。
CavalryFightは、騎兵戦闘をテーマにしたFPSとアクションゲームの融合型Unity 6プロジェクトです。

## 🚀 開発者向けクイックスタート

### 前提条件
- **Unity 6** (6000.2.6f2以降)
- **Git LFS** (大容量アセット用)
- **.NET 8.0** (Unity scripting用)
- **Visual Studio 2022** または **VS Code** with Unity extensions

### セットアップ手順
1. **Fork & Clone**
   ```bash
   git clone https://github.com/yourusername/CavalryFight.git
   cd CavalryFight
   git lfs pull  # 重要: 大容量アセットをプル
   ```

2. **Unityで開く**
   - Unity Hubを起動
   - プロジェクトを開く（Unityが自動設定）
   - 初回コンパイルを待つ

3. **セットアップ確認**
   - Unity Editorを開く
   - コンソールにエラーがないか確認
   - サンプルシーンが正常に読み込まれるか確認

## 🎮 ゲーム概要

### コアコンセプト
CavalryFightは、馬や動物に騎乗して戦う戦闘ゲームです。弓矢による遠距離戦闘を中心に、戦術的なゲームプレイを提供します。

### 主要機能（開発予定）
- **🏹 戦闘システム**: 弓矢による精密な遠距離戦闘
- **🐴 騎乗システム**: 馬や他の動物に騎乗
- **🎯 部位ダメージ**: 命中部位によるスコアリングシステム
- **🎨 カスタマイゼーション**: キャラクター・装備のカスタマイズ
- **📹 リプレイシステム**: マッチの録画と再生（サードパーソン視点）
- **🎓 トレーニングモード**: 初心者向け練習モード

### ゲームモード（開発予定）
- **Arena Mode**: アリーナでの制限時間付きバトル
- **Score Match**: 弓矢の命中部位でポイント獲得（ベストオブ7形式）
- **Team Fight**: チーム対戦モード
- **Deathmatch**: 個人戦デスマッチ
- **PvE Mode**: AI敵との戦闘

### 技術的特徴
- **アーキテクチャ**: MVVMパターン採用
- **マルチプレイ**: PvP/PvE対応予定
- **リプレイ**: イベントベースのリプレイシステム
- **スコアリング**: 部位別ダメージ計算システム

## 📋 コーディング規則

### 必須ドキュメント
プロジェクトの詳細なコーディング規則は、以下のドキュメントを参照してください：
**📄 [Docs/CODING_RULES.md](../Docs/CODING_RULES.md)**

### 重要な規則の概要

#### 1. MVVMパターンの採用
プロジェクト全体でMVVM（Model-View-ViewModel）パターンを使用します。

```csharp
// Model: ゲームロジックとデータ
namespace CavalryFight.Models.Combat
{
    public class ArrowData
    {
        public int Damage { get; set; }
        public Vector3 Trajectory { get; set; }
    }
}

// ViewModel: プレゼンテーションロジック
namespace CavalryFight.ViewModels.Combat
{
    public class CombatViewModel : INotifyPropertyChanged
    {
        // ViewとModelの仲介
    }
}

// View: UI表示
namespace CavalryFight.Views.Combat
{
    public class CombatView : MonoBehaviour
    {
        // MonoBehaviourコンポーネント
    }
}
```

#### 2. Nullable参照型の使用（必須）
ViewModelでは必ずNullable参照型を有効にします。

```csharp
#nullable enable

namespace CavalryFight.ViewModels.Player
{
    public class PlayerViewModel
    {
        public string? PlayerName { get; set; }
        public int Score { get; set; } = 0;
    }
}
```

#### 3. Regionsによるコード整理（必須）
すべてのスクリプトでRegionsを使用してコードを整理します。

```csharp
#region Fields
private Transform _cachedTransform;
#endregion

#region Properties
public int Health { get; private set; }
#endregion

#region Unity Lifecycle
private void Awake() { }
private void Start() { }
#endregion

#region Public Methods
public void Attack() { }
#endregion

#region Private Methods
private void CalculateDamage() { }
#endregion

#region Event Handlers
private void OnPlayerHit() { }
#endregion
```

#### 4. Namespace規則
全てのスクリプトに適切なNamespaceを設定します。

```csharp
namespace CavalryFight.{Layer}.{Feature}

// 例
namespace CavalryFight.Models.Combat
namespace CavalryFight.ViewModels.Player
namespace CavalryFight.Views.UI
namespace CavalryFight.Services.Replay
```

#### 5. XMLドキュメントコメント（必須）
すべてのpublic/protected/internalメンバーにXMLコメントを記述します。

```csharp
/// <summary>
/// 弓矢の命中部位に応じてスコアを計算します
/// </summary>
/// <param name="hitPart">命中した部位</param>
/// <returns>計算されたスコア</returns>
public int CalculateHitScore(BodyPart hitPart)
{
    // 実装
}
```

## 🎯 プロジェクト固有のガイドライン

### 戦闘システム
- **決定論的実装**: 全ての戦闘ロジックは再現可能にする（リプレイシステムのため）
- **部位ダメージ**: ヒットボックスとスコアリングの明確な分離
- **弓矢物理**: リアルな弾道計算の実装
- **ネットワーク対応**: マルチプレイを考慮した設計

### リプレイシステム
- **イベント記録**: すべてのゲームイベントをタイムスタンプ付きで記録
- **決定論的再生**: 同じ入力で同じ結果を保証
- **カメラシステム**: サードパーソン視点での自由なカメラ制御
- **パフォーマンス**: リプレイ記録がゲームプレイに影響を与えないこと

### カスタマイゼーションシステム
- **データ駆動**: ScriptableObjectsを活用
- **プリセット管理**: プレイヤー設定の保存と読み込み
- **バリデーション**: 不正なカスタマイズの防止

### スコアリングシステム
- **部位別ポイント**: 命中部位による差別化
- **マッチ管理**: ベストオブN形式のサポート
- **タイマー**: 制限時間の正確な管理
- **リーダーボード**: スコア記録とランキング

## 🛠️ 開発ワークフロー

### 1. 作業開始前
- 最新の`main`ブランチをプル
- 機能ブランチを作成: `feature/your-feature-name`
- 関連Issueを確認

### 2. 開発中
- [コーディング規則](../Docs/CODING_RULES.md)に従う
- 新機能にはテストを記述
- XMLドキュメントコメントを記述
- Regionsでコードを整理

### 3. コミット前
- ビルドがエラーなくコンパイルされるか確認
- コンソールに警告がないか確認
- 変更をエディタとビルドでテスト
- コーディング規則チェックリストを確認

### 4. プルリクエスト提出
- PRテンプレートを使用（自動入力）
- 視覚的変更にはスクリーンショット/動画を追加
- 関連Issueをリンク
- レビュアーを指定

## 🧪 テストガイドライン

### ユニットテスト
- `Assets/Tests/EditMode/`に配置
- Unity Test Runnerの規約に従う
- パブリックAPIと重要パスをテスト
- 外部依存関係はモック化

### 統合テスト
- `Assets/Tests/PlayMode/`に配置
- 完全なワークフローをテスト
- 重要システムのパフォーマンスベンチマークを含める

### 手動テストチェックリスト
- [ ] エディタ機能が正常に動作
- [ ] ビルドがコンパイルされ実行可能
- [ ] パフォーマンスが許容範囲内（Profiler使用）
- [ ] リプレイシステムが正確に再生
- [ ] コンソールにエラーや警告がない

## 🚀 パフォーマンス最適化

### 最適化前に
1. **まずプロファイル**: Unity Profilerで実際のボトルネックを特定
2. **影響を測定**: 最適化前後のメトリクスを比較
3. **優先順位付け**: 最も影響の大きい問題から対処

### 一般的な最適化
- **コンポーネントキャッシング**: `GetComponent()`の繰り返し呼び出しを避ける
- **オブジェクトプーリング**: 矢などの頻繁に生成されるオブジェクトを再利用
- **LODグループ**: 複雑な3Dモデルに実装
- **テクスチャ圧縮**: ターゲットプラットフォームに適したフォーマット使用

### 騎兵戦闘固有の最適化
- **矢の物理計算**: プーリングと最適化された弾道計算
- **騎乗アニメーション**: IKとブレンドツリーの最適化
- **リプレイ記録**: 効率的なイベントシリアライゼーション
- **マルチプレイ同期**: 必要最小限のデータ送信

## 📊 品質基準

### 自動チェック（CI/CD）
- 警告なしでコードコンパイル
- すべてのテストがパス
- コード品質分析
- セキュリティスキャン

### 手動検証
- ビジュアル/ゲームプレイテスト
- パフォーマンスプロファイリング
- クロスプラットフォームテスト（該当する場合）
- リプレイシステム精度検証

### 品質基準
- **テストカバレッジ**: 重要パスの適切なカバレッジを維持
- **パフォーマンス**: パフォーマンスの大幅な低下がないこと
- **ドキュメント**: すべてのパブリックAPIがドキュメント化
- **コード品質**: 静的解析チェックをパス

## 🎯 Issue報告

### Issue作成前
1. **既存を検索**: 同じIssueが既にないか確認
2. **データ収集**: Profilerスクリーンショット、ログ出力を含める
3. **再現テスト**: 問題が再現可能か確認

### Issueテンプレート
- **🐛 バグ報告**: 予期しない動作
- **⚡ パフォーマンス問題**: 最適化の機会
- **✨ 機能リクエスト**: 新機能
- **📚 ドキュメント**: ドキュメント改善
- **🎮 ゲームプレイ**: ゲームデザイン関連

### パフォーマンスIssue
常に以下を含める：
- Unity Profilerスクリーンショット
- 具体的なメトリクス（FPS、メモリ使用量）
- ハードウェア仕様
- 再現手順

## 🌟 ベストプラクティス要約

### 日常開発
1. **開始**: 最新版をプル、コーディング規則を確認
2. **コーディング**: 規則に従い、すべてをドキュメント化
3. **テスト**: 頻繁にテストを実行、新テストを記述
4. **コミット**: 品質チェック、ビルド確認

### コード品質
- 意味のある名前と明確な構造を使用
- メソッドは小さく集中させる
- エラーを適切に処理
- まず可読性、次にパフォーマンスを最適化（クリティカルな場合を除く）

### パフォーマンス文化
- 最適化前にプロファイル
- CI/CD品質ゲートを監視
- Update()ループでのアロケーションを避ける
- リプレイシステムへの影響を考慮

## 📚 重要なドキュメント

- **[コーディング規則](../Docs/CODING_RULES.md)** - 必読の詳細規則
- **[README.md](../README.md)** - プロジェクト概要
- **[LICENSE](../LICENSE)** - ライセンス情報

## 🤝 コミュニティ

- **尊重**: すべてのやり取りで親切で建設的に
- **協力**: 知識を共有し、他者の学習を支援
- **品質重視**: コードとテストの高い基準を維持
- **パフォーマンス意識**: ゲームパフォーマンスへの影響を考慮

CavalryFightへの貢献ありがとうございます！一緒に素晴らしい騎兵戦闘ゲームを作りましょう！ 🏇⚔️

---

*これらのガイドラインに関する質問は、ディスカッションを開くか既存のドキュメントを確認してください。*
