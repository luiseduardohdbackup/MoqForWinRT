﻿using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Moq.Matchers;

#if NETFX_CORE
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fact = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
using Assert = Moq.Tests.AssertExtender;
#else
using Xunit;
#endif

namespace Moq.Tests
{
#if NETFX_CORE
	[TestClass]
#endif
	public class MatchersFixture
	{
		[Fact]
		public void MatchesAnyParameterValue()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(x => x.Echo(It.IsAny<int>())).Returns(5);
			mock.Setup(x => x.Execute(It.IsAny<string>())).Returns("foo");

			Assert.Equal(5, mock.Object.Echo(5));
			Assert.Equal(5, mock.Object.Echo(25));
			Assert.Equal("foo", mock.Object.Execute("hello"));
			Assert.Equal("foo", mock.Object.Execute((string)null));
		}

		[Fact]
		public void MatchesPredicateParameter()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(x => x.Echo(It.Is<int>(value => value < 5 && value > 0)))
				.Returns(1);
			mock.Setup(x => x.Echo(It.Is<int>(value => value <= 0)))
				.Returns(0);
			mock.Setup(x => x.Echo(It.Is<int>(value => value >= 5)))
				.Returns(2);

			Assert.Equal(1, mock.Object.Echo(3));
			Assert.Equal(0, mock.Object.Echo(0));
			Assert.Equal(0, mock.Object.Echo(-5));
			Assert.Equal(2, mock.Object.Echo(5));
			Assert.Equal(2, mock.Object.Echo(6));
		}

		[Fact]
		public void MatchesRanges()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(x => x.Echo(It.IsInRange(1, 5, Range.Inclusive))).Returns(1);
			mock.Setup(x => x.Echo(It.IsInRange(6, 10, Range.Exclusive))).Returns(2);

			Assert.Equal(1, mock.Object.Echo(1));
			Assert.Equal(1, mock.Object.Echo(2));
			Assert.Equal(1, mock.Object.Echo(5));

			Assert.Equal(2, mock.Object.Echo(7));
			Assert.Equal(2, mock.Object.Echo(9));
		}

		[Fact]
		public void DoesNotMatchOutOfRange()
		{
			var mock = new Mock<IFoo>(MockBehavior.Strict);

			mock.Setup(x => x.Echo(It.IsInRange(1, 5, Range.Exclusive))).Returns(1);

			Assert.Equal(1, mock.Object.Echo(2));

			var mex = Assert.Throws<MockException>(() => mock.Object.Echo(1));
			Assert.Equal(MockException.ExceptionReason.NoSetup, mex.Reason);
		}

		[Fact]
		public void RangesCanIncludeVariableAndMethodInvocation()
		{
			var mock = new Mock<IFoo>();
			var from = 1;

			mock.Setup(x => x.Echo(It.IsInRange(from, GetToRange(), Range.Inclusive))).Returns(1);

			Assert.Equal(1, mock.Object.Echo(1));
		}

		[Fact]
		public void RangesAreEagerEvaluated()
		{
			var mock = new Mock<IFoo>(MockBehavior.Loose);
			var from = "a";
			var to = "d";

			mock.Setup(x => x.Execute(It.IsInRange(from, to, Range.Inclusive))).Returns("ack");

			Assert.Equal("ack", mock.Object.Execute("b"));

			from = "c";

			Assert.Equal("ack", mock.Object.Execute("b"));
		}

		[Fact]
		public void RegexMatchesAndEagerlyEvaluates()
		{
			var mock = new Mock<IFoo>(MockBehavior.Loose);
			var reg = "[a-d]+";

			mock.Setup(x => x.Execute(It.IsRegex(reg, RegexOptions.IgnoreCase))).Returns("foo");
			mock.Setup(x => x.Execute(It.IsRegex(reg))).Returns("bar");

			Assert.Equal("bar", mock.Object.Execute("b"));
			Assert.Equal("bar", mock.Object.Execute("abc"));
			Assert.Equal("foo", mock.Object.Execute("B"));
			Assert.Equal("foo", mock.Object.Execute("BC"));

			reg = "[c-d]+";

			// Will still match both the 1 and 2 return values we had.
			Assert.Equal("bar", mock.Object.Execute("b"));
			Assert.Equal("foo", mock.Object.Execute("B"));
		}

		[Fact]
		public void MatchesEvenNumbersWithLambdaMatching()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(x => x.Echo(It.Is<int>(i => i % 2 == 0))).Returns(1);

			Assert.Equal(1, mock.Object.Echo(2));
		}

		[Fact]
		public void MatchesDifferentOverloadsWithItIsAny()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(foo => foo.DoTypeOverload(It.IsAny<Bar>()))
				.Returns(true);
			mock.Setup(foo => foo.DoTypeOverload(It.IsAny<Baz>()))
				.Returns(false);

			bool bar = mock.Object.DoTypeOverload(new Bar());
			bool baz = mock.Object.DoTypeOverload(new Baz());

			Assert.True(bar);
			Assert.False(baz);
		}

		[Fact]
		public void CanExternalizeLambda()
		{
			var foo = new Mock<IFoo>();

			Expression<Func<int, bool>> isSix = (arg) => arg == 6;

			foo.Setup((f) => f.Echo(It.Is(isSix))).Returns(12);

			Assert.Equal(12, foo.Object.Echo(6));
		}

		[Fact]
		public void MatchesSameReference()
		{
			var a = new object();
			var b = new object();

			var matcher = new RefMatcher(a);
			Assert.True(matcher.Matches(a));
			Assert.False(matcher.Matches(b));
		}

		[Fact]
		public void MatchesEnumerableParameterValue()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(x => x.DoAddition(new int[] { 2, 4, 6 })).Returns(12);

			Assert.Equal(12, mock.Object.DoAddition(new[] { 2, 4, 6 }));
		}

		[Fact]
		public void DoesNotMatchDifferentEnumerableParameterValue()
		{
			var mock = new Mock<IFoo>();

			mock.Setup(x => x.DoAddition(new[] { 2, 4, 6 })).Returns(12);

			Assert.Equal(0, mock.Object.DoAddition(new[] { 2, 4 }));
			Assert.Equal(0, mock.Object.DoAddition(new[] { 2, 4, 5 }));
			Assert.Equal(0, mock.Object.DoAddition(new[] { 2, 4, 6, 8 }));
		}

		private int GetToRange()
		{
			return 5;
		}

		public class Bar { }
		public class Baz { }

		public interface IFoo
		{
			int Echo(int value);
			string Execute(string command);
			bool DoTypeOverload(Bar bar);
			bool DoTypeOverload(Baz baz);
			int DoAddition(int[] numbers);
			int[] Items { get; set; }
		}
	}
}
