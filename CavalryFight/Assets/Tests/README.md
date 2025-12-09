# Tests

## 概要
ユニットテストと統合テストを配置します。Unity Test Frameworkを使用します。

## フォルダ構成
- **EditMode/**: エディットモードテスト（ゲーム実行不要）
- **PlayMode/**: プレイモードテスト（ゲーム実行が必要）

## EditMode Tests
ピュアなC#ロジックのテスト（Model、ViewModel、Services等）

### 例
```csharp
using NUnit.Framework;
using CavalryFight.Models.Player;

namespace CavalryFight.Tests.EditMode
{
    public class PlayerModelTests
    {
        [Test]
        public void TakeDamage_ReducesHealth()
        {
            // Arrange
            var player = new PlayerModel("TestPlayer", 100);

            // Act
            player.TakeDamage(30);

            // Assert
            Assert.AreEqual(70, player.Health);
        }

        [Test]
        public void TakeDamage_DoesNotGoBelowZero()
        {
            // Arrange
            var player = new PlayerModel("TestPlayer", 100);

            // Act
            player.TakeDamage(150);

            // Assert
            Assert.AreEqual(0, player.Health);
        }
    }
}
```

## PlayMode Tests
Unityシーン内での統合テスト（MonoBehaviour、物理演算等）

### 例
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavalryFight.Tests.PlayMode
{
    public class ArrowPhysicsTests
    {
        [UnityTest]
        public IEnumerator Arrow_FollowsBallisticTrajectory()
        {
            // Arrange
            var arrowPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Arrow");
            var arrow = Object.Instantiate(arrowPrefab);

            // Act
            arrow.GetComponent<Rigidbody>().velocity = new Vector3(10, 10, 0);
            yield return new WaitForSeconds(1.0f);

            // Assert
            Assert.Less(arrow.transform.position.y, 10f);

            // Cleanup
            Object.Destroy(arrow);
        }
    }
}
```

## テスト実行
Unity Editor → Window → General → Test Runner

## 注意事項
- すべてのビジネスロジックにテストを記述
- テストカバレッジを意識
- テストは高速に実行できるように
- モックを活用して依存関係を分離
