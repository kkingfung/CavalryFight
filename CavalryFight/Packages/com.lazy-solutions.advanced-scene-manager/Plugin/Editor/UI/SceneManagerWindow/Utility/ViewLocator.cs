using AdvancedSceneManager.Editor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Utility
{

    public class ViewLocator : ScriptableObject
    {

        static ViewLocator m_instance;
        internal static ViewLocator instance
        {
            get
            {
                if (m_instance)
                    return m_instance;

                m_instance = AssetDatabaseUtility.FindAssets<ViewLocator>().FirstOrDefault();
                if (!m_instance)
                    throw new InvalidOperationException("Could not find ViewLocator, you may have to re-install ASM.");

                return m_instance;

            }
        }

        public Main main;
        public Popups popups;
        public WelcomeWizard welcomeWizard;
        public Items items;
        public Misc misc;
        public Styles styles;
        public Fonts fonts;

        [Serializable]
        public struct Main
        {
            public VisualTreeAsset progressSpinner;
            public VisualTreeAsset header;
            public VisualTreeAsset search;
            public VisualTreeAsset collection;
            public VisualTreeAsset selection;
            public VisualTreeAsset notification;
            public VisualTreeAsset undo;
            public VisualTreeAsset childProfiles;
            public VisualTreeAsset footer;
        }

        [Serializable]
        public struct Popups
        {
            public VisualTreeAsset root;
            public VisualTreeAsset pickName;
            public VisualTreeAsset confirm;
            public VisualTreeAsset importScene;
            public VisualTreeAsset dynamicCollection;
            public VisualTreeAsset menu;
            public VisualTreeAsset overview;
            public VisualTreeAsset list;
            public VisualTreeAsset legacy;
            public VisualTreeAsset update;
            public VisualTreeAsset unreferencedCollections;

            public ScenePopup scene;
            public CollectionPopup collection;
            public DiagPopup diag;
            public SettingsPopup settings;


            [Serializable]
            public struct ScenePopup
            {
                public VisualTreeAsset root;
                public VisualTreeAsset main;
                public VisualTreeAsset events;
                public VisualTreeAsset standalone;
            }

            [Serializable]
            public struct CollectionPopup
            {
                public VisualTreeAsset root;
                public VisualTreeAsset main;
                public VisualTreeAsset events;
                public VisualTreeAsset inputBindings;
            }

            [Serializable]
            public struct DiagPopup
            {
                public VisualTreeAsset root;
                public VisualTreeAsset main;
                public VisualTreeAsset coroutines;
                public VisualTreeAsset discoverables;
                public VisualTreeAsset services;
            }

            [Serializable]
            public struct SettingsPopup
            {
                public VisualTreeAsset root;
                public VisualTreeAsset main;
                public VisualTreeAsset startup;
                public VisualTreeAsset sceneLoading;
                public VisualTreeAsset network;
                public VisualTreeAsset updates;
                public VisualTreeAsset experimental;
                public VisualTreeAsset extensions;

                public AssetsPage assets;
                public EditorPage editor;
                public AppearancePage appearance;

                [Serializable]
                public struct AssetsPage
                {
                    public VisualTreeAsset root;
                    public VisualTreeAsset whitelist;
                    public VisualTreeAsset blacklist;
                }

                [Serializable]
                public struct EditorPage
                {
                    public VisualTreeAsset root;
                    public VisualTreeAsset logging;
                }

                [Serializable]
                public struct AppearancePage
                {
                    public VisualTreeAsset root;
                    public VisualTreeAsset extendableUI;
                }

            }

        }

        [Serializable]
        public struct WelcomeWizard
        {
            public VisualTreeAsset root;
            public VisualTreeAsset main;
            public VisualTreeAsset git;
            public VisualTreeAsset dependencies;
            public VisualTreeAsset sceneImport;
            public VisualTreeAsset profileSelector;
            public VisualTreeAsset end;
        }

        [Serializable]
        public struct Items
        {
            public VisualTreeAsset collection;
            public VisualTreeAsset scene;
            public VisualTreeAsset undo;
            public VisualTreeAsset list;
            public VisualTreeAsset importScene;
            public VisualTreeAsset notification;
            public VisualTreeAsset autoScene;
        }

        [Serializable]
        public struct Misc
        {
            public VisualTreeAsset utilityFunctionsWindow;
        }

        [Serializable]
        public struct Styles
        {

            public StyleSheet @base;
            public StyleSheet overrides;
            public StyleSheet collectionView;
            public StyleSheet sceneField;
            public StyleSheet sceneManagerWindow;
            public StyleSheet featureFlags;

            public readonly IEnumerable<StyleSheet> Enumerate() =>
                new[] { @base, overrides, collectionView, sceneField, sceneManagerWindow, featureFlags };

        }

        [Serializable]
        public struct Fonts
        {
            public FontAsset fontAwesomeSolid;
            public FontAsset fontAwesomeRegular;
            public FontAsset fontAwesomeBrands;
        }

    }

}
