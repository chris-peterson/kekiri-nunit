namespace Kekiri.Exceptions
{
    internal class FixtureShouldHaveWhens : ScenarioTestException
    {
        public FixtureShouldHaveWhens(object test)
            : base(test, GetMessage(test))
        {
        }

        private static string GetMessage(object test)
        {
            string messageDetail = string.Empty;

            if (test is Test)
                messageDetail = "; a when method should be a parameterless public method that uses the [When] attribute";

            return "No whens found" + messageDetail;
        }
    }
}