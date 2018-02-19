using System;
using NUnit.Framework;
using Kekiri.IoC.Autofac;

namespace AutofacTests
{
    [SetUpFixture]
    public class Bootstrap
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            AutofacBootstrapper.Initialize(
                c => c.Modules.Add(new FakesModule())
            );
        }
    }

    public class FakesModule : Autofac.Module
    {
    }
}