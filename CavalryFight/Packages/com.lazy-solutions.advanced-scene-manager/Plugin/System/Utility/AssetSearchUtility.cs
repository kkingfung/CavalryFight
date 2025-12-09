using AdvancedSceneManager.Models.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides utility functions for searching ASM assets.</summary>
    public static class AssetSearchUtility
    {

        #region Auto list

        /// <summary>Finds the <typeparamref name="T"/> with the specified name.</summary>
        public static T Find<T>(string q) where T : IASMModel, IFindable =>
            Find(SceneManager.assets.Enumerate<T>(), q);

        /// <summary>Finds the <typeparamref name="T"/> with the specified name.</summary>
        public static bool TryFind<T>(string q, out T result) where T : IASMModel, IFindable =>
            TryFind(SceneManager.assets.Enumerate<T>(), q, out result);

        #endregion
        #region Array

        /// <inheritdoc cref="Find{T}(string)"/>
        public static T Find<T>(this T[] list, string q) where T : IASMModel, IFindable =>
            Find((IEnumerable<T>)list, q);

        /// <inheritdoc cref="Find{T}(string)"/>
        public static bool TryFind<T>(this T[] list, string q, out T result) where T : IFindable =>
            TryFind((IEnumerable<T>)list, q, out result);

        #endregion
        #region Enumerable

        /// <inheritdoc cref="Find{T}(string)"/>
        public static T Find<T>(this IEnumerable<T> list, string q) where T : IASMModel, IFindable =>
            list.FirstOrDefault(o => o != null && o.IsMatch(q));

        /// <inheritdoc cref="Find{T}(string)"/>
        public static bool TryFind<T>(this IEnumerable<T> list, string q, out T result) where T : IFindable
        {
            result = default;

            if (list == null)
                return false;

            result = list.FirstOrDefault(o => o != null && o.IsMatch(q));
            return result != null;
        }

        #endregion

    }

}
