#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力バインディングプロファイル
    /// </summary>
    /// <remarks>
    /// すべての入力アクションに対するキーバインディングのコレクションを管理します。
    /// デフォルト設定の作成、バインディングの取得/設定、JSON保存をサポートします。
    /// </remarks>
    [Serializable]
    public class InputBindingProfile
    {
        /// <summary>
        /// プロファイル名
        /// </summary>
        public string ProfileName;

        /// <summary>
        /// すべてのバインディング
        /// </summary>
        public List<InputBinding> Bindings;

        /// <summary>
        /// InputBindingProfileの新しいインスタンスを初期化します。
        /// </summary>
        public InputBindingProfile()
        {
            ProfileName = "Default";
            Bindings = new List<InputBinding>();
        }

        /// <summary>
        /// InputBindingProfileの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="profileName">プロファイル名</param>
        public InputBindingProfile(string profileName)
        {
            ProfileName = profileName;
            Bindings = new List<InputBinding>();
        }

        /// <summary>
        /// 指定されたアクションのバインディングを取得します。
        /// </summary>
        /// <param name="action">取得するアクション</param>
        /// <returns>バインディング。存在しない場合はnull</returns>
        public InputBinding? GetBinding(InputAction action)
        {
            return Bindings.FirstOrDefault(b => b.Action == action);
        }

        /// <summary>
        /// 指定されたアクションのバインディングを設定します。
        /// </summary>
        /// <param name="binding">設定するバインディング</param>
        public void SetBinding(InputBinding binding)
        {
            var existing = GetBinding(binding.Action);
            if (existing != null)
            {
                Bindings.Remove(existing);
            }

            Bindings.Add(binding);
        }

        /// <summary>
        /// デフォルトのバインディングプロファイルを作成します。
        /// </summary>
        /// <returns>デフォルト設定のプロファイル</returns>
        public static InputBindingProfile CreateDefault()
        {
            var profile = new InputBindingProfile("Default");

            // 移動キー（WASD + 矢印キー）
            profile.Bindings.Add(new InputBinding(
                InputAction.MoveForward,
                primaryKey: KeyCode.W,
                secondaryKey: KeyCode.UpArrow,
                gamepadAxis: "Vertical"
            ));

            profile.Bindings.Add(new InputBinding(
                InputAction.MoveBackward,
                primaryKey: KeyCode.S,
                secondaryKey: KeyCode.DownArrow,
                gamepadAxis: "Vertical"
            ));

            profile.Bindings.Add(new InputBinding(
                InputAction.MoveLeft,
                primaryKey: KeyCode.A,
                secondaryKey: KeyCode.LeftArrow,
                gamepadAxis: "Horizontal"
            ));

            profile.Bindings.Add(new InputBinding(
                InputAction.MoveRight,
                primaryKey: KeyCode.D,
                secondaryKey: KeyCode.RightArrow,
                gamepadAxis: "Horizontal"
            ));

            // カメラ操作（マウス + ゲームパッド右スティック）
            profile.Bindings.Add(new InputBinding(
                InputAction.CameraHorizontal,
                gamepadAxis: "Mouse X"
            ));

            profile.Bindings.Add(new InputBinding(
                InputAction.CameraVertical,
                gamepadAxis: "Mouse Y"
            ));

            // 攻撃（左クリック + ゲームパッドボタン）
            profile.Bindings.Add(new InputBinding(
                InputAction.Attack,
                primaryKey: KeyCode.Mouse0,
                gamepadButton: "Fire1"
            ));

            // 攻撃キャンセル（右クリック）
            profile.Bindings.Add(new InputBinding(
                InputAction.CancelAttack,
                primaryKey: KeyCode.Mouse1,
                gamepadButton: "Fire2"
            ));

            // 騎乗/降馬（Eキー）
            profile.Bindings.Add(new InputBinding(
                InputAction.Mount,
                primaryKey: KeyCode.E,
                gamepadButton: "Fire3"
            ));

            // ジャンプ（スペース）
            profile.Bindings.Add(new InputBinding(
                InputAction.Jump,
                primaryKey: KeyCode.Space,
                gamepadButton: "Jump"
            ));

            // メニュー（ESC）
            profile.Bindings.Add(new InputBinding(
                InputAction.Menu,
                primaryKey: KeyCode.Escape
            ));

            // ポーズ（P）
            profile.Bindings.Add(new InputBinding(
                InputAction.Pause,
                primaryKey: KeyCode.P,
                gamepadButton: "Cancel"
            ));

            return profile;
        }

        /// <summary>
        /// プロファイルの妥当性を検証します。
        /// </summary>
        /// <returns>すべてのアクションにバインディングが存在する場合true</returns>
        public bool Validate()
        {
            var allActions = Enum.GetValues(typeof(InputAction)).Cast<InputAction>();

            foreach (var action in allActions)
            {
                var binding = GetBinding(action);
                if (binding == null)
                {
                    Debug.LogWarning($"[InputBindingProfile] Missing binding for action: {action}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// プロファイルをJSON文字列に変換します。
        /// </summary>
        /// <returns>JSON文字列</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, prettyPrint: true);
        }

        /// <summary>
        /// JSON文字列からプロファイルを作成します。
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>デシリアライズされたプロファイル</returns>
        public static InputBindingProfile? FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<InputBindingProfile>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputBindingProfile] Failed to deserialize profile: {ex.Message}");
                return null;
            }
        }
    }
}
