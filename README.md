# 🏇 CavalryFight

**騎兵戦闘をテーマにしたFPS × アクションゲーム**

CavalryFightは、馬や動物に騎乗して戦う新しいタイプの戦闘ゲームです。弓矢による精密な遠距離戦闘と戦術的なゲームプレイを特徴としています。

[![Unity Version](https://img.shields.io/badge/Unity-6000.2.6f2-blue)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

---

## 📋 目次

- [ゲーム概要](#-ゲーム概要)
- [主要機能](#-主要機能)
- [ゲームモード](#-ゲームモード)
- [技術スタック](#-技術スタック)
- [セットアップ](#-セットアップ)
- [開発ガイドライン](#-開発ガイドライン)
- [プロジェクト構造](#-プロジェクト構造)
- [貢献方法](#-貢献方法)
- [ロードマップ](#-ロードマップ)
- [ライセンス](#-ライセンス)

---

## 🎮 ゲーム概要

CavalryFightは、FPSとアクションゲームの要素を融合させた革新的な騎兵戦闘ゲームです。プレイヤーは馬や他の動物に騎乗し、弓矢を使った遠距離戦闘で敵と戦います。

### コアコンセプト
- **🏹 精密な弓矢戦闘**: 弾道物理と部位別ダメージシステム
- **🐴 騎乗メカニクス**: リアルな騎乗システムと機動性
- **🎯 スキルベース**: 技術と戦術が勝敗を分ける
- **🎨 深いカスタマイゼーション**: キャラクターと装備のカスタマイズ
- **📹 リプレイシステム**: マッチを記録し、分析・共有可能

---

## ✨ 主要機能

### 🏹 戦闘システム
- **弓矢戦闘**: リアルな弾道計算と物理エンジン
- **部位ダメージ**: 命中部位によって異なるダメージとスコア
- **近接戦闘**: 弓矢が尽きた時の近接オプション（将来実装予定）
- **戦術的深度**: 地形を活用した戦略的プレイ

### 🐴 騎乗システム
- **多様な動物**: 馬をはじめとする様々な騎乗動物
- **機動性**: スピードと機動性を活かした戦闘
- **制御システム**: 直感的な騎乗コントロール

### 🎯 スコアリングシステム
- **部位別ポイント**: ヘッドショット、ボディショット等で異なるポイント
- **矢の制限**: 限られた矢でどれだけスコアを稼げるか
- **マッチ形式**: ベストオブN形式（例：7ゲーム中4勝）
- **制限時間**: 各マッチに時間制限を設定

### 📹 リプレイシステム
- **完全記録**: すべてのマッチを自動記録
- **サードパーソン視点**: 自由なカメラアングルで再生
- **分析ツール**: プレイを振り返り、改善点を発見
- **共有機能**: ベストプレイを他のプレイヤーと共有

### 🎨 カスタマイゼーション
- **キャラクター**: 外見、装備、カラースキーム
- **装備**: 弓、矢、防具のカスタマイズ
- **騎乗動物**: 見た目と能力のカスタマイズ
- **プリセット**: お気に入りの設定を保存

### 🎓 トレーニングモード
- **練習環境**: ゲームメカニクスを学ぶ安全な空間
- **チュートリアル**: 基本から上級テクニックまで
- **ターゲット練習**: 射撃精度を向上させる練習場

---

## 🎯 ゲームモード

### Arena Mode（アリーナモード）
- 定められたアリーナで戦闘
- 制限時間内でのスコア獲得
- 環境を活用した戦術的戦闘

### Score Match（スコアマッチ）
- 限られた矢でポイント獲得
- 部位別スコアリング
- ベストオブN形式の勝負

### Team Fight（チームファイト）
- チーム対チームの協力プレイ
- 戦術と連携が重要
- チームスコアで勝敗決定

### Deathmatch（デスマッチ）
- 個人戦の自由戦闘
- 最後の一人まで戦う
- サバイバル要素

### PvE Mode（PvEモード）
- AIを相手にした戦闘
- スキル向上とトレーニング
- ストーリーミッション（将来実装予定）

### Training Mode（トレーニングモード）
- 初心者向け練習モード
- メカニクスの習得
- ターゲット練習とチュートリアル

---

## 🔧 技術スタック

### ゲームエンジン
- **Unity 6** (6000.2.6f2)
- **.NET 8.0**

### アーキテクチャ
- **MVVMパターン**: 明確な責務分離
- **Nullable参照型**: 型安全性の確保
- **イベント駆動**: リプレイシステムのため

### 主要パッケージ
- **Unity Input System**: 最新の入力処理
- **Unity Mathematics**: 高性能な数学計算
- *(その他のパッケージは開発中に追加予定)*

### 開発ツール
- **Visual Studio 2022** / **VS Code**
- **Git** + **Git LFS** (大容量アセット管理)
- **Unity Test Framework** (ユニット＆統合テスト)

---

## 🚀 セットアップ

### 前提条件
- **Unity Hub** (最新版)
- **Unity 6** (6000.2.6f2以降)
- **Git** (バージョン管理)
- **Git LFS** (大容量アセット用)
- **Visual Studio 2022** または **VS Code** (Unity拡張機能付き)

### インストール手順

1. **リポジトリをクローン**
   ```bash
   git clone https://github.com/kkingfung/CavalryFight.git
   cd CavalryFight
   ```

2. **Git LFSをセットアップ**
   ```bash
   git lfs install
   git lfs pull
   ```

3. **Unity Hubでプロジェクトを開く**
   - Unity Hubを起動
   - 「開く」をクリック
   - `CavalryFight/CavalryFight`フォルダを選択
   - Unity 6で開く

4. **初回コンパイルを待つ**
   - Unityが自動的にプロジェクトを設定
   - 初回は時間がかかる場合があります

5. **動作確認**
   - コンソールにエラーがないか確認
   - サンプルシーンが正常に読み込まれるか確認

---

## 📖 開発ガイドライン

### コーディング規則

**必読**: すべての貢献者は[コーディング規則](Docs/CODING_RULES.md)を熟読してください。

#### 重要な規則
1. **MVVMパターン**: Model-View-ViewModelアーキテクチャを使用
2. **Nullable参照型**: ViewModelで`#nullable enable`を使用
3. **Regions**: コードを以下のRegionsで整理
   - `#region Fields`
   - `#region Properties`
   - `#region Unity Lifecycle`
   - `#region Public Methods`
   - `#region Private Methods`
   - `#region Event Handlers`
4. **Namespace**: `CavalryFight.{Layer}.{Feature}`の形式
5. **XMLコメント**: すべてのpublic/protected/internalメンバーに必須

#### 命名規則
- **クラス/構造体**: `PascalCase`
- **インターフェース**: `IPascalCase`
- **プライベートフィールド**: `_camelCase`
- **パブリックプロパティ**: `PascalCase`
- **定数**: `UPPER_SNAKE_CASE`

### 開発ワークフロー

1. **ブランチ戦略**
   - `main`: 安定版
   - `feature/*`: 新機能開発
   - `bugfix/*`: バグ修正
   - `hotfix/*`: 緊急修正

2. **コミット前のチェック**
   - [ ] コードがコンパイルされる
   - [ ] コーディング規則に従っている
   - [ ] XMLコメントを記述している
   - [ ] テストが通る
   - [ ] パフォーマンスに問題がない

3. **プルリクエスト**
   - PRテンプレートを使用
   - コーディング規則チェックリストを確認
   - スクリーンショット/動画を追加（ビジュアル変更の場合）
   - レビューを待つ

詳細は[CONTRIBUTING.md](.github/CONTRIBUTING.md)を参照してください。

---

## 📁 プロジェクト構造

```
CavalryFight/
├── .github/                    # GitHub設定（Issue/PRテンプレート等）
│   ├── ISSUE_TEMPLATE/
│   ├── PULL_REQUEST_TEMPLATE/
│   └── CONTRIBUTING.md
├── CavalryFight/              # Unityプロジェクトルート
│   ├── Assets/
│   │   ├── Scenes/           # ゲームシーン
│   │   ├── Scripts/          # C#スクリプト
│   │   │   ├── Models/      # MVVMのModel層
│   │   │   ├── Views/       # MVVMのView層
│   │   │   ├── ViewModels/  # MVVMのViewModel層
│   │   │   ├── Services/    # 共通サービス
│   │   │   └── Commands/    # UIコマンド
│   │   ├── Prefabs/         # プレハブ
│   │   ├── Materials/       # マテリアル
│   │   ├── Textures/        # テクスチャ
│   │   ├── Models/          # 3Dモデル
│   │   ├── Animations/      # アニメーション
│   │   ├── Audio/           # サウンドエフェクト/BGM
│   │   ├── UI/              # UIアセット
│   │   └── Tests/           # テストコード
│   │       ├── EditMode/    # エディットモードテスト
│   │       └── PlayMode/    # プレイモードテスト
│   ├── ProjectSettings/      # Unityプロジェクト設定
│   └── Packages/             # パッケージ依存関係
├── Docs/                      # ドキュメント
│   └── CODING_RULES.md       # コーディング規則
├── .gitignore
├── LICENSE
└── README.md                  # このファイル
```

---

## 🤝 貢献方法

CavalryFightへの貢献を歓迎します！

### 貢献の流れ

1. **Issueを確認**
   - 既存のIssueを確認
   - 新しいIssueを作成（必要に応じて）

2. **フォーク＆クローン**
   ```bash
   git clone https://github.com/YOUR_USERNAME/CavalryFight.git
   ```

3. **ブランチを作成**
   ```bash
   git checkout -b feature/your-feature-name
   ```

4. **開発**
   - [コーディング規則](Docs/CODING_RULES.md)に従う
   - テストを記述
   - コミットメッセージは明確に

5. **プルリクエスト**
   - PRテンプレートを使用
   - レビューを受ける
   - マージされる

詳細な貢献ガイドラインは[CONTRIBUTING.md](.github/CONTRIBUTING.md)を参照してください。

### 報告とフィードバック

- **🐛 バグ報告**: [バグ報告テンプレート](https://github.com/kkingfung/CavalryFight/issues/new?template=bug_report.md)
- **✨ 機能リクエスト**: [機能リクエストテンプレート](https://github.com/kkingfung/CavalryFight/issues/new?template=feature_request.md)
- **💬 ディスカッション**: [Discussions](https://github.com/kkingfung/CavalryFight/discussions)

---

## 🗺️ ロードマップ

### フェーズ1: 基礎実装（現在）
- [x] プロジェクトセットアップ
- [x] コーディング規則策定
- [x] ドキュメント整備
- [ ] 基本的な騎乗システム
- [ ] 弓矢戦闘システム
- [ ] 部位ダメージシステム

### フェーズ2: コアゲームプレイ
- [ ] Arena Mode実装
- [ ] Score Match実装
- [ ] 基本的なマルチプレイ
- [ ] トレーニングモード

### フェーズ3: 拡張機能
- [ ] リプレイシステム
- [ ] カスタマイゼーション
- [ ] Team Fight Mode
- [ ] Deathmatch Mode
- [ ] PvE Mode

### フェーズ4: 磨き上げ
- [ ] UI/UX改善
- [ ] パフォーマンス最適化
- [ ] バランス調整
- [ ] コンテンツ追加

### 将来の展望
- [ ] ストーリーモード
- [ ] ランクマッチ
- [ ] シーズンシステム
- [ ] 追加の騎乗動物
- [ ] 新しいマップとアリーナ
- [ ] カスタムゲームモード

---

## 📊 プロジェクト状態

| カテゴリ | 状態 |
|---------|------|
| 開発段階 | 🟡 初期開発 |
| コア機能 | 🔴 未実装 |
| ドキュメント | 🟢 完了 |
| テスト | 🔴 未実装 |
| CI/CD | 🔴 未設定 |

---

## 📚 追加リソース

- **[コーディング規則](Docs/CODING_RULES.md)** - 必読の開発ガイドライン
- **[貢献ガイド](.github/CONTRIBUTING.md)** - 貢献方法の詳細
- **[Wiki](https://github.com/kkingfung/CavalryFight/wiki)** - プロジェクトWiki（作成予定）
- **[Discussions](https://github.com/kkingfung/CavalryFight/discussions)** - コミュニティディスカッション

---

## 👥 チーム

現在、プロジェクトは初期開発段階です。貢献者を募集中です！

---

## 📄 ライセンス

このプロジェクトは[MITライセンス](LICENSE)の下でライセンスされています。

---

## 🙏 謝辞

- Unity Technologies - ゲームエンジン
- すべての貢献者とサポーター

---

<div align="center">

**CavalryFightで素晴らしい騎兵戦闘を体験しよう！** 🏇⚔️

[Issues](https://github.com/kkingfung/CavalryFight/issues) • [Discussions](https://github.com/kkingfung/CavalryFight/discussions) • [Wiki](https://github.com/kkingfung/CavalryFight/wiki)

Made with ❤️ by the CavalryFight Team

</div>
