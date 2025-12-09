using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Editor.UI.Notifications;
using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(PersistentNotificationService))]
    interface IPersistentNotificationService : DependencyInjectionUtility.IInjectable
    {
        void ReloadNotifications();
        void SetAllVisible(bool visible);
        bool forceDisplayAll { get; }
    }

    class PersistentNotificationService : ServiceBase, IPersistentNotificationService
    {

        readonly Dictionary<Type, ViewModel> viewModels = new()
        {
            { typeof(BadScenePathNotification), null },
            { typeof(DuplicateScenesNotification), null },
            { typeof(EditorCoroutinesNotification), null },
            { typeof(GitIgnoreNotification), null },
            { typeof(ImportedBlacklistedSceneNotification), null },
            { typeof(ImportSceneNotification), null },
            { typeof(InvalidSceneNotification), null },
            { typeof(LegacyNotification), null },
            { typeof(NotifyProfileNotification), null },
            { typeof(UnreferencedCollectionsNotification), null },
            { typeof(UntrackedSceneNotification), null },
            { typeof(UpdateNotification), null },
            { typeof(WelcomeNotification), null },
        };

        public bool forceDisplayAll
        {
            get => sessionState.GetProperty(defaultValue: false);
            private set => sessionState.SetProperty(value);
        }

        public void SetAllVisible(bool visible)
        {
            forceDisplayAll = visible;
            ReloadNotifications();
        }

        protected override void OnInitialize() =>
            SceneManager.OnInitialized(() =>
            {

                RegisterEvent<ProfileChangedEvent>(e => ReloadNotifications());

                foreach (var type in viewModels.Keys.ToList())
                    if (ViewModel.Instantiate(type, out var viewModel))
                        viewModels[type] = viewModel;
                    else
                        Log.Error("Could not instantiate " + type.FullName);

                foreach (var viewModel in viewModels.Values)
                    viewModel?.Add(null, ignoreAddedCheck: true);

            });

        protected override void OnDispose()
        {
            foreach (var viewModel in viewModels.Values)
                viewModel?.Remove();
        }

        public void ReloadNotifications()
        {
            EditorApplication.delayCall -= ReloadNotificationsInternal;
            EditorApplication.delayCall += ReloadNotificationsInternal;
        }

        void ReloadNotificationsInternal()
        {
            foreach (var viewModel in viewModels.Values)
                ((IPersistentNotification)viewModel!).ReloadNotification();
        }

    }

}
