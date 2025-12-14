#nullable enable

using System;

namespace CavalryFight.Services.SceneManagement
{
    /// <summary>
    /// シーンロードエラーイベントの引数
    /// </summary>
    public class SceneLoadErrorEventArgs : EventArgs
    {
        /// <summary>
        /// エラーが発生したシーン名
        /// </summary>
        public string SceneName { get; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 例外
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// SceneLoadErrorEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="sceneName">エラーが発生したシーン名</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <param name="exception">発生した例外（オプション）</param>
        public SceneLoadErrorEventArgs(string sceneName, string errorMessage, Exception? exception = null)
        {
            SceneName = sceneName;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}
