using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.UtilityFunctions.Components;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.UtilityFunctions
{
    public class GenerateSplashScreen : ASMUtilityFunction
    {
        public override string Name { get => "+ SplashScreen"; }
        public override string Description { get => "Generates a new empty splash screen to build off."; }
        public override string Group { get => "Loadingscreen"; }

        public override void OnInvoke(ref VisualElement optionsGUI)
        {
            VisualElement visualElement = new();


            TextField targetName = new("Name");

            VisualElement pathElement = PathComponent.PathPicker("Target folder", "", "", out var folder);

            Button button = new(() => Create(folder.value, targetName.value)) { text = "Create" };

            visualElement.Add(targetName);
            visualElement.Add(pathElement);
            visualElement.Add(button);
            optionsGUI = visualElement;
        }

        private void Create(string folder, string name)
        {
            SceneUtility.CreateSplashScreenAsset(folder, name);
        }
    }
}