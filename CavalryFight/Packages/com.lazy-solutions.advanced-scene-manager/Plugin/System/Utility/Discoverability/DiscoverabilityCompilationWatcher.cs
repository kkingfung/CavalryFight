#if UNITY_EDITOR
using AdvancedSceneManager.Callbacks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace AdvancedSceneManager.Utility.Discoverability
{

    [OnLoad]
    static class DiscoverabilityCompilationWatcher
    {

        static List<string> assembliesToInvalidate
        {
            get => SessionStateUtility.Get(string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            set => SessionStateUtility.Set(value is not null ? string.Join("\n", value) : null);
        }

        static DiscoverabilityCompilationWatcher()
        {

            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            var paths = assembliesToInvalidate.Distinct().ToList();
            assembliesToInvalidate = null;

            var assemblies = DiscoverabilityUtility.GetAssemblies();
            var anyChanged = paths.Any(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var targetAssembly = assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

                return targetAssembly != null && DiscoverabilityUtility.IsValidAssembly(targetAssembly);

            });

            if (anyChanged)
                DiscoverabilityUtility.InvalidateCache();

        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {

            // Only proceed if compilation succeeded
            if (messages.Any(m => m.type == CompilerMessageType.Error))
                return;

            var list = assembliesToInvalidate;
            list!.Add(assemblyPath);
            assembliesToInvalidate = list;

        }

    }

}
#endif
