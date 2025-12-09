#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    partial class BuildUtility
    {

        /// <summary>Occurs before build.</summary>
        public static event Action<BuildReport> preBuild;

        /// <summary>Occurs after build.</summary>
        public static event Action<PostBuildEventArgs> postBuild;

        /// <summary>Represents a single logged message during build.</summary>
        public record LogEntry(string condition, string stacktrace);

        /// <summary>Represents a post build summary.</summary>
        public record PostBuildEventArgs(BuildReport report, LogEntry[] warning, LogEntry[] error);

        /// <summary>Bridges Unity build pipeline events to ASM build events.</summary>
        class BuildEvents : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {

            public int callbackOrder => int.MinValue;

            public void OnPreprocessBuild(BuildReport e)
            {
                SetupListener();
                preBuild?.Invoke(e);
            }

            public void OnPostprocessBuild(BuildReport e)
            {
                StopListener(out var warnings, out var errors);
                postBuild?.Invoke(new(e, warnings, errors));
            }

            public void OnAfterASMBuild(BuildReport report)
            {
                if (hasListener)
                    OnPostprocessBuild(report);
            }

            #region Log listener

            bool hasListener;
            readonly List<LogEntry> warnings = new();
            readonly List<LogEntry> errors = new();

            void SetupListener()
            {
                hasListener = true;
                Application.logMessageReceived += LogMessageReceived;
            }

            void StopListener(out LogEntry[] warnings, out LogEntry[] errors)
            {

                hasListener = false;
                Application.logMessageReceived -= LogMessageReceived;

                warnings = this.warnings.ToArray();
                errors = this.errors.ToArray();

                this.warnings.Clear();
                this.errors.Clear();

            }

            void LogMessageReceived(string condition, string stacktrace, LogType type)
            {
                if (type == LogType.Warning)
                    warnings.Add(new(condition, stacktrace));
                else if (type is LogType.Error or LogType.Exception)
                    errors.Add(new(condition, stacktrace));
            }

            #endregion

        }

    }

}
#endif
