using System;
using System.Text.RegularExpressions;

namespace Utils.General
{
    internal static class LangUtils
    {
        public static TimeSpan Seconds(this int self) => TimeSpan.FromSeconds(self);
        public static TimeSpan Seconds(this double self) => TimeSpan.FromSeconds(self);
        public static string HumanizeCamelCase(this string str) => Regex.Replace(str, "(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])", " ");
    }
}