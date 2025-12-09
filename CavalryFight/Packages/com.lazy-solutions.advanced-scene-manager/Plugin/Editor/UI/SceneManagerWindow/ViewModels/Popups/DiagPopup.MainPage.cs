using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class DiagPopup
    {

        public class MainPage : SubPage
        {
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.diag.main;
            public override string title => "Diagnostics";

            Label sceneInfoLabel;
            Label textServicesLabel;
            Label textDiscoverablesLabel;

            double lastUpdateTime;

            protected override void OnAdded()
            {

                base.OnAdded();

                var initializationTextOverall = $"ASM took {ASMSettings.initializationTimeOverall:F3}s to initialize.\n";
                var initializationTextDiscoverables = $"    <color=#888888>Discoverables: {ASMSettings.initializationTimeDiscoverables:F3}s.</color>\n";
                var initializationTextServices = $"    <color=#888888>Services: {ASMSettings.initializationTimeServices:F3}s.</color>\n";
                var initializationTextCallbacks = $"    <color=#888888>Callbacks: {ASMSettings.initializationTimeCallbacks:F3}s.</color>";

                view.Q<Label>("text-startup-time").text =
                    initializationTextOverall +
                    initializationTextDiscoverables +
#if ASM_DEV
                    initializationTextServices +
#endif
                    initializationTextCallbacks;

                sceneInfoLabel = view.Q<Label>("text-scene-info");
                textServicesLabel = view.Q<Label>("text-services-info");
                textDiscoverablesLabel = view.Q<Label>("text-discoverables-info");

                EditorApplication.update += Update;
                Update();

                textServicesLabel.Hide();

#if !ASM_DEV
                view.Q("navigate-services").Hide();
#endif

            }

            protected override void OnRemoved()
            {
                EditorApplication.update -= Update;
                base.OnRemoved();
            }

            void Update()
            {

                // throttle updates to ~1s
                if (EditorApplication.timeSinceStartup - lastUpdateTime < 1.0)
                    return;
                lastUpdateTime = EditorApplication.timeSinceStartup;

                RefreshScenes();
                RefreshDiscoverables();
                RefreshServices();

            }

            void RefreshScenes()
            {
                var fallbackScene = FallbackSceneUtility.GetScene();

                var openScenes = SceneUtility.GetAllOpenUnityScenes()
                    .Where(s => s != fallbackScene)
                    .Select(s => s.ASMScene())
                    .ToList();

                var openingScenes = SceneManager.runtime.runningOperations.Select(o => o.open).ToList();
                var closingScenes = SceneManager.runtime.runningOperations.Select(o => o.close).ToList();

                string text = "";

                if (openScenes.Count == 0 && openingScenes.Count == 0 && closingScenes.Count == 0)
                {
                    text = "No user scenes open, and no open requests.";
                }
                else
                {
                    if (openScenes.Count > 0)
                    {
                        var untrackedCount = openScenes.Count(s => !s);
                        var untracked = untrackedCount == 0
                            ? string.Empty
                            : $" <color=#888888>({untrackedCount} untracked)</color>";

                        text += $"{openScenes.Count} user scene{Plural(openScenes.Count)} open{untracked}.\n";
                    }

                    if (openingScenes.Count > 0)
                        text += $"{openingScenes.Count} scene open request{Plural(openingScenes.Count)}.\n";

                    if (closingScenes.Count > 0)
                        text += $"{closingScenes.Count} scene close request{Plural(closingScenes.Count)}.\n";
                }

                sceneInfoLabel.text = text.TrimEnd('\n');
            }

            void RefreshDiscoverables()
            {
                var discoverables = DiscoverabilityUtility.GetMembers();
                var discoverableCount = discoverables.Count();
                var asmDiscoverableCount = discoverables
                    .Count(IsASMDiscoverable);

                textDiscoverablesLabel.text =
                    $"{discoverableCount} discoverable{Plural(discoverableCount)} found " +
                    $"<color=#888888>({asmDiscoverableCount} ASM)</color>";
            }

            void RefreshServices()
            {

#if ASM_DEV

                var services = ServiceUtility.GetAll().ToList();
                var asmServices = services
                    .Select(s => s.Key)
                    .Count(IsASMService);

                textServicesLabel.text =
                    $"{services.Count} service{Plural(services.Count)} registered " +
                    $"<color=#888888>({asmServices} ASM)</color>";
#endif

            }

            string Plural(int count) => count == 1 ? string.Empty : "s";

            Assembly mainAssembly { get; } = typeof(SceneManager).Assembly;
            Assembly editorAssembly { get; } = typeof(DiagPopup).Assembly;

            bool IsASMDiscoverable(DiscoveredMember member)
            {
                var assembly = (member.member as Type ?? member.member.DeclaringType)?.Assembly;
                return assembly.FullName == mainAssembly.FullName || assembly.FullName == editorAssembly.FullName;
            }

            bool IsASMService(Type type)
            {
                var mainAssembly = typeof(SceneManager).Assembly;
                var editorAssembly = typeof(DiagPopup).Assembly;
                return type.Assembly == mainAssembly || type.Assembly == editorAssembly;
            }

        }
    }
}
