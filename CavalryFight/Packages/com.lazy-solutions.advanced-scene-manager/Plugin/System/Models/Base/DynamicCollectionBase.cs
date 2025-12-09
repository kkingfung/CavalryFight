using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Models.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>Base class for dynamic collections.</summary>
    public abstract class DynamicCollectionBase<T> : ASMModelBase, ISceneCollection<T>, IEquatable<DynamicCollectionBase<T>>, IFindable
    {

        [SerializeField] internal string m_title = "New dynamic collection";

        /// <summary />
        [SerializeField] protected string m_description;

        #region ISceneCollection

        /// <summary>Gets the scenes or scene paths contained in this collection.</summary>
        public abstract IEnumerable<T> scenes { get; }

        /// <summary>Gets the scene paths contained in this collection.</summary>
        public abstract IEnumerable<string> scenePaths { get; }

        /// <summary>Gets the count of scenes or scene paths contained in this collection.</summary>
        public virtual int count =>
            scenes.Count();

        /// <summary>Gets the scene or scene path at the specified index.</summary>
        public T this[int index] =>
            scenes.ElementAtOrDefault(index);

        /// <summary>Gets the title of this collection.</summary>
        public string title =>
            m_title;

        /// <summary>Gets the description of this collection.</summary>
        [HideInInspector]
        public string description
        {
            get => m_description;
            set => m_description = value;
        }

        /// <summary>Gets if this collection has any scenes.</summary>
        public bool hasScenes => scenes.Any();

        /// <summary>Gets an enumerator for the scenes or scene paths contained in this collection.</summary>
        public IEnumerator<T> GetEnumerator() =>
            scenes?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        /// <summary>Gets whether this collection contains the specified scene or scene path.</summary>
        public bool Contains(T scene) =>
            scenes.Contains(scene);

        bool ISceneCollection.Contains(object scene) =>
           scene is T t && scenes.Contains(t);

        internal override void Rename(string newName)
        {
            m_title = newName;
            base.Rename(newName);
        }

        #endregion
        #region Profile

        /// <summary>Finds the profile that contains this collection.</summary>
        public bool FindProfile(out Profile profile) =>
            profile = FindProfile();

        /// <summary>Finds the profile that contains this collection.</summary>
        public Profile FindProfile() =>
            SceneManager.assets.profiles.FirstOrDefault(p => p && p.Contains(this, checkRemoved: true));

        [NonSerialized] private Profile m_profile;

        /// <summary>Gets the profile that contains this collection. Cached.</summary>
        public Profile profile
        {
            get
            {
                if (!m_profile)
                    m_profile = FindProfile();
                return m_profile;
            }
        }

        #endregion
        #region Equality

        /// <inheritdoc />
        public override bool Equals(object obj) =>
            Equals(obj as DynamicCollectionBase<T>);

        /// <summary>Determines whether this collection is equal to another collection.</summary>
        public bool Equals(DynamicCollectionBase<T> other)
        {
            if (ReferenceEquals(other, null)) // real null check, not Unity's
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return string.Equals(id, other.id, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode() =>
            id?.GetHashCode() ?? 0;

        /// <inheritdoc />
        public static bool operator ==(DynamicCollectionBase<T> left, DynamicCollectionBase<T> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(DynamicCollectionBase<T> left, DynamicCollectionBase<T> right) =>
            !(left == right);

        #endregion
        #region IFindable

        /// <summary>Matches this collection against the query string.</summary>
        public override bool IsMatch(string q) =>
            base.IsMatch(q) || title == q;

        #endregion

        /// <summary>Called when this collection is enabled.</summary>
        protected virtual void OnEnable()
        {
            m_title = name;
        }

        /// <summary>Returns the title of this collection.</summary>
        public override string ToString() => title;

    }

}
