using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class PageStackView : VisualElement
    {

        readonly Stack<ViewModel> history = new();
        readonly VisualElement container;

        public ViewModel current => history.Count > 0 ? history.Peek() : null;
        public PopupHeader header { get; } = new();

        public ViewModelContext? parentContext { get; set; }

        public override VisualElement contentContainer => header.contentContainer;

        readonly EventCallback<AttachToPanelEvent> attachToPanelEvent;
        readonly EventCallback<DetachFromPanelEvent> detachFromPanelEvent;

        public PageStackView()
        {

            style.flexGrow = 1;
            style.flexShrink = 1;
            style.flexDirection = FlexDirection.Column;

            header.RegisterBackButtonClick(e => Pop());
            hierarchy.Add(header);

            container = new VisualElement() { name = "container" };
            container.style.flexGrow = 1;
            container.style.position = Position.Relative;
            hierarchy.Add(container);

            RegisterCallback(attachToPanelEvent = new(OnAttached));
            RegisterCallback(detachFromPanelEvent = new(OnDetached));
            AddDevMenu();

        }

        History? savedHistory;

        void OnDetached(DetachFromPanelEvent e)
        {
            // Persist stack *before* calling Remove
            GetHistory(out var h);
            savedHistory = h;

            // Clean up correctly
            foreach (var viewModel in history)
                _ = viewModel.Remove();

            container.Clear();
        }

        void OnAttached(AttachToPanelEvent e)
        {
            if (savedHistory is { } h)
            {
                RestoreHistory(h);
                savedHistory = null;
            }
        }

        public void Insert<T>(int index) where T : ViewModel
        {
            var type = typeof(T);

            var viewModel = ViewModel.Instantiate(type)
                ?? throw new InvalidOperationException($"'{type.FullName}' could not be opened.");

            // Rebuild history with insertion
            var list = history.Reverse().ToList(); // oldest → newest
            index = Mathf.Clamp(index, 0, list.Count);

            list.Insert(index, viewModel);

            history.Clear();
            foreach (var vm in list.AsEnumerable().Reverse()) // newest → oldest
                history.Push(vm);

            // Do NOT show/activate this view – just keep it in history.
            // That means no GetView(), no AnimateIn().

            UpdateHeader();
            OnHistoryChanged(EventType.Navigation);
        }

        public void ResetStack()
        {
            foreach (var viewModel in history)
                _ = viewModel.Remove();
            history.Clear();
            container.Clear();
        }

        #region Persistence

        public enum EventType
        {
            Navigation = 0,
            Scroll = 1,
        }

        public record HistoryChangedEvent(History history, EventType eventType);

        [Serializable]
        public struct History
        {

            public History(SerializableViewModelData[] items) =>
                this.items = items;

            public SerializableViewModelData[] items;

        }

        public void GetHistory(out History history) =>
            history = new History(this.history.Reverse().Select(ViewModel.Serialize).OfType<SerializableViewModelData>().ToArray());

        public void RestoreHistory(string json)
        {
            var history = JsonUtility.FromJson<History>(json);
            RestoreHistory(history);
        }

        public void RestoreHistory(History history)
        {

            var viewModels = history.items.Select(ViewModel.Deserialize).OfType<ViewModel>().ToList();

            if (viewModels.Count == 0)
                return;

            this.history.Clear();
            container.Clear();

            var latest = viewModels.Last();
            viewModels.Remove(latest);

            foreach (var viewModel in viewModels)
                this.history.Push(viewModel);

            Push(latest, latest.GetType(), animate: false);

        }

        readonly List<EventCallback<HistoryChangedEvent>> historyCallbacks = new();
        public void RegisterHistoryChangedEvent(EventCallback<HistoryChangedEvent> callback)
        {
            historyCallbacks.Add(callback);
        }

        void OnHistoryChanged(EventType type)
        {
            GetHistory(out var history);
            foreach (var callback in historyCallbacks)
            {
                callback?.Invoke(new(history, type));
            }
        }

        #endregion
        #region Navigation

        VisualElement GetView(ViewModel viewModel, Type type)
        {

            if (viewModel is null)
                throw new InvalidOperationException($"'{type.FullName}' could not be opened.");

            if (viewModel.view is not null)
                return viewModel.view;

            var viewTemplate = viewModel.CreateGUI() ?? throw new InvalidOperationException($"'{type.FullName}' could not be opened.");
            var view = WrapInContainer(viewTemplate, viewModel);

            container.Add(view);
            viewModel.Add(view, parentContext);

            return view;

        }

        VisualElement WrapInContainer(VisualElement view, ViewModel viewModel)
        {

            if (!viewModel.useScrollView)
                return view;

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.Add(view);

            RestoreScroll();
            PersistScroll();

            return scroll;

            void RestoreScroll()
            {

                //Spam set scroll pos in GeometryChangedEvent, this prevents scrollbar from jumping around
                scroll.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

                void OnGeometryChanged(GeometryChangedEvent e)
                {

                    scroll.verticalScroller.value = viewModel.scrollPos ?? 0f;
                    //Debug.Log($"{viewModel.GetType().Name} (restore): {viewModel.scrollPos}");

                    //Unregistering after an arbitary delay seems to work (RegisterCallbackOnce<GeometryChangedEvent> does not work)
                    scroll.schedule.
                        Execute(() => scroll.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged)).
                        ExecuteLater(1);

                }
            }

            void PersistScroll()
            {

                scroll.verticalScroller.valueChanged += VerticalScroller_valueChanged;

                scroll.RegisterCallback<DetachFromPanelEvent>(_ => scroll.verticalScroller.valueChanged -= VerticalScroller_valueChanged);

                //Scroll is saved in serialized history
                void VerticalScroller_valueChanged(float value)
                {
                    if (isNavigating)
                        return;

                    viewModel.scrollPos = value;
                    //Debug.Log($"{viewModel.GetType().Name} (save): {viewModel.scrollPos}");
                    OnHistoryChanged(EventType.Scroll);
                }
            }

        }

        void UpdateHeader()
        {
            header.title = current?.title ?? ObjectNames.NicifyVariableName(current?.GetType()?.Name?.Replace("Popup", "")?.Replace("View", ""));
            header.displayBackButton = history.Count > 1;
        }

        void AddHeaderView(ViewModel viewModel)
        {

            viewModel.headerView?.RemoveFromHierarchy();

            if (viewModel is not null)
            {

                viewModel.headerView = viewModel.CreateHeaderGUI();

                if (viewModel.headerView is not null)
                    header.Add(viewModel.headerView);

            }

        }

        void RemoveHeaderView(ViewModel viewModel)
        {

            if (viewModel is null)
                return;

            viewModel.headerView?.RemoveFromHierarchy();
            viewModel.headerView = null;

        }

        #endregion
        #region Forward navigation

        public void Push<T>(bool animate = true) where T : ViewModel =>
            Push(typeof(T), animate);

        public void Push(Type type, bool animate = true)
        {

            if (current is not null && current?.GetType() == type)
                return;

            var viewModel = ViewModel.Instantiate(type);
            Push(viewModel, type, animate);

        }

        void Push(ViewModel viewModel, Type type, bool animate = true)
        {

            if (viewModel is null)
                throw new InvalidOperationException($"'{type.FullName}' could not be opened.");

            if (current?.GetType() == type)
                return;

            var view = GetView(viewModel, type) ?? throw new InvalidOperationException("Page must have a view before being pushed.");

            EnqueueAnimation(() =>
            {

                view.style.opacity = 1f;
                bool hasPrevious = history.Count > 0;

                if (hasPrevious)
                {
                    var current = history.Peek();
                    if (current.view is null)
                        GetView(current, current.GetType());
                    current.view!.style.position = Position.Absolute;

                    RemoveHeaderView(current);
                    AnimateOut(current.view, async () => await current.Remove(), toLeft: true, animate: animate);
                }

                AddHeaderView(viewModel);
                AnimateIn(view, fromLeft: true, animate: hasPrevious && animate, onComplete: () =>
                {
                    AnimationComplete();
                    viewModel.OnAddAnimationComplete();
                });

                history.Push(viewModel);
                UpdateHeader();

                OnHistoryChanged(EventType.Navigation);

            });

        }

        #endregion
        #region Backward navigation

        public void Pop()
        {

            if (history.Count <= 1)
                return;

            EnqueueAnimation(() =>
            {

                if (history.Count <= 1)
                {
                    AnimationComplete();
                    return; // double-check to avoid popping the initial page
                }

                var closingPage = history.Pop();
                if (!history.TryPeek(out var returningPage))
                {
                    AnimationComplete();
                    return;
                }

                GetView(returningPage, returningPage.GetType());
                GetView(closingPage, closingPage.GetType());

                closingPage.view.style.position = Position.Absolute;
                RemoveHeaderView(closingPage);
                AnimateOut(closingPage.view, async () =>
                {
                    await closingPage.Remove();
                    AnimationComplete(); // Signals the next animation can run
                }, toLeft: false, animate: true);

                var view = GetView(returningPage, returningPage.GetType());
                AddHeaderView(returningPage);
                AnimateIn(view, fromLeft: false, animate: true, onComplete: () => { returningPage.OnAddAnimationComplete(); });

                UpdateHeader();
                OnHistoryChanged(EventType.Navigation);

            });

        }

        #endregion
        #region Animations

        public bool isNavigating => animationQueue.Any() || animationInProgress;

        readonly Queue<Action> animationQueue = new();
        bool animationInProgress = false;

        void EnqueueAnimation(Action animation)
        {
            if (animationInProgress && history.Count <= 1)
                return; // Don't queue pop when only one page remains

            animationQueue.Enqueue(animation);
            TryRunNextAnimation();
        }

        void TryRunNextAnimation()
        {
            if (animationInProgress || animationQueue.Count == 0)
                return;

            animationInProgress = true;
            var next = animationQueue.Dequeue();
            next.Invoke();
        }

        void AnimationComplete()
        {
            animationInProgress = false;
            TryRunNextAnimation();
        }

        static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        void AnimateIn(VisualElement view, Action onComplete = null, bool fromLeft = true, bool animate = true)
        {
            if (!animate)
            {
                view.style.left = new Length(0, LengthUnit.Percent);
                view.style.opacity = 1f;
                view.Show();
                onComplete?.Invoke();
                return;
            }

            float duration = 0.25f;
            float elapsed = 0f;
            int direction = fromLeft ? 1 : -1;

            view.style.top = 0;
            view.style.bottom = 0;
            view.style.left = new Length(100 * direction, LengthUnit.Percent);
            view.style.right = 0;
            view.style.width = Length.Percent(100);
            view.style.height = Length.Percent(100);
            view.style.opacity = 0f;
            view.Show();

            container.schedule.Execute(() =>
            {
                elapsed += 0.01f;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                float x = Mathf.Lerp(100 * direction, 0, eased);
                float opacity = Mathf.Lerp(0f, 1f, eased);

                view.style.left = new Length(x, LengthUnit.Percent);
                view.style.opacity = opacity;

                if (t >= 1f)
                {
                    view.style.left = new Length(0, LengthUnit.Percent);
                    view.style.opacity = 1f;
                    onComplete?.Invoke();
                }
            }).Every(10).Until(() => elapsed >= duration);
        }

        void AnimateOut(VisualElement view, Action onComplete = null, bool toLeft = true, bool animate = true)
        {

            if (!animate)
            {
                view.Hide();
                view.style.left = new(); // Reset
                view.style.opacity = 1f;
                view.style.position = Position.Relative;
                onComplete?.Invoke();
                return;
            }

            float duration = 0.25f;
            float elapsed = 0f;
            int direction = toLeft ? -1 : 1;

            container.schedule.Execute(() =>
            {
                elapsed += 0.01f;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                float x = Mathf.Lerp(0, 100 * direction, eased);
                float opacity = Mathf.Lerp(1f, 0f, eased);

                view.style.left = new Length(x, LengthUnit.Percent);
                view.style.opacity = opacity;

                if (t >= 1f)
                {
                    view.Hide();
                    view.style.left = new(); // Reset
                    view.style.opacity = 1f;
                    view.style.position = Position.Relative;
                    onComplete?.Invoke();
                }
            }).Every(10).Until(() => elapsed >= duration);
        }

        #endregion
        #region Dev Menu

        void AddDevMenu()
        {
#if ASM_DEV
            header.Q<Label>().ContextMenu(e =>
            {
                var template = current?.template;
                if (!template)
                    template = (current?.view as TemplateContainer)?.templateSource;

                if (template)
                {
                    e.menu.AppendAction(
                        "View view template",
                        _ => AssetDatabaseUtility.ShowFolder(template),
                        DropdownMenuAction.Status.Normal);
                }

                // Resolve the "main" type (outermost declaring type)
                var type = current?.GetType();
                var searchType = type;
                while (searchType?.DeclaringType != null)
                    searchType = searchType.DeclaringType;

                // Find the MonoScript for the main type
                var asset = searchType != null
                    ? MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(s => s.GetClass() == searchType)
                    : null;

                e.menu.AppendAction(
                    "View view model",
                    _ => AssetDatabaseUtility.ShowFolder(asset),
                    asset ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            });
#endif
        }

        #endregion

    }

}
