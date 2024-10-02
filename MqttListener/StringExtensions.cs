namespace MqttListener
{
    public static class StringExtensions
    {
        public static string SnakeCaseToWords(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var words = input.Split('_');
            var result = words.Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower());
            return string.Join(' ', result);
        }
    }
}