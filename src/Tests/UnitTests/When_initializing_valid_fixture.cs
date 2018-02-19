using FluentAssertions;
using NUnit.Framework;
using Kekiri.TestSupport.Scenarios;

namespace Kekiri.UnitTests
{
    [TestFixture]
    class When_initializing_valid_fixture
    {
        readonly When_initializing_valid_fixture_scenario _scenario = new When_initializing_valid_fixture_scenario();

        [OneTimeSetUp]
        public void When()
        {
            _scenario.SetupScenario();
        }

        [Test]
        public void It_should_call_given()
        {
            _scenario.GivenRunCount.Should().Be(1);
        }

        [Test]
        public void And_when()
        {
            _scenario.WhenRunCount.Should().Be(1);
        }

        [Test]
        public void But_not_then()
        {
            _scenario.ThenRunCount.Should().Be(0);
        }
    }
}