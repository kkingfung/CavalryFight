using AdvancedSceneManager.Editor.UI.Views.Popups;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class HeaderView
    {

        static VisualElement GetButton(string fontAwesomeIcon, string tooltip, Action callback)
        {
            var button = new Button(callback) { text = fontAwesomeIcon, tooltip = tooltip };
            button.UseFontAwesome();
            return button;
        }

        [ASMWindowElement(ElementLocation.Header)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement DevBuild() =>
            GetButton("", "Dev build", () => MenuPopup.DoDevBuild());

        [ASMWindowElement(ElementLocation.Header)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement BuildProfiles() =>
#if UNITY_6000_0_OR_NEWER
            GetButton("", "Build profiles", () => BuildPlayerWindow.ShowBuildPlayerWindow());
#else
            GetButton("", "Build Settings", () => BuildPlayerWindow.ShowBuildPlayerWindow());
#endif

        [ASMWindowElement(ElementLocation.Header)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement ProjectSettings() =>
            GetButton("", "Project settings", () => SettingsService.OpenProjectSettings());

        [ASMWindowElement(ElementLocation.Header)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement OpenEditor() =>
           GetButton("", "Open code editor", () => EditorApplication.ExecuteMenuItem("Assets/Open C# Project"));

        [ASMWindowElement(ElementLocation.Header, isVisibleByDefault: true)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement OpenOverview() =>
           GetButton("", "Overview", () => ASMWindow.OpenPopup<OverviewPopup>());

        [ASMWindowElement(ElementLocation.Header)]
        [ASMWindowElement(ElementLocation.Footer)]
        static VisualElement OpenUnitySearch() =>
           GetButton("", "Unity search", () => UnityEditor.Search.SearchService.ShowContextual("ASM"));

    }

}
