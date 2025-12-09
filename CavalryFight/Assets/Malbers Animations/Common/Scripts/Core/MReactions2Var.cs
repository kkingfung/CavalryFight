using MalbersAnimations.Scriptables;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [CreateAssetMenu(menuName = "Malbers Animations/Reactions2 Var", order = 100)]
    public class MReactions2Var : ScriptableObject
    {
        public Reaction2 reaction;

        public void React(Component component)
        {
            if (component == null)
            {
                Debug.LogWarning("There's no component set to apply the reactions");
                return;
            }
            reaction.React(component);
        }

        public void React(GameObject go)
        {
            if (go == null)
            {
                Debug.LogWarning("There's no gameobject set to apply the reactions");
                return;
            }
            reaction.React(go);
        }

        public void React(Transform t) => React((Component)t);

        public void React(GameObjectVar go) => React(go.Value);

        public void React(TransformVar t) => React((Component)t.Value);
    }
}

