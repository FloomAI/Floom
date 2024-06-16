public static class FunctionsUtils
{
    public static string NormalizeFunctionName(string name)
    {
        return name.Replace(" ", "-").ToLower();
    }
}