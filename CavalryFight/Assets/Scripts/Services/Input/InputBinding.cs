#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力アクションに対するキー/ボタンバインディング
    /// </summary>
    /// <remarks>
    /// 1つのアクションに対して、プライマリとセカンダリの2つのバインディングを持てます。
    /// キーボード、マウス、ゲームパッドの入力をサポートします。
    /// </remarks>
    [Serializable]
    public class InputBinding
    {
        /// <summary>
        /// このバインディングが対応するアクション
        /// </summary>
        public InputAction Action;

        /// <summary>
        /// プライマリキー（キーボード/マウスボタン）
        /// </summary>
        public KeyCode PrimaryKey;

        /// <summary>
        /// セカンダリキー（代替キー）
        /// </summary>
        public KeyCode SecondaryKey;

        /// <summary>
        /// ゲームパッドボタン名（Unity Input Managerで定義されたボタン名）
        /// </summary>
        /// <remarks>
        /// 例: "Fire1", "Fire2", "Jump" など
        /// </remarks>
        public string GamepadButton;

        /// <summary>
        /// ゲームパッド軸名（Unity Input Managerで定義された軸名）
        /// </summary>
        /// <remarks>
        /// 移動やカメラ操作などの軸入力に使用します。
        /// 例: "Horizontal", "Vertical" など
        /// </remarks>
        public string GamepadAxis;

        /// <summary>
        /// InputBindingの新しいインスタンスを初期化します。
        /// </summary>
        public InputBinding()
        {
            Action = InputAction.MoveForward;
            PrimaryKey = KeyCode.None;
            SecondaryKey = KeyCode.None;
            GamepadButton = string.Empty;
            GamepadAxis = string.Empty;
        }

        /// <summary>
        /// InputBindingの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="action">アクション</param>
        /// <param name="primaryKey">プライマリキー</param>
        /// <param name="secondaryKey">セカンダリキー</param>
        /// <param name="gamepadButton">ゲームパッドボタン名</param>
        /// <param name="gamepadAxis">ゲームパッド軸名</param>
        public InputBinding(
            InputAction action,
            KeyCode primaryKey = KeyCode.None,
            KeyCode secondaryKey = KeyCode.None,
            string gamepadButton = "",
            string gamepadAxis = "")
        {
            Action = action;
            PrimaryKey = primaryKey;
            SecondaryKey = secondaryKey;
            GamepadButton = gamepadButton ?? string.Empty;
            GamepadAxis = gamepadAxis ?? string.Empty;
        }

        /// <summary>
        /// 指定されたキーがこのバインディングに含まれるかを判定します。
        /// </summary>
        /// <param name="key">判定するキー</param>
        /// <returns>含まれる場合true</returns>
        public bool ContainsKey(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return false;
            }

            return PrimaryKey == key || SecondaryKey == key;
        }

        /// <summary>
        /// 指定されたボタン名がこのバインディングに含まれるかを判定します。
        /// </summary>
        /// <param name="buttonName">判定するボタン名</param>
        /// <returns>含まれる場合true</returns>
        public bool ContainsButton(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                return false;
            }

            return GamepadButton == buttonName;
        }

        /// <summary>
        /// 指定された軸名がこのバインディングに含まれるかを判定します。
        /// </summary>
        /// <param name="axisName">判定する軸名</param>
        /// <returns>含まれる場合true</returns>
        public bool ContainsAxis(string axisName)
        {
            if (string.IsNullOrEmpty(axisName))
            {
                return false;
            }

            return GamepadAxis == axisName;
        }
    }
}
