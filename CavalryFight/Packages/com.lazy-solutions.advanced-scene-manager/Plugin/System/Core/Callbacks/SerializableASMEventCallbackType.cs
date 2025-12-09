using System;
using System.Linq;
using System.Reflection;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Utility;
using UnityEngine;
using UnityEngine.UIElements;
using AdvancedSceneManager.Editor.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace AdvancedSceneManager.Callbacks.Events.Utility
{

    /// <summary>Represents a serializable event callback type.</summary>
    /// <remarks>
    /// Provides <see cref="RegisterCallback(string, SceneOperation, Callbacks.Events.EventCallback{SceneOperationEventBase})"/> for easily registering the callback.
    /// <para>Also provides a property drawer.</para>
    /// </remarks>
    [Serializable]
    public class SerializableASMEventCallbackType
    {
        /// <summary>The <see cref="Type.AssemblyQualifiedName"/>.</summary>
        /// <remarks>Serialized using <see cref="XmlEscapeUtility.Escape(string)"/>, since unity does not automatically escape when writing strings to uxml.</remarks>
        public string typeName;

        /// <summary>Represents when this event is to be called.</summary>
        public When when;

        /// <inheritdoc />
        public override string ToString() =>
            $"({when}) {typeName}";

        Type GetCallbackType(bool warnIfNull = true, bool isUnregister = false)
        {
            var decodedTypeName = XmlEscapeUtility.Unescape(typeName);
            var t = Type.GetType(decodedTypeName, throwOnError: false);

            if (!typeof(SceneOperationEventBase).IsAssignableFrom(t))
            {
                if (warnIfNull)
                    Debug.LogWarning($"Cannot {(isUnregister ? "unregister" : "register")} event callback. Type '{decodedTypeName}' could not be found or is invalid.");
                return null;
            }

            return t;
        }

        /// <summary>Gets if this event callback type is valid.</summary>
        public bool IsValid() =>
            GetCallbackType(warnIfNull: false) is not null;

        #region Register

        /// <summary>Register callback on <paramref name="operation"/>.</summary>
        public void RegisterCallback(string key, SceneOperation operation, Callbacks.Events.EventCallback<SceneOperationEventBase> callback) =>
            operation.events.RegisterCallback(GetCallbackType(), callback, when, key);

        /// <summary>Register callback globally.</summary>
        public void RegisterGlobalCallback(string key, Callbacks.Events.EventCallback<EventCallbackBase> callback) =>
            SceneManager.events.RegisterCallback(GetCallbackType(), callback, when, key);

        #endregion
        #region Unregister

        /// <summary>Unregisters the callback on <paramref name="operation"/>.</summary>
        public SceneOperation UnregisterCallback(string key, SceneOperation operation) =>
            EventCallbackUtility.UnregisterCallback(key, operation);

        /// <summary>Unregisters the callback globally.</summary>
        public void UnregisterGlobalCallback(string key) =>
            EventCallbackUtility.UnregisterCallbackGlobal(key);

        #endregion

    }

#if UNITY_EDITOR

    class SerializableASMEventCallbackTypeConverter : UxmlAttributeConverter<SerializableASMEventCallbackType>
    {
        public override SerializableASMEventCallbackType FromString(string value)
        {
            return JsonUtility.FromJson<SerializableASMEventCallbackType>(XmlEscapeUtility.Unescape(value));
        }
        public override string ToString(SerializableASMEventCallbackType value)
        {
            return XmlEscapeUtility.Unescape(JsonUtility.ToJson(value));
        }
    }

    /// <summary>Applies a filter to the types displayed in <see cref="ASMEventCallbackPropertyDrawer"/>, used for <see cref="SerializableASMEventCallbackType"/> fields.</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ASMCallbackEventPropertyDrawerFilterAttribute : Attribute
    {
        /// <summary>Specifies the types to hide in the list.</summary>
        public Type[] HiddenTypes { get; }

        /// <summary />
        public ASMCallbackEventPropertyDrawerFilterAttribute(params Type[] hiddenTypes) =>
            HiddenTypes = hiddenTypes ?? Array.Empty<Type>();
    }

    [CustomPropertyDrawer(typeof(SerializableASMEventCallbackType))]
    class ASMEventCallbackPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            property.serializedObject.Update();

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            var typeNameProp = property.FindPropertyRelative(nameof(SerializableASMEventCallbackType.typeName));
            var whenProp = property.FindPropertyRelative("when");

            if (typeNameProp == null || whenProp == null)
            {
                Debug.LogError("Could not find expected properties");
                return container;
            }

            // Get the attribute if applied
            var filterAttribute = fieldInfo?.GetCustomAttribute<ASMCallbackEventPropertyDrawerFilterAttribute>();
            var hiddenTypes = filterAttribute?.HiddenTypes ?? Array.Empty<Type>();

            // Get all subclasses of SceneOperationEventBase and order them
            var types = TypeUtility.FindSubclasses<SceneOperationEventBase>(includeAbstract: false)
                .Where(t => t != null && !hiddenTypes.Contains(t)) // Filter out hidden types
                .OrderBy(EventCallbackUtility.GetInvokationOrder)
                .ToList();

            types.Insert(0, null!); // Keep "None" at the top
            var selectedType = types.FirstOrDefault(t => t?.AssemblyQualifiedName == XmlEscapeUtility.Escape(typeNameProp.stringValue));

            // Create type dropdown
            var typeField = new DropdownField(property.displayName, types.Select(t => t?.Name ?? "None").ToList(), 0)
            {
                value = selectedType?.Name ?? "None"
            };

            // Create `When` dropdown
            var whenValues = Enum.GetValues(typeof(When)).Cast<When>().ToList();
            var whenOptions = whenValues.Select(e => e.ToString()).ToList();
            var currentWhen = (When)whenProp.enumValueIndex;
            var whenField = new DropdownField(whenOptions, whenOptions.IndexOf(currentWhen.ToString()));

            // Type selection callback
            typeField.RegisterValueChangedCallback(evt =>
            {
                typeNameProp.stringValue = XmlEscapeUtility.Escape(types[typeField.index]?.AssemblyQualifiedName);
                UpdateWhenFieldEnabledState(types[typeField.index]);
                typeNameProp.serializedObject.ApplyModifiedProperties();
            });

            // When selection callback
            whenField.RegisterValueChangedCallback(evt =>
            {
                whenProp.enumValueIndex = (int)whenValues[whenOptions.IndexOf(evt.newValue)];
                property.serializedObject.ApplyModifiedProperties();
            });

            // Styling
            typeField.style.flexGrow = 1;
            whenField.style.flexGrow = 0;

            // Initial state update
            UpdateWhenFieldEnabledState(selectedType);

            container.Add(typeField);
            container.Add(whenField);

            void UpdateWhenFieldEnabledState(Type selectedType) =>
                whenField.SetEnabled(EventCallbackUtility.IsWhenApplicable(selectedType));

            return container;
        }
    }

#endif

}
