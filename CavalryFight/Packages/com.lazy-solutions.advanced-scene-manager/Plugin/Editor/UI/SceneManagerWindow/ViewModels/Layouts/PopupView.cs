using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Utility;
using System;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    class PopupView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.root;

        internal static event Action onPopupClose;

        public ViewModel current { get; private set; }

        EventCallback<GeometryChangedEvent> contentContainerGeometryChangedCallback = null!;
        EventCallback<GeometryChangedEvent> contentContainerGeometryChangedCallback2 = null!;

        public VisualElement contentContainer = null!;

        SerializableViewModelData persistedViewModel
        {
            get => sessionState.GetProperty<SerializableViewModelData>(default);
            set => sessionState.SetProperty(value);
        }

        protected override void OnAdded()
        {

            DisableTemplateContainer();

            contentContainer = view?.Q("contentContainer") ?? throw new InvalidOperationException("Could not find content container for PopupView.");

            contentContainerGeometryChangedCallback = new(OnGeometryChanged);
            contentContainerGeometryChangedCallback2 = new(OnGeometryChanged2);
            view.RegisterCallback(contentContainerGeometryChangedCallback2);

            view.SetVisible(false);

            view.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == view)
                    Close();
            });

            base.OnAdded();

            var scroll = view.Q<ScrollView>();
            if (scroll is not null)
                scroll.verticalScroller.value = 0;

            RegisterEvent<ASMWindow.OpenPopupRequestEvent>(Open);
            RegisterEvent<ASMWindow.ClosePopupRequestEvent>(Close);

            Open(persistedViewModel);

            view.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (current is not null)
                    persistedViewModel = Serialize(current);
            });

        }

        protected override void OnRemoved()
        {
            _ = current?.Remove();
            current = null;
        }

        void OnGeometryChanged2(GeometryChangedEvent e)
        {
            contentContainer.style.maxHeight = view.resolvedStyle.height - 60;
        }

        #region Open

        public void Open(SerializableViewModelData persistedViewModel)
        {
            if (TryDeserialize(persistedViewModel, out var viewModel) && viewModel.remainOpenAsPopupAfterDomainReload)
                Open(viewModel.GetType(), null, viewModel.context, animate: false);
        }

        public void Open(ASMWindow.OpenPopupRequestEvent e) =>
            Open(e.type, e.context);

        public void Open<T>(ViewModelContext? context = null) where T : ViewModel =>
            Open(typeof(T), context);

        public void Open(Type type, ViewModelContext? context = null) =>
            Open(type, null, context);

        void Open(Type type, ViewModel viewModel = null, ViewModelContext? context = null, bool animate = true)
        {

            if (!type.IsViewModel())
                throw new InvalidOperationException($"'{type.FullName}' is not a valid view model.");

            if (current?.GetType() == type && type != typeof(SettingsPopup))
                return;

            if (current is not null)
                Close(animate: false);

            VisualElement view;

            if (viewModel is null)
            {
                if (!Instantiate(type, out viewModel, out view))
                    throw new InvalidOperationException($"Could not instantiate '{type.FullName}'.");
            }
            else
            {
                view = viewModel.CreateGUI() ?? throw new InvalidOperationException($"'{type.FullName}'.CreateGUI() did not return a visual element.");
            }

            current = viewModel;
            ASMWindow.currentPopup = type;
            persistedViewModel = Serialize(viewModel);

            contentContainer.Clear();
            contentContainer.Add(view);

            viewModel.Add(view, context);

            // ClosePopup() can be called to cancel during Add callback
            if (current is null)
                return;

            if (animate)
                AnimateOpen();
            this.view.SetVisible(true);

        }

        #endregion
        #region Close

        public void Close(ASMWindow.ClosePopupRequestEvent e) =>
            Close();

        public void Close(bool animate = true)
        {

            if (current is null)
                return;

            persistedViewModel = default;
            ASMWindow.currentPopup = null;

            var popup = current;
            current = null;

            if (animate)
                AnimateClose(OnCompleted);
            else
                OnCompleted();

            void OnCompleted()
            {

                if (popup is not null)
                {
                    popup.view?.RemoveFromHierarchy();
                    _ = popup.Remove();
                }

                view.SetVisible(false);
                contentContainer.Clear();

                onPopupClose?.Invoke();

            }

        }

        #endregion
        #region Animations

        void AnimateOpen() =>
            contentContainer.RegisterCallback(contentContainerGeometryChangedCallback);

        void OnGeometryChanged(GeometryChangedEvent e)
        {

            view.style.opacity = 0;

            contentContainer.UnregisterCallback(contentContainerGeometryChangedCallback);
            VisualElementUtility.Animate(
                onComplete: null,
                view.Fade(1, 0.2f),
                contentContainer.AnimateBottom(-view.resolvedStyle.height + view.resolvedStyle.marginBottom, 0, 0.15f));

        }

        void AnimateClose(Action onComplete = null) =>
            VisualElementUtility.Animate(onComplete, view.Fade(0));

        #endregion

    }

}
