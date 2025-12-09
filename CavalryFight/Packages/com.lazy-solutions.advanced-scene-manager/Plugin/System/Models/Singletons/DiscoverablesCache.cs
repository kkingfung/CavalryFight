using UnityEngine;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;

#if UNITY_EDITOR
#endif

namespace AdvancedSceneManager.Models
{
    [ASMFilePath("ProjectSettings/AdvancedSceneManager.DiscoverablesCache.asset")]
    class DiscoverablesCache : ASMScriptableSingleton<DiscoverablesCache>
    {
        [SerializeField] internal List<string> m_cachedDiscoverables = new();
    }

}
