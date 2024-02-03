using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Floom.Utils
{
    public static class Extensions
    {
        public static bool OnlyIdHasValue<T>(this T obj)
        {
            PropertyInfo idProperty = typeof(T).GetProperty("id");

            if (idProperty == null)
                throw new ArgumentException("The 'id' property does not exist in the class.");

            var idValue = idProperty.GetValue(obj)?.ToString();

            if (string.IsNullOrEmpty(idValue)) // Check if id is not set
                return false;

            // Check if all other properties are null or empty
            foreach (var property in typeof(T).GetProperties())
            {
                if (property.Name != "id")
                {
                    var value = property.GetValue(obj)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        return false;
                }
            }

            return true;
        }

        public static async Task<string> GetRawBodyAsync(
            this HttpRequest request,
            Encoding? encoding = null)
        {
            if (!request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8);

            var body = await reader.ReadToEndAsync().ConfigureAwait(false);

            request.Body.Position = 0;

            return body;
        }

        public static bool Compare(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null)
                return obj1 == null && obj2 == null;

            Type type1 = obj1.GetType();
            Type type2 = obj2.GetType();

            // Check if the underlying types are different
            if (GetUnderlyingType(type1) != GetUnderlyingType(type2))
                return false;

            PropertyInfo[] properties = type1.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;
                object value1 = property.GetValue(obj1);
                object value2 = type2.GetProperty(property.Name)?.GetValue(obj2);

                // Compare property values (considering nullability and underlying types)
                if (!AreValuesEqual(propertyType, value1, value2))
                    return false;

                // Recursive comparison for nested objects
                if (!AreNestedObjectsEqual(propertyType, value1, value2))
                    return false;
            }

            return true;
        }

        private static bool AreValuesEqual(Type propertyType, object value1, object value2)
        {
            if (propertyType.IsValueType)
            {
                if (GetUnderlyingType(propertyType) != null)
                {
                    // Value type is nullable
                    return Nullable.Equals(value1, value2);
                }
                else
                {
                    // Non-nullable value type
                    return Equals(value1, value2);
                }
            }
            else
            {
                // Reference type
                return Equals(value1, value2);
            }
        }

        public static string TrimNewLines(this string input)
        {
            return input.Trim('\n');
        }

        public static string RemoveAnswerPrefix(this string input)
        {
            // Define the regular expression pattern to match the variations of "Answer:" at the beginning of the string
            string pattern = @"^(\s*answer\s*:?\s*)";

            // Use Regex.Replace to remove the matched pattern from the input string
            string result = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);

            return result;
        }

        private static bool AreNestedObjectsEqual(Type propertyType, object value1, object value2)
        {
            if (propertyType.IsValueType || propertyType == typeof(string))
            {
                // Value types or strings are not considered nested objects
                return true;
            }

            // Handle null values
            if (value1 == null || value2 == null)
                return value1 == null && value2 == null;

            // Recursive comparison for nested objects
            return Compare(value1, value2);
        }

        private static Type GetUnderlyingType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        public static string CompileWithVariables(this string input, Dictionary<string, string> variables)
        {
            //Fill Variables
            string compiledInput = input;
            if (variables == null)
            {
                return input;
            }

            foreach (var variableKvp in variables)
            {
                compiledInput = compiledInput.Replace("{{" + variableKvp.Key + "}}", variableKvp.Value);
            }

            return compiledInput.Trim();
        }
    }
}