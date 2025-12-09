using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Services;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class CollectionPopup
    {

        public class MainPage : SubPage
        {

            public override string title => context.collection ? context.collection.title : null;
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.collection.main;

            [Inject] private readonly IDialogService dialogService = null!;

            protected override void OnAdded()
            {

                base.OnAdded();

                view.Q<SceneLoaderPicker>().Initialize(context.collection);

                SetupLoadingOptions();
                SetupStartupOptions();
                SetupActiveScene();
                SetupLoadPriority();

#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                view.Q("navigate-input-bindings").Show();
#endif

            }

            #region Header

            public override VisualElement CreateHeaderGUI()
            {

                var element = view.Q("header");

                element.RemoveFromHierarchy();
                element.Show();

                SetupTitle(element);
                SetupLock(element);

                return element;

            }

            void SetupTitle(VisualElement header)
            {

                var renameButton = header.Q<Button>("button-rename");

                renameButton.RegisterCallback<ClickEvent>(e =>
                {
                    var collection = context.collection;
                    dialogService.PromptName(
                        value: collection.title,
                        onContinue: (title) =>
                        {
                            collection.Rename(title);
                            ASMWindow.OpenPopup<CollectionPopup>(new(collection));
                        },
                        onCancel: () => ASMWindow.OpenPopup<CollectionPopup>(new(collection)));
                });

            }

            void SetupLock(VisualElement header)
            {

                var lockButton = header.Q<Button>("button-lock");
                var unlockButton = header.Q<Button>("button-unlock");

                lockButton.clicked += () => context.collection.Lock(prompt: true);
                unlockButton.clicked += () => context.collection.Unlock(prompt: true);

                BindingHelper lockBinding = null;
                BindingHelper unlockBinding = null;

                ReloadButtons();
                view.SetupLockBindings(context.collection);

                void ReloadButtons()
                {

                    lockBinding?.Unbind();
                    unlockBinding?.Unbind();
                    lockButton.SetVisible(false);
                    unlockButton.SetVisible(false);

                    if (!SceneManager.settings.project.allowCollectionLocking)
                        return;

                    lockBinding = lockButton.BindVisibility(context.collection, nameof(context.collection.isLocked), true);
                    unlockBinding = unlockButton.BindVisibility(context.collection, nameof(context.collection.isLocked));

                }

            }

            #endregion
            #region Loading options

            void SetupLoadingOptions()
            {

                var dropdown = view.Q<ScenePicker>("picker-loading-scene");

                dropdown.SetReadOnly(context.collection.loadingScreenUsage != LoadingScreenUsage.Override);
                _ = view.Q<EnumField>("enum-loading-screen").
                    RegisterValueChangedCallback(e =>
                        dropdown.SetReadOnly((LoadingScreenUsage)e.newValue != LoadingScreenUsage.Override));

            }

            #endregion
            #region Startup options

            void SetupStartupOptions()
            {

                var group = view.Q<RadioButtonGroup>("radio-group-startup");
                group.RegisterValueChangedCallback(e => context.collection.OnPropertyChanged(nameof(context.collection.startupOption)));
            }

            #endregion
            #region Active scene

            void SetupActiveScene() =>
                view.Q<ScenePicker>("picker-active-scene").SetScenes(context.collection.scenes);

            #endregion
            #region Load priority

            void SetupLoadPriority()
            {

                var descriptionExpanded = view.Q("description-expanded");

                bool isVisible = false;
                var button = view.Q<Button>("button-loading-priority-description");
                button.clicked += () => Toggle();

                void Toggle()
                {
                    isVisible = !isVisible;
                    descriptionExpanded.SetVisible(isVisible);
                    button.text = isVisible ? "Read less" : "Read more";

                    view.schedule.Execute(() => view.GetAncestor<ScrollView>().verticalScroller.value = view.GetAncestor<ScrollView>().verticalScroller.highValue).ExecuteLater(1);
                }

            }

            #endregion

        }

    }

}
