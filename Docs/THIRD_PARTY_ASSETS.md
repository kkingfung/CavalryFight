# サードパーティアセット

## 概要
CavalryFightプロジェクトでは、以下の4つのサードパーティアセットを使用して、騎馬戦闘ゲームの開発を効率化します。

## アセット一覧

| アセット名 | 主な用途 | CavalryFightでの使用箇所 |
|----------|---------|---------------------|
| **Malbers Animations** | 動物コントローラー・騎乗システム | 馬・動物の騎乗、移動、アニメーション |
| **MasterStylizedProjectiles** | スタイライズされた発射体エフェクト | 矢のビジュアルエフェクト |
| **P09_Modular_Humanoid** | モジュラー人型キャラクター | プレイヤーキャラクターのカスタマイゼーション |
| **Febucci Text Animator** | テキストアニメーションシステム | UI文字演出、メニュー、HUD、リザルト画面 |

---

## 1. Malbers Animations

### 📦 場所
`CavalryFight/Assets/Malbers Animations/`

### 📝 概要
Animal ControllerとHorse AnimSet Proを含む、動物の動作とアニメーションを管理するシステムです。

### 🎯 CavalryFightでの用途
- **騎乗システム**: プレイヤーが馬に乗って移動
- **動物のAI**: 馬やその他の動物の自然な動き
- **アニメーション制御**: 歩行、走行、ジャンプ、攻撃時の動作

### ✨ 主要機能
- **Animal Controller**: 動物の基本的な動作制御
- **Horse AnimSet Pro**: 馬専用の高品質なアニメーションセット
- **Riding System**: 騎乗と降車の管理
- **AI States**: 動物の行動パターン制御
- **Input System**: 入力に応じた動物の操作

### 🔧 主要コンポーネント
- `MAnimal`: 動物コントローラーのコアクラス
- `MRider`: 騎乗者の制御
- `AnimalController`: アニメーションと物理の統合
- `Horse AnimSet`: 馬専用のアニメーション

### 📚 MVVMとの統合
```
Model:      AnimalModel (動物の状態データ)
View:       MAnimal, MRider (Malbers提供)
ViewModel:  AnimalViewModel (動物の状態とコマンドの管理)
Service:    RidingService (騎乗システムの管理)
```

### ⚠️ 注意事項
- **ライセンス**: Asset Storeの購入ライセンス
- **バージョン管理**: アセット更新時は互換性を確認
- **パフォーマンス**: 複数の動物を同時に表示する場合はLOD設定を検討

### 🔗 ドキュメント
`CavalryFight/Assets/Malbers Animations/Common/ReadMe/README!.asset`

---

## 2. MasterStylizedProjectiles

### 📦 場所
`CavalryFight/Assets/MasterStylizedProjectiles/`

### 📝 概要
Shader Graphベースのスタイライズされた発射体エフェクトのコレクションです。

### 🎯 CavalryFightでの用途
- **矢のビジュアル**: 弓から発射される矢のエフェクト
- **軌跡エフェクト**: 矢の飛行軌跡の視覚化
- **ヒットエフェクト**: 矢が命中したときのエフェクト

### ✨ 主要機能
- **Multiple Styles**: 複数のビジュアルスタイル
- **Shader Graph Materials**: カスタマイズ可能なマテリアル
- **Trail Effects**: 軌跡エフェクト
- **Particle Systems**: パーティクルシステム統合

### 📂 フォルダ構成
```
MasterStylizedProjectiles/
├── Prefabs/         # 発射体プレハブ
├── Materials/       # Shader Graphマテリアル
├── Textures/        # テクスチャ
└── Shaders/         # カスタムシェーダー
```

### 📚 MVVMとの統合
```
Model:      ArrowModel (矢の物理データ)
View:       MasterProjectile Prefabs (ビジュアル表現)
ViewModel:  ArrowViewModel (矢の状態管理)
Service:    ProjectilePoolService (オブジェクトプーリング)
```

### ⚙️ 推奨設定
- **Object Pooling**: 矢のプレハブはプーリングして再利用
- **LOD設定**: 遠距離の矢は簡易版を使用
- **パーティクル制限**: 同時表示数に上限を設定

### ⚠️ 注意事項
- **パフォーマンス**: 多数の矢を同時に飛ばす場合はプーリング必須
- **シェーダー互換性**: URP (Universal Render Pipeline)を使用

### 🔗 ドキュメント
`CavalryFight/Assets/MasterStylizedProjectiles/ReadMe.txt`

---

## 3. P09_Modular_Humanoid

### 📦 場所
`CavalryFight/Assets/P09_Modular_Humanoid/`

### 📝 概要
モジュラー構造の人型キャラクターシステムで、パーツを組み合わせてカスタマイズ可能です。

### 🎯 CavalryFightでの用途
- **プレイヤーキャラクター**: カスタマイズ可能な騎手
- **キャラクターカスタマイゼーション**: 外見のカスタマイズシステム
- **装備表示**: 武器、防具の視覚的表現

### ✨ 主要機能
- **Modular Parts**: 頭、胴、手、足などのパーツを個別に変更
- **Material Swapping**: マテリアルを変更して色やテクスチャを変更
- **Equipment System**: 武器や防具の装備
- **Multiple Characters**: 複数のキャラクタータイプをサポート

### 📂 主要パーツ
```
Character Parts:
├── Head (頭部)
├── Torso (胴体)
├── Arms (腕)
├── Hands (手)
├── Legs (脚)
└── Feet (足)

Equipment:
├── Helmet (兜)
├── Armor (鎧)
├── Weapons (武器)
└── Accessories (アクセサリー)
```

### 🔗 依存関係
1. **lilToon Shader** (含まれています)
   - インストーラー: `P09_Modular_Humanoid/lilToonInstaller/lilToon-1.8.0.unitypackage`
   - セットアップ: インストール後、メニューから `lilToon > [Settings] Activate Features` を選択

2. **MagicaCloth2** (別途購入が必要)
   - 布物理シミュレーション用
   - マント、布、髪の物理演算に使用
   - Asset Storeから購入: [MagicaCloth2](https://assetstore.unity.com/packages/tools/physics/magica-cloth-2-242307)

### 📚 MVVMとの統合
```
Model:      CharacterModel (キャラクター設定データ)
View:       P09_Humanoid Prefabs (ビジュアル表現)
ViewModel:  CharacterCustomizationViewModel (カスタマイゼーション管理)
Service:    CharacterService (キャラクター管理)
            EquipmentService (装備管理)
```

### 🎨 カスタマイゼーション例
```csharp
// ViewModelからキャラクターパーツを変更
public void ChangeHelmet(HelmetType helmetType)
{
    _model.EquippedHelmet = helmetType;
    _characterService.UpdateHelmetMesh(helmetType);
}

public void ChangeArmorColor(Color color)
{
    _model.ArmorColor = color;
    _characterService.UpdateArmorMaterial(color);
}
```

### ⚠️ 注意事項
- **lilToonのセットアップ**: 必ずFeatureを有効化すること
- **MagicaCloth2**: 布物理が必要な場合は別途購入
- **パフォーマンス**: カスタマイゼーション時はメッシュ結合を検討
- **メモリ管理**: 使用していないパーツはメモリから解放

### 🔗 ドキュメント
`CavalryFight/Assets/P09_Modular_Humanoid/ReadMe_Jpn.txt`

---

## 4. Febucci Text Animator

### 📦 場所
`CavalryFight/Assets/Plugins/Febucci/Text Animator for Unity/`

### 📝 概要
テキストに様々なアニメーション効果を適用できるシステムです。TextMeshProおよびUI Toolkitに対応しています。

### 🎯 CavalryFightでの用途
- **メニューUI**: メインメニュー、設定画面のテキストアニメーション
- **戦闘HUD**: スコア表示、ダメージ数値の演出
- **リザルト画面**: 試合結果の華やかな表示
- **チュートリアル**: トレーニングモードでの説明テキスト
- **ダイアログ**: 会話、メッセージの演出

### ✨ 主要機能

#### アニメーション効果
- **Color Effects**: 色変化、レインボー効果
- **Transform Effects**: 位置、回転、サイズ、シア変形
- **Special Effects**: カスタム曲線、ランダム配置

#### タイプライター機能
- **Character-by-character Display**: 一文字ずつ表示
- **Typing Speed Control**: 速度制御
- **Sound Effects**: タイピング音の再生
- **Custom Timing**: 文字ごとの表示タイミング

#### カーブ
- **Linear**: 直線的な変化
- **Sine**: 波形の動き
- **Bounce**: バウンス効果
- **Square**: 矩形波
- **Hold**: 保持
- **Step**: 段階的変化

### 📂 主要コンポーネント
```
Core Components:
├── TextAnimator_TMP           # TextMeshPro用
├── TextElementAnimator        # UI Toolkit用
├── TypewriterComponent        # タイプライター機能
└── AnimationsDatabase         # エフェクトデータベース

Scriptables:
├── EffectScriptable          # アニメーション効果
├── CurveScriptable           # カーブ設定
├── PlaybackScriptable        # 再生設定
└── ActionScriptable          # タイプライターアクション
```

### 📚 MVVMとの統合
```
Model:      MessageModel (メッセージデータ)
View:       TextAnimator_TMP, TypewriterComponent
ViewModel:  UIMessageViewModel (メッセージ表示管理)
Service:    UIAnimationService (UI演出管理)
```

### 💻 使用例

#### 基本的なアニメーション
```csharp
// ViewModelからテキストアニメーションを制御
public class ScoreViewModel : ViewModelBase
{
    private readonly UIAnimationService _animationService;

    public void ShowScoreIncrease(int points)
    {
        string message = $"+{points} Points!";
        _animationService.PlayTextAnimation(message, "bounce");
    }
}
```

#### タイプライター効果
```csharp
// チュートリアルメッセージの表示
public class TutorialViewModel : ViewModelBase
{
    public async Task ShowTutorialMessage(string message)
    {
        await _animationService.TypewriterDisplay(
            message,
            typingSpeed: 0.05f,
            playSound: true
        );
    }
}
```

### 🎨 CavalryFightでの推奨用途

#### メインメニュー
- タイトルロゴ: `<bounce>` エフェクト
- メニュー項目: `<fade>` + `<size>` でフォーカス演出

#### 戦闘HUD
- スコア増加: `<shake>` + `<size>` + `<color>` で強調
- ダメージ数値: `<bounce>` + 色グラデーション
- 矢残弾: 少なくなったら `<shake>` で警告

#### リザルト画面
- 勝利メッセージ: `<rainbow>` + `<wave>` で華やかに
- スコア表示: タイプライター効果で順次表示
- ランク表示: `<expand>` でインパクト

#### トレーニングモード
- 説明テキスト: タイプライター効果で読みやすく
- ヒント表示: `<fade>` でさりげなく表示

### ⚙️ パフォーマンス最適化
```csharp
// 大量のテキストアニメーションを扱う場合
public class UIAnimationService : IService
{
    // アニメーション中のテキスト数を制限
    private const int MAX_ANIMATED_TEXTS = 10;

    // 画面外のアニメーションは無効化
    public void OptimizeAnimations()
    {
        foreach (var animator in _activeAnimators)
        {
            if (!IsVisible(animator))
            {
                animator.enabled = false;
            }
        }
    }
}
```

### 📋 ライセンス
- **コア機能**: Febucciのライセンス
- **Latoフォント**: SIL Open Font License (OFL)
- **BounceOutメソッド**: MIT License

### ⚠️ 注意事項
- **TextMeshPro必須**: TMPがプロジェクトにインストールされている必要があります
- **パフォーマンス**: 大量のテキストを同時にアニメーションさせる場合は最適化が必要
- **ガベージコレクション**: 頻繁なテキスト更新はアロケーションに注意
- **UI Toolkit対応**: UI ToolkitとTextMeshProの両方をサポート

### 🔗 ドキュメント
- `CavalryFight/Assets/Plugins/Febucci/Text Animator for Unity/README.pdf`
- `CavalryFight/Assets/Plugins/Febucci/Text Animator for Unity/Third-Party Notices.txt`

---

## セットアップ手順

### 1. lilToon Shaderのセットアップ
```
1. P09_Modular_Humanoid/lilToonInstaller/lilToon-1.8.0.unitypackage をインポート
2. Unityメニュー > lilToon > [Settings] Activate Features を選択
3. 必要なFeatureを有効化
```

### 2. MagicaCloth2のインストール（オプション）
```
1. Asset Storeから購入
2. Package Managerでインポート
3. P09_Modular_Humanoidの布パーツで使用
```

### 3. TextMeshProの確認
```
1. Window > TextMeshPro > Import TMP Essential Resources
2. Febucci Text Animatorが正常に動作することを確認
```

---

## MVVMアーキテクチャとの統合パターン

### 騎乗システムの例
```csharp
// Model
public class RidingModel
{
    public bool IsMounted { get; set; }
    public AnimalType CurrentAnimal { get; set; }
    public float Speed { get; set; }
}

// ViewModel
public class RidingViewModel : ViewModelBase
{
    private readonly RidingModel _model;
    private readonly RidingService _service;

    public bool IsMounted => _model.IsMounted;

    public void Mount(AnimalType animal)
    {
        _service.MountAnimal(animal); // Malbers Animal Controller使用
        _model.IsMounted = true;
        OnPropertyChanged(nameof(IsMounted));
    }
}

// Service
public class RidingService : IService
{
    private MAnimal _currentAnimal;
    private MRider _rider;

    public void MountAnimal(AnimalType type)
    {
        // Malbers Animationsのコンポーネントを使用
        _currentAnimal = GetAnimalPrefab(type);
        _rider.Mount(_currentAnimal);
    }
}
```

### 矢の発射システムの例
```csharp
// Model
public class ArrowModel
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public int Damage { get; set; }
}

// ViewModel
public class CombatViewModel : ViewModelBase
{
    private readonly ProjectilePoolService _poolService;

    public void FireArrow(Vector3 direction, float power)
    {
        // MasterStylizedProjectilesのプレハブを使用
        var arrow = _poolService.GetArrow();
        arrow.Fire(direction, power);
    }
}

// Service
public class ProjectilePoolService : IService
{
    private readonly Queue<GameObject> _arrowPool;

    public GameObject GetArrow()
    {
        // MasterStylizedProjectilesのプレハブをプーリング
        if (_arrowPool.Count > 0)
        {
            return _arrowPool.Dequeue();
        }
        return InstantiateNewArrow();
    }
}
```

### キャラクターカスタマイゼーションの例
```csharp
// Model
public class CharacterCustomizationModel
{
    public HelmetType Helmet { get; set; }
    public ArmorType Armor { get; set; }
    public Color PrimaryColor { get; set; }
}

// ViewModel
public class CustomizationViewModel : ViewModelBase
{
    private readonly CharacterService _service;

    public void ChangeHelmet(HelmetType type)
    {
        _model.Helmet = type;
        _service.UpdateCharacterPart("Helmet", type);
        OnPropertyChanged(nameof(Helmet));
    }
}

// Service
public class CharacterService : IService
{
    // P09_Modular_Humanoidのパーツ管理
    public void UpdateCharacterPart(string partName, object partType)
    {
        // モジュラーキャラクターのパーツを変更
    }
}
```

### UI演出の例
```csharp
// ViewModel
public class MatchResultViewModel : ViewModelBase
{
    private readonly UIAnimationService _animationService;

    public async Task ShowMatchResult(MatchResult result)
    {
        // Febucci Text Animatorを使用
        await _animationService.TypewriterDisplay(
            "<bounce>Victory!</bounce>",
            typingSpeed: 0.05f
        );

        await _animationService.PlayTextAnimation(
            $"<rainbow>Score: {result.Score}</rainbow>",
            "wave"
        );
    }
}

// Service
public class UIAnimationService : IService
{
    public async Task TypewriterDisplay(string text, float speed)
    {
        // Febucci TypewriterComponentを制御
    }

    public async Task PlayTextAnimation(string text, string effectName)
    {
        // Febucci TextAnimatorを制御
    }
}
```

---

## パフォーマンス考慮事項

### オブジェクトプーリング
```csharp
// 矢のプーリング (MasterStylizedProjectiles)
public class ArrowPool : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int poolSize = 50;

    private Queue<GameObject> _pool;

    void Start()
    {
        _pool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            var arrow = Instantiate(arrowPrefab);
            arrow.SetActive(false);
            _pool.Enqueue(arrow);
        }
    }
}
```

### LOD設定
- **Malbers Animals**: 遠距離の動物はLODを使用
- **P09_Humanoid**: キャラクターメッシュにLODを設定
- **MasterProjectiles**: 遠距離の矢は簡易エフェクト

### メモリ管理
- **キャラクターパーツ**: 使用していないパーツはアンロード
- **アニメーションクリップ**: 不要なアニメーションは読み込まない
- **テクスチャ圧縮**: 適切な圧縮形式を使用

---

## 注意事項とベストプラクティス

### ライセンス管理
- すべてのアセットはAsset Storeの利用規約に従う
- 商用利用可能（Asset Store購入済みの場合）
- ソースコードの再配布は禁止

### バージョン管理
- アセットの更新時は必ずテストを実施
- 互換性の問題がある場合は別ブランチで検証
- `.gitignore`に不要なファイルを追加

### 依存関係の管理
```
依存関係チェーン:
lilToon ← P09_Modular_Humanoid
MagicaCloth2 ← P09_Modular_Humanoid (オプション)
TextMeshPro ← Febucci Text Animator
```

### コーディング規則との整合性
- アセットのコンポーネントはViewとして扱う
- アセットとのやり取りはServiceレイヤーを経由
- ViewModelから直接アセットのコンポーネントを操作しない

---

## トラブルシューティング

### Malbers Animations
**問題**: 馬に乗れない
**解決策**:
1. MAnimalコンポーネントが正しく設定されているか確認
2. MRiderコンポーネントがプレイヤーにアタッチされているか確認
3. Colliderの設定を確認

### MasterStylizedProjectiles
**問題**: エフェクトが表示されない
**解決策**:
1. URPが有効になっているか確認
2. Shader Graphがインポートされているか確認
3. マテリアルが正しいシェーダーを使用しているか確認

### P09_Modular_Humanoid
**問題**: キャラクターが正しく表示されない
**解決策**:
1. lilToonがインストールされているか確認
2. Featuresが有効化されているか確認 (lilToon > [Settings] Activate Features)
3. シェーダーのコンパイルエラーを確認

### Febucci Text Animator
**問題**: アニメーションが動かない
**解決策**:
1. TextMeshProがインストールされているか確認
2. TextAnimator_TMPコンポーネントがアタッチされているか確認
3. AnimationsDatabaseが正しく設定されているか確認

---

## 参考リンク

### Asset Store
- [Malbers Animations](https://assetstore.unity.com/publishers/14246)
- [MagicaCloth2](https://assetstore.unity.com/packages/tools/physics/magica-cloth-2-242307)
- Febucci Text Animator (プロジェクト内ドキュメント参照)

### Unity公式
- [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
- [TextMeshPro](https://docs.unity3d.com/Manual/com.unity.textmeshpro.html)
- [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest)

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| 2025-12-10 | 初版作成 - 4つのサードパーティアセットのドキュメント化 |

