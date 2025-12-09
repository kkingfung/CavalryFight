using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using System.Linq;
#endif

namespace AdvancedSceneManager.Editor.Utility
{

    static class IndicatorUtility
    {

        [OnLoad]
        static IndicatorUtility() =>
            HierarchyGUIUtility.AddSceneGUI(s =>
            {

                if (FallbackSceneUtility.IsFallbackScene(s))
                    FallbackSceneIndicator();
                else if (s.ASMScene(out var scene))
                {

                    SceneLoaderIndicator(scene);
                    CollectionIndicator(scene);
                    PersistentIndicator(scene);
                    UntrackedIndicator(scene);
                    LockIndicator(scene);

                }
                else
                {
                    UnimportedIndicator(s);
                    TestIndicator(s);
                }

            });

        #region Fallback scene

        static void FallbackSceneIndicator()
        {
            if (SceneManager.settings.user.m_defaultSceneIndicator)
                Label("Fallback scene", "This is the 'fallback scene', it makes sure scene management works well");
        }

        #endregion
        #region ASM scene

        static void CollectionIndicator(Scene scene)
        {
            if (SceneManager.settings.user.m_collectionIndicator && scene.openedBy != null)
            {
#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                if (scene.openedBy is StandaloneCollection && SceneBindingUtility.WasOpenedByBinding(scene))
                    Label(string.Join("\n", scene.inputBindings.Select(b => string.Join("+", b.buttons.Select(b => ObjectNames.NicifyVariableName(b.name))))), "This scene was opened from a binding");
                else
#endif
                    Label(scene.openedBy.title, $"This scene was opened from '{scene.openedBy.title}' collection");
            }
        }

        static void PersistentIndicator(Scene scene)
        {
            if (SceneManager.settings.user.m_persistentIndicator)
                if (Application.isPlaying && scene.isPersistent)
                    Label("Persistent", "This scene is persistent, it will not be closed when collections close");
        }

        static void UntrackedIndicator(Scene scene)
        {
#if ADDRESSABLES
            if (scene.isAddressable && scene.m_assetReference != null)
                return;
#endif

            if (SceneManager.settings.user.m_untrackedIndicator)
                if (!SceneManager.runtime.IsTracked(scene))
                    Label("Untracked", "This scene is untracked, something either went wrong somewhere, or scene is unimported.");
        }

        static void SceneLoaderIndicator(Scene scene)
        {
            if (!SceneManager.settings.user.m_sceneLoaderIndicator)
                return;

            var sceneLoader = scene.GetEffectiveSceneLoader();
            if (!string.IsNullOrEmpty(sceneLoader?.indicator.text))
                Label(sceneLoader.indicator.text, "This scene was opened by: " + sceneLoader.Key);

        }

        static void LockIndicator(Scene scene)
        {

            if (SceneManager.settings.user.m_lockIndicator)
                if (SceneManager.settings.project.allowSceneLocking && GUILayout.Button(GetContent(scene), EditorStyles.iconButton))
                    EditorApplication.delayCall += () =>
                    {
                        LockUtility.Toggle(scene, true);
                        HierarchyGUIUtility.Repaint();
                    };

        }

        static Texture lockedIcon;
        static Texture unlockedIcon;
        static GUIContent lockContent;
        static GUIContent unlockContent;
        static GUIContent modifiedContent;
        static GUIContent GetContent(Scene scene)
        {

            lockedIcon ??= EditorGUIUtility.IconContent("Locked@2x").image;
            unlockedIcon ??= EditorGUIUtility.IconContent("Unlocked@2x").image;
            modifiedContent ??= new(EditorGUIUtility.IconContent("d_P4_LockedLocal").image, "Locked:\nThis scene has been modified, you will be asked to save as new scene, or discard.\nClick to unlock.");

            lockContent ??= new(unlockedIcon, "Lock");
            unlockContent ??= new(lockedIcon, "Unlock");

            if (!scene.isLocked)
                return lockContent;
            else if (scene.internalScene?.isDirty ?? false)
                return modifiedContent;
            else if (!string.IsNullOrWhiteSpace(scene.lockMessage))
                return new(lockedIcon, "Unlock:\n" + scene.lockMessage);
            else
                return unlockContent;

        }

        #endregion
        #region Unity scene

        static void UnimportedIndicator(UnityEngine.SceneManagement.Scene scene)
        {

            if (!SceneManager.settings.user.m_unimportedIndicator)
                return;

            if (SceneManager.runtime.dontDestroyOnLoad && SceneManager.runtime.dontDestroyOnLoad.internalScene?.path == scene.path)
                return;

            if (!SceneImportUtility.StringExtensions.IsValidSceneToImport(scene.path))
                return;

            Label("Unimported", "This scene is not imported.");

        }

        static void TestIndicator(UnityEngine.SceneManagement.Scene scene)
        {

            if (SceneManager.settings.user.m_testIndicator)
                if (SceneImportUtility.StringExtensions.IsTestScene(scene.path))
                    Label("TestRunner", "This scene makes sure the test runner, runs.");

        }

        #endregion
        #region Settings

        [OnLoad]
        static void AddSceneContextMenuItems() =>
                SceneHierarchyHooks.addItemsToSceneHeaderContextMenu += (menu, e) =>
                {

                    if (!e.ASMScene(out var scene))
                        return;

                    menu.AddSeparator("");

                    menu.AddItem(new("Indicators/Show fallback scene indicator"), SceneManager.settings.user.m_defaultSceneIndicator, () => SceneManager.settings.user.m_defaultSceneIndicator = !SceneManager.settings.user.m_defaultSceneIndicator);
                    menu.AddItem(new("Indicators/Show scene loader indicator"), SceneManager.settings.user.m_sceneLoaderIndicator, () => SceneManager.settings.user.m_sceneLoaderIndicator = !SceneManager.settings.user.m_sceneLoaderIndicator);
                    menu.AddItem(new("Indicators/Show collection scene indicator"), SceneManager.settings.user.m_collectionIndicator, () => SceneManager.settings.user.m_collectionIndicator = !SceneManager.settings.user.m_collectionIndicator);
                    menu.AddItem(new("Indicators/Show persistent scene indicator"), SceneManager.settings.user.m_persistentIndicator, () => SceneManager.settings.user.m_persistentIndicator = !SceneManager.settings.user.m_persistentIndicator);
                    menu.AddItem(new("Indicators/Show untracked indicator"), SceneManager.settings.user.m_untrackedIndicator, () => SceneManager.settings.user.m_untrackedIndicator = !SceneManager.settings.user.m_untrackedIndicator);
                    menu.AddItem(new("Indicators/Show unimported indicator"), SceneManager.settings.user.m_unimportedIndicator, () => SceneManager.settings.user.m_unimportedIndicator = !SceneManager.settings.user.m_unimportedIndicator);
                    menu.AddItem(new("Indicators/Show test scene indicator"), SceneManager.settings.user.m_testIndicator, () => SceneManager.settings.user.m_testIndicator = !SceneManager.settings.user.m_testIndicator);
                    menu.AddItem(new("Indicators/Show lock"), SceneManager.settings.user.m_lockIndicator, () => SceneManager.settings.user.m_lockIndicator = !SceneManager.settings.user.m_lockIndicator);

                    menu.AddSeparator("Indicators/");

                    menu.AddItem(new("Indicators/Settings"), false, () =>
                    {
                        SceneManagerWindow.Open();
                        ASMWindow.OpenSettings<SettingsPopup.AppearancePage>();
                    });

                };

        #endregion

        static void Label(string text, string tooltip) =>
            GUILayout.Label(new GUIContent(text, tooltip), HierarchyGUIUtility.defaultStyle, GUILayout.ExpandWidth(false));

    }

}
