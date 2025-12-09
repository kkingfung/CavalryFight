using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(ProfileBindingsService))]
    public interface IProfileBindingsService
    {
        void BindEnabledToProfile(VisualElement element);
    }

    class ProfileBindingsService : ServiceBase, IProfileBindingsService
    {

        protected override void OnInitialize()
        {

            var callback = new EventCallback<GeometryChangedEvent>(e => UpdateProfileElements());

            RegisterEvent<ASMWindowOpenEvent>(e =>
            {
                UpdateProfileElements();
                SceneManagerWindow.rootVisualElement.RegisterCallback(callback);
            });

            RegisterEvent<ASMWindowCloseEvent>(e =>
            {
                UpdateProfileElements();
                SceneManagerWindow.rootVisualElement?.UnregisterCallback(callback);
            });

            UpdateProfileElements();
            ProfileUtility.onProfileChanged += UpdateProfileElements;

        }

        readonly List<VisualElement> profileElements = new();
        public void BindEnabledToProfile(VisualElement element) =>
            profileElements.Add(element);

        void UpdateProfileElements() =>
            profileElements.ForEach(e => e?.SetEnabled(SceneManager.profile));

    }

}