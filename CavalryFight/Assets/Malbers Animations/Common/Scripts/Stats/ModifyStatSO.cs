using MalbersAnimations.Scriptables;
using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Controller.Reactions
{
    /// <summary> Reaction Script for Making the Animal do something </summary>
    [CreateAssetMenu(menuName = "Malbers Animations/Modifier/Stat", fileName = "New Stat Modifier", order = -100)]
    public class ModifyStatSO : ScriptableObject
    {
        public List<StatModifier> modifiers = new();
        /// <summary>Instant Reaction ... without considering Active or Delay parameters</summary>

        public void Modify(Stats stats) => modifiers.ForEach(item => item.ModifyStat(stats));

        public void Modify(Component stats) => Modify(stats.MFindComponentInRoot<Stats>());

        public void Modify(GameObject stats) => Modify(stats.MFindComponentInRoot<Stats>());

        public void Modify(Transform t) => Modify((Component)t);

        public void Modify(GameObjectVar go) => Modify(go.Value);

        public void Modify(TransformVar t) => Modify((Component)t.Value);

        private void Reset()
        {
            modifiers = new()
            {
                new StatModifier()
                {
                    ID = MTools.GetInstance<StatID>("Health"),
                    Base = null,
                    MaxValue = new(20),
                    MinValue = new(20)
                }
            };
        }
    }
}