using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedSceneManager.Utility.CrossSceneReferences
{

    /// <summary>Represents a resolved <see cref="ObjectReference"/>.</summary>
    public struct ResolvedReference
    {

        /// <summary>The result of the resolution.</summary>
        public ResolveStatus result;

        /// <summary>The scene containing the resolved object, if any.</summary>
        public Scene? scene;

        /// <summary>The resolved <see cref="GameObject"/>, if applicable.</summary>
        public GameObject gameObject;

        /// <summary>The resolved <see cref="Component"/>, if applicable.</summary>
        public Component component;

        /// <summary>The resolved <see cref="FieldInfo"/>, if applicable.</summary>
        public FieldInfo field;

        /// <summary>The element index if targeting an array or event list.</summary>
        public int index;

        /// <summary>Whether the reference targets a UnityEvent entry.</summary>
        public bool isTargetingUnityEvent;

        /// <summary>Whether the reference targets an array element.</summary>
        public bool isTargetingArray;

        /// <summary>The resolved target object.</summary>
        public Object resolvedTarget;

        /// <summary>Whether the reference target has been removed.</summary>
        public bool hasBeenRemoved;

        /// <summary>Initializes a new instance of <see cref="ResolvedReference"/>.</summary>
        public ResolvedReference(
            ResolveStatus result,
            Scene? scene = null,
            GameObject gameObject = null,
            Component component = null,
            FieldInfo field = null,
            int index = 0,
            bool isTargetingArray = false,
            bool isTargetingUnityEvent = false,
            Object resolvedTarget = null,
            bool hasBeenRemoved = false)
        {
            this.scene = scene;
            this.result = result;
            this.gameObject = gameObject;
            this.component = component;
            this.field = field;
            this.index = index;
            this.isTargetingArray = isTargetingArray;
            this.isTargetingUnityEvent = isTargetingUnityEvent;
            this.resolvedTarget = resolvedTarget;
            this.hasBeenRemoved = hasBeenRemoved;
        }

        /// <inheritdoc/>
        public override string ToString() =>
            ToString(includeScene: true, includeGameObject: false);

        /// <summary>Returns a string representation of this reference.</summary>
        /// <param name="includeScene">Whether to include the scene name.</param>
        /// <param name="includeGameObject">Whether to include the GameObject name.</param>
        public string ToString(bool includeScene = true, bool includeGameObject = true)
        {
            var str = "";
            if (scene.HasValue && includeScene)
                str += scene.Value.name;

            if (gameObject != null && includeGameObject)
                str += (includeScene ? "." : "") + gameObject.name;

            if (!includeScene || !includeGameObject)
                str = "::" + str;

            if (component)
            {
                if (includeGameObject)
                    str += ".";
                str += GetComponentName();
            }

            if (field != null)
                str += "." + field.Name;

            if (isTargetingArray || isTargetingUnityEvent)
                str += $"[{index}]";

            if (result != ResolveStatus.Succeeded)
                str += "\nError: " + result.ToString();

            return str;
        }

        string GetComponentName() =>
            component ? component.GetType().Name : null;
    }

}
