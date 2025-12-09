namespace AdvancedSceneManager.Loading
{

    /// <summary>An implementation of <see cref="ILoadProgressData"/> that provides a string message.</summary>
    public readonly struct MessageLoadProgressData : ILoadProgressData
    {

        /// <summary>The reported progress value.</summary>
        public float value { get; }

        /// <summary>The message of this report.</summary>
        public string message { get; }

        /// <summary />
        public MessageLoadProgressData(string message, float value)
        {
            this.message = message;
            this.value = value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{value * 100}%: {message}";
        }

    }

}
