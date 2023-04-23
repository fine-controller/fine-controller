namespace Common.Models
{
	public class AppSettings
	{
		public string RootPath { get; set; }
		public bool IsProduction { get; set; }
		public string PORT { get; set; }
		public string SPEC_PATH { get; set; }
		public string SPEC_FORMAT { get; set; }
	}
}
