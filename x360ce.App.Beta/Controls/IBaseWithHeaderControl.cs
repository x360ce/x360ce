namespace x360ce.App.Controls
{
	public interface IBaseWithHeaderControl
	{
		void AddTask(TaskName name);
		void RemoveTask(TaskName name);
		void SetBodyError(string content, params object[] args);
		void SetBodyInfo(string content, params object[] args);
		void SetHead(string format, params object[] args);
		void SetTitle(string format, params object[] args);
	}
}
