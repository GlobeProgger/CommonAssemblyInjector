namespace CommonAssemblyInjector
{
    public static class Extensions
    {
        public static string StripQuotes(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length > 2)
            {
                text = text.Substring(1, text.Length - 2);
            }

            if (text.StartsWith("'") && text.EndsWith("'") && text.Length > 2)
            {
                text = text.Substring(1, text.Length - 2);
            }

            return text;
        }
    }
}