using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Utility.Discoverability;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class HeaderView
    {

        [Inject] public IPersistentNotificationService persistentNotificationService { get; private set; } = null!;
        [Inject] public ICollectionViewService collectionView { get; private set; } = null!;

        void OnAdded_DevMenu()
        {
            rootVisualElement.Q("button-menu").ContextMenu(AddMenuActions);
        }

        void AddMenuActions(ContextualMenuPopulateEvent e)
        {

#if ASM_DEV
            e.menu.AppendAction("Enable dev features", _ => ToggleScriptingDefine("ASM_DEV"), DropdownMenuAction.Status.Checked);
#else
            e.menu.AppendAction("Enable dev features", _ => ToggleScriptingDefine("ASM_DEV"), DropdownMenuAction.Status.Normal);
#endif

            e.menu.AppendSeparator();

            AddViewMenu(e);
            AddToolsMenu(e);
            AddDebugMenu(e);

            e.menu.AppendSeparator();
            e.menu.AppendAction("Reload domain...", _ => UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation());

        }

        void AddViewMenu(ContextualMenuPopulateEvent e)
        {
            e.menu.AppendAction("View/ASM folder...", _ => AssetDatabaseUtility.ShowFolder(SceneManager.package.folder));
            e.menu.AppendAction("View/Window source...", _ => AssetDatabaseUtility.ShowFolder(WindowPath()));

            e.menu.AppendSeparator("View/");

            e.menu.AppendAction("View/Profiles...", _ => AssetDatabaseUtility.ShowFolder(ProfilePath()));
            e.menu.AppendAction("View/Imported scenes...", _ => AssetDatabaseUtility.ShowFolder(ScenePath()));

            e.menu.AppendSeparator("View/");

            e.menu.AppendAction("View/Settings...", _ => InspectorWindow.Open());

        }

        void AddToolsMenu(ContextualMenuPopulateEvent e)
        {
            e.menu.AppendAction("Tools/Reload collection view...", _ => collectionView.Reload());
            e.menu.AppendAction("Tools/Reload notifications...", _ => persistentNotificationService.ReloadNotifications());
        }

        void AddDebugMenu(ContextualMenuPopulateEvent e)
        {
            e.menu.AppendAction("Debug/Force all notifications visible...", _ => ToggleForceNotificationsVisible(),
                persistentNotificationService.forceDisplayAll ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            e.menu.AppendAction("Debug/Prevent undo countdown from finishing...", _ => TogglePreventUndoCountdownFromFinishing(),
                undoService.preventCountdownFromFinishing ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            e.menu.AppendSeparator("Debug/");

            e.menu.AppendAction("Debug/Unset profile...", _ => ProfileUtility.SetProfile(null));
            e.menu.AppendAction("Debug/Invalidate discoverable caches...", _ => InvalidateDiscoverableCaches());
            e.menu.AppendAction("Debug/Open welcome wizard...", _ => ResetWelcomeWizard());
        }

        void ToggleForceNotificationsVisible()
        {
            persistentNotificationService.SetAllVisible(!persistentNotificationService.forceDisplayAll);
        }

        void TogglePreventUndoCountdownFromFinishing()
        {
            undoService.preventCountdownFromFinishing = !undoService.preventCountdownFromFinishing;
        }

        void ToggleScriptingDefine(string define)
        {
            // Get the current define symbols for the active build target
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            if (defines.Contains(define))
            {
                defines = defines.Replace(define, "");
            }
            else
            {
                if (!EditorUtility.DisplayDialog(
                    "Enable dev features",
                    "This will enable experimental features currently under development.\n" +
                    "The features may not work, are you sure you wish to proceed?\n" +
                    "\n" +
                    "Note that if you receive compilation errors and the window does not work, then you may remove the scripting define #ASM_DEV from project settings to disable manually.", "Continue", "Cancel"))
                    return;

                defines = defines + ";" + define;
            }

            // Apply the new set of defines
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), defines);
        }

        string WindowPath() => SceneManager.package.folder + "/Plugin/Editor/UI/SceneManagerWindow";
        string ProfilePath() => SceneManager.assetImport.GetFolder<Profile>();
        string ScenePath() => SceneManager.assetImport.GetFolder<Scene>();

        abstract class InspectorWindow : EditorWindow
        {

            class ProjectSettingsInspectorWindow : InspectorWindow
            {
                public override Object target => SceneManager.settings.project;
            }

            class UserSettingsInspectorWindow : InspectorWindow
            {
                public override Object target => SceneManager.settings.user;
            }

            class AssetsInspectorWindow : InspectorWindow
            {
                public override Object target => SceneManager.settings.project.assets;
            }

            class DiscoverablesInspectorWindow : InspectorWindow
            {
                public override Object target => SceneManager.settings.project.discoverablesCache;
            }

            public abstract Object target { get; }

            public static void Open()
            {

                var w = GetWindow<ProjectSettingsInspectorWindow>("\u200BProject");
                w.Show();

                GetWindow<UserSettingsInspectorWindow>("User", typeof(ProjectSettingsInspectorWindow));
                GetWindow<AssetsInspectorWindow>("Assets", typeof(ProjectSettingsInspectorWindow));
                GetWindow<DiscoverablesInspectorWindow>("Discoverables", typeof(ProjectSettingsInspectorWindow));

                w.Focus();

            }

            public UnityEditor.Editor editor;

            GUIStyle style = null!;
            private void OnEnable()
            {
                style = new GUIStyle() { padding = new RectOffset(20, 20, 20, 20) };
                editor = UnityEditor.Editor.CreateEditor(target);

            }

            Vector2 scrollPos;
            private void OnGUI()
            {

                scrollPos = GUILayout.BeginScrollView(scrollPos, style);
                GUI.enabled = true;

                if (editor)
                {

                    EditorGUI.BeginChangeCheck();

                    //Fixes issue where object can't be modified
                    if (editor.target.hideFlags != (HideFlags.HideInHierarchy | HideFlags.DontSave))
                    {
                        Debug.Log("Fixing hideFlags for ASMSettings...");
                        editor.target.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                        EditorUtility.SetDirty(this);
                    }
                    editor.DrawDefaultInspector();

                    var changed = EditorGUI.EndChangeCheck();

                    if (changed)
                        editor.serializedObject.ApplyModifiedProperties();

                }

                GUI.enabled = true;
                GUILayout.EndScrollView();
            }

        }

        void InvalidateDiscoverableCaches()
        {
            DiscoverabilityUtility.InvalidateCache();
        }

        void ResetWelcomeWizard()
        {
            SceneManager.settings.user.hasCompletedWelcomeWizard = false;
            ((SceneManagerWindow)window).mainView.Show<WelcomeWizardView>();
        }

    }

}
