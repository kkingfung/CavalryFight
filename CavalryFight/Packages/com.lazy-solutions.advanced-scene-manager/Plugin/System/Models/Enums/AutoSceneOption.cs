namespace AdvancedSceneManager.Models.Enums
{

    /// <summary>Specifies when ASM should open or close an auto scene.</summary>
    /// <remarks>See also: <c><see cref="Scene.SetAutoScene(Scene, AutoSceneOption)"/></c></remarks>
    public enum AutoSceneOption
    {
        /// <summary>Never open the auto scene automatically.</summary>
        Never,

        /// <summary>Only open the auto scene automatically outside of play mode.</summary>
        EditModeOnly,

        /// <summary>Only open the auto scene automatically in play mode.</summary>
        PlayModeOnly,

        /// <summary>Always open the auto scene automatically in either outside or in play mode.</summary>
        Always
    }

}
