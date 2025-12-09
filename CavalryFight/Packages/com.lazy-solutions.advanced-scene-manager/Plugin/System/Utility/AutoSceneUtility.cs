using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Represents event args for <see cref="AutoSceneHandlerAttribute"/>.</summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// [AutoSceneHandler]
    /// static void OnHandleAutoScene(AutoSceneEventArgs e)
    /// { }
    /// </code>
    /// </remarks>
    public class AutoSceneEventArgs
    {

        /// <summary />
        public AutoSceneEventArgs(Scene parentScene, Scene autoScene, string scenePath, string autoSceneKey, SceneEvent e)
        {
            this.parentScene = parentScene;
            this.autoScene = autoScene;
            this.scenePath = scenePath;
            this.autoSceneKey = autoSceneKey;
            this.sceneEvent = e;
        }

        /// <summary>The scene that the auto scene belongs to.</summary>
        public Scene parentScene { get; }

        /// <summary>The auto scene to handle.</summary>
        /// <remarks><see langword="null"/> if <see cref="scenePath"/> was used.</remarks>
        public Scene autoScene { get; }

        /// <summary>The path of the auto scene to handle.</summary>
        /// <remarks><see langword="null"/> if <see cref="autoScene"/> was used.</remarks>
        public string scenePath { get; }

        /// <summary>The key of the auto scene.</summary>
        public string autoSceneKey { get; }

        /// <summary>Gets the event type.</summary>
        /// <remarks>
        /// Available events are:
        /// <list type="bullet">
        /// <item><see cref="SceneOpenEvent"/></item>
        /// <item><see cref="SceneCloseEvent"/></item>
        /// </list>
        /// </remarks>
        public SceneEvent sceneEvent { get; }

    }

    /// <summary>Registers the method to handle the auto scene with the specified auto scene key.</summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// [AutoSceneHandler]
    /// static void OnHandleAutoScene(AutoSceneEventArgs e)
    /// { }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class AutoSceneHandlerAttribute : DiscoverableAttribute
    {

        /// <summary />
        public AutoSceneHandlerAttribute(string autoSceneKey) =>
            AutoSceneKey = autoSceneKey;

        /// <summary>The auto scene key to handle.</summary>
        public string AutoSceneKey { get; }

        /// <inheritdoc />
        public override bool IsValidTarget(MemberInfo member) =>
           member.HasParameters<AutoSceneEventArgs>() && //Checks if member is a method, and has parameter AutoSceneEventArgs
            (member.ReturnsVoid() || member.ReturnsCoroutine());  //Can return either nothing or a coroutine

        /// <inheritdoc />
        public override string friendlyDescription =>
            "Registers the method to handle the auto scene with the specified auto scene key.";

    }

    /// <summary>Contains utility methods related to auto scenes.</summary>
    public static class AutoSceneUtility
    {

        [OnLoad]
        static void OnLoad()
        {

            SceneManager.events.RegisterCallback<SceneOpenEvent>(e => HandleEvent(e), when: When.After);
            SceneManager.events.RegisterCallback<SceneCloseEvent>(e => HandleEvent(e), when: When.After);

            void HandleEvent<T>(T e) where T : SceneEvent
            {
                var autoScenes = e.scene.EnumerateAutoScenes();
                foreach (var autoScene in autoScenes)
                {
                    if (!autoScene.IsValid())
                        continue;

                    if (autoScene.scene && autoScene.option.HasValue)
                    {
                        HandleDefault(e, autoScene);
                    }
                    else if (!string.IsNullOrEmpty(autoScene.customOption) && (autoScene.scene || !string.IsNullOrEmpty(autoScene.scenePath)))
                    {
                        HandlePlugin<T>(e, autoScene);
                    }
                }
            }

            void HandleDefault<T>(T e, AutoSceneEntry autoScene) where T : SceneEvent
            {
                //ASM handles AutoSceneOption

                if (autoScene.option == AutoSceneOption.Never ||
                   (autoScene.option == AutoSceneOption.EditModeOnly && Application.isPlaying) ||
                   (autoScene.option == AutoSceneOption.PlayModeOnly && !Application.isPlaying))
                    return;

                if (e is SceneOpenEvent && !autoScene.scene.isOpen)
                    autoScene.scene.Open();
                else if (e is SceneCloseEvent && autoScene.scene.isOpen)
                    autoScene.scene.Close();

            }

            void HandlePlugin<T>(T e, AutoSceneEntry autoScene) where T : SceneEvent
            {
                var members = DiscoverabilityUtility.GetMembers<AutoSceneHandlerAttribute>();
                var method = members.FirstOrDefault(m => ((AutoSceneHandlerAttribute)m.attribute).AutoSceneKey == autoScene.customOption).member as MethodInfo;

                if (method is null)
                    return;

                try
                {

                    var args = new AutoSceneEventArgs(e.scene, autoScene.scene, autoScene.scenePath, autoScene.customOption, e);
                    var obj = method.Invoke(null, new object[] { args });

                    if (obj is IEnumerator c)
                        c.StartCoroutine();

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Could not invoke auto scene handler for '{autoScene.customOption}'.");
                    Debug.LogException(ex);
                }
            }

        }

        /// <summary>Finds the auto scene entry matching <paramref name="scene"/> and <paramref name="option"/>.</summary>
        public static AutoSceneEntry FindAutoScene<TKey, TOption>(this IAutoScenes<TKey, TOption> obj, TKey scene, TOption option) =>
            obj.autoScenes.FirstOrDefault(entry => MatchKey(entry, scene) && MatchOption(entry, option));

        /// <summary>Sets an auto scene for this scene.</summary>
        public static void SetAutoScene<TKey, TOption>(this IAutoScenes<TKey, TOption> obj, TKey scene, TOption option)
        {

            obj.RemoveAutoScene(scene, option);

            var entry = CreateEntry(scene, option);

            if (!entry.IsValid())
                throw new InvalidOperationException("Cannot add auto scene entry as it was invalid.");

            obj.autoScenes.Add(entry);
            obj.Save();

        }

        /// <summary>Removes an auto scene for this scene.</summary>
        public static void RemoveAutoScene<TKey, TOption>(this IAutoScenes<TKey, TOption> obj, TKey scene, TOption option)
        {

            var count = obj.autoScenes.RemoveAll(entry => MatchKey(entry, scene) && MatchOption(entry, option));
            if (count > 0)
                obj.Save();

        }

        static AutoSceneEntry CreateEntry<TKey, TOption>(TKey scene, TOption option)
        {

            var entry = new AutoSceneEntry();

            if (scene is Scene s)
                entry.scene = s;
            else if (scene is string scenePath)
                entry.scenePath = scenePath;

            if (option is AutoSceneOption enumOption)
                entry.option = enumOption;
            else if (option is string customOption)
                entry.customOption = customOption;

            return entry;

        }

        static bool MatchKey<TKey>(AutoSceneEntry e, TKey scene)
        {
            return scene switch
            {
                Scene s => e.scene == s,
                string path => e.scenePath == path,
                _ => false
            };
        }

        static bool MatchOption<TOption>(AutoSceneEntry e, TOption option)
        {
            return option switch
            {
                AutoSceneOption enumOption => e.option == enumOption,
                string customOption => e.customOption == customOption,
                _ => false
            };
        }

    }

}
