using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility.CrossSceneReferences;
using AdvancedSceneManager.Models.Interfaces;

using unityScene = UnityEngine.SceneManagement.Scene;
using Object = UnityEngine.Object;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a scene. This scene can be dragged dropped, and is used by ASM to perform operations on the wrapped unity scene.</summary>
    /// <remarks>A scene can be imported in the ASM window (via notification / popup), or by using <see cref="AdvancedSceneManager.Editor.Utility.SceneImportUtility"/>.</remarks>
    public partial class Scene : ASMModel,
        ISerializationCallbackReceiver,
        Scene.IEquality,
        IScene,
        IOpenableScene,
        ILockable,
        IFindable
    {

        #region ISerializationCallbackReceiver

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            PersistOpenedBy();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UnpersistOpenedBy();
        }

        void PersistOpenedBy()
        {
#if UNITY_EDITOR
            SessionStateUtility.Set(openedBy?.id, id + "+" + nameof(openedBy));
#endif
        }

        void UnpersistOpenedBy()
        {
#if UNITY_EDITOR
            SceneManager.OnInitialized(() =>
            {

                EditorApplication.delayCall += () =>
                {
                    var collectionID = SessionStateUtility.Get(string.Empty, id + "+" + nameof(openedBy));
                    if (!string.IsNullOrEmpty(collectionID))
                    {
                        var collection = SceneManager.profile.allCollections.FirstOrDefault(c => c.id == collectionID);
                        if (collection != null)
                            openedBy = collection;
                    }
                };

            });
#endif
        }

        #endregion

        #region IScene

        [Header("Scene")]
        [SerializeField] private string m_path;
        [SerializeField] internal string m_sceneAssetGUID;
        [SerializeField] private Object m_sceneAsset;

#if UNITY_EDITOR

        /// <summary>Gets the path of this <see cref="Scene"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public string asmPath => AssetDatabase.GetAssetPath(this);

        /// <summary>Gets the associated <see cref="SceneAsset"/>.</summary>
        /// <remarks>Only available in the editor.</remarks>
        public SceneAsset sceneAsset
        {
            get => m_sceneAsset as SceneAsset;
            internal set => m_sceneAsset = value;
        }

        /// <summary>Gets if <see cref="m_sceneAsset"/> has a value.</summary>
        /// <remarks>Only available in the editor.</remarks>
        public bool hasSceneAsset => m_sceneAsset;

#endif

        /// <summary>Gets the asset id of <see cref="sceneAsset"/>.</summary>
        public string sceneAssetGUID
        {
            get
            {

#if UNITY_EDITOR

#if ADDRESSABLES
                if (isAddressable && GUID.TryParse(m_assetReference?.AssetGUID, out _))
                    return m_assetReference.AssetGUID;
#endif

                var guid = AssetDatabase.GUIDFromAssetPath(m_path).ToString();

                if (guid != m_sceneAssetGUID)
                {
                    m_sceneAssetGUID = guid;
                    Save();
                }
#endif

                return m_sceneAssetGUID;

            }
        }

        /// <summary>Gets the path of the associated <see cref="SceneAsset"/>.</summary>
        public string path
        {
            get => m_path ?? "";
            internal set { m_path = value; OnPropertyChanged(); }
        }

        #endregion

        #region IOpenableScene

        /// <summary>Gets whatever the scene is open.</summary>
        public override bool isOpen
        {
            get
            {

#if ADDRESSABLES
                if (isAddressable && m_assetReference != null && m_assetReference.OperationHandle.IsValid())
                    return m_assetReference.OperationHandle.Status == AsyncOperationStatus.Succeeded &&
                           m_sceneInstance.HasValue && m_sceneInstance.Value.Scene.IsValid();
#endif

                return SceneManager.runtime.IsTracked(this);

            }
        }

        /// <inheritdoc cref="Runtime.IsQueued(Scene)"/>
        public override bool isQueued => SceneManager.runtime.IsQueued(this);

        /// <summary>Opens this scene.</summary>
        public override SceneOperation Open() => !isOpen ? SceneManager.runtime.Open(this) : Error($"The scene '{name}' cannot be opened, as it is already open.");

        /// <summary>Reopens this scene.</summary>
        public override SceneOperation Reopen() => SceneManager.runtime.Reopen(this);

        /// <summary>Toggles this scene open or closed.</summary>
        public override SceneOperation ToggleOpen() => SceneManager.runtime.ToggleOpen(this);

        /// <summary>Closes this scene.</summary>
        public override SceneOperation Close() => isOpen ? SceneManager.runtime.Close(this) : Error($"The scene '{name}' cannot be closed, as it is not open.");


        /// <summary>Opens this scene with the specified <paramref name="loadingScene"/>.</summary>
        public SceneOperation OpenWithLoadingScreen(Scene loadingScene) => !isOpen ? SceneManager.runtime.OpenWithLoadingScreen(this, loadingScene) : Error($"The scene '{name}' cannot be opened, as it is already open.");

        /// <inheritdoc cref="OpenWithLoadingScreen(Scene)"/>
        public void _OpenWithLoadingScreen(Scene loadingScene) => SpamCheck.EventMethods.Execute(() => OpenWithLoadingScreen(loadingScene));

        /// <summary>Closes this scene with the specified <paramref name="loadingScene"/>.</summary>
        public SceneOperation CloseWithLoadingScreen(Scene loadingScene) => isOpen ? SceneManager.runtime.CloseWithLoadingScreen(this, loadingScene) : Error($"The scene '{name}' cannot be closed, as it is not open.");
        /// <inheritdoc cref="CloseWithLoadingScreen(Scene)"/>
        public void _CloseWithLoadingScreen(Scene loadingScene) => SpamCheck.EventMethods.Execute(() => CloseWithLoadingScreen(loadingScene));

        /// <summary>Opens this scene and activates it.</summary>
        public SceneOperation OpenAndActivate() => !isOpen ? SceneManager.runtime.OpenAndActivate(this) : Error($"The scene '{name}' cannot be opened, as it is already open.");
        /// <inheritdoc cref="OpenAndActivate"/>
        public void _OpenAndActivate() => SpamCheck.EventMethods.Execute(() => OpenAndActivate());

        /// <summary>Activates this scene.</summary>
        public void Activate() => SceneManager.runtime.Activate(this);
        /// <inheritdoc cref="Activate"/>
        public void _Activate() => Activate();

        SceneOperation Error(string message)
        {
            Debug.LogError(message);
            return SceneOperation.done;
        }

        #endregion

        #region IPreloadable

        /// <summary>Gets whatever the scene is preloaded.</summary>
        public override bool isPreloaded =>
            state == SceneState.Preloaded;

        /// <summary>Preloads this scene.</summary>
        public override SceneOperation Preload() => Preload(onPreloaded: null);

        /// <summary>Preloads this scene.</summary>
        public SceneOperation Preload(Action onPreloaded = null) => !isOpenInHierarchy ? SceneManager.runtime.Preload(this, onPreloaded) : Error($"The scene '{name}' cannot be preloaded, as it is already open.");

        #endregion

        #region IFindable

        /// <summary>Gets: <code>t:AdvancedSceneManager.Models.Scene</code> Used in <see cref="AssetDatabase.FindAssets(string)"/>.</summary>
        public readonly static string AssetSearchString = "t:" + typeof(Scene).FullName;

        /// <summary>Gets if <paramref name="q"/> matches <see cref="ASMModelBase.name"/>, <see cref="ASMModelBase.id"/>, <see cref="path"/>.</summary>
        public override bool IsMatch(string q) =>
            base.IsMatch(q) || IsPathMatch(q) || IsGUIDMatch(q);

        bool IsPathMatch(string q) =>
            q == path;

        bool IsGUIDMatch(string q) =>
            q == sceneAssetGUID;

        /// <inheritdoc cref="SceneUtility.Find(Func{Scene, bool})"/>
        public static IEnumerable<Scene> Find(Func<Scene, bool> predicate) =>
            SceneUtility.Find(predicate);

        /// <inheritdoc cref="SceneUtility.Find(string)"/>
        public static Scene Find(string q) =>
            SceneUtility.Find(q);

        /// <inheritdoc cref="SceneUtility.Find(string)"/>
        public static bool TryFind(string q, out Scene scene) =>
            scene = Find(q);

        /// <inheritdoc cref="SceneUtility.FindOpen(string)"/>
        public static IEnumerable<Scene> FindOpen(string q) =>
            SceneUtility.FindOpen(q);

        /// <inheritdoc cref="SceneUtility.FindOpen(Func{Scene, bool})"/>
        public static IEnumerable<Scene> FindOpen(Func<Scene, bool> predicate) =>
            SceneUtility.FindOpen(predicate);

        #endregion

        #region ILockable

        [Header("Locking")]
        [SerializeField] private bool m_isLocked;
        [SerializeField] private string m_lockMessage;

        /// <summary>Gets if this scene is locked.</summary>
        public bool isLocked
        {
            get => m_isLocked;
            set { m_isLocked = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the message to be displayed when unlocking this scene.</summary>
        public string lockMessage
        {
            get => m_lockMessage;
            set { m_lockMessage = value; OnPropertyChanged(); }
        }

        #endregion 

        #region Scene Loader

        [SerializeField] private string m_sceneLoader;

        /// <summary>Specifies what <see cref="SceneLoader"/> to use.</summary>
        public string sceneLoader
        {
            get => m_sceneLoader;
            set
            {
                var prevLoader = m_sceneLoader;
                m_sceneLoader = value;
                OnPropertyChanged();
                NotifyLoaderChanged(prevLoader, value);
            }
        }

        void NotifyLoaderChanged(string prevLoader, string newLoader)
        {

            if (string.IsNullOrEmpty(prevLoader)) prevLoader = typeof(RuntimeSceneLoader).AssemblyQualifiedName;
            if (string.IsNullOrEmpty(newLoader)) newLoader = typeof(RuntimeSceneLoader).AssemblyQualifiedName;

            SceneManager.runtime.InvokeSceneLoaderToggled(this, Type.GetType(prevLoader), Type.GetType(newLoader));
        }

        /// <summary>Specifies the scene loader to use for this scene.</summary>
        /// <remarks>If the specified scene loader is not registered when scene is opened, then ASM will fallback to other scene loaders, if any (normal ASM functionality is used if not).</remarks>
        public void SetSceneLoader<T>() where T : SceneLoader =>
            sceneLoader = SceneLoader.GetKey<T>();

        /// <summary>Gets the scene loader specified for this scene. <see langword="null"/> if none set.</summary>
        public SceneLoader GetSceneLoader() =>
            SceneManager.runtime.GetSceneLoader(sceneLoader);

        /// <summary>Gets the effective, contextual, scene loader for this scene. <see langword="null"/> if none found (this means normal ASM loader will be used).</summary>
        public SceneLoader GetEffectiveSceneLoader() =>
            SceneManager.runtime.GetLoaderForScene(this);

        /// <summary>Gets whatever <typeparamref name="T"/> is enabled for this scene.</summary>
        public bool UsesSceneLoader<T>() where T : SceneLoader =>
            SceneManager.runtime.GetSceneLoaderType(sceneLoader) == typeof(T);

        /// <summary>Clears custom scene loader for this scene. This means normal ASM functionality will be used.</summary>
        public void ClearSceneLoader() =>
            sceneLoader = null;

        /// <summary>Find scenes by enabled scene loader.</summary>
        public static IEnumerable<Scene> Find<TSceneLoader>() where TSceneLoader : SceneLoader =>
            SceneManager.assets.scenes.Where(s => s.UsesSceneLoader<TSceneLoader>());

        #endregion

        #region Load priority

        [SerializeField] private LoadPriority m_loadPriority = LoadPriority.Auto;

        /// <summary>Specifies the <see cref="LoadPriority"/> to use when opening this scene.</summary>
        public LoadPriority loadPriority
        {
            get => m_loadPriority;
            set { m_loadPriority = value; OnPropertyChanged(); }
        }

        #endregion

        #region Special scene

        [Header("Special scene flags")]
        [SerializeField] private bool m_isLoadingScreen;
        [SerializeField] private bool m_isSplashScreen;
        [SerializeField] internal bool m_isDefaultASMScene;

        /// <summary>Gets if this scene is a loading screen.</summary>
        /// <remarks>
        /// <para>Automatically updated.</para>
        /// <para>If this is <see langword="false"/> for an actual loading screen, please make sure scene contains a <see cref="AdvancedSceneManager.Loading.LoadingScreen"/> script.</para>
        /// <para>Scene might sometimes have to be re-saved for this flag to appear.</para>
        /// </remarks>
        public bool isLoadingScreen
        {
            get => m_isLoadingScreen;
            internal set => m_isLoadingScreen = value;
        }

        /// <summary>Gets if this scene is a splash screen.</summary>
        /// <remarks>
        /// <para>Automatically updated.</para>
        /// <para>If this is <see langword="false"/> for an actual splash screen screen, please make sure scene contains a <see cref="AdvancedSceneManager.Loading.SplashScreen"/> script.</para>
        /// <para>Scene might sometimes have to be re-saved for this flag to appear.</para>
        /// </remarks>
        public bool isSplashScreen
        {
            get => m_isSplashScreen;
            internal set => m_isSplashScreen = value;
        }

        /// <summary>Gets if this is a 'special' scene.</summary>
        /// <remarks>A scene is special if any of the following is <see langword="true"/>: <see cref="isSplashScreen"/>, <see cref="isLoadingScreen"/> or <see cref="isDontDestroyOnLoad"/>.</remarks>
        public bool isSpecial =>
            isSplashScreen || isLoadingScreen || isDontDestroyOnLoad;

        /// <summary>Gets if this is a default ASM scene.</summary>
        public bool isDefaultASMScene => m_isDefaultASMScene;

#if UNITY_EDITOR

        internal bool CheckIfSpecialScene()
        {

            if (File.Exists(path))
            {

                var str = File.ReadAllText(path);
                var isSplashScreen = str.Contains("isSplashScreen: 1");
                var isLoadingScreen = str.Contains("isLoadingScreen: 1");

                if (this.isSplashScreen != isSplashScreen || this.isLoadingScreen != isLoadingScreen)
                {
                    this.isSplashScreen = isSplashScreen;
                    this.isLoadingScreen = isLoadingScreen;
                    return true;
                }

            }

            return false;

        }

#endif

        #endregion

        #region Persistent properties

        [Header("Persistent")]
        [SerializeField] private bool m_keepOpenWhenCollectionsClose;
        [SerializeField] private bool m_keepOpenWhenNewCollectionWouldReopen = true;

        /// <summary>Specifies whatever this scene will remain open when collections close.</summary>
        /// <remarks>You'll have to close it manually, if needed.</remarks>
        public bool keepOpenWhenCollectionsClose
        {
            get => m_keepOpenWhenCollectionsClose;
            set { m_keepOpenWhenCollectionsClose = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever this will remain open when a newly opened collection would have reopened it.</summary>
        /// <remarks>You'll have to close it manually, if needed.</remarks>
        public bool keepOpenWhenNewCollectionWouldReopen
        {
            get => m_keepOpenWhenNewCollectionWouldReopen;
            set { m_keepOpenWhenNewCollectionWouldReopen = value; OnPropertyChanged(); }
        }

        /// <summary>Gets whatever this scene will close normally after a collection closes.</summary>
        public bool isNonPersistant => !keepOpenWhenCollectionsClose && !keepOpenWhenNewCollectionWouldReopen;

        #endregion

        #region Startup / Auto open

        [Header("Auto open")]
        [SerializeField] private bool m_openOnStartup;
        [SerializeField] private bool m_openOnPlayMode;
        [SerializeField] private EditorPersistentOption m_autoOpenInEditor;
        [SerializeField] internal List<Scene> m_autoOpenInEditorScenes = new();

        /// <summary>Specifies whatever this scene should be opened on startup.</summary>
        /// <remarks>Only effective when scene added to <see cref="Profile.standaloneScenes"/>.</remarks>
        public bool openOnStartup
        {
            get => m_openOnStartup;
            set { m_openOnStartup = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever this scene should be opened when entering playmode.</summary>
        /// <remarks>Only effective when scene added to <see cref="Profile.standaloneScenes"/>.</remarks>
        public bool openOnPlayMode
        {
            get => m_openOnPlayMode;
            set { m_openOnPlayMode = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever this scene should be opened automatically outside of play-mode.</summary>
        public EditorPersistentOption autoOpenInEditor
        {
            get => m_autoOpenInEditor;
            set { m_autoOpenInEditor = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the scenes that should trigger this scene to open when <see cref="autoOpenInEditor"/> is set to <see cref="EditorPersistentOption.WhenAnyOfTheFollowingScenesAreOpened"/>.</summary>
        public List<Scene> autoOpenInEditorScenes => m_autoOpenInEditorScenes;

        #endregion

        #region Cross-scene references

        [Header("Cross-scene references")]
        [SerializeField] private List<CrossSceneReference> m_crossSceneReferences = new();

        /// <summary>Enumerates the cross-scene references defined on this scene.</summary>
        public IEnumerable<CrossSceneReference> crossSceneReferences
        {
            get => m_crossSceneReferences;
            internal set
            {
                m_crossSceneReferences = value?.ToList() ?? new();
                Save();
            }
        }

#if UNITY_EDITOR

        /// <summary>Adds a cross-scene reference for this scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void AddCrossSceneReference(CrossSceneReference reference)
        {

            if (RemoveCrossSceneReference(reference, out var i))
                m_crossSceneReferences.Insert(i, reference);
            else
                m_crossSceneReferences.Add(reference);

            Save();

        }

        /// <summary>Removes a cross-scene reference for this scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void RemoveCrossSceneReference(CrossSceneReference reference)
        {
            RemoveCrossSceneReference(reference, out _);
            if (internalScene.HasValue)
            {
                CrossSceneReferenceUtility.ResetScene(internalScene.Value);
                CrossSceneReferenceUtility.ResolveScene(internalScene.Value);
            }
            Save();
        }

        bool RemoveCrossSceneReference(CrossSceneReference reference, out int index)
        {

            index = m_crossSceneReferences.FindIndex(r => r.id == reference.id);
            if (index == -1)
                return false;
            else
            {
                m_crossSceneReferences.RemoveAt(index);
                return true;
            }

        }

#endif

        #endregion

        #region Input bindings

        [SerializeField] internal InputBinding[] m_inputBindings = Array.Empty<InputBinding>();
        [SerializeField] private Scene m_inputBindingsLoadingScene;
        [SerializeField] private Scene[] m_ignoreInputBindingsForScenes;

        /// <summary>Gets or sets the input bindings for this scene.</summary>
        /// <remarks>No effect for non-standalone scenes.</remarks>
        public InputBinding[] inputBindings
        {
            get => m_inputBindings;
            set { m_inputBindings = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies loading screen to use when opening or closing this scene using a input binding.</summary>
        public Scene inputBindingsLoadingScene
        {
            get => m_inputBindingsLoadingScene;
            set { m_inputBindingsLoadingScene = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies scenes where input bindings should be ignored while open (for this scene).</summary>
        public Scene[] ignoreInputBindingsForScenes
        {
            get => m_ignoreInputBindingsForScenes;
            set { m_ignoreInputBindingsForScenes = value; OnPropertyChanged(); }
        }

        #endregion

        #region Events

        [SerializeField] Events m_events = new();

        /// <summary>Gets the unity events for this scene.</summary>
        public Events events => m_events;

        /// <summary>A struct providing unity events.</summary>
        [Serializable]
        public struct Events
        {

            /// <summary>Occurs when this scene is opened.</summary>
            public UnityEvent<Scene> OnOpen;
            /// <summary>Occurs when this scene is closed.</summary>
            public UnityEvent<Scene> OnClose;
            /// <summary>Occurs when this scene is preloaded.</summary>
            public UnityEvent<Scene> OnPreload;
            /// <summary>Occurs when preload is finished for this scene.</summary>
            public UnityEvent<Scene> OnPreloadFinished;

            /// <summary>Occurs when a collection opened this scene.</summary>
            public UnityEvent<Scene, SceneCollection> OnCollectionOpened;
            /// <summary>Occurs when a collection closed this scene.</summary>
            public UnityEvent<Scene, SceneCollection> OnCollectionClosed;

            /// <summary>Occurs when this scene is opened or closed, provides the open status as a bool.</summary>
            public UnityEvent<bool> OnOpenChanged;

        }

        #endregion

        #region Netcode

#if NETCODE

        /// <summary>Gets or sets if this scene is enabled for netcode.</summary>
        public bool isNetcode
        {
            get => UsesSceneLoader<PackageSupport.Netcode.NetcodeSceneLoader>();
            set { if (value) SetSceneLoader<PackageSupport.Netcode.NetcodeSceneLoader>(); OnPropertyChanged(); }
        }

        /// <summary>Gets if this scene was synced using netcode.</summary>
        public bool isSynced { get; internal set; }

#endif

        #endregion

        #region Addressables

#if ADDRESSABLES

        /// <summary>Gets or sets if this scene is enabled for addressables.</summary>
        public bool isAddressable
        {
            get => UsesSceneLoader<PackageSupport.Addressables.AddressablesSceneLoader>();
            set { if (value) SetSceneLoader<PackageSupport.Addressables.AddressablesSceneLoader>(); OnPropertyChanged(); }
        }

        [SerializeField] internal AssetReference m_assetReference;
        [SerializeField] internal SceneInstance? m_sceneInstance;

        /// <summary>Gets the addressable address for this scene.</summary>
        public string address
        {
            get
            {
                if (Guid.TryParse(m_assetReference?.AssetGUID, out _))
                    return m_assetReference.AssetGUID;

                return isAddressable ? $"{name} ({id})" : null;
            }
        }

#endif

        #endregion

        #region Runtime

        /// <summary>Gets if this scene is currently active.</summary>
        public bool isActive =>
            SceneManager.runtime.activeScene == this;

        /// <summary>Gets whatever the scene is open in the hierarchy, this is <see langword="true"/> if scene is currently loading, if scene is preloaded, if scene is fully open.</summary>
        public bool isOpenInHierarchy =>
            SceneUtility.GetAllOpenUnityScenes().Any(s => s.path == path);

        /// <inheritdoc cref="Runtime.GetState(Scene)"/>
        public SceneState state =>
            SceneManager.runtime.GetState(this);

        /// <summary>Gets the <see cref="unityScene"/> that this scene is associated with.</summary>
        /// <remarks><see langword="null"/> if scene is not open.</remarks>
        public unityScene? internalScene { get; internal set; }

        /// <summary>Gets if this scene is opened as persistent.</summary>
        public bool isPersistent =>
            isOpen &&
            isDontDestroyOnLoad ||
            keepOpenWhenCollectionsClose ||
            (openedBy is SceneCollection c && c.openAsPersistent);

        internal ISceneCollection openedBy { get; set; }

        /// <summary>Gets if this scene is the dontDestroyOnLoad scene.</summary>
        public bool isDontDestroyOnLoad =>
            internalScene?.handle == SceneManager.runtime.dontDestroyOnLoadScene.handle;

        /// <summary>Gets if this scene is dynamic, it is not persisted to disk.</summary>
        public bool isDynamic =>
            internalScene?.IsValid() ?? false && string.IsNullOrWhiteSpace(internalScene.Value.path);

        /// <summary>Gets the root game objects in this <see cref="Scene"/>.</summary>
        /// <remarks>Only usable if scene is open.</remarks>
        public IEnumerable<GameObject> GetRootGameObjects() =>
            (internalScene?.IsValid() ?? false)
            ? internalScene.Value.GetRootGameObjects()
            : Array.Empty<GameObject>();

        /// <summary>Finds the object in the hierarchy of this <see cref="Scene"/>.</summary>
        /// <remarks>Only works if scene is loaded.</remarks>
        public T FindObject<T>() =>
            FindObjects<T>().FirstOrDefault();

        /// <inheritdoc cref="FindObject{T}()"/>
        public bool FindObject<T>(out T component) =>
            (component = FindObject<T>()) != null;

        /// <summary>Finds the objects in the hierarchy of this <see cref="Scene"/>.</summary>
        /// <remarks>Only works if scene is loaded.</remarks>
        public IEnumerable<T> FindObjects<T>()
        {
            foreach (var obj in GetRootGameObjects())
            {
                var components = obj.GetComponentsInChildren<T>();
                foreach (var component in components)
                    yield return component;
            }
        }

        #endregion

        #region Equality

        /// <inheritdoc />
        public interface IEquality : IEquatable<Scene>, IEquatable<unityScene?>
#if UNITY_EDITOR
    , IEquatable<SceneAsset>
#endif
        { }

        /// <inheritdoc />
        public override int GetHashCode() => id?.GetHashCode() ?? 0;

        /// <inheritdoc />
        public override bool Equals(object obj) => IsEqual(this, obj);
        /// <inheritdoc />
        public bool Equals(Scene other) => IsEqual(this, other);
        /// <inheritdoc />
        public bool Equals(UnityEngine.SceneManagement.Scene? other) => IsEqual(this, other);
#if UNITY_EDITOR
        /// <inheritdoc />
        public bool Equals(SceneAsset other) => IsEqual(this, other);
#endif

        // ---------------- Core equality logic ----------------

        /// <inheritdoc />
        public static bool IsEqual(object left, object right)
        {
            // Respect Unity null handling (destroyed objects count as null)
            if (left is UnityEngine.Object lObj && !lObj) left = null;
            if (right is UnityEngine.Object rObj && !rObj) right = null;

            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return GetPath(left, out var lPath) &&
                   GetPath(right, out var rPath) &&
                   lPath == rPath;
        }

        static bool GetPath(object obj, out string path)
        {
            path = obj switch
            {
                Scene scene when scene => scene.path,
                UnityEngine.SceneManagement.Scene uScene => uScene.path,
#if UNITY_EDITOR
                SceneAsset sceneAsset when sceneAsset => AssetDatabase.GetAssetPath(sceneAsset),
#endif
                _ => null
            };

            return !string.IsNullOrEmpty(path);
        }

        #endregion

        #region Implicit

        /// <inheritdoc />
        public static implicit operator string(Scene scene) => scene ? scene.path : string.Empty;

        /// <inheritdoc />
        public static implicit operator unityScene?(Scene scene) => scene ? scene.internalScene : default;
        /// <inheritdoc />
        public static implicit operator unityScene(Scene scene) => scene ? scene.internalScene ?? default : default;

        /// <inheritdoc />
        public static implicit operator Scene(unityScene scene) => scene.ASMScene();
        /// <inheritdoc />
        public static implicit operator Scene(unityScene? scene) => scene?.ASMScene();

#if UNITY_EDITOR

        /// <inheritdoc />
        public static implicit operator SceneAsset(Scene scene) => scene ? (SceneAsset)scene.sceneAsset : default;
        /// <inheritdoc />
        public static implicit operator Scene(SceneAsset scene) => scene ? scene.ASMScene() : default;

#endif

        #endregion

        #region ToString

        /// <summary>Gets the description for the in-game toolbar scene.</summary>
        public const string InGameToolbarDescription = "The in-game toolbar can help with diagnosing scene state during builds.";

        /// <summary>Gets the description for the default pause screen scene.</summary>
        public const string PauseScreenDescription = "The pause scene provides a default pause screen, for when your game does not yet have one.";

        /// <inheritdoc />
        public override string ToString()
        {

            var persistentIndicator = "";
            if (keepOpenWhenCollectionsClose)
                persistentIndicator = " (keepOpenWhenCollectionsClose)";
            else if (keepOpenWhenNewCollectionWouldReopen)
                persistentIndicator = " (keepOpenWhenNewCollectionWouldReopen)";

            return $"{name}{persistentIndicator}";

        }

        /// <summary>Gets the SceneField tooltip.</summary>
        public string GetTooltip()
        {

            var sb = new StringBuilder();

            GetPathInfo(sb);
            GetStartupInfo(sb);
            GetPersistentInfo(sb);
            GetDescriptionInfo(sb);
            GetHotkeyInfo(sb);
            GetSceneLoaderInfo(sb);

            return sb.ToString().Trim('\n');

        }

        void GetPathInfo(StringBuilder sb)
        {
            sb.AppendLine("\n<i><b>" + name + "</b></i>");
            sb.AppendLine("<i>" + path + "</i>");
        }

        void GetStartupInfo(StringBuilder sb)
        {

            sb.AppendLine("\n<b>Startup</b>");

            if (openOnStartup ||
                (SceneManager.profile && SceneManager.profile.startupScenes.Contains(this)) ||
                (SceneManager.profile && SceneManager.profile.startupCollections.SelectMany(s => s.scenesToAutomaticallyOpen).Contains(this)))
                sb.AppendLine("Will <b>open</b> during startup.");
            else
                sb.AppendLine("Will <b>not open</b> during startup.");

        }

        void GetPersistentInfo(StringBuilder sb)
        {

            sb.AppendLine("\n<b>Persistent</b>");

            if (keepOpenWhenCollectionsClose)
                sb.AppendLine("This scene will <b>remain open</b> when the collection is closed, or when another collection tries to close it.");
            else
                sb.AppendLine("This scene will <b>close</b> when the collection is closed, or when another collection tries to close it.");

            sb.AppendLine();

            if (keepOpenWhenNewCollectionWouldReopen)
                sb.AppendLine("This scene will <b>stay open</b> when a new collection tries to reopen it.");
            else
                sb.AppendLine("This scene will <b>be closed</b> when a new collection tries to reopen it.");

        }

        void GetDescriptionInfo(StringBuilder sb)
        {

            if (this == SceneManager.assets.defaults.inGameToolbarScene)
            {
                sb.AppendLine("\n<b>Description:</b>");
                sb.AppendLine(InGameToolbarDescription);
            }
            else if (this == SceneManager.assets.defaults.pauseScene)
            {
                sb.AppendLine("\n<b>Description:</b>");
                sb.AppendLine(PauseScreenDescription);
            }

        }

        void GetHotkeyInfo(StringBuilder sb)
        {

            sb.AppendLine("\n<b>Hotkeys:</b>");

#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM

            if (!SceneManager.profile)
            {
                sb.AppendLine("--");
                return;
            }

            var scene = SceneManager.profile.standaloneScenes.FirstOrDefault(s => s == this);
            if (!scene || scene.inputBindings?.Length == 0)
            {
                sb.AppendLine("--");
                return;
            }

            foreach (var binding in scene.inputBindings)
            {

                if (binding.buttons.Any())
                    sb.AppendJoin(" + ", binding.buttons.Select(b => b.name));
                else
                    sb.Append("None");

                sb.Append($" <i>(interaction: {binding.interactionType})</i>");
                sb.Append("\n");

            }

#else
            sb.AppendLine("--");
#endif

        }

        void GetSceneLoaderInfo(StringBuilder sb)
        {

            sb.AppendLine("\n<b>Scene loader:</b>");

            var sceneLoader = SceneManager.runtime.GetSceneLoader(this.sceneLoader);
            sb.AppendLine(sceneLoader?.Key ?? "ASM runtime scene loader");

        }

        #endregion

        /// <summary>Toggles whatever this scene should be automatically opened when a collection is opened. No effect if scene is not already contained within collection.</summary>
        /// <remarks>This is the save as toggling option in scene popup.</remarks>
        /// <param name="collection">The collection to flag auto open option for.</param>
        /// <param name="openSceneWhenCollectionOpen">Specifies whatever scene should be automatically opened or not, when collection is opened.</param>
        public void SetAutoOpenIn(SceneCollection collection, bool openSceneWhenCollectionOpen)
        {

            if (!collection || !collection.Contains(this))
                return;

            collection.SetAutoOpen(this, openSceneWhenCollectionOpen);

        }

        /// <summary>Gets whatever the scene should automatically open, when this collection is opened.</summary>
        public bool ShouldAutoOpenIn(SceneCollection collection)
        {
            if (!collection)
                return false;

            return collection.ShouldAutoOpen(this);
        }

        /// <summary>Evaluates whether this scene should remain open as persistent.</summary>
        /// <param name="parentCollection">The collection that was open before <paramref name="collectionToOpen"/>.</param>
        /// <param name="collectionToOpen">The collection that will be opened. Pass <see langword="null"/> if evaluating before it opens. If multiple collections are opened in sequence, pass the final one.</param>
        public bool EvalOpenAsPersistent(SceneCollection parentCollection, SceneCollection collectionToOpen = null) =>
            keepOpenWhenCollectionsClose ||
            (parentCollection && parentCollection.openAsPersistent) ||
            (keepOpenWhenNewCollectionWouldReopen && collectionToOpen && collectionToOpen.Contains(this));

        /// <summary>Gets whatever the scene is tracked by ASM.</summary>
        public bool isImported => SceneManager.assets.scenes.Contains(this);

        /// <summary>Gets whatever this scene is included in build.</summary>
        public bool isIncludedInBuilds => SceneUtility.IsIncluded(this);

    }

}
