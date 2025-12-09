using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView
    {

        public class DependenciesPage : SubPage
        {

            public override string title => "Dependencies";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.dependencies;

            protected override void OnAdded()
            {

                view.Q<Label>("link-netcode").RegisterCallback<ClickEvent>(e => Application.OpenURL("https://github.com/Lazy-Solutions/AdvancedSceneManager/blob/main/docs/guides/Netcode.md"));

                SetupDependency(view.Q("group-editor-coroutines"), "com.unity.editorcoroutines");
                SetupDependency(view.Q("group-input-system"), "com.unity.inputsystem");
                SetupDependency(view.Q("group-addressables"), "com.unity.addressables");
                SetupDependency(view.Q("group-netcode"), "com.unity.netcode.gameobjects");

                UpdateButtons();
                QueryDependencies();

            }

            protected override void OnRemoved()
            {
                elements.Clear();
            }

            readonly Dictionary<string, (VisualElement installedLabel, VisualElement installContainer, VisualElement checkingContainer)> elements = new();

            void SetupDependency(VisualElement element, string packageName)
            {

                var installedLabel = element.Q("label-installed");
                var installButton = element.Q("button-install");
                var installContainer = element.Q("container-install");
                var checkingContainer = element.Q("container-checking");

                installButton.RegisterCallback<ClickEvent>(e => Install(packageName));

                elements.Add(packageName, (installedLabel, installContainer, checkingContainer));

            }

            void UpdateButtons(IEnumerable<string> packages = null)
            {
                foreach (var dependency in elements)
                {
                    var isInstalled = packages?.Contains(dependency.Key);
                    dependency.Value.installedLabel.SetVisible(isInstalled is true);
                    dependency.Value.installContainer.SetVisible(isInstalled is false);
                    dependency.Value.checkingContainer.SetVisible(isInstalled is null);
                }
            }

            void QueryDependencies()
            {
                var installed = new List<string>();

#if INPUTSYSTEM
                installed.Add("com.unity.inputsystem");
#endif
#if ADDRESSABLES
                installed.Add("com.unity.addressables");
#endif
#if NETCODE
                installed.Add("com.unity.netcode.gameobjects");
#endif
#if COROUTINES
                installed.Add("com.unity.editorcoroutines");
#endif

                UpdateButtons(installed);
            }


            void Install(string packageName)
            {
                EditorUtility.DisplayProgressBar("Installing package...", $"Installing {packageName}...", 0);

                Request(
                    () => Client.Add(packageName),
                    request =>
                    {

                        EditorUtility.ClearProgressBar();

                        if (request.Result.errors is not null)
                            foreach (var error in request.Result.errors)
                                Debug.LogError(error.message);

                        if (view?.panel is null)
                            return; //User might have navigated away from page

                        view.SetEnabled(true);
                        OnRemoved();
                        OnAdded();
                    });

            }

            void Request<T>(Func<T> request, Action<T> onComplete) where T : Request
            {
                Coroutine().StartCoroutine();
                IEnumerator Coroutine()
                {

                    view.SetEnabled(false);

                    UnityEditor.PackageManager.Client.Resolve();

                    var r = request();
                    yield return new WaitUntil(() => r.IsCompleted);

                    if (!isAdded)
                        yield break;

                    view?.SetEnabled(true);
                    onComplete(r);

                }
            }

        }

    }

}

