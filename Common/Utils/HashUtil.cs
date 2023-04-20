using System;
using System.Security.Cryptography;
using System.Text;

namespace Common.Utils
{
	public static class HashUtil
	{
		public static string Hash(string input)
		{
			if (input is null)
			{
				throw new ArgumentNullException(nameof(input));
			}

			var inputBytes = Encoding.ASCII.GetBytes(input);
			var hashBytes = MD5.HashData(inputBytes);
			var sb = new StringBuilder();
			
			for (int i = 0; i < hashBytes.Length; i++)
			{
				sb.Append(hashBytes[i].ToString("x2"));
			}

			return sb.ToString();
		}
	}
}
