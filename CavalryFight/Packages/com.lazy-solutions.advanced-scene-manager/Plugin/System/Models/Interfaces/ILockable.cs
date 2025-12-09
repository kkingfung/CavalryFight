using System.ComponentModel;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Specifies a object that can be locked, using <see cref="AdvancedSceneManager.Editor.Utility.LockUtility"/>.</summary>
    /// <remarks>Available, but no effect in build.</remarks>
    public interface ILockable : INotifyPropertyChanged
    {

        /// <summary>Gets if this <see cref="ILockable"/> is locked.</summary>
        public bool isLocked { get; set; }

        /// <summary>Gets or sets the message to be displayed when unlocking this <see cref="ILockable"/>.</summary>
        public string lockMessage { get; set; }

        /// <summary>Saves this <see cref="ILockable"/>.</summary>
        public void Save();

    }

}
