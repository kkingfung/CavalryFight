using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using AdvancedSceneManager.Models.Interfaces;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.MPE;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Callbacks.Events.Editor;
#endif

namespace AdvancedSceneManager.Models.Internal
{

    /// <summary>A base class for <see cref="Profile"/>, <see cref="SceneCollection"/> and <see cref="Scene"/>.</summary>
    public abstract class ASMModelBase : ScriptableObject, IASMModel
    {

        [SerializeField] internal string m_id = GenerateID();

        internal bool hasID => !string.IsNullOrEmpty(m_id);

        /// <summary>Generate id.</summary>
        public static string GenerateID()
        {
            return Path.GetRandomFileName();
        }

        /// <summary>Gets the id of this <see cref="ASMModelBase"/>.</summary>
        public string id => m_id;

        internal void SetID(string id) => m_id = id;

        #region Save

        /// <summary>Called when a property changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Called when object is destroyed.</summary>
        public event Action onDestroy;

        /// <summary>Invoke <see cref="PropertyChanged"/>.</summary>
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
#if UNITY_EDITOR
            SceneManager.events.InvokeCallback(new ModelPropertyChangedEvent(this, propertyName));
#endif
        }

        /// <summary>Called when this object is destroyed.</summary>
        public virtual void OnDestroy() =>
            onDestroy?.Invoke();

#if UNITY_EDITOR

        /// <inheritdoc />
        public virtual void OnValidate()
        {

            if (EditorApplication.isUpdating || ProcessService.level != ProcessLevel.Main)
                return;

            //Save();
            OnPropertyChanged("");
            SceneImportUtility.Notify();

        }

#endif

        internal bool suppressSave { get; set; }

        /// <summary>Saves the singleton to disk after a delay.</summary>
        /// <remarks>
        /// <para>Can be called outside of editor, but has no effect.</para>
        /// <para>No effect if <see cref="suppressSave"/> is <see langword="true"/>, <see cref="SaveNow()"/> can still be used.</para>
        /// </remarks>
        public virtual void Save()
        {
#if UNITY_EDITOR

            if (!this)
                return;

            if (suppressSave)
                return;

            EditorApplication.delayCall -= SaveNow;
            EditorApplication.delayCall += SaveNow;

#endif
        }

        /// <summary>Saves the singleton to disk.</summary>
        /// <remarks>Can be called outside of editor, but has no effect.</remarks>
        public void SaveNow() => SaveNow(setDirty: true);

        /// <summary>Saves the singleton to disk.</summary>
        /// <remarks>Can be called outside of editor, but has no effect.</remarks>
        public void SaveNow(bool setDirty = true)
        {
#if UNITY_EDITOR

            if (ProcessService.level != ProcessLevel.Main)
                return;

            if (!this)
                return;

            if (!EditorUtility.IsPersistent(this))
                return;

            if (setDirty)
                EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);

#endif
        }

        /// <inheritdoc />
        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsDirty(this))
                SaveNow(false);
#endif
        }

        #endregion
        #region Find

        /// <summary>Determines whether the specified query matches this model.</summary>
        public virtual bool IsMatch(string q) =>
            !string.IsNullOrEmpty(q) && (IsNameMatch(q) || IsIDMatch(q));

        /// <summary>Gets if <paramref name="q"/> matches <see cref="name"/>.</summary>
        protected bool IsNameMatch(string q) =>
            string.Equals(q, name, StringComparison.OrdinalIgnoreCase);

        /// <summary>Gets if <paramref name="q"/> matches <see cref="id"/>.</summary>
        protected bool IsIDMatch(string q) =>
             string.Equals(q, id, StringComparison.Ordinal);

        #endregion
        #region Name

        //Don't allow renaming from UnityEvent
        /// <summary>Gets the name of this model.</summary>
        public new string name => this ? base.name : "(null)";

        internal virtual void Rename(string newName)
        {
#if UNITY_EDITOR

            if (name == newName)
                return;

            var oldName = name;

            if (AssetDatabase.IsMainAsset(this))
            {
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), newName);
                base.name = newName;
            }
            else
            {
                base.name = newName;
                SaveNow();
            }

            SceneManager.events.InvokeCallbackSync(new ASMModelRenamedEvent(this, oldName, newName));

#endif
        }

        #endregion
        #region Create

        /// <summary>Creates a profile. Throws if name is invalid.</summary>
        protected static T CreateInternal<T>(string name) where T : ASMModelBase, new()
        {

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(nameof(name), "Name cannot be whitespace.");

            if (Path.GetInvalidFileNameChars().Any(name.Contains))
                throw new ArgumentException(nameof(name), "Name cannot contain invalid path chars.");

            var id = Path.GetRandomFileName();

            if (SceneManager.assetImport.IsIDTaken<T>(id))
                throw new InvalidOperationException("The generated id already exists.");

            //Windows / .net does not have an issue with paths over 260 chars anymore,
            //but unity still does, and it does not handle it gracefully, so let's have a check for that too
            //No clue how to make this cross-platform since we cannot even get the value on windows, so lets just hardcode it for now
            //This should be removed in the future when unity does handle it
            if (Path.GetFullPath(SceneManager.assetImport.GetPath<T>(id, name)).Length > 260)
                throw new PathTooLongException("Path cannot exceed 260 characters in length.");

            var model = CreateInstance<T>();
            model.m_id = id;
            ((ScriptableObject)model).name = name;

            return model;

#else
            throw new InvalidOperationException("Cannot create ASM model instance outside of editor!");
#endif

        }

        #endregion
        #region Hide

        /// <summary>Specifies if this ASM asset is hidden. If it is, it won't show up in UI, and won't be enumerated when using <see cref="SceneManager.assets"/>.</summary>
        public bool isHidden => m_isHidden;

        /// <summary>Prevents ASM from displaying a profile in the UI.</summary>
        [SerializeField] internal bool m_isHidden;

        /// <summary>Gets if this ASM asset is located under <c>Packages/com.asm.tests/</c></summary>
#if UNITY_EDITOR
        internal bool IsTestAsset => AssetDatabase.GetAssetPath(this).Contains("Packages/com.asm.tests/");
#else
        internal bool IsTestAsset => false;
#endif

        #endregion

    }

}
