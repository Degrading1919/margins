// Draft implementation — Unity verification pending
using System;

namespace Margins
{
    public static class FirstStoreIdentifier
    {
        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            {
                return false;
            }

            if (!IsAsciiLetterOrDigit(value[0]) || !IsAsciiLetterOrDigit(value[value.Length - 1]))
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!IsAsciiLetterOrDigit(character) &&
                    character != '-' &&
                    character != '_' &&
                    character != '.')
                {
                    return false;
                }

                if (character >= 'A' && character <= 'Z')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAsciiLetterOrDigit(char character)
        {
            return (character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z') ||
                   (character >= '0' && character <= '9');
        }
    }

    internal static class FirstStoreEquality
    {
        public static bool AreEqual<T>(T left, T right)
            where T : class, IEquatable<T>
        {
            return ReferenceEquals(left, right) ||
                   (left != null && left.Equals(right));
        }
    }
}
