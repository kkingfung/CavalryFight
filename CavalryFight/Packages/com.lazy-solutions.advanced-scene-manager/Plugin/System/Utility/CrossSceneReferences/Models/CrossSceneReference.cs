using System;

namespace AdvancedSceneManager.Utility.CrossSceneReferences
{

    /// <summary>Represents a reference between two objects in different scenes.</summary>
    [Serializable]
    public class CrossSceneReference
    {
        /// <summary>The unique identifier for this reference.</summary>
        public string id;

        /// <summary>The variable being referenced in another scene.</summary>
        public ObjectReference variable;

        /// <summary>The value assigned to the referenced variable.</summary>
        public ObjectReference value;

        /// <summary>Creates an empty cross-scene reference.</summary>
        public CrossSceneReference()
        { }

        /// <summary>Creates a new cross-scene reference between two objects.</summary>
        /// <param name="variable">The variable reference.</param>
        /// <param name="value">The value reference.</param>
        public CrossSceneReference(ObjectReference variable, ObjectReference value)
        {
            this.variable = variable;
            this.value = value;
            id = GuidReferenceUtility.GenerateID();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            id == (obj as CrossSceneReference)?.id;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            id.GetHashCode();
    }

}
