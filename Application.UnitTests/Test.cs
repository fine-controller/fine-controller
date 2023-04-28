using NUnit.Framework;

namespace Application.UnitTests
{
	public class Test
	{
		[Test]
		public void Test1() => Assert.That((object)true, Is.True);
	}
}
