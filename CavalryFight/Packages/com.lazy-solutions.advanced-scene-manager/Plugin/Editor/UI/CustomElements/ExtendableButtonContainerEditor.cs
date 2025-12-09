using AdvancedSceneManager.Editor.UI;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class ExtendableButtonContainerEditor : ExtendableButtonContainer
    {

        public ExtendableButtonContainerEditor()
        {
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        protected override bool IsElementVisible(Callback callback) =>
            true;

        protected override VisualElement Wrap(VisualElement element, Callback callback)
        {

            var button = new Button() { userData = callback, tooltip = element.tooltip, viewDataKey = element.viewDataKey };
            button.Add(element);

            element.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);
            element.SetVisible(true);

            UpdateEnabledIndicator(button, callback);
            button.AddToClassList("extendable-element-toggle");

            button.RegisterCallback<AttachToPanelEvent>(e => EditorApplication.update += Update);
            button.RegisterCallback<DetachFromPanelEvent>(e => EditorApplication.update -= Update);

            void Update()
            {
                if (button.tooltip != element.tooltip)
                    button.tooltip = element.tooltip;
            }

            return button;
        }

        #region Handle drag drop

        VisualElement draggedElement;
        VisualElement dummy;
        int originalIndex;
        Vector2 originalPosition;
        bool isDragging;

        const float dragThreshold = 10.0f; // Distance threshold in pixels

        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button != 0)
                return;

            if (!((VisualElement)e.target).ClassListContains("extendable-element-toggle"))
                return;

            // Call BeginDrag to start the dragging process
            BeginDrag(e);
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (draggedElement == null)
                return;

            e.StopImmediatePropagation();
            e.StopPropagation();

            var distanceMoved = Vector2.Distance(e.localPosition, originalPosition);

            if (!isDragging && distanceMoved < dragThreshold)
                return;  // Do nothing until the pointer has moved past the threshold

            // Once we've crossed the threshold, start the drag
            if (!isDragging)
                isDragging = true;

            UpdateDragPosition(e.localPosition);
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (draggedElement == null)
                return;

            e.StopImmediatePropagation();
            e.StopPropagation();

            // Call EndDrag to finalize the drag and cleanup
            EndDrag(e);
        }

        void BeginDrag(PointerDownEvent e)
        {
            e.StopImmediatePropagation();
            e.StopPropagation();

            draggedElement = e.target as VisualElement;
            originalIndex = draggedElement.parent.IndexOf(draggedElement);

            originalPosition = e.localPosition;
            isDragging = false;

            draggedElement.panel.visualTree.RegisterCallback<PointerUpEvent>(OnPointerUp);
            draggedElement.panel.visualTree.RegisterCallback<MouseLeaveWindowEvent>(OnMouseLeaveWindow);
        }

        void EndDrag(PointerUpEvent e)
        {

            if (draggedElement is null)
                return;

            var callback = (Callback)draggedElement.userData;
            if (!isDragging)
                ToggleVisibility(callback);
            else
                SaveOrder(draggedElement, callback);

            draggedElement.ReleasePointer(e.pointerId);
            SetPosition(Position.Relative);
            draggedElement = null;

            dummy?.RemoveFromHierarchy();
            dummy = null;

            isDragging = false;

            if (e != null && e.target != null)
            {
                ((VisualElement)e.target).panel?.visualTree.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                ((VisualElement)e.target).panel?.visualTree.UnregisterCallback<MouseLeaveWindowEvent>(OnMouseLeaveWindow);
            }

            if (e is not null)
            {
                e.StopImmediatePropagation();
                e.StopPropagation();
            }
        }

        void UpdateDragPosition(Vector2 currentPosition)
        {

            if (draggedElement.parent.Children().OfType<Button>().Count() == 1)
                return;

            SetPosition(Position.Absolute, currentPosition.x - (draggedElement.resolvedStyle.width / 2));

            if (dummy is null)
            {
                // Create a dummy element to keep track of the original spot
                dummy = new VisualElement();
                dummy.style.width = draggedElement.resolvedStyle.width + draggedElement.resolvedStyle.marginLeft + draggedElement.resolvedStyle.marginRight;
                draggedElement.parent.Insert(originalIndex, dummy);
            }

            // Reorder the element based on the new position
            VisualElement container = draggedElement.parent;
            foreach (var sibling in container.Children())
            {
                if (sibling == draggedElement) continue;

                if (IsMouseOverElement(currentPosition, sibling))
                {
                    int targetIndex = container.IndexOf(sibling);

                    // For row-reverse, insertion logic needs to consider reverse order
                    if (targetIndex != originalIndex)
                    {
                        container.Remove(draggedElement);
                        originalIndex = targetIndex > originalIndex ? targetIndex - 1 : targetIndex;
                        container.Insert(targetIndex, draggedElement);
                        dummy.RemoveFromHierarchy();
                        container.Insert(originalIndex, dummy);
                    }
                    break;
                }
            }
        }

        void SetPosition(Position position, float left = 0)
        {

            if (draggedElement == null)
                return;

            if (draggedElement.parent?.Children()?.OfType<Button>()?.Count() < 2)
                return;

            draggedElement.style.position = position;
            draggedElement.style.left = position == Position.Absolute ? new Length(left) : StyleKeyword.Auto;
            draggedElement.pickingMode = position == Position.Absolute ? PickingMode.Ignore : PickingMode.Position;

        }

        bool IsMouseOverElement(Vector2 mousePosition, VisualElement element)
        {
            var worldBound = element.localBound;
            return worldBound.Contains(mousePosition);
        }

        void OnMouseLeaveWindow(MouseLeaveWindowEvent evt)
        {
            // Drop the element when the mouse leaves the window by calling EndDrag
            EndDrag(PointerUpEvent.GetPooled());
        }

        void UpdateEnabledIndicator(VisualElement element, Callback callback)
        {
            GetData(callback, out _, out var isVisible);
            element.EnableInClassList("enabled", !callback.attribute.canToggleVisible || isVisible);
            element.EnableInClassList("forced", !callback.attribute.canToggleVisible);
        }

        void ToggleVisibility(Callback callback)
        {

            if (!callback.attribute.canToggleVisible)
                return;

            GetData(callback, out var index, out var isVisible);
            SetData(callback, index, newIsVisible: !isVisible);

            UpdateEnabledIndicator(draggedElement, callback);
            ResetAll(callback.attribute.location);

        }

        void SaveOrder(VisualElement draggedElement, Callback callback)
        {

            var list = draggedElement.parent.Children().Select(e => e.userData).OfType<Callback>().ToList();
            for (int i = 0; i < list.Count; i++)
                SetData(list[i], newIndex: i, save: false);

            SceneManager.settings.user.Save();
            ResetAll(callback.attribute.location);

        }

        #endregion

    }

}
