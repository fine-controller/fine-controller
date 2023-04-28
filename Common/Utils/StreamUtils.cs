using System;
using System.IO;

namespace Common.Utils
{
	public static class StreamUtils
	{
		public static Stream FromString(string value)
		{
			if (value is null) // empty/whitespace acceptable
			{
				throw new ArgumentNullException(value);
			}

			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			
			writer.Write(value);
			writer.Flush();

			stream.Position = 0;

			return stream;
		}
	}
}
