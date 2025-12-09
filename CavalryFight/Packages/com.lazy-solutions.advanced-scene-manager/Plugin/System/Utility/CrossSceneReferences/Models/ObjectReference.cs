using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Utility.CrossSceneReferences
{

    /// <summary>Specifies the result of a resolve.</summary>
    public enum ResolveStatus
    {
        /// <summary>The reference has not been resolved yet.</summary>
        Unresolved,
        /// <summary>An unknown error occurred during resolution.</summary>
        Unknown,
        /// <summary>The reference was successfully resolved.</summary>
        Succeeded,
        /// <summary>The referenced scene is not currently open.</summary>
        SceneIsNotOpen,
        /// <summary>The object path was invalid or could not be found.</summary>
        InvalidObjectPath,
        /// <summary>The referenced component could not be found.</summary>
        ComponentNotFound,
        /// <summary>The referenced field could not be found.</summary>
        InvalidField,
        /// <summary>The resolved value type does not match the expected type.</summary>
        TypeMismatch,
        /// <summary>The referenced array or event index was out of range.</summary>
        IndexOutOfRange
    }

    /// <summary>Represents a reference to an object within a scene.</summary>
    [Serializable]
    public class ObjectReference : IEqualityComparer<ObjectReference>
    {

        /// <summary>Creates an empty object reference.</summary>
        public ObjectReference()
        { }

        /// <summary>Creates a reference to an object in a scene.</summary>
        /// <param name="scene">The scene to reference.</param>
        /// <param name="objectID">The unique object identifier.</param>
        /// <param name="field">Optional field to reference.</param>
        public ObjectReference(scene scene, string objectID, FieldInfo field = null)
        {
            this.scene = scene.path;
            this.objectID = objectID;
            this.field = field?.Name;
            this.fieldType = field?.FieldType?.AssemblyQualifiedName;
        }

        /// <summary>Adds data about a component to this reference.</summary>
        /// <param name="component">The component to reference.</param>
        public ObjectReference With(Component component)
        {
            componentType = component.GetType().AssemblyQualifiedName;
            componentTypeIndex = component.gameObject.GetComponents(component.GetType()).ToList().IndexOf(component);
            return this;
        }

        /// <summary>Adds array or UnityEvent index data to this reference.</summary>
        /// <param name="unityEventIndex">Optional UnityEvent index.</param>
        /// <param name="arrayIndex">Optional array index.</param>
        public ObjectReference With(int? unityEventIndex = null, int? arrayIndex = null)
        {
            index = unityEventIndex ?? arrayIndex ?? 0;
            isTargetingUnityEvent = unityEventIndex.HasValue;
            isTargetingArray = arrayIndex.HasValue;
            return this;
        }

        /// <summary>Gets the corresponding ASM scene, if found.</summary>
        public Scene asmScene => SceneManager.assets.scenes.Find(scene);

        /// <summary>Returns whether this reference targets a component.</summary>
        public bool isTargetingComponent => !string.IsNullOrWhiteSpace(componentType);

        /// <summary>Returns whether this reference targets a field.</summary>
        public bool isTargetingField => !string.IsNullOrWhiteSpace(field);

        /// <summary>The path of the scene this reference belongs to.</summary>
        public string scene;

        /// <summary>The unique object identifier of the referenced object.</summary>
        public string objectID;

        /// <summary>The assembly-qualified name of the referenced component type.</summary>
        public string componentType;

        /// <summary>The index of the component within its GameObject.</summary>
        public int componentTypeIndex;

        /// <summary>Whether this reference targets a UnityEvent entry.</summary>
        public bool isTargetingUnityEvent;

        /// <summary>Whether this reference targets an array element.</summary>
        public bool isTargetingArray;

        /// <summary>The name of the referenced field, if applicable.</summary>
        public string field;

        /// <summary>The assembly-qualified type name of the referenced field.</summary>
        public string fieldType;

        /// <summary>The element index if targeting an array or UnityEvent.</summary>
        public int index;

        #region Resolve

        /// <summary>Resolves this reference to its target.</summary>
        /// <returns>A <see cref="ResolvedReference"/> describing the resolution result.</returns>
        public ResolvedReference Resolve()
        {

            if (!GetScene(this.scene, out var scene))
                return new ResolvedReference(ResolveStatus.SceneIsNotOpen);

            if (!GetGameObject(objectID, out var obj))
                return new ResolvedReference(ResolveStatus.InvalidObjectPath, scene);

            Object targetObj = obj;

            Component component = null;
            if (isTargetingComponent)
                if (GetComponent(obj, componentType, componentTypeIndex, out component))
                    targetObj = component;
                else
                    return new ResolvedReference(ResolveStatus.ComponentNotFound, scene, obj);

            FieldInfo field = null;
            bool hasValue = true;
            if (isTargetingField)
                if (GetField(targetObj, this.field, out field))
                {

                    if (isTargetingArray)
                    {
                        if (field.GetValue(targetObj) is Array array)
                            if (array.Length - 1 > index)
                                return new ResolvedReference(ResolveStatus.IndexOutOfRange, scene, obj, component);
                            else
                                hasValue = array.GetValue(index) != null;
                    }
                    else if (isTargetingUnityEvent)
                    {
                        if (field.GetValue(targetObj) is UnityEventBase unityEvent)
                            if (index < unityEvent.GetPersistentEventCount())
                                hasValue = unityEvent.GetPersistentTarget(index);
                            else
                                return new ResolvedReference(ResolveStatus.IndexOutOfRange, scene, obj, component);
                    }
                    else
                    {

                        var type = Type.GetType(fieldType ?? "", false);
                        if (!field.FieldType.IsAssignableFrom(type))
                            return new ResolvedReference(ResolveStatus.TypeMismatch, scene, obj, component);
                        else
                            hasValue = field.GetValue(targetObj) != null;

                    }

                }
                else
                    return new ResolvedReference(ResolveStatus.InvalidField, scene, obj, component);

            return new ResolvedReference(ResolveStatus.Succeeded, scene, obj, component, field, index, isTargetingArray, isTargetingUnityEvent, resolvedTarget: targetObj, hasBeenRemoved: !hasValue);

        }

        static bool GetScene(string scenePath, out scene scene)
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            return scene.isLoaded;
        }

        static bool GetGameObject(string objectID, out GameObject obj)
        {

            if (GuidReferenceUtility.TryFind(objectID, out var reference))
            {
                obj = reference.gameObject;
                return true;
            }

            obj = null;
            return false;

        }

        static bool GetComponent(GameObject obj, string name, int index, out Component component)
        {

            component = null;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var type = Type.GetType(name, throwOnError: false);
            if (type == null)
                return false;

            component = obj.GetComponents(type).ElementAtOrDefault(index);
            if (!component)
                return false;

            return true;

        }

        static bool GetField(object obj, string name, out FieldInfo field)
        {
            field = obj?.GetType()?.FindField(name);
            return field != null;
        }

        #endregion
        #region Set

        /// <summary>Resets the referenced value.</summary>
        public static void ResetValue(ResolvedReference variable) =>
            SetValueDirect(variable, null);

        /// <summary>Sets the referenced value to another resolved reference.</summary>
        public static ResolveStatus SetValue(ResolvedReference variable, ResolvedReference value)
        {

            if (!value.resolvedTarget)
                return ResolveStatus.Unknown;

            return SetValueDirect(variable, value.resolvedTarget);

        }

        static ResolveStatus SetValueDirect(ResolvedReference variable, Object value)
        {

            if (!variable.resolvedTarget)
                return ResolveStatus.Unknown;

            if (variable.field == null)
                return ResolveStatus.InvalidField;

            try
            {

                if (variable.isTargetingArray)
                    return SetArrayElement((IList)variable.field.GetValue(variable.resolvedTarget), variable.index, value);
                else if (variable.isTargetingUnityEvent)
                    return SetPersistentListener((UnityEvent)variable.field.GetValue(variable.resolvedTarget), variable.index, value);
                else
                    return SetField(variable.field, variable.resolvedTarget, value);

            }
            catch (Exception)
            { }

            return ResolveStatus.Unknown;

        }

        static ResolveStatus SetField(FieldInfo field, object target, object value)
        {

            if (!EnsureCorrectType(value, field.FieldType))
                return ResolveStatus.TypeMismatch;
            field.SetValue(target, value);

            return ResolveStatus.Succeeded;

        }

        static ResolveStatus SetPersistentListener(UnityEvent ev, int index, Object value)
        {

            var persistentCallsField = typeof(UnityEvent)._GetFields().FirstOrDefault(f => f.Name == "m_PersistentCalls");
            FieldInfo CallsField(object o) => o.GetType()._GetFields().FirstOrDefault(f => f.Name == "m_Calls");
            FieldInfo TargetField(object o) => o.GetType()._GetFields().FirstOrDefault(f => f.Name == "m_Target");

            if (persistentCallsField is null)
            {
                Debug.LogError("Cross-scene utility: Could not find field for setting UnityEvent listener.");
                return ResolveStatus.InvalidField;
            }

            var persistentCallGroup = persistentCallsField.GetValue(ev);
            var calls = CallsField(persistentCallGroup).GetValue(persistentCallGroup);
            var call = (calls as IList)[index];

            var field = TargetField(call);
            if (!EnsureCorrectType(value, field.FieldType))
                return ResolveStatus.TypeMismatch;

            TargetField(call).SetValue(call, value);
            return ResolveStatus.Succeeded;

        }

        static ResolveStatus SetArrayElement(IList list, int index, Object value)
        {

            var type = list.GetType().GetInterfaces().FirstOrDefault(t => t.IsGenericType).GenericTypeArguments[0];

            if (!EnsureCorrectType(value, type))
                return ResolveStatus.TypeMismatch;

            if (list.Count < index)
                return ResolveStatus.IndexOutOfRange;

            list[index] = value;
            return ResolveStatus.Succeeded;

        }

        static bool EnsureCorrectType(object value, Type target)
        {

            var t = value?.GetType();

            if (t == null)
                return true;
            if (target.IsAssignableFrom(t))
                return true;

            return false;

        }

        #endregion
        #region Equals

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            obj is ObjectReference re &&
            this.AsTuple() == re.AsTuple();

        /// <inheritdoc/>
        public override int GetHashCode() =>
            AsTuple().GetHashCode();

        /// <summary>Returns a tuple representation of this reference.</summary>
        public (string scene, string objectID, string componentType, int componentTypeIndex, string field, int index, bool isTargetingArray, bool isTargetingUnityEvent) AsTuple() =>
            (scene, objectID, componentType, componentTypeIndex, field, index, isTargetingArray, isTargetingUnityEvent);

        /// <inheritdoc/>
        public bool Equals(ObjectReference x, ObjectReference y) =>
            x?.Equals(y) ?? false;

        /// <inheritdoc/>
        public int GetHashCode(ObjectReference obj) =>
            obj?.GetHashCode() ?? -1;

        #endregion

        /// <summary>Returns whether this reference is still valid.</summary>
        /// <param name="returnTrueWhenSceneIsUnloaded">If true, returns <see langword="true"/> when the scene is unloaded.</param>
        public bool IsValid(bool returnTrueWhenSceneIsUnloaded = false)
        {

            var result = Resolve();
            if (returnTrueWhenSceneIsUnloaded && result.result == ResolveStatus.SceneIsNotOpen)
                return true;
            else if (result.result == ResolveStatus.Succeeded && result.hasBeenRemoved)
                return false;
            else
                return result.result == ResolveStatus.Succeeded;

        }

        /// <inheritdoc/>
        public override string ToString() => ToString(includeScene: true, includeGameObject: true);

        /// <summary>Returns a string representation of this reference.</summary>
        /// <param name="includeScene">Whether to include the scene name.</param>
        /// <param name="includeGameObject">Whether to include the GameObject name.</param>
        public string ToString(bool includeScene = true, bool includeGameObject = true)
        {

            var str = "";
            if (includeScene)
                str += Path.GetFileNameWithoutExtension(scene);

            if (includeGameObject)
                str += (includeScene ? "." : "") + objectID;

            if (!includeScene || !includeGameObject)
                str = "::" + str;

            if (!string.IsNullOrEmpty(componentType))
            {
                if (includeGameObject)
                    str += ".";

                var type = Type.GetType(componentType, false);
                str += (type?.Name ?? componentType) + "[" + componentTypeIndex + "]";

            }

            if (!string.IsNullOrEmpty(field))
                str += "." + field;

            if (isTargetingArray || isTargetingUnityEvent)
                str += $"[{index}]";

            return str;

        }

    }

}
