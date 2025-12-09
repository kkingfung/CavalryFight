using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Services;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(NotificationService))]
    interface INotificationService
    {
        IEnumerable<Notification> overflowItems { get; }
        IEnumerable<Notification> visibleItems { get; }
        Dictionary<NotificationKind, (string fontAwesomeIcon, string font)> Icons { get; }
        IEnumerable<Notification> mutedItems { get; }

        void ClearNotification(Notification notification);
        void ClearNotification(string id);
        VisualElement GenerateView(Notification notification);
        void MuteNotification(Notification notification);
        void UnmuteNotification(Notification notification);
        bool IsMuted(Notification notification);
        void Notify(Notification notification);
    }

    class NotificationService : ServiceBase, INotificationService
    {

        public Dictionary<NotificationKind, (string fontAwesomeIcon, string font)> Icons { get; } = new()
        {
            { NotificationKind.Default, ("", "") },
            { NotificationKind.Info, ("\uf129", "") },
            { NotificationKind.FixUp, ("\uf0ad","") },
            { NotificationKind.Scene, ("\ue049", "fontAwesomeBrands") },
            { NotificationKind.Link, ("\uf08e", "") },
            { NotificationKind.Profile, ("\uf187", "") },
        };

        record NotificationInfo(Notification notification, bool isPriority);

        readonly Dictionary<string, NotificationInfo> notifications = new();
        public IEnumerable<Notification> visibleItems => notifications.Values.Where(n => IsVisibleInMain(n)).Select(n => n.notification).OrderBy(n => n.id).ToList();
        public IEnumerable<Notification> overflowItems => notifications.Values.Where(n => IsVisibleInOverflow(n)).Select(n => n.notification).OrderBy(n => n.id).ToList();
        public IEnumerable<Notification> mutedItems => notifications.Values.Where(n => IsMuted(n.notification)).Select(n => n.notification).OrderBy(n => n.id).ToList();

        bool isInPriorityMode;

        protected override void OnInitialize()
        {
            RegisterEvent<ASMWindow.AddNotificationRequestEvent>(e => Notify(e.notification));
            RegisterEvent<ASMWindow.RemoveNotificationRequestEvent>(e => ClearNotification(e.notification));
        }

        bool IsVisibleInMain(NotificationInfo n)
        {

            if (IsMuted(n.notification))
                return false;

            if (isInPriorityMode)
                return n.isPriority; // all priorities go main, none overflow
                                     // normal mode: max 2 visible, others overflow
            var nonPriority = notifications.Values.Where(x => !x.isPriority).ToList();
            var priority = notifications.Values.Where(x => x.isPriority).ToList();
            var slots = 2;

            // fill with priorities first
            var visible = priority.Take(slots).ToList();
            slots -= visible.Count;
            visible.AddRange(nonPriority.Take(slots));
            return visible.Contains(n);

        }

        bool IsVisibleInOverflow(NotificationInfo n)
        {

            if (IsMuted(n.notification))
                return false;

            if (isInPriorityMode)
                return !n.isPriority; // everything non-priority goes overflow

            return !IsVisibleInMain(n);

        }

        void UpdateIsInPriorityMode() =>
            isInPriorityMode = notifications.Values.Any(n => n.isPriority && !IsMuted(n.notification));

        public void Notify(Notification notification)
        {
            ClearNotification(notification);

            var isPriority = notification.importance is NotificationImportance.Priority;
            notifications[notification.id] = new(notification, isPriority);

            UpdateIsInPriorityMode();
            SceneManager.events.InvokeCallbackSync<NotificationsChangedEvent>();
        }

        public void MuteNotification(Notification notification)
        {
            if (!IsMuted(notification))
            {
                SceneManager.settings.user.m_mutedNotifications.Add(notification.id);
                SceneManager.settings.user.Save();

                UpdateIsInPriorityMode();
                SceneManager.events.InvokeCallbackSync<NotificationsChangedEvent>();
            }
        }

        public void UnmuteNotification(Notification notification)
        {
            if (SceneManager.settings.user.m_mutedNotifications.Remove(notification.id))
            {
                SceneManager.settings.user.Save();
                UpdateIsInPriorityMode();
                SceneManager.events.InvokeCallbackSync<NotificationsChangedEvent>();
            }
        }

        public bool IsMuted(Notification notification) =>
              SceneManager.settings.user.m_mutedNotifications.Contains(notification.id);

        public void ClearNotification(Notification notification) =>
            ClearNotification(notification.id);

        public void DismissNotification(Notification notification)
        {

            if (!notification.canDismiss)
                return;

            ClearNotification(notification.id);
            notification.onDismiss?.Invoke();

        }

        public void ClearNotification(string id)
        {
            if (notifications.Remove(id))
            {
                UpdateIsInPriorityMode();
                SceneManager.events.InvokeCallbackSync<NotificationsChangedEvent>();
            }
        }

        public VisualElement GenerateView(Notification notification)
        {

            var view = SceneManagerWindow.window!.viewLocator.items.notification.CloneTree();
            var mainButton = view.Q<Button>("button-main");

            mainButton.EnableInClassList(nameof(Notification.allowTextClippingIntoMenuButton), notification.allowTextClippingIntoMenuButton);

            mainButton.RegisterCallback<ClickEvent>(e =>
            {

                notification.onClick?.Invoke();

                if (notification.canDismiss && notification.dismissOnClick)
                    ClearNotification(notification);

            });

            view.Q<Button>("button-menu").RegisterCallback<ClickEvent>(e =>
            {

                e.StopPropagation();
                e.StopImmediatePropagation();

                ShowMenu();

            });

            view.Q("button-main").AddToClassList(notification.kind.ToString("G").ToLowerInvariant());
            view.Q("button-main").EnableInClassList("muted", IsMuted(notification));

            view.Q<Label>("label-text").text = notification.message;

            SetupIcon();

            view.Q("icon-expanded").SetVisible(notification.isExpanded ?? false);
            view.Q("icon-collapsed").SetVisible(!(notification.isExpanded ?? true));

            return view;

            void SetupIcon()
            {

                var iconLabel = view.Q<Label>("icon-info");

                iconLabel.text =
                    !string.IsNullOrEmpty(notification.fontAwesomeIcon)
                    ? notification.fontAwesomeIcon
                    : Icons[notification.kind].fontAwesomeIcon;

                if (!string.IsNullOrEmpty(notification.iconFont))
                    iconLabel.AddToClassList(notification.iconFont);
                else if (!string.IsNullOrEmpty(Icons[notification.kind].font))
                    iconLabel.AddToClassList(Icons[notification.kind].font);

                iconLabel.tooltip =
                    string.IsNullOrEmpty(notification.iconInfo)
                    ? ObjectNames.NicifyVariableName(notification.kind.ToString())
                    : notification.iconInfo;

                if (notification.kind is NotificationKind.Scene)
                {
                    iconLabel.text = Icons[notification.kind].fontAwesomeIcon;
                    iconLabel.tooltip = "Scene import";
                }

                iconLabel.SetVisible(!string.IsNullOrEmpty(iconLabel.text));

            }

            void ShowMenu()
            {
                var menu = new GenericDropdownMenu();

                if (notification.canDismiss)
                    menu.AddItem("Dismiss", false, () => DismissNotification(notification));
                else
                    menu.AddDisabledItem("Dismiss", false);

                if (!notification.canMute)
                    menu.AddDisabledItem("Mute", false);
                else if (!IsMuted(notification))
                    menu.AddItem("Mute", false, () => MuteNotification(notification));
                else
                    menu.AddItem("Unmute", false, () => UnmuteNotification(notification));

                menu.DropDown(view.Q<Button>("button-menu").worldBound, view, anchored: false);
            }

        }

    }

}
