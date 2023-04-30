using Common.Utils;
using NUnit.Framework;
using Shouldly;
using System;

namespace Common.UnitTests.HashUtilTests
{
	internal class HashUtil_Hash
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

			string input = "Hello, world!";

			// act
			
			string actualHash = HashUtil.Hash(input);

			// assert

			actualHash.ShouldBe("3e25960a79dbc69b674cd4ec67a72c62");
		}
	}
}
