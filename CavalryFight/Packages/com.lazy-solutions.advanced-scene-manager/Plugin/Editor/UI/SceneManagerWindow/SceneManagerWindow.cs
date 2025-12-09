using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.DependencyInjection.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Utility;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>The scene manager window provides the front-end for Advanced Scene Manager.</summary>
    internal partial class SceneManagerWindow : EditorWindow
    {

        internal static new VisualElement rootVisualElement { get; private set; }
        internal static SceneManagerWindow window { get; private set; }

        public MainView mainView { get; private set; } = null!;
        public ViewLocator viewLocator => ViewLocator.instance;

        void CreateGUI()
        {

            ViewModel.m_window = this;

            titleContent = new GUIContent("Scene Manager");

            minSize = new(466, 230);

            window = this;
            rootVisualElement = base.rootVisualElement;

            mainView = ViewModel.Instantiate<MainView>();

            SceneManager.OnInitialized(() =>
            {
                mainView.Add(rootVisualElement);
                DependencyInjectionUtility.Add<ISceneManagerWindow, SceneManagerWindowService>();
                SceneManager.events.InvokeCallbackSync<ASMWindowOpenEvent>();
            });

        }

        private void OnDestroy()
        {
            SceneManager.events.InvokeCallbackSync<ASMWindowCloseEvent>();
            mainView?.Remove();
            ViewModel.m_window = null!;
        }

        /// <summary>Closes the window.</summary>
        public static new void Close()
        {
            if (window)
                ((EditorWindow)window).Close();
        }

        [MenuItem("File/Scene Manager %#m", priority = 205)]
        [MenuItem("Window/Advanced Scene Manager/Scene Manager", priority = 3030)]
        public static void Open() => GetWindow<SceneManagerWindow>();

        public static new void Focus()
        {
            if (window)
                ((EditorWindow)window).Focus();
        }

        #region ISceneManagerWindow

        sealed class SceneManagerWindowService : ISceneManagerWindow
        {

            public void OpenWindow() =>
                Open();

            public void CloseWindow() =>
                Close();

        }

        #endregion
        #region Callbacks

        void OnEnable()
        {
            EditorApplication.focusChanged += EditorApplication_focusChanged;
            SceneManager.events.InvokeCallbackSync<OnWindowEnableEvent>();
            ASMScriptableSingletonBuildStep.Cleanup();
        }

        void OnDisable()
        {
            EditorApplication.focusChanged -= EditorApplication_focusChanged;
            SceneManager.events.InvokeCallbackSync<OnWindowDisableEvent>();
        }

        private void EditorApplication_focusChanged(bool focused)
        {
            if (focused)
                OnFocus();
            else
                OnLostFocus();
        }

        void OnFocus()
        {

            ASMScriptableSingletonBuildStep.Cleanup();

            if (!SceneManager.isInitialized)
                return;

            SceneManager.events.InvokeCallbackSync<OnWindowFocusEvent>();

        }

        void OnLostFocus()
        {
            SceneManager.events.InvokeCallbackSync<OnWindowLostFocusEvent>();
        }

        public bool wantsConstantRepaint { get; set; }

        void OnGUI()
        {
            if (wantsConstantRepaint)
                Repaint();
        }

        #endregion
        #region Close on uninstall

        [OnLoad]
        static void OnLoad()
        {
            Events.registeringPackages += (e) =>
            {
                var id = SceneManager.package.id;
                if (e.removed.Any(p => p.packageId == id))
                    Close();
            };
        }

        #endregion

    }

}
