using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines core members for ASM models.</summary>
    public interface IASMModel : INotifyPropertyChanged
    {

        /// <summary>Gets the id of this <see cref="IASMModel"/>.</summary>
        public string id { get; }

        /// <summary>Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</summary>
        /// <param name="propertyName">
        /// The property that was changed. 
        /// <para>Pass <see langword="null"/> to use caller name.</para> 
        /// <para>Pass <see cref="string.Empty"/> to notify all properties has changed.</para>
        /// </param>
        public void OnPropertyChanged([CallerMemberName] string propertyName = null);

    }

}
