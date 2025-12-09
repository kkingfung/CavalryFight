using AdvancedSceneManager.Editor.UI;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class PopupHeader : VisualElement
    {

        [UxmlAttribute]
        public string title
        {
            get => label.text;
            set => label.text = value;
        }

        [UxmlAttribute]
        public bool displayCloseButton
        {
            get => closeButton.style.display == DisplayStyle.Flex;
            set => closeButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [UxmlAttribute]
        public bool displayBackButton
        {
            get => backButton.style.display == DisplayStyle.Flex;
            set => backButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
        Label label;
        VisualElement container;
        Button closeButton;
        Button backButton;

        readonly EventCallback<ClickEvent> ClosePopup = new(e => ASMWindow.ClosePopup());

        public override VisualElement contentContainer => container;

        public PopupHeader()
        {

            label = new Label { name = "label-title" };
            container = new VisualElement() { name = "container" };
            backButton = new Button() { name = "button-back", text = "←", tooltip = "Go back" };
            closeButton = new Button() { name = "button-close", text = "\uf00d", tooltip = "Close popup" };
            closeButton.UseFontAwesome();

            hierarchy.Add(backButton);
            hierarchy.Add(label);
            hierarchy.Add(container);
            hierarchy.Add(closeButton);

            closeButton.RegisterCallback(ClosePopup);

            backButton.style.display = DisplayStyle.None;
            displayCloseButton = true;

            style.flexDirection = FlexDirection.Row;
            style.flexShrink = 0;
            container.style.flexDirection = FlexDirection.RowReverse;
            container.style.flexShrink = 1;

        }

        public void RegisterBackButtonClick(EventCallback<ClickEvent> e)
        {
            backButton.RegisterCallback(e);
        }

        public void UnregisterBackButtonClick(EventCallback<ClickEvent> e)
        {
            backButton.UnregisterCallback(e);
        }

        public void RegisterCloseButtonClick(EventCallback<ClickEvent> e)
        {
            closeButton.UnregisterCallback(ClosePopup);
            closeButton.RegisterCallback(e);
        }

        public void UnregisterCloseButtonClick(EventCallback<ClickEvent> e)
        {
            closeButton.UnregisterCallback(e);
        }

    }

}
