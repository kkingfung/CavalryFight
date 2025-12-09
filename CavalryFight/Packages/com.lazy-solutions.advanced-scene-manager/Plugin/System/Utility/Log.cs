#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace AdvancedSceneManager
{

    /// <summary>Provides simple logging methods with support for development-only logs.</summary>
    /// <remarks>Only available in #ASM_DEV.</remarks>
#if ASM_DEV
    public
#else
    internal
#endif
    static class Log
    {

        [HideInCallstack]
        public static void Info(string message, bool onlyLogInDev = true, bool logStackTrace = false)
        {
            if (!onlyLogInDev || IsDev())
                LogInternal(LogType.Log, message, logStackTrace);
        }

        [HideInCallstack]
        public static void Info(object obj, bool onlyLogInDev = true, bool logStackTrace = false)
        {
            if (onlyLogInDev && !IsDev())
                return;

            if (obj is IList list)
                List(list, logStackTrace: logStackTrace);
            else
                LogInternal(LogType.Log, obj?.ToString() ?? "(null)", logStackTrace);
        }

        [HideInCallstack]
        public static void Warning(string message, bool onlyLogInDev = false, bool logStackTrace = true)
        {
            if (!onlyLogInDev || IsDev())
                LogInternal(LogType.Warning, message, logStackTrace);
        }

        [HideInCallstack]
        public static void Error(string message, bool onlyLogInDev = false, bool logStackTrace = true)
        {
            if (!onlyLogInDev || IsDev())
                LogInternal(LogType.Error, message, logStackTrace);
        }

        [HideInCallstack]
        public static void Exception(Exception ex, bool onlyLogInDev = false)
            => Exception(ex, null, onlyLogInDev);

        [HideInCallstack]
        public static void Exception(Exception ex, string message, bool onlyLogInDev = false)
        {
            if (!onlyLogInDev || IsDev())
            {
                if (!string.IsNullOrEmpty(message))
                    LogInternal(LogType.Error, message!, logStackTrace: true);
                Debug.LogException(ex); // always with stack trace
            }
        }

        [HideInCallstack]
        private static void LogInternal(LogType type, string message, bool logStackTrace)
        {

#if !UNITY_EDITOR
            OnScreenLogConsole.Log((type, message));
#endif

            if (logStackTrace)
            {
                // Standard logging with stack trace (respects Console prefs)
                Debug.unityLogger.Log(type, message);
            }
            else
            {
                // Completely suppresses stack trace and hides LogInternal from caller
                Debug.LogFormat(type, LogOption.NoStacktrace, null, "{0}", message);
            }
        }

#if ASM_DEV
        public static bool IsDev() => true;
#else
        public static bool IsDev() => false;
#endif

        public sealed class LogTimer : IDisposable
        {
            private Action<TimeSpan> onStop;
            private readonly Stopwatch watch = new();
            private bool disposed;

            public TimeSpan Elapsed => watch.Elapsed;

            public LogTimer(Action<TimeSpan> onStop)
            {
                this.onStop = onStop;
                watch.Start();
            }

            public void Stop() => ((IDisposable)this).Dispose();

            void IDisposable.Dispose()
            {
                if (disposed) return;
                disposed = true;

                watch.Stop();
                onStop?.Invoke(watch.Elapsed);
                onStop = null!;
            }
        }

        [HideInCallstack]
        public static LogTimer Duration(string logMessage, bool onlyLogInDev = true, Func<TimeSpan, string> toStringOverride = null, bool logStackTrace = false)
        {
            toStringOverride ??= t => t.TotalSeconds.ToString("F3");
            return new(time => Info(string.Format(logMessage, toStringOverride(time)), onlyLogInDev, logStackTrace));
        }

        [HideInCallstack]
        public static LogTimer Duration()
        {
            return new(_ => { });
        }

        [HideInCallstack]
        public static void List(this IEnumerable list, string header = null, string separator = "\n", bool logStackTrace = false, bool onlyLogInDev = true)
        {
            if (onlyLogInDev && !IsDev())
                return;

            var items = list?.OfType<object>()?.ToArray();
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(header))
                sb.AppendLine(header);

            if (items is null || items.Length == 0)
                sb.AppendLine("No items");
            else
                sb.Append(string.Join(separator, items));

            LogInternal(LogType.Log, sb.ToString(), logStackTrace);
        }

        [HideInCallstack]
        public static void List(this IList list, string header = null, string separator = "\n", bool logStackTrace = false, bool onlyLogInDev = true) =>
            List((IEnumerable)list, header, separator, logStackTrace, onlyLogInDev);

        [HideInCallstack]
        public static void List<T>(this T[] list, string header = null, string separator = "\n", bool logStackTrace = false, bool onlyLogInDev = true) =>
            List((IEnumerable)list, header, separator, logStackTrace, onlyLogInDev);

        [DefaultExecutionOrder(int.MaxValue)]
        class OnScreenLogConsole : MonoBehaviour
        {

            private static List<(LogType type, string message)> logQueue = new();
            const int maxCount = 100;
            private Vector2 scroll;

            private static int logVersion; // increments each time a log is added

            static OnScreenLogConsole instance;

            public static void Log((LogType type, string message) log)
            {

                Initialize();

                logQueue.Add(log);
                if (logQueue.Count > maxCount)
                    logQueue = logQueue.TakeLast(maxCount).ToList();

                logVersion++; // mark that a new log was added

            }

            static void Initialize()
            {
                if (instance)
                    return;

                var existing = FindFirstObjectByType<OnScreenLogConsole>();
                if (existing)
                {
                    instance = existing;
                    return;
                }

                var obj = new GameObject("OnScreenLogConsole");
                DontDestroyOnLoad(obj);
                instance = obj.AddComponent<OnScreenLogConsole>();
            }

            void OnEnable()
            {
                if (instance && instance != this)
                {
                    DestroyImmediate(gameObject);
                    return;
                }

                instance = this;
            }

            void OnDisable()
            {
                if (instance == this)
                {
                    instance = null;
                    logQueue.Clear();
                }
            }

            private int lastVersion;

            void OnGUI()
            {

                const float padding = 10f;
                const float width = 600f;
                const float maxHeight = 300f;

                // Auto-scroll only when new log added
                if (logVersion != lastVersion)
                {
                    scroll.y = float.MaxValue;
                    lastVersion = logVersion;
                }

                // Calculate total text height dynamically
                float lineHeight = GUI.skin.label.lineHeight;
                float totalHeight = (logQueue.Count * (lineHeight + 2)) + 10f; // +2 for spacing, +10 padding
                float height = Mathf.Min(maxHeight, totalHeight);

                var rect = new Rect(padding, padding, width, height);

                // Background
                GUI.color = new Color(0, 0, 0, 0.6f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Align text exactly with background
                GUILayout.BeginArea(rect);
                scroll = GUILayout.BeginScrollView(scroll, false, false, GUILayout.Width(width), GUILayout.Height(height));
                foreach (var (type, log) in logQueue)
                {
                    var c = GUI.color;
                    GUI.color = type switch
                    {
                        LogType.Error or LogType.Exception => Color.red,
                        LogType.Warning => Color.yellow,
                        _ => Color.white
                    };
                    GUILayout.Label(log);
                    GUI.color = c;
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

        }

    }

}
