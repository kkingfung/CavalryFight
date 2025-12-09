using System.Collections;
using System.Collections.Generic;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines some core properties for scene collections.</summary>
    public interface ISceneCollection : IASMModel, IFindable, IEnumerable
    {

        /// <summary>Gets the scenes of this collection.</summary>
        public IEnumerable<string> scenePaths { get; }

        /// <summary>Gets the title of this collection.</summary>
        public string title { get; }

        /// <summary>Gets the description of this collection.</summary>
        public string description { get; }

        /// <summary>Gets the scene count of this collection.</summary>
        public int count { get; }

        /// <summary>Gets whether this collection contains the specified object.</summary>
        public bool Contains(object obj);

        /// <summary>Saves this collection to disk.</summary>
        public void Save();

    }

    /// <summary>Defines some core properties for scene collections.</summary>
    public interface ISceneCollection<T> : ISceneCollection, IEnumerable<T>
    {

        /// <summary>Gets the scenes of this collection.</summary>
        public IEnumerable<T> scenes { get; }

        /// <summary>Gets if this collection contains <paramref name="scene"/>.</summary>
        public bool Contains(T scene);

        /// <summary>Gets the scene at the specified index.</summary>
        public T this[int index] { get; }

    }

}
