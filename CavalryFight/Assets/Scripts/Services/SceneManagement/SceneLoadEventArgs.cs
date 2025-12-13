#nullable enable

using System;

namespace CavalryFight.Services.SceneManagement
{
    /// <summary>
    /// シーンロードイベントの引数
    /// </summary>
    public class SceneLoadEventArgs : EventArgs
    {
        /// <summary>
        /// ロードされたシーン名
        /// </summary>
        public string SceneName { get; }

        /// <summary>
        /// ロードにかかった時間（秒）
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// SceneLoadEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="sceneName">ロードされたシーン名</param>
        /// <param name="duration">ロードにかかった時間（秒）</param>
        public SceneLoadEventArgs(string sceneName, float duration)
        {
            SceneName = sceneName;
            Duration = duration;
        }
    }
}
