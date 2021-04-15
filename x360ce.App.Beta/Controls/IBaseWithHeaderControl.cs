using System.Windows;

namespace x360ce.App.Controls
{
	public interface IBaseWithHeaderControl
	{
		void SetTitle(string format, params object[] args);
		void SetHead(string format, params object[] args);
		void SetBody(MessageBoxImage image, string content = null, params object[] args);
		void SetBodyError(string content, params object[] args);
		void SetBodyInfo(string content, params object[] args);
		void AddTask(TaskName name);
		void RemoveTask(TaskName name);
	}
}
