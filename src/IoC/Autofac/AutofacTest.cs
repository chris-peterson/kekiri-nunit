namespace Kekiri.IoC.Autofac
{
    public class AutofacTest : IoCTest
    {
        public AutofacTest() : base(new AutofacContainer(Customizations))
        {
        }

        internal static CustomizeBehaviorApi Customizations { get; set; }
    }
}