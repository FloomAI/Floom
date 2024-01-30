namespace Floom.Entities
{
    public class MainDto
    {
        public string id { get; set; }
        public string schema { get; set; }
        public string kind { get; set; }

        public Dictionary<string, string?> ConvertToDictionary()
        {
            var dictionary = new Dictionary<string, string?>();
            foreach (var prop in GetType().GetProperties())
            {
                var value = prop.GetValue(this, null);
                if (value != null)
                {
                    dictionary.Add(prop.Name, value.ToString());
                }
            }

            return dictionary;
        }
    }
}