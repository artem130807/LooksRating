using System;

namespace Solution
{
    public class LoginGenerator
    {
        public string[] GenerateLogins(string firstNames, string lastNames)
        {
            var firstNameList = firstNames.Split(',');
            var lastNameList = lastNames.Split(',');
            var logins = new string[firstNameList.Length];

            for (var i = 0; i < firstNameList.Length; i++)
            {
                var namePart = firstNameList[i][..3];
                var lastName = lastNameList[i];
                var surnamePart = string.Concat(lastName[4], lastName[3], lastName[2]);
                logins[i] = string.Concat(namePart, surnamePart).ToUpperInvariant();
            }

            return logins;
        }
    }
}
