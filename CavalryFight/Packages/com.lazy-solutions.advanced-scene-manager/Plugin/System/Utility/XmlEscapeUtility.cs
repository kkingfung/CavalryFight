namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides methods for escaping and unescaping XML strings.</summary>
    public static class XmlEscapeUtility
    {

        /// <summary>Escapes special XML characters in the given string.</summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        /// <summary>Unescapes XML entities in the given string.</summary>
        public static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&");
        }
    }

}
