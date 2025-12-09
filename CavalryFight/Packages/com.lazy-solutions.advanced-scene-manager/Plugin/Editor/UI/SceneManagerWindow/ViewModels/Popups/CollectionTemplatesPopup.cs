using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class CollectionTemplatesPopup : ListPopup<SceneCollectionTemplate>
    {

        public override string noItemsText { get; } = "No templates, you can create one using + button.";
        public override string headerText { get; } = "Collection templates";
        public override IEnumerable<SceneCollectionTemplate> items => SceneManager.assets.collectionTemplates;

        public override bool displayRenameButton => true;
        public override bool displayRemoveButton => true;
        public override bool displaySortButton => true;

        public override void OnMakeItem(VisualElement element)
        {
            var button = new Button() { name = "button-apply", text = "", tooltip = "Create a new collection from template" };
            button.UseFontAwesome();
            button.RegisterCallback<ClickEvent>(e => Create(element.userData as SceneCollectionTemplate));

            element.Q("container").Add(button);
        }

        void Create(SceneCollectionTemplate template)
        {
            if (!template)
                return;

            SceneManager.profile.CreateCollection(template);
            ASMWindow.ClosePopup();
        }

        public override void OnAdd()
        {

            DependencyInjectionUtility.GetService<IDialogService>().PromptName(value =>
              {
                  SceneCollectionTemplate.CreateTemplate(value);
                  ASMWindow.OpenPopup<CollectionTemplatesPopup>();
              },
              onCancel: ASMWindow.OpenPopup<ProfilePopup>);

        }

        public override void OnSelected(SceneCollectionTemplate template)
        {
            //It is more clear to perform action on button that we add in OnMakeItem instead of here
        }

        public override void OnRename(SceneCollectionTemplate template)
        {

            DependencyInjectionUtility.GetService<IDialogService>().PromptName(
                value: template.title,
                onContinue: value =>
                {
                    template.m_title = value;
                    template.Rename(value);
                    ASMWindow.OpenPopup<CollectionTemplatesPopup>();
                },
                onCancel: ASMWindow.OpenPopup<CollectionTemplatesPopup>);

        }

        public override void OnRemove(SceneCollectionTemplate template)
        {

            DependencyInjectionUtility.GetService<IDialogService>().PromptConfirm(() =>
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(template));
                ASMWindow.OpenPopup<CollectionTemplatesPopup>();
            },
            onCancel: ASMWindow.OpenPopup<CollectionTemplatesPopup>,
            message: $"Are you sure you wish to remove '{template.name}'?");

        }

        public override IEnumerable<SceneCollectionTemplate> Sort(IEnumerable<SceneCollectionTemplate> items, ListSortDirection sortDirection)
        {
            return sortDirection == ListSortDirection.Ascending ? items.OrderBy(p => p.name) : items.OrderByDescending(p => p.name);
        }

    }

}
