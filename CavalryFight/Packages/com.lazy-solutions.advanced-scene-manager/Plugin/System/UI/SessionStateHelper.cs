using AdvancedSceneManager.Utility;
using System.Runtime.CompilerServices;

namespace AdvancedSceneManager.Services
{

#if UNITY_EDITOR
    /// <summary>A helper wrapper for <see cref="UnityEditor.SessionState"/>, uses type name + property name for naming.</summary>
#endif
    public class SessionStateHelper
    {

        readonly string name;

        /// <summary />
        public SessionStateHelper(object target) =>
            name = target.GetType().FullName;

        /// <summary>Sets a session wide persisted value.</summary>
        /// <remarks>Uses caller name for <paramref name="propertyName"/> by default.</remarks>
        public void SetProperty<T>(T value, [CallerMemberName] string propertyName = "") =>
            SessionStateUtility.Set(value, $"{name}.{propertyName}");

        /// <summary>Gets a session wide persisted value.</summary>
        /// <remarks>Uses caller name for <paramref name="propertyName"/> by default.</remarks>
        public T GetProperty<T>(T defaultValue, [CallerMemberName] string propertyName = "") =>
            SessionStateUtility.Get(defaultValue, $"{name}.{propertyName}");

        /// <summary>Sets a session wide persisted value.</summary>
        /// <remarks>Uses caller name for <paramref name="propertyName"/> by default.</remarks>
        public void SetValue<T>(T value, string propertyName) =>
            SessionStateUtility.Set(value, $"{name}.{propertyName}");

        /// <summary>Gets a session wide persisted value.</summary>
        /// <remarks>Uses caller name for <paramref name="propertyName"/> by default.</remarks>
        public T GetValue<T>(T defaultValue, string propertyName) =>
            SessionStateUtility.Get(defaultValue, $"{name}.{propertyName}");

    }

}
