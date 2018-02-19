using FluentAssertions;
using NUnit.Framework;
using Kekiri.TestSupport.Scenarios.DepthTest;

namespace Kekiri.UnitTests.DepthTest
{
    [TestFixture]
    class When_test_has_givens_at_multiple_inheritence_levels 
    {
        readonly When_scenario_test_has_derived_depth2 _test = new When_scenario_test_has_derived_depth2();

        [OneTimeSetUp]
        public void When()
        {
            _test.SetupScenario();
        }

        [Test]
        public void It_should_have_correct_number_of_invocations()
        {
            _test.Levels.Count.Should().Be(3);
        }

        [Test]
        public void It_should_call_base_first()
        {
            _test.Levels[0].Should().Be(ScenarioDepthTestLevel.Base);
        }

        [Test]
        public void It_should_call_depth1_second()
        {
            _test.Levels[1].Should().Be(ScenarioDepthTestLevel.Depth1);
        }

        [Test]
        public void It_should_call_depth2_third()
        {
            _test.Levels[2].Should().Be(ScenarioDepthTestLevel.Depth2);
        }
    }
}