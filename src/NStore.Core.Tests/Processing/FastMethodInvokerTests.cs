using System;
using NStore.Core.Processing;
using Xunit;

namespace NStore.Core.Tests.Processing
{
    public class FastMethodInvokerTests
    {
        private readonly Target _target = new Target();

        [Fact]
        public void invoker_correctly_invoke_method()
        {
            FastMethodInvoker.CallPublicReturningVoid(_target, "DoSomething", "hello");
            Assert.Equal("hello", _target.Param);
        }

        [Fact]
        public void invoker_correctly_invoke_method_with_return_type()
        {
            var result = (string)FastMethodInvoker.CallPublic(_target, "DoSomethingReturn", "hello");
            Assert.Equal("hello", _target.Param);
            Assert.Equal("processed hello", result);
        }

        [Fact]
        public void invoker_correctly_dispatch_to_object()
        {
            var result = (string)FastMethodInvoker.CallPublic(_target, "DoSomethingWithObjectReturn", new object());
            Assert.Equal("processed", result);
        }

        [Fact]
        public void invoker_on_optional_public_method_should_not_mask_exception()
        {
            Assert.Throws<TargetException>(() =>
                FastMethodInvoker.CallPublicReturningVoid(_target, "FailPublic", new object())
            );
        }

        [Fact]
        public void invoker_on_private_method_should_not_mask_exception()
        {
            Assert.Throws<TargetException>(() =>
                FastMethodInvoker.CallNonPublicIfExists(_target, "FailPrivate", new object())
            );
        }

        [Fact]
        public void invoker_on_private_methods_should_not_mask_exception()
        {
            Assert.Throws<TargetException>(() =>
                FastMethodInvoker.CallNonPublicIfExists(_target, new[] { "FailPrivate", "FailPrivate" }, new object())
            );
        }
    }
}