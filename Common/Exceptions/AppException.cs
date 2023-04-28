using System;
using System.Runtime.Serialization;

namespace Common.Exceptions
{
	[Serializable]
	public class AppException : ApplicationException
	{
		public AppException(string message) : base(message) { }
		public AppException(string message, Exception innerException) : base(message, innerException) { }
		protected AppException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
