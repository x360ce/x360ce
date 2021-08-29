using System.Windows;

namespace JocysCom.ClassLibrary.Controls
{
	public interface IBaseWithHeaderControl<T>
	{
		void SetTitle(string format, params object[] args);
		void SetHead(string format, params object[] args);
		void SetBody(MessageBoxImage image, string content = null, params object[] args);
		void SetBodyError(string content, params object[] args);
		void SetBodyInfo(string content, params object[] args);
		void AddTask(T name);
		void RemoveTask(T name);
	}
}
