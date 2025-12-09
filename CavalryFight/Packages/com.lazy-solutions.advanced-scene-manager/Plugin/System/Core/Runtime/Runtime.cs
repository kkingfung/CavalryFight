namespace AdvancedSceneManager.Core
{

    /// <summary>Manages runtime functionality for Advanced Scene Manager such as open scenes and collection.</summary>
    public partial class Runtime : DependencyInjection.ISceneManager
    {

        internal Runtime()
        {
            InitializeQueue();
        }

        internal void Reset()
        {
            UntrackScenes();
            UntrackPreload();
            UntrackCollections();
        }

    }

}
