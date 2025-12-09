using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Core
{

    partial class Runtime
    {

        /// <summary>Helper script hosted in DontDestroyOnLoad.</summary>
        [AddComponentMenu("")]
        internal class ASM : MonoBehaviour
        { }

        internal UnityEngine.SceneManagement.Scene dontDestroyOnLoadScene => helper ? helper.scene : default;
        bool hasDontDestroyOnLoadScene;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void UnsetHasDontDestroyOnLoadScene() =>
            SceneManager.runtime.hasDontDestroyOnLoadScene = false;
#endif

        GameObject m_helper;
        GameObject helper
        {
            get
            {

                if (!Application.isPlaying)
                    return null;

                if (!m_helper && !hasDontDestroyOnLoadScene)
                {

                    var script = Object.FindFirstObjectByType<ASM>();
                    if (script)
                        m_helper = script.gameObject;
                    else
                    {
                        m_helper = new GameObject("ASM helper");
                        _ = m_helper.AddComponent<ASM>();
                        hasDontDestroyOnLoadScene = true;
                    }

                    Object.DontDestroyOnLoad(m_helper);

                }

                return m_helper;

            }
        }

        Scene m_dontDestroyOnLoadScene;

        /// <summary>Gets the dontDestroyOnLoad scene.</summary>
        /// <remarks>Returns <see langword="null"/> outside of play mode.</remarks>
        public Scene dontDestroyOnLoad
        {
            get
            {

                if (!Application.isPlaying)
                    return null;

                if (!m_dontDestroyOnLoadScene)
                {
                    m_dontDestroyOnLoadScene = ScriptableObject.CreateInstance<Scene>();
                    ((Object)m_dontDestroyOnLoadScene).name = "DontDestroyOnLoad";
                }

                if (m_dontDestroyOnLoadScene.internalScene?.handle != dontDestroyOnLoadScene.handle)
                    m_dontDestroyOnLoadScene.internalScene = dontDestroyOnLoadScene;

                return m_dontDestroyOnLoadScene;

            }
        }

        /// <inheritdoc cref="AddToDontDestroyOnLoad{T}(out T)"/>
        internal bool AddToDontDestroyOnLoad<T>() where T : Component =>
            AddToDontDestroyOnLoad<T>(out _);

        /// <summary>Adds the component to the 'Advanced Scene Manager' gameobject in DontDestroyOnLoad.</summary>
        /// <remarks>Returns <see langword="false"/> outside of play-mode.</remarks>
        internal bool AddToDontDestroyOnLoad<T>(out T component) where T : Component
        {

            component = null;

            if (helper && helper.gameObject)
            {
                component = helper.gameObject.AddComponent<T>();
                return true;
            }
            else
                Debug.LogError("Cannot access DontDestroyOnLoad outside of play mode.");

            return false;

        }

        /// <summary>Adds the component to a new gameobject in DontDestroyOnLoad.</summary>
        /// <remarks>Returns <see langword="false"/> outside of play-mode.</remarks>
        internal bool AddToDontDestroyOnLoad<T>(out T component, out GameObject obj) where T : Component
        {

            obj = null;
            component = null;
            if (Application.isPlaying)
            {
                obj = new GameObject(typeof(T).Name);
                Object.DontDestroyOnLoad(obj);
                component = obj.AddComponent<T>();
                return true;
            }
            else
                Debug.LogError("Cannot access DontDestroyOnLoad outside of play mode.");

            return false;

        }

    }

}
