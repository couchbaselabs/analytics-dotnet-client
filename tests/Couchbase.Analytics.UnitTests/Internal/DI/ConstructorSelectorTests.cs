using Couchbase.AnalyticsClient.Internal.DI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Internal.DI
{
    public class ConstructorSelectorTests
    {
        private class DepA { }
        private class DepB { }

        private class ClassWithPreferred
        {
            public int SelectedId { get; }

            public ClassWithPreferred(DepA a, DepB b)
            {
                SelectedId = 1;
            }

            [PreferredConstructor]
            public ClassWithPreferred(DepA a)
            {
                SelectedId = 2;
            }
        }

        private class ClassWithoutPreferred
        {
            public int SelectedId { get; }

            public ClassWithoutPreferred(DepA a)
            {
                SelectedId = 1;
            }

            public ClassWithoutPreferred(DepA a, DepB b)
            {
                SelectedId = 2;
            }
        }

        private class ClassWithMultiplePreferred
        {
            [PreferredConstructor]
            public ClassWithMultiplePreferred(DepA a) { }

            [PreferredConstructor]
            public ClassWithMultiplePreferred(DepA a, DepB b) { }
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<DepA>();
            services.AddSingleton<DepB>();

            return services.BuildServiceProvider();
        }

        [Fact]
        public void PreferredConstructor_Is_Selected_When_Present()
        {
            var sp = BuildServiceProvider();
            var factory = new SingletonServiceFactory(typeof(ClassWithPreferred));
            factory.Initialize(sp);

            var instance = (ClassWithPreferred)factory.CreateService(typeof(ClassWithPreferred));
            Assert.Equal(2, instance.SelectedId);
        }

        [Fact]
        public void MostParameterConstructor_Is_Selected_When_No_Preferred()
        {
            var sp = BuildServiceProvider();
            var factory = new SingletonServiceFactory(typeof(ClassWithoutPreferred));
            factory.Initialize(sp);

            var instance = (ClassWithoutPreferred)factory.CreateService(typeof(ClassWithoutPreferred));

            Assert.Equal(2, instance.SelectedId);
        }

        [Fact]
        public void Multiple_Preferred_Constructors_Throws()
        {
            Action act = () => ConstructorSelector.SelectConstructor(typeof(ClassWithMultiplePreferred));

            Assert.Throws<InvalidOperationException>(act.Invoke);
        }
    }
}