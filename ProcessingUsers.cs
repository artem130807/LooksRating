using System.Collections.Generic;

namespace Solution
{
    public class ProcessingUsers
    {
        public IList<string> GenerateLogins(string firstNamesInput, string lastNamesInput)
        {
            var firstNames = firstNamesInput.Split(',');
            var lastNames = lastNamesInput.Split(',');
            var result = new List<string>(firstNames.Length);

            for (var i = 0; i < firstNames.Length; i++)
            {
                var firstName = firstNames[i];
                var lastName = lastNames[i];

                var namePart = firstName[..3];
                var surnamePart = string.Concat(lastName[4], lastName[3], lastName[2]);
                result.Add(string.Concat(namePart, surnamePart).ToUpperInvariant());
            }

            return result;
        }
    }
}
