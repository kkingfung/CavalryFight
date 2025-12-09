using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Editor
{

    /// <summary>A <see cref="ObjectField"/> that only accepts <see cref="Scene"/>, with support for <see cref="SceneAsset"/> drag drop.</summary>
    [UxmlElement]
    public partial class SceneField : ObjectField, INotifyValueChanged<Scene>
    {

        public SceneField()
        {
            SetupObjectField();
            SetupDragDropTarget();
            SetupPointerEvents();
            SetupTooltip();
        }

        #region Object field

        public void SetObjectPickerEnabled(bool value) =>
            this.Q(className: "unity-base-field__input").SetEnabled(value);

        void SetupObjectField()
        {
            allowSceneObjects = false;
            objectType = typeof(Scene);
            RegisterCallback<ChangeEvent<Object>>(OnValueChanged);
        }

        void OnValueChanged(ChangeEvent<Object> e)
        {
            using var ev = ChangeEvent<Scene>.GetPooled(e.previousValue as Scene, e.newValue as Scene);
            ev.target = this;
            SendEvent(ev);
        }

        public Scene _value
        {
            get => base.value as Scene;
            set => base.value = value;
        }

        Scene INotifyValueChanged<Scene>.value
        {
            get => _value;
            set => _value = value;
        }

        public void SetValueWithoutNotify(Scene newValue)
        {

            if (_value) _value.PropertyChanged -= Value_PropertyChanged;
            if (newValue) newValue.PropertyChanged += Value_PropertyChanged;

            base.SetValueWithoutNotify(newValue);

        }

        void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshTooltip();
        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<Scene>> callback) =>
            INotifyValueChangedExtensions.RegisterValueChangedCallback(this, callback);

        public void UnregisterValueChangedCallback(EventCallback<ChangeEvent<Scene>> callback) =>
            INotifyValueChangedExtensions.UnregisterValueChangedCallback(this, callback);

        #endregion
        #region Pointer events

        public delegate void OnClick(PointerDownEvent e);

        OnClick onClick;
        public void OnClickCallback(OnClick onClick) =>
            this.onClick = onClick;

        void SetupPointerEvents()
        {

            var element = this.Q(className: "unity-object-field__object");
            var clickCount = 0;

            element.RegisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerLeaveEvent>(PointerLeave);
            element.RegisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);

            void PointerDown(PointerDownEvent e)
            {

                if (e.button != 0)
                    return;

                clickCount = e.clickCount;
                element.CapturePointer(e.pointerId);

                e.StopPropagation();

#if !UNITY_2023_1_OR_NEWER
                e.PreventDefault();
#endif

                onClick?.Invoke(e);

            }

            void PointerLeave(PointerLeaveEvent e)
            {

                if (clickCount == 1 && element.HasPointerCapture(e.pointerId))
                    StartDrag();

                clickCount = 0;
                element.ReleasePointer(e.pointerId);

            }

            void PointerUp(PointerUpEvent e)
            {

                if (clickCount > 0 && value && element.HasPointerCapture(e.pointerId))
                    PingAsset();

                element.ReleasePointer(e.pointerId);
                clickCount = 0;

            }

        }

        void StartDrag()
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { _value };
            DragAndDrop.StartDrag("Scene drag:" + _value.name);
        }

        /// <summary>Pings the associated SceneAsset in project window.</summary>
        public void PingAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(((Scene)_value).path);
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>Opens the associated SceneAsset.</summary>
        public void OpenAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(((Scene)_value).path);
            _ = AssetDatabase.OpenAsset(asset);
            Selection.activeObject = asset;
        }

        #endregion
        #region Drag drop target

        void SetupDragDropTarget()
        {

            //This fixes a bug where dropping a scene one pixel above this element would result in null being assigned to this field
            var element = this.Q(className: "unity-object-field-display");
            if (element == null)
                return;

            element.RegisterCallback<DragUpdatedEvent>(DragUpdated, TrickleDown.TrickleDown);
            element.RegisterCallback<DragPerformEvent>(DragPerform, TrickleDown.TrickleDown);

            void DragUpdated(DragUpdatedEvent e)
            {

                if (!HasSceneAsset(out var scene))
                    return;

                e.StopImmediatePropagation();

                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                DragAndDrop.AcceptDrag();

            }

            void DragPerform(DragPerformEvent e)
            {

                if (!HasSceneAsset(out var scene))
                    return;

                _value = scene;

            }

            bool HasSceneAsset(out Scene asset)
            {
                var l = GetDragDropScenes().ToArray();
                asset = GetDragDropScenes().FirstOrDefault();
                return asset;
            }

        }

        public static IEnumerable<Scene> GetDragDropScenes() =>
            DragAndDrop.objectReferences.OfType<Scene>().Concat(
                DragAndDrop.objectReferences.
                OfType<SceneAsset>().
                Select(o => o.ASMScene())).
                NonNull().
                Distinct();

        #endregion
        #region Tooltip

        void SetupTooltip()
        {

            PopupView.onPopupClose += RefreshTooltip;
            RegisterCallbackOnce<DetachFromPanelEvent>(e =>
            {
                PopupView.onPopupClose -= RefreshTooltip;
            });

        }

        void RefreshTooltip()
        {
            tooltip = _value && SceneManager.settings.user.displaySceneTooltips ? _value.GetTooltip() : null;
        }

        void RegisterCallbackOnce<TEventType>(EventCallback<TEventType> callback) where TEventType : EventBase<TEventType>, new()
        {

            void Wrapper(TEventType e)
            {
                UnregisterCallback<TEventType>(Wrapper);
                callback(e);
            }

            RegisterCallback<TEventType>(Wrapper);

        }

        #endregion

    }

}
