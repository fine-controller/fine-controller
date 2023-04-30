using Common.Utils;
using NUnit.Framework;
using Shouldly;
using System;

namespace Common.UnitTests.HashUtilTests
{
	internal class Hash
	{
		[Test]
		public void WhenInputIsNull_ThrowsArgumentNullException()
		{
			// arrange

			var input = default(string);

			// act + assert
			
			Should.Throw<ArgumentNullException>(() => HashUtil.Hash(input));
		}

		[Test]
		public void WhenInputIsEmpty_ReturnsExpectedHash()
		{
			// arrange

			var input = "";

			// act

			var actualHash = HashUtil.Hash(input);

			// assert
			
			actualHash.ShouldBe("d41d8cd98f00b204e9800998ecf8427e");
		}

		[Test]
		public void WhenInputIsNonEmpty_ReturnsExpectedHash()
		{
			// arrange

			var input = "Hello, world!";

			// act
			
			var actualHash = HashUtil.Hash(input);

			// assert

			actualHash.ShouldBe("6cd3556deb0da54bca060b4c39479839");
		}
	}
}
