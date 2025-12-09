using System.Collections.Generic;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines properties for collections whose scene list can be modified.</summary>
    /// <remarks>Provides extension methods, see <see cref="ASMModelExtensions.Add{T}(T, Scene[])"/> for example.</remarks>
    public interface IEditableCollection : ISceneCollection
    {

        /// <summary>The list of scenes that this collection manages.</summary>
        public List<Scene> sceneList { get; }

    }

}
