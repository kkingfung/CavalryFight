using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Models;
using System;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup : PageStackPopup<ViewModel, SettingsPopup.MainPage>
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.root;

        public class SubPage : ViewModel, ISettingsPage
        {

            public enum BindTo
            {
                None, ProjectSettings, UserSettings, Profile
            }

            public virtual BindTo bindTo { get; }

            protected override void OnAdded()
            {
                switch (bindTo)
                {
                    case BindTo.None:
                        break;
                    case BindTo.ProjectSettings:
                        view.BindToSettings();
                        break;
                    case BindTo.UserSettings:
                        view.BindToUserSettings();
                        break;
                    case BindTo.Profile:
                        view.BindToProfile();
                        break;
                }
            }

        }

        protected override void OnAdded()
        {

            base.OnAdded();

            if (context.customParam is Type type && type.IsViewModel() && Instantiate(type, out var viewModel))
            {

                if (viewModel.priorPages is not null)
                    foreach (var page in viewModel.priorPages)
                        Push(page, animate: false);

                Push(type);

            }

        }

        protected override void OnRemoved()
        {

            if (SceneManager.profile)
                SceneManager.profile.Save();

            ASMSettings.instance.Save();
            ASMUserSettings.instance.Save();

            base.OnRemoved();

        }

        public void Open(ASMWindow.OpenSettingsPageRequest e)
        {
            if (e.type is null)
                Open();
            else
                Open(e.type);
        }

        public void Open<TPopup>() where TPopup : ViewModel =>
            Open(typeof(TPopup));

        public void Open(Type type = null) =>
            ASMWindow.OpenPopup<SettingsPopup>(new(customParam: type));

    }

}
