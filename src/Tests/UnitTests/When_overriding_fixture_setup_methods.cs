using FluentAssertions;
using NUnit.Framework;
using Kekiri.TestSupport.Scenarios;

namespace Kekiri.UnitTests
{
    [TestFixture]
    class When_overriding_fixture_setup_methods
    {
        readonly When_overriding_fixture_setup_methods_scenario _scenario = new When_overriding_fixture_setup_methods_scenario();

        [OneTimeSetUp]
        public void When()
        {
            _scenario.SetupScenario();
            _scenario.CleanupScenario();
        }

        [Test]
        public void It_should_call_setup_once()
        {
            _scenario.SetupScenairoCalledCount.Should().Be(1);
        }

        [Test]
        public void It_should_call_cleanup_once()
        {
            _scenario.CleanupScenarioCalledCount.Should().Be(1);
        }
    }
}