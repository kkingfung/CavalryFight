using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Models.Internal;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Models.Interfaces;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>A profile for ASM, contains settings and collections.</summary>
    public partial class Profile : ASMModelBase, IFindable
    {

        void OnEnable()
        {
            OnEnable_Collections();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            OnDisable_Collections();
            base.OnDisable();
        }

        /// <summary>Gets if this profile is set as active.</summary>
        /// <remarks>See also: <see cref="ProfileUtility.active"/>.</remarks>
        public bool isActive =>
            ProfileUtility.active == this;

        #region Prefix

        void UpdatePrefix()
        {
#if UNITY_EDITOR
            collections.NonNull().ForEach(c => c.Rename(c.title, prefix));
#endif
        }

        internal const string PrefixDelimiter = " - ";

        /// <summary>Gets the prefix that is used on collections in this profile.</summary>
        /// <remarks>This would be <see cref="UnityEngine.Object.name"/> + <see cref="PrefixDelimiter"/>.</remarks>
        internal string prefix => name + PrefixDelimiter;

        #endregion
        #region Find

        /// <summary>Gets 't:AdvancedSceneManager.Models.Profile', the string to use in <see cref="AssetDatabase.FindAssets(string)"/>.</summary>
        public readonly static string AssetSearchString = "t:" + typeof(Profile).FullName;

        /// <summary>Finds the profile with the specified name or id.</summary>
        public static Profile Find(string q) =>
            SceneManager.assets.profiles.Find(q);

        /// <summary>Finds the profile with the specified name or id.</summary>
        public static bool TryFind(string q, out Profile profile) =>
            SceneManager.assets.profiles.TryFind(q, out profile);

        #endregion
        #region OnValidate / OnPropertyChanged

#if UNITY_EDITOR

        /// <inheritdoc />
        public override void OnValidate()
        {
            UpdatePrefix();
            base.OnValidate();

            if (SceneManager.profile == this)
                EditorApplication.delayCall += BuildUtility.UpdateSceneList;
        }

        /// <inheritdoc />
        public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName is nameof(splashScene) or nameof(loadingScene))
                BuildUtility.UpdateSceneList();
        }

#endif

        #endregion

        /// <inheritdoc />
        public override string ToString() =>
            name;

    }

}
