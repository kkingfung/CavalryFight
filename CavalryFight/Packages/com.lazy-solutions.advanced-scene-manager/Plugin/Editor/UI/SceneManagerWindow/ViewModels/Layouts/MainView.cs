using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Editor.UI
{

    class MainView : ViewModel
    {

        readonly Dictionary<Type, ViewModel> viewModels = new()
        {
            { typeof(HeaderView), null },
            { typeof(SearchView), null },
            { typeof(CollectionView), null },
            { typeof(UndoView), null },
            { typeof(NotificationView), null },
            { typeof(SelectionView), null },
#if ASM_CHILD_PROFILES
            { typeof(ChildProfilesView), null },
#endif
            { typeof(FooterView), null },
            { typeof(PopupView), null },
        };

        readonly SettingsPopup settingsPopup = new();

        protected override void OnAdded()
        {

            //SettingsPopup needs to be initialized, but shouldn't add its view until a settings page is actually opened.
            //Injecting here allows us to initialize it in OnAdded.
            ServiceUtility.Register(settingsPopup);

            AddStyles();
            ShowViews();

            RegisterEvent<ASMWindow.OpenSettingsPageRequest>(settingsPopup.Open);

            if (!SceneManager.settings.user.hasCompletedWelcomeWizard)
                Show<WelcomeWizardView>();

        }

        protected override void OnRemoved()
        {
            foreach (var viewModel in viewModels.Values)
                viewModel?.Remove();
        }

        void AddStyles()
        {

            var styles = ((SceneManagerWindow)window).viewLocator.styles.Enumerate();
            if (!styles.Any())
                Log.Error("Could not find any styles for the scene manager window. You may try re-importing or re-installing ASM.");

            foreach (var style in styles)
                view.styleSheets.Add(style);

        }

        void ShowViews()
        {
            foreach (var type in viewModels.Keys.ToList())
                Show(type);
        }

        public void Show<T>(ViewModelContext? context = null) where T : ViewModel =>
            Show(typeof(T), context);

        void Show(Type type, ViewModelContext? context = null)
        {

            if (viewModels.GetValueOrDefault(type)?.isAdded ?? false)
                return;

            var viewModel = Instantiate(type);
            var view = viewModel.CreateGUI();

            if (view is not null)
            {
                if (viewModels[typeof(PopupView)] is PopupView popupView && popupView.view is not null)
                    this.view.Insert(this.view.IndexOf(popupView.view), view);
                else
                    this.view.Add(view);
            }

            viewModel.Add(view, context);
            viewModels[type] = viewModel;

        }

        public void Hide<T>() where T : ViewModel =>
            Hide(typeof(T));

        async void Hide(Type type)
        {

            if (!viewModels.TryGetValue(type, out var viewModel) || !viewModel!.isAdded)
                return;

            await viewModel.Remove();

        }

        public T GetViewModel<T>() where T : ViewModel
        {
            return viewModels.GetValueOrDefault(typeof(T)) as T;
        }

    }

}
