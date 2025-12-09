#if ASM_DEV && UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    static class Tools
    {

        [MenuItem("ASM/Recompile...")]
        static void Recompile() =>
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

        [MenuItem("ASM/View Unity API timeline...")]
        static void ApiTimeline() =>
            Application.OpenURL("https://ngtools.tech/unityapitimeline.php");

    }

}
#endif
