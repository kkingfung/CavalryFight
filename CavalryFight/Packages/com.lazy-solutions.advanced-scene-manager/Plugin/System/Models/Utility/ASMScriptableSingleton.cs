using System.Reflection;
using UnityEngine;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using AdvancedSceneManager.Editor.UI;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Callbacks.Events.Editor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.MPE;
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    #region Build

#if UNITY_EDITOR

    class ASMScriptableSingletonBuildStep : IPreprocessBuildWithReport
    {

        public const string Folder = "Assets/ASMBuild";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {

            // Force-load all ASMScriptableSingleton<> instances
            var singletonTypes = TypeCache.GetTypesDerivedFrom(typeof(IASMScriptableSingleton));
            foreach (var type in singletonTypes)
            {
                if (type.IsAbstract || type.ContainsGenericParameters)
                    continue;

                var instanceProp = type.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                instanceProp?.GetValue(null); // Force-load instance
            }

            // Now that they're loaded, FindObjectsOfTypeAll should find them
            var allSingletons = Resources
                .FindObjectsOfTypeAll<ScriptableObject>()
                .Where(obj =>
                    obj.GetType().IsSubclassOfRawGeneric(typeof(ASMScriptableSingleton<>)) &&
                    obj.GetType().GetCustomAttribute<ASMFilePathAttribute>() != null &&
                    !AssetDatabase.Contains(obj) &&
                    !((IASMScriptableSingleton)obj).editorOnly);

            foreach (var obj in allSingletons)
                Move(obj);

        }

        static void Move(ScriptableObject obj)
        {

            if (!obj)
                return;

            if (AssetDatabase.Contains(obj))
                return;

            var resourcesPath = obj.GetType().GetCustomAttribute<ASMFilePathAttribute>().path;
            if (Application.isBatchMode)
                Debug.Log($"#UCB Preparing '{Path.GetFileName(resourcesPath)}.asset' for build.");
            Log.Info($"Preparing '{Path.GetFileName(resourcesPath)}.asset' for build.");

            var path = $"{Folder}/Resources/{resourcesPath}";

            ((IASMScriptableSingleton)obj).SaveNow();
            obj.hideFlags = HideFlags.None;
            var s = Directory.GetParent(path).FullName.ConvertToUnixPath();

            AssetDatabaseUtility.CreateFolder(s);
            AssetDatabase.CreateAsset(obj, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        }

        public static void Cleanup()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                Log.Info("Cleaning up " + Folder);
                AssetDatabase.DeleteAsset(Folder);
            }
        }

    }

#endif

    #endregion
    #region FilePath

    /// <summary>A <see cref="FilePathAttribute"/> that supports build.</summary>
    public class ASMFilePathAttribute
#if UNITY_EDITOR
        : FilePathAttribute
#else
        : System.Attribute
#endif
    {

        /// <summary>The path to the associated <see cref="ScriptableSingleton{T}"/>.</summary>
        public string path { get; }

        /// <inheritdoc />
        public ASMFilePathAttribute(string relativePath)
#if UNITY_EDITOR
            : base(relativePath, Location.ProjectFolder)
#endif
        {
            path = relativePath;
        }

    }

    #endregion
    #region ScriptableSingleton

    interface IASMScriptableSingleton
    {
        bool editorOnly { get; }
        void SaveNow();
    }

    /// <summary>A <see cref="ScriptableSingleton{T}"/> that supports build.</summary>
    public abstract class ASMScriptableSingleton<T>
#if UNITY_EDITOR
        : ScriptableSingleton<T>, INotifyPropertyChanged, IASMScriptableSingleton
#else
        : ScriptableObject, INotifyPropertyChanged, IASMScriptableSingleton
#endif
        where T : ASMScriptableSingleton<T>
    {

        #region Build step

        /// <summary>Specifies that build support will not be applied to this <see cref="ScriptableSingleton{T}"/>.</summary>
        public virtual bool editorOnly { get; }

        #endregion
        #region Instance

#if !UNITY_EDITOR

            public static T instance => GetInstance();

            static T m_instance;
            static T GetInstance()
            {

                if (!m_instance)
                    m_instance = Resources.Load<T>(typeof(T).GetCustomAttribute<ASMFilePathAttribute>().path.Replace(".asset", ""));

                return m_instance;

            }

#endif

        #endregion
        #region SerializedObject

#if UNITY_EDITOR

        SerializedObject m_serializedObject;

        /// <summary>Gets a cached <see cref="SerializedObject"/> for this <see cref="ScriptableSingleton{T}"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SerializedObject serializedObject => m_serializedObject ??= new(this);

#endif

        #endregion
        #region Save

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new(propertyName));

#if UNITY_EDITOR

        /// <inheritdoc />
        public virtual void OnValidate()
        {

            if (EditorApplication.isUpdating || ProcessService.level != ProcessLevel.Main)
                return;

            Save();
            OnPropertyChanged("");
            SceneManager.events.InvokeCallback<ASMSettingsChangedEvent>();
            SceneImportUtility.Notify();

        }

#endif

        /// <summary>Saves the singleton to disk, with a debounce. See also <see cref="SaveNow"/>.</summary>
        /// <remarks>Can be called outside of editor, but has no effect.</remarks>
        public virtual void Save()
        {
#if UNITY_EDITOR
            //Debounces save for a frame
            EditorApplication.delayCall -= SaveNow;
            EditorApplication.delayCall += SaveNow;
#endif
        }

        /// <summary>Saves the singleton to disk.</summary>
        /// <remarks>Can be called outside of editor, but has no effect.</remarks>
        public virtual void SaveNow()
        {
#if UNITY_EDITOR

            //Prevents errors in profiler, when hosted as external process
            if (ProcessService.level != ProcessLevel.Main)
                return;

            if (!this)
                return;

            if (!EditorUtility.IsPersistent(this))
            {
                //Save using special ScriptableSingleton method for initial create
                base.Save(saveAsText: true);
            }
            else
            {
                //Beyond initial create, we can use normal asset database methods. Prevents random errors, that would occur when only using base.Save().
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
#endif
        }

        /// <inheritdoc />
        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsDirty(this))
                SaveNow();
#endif
        }

        #endregion

    }

    #endregion

}