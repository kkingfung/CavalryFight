using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Utility
{

    internal static class SessionStateUtility
    {

        static readonly Dictionary<string, object> cache = new();

        public static void Set<T>(T value, [CallerMemberName] string propertyName = "")
        {
            var key = $"ASM.{propertyName}";
            cache[key] = value!;

#if UNITY_EDITOR
            switch (value)
            {
                case string s:
                    SessionState.SetString(key, s ?? string.Empty);
                    break;
                case float f:
                    SessionState.SetFloat(key, f);
                    break;
                case bool b:
                    SessionState.SetBool(key, b);
                    break;
                case int i:
                    SessionState.SetInt(key, i);
                    break;
                case int[] array:
                    SessionState.SetIntArray(key, array);
                    break;
                case Vector3 v3:
                    SessionState.SetVector3(key, v3);
                    break;
                case Type type:
                    SessionState.SetString(key, type?.AssemblyQualifiedName ?? string.Empty);
                    break;
                case null:
                    SessionState.SetString(key, string.Empty);
                    break;
                default:
                    SessionState.SetString(key, JsonUtility.ToJson(value));
                    break;
            }
#endif
        }

        public static T Get<T>(T defaultValue = default!, [CallerMemberName] string propertyName = "")
        {
            var key = $"ASM.{propertyName}";

            if (cache.TryGetValue(key, out var cached) && cached is T t)
                return t;

#if UNITY_EDITOR
            return typeof(T) switch
            {
                var t2 when t2 == typeof(string) => (T)(object)SessionState.GetString(key, (string)(object)defaultValue ?? string.Empty),
                var t2 when t2 == typeof(float) => (T)(object)SessionState.GetFloat(key, (float)(object)defaultValue!),
                var t2 when t2 == typeof(bool) => (T)(object)SessionState.GetBool(key, (bool)(object)defaultValue!),
                var t2 when t2 == typeof(int) => (T)(object)SessionState.GetInt(key, (int)(object)defaultValue!),
                var t2 when t2 == typeof(int[]) => (T)(object)SessionState.GetIntArray(key, ((int[])(object)defaultValue ?? Array.Empty<int>())!),
                var t2 when t2 == typeof(Vector3) => (T)(object)SessionState.GetVector3(key, (Vector3)(object)defaultValue!),
                var t2 when t2 == typeof(Type) => (T)(object)(Type.GetType(SessionState.GetString(key, ((Type)(object)defaultValue)?.AssemblyQualifiedName ?? string.Empty), throwOnError: false)),
                _ => TryFromJson<T>(key, defaultValue)
            };
#else
            return defaultValue!;
#endif
        }

        static T TryFromJson<T>(string key, T fallback)
        {
#if UNITY_EDITOR
            var json = SessionState.GetString(key, string.Empty);
            return string.IsNullOrEmpty(json) ? fallback : JsonUtility.FromJson<T>(json);
#else
    return fallback;
#endif
        }

    }

}