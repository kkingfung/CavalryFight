namespace AdvancedSceneManager.Callbacks.Events
{

    /// <summary>Specifies when the event callback is invoked for the action it represents.</summary>
    public enum When
    {
        /// <summary>Specifies that the event callback runs both <see cref="Before"/> and <see cref="After"/>, if applicable.</summary>
        /// <remarks>Some events ignore <see cref="When"/>, and will use this value.</remarks>
        Unspecified,

        /// <summary>Specifies that the event callback should be invoked before the action it represents.</summary>
        Before,

        /// <summary>Specifies that the event callback was invoked after the action it represents.</summary>
        After
    }

}
