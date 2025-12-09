using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Adds support for reorder in <see cref="SerializableDictionary{TKey, TValue}"/>. Used by property drawer.</summary>
    public interface IReorderableDictionary
    {
        /// <summary>Move the item at <paramref name="oldIndex"/> to <paramref name="newIndex"/>.</summary>
        void Move(int oldIndex, int newIndex);
    }

    /// <summary>A serializable dictionary that supports Unity serialization and implements <see cref="IDictionary{TKey, TValue}"/>.</summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {

        /// <summary>Represents the serialized list of keys.</summary>
        [SerializeField]
        protected List<TKey> keys = new List<TKey>();

        /// <summary>Represents the serialized list of values.</summary>
        [SerializeField]
        protected List<TValue> values = new List<TValue>();

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {

            Clear();

            if (keys.Count != values.Count)
                throw new Exception(string.Format($"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable."));

            for (int i = 0; i < keys.Count; i++)
                Add(keys[i], values[i]);

        }

    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    class SerializableDictionaryDrawer : PropertyDrawer
    {
        VisualElement container;
        SerializedProperty property;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.property = property;
            container = new VisualElement();
            Reload();
            return container;
        }

        void Reload()
        {
            container.Clear();
            container.Add(CreateDictionaryListViews(property, Reload));
        }

        VisualElement CreateDictionaryListViews(SerializedProperty property, Action onChanged)
        {
            if (GetTargetObject(property) is not IDictionary dict)
                return new Label("Invalid dictionary");

            var keyType = dict.GetType().GenericTypeArguments[0];
            var valueType = dict.GetType().GenericTypeArguments[1];

            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var kvpKeyProp = kvpType.GetProperty("Key");
            var kvpValueProp = kvpType.GetProperty("Value");

            var items = dict.OfType<object>().ToList();

            var list = new ListView
            {
                itemsSource = items,
                showBorder = true,
                showAddRemoveFooter = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showFoldoutHeader = true,
                headerTitle = preferredLabel,
                allowAdd = CanAutoGenerateKey(keyType, dict.Keys.Cast<object>()),
                allowRemove = dict.Count > 0,
                makeItem = () =>
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    var keyField = CreateElement(keyType);
                    var valueField = CreateElement(valueType);

                    keyField.name = "key";
                    valueField.name = "value";

                    keyField.style.flexGrow = 1;
                    valueField.style.flexGrow = 1;

                    row.Add(keyField);
                    row.Add(valueField);

                    return row;
                }
            };

            var keysToRevert = new Dictionary<int, object>();

            list.bindItem = (element, index) =>
            {
                var item = items[index];
                var key = kvpKeyProp.GetValue(item);
                var value = kvpValueProp.GetValue(item);

                var keyElement = element.Q("key");
                var valueElement = element.Q("value");

                keyElement.RegisterCallback<FocusInEvent>(e =>
                {
                    list.selectedIndex = index;
                });

                valueElement.RegisterCallback<FocusInEvent>(e =>
                {
                    list.selectedIndex = index;
                });

                BindElement(keyElement, key, newKey =>
                {
                    keysToRevert.Remove(index);
                    SetError(false);
                    if (Equals(key, newKey))
                        return;

                    //Check if new key is duplicated
                    if (dict.Contains(newKey))
                    {
                        element.RegisterCallbackOnce<FocusOutEvent>(_ =>
                        {
                            if (keysToRevert.Remove(index, out var original))
                            {
                                //Key was duplicated, revert to original
                                SetError(false);
                                BindElement(keyElement, original, null);
                                EditorUtility.SetDirty(property.serializedObject.targetObject);
                                AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                            }
                        });

                        SetError(true);
                        keysToRevert.Add(index, key);
                        return;
                    }

                    dict.Remove(key);
                    dict[newKey] = value;
                    items[index] = Activator.CreateInstance(kvpType, newKey, value);
                    key = newKey;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);

                    void SetError(bool error)
                    {
                        keyElement.style.backgroundColor = error ? new(Color.red) : new(StyleKeyword.Initial);
                        keyElement.style.paddingBottom = error ? 1 : 0;
                    }
                });

                BindElement(element.Q("value"), value, newVal =>
                {
                    dict[kvpKeyProp.GetValue(items[index])] = newVal;
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                });
            };

            list.onAdd += _ =>
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
                {
                    var kvp = Activator.CreateInstance(kvpType);

                    items.Add(kvp);
                }
                else if (GetNewKey(dict, out var key))
                {
                    dict.Add(key, GetDefault(valueType));
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                    onChanged?.Invoke();
                }
            };

            list.onRemove += _ =>
            {
                int index = list.selectedIndex;
                if (index >= 0 && index < items.Count)
                {
                    var key = kvpKeyProp.GetValue(items[index]);
                    dict.Remove(key);
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                    onChanged?.Invoke();
                }
            };

            list.itemIndexChanged += (oldIndex, newIndex) =>
            {
                ((IReorderableDictionary)dict).Move(oldIndex, newIndex);
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
            };

            return list;
        }

        object GetTargetObject(SerializedProperty prop)
        {
            object obj = prop.serializedObject.targetObject;
            string[] path = prop.propertyPath.Replace(".Array.data[", "[").Split('.');
            foreach (string part in path)
            {
                if (part.Contains("["))
                {
                    var name = part.Substring(0, part.IndexOf("["));
                    int index = int.Parse(part[(part.IndexOf("[") + 1)..^1]);
                    obj = GetValue(obj, name, index);
                }
                else obj = GetValue(obj, part);
            }
            return obj;
        }

        object GetValue(object source, string name)
        {
            if (source == null) return null;
            var field = source.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(source);
        }

        object GetValue(object source, string name, int index)
        {
            if (GetValue(source, name) is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                for (int i = 0; i <= index; i++) if (!enumerator.MoveNext()) return null;
                return enumerator.Current;
            }
            return null;
        }

        bool CanAutoGenerateKey(Type keyType, IEnumerable<object> existingKeys)
        {
            if (keyType == typeof(int) || keyType == typeof(string)) return true;
            if (keyType == typeof(bool)) return existingKeys.OfType<bool>().Distinct().Count() < 2;
            if (keyType.IsEnum)
            {
                var all = Enum.GetValues(keyType).Cast<object>();
                var existing = existingKeys.Where(k => k?.GetType() == keyType).ToHashSet();
                return all.Any(v => !existing.Contains(v));
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
            {
                return true;
            }

            return false;
        }

        bool GetNewKey(IDictionary dictionary, out object key)
        {
            key = null;

            var keyType = dictionary.GetType().GenericTypeArguments[0];
            if (keyType == typeof(int))
            {
                int max = dictionary.Keys.Cast<int>().DefaultIfEmpty(-1).Max();
                key = max + 1;
                return true;
            }
            if (keyType == typeof(string))
            {
                int i = 1;
                var existing = new HashSet<string>(dictionary.Keys.Cast<string>());
                string candidate;
                do candidate = $"Key{i++}"; while (existing.Contains(candidate));
                key = candidate;
                return true;
            }
            if (keyType.IsEnum)
            {
                foreach (var value in Enum.GetValues(keyType))
                    if (!dictionary.Contains(value))
                    {
                        key = value;
                        return true;
                    }
            }
            if (keyType == typeof(bool))
            {
                if (!dictionary.Contains(false))
                {
                    key = false;
                    return true;
                }

                if (!dictionary.Contains(true))
                {
                    key = true;
                    return true;
                }
            }

            if (typeof(ScriptableObject).IsAssignableFrom(keyType))
            {
                //key = ScriptableObject.CreateInstance(keyType);
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
            {
                return false;
            }

            try
            {
                var instance = Activator.CreateInstance(keyType);
                if (instance != null && !dictionary.Contains(instance))
                {
                    key = instance;
                    return true;
                }
            }
            catch { }

            return false;
        }

        object GetDefault(Type type)
        {
            try { return Activator.CreateInstance(type); }
            catch { return null; }
        }

        VisualElement CreateElement(Type type)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return new ObjectField { objectType = type, allowSceneObjects = true };
            if (type == typeof(int)) return new IntegerField();
            if (type == typeof(float)) return new FloatField();
            if (type == typeof(double)) return new DoubleField();
            if (type == typeof(long)) return new LongField();
            if (type == typeof(string)) return new TextField();
            if (type == typeof(bool)) return new Toggle();
            if (type == typeof(Vector2)) return new Vector2Field();
            if (type == typeof(Vector3)) return new Vector3Field();
            if (type == typeof(Vector4)) return new Vector4Field();
            if (type == typeof(Color)) return new ColorField();
            if (type == typeof(Rect)) return new RectField();
            if (type == typeof(Bounds)) return new BoundsField();
            if (type.IsEnum) return new EnumField((Enum)Enum.GetValues(type).GetValue(0));
            return new Label();
        }

        void BindElement(VisualElement element, object value, Action<object> onChange)
        {
            switch (element)
            {
                case ObjectField f: f.value = value as UnityEngine.Object; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case IntegerField f: f.value = value is int i ? i : 0; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case FloatField f: f.value = value is float fl ? fl : 0f; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case DoubleField f: f.value = value is double d ? d : 0.0; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case LongField f: f.value = value is long l ? l : 0L; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case TextField f:
                    f.value = value?.ToString() ?? "";
                    f.RegisterValueChangedCallback(evt => onChange?.Invoke(ParsePrimitive(evt.newValue, value?.GetType())));
                    break;
                case Toggle f: f.value = value is bool b && b; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case Vector2Field f: f.value = value is Vector2 v2 ? v2 : default; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case Vector3Field f: f.value = value is Vector3 v3 ? v3 : default; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case Vector4Field f: f.value = value is Vector4 v4 ? v4 : default; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case ColorField f: f.value = value is Color c ? c : default; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case RectField f: f.value = value is Rect r ? r : default; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case BoundsField f: f.value = value is Bounds bds ? bds : default; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case EnumField f: f.Init((Enum)value); f.value = (Enum)value; f.RegisterValueChangedCallback(evt => onChange?.Invoke(evt.newValue)); break;
                case Label f: f.text = value?.ToString() ?? "<null>"; break;
            }
        }

        object ParsePrimitive(string input, Type type)
        {
            try
            {
                if (type == typeof(int)) return int.Parse(input);
                if (type == typeof(float)) return float.Parse(input);
                if (type == typeof(double)) return double.Parse(input);
                if (type == typeof(long)) return long.Parse(input);
                if (type == typeof(bool)) return bool.Parse(input);
                if (type == typeof(string)) return input;
            }
            catch { }
            return null;
        }
    }

#endif

}
