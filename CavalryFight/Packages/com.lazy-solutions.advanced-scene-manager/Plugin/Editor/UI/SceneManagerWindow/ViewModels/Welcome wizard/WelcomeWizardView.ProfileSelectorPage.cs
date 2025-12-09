using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView
    {

        public class ProfileSelectorPage : SubPage
        {

            public override string title => "Profiles";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.profileSelector;
            public override bool useScrollView => false;

            string selectedProfile
            {
                get => sessionState.GetProperty<string>(null);
                set => sessionState.SetProperty(value);
            }

            public static Profile profileToActivate;

            ASMListView list;
            protected override void OnAdded()
            {

                view.BindToSettings();

                view.Q("group-force").SetVisible(ProfileUtility.forceProfile);
                view.Q("group-select").SetVisible(!ProfileUtility.forceProfile);

                if (!ProfileUtility.forceProfile)
                    SetupList();

            }

            protected override void OnRemoved()
            {
                list = null;
                selectedProfile = null;
                profilePicker = null;
            }

            public override VisualElement CreateFooterGUI()
            {

                var footer = view.Q("footer");

                var createButton = footer.Q<Button>("button-create");
                createButton.SetVisible(!ProfileUtility.forceProfile);
                createButton.clickable = new(CreateProfile);

                footer.RemoveFromHierarchy();
                footer.Show();

                return footer;

            }

            #region List

            ObjectField profilePicker;
            void SetupList()
            {

                profilePicker = view.Q<ObjectField>("picker-profile");
                profilePicker.SetEnabled(false);

                profilePicker.value = ProfileUtility.defaultProfile;

                if (Profile.TryFind(selectedProfile, out var profile))
                    profilePicker.value = profile;

                if (SceneManager.profile)
                    profilePicker.value = SceneManager.profile;

                OnProfileSelected((Profile)profilePicker.value);

                list = view.Q<ASMListView>();
                list.RegisterClickCallback(item => OnProfileSelected((Profile)item));
                list.RegisterContextMenu((menu, pos, item) => menu.AddItem("Rename...", isChecked: false, () => Rename((Profile)item)));

                Reload();

                RegisterEvent<ProfileAddedEvent>(e => Reload());
                RegisterEvent<ProfileRemovedEvent>(e => Reload());

            }

            void OnProfileSelected(Profile profile)
            {
                if (profilePicker is not null)
                    profilePicker.value = profile;
                selectedProfile = profile ? profile.id : null;
                profileToActivate = profile;
            }

            void Reload()
            {
                if (list is not null)
                    list.itemsSource = SceneManager.assets.profiles.ToList();
            }

            void CreateProfile()
            {
                DependencyInjectionUtility.GetService<IDialogService>().PromptName(value =>
                {
                    try
                    {
                        var profile = Profile.Create(value);
                        profilePicker!.value = profile;
                        OnProfileSelected(profile);
                        ASMWindow.ClosePopup();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }

                },
                onCancel: ASMWindow.ClosePopup);
            }

            void Rename(Profile profile)
            {
                DependencyInjectionUtility.GetService<IDialogService>().PromptName(value =>
                {
                    try
                    {
                        profile.Rename(value);
                        ASMWindow.ClosePopup();
                        Reload();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }

                },
                onCancel: ASMWindow.ClosePopup);
            }

            #endregion

        }

    }

}

