#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using CavalryFight.Services.Customization;
using CavalryFight.Services.Replay;

namespace CavalryFight.Editor
{
    /// <summary>
    /// リプレイデータを生成してファイルに保存するエディタツール
    /// </summary>
    /// <remarks>
    /// History画面でリプレイを確認するためのテストデータを生成します。
    /// メニュー: Tools > CavalryFight > Generate Test Replay Data
    /// </remarks>
    public static class ReplayDataGenerator
    {
        private const string ReplayDirectory = "Replays";
        private const string ReplayFileExtension = ".replay.json";

        [MenuItem("Tools/CavalryFight/Generate Test Replay Data")]
        public static void GenerateTestReplayData()
        {
            // 複数のテストリプレイを生成
            GenerateReplay("Arena", "ScoreMatch", 180f, 15, 10, "Player");
            GenerateReplay("Forest", "Deathmatch", 300f, 8, 12, "Player");
            GenerateReplay("Nature", "TeamFight", 240f, 20, 18, "Player");

            Debug.Log("[ReplayDataGenerator] テストリプレイデータの生成が完了しました。Historyシーンで確認できます。");
            EditorUtility.DisplayDialog(
                "リプレイデータ生成完了",
                $"3つのテストリプレイを生成しました。\n保存先: {GetReplayFolderPath()}",
                "OK"
            );
        }

        [MenuItem("Tools/CavalryFight/Generate Single Test Replay")]
        public static void GenerateSingleTestReplay()
        {
            GenerateReplay("Arena", "ScoreMatch", 120f, 10, 5, "Player");

            Debug.Log("[ReplayDataGenerator] テストリプレイデータを1つ生成しました。");
            EditorUtility.DisplayDialog(
                "リプレイデータ生成完了",
                $"テストリプレイを生成しました。\n保存先: {GetReplayFolderPath()}",
                "OK"
            );
        }

        [MenuItem("Tools/CavalryFight/Open Replay Folder")]
        public static void OpenReplayFolder()
        {
            string path = GetReplayFolderPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Tools/CavalryFight/Clear All Replay Data")]
        public static void ClearAllReplayData()
        {
            string path = GetReplayFolderPath();
            if (!Directory.Exists(path))
            {
                Debug.Log("[ReplayDataGenerator] リプレイフォルダが存在しません。");
                return;
            }

            var files = Directory.GetFiles(path, "*" + ReplayFileExtension);
            if (files.Length == 0)
            {
                Debug.Log("[ReplayDataGenerator] 削除するリプレイファイルがありません。");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "リプレイデータ削除確認",
                $"{files.Length}個のリプレイファイルを削除しますか？",
                "削除",
                "キャンセル"))
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                Debug.Log($"[ReplayDataGenerator] {files.Length}個のリプレイファイルを削除しました。");
            }
        }

        /// <summary>
        /// リプレイデータを生成してファイルに保存します
        /// </summary>
        private static void GenerateReplay(
            string mapName,
            string gameMode,
            float duration,
            int playerScore,
            int enemyScore,
            string playerName)
        {
            var random = new System.Random();

            var replayData = new ReplayData
            {
                ReplayId = Guid.NewGuid().ToString(),
                RecordedAt = DateTime.UtcNow.ToString("o"),
                MapName = mapName,
                GameMode = gameMode,
                PlayerName = playerName,
                MatchDuration = duration,
                FinalPlayerScore = playerScore,
                FinalEnemyScore = enemyScore,
                // プレイヤーのカスタマイズを生成
                PlayerCharacter = GenerateRandomCharacterCustomization(random),
                PlayerMount = GenerateRandomMountCustomization(random)
            };

            // 敵のカスタマイズを生成
            var enemyCustomization = new ReplayEntityCustomization(
                "enemy_0",
                "Enemy Rider",
                GenerateRandomCharacterCustomization(random),
                GenerateRandomMountCustomization(random)
            );
            replayData.Enemies.Add(enemyCustomization);

            // フレーム、イベント、ハイライトを生成
            GenerateFrames(replayData);
            GenerateEvents(replayData);
            replayData.GenerateHighlightsFromScoreEvents();

            // ファイルに保存
            string filePath = GetReplayFilePath(replayData.ReplayId);
            replayData.SaveToFile(filePath);

            Debug.Log($"[ReplayDataGenerator] リプレイを保存しました: {filePath}");
            Debug.Log($"  - フレーム数: {replayData.Frames.Count}");
            Debug.Log($"  - イベント数: {replayData.Events.Count}");
            Debug.Log($"  - ハイライト数: {replayData.Highlights.Count}");
        }

        /// <summary>
        /// フレームを生成します（0.1秒間隔）
        /// </summary>
        private static void GenerateFrames(ReplayData replayData)
        {
            var random = new System.Random(replayData.ReplayId.GetHashCode());
            float duration = replayData.MatchDuration;
            const float KEYFRAME_INTERVAL = 0.1f;

            // 初期位置を設定
            Vector3 playerStartPos = new Vector3(random.Next(-20, 20), 0f, random.Next(-20, 20));
            Vector3 enemyStartPos = new Vector3(random.Next(-20, 20), 0f, random.Next(-20, 20));

            // 移動パラメータ
            float playerAngle = (float)(random.NextDouble() * Math.PI * 2);
            float enemyAngle = (float)(random.NextDouble() * Math.PI * 2);
            float playerSpeed = 3f + (float)random.NextDouble() * 2f;
            float enemySpeed = 2f + (float)random.NextDouble() * 2f;

            int playerScore = 0;
            int enemyScore = 0;

            // スコアが発生するタイミングを事前に計算
            var playerScoreTimes = new List<float>();
            var enemyScoreTimes = new List<float>();
            for (int i = 0; i < replayData.FinalPlayerScore; i++)
            {
                playerScoreTimes.Add((float)(random.NextDouble() * (duration - 10) + 5));
            }
            for (int i = 0; i < replayData.FinalEnemyScore; i++)
            {
                enemyScoreTimes.Add((float)(random.NextDouble() * (duration - 10) + 5));
            }
            playerScoreTimes.Sort();
            enemyScoreTimes.Sort();

            int playerScoreIndex = 0;
            int enemyScoreIndex = 0;

            // フレームを生成（0.1秒間隔）
            for (float time = 0f; time <= duration; time += KEYFRAME_INTERVAL)
            {
                // スコアの更新チェック
                while (playerScoreIndex < playerScoreTimes.Count && time >= playerScoreTimes[playerScoreIndex])
                {
                    playerScore++;
                    playerScoreIndex++;
                }
                while (enemyScoreIndex < enemyScoreTimes.Count && time >= enemyScoreTimes[enemyScoreIndex])
                {
                    enemyScore++;
                    enemyScoreIndex++;
                }

                var frame = new ReplayFrame(time)
                {
                    PlayerScore = playerScore,
                    EnemyScore = enemyScore,
                    TimeRemaining = duration - time,
                    GameState = "Playing"
                };

                // プレイヤーの移動（円を描くように移動）
                float playerX = playerStartPos.x + Mathf.Cos(playerAngle + time * 0.5f) * playerSpeed * time * 0.1f;
                float playerZ = playerStartPos.z + Mathf.Sin(playerAngle + time * 0.5f) * playerSpeed * time * 0.1f;
                playerX = Mathf.Clamp(playerX, -50f, 50f);
                playerZ = Mathf.Clamp(playerZ, -50f, 50f);
                Vector3 playerPos = new Vector3(playerX, 0f, playerZ);

                // プレイヤーの向きを計算
                float playerYaw = (playerAngle + time * 0.5f) * Mathf.Rad2Deg + 90f;
                Quaternion playerRot = Quaternion.Euler(0f, playerYaw, 0f);

                // プレイヤーキャラクターのスナップショット
                var playerSnapshot = new EntitySnapshot
                {
                    EntityId = "player_0",
                    EntityType = EntityType.Player,
                    Position = playerPos + Vector3.up * 1.5f,
                    Rotation = playerRot,
                    Velocity = new Vector3(
                        Mathf.Cos(playerAngle + time * 0.5f) * playerSpeed,
                        0f,
                        Mathf.Sin(playerAngle + time * 0.5f) * playerSpeed
                    ),
                    AnimationStateHash = GetAnimationHash(time),
                    AnimationNormalizedTime = (time % 1f),
                    IsAlive = true
                };
                frame.AddEntity(playerSnapshot);

                // プレイヤーの馬のスナップショット
                var playerMountSnapshot = new EntitySnapshot
                {
                    EntityId = "mount_0",
                    EntityType = EntityType.Other,
                    Position = playerPos,
                    Rotation = playerRot,
                    Velocity = playerSnapshot.Velocity,
                    AnimationStateHash = GetMountAnimationHash(time),
                    AnimationNormalizedTime = (time % 1f),
                    IsAlive = true
                };
                frame.AddEntity(playerMountSnapshot);

                // 敵の移動
                float enemyX = enemyStartPos.x + Mathf.Sin(enemyAngle + time * 0.3f) * enemySpeed * time * 0.1f;
                float enemyZ = enemyStartPos.z + Mathf.Cos(enemyAngle + time * 0.3f) * enemySpeed * time * 0.1f;
                enemyX = Mathf.Clamp(enemyX, -50f, 50f);
                enemyZ = Mathf.Clamp(enemyZ, -50f, 50f);
                Vector3 enemyPos = new Vector3(enemyX, 0f, enemyZ);

                float enemyYaw = (enemyAngle + time * 0.3f) * Mathf.Rad2Deg + 90f;
                Quaternion enemyRot = Quaternion.Euler(0f, enemyYaw, 0f);

                // 敵キャラクターのスナップショット
                var enemySnapshot = new EntitySnapshot
                {
                    EntityId = "enemy_0",
                    EntityType = EntityType.Enemy,
                    Position = enemyPos + Vector3.up * 1.5f,
                    Rotation = enemyRot,
                    Velocity = new Vector3(
                        Mathf.Sin(enemyAngle + time * 0.3f) * enemySpeed,
                        0f,
                        Mathf.Cos(enemyAngle + time * 0.3f) * enemySpeed
                    ),
                    AnimationStateHash = GetAnimationHash(time + 0.5f),
                    AnimationNormalizedTime = ((time + 0.5f) % 1f),
                    IsAlive = true
                };
                frame.AddEntity(enemySnapshot);

                // 敵の馬のスナップショット
                var enemyMountSnapshot = new EntitySnapshot
                {
                    EntityId = "mount_enemy_0",
                    EntityType = EntityType.Other,
                    Position = enemyPos,
                    Rotation = enemyRot,
                    Velocity = enemySnapshot.Velocity,
                    AnimationStateHash = GetMountAnimationHash(time + 0.5f),
                    AnimationNormalizedTime = ((time + 0.5f) % 1f),
                    IsAlive = true
                };
                frame.AddEntity(enemyMountSnapshot);

                replayData.AddFrame(frame);
            }
        }

        /// <summary>
        /// イベントを生成します
        /// </summary>
        private static void GenerateEvents(ReplayData replayData)
        {
            var random = new System.Random(replayData.ReplayId.GetHashCode() + 1);
            float duration = replayData.MatchDuration;

            // マッチ開始イベント
            var matchStartEvent = new ReplayEvent(0f, ReplayEventType.MatchStart, "System", "Match started");
            replayData.AddEvent(matchStartEvent);

            // プレイヤースポーンイベント
            var playerSpawnEvent = new ReplayEvent(0.1f, ReplayEventType.PlayerSpawn, "player_0", "Player spawned");
            replayData.AddEvent(playerSpawnEvent);

            // 敵スポーンイベント
            var enemySpawnEvent = new ReplayEvent(0.2f, ReplayEventType.EnemySpawn, "enemy_0", "Enemy spawned");
            replayData.AddEvent(enemySpawnEvent);

            // スコアイベントを生成（プレイヤー）
            var playerScoreTimes = new List<float>();
            for (int i = 0; i < replayData.FinalPlayerScore; i++)
            {
                playerScoreTimes.Add((float)(random.NextDouble() * (duration - 10) + 5));
            }
            playerScoreTimes.Sort();

            string[] hitLocations = { "Head", "Body", "Arm", "Leg" };
            int[] hitScores = { 100, 50, 30, 20 };

            for (int i = 0; i < playerScoreTimes.Count; i++)
            {
                int locationIndex = random.Next(hitLocations.Length);
                string location = hitLocations[locationIndex];
                int score = hitScores[locationIndex];

                var scoreEvent = new ReplayEvent(
                    playerScoreTimes[i],
                    ReplayEventType.Score,
                    "player_0",
                    $"{location} Shot! +{score}"
                );
                scoreEvent.TargetEntityId = "enemy_0";
                scoreEvent.AddCustomData("HitLocation", location);
                scoreEvent.AddCustomData("Score", score.ToString());
                replayData.AddEvent(scoreEvent);
            }

            // スコアイベントを生成（敵）
            var enemyScoreTimes = new List<float>();
            for (int i = 0; i < replayData.FinalEnemyScore; i++)
            {
                enemyScoreTimes.Add((float)(random.NextDouble() * (duration - 10) + 5));
            }
            enemyScoreTimes.Sort();

            for (int i = 0; i < enemyScoreTimes.Count; i++)
            {
                int locationIndex = random.Next(hitLocations.Length);
                string location = hitLocations[locationIndex];
                int score = hitScores[locationIndex];

                var scoreEvent = new ReplayEvent(
                    enemyScoreTimes[i],
                    ReplayEventType.Score,
                    "enemy_0",
                    $"Enemy {location} Shot! +{score}"
                );
                scoreEvent.TargetEntityId = "player_0";
                scoreEvent.AddCustomData("HitLocation", location);
                scoreEvent.AddCustomData("Score", score.ToString());
                replayData.AddEvent(scoreEvent);
            }

            // マッチ終了イベント
            var matchEndEvent = new ReplayEvent(duration, ReplayEventType.MatchEnd, "System", "Match ended");
            replayData.AddEvent(matchEndEvent);

            // イベントをタイムスタンプでソート
            replayData.Events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }

        /// <summary>
        /// キャラクター用のアニメーションハッシュを取得します
        /// </summary>
        private static int GetAnimationHash(float time)
        {
            int[] mockHashes = { 12345, 23456, 34567, 45678 };
            int index = (int)(time * 0.5f) % mockHashes.Length;
            return mockHashes[index];
        }

        /// <summary>
        /// 馬用のアニメーションハッシュを取得します
        /// </summary>
        private static int GetMountAnimationHash(float time)
        {
            int[] mockHashes = { 11111, 22222, 33333 };
            int index = (int)(time * 0.3f) % mockHashes.Length;
            return mockHashes[index];
        }

        /// <summary>
        /// リプレイフォルダのパスを取得します
        /// </summary>
        private static string GetReplayFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, ReplayDirectory);
        }

        /// <summary>
        /// リプレイファイルのパスを取得します
        /// </summary>
        private static string GetReplayFilePath(string replayId)
        {
            string folderPath = GetReplayFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(folderPath, replayId + ReplayFileExtension);
        }

        #region Customization Generation

        /// <summary>
        /// ランダムなキャラクターカスタマイズを生成します
        /// </summary>
        private static CharacterCustomization GenerateRandomCharacterCustomization(System.Random random)
        {
            return new CharacterCustomization
            {
                Gender = random.Next(2) == 0 ? Gender.Male : Gender.Female,
                FaceType = random.Next(1, 4),
                HairstyleId = random.Next(0, 15),
                HairColorId = random.Next(1, 10),
                EyeColorId = random.Next(1, 6),
                FacialHairId = random.Next(0, 9),
                SkinToneId = random.Next(1, 4),
                BustSize = random.Next(1, 4),
                HeadArmorId = random.Next(0, 13),
                ChestArmorId = random.Next(0, 13),
                ArmsArmorId = random.Next(0, 13),
                WaistArmorId = random.Next(1, 13),
                LegsArmorId = random.Next(0, 13),
                BowId = random.Next(10, 14),
                ArrowType = (ArrowType)random.Next(0, 3)
            };
        }

        /// <summary>
        /// ランダムな馬カスタマイズを生成します
        /// </summary>
        private static MountCustomization GenerateRandomMountCustomization(System.Random random)
        {
            var mountTypes = (MountType[])Enum.GetValues(typeof(MountType));
            var coatColors = (HorseColor[])Enum.GetValues(typeof(HorseColor));
            var maneStyles = (ManeStyle[])Enum.GetValues(typeof(ManeStyle));
            var maneColors = (ManeColor[])Enum.GetValues(typeof(ManeColor));

            return new MountCustomization
            {
                MountType = mountTypes[random.Next(mountTypes.Length)],
                CoatColor = coatColors[random.Next(coatColors.Length)],
                ManeStyle = maneStyles[random.Next(maneStyles.Length)],
                ManeColor = maneColors[random.Next(maneColors.Length)],
                HornType = HornType.None,
                ArmorId = random.Next(0, 4),
                HasSaddle = true,
                HasReins = true
            };
        }

        #endregion
    }
}
