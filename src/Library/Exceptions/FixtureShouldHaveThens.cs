namespace Kekiri.Exceptions
{
    internal class FixtureShouldHaveThens : ScenarioTestException
    {
        public FixtureShouldHaveThens(object test)
            : base(test, GetMessage(test))
        {
        }

        private static string GetMessage(object test)
        {
            string messageDetail = string.Empty;

            if (test is Test)
                messageDetail = "; a then method should be a parameterless public method that uses the [Then] attribute";

            return "No thens found" + messageDetail;
        }
    }
}