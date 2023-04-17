namespace Common.Interfaces
{
	// Why
	// - The general service provider does not communicate what Type of objects it resolves thereby reducing readability
	// - Also, the temptation is strong to wind up bypassing the contructor inject completly thereby reducing readability

	public interface IServiceProvider<T>
	{
		public T GetService();
		public T GetRequiredService();
	}
}