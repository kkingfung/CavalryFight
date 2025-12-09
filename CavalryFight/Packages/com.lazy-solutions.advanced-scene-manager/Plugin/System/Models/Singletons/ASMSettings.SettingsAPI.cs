using AdvancedSceneManager.Models.Interfaces;

namespace AdvancedSceneManager.Models
{

    partial class ASMSettings : ISettingsAPI
    {
#if UNITY_EDITOR
        ASMUserSettings ISettingsAPI.user => ASMUserSettings.instance;
#endif
        ASMSettings ISettingsAPI.project => this;
        Profile ISettingsAPI.profile => SceneManager.profile;
    }

}
