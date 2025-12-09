using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class UnreferencedCollectionsPopup : ViewModel, IPopup
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.unreferencedCollections;

        protected override void OnAdded()
        {

            view.Q("button-cancel").RegisterCallback<ClickEvent>(e => ASMWindow.ClosePopup());
            view.Q("button-cleanup").RegisterCallback<ClickEvent>(e => Cleanup());
            view.Q("button-close").RegisterCallback<ClickEvent>(e => ASMWindow.ClosePopup());

            RegisterEvent<ScenesAvailableForImportChangedEvent>(e => PopulateList());

            PopulateList();

        }

        static readonly Dictionary<ISceneCollection, bool> checkedItems = new();

        void PopulateList()
        {

            var scroll = view.Q<ScrollView>("scroll");
            scroll.Clear();

            var profiles = GetUnreferencedCollections();

            if (!profiles.Any())
            {
                ASMWindow.ClosePopup();
                return;
            }

            var dict = new Dictionary<Profile, Foldout>();

            foreach (var (profile, collections) in profiles)
            {
                var label = new Label(profile.name);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 8;
                scroll.Add(label);

                foreach (var collection in collections)
                {

                    var toggle = new Toggle
                    {
                        text = collection.title,
                        value = checkedItems.GetValueOrDefault(collection)
                    };

                    ApplyToggleStyle(toggle);

                    toggle.RegisterValueChangedCallback(e =>
                    {
                        checkedItems[collection] = e.newValue;
                        view.Q("button-cleanup").SetEnabled(checkedItems.Where(c => c.Value).Any());
                    });

                    scroll.Add(toggle);
                }
            }

            view.Q("button-cleanup").SetEnabled(checkedItems.Where(c => c.Value).Any());

        }

        void ApplyToggleStyle(Toggle toggle)
        {

            toggle.Q(className: "unity-toggle__input").style.flexDirection = FlexDirection.RowReverse;
            toggle.Q(className: "unity-toggle__input").style.flexGrow = 1;
            toggle.Q<Label>().pickingMode = PickingMode.Ignore;
            toggle.style.width = new Length(100, LengthUnit.Percent);

        }

        void Cleanup()
        {

            var collections = checkedItems.Where(c => c.Value).Select(c => c.Key).ToList();
            if (!collections.Any())
                return;

            AssetDatabase.StartAssetEditing();

            try
            {

                foreach (var collection in collections)
                {

                    var path = AssetDatabase.GetAssetPath((ScriptableObject)collection);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (var obj in assets)
                    {
                        if (obj == (ScriptableObject)collection)
                        {
                            Object.DestroyImmediate(obj, true);
                            break;
                        }
                    }

                    AssetDatabase.ImportAsset(path);

                }

                foreach (var profile in SceneManager.assets.profiles)
                    profile.SaveNow();

            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

        }

        public static Dictionary<Profile, ISceneCollection[]> GetUnreferencedCollections() =>
            SceneManager.assets.profiles.ToDictionary(p => p, p => p.FindUntrackedCollections().ToArray()).Where(kvp => kvp.Value?.Any() ?? false).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    }

}