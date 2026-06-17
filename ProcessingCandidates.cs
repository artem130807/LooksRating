using System;
using System.Linq;
using System.Collections.Generic;

namespace Solution
{
    public class ProcessingCandidates
    {
        public string Name { get; set; }
        public int Rating { get; set; }

        public IList<string> PrintCorrectCandidates(string scoresInput, string namesInput)
        {
            var scores = scoresInput.Split(',').Select(int.Parse).ToArray();
            var names = namesInput.Split(',');

            var average = scores.Average();
            var result = new List<string>();

            for (var i = 0; i < scores.Length; i++)
            {
                if (scores[i] > average)
                    result.Add(names[i]);
            }

            if (result.Count == 0)
                result.Add("нет");

            return result;
        }
    }
}
