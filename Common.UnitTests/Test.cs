using NUnit.Framework;

namespace Common.UnitTests
{
	public class Test
	{
		[Test]
		public void Test1() => Assert.That((object)true, Is.True);
	}
}
