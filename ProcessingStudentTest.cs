using System;
using System.Linq;
using System.Collections.Generic;

namespace Solution
{
    public abstract class Test
    {
        protected int score;

        protected Test(int score)
        {
            this.score = score;
        }

        public abstract string Evaluate();
    }

    public class SimpleTest : Test
    {
        public SimpleTest(int score) : base(score)
        {
        }

        public override string Evaluate()
        {
            return score >= 60 ? "Pass" : "Fail";
        }
    }

    public class AdvancedTest : Test
    {
        public AdvancedTest(int score) : base(score)
        {
        }

        public override string Evaluate()
        {
            if (score >= 90) return "A";
            if (score >= 75) return "B";
            if (score >= 60) return "C";
            return "Fail";
        }
    }

    public class ProcessingStudentTest
    {
        private readonly List<string> _results = new();

        public ProcessingStudentTest(string surnamesInput, string testResultsInput)
        {
            var surnames = surnamesInput.Split(';');
            var testResults = testResultsInput.Split(';');

            for (var i = 0; i < surnames.Length; i++)
            {
                var parts = testResults[i].Split(' ');
                var testType = parts[0];
                var mark = int.Parse(parts[1]);

                Test test = testType == "S"
                    ? new SimpleTest(mark)
                    : new AdvancedTest(mark);

                var surname = surnames[i];
                var formattedSurname = char.ToUpper(surname[0]) + surname.Substring(1).ToLower();
                _results.Add($"{formattedSurname} {test.Evaluate()}");
            }
        }

        public IList<string> PrintResults()
        {
            return _results;
        }
    }
}
