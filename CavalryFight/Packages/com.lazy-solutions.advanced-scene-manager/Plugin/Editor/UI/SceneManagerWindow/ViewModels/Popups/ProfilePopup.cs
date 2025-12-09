using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if ASM_CHILD_PROFILES
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Services;
#endif

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class ProfilePopup : ListPopup<Profile>
    {

        public override string noItemsText { get; } = "No profiles, you can create one using + button.";
        public override string headerText { get; } = "Profiles";
        public override IEnumerable<Profile> items => SceneManager.assets.profiles.OrderBy(p => p.name);

        public override bool displayRenameButton => true;
        public override bool displayRemoveButton => true;
        public override bool displayDuplicateButton => true;
        public override bool displaySortButton => true;

        protected override void OnAdded()
        {
            base.OnAdded();
            RegisterEvent<ScenesAvailableForImportChangedEvent>(e => Reload());
        }

        public override void OnAdd()
        {

            DependencyInjectionUtility.GetService<IDialogService>().PromptName(async value =>
            {

                try
                {
                    ((SceneManagerWindow)window).mainView.Show<ProgressSpinnerView>();
                    await Task.Delay(250);

                    ProfileUtility.SetProfile(Profile.Create(value));
                    ASMWindow.ClosePopup();

                    ((SceneManagerWindow)window).mainView.Hide<ProgressSpinnerView>();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

            },
            onCancel: ASMWindow.OpenPopup<ProfilePopup>);

        }

        public override async void OnSelected(Profile profile)
        {

            ((SceneManagerWindow)window).mainView.Show<ProgressSpinnerView>();
            ASMWindow.ClosePopup();
            await Task.Delay(250);

            ProfileUtility.SetProfile(profile);

            ((SceneManagerWindow)window).mainView.Hide<ProgressSpinnerView>();

        }

        public override void OnRename(Profile profile)
        {

            DependencyInjectionUtility.GetService<IDialogService>().PromptName(
                value: profile.name,
                onContinue: (value) =>
                {
                    profile.Rename(value);
                    ASMWindow.OpenPopup<ProfilePopup>();
                },
                onCancel: ASMWindow.OpenPopup<ProfilePopup>);

        }

        public override void OnRemove(Profile profile)
        {

            DependencyInjectionUtility.GetService<IDialogService>().PromptConfirm(async () =>
            {
                if (SceneManager.profile == profile)
                {
                    ((SceneManagerWindow)window).mainView.Show<ProgressSpinnerView>();
                    await Task.Delay(250);
                }

                Profile.Delete(profile);
                ASMWindow.OpenPopup<ProfilePopup>();

                ((SceneManagerWindow)window).mainView.Hide<ProgressSpinnerView>();
            },
            onCancel: ASMWindow.OpenPopup<ProfilePopup>,
            message: $"Are you sure you wish to remove '{profile.name}'?");

        }

        public override void OnDuplicate(Profile profile)
        {
            Profile.Duplicate(profile);
            Reload();
        }

#if ASM_CHILD_PROFILES
        public override IEnumerable<MenuButtonItem> CustomMenuItems(Profile profile)
        {
            if (SceneManager.profile.childProfiles.Contains(profile))
                yield return new("Remove as child profile", () => RemoveChildProfile(profile));
            else
                yield return new("Add as child profile", () => AddChildProfile(profile));
        }

        void AddChildProfile(Profile profile)
        {
            SceneManager.profile.AddChildProfile(profile);
            ServiceUtility.Get<IChildProfilesService>().Reload();
        }

        void RemoveChildProfile(Profile profile)
        {
            SceneManager.profile.RemoveChildProfile(profile);
            ServiceUtility.Get<IChildProfilesService>().Reload();
        }
#endif

        public override IEnumerable<Profile> Sort(IEnumerable<Profile> items, ListSortDirection sortDirection)
        {
            return sortDirection == ListSortDirection.Ascending ? items.OrderBy(p => p.name) : items.OrderByDescending(p => p.name);
        }

    }

}
