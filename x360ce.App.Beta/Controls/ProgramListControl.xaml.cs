using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Web.Services;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for ProgramsListControl.xaml
	/// </summary>
	public partial class ProgramListControl : UserControl
	{
		public ProgramListControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			// Make font more consistent with the rest of the interface.
			MainDataGrid.AutoGenerateColumns = false;
			ImportOpenFileDialog = new OpenFileDialog();
			ExportSaveFileDialog = new SaveFileDialog();
		}

		OpenFileDialog ImportOpenFileDialog;
		SaveFileDialog ExportSaveFileDialog;

		void Programs_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPrograms();
		}

		void UpdateControlsFromPrograms()
		{
			var enabled = SettingsManager.Programs.Items.Count > 0;
			if (ExportButton.IsEnabled != enabled)
				ExportButton.IsEnabled = enabled;
		}

		/// <summary>
		/// Import Programs (Default Game Settings) from external file.
		/// </summary>
		void ImportPrograms()
		{
			var dialog = ImportOpenFileDialog;
			dialog.DefaultExt = "*.xml";
			dialog.Filter = "Game Settings (*.xml;*.xml.gz)|*.xml;*.xml.gz|All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(dialog.FileName)) dialog.FileName = "x360ce_Games";
			if (string.IsNullOrEmpty(dialog.InitialDirectory))
				dialog.InitialDirectory = EngineHelper.AppDataPath;
			dialog.Title = "Import Games Settings File";
			var result = dialog.ShowDialog();
			if (result == true)
			{
				List<x360ce.Engine.Data.Program> programs;
				if (dialog.FileName.EndsWith(".gz"))
				{
					var compressedBytes = System.IO.File.ReadAllBytes(dialog.FileName);
					var bytes = EngineHelper.Decompress(compressedBytes);
					programs = Serializer.DeserializeFromXmlBytes<List<x360ce.Engine.Data.Program>>(bytes);
				}
				else
				{
					programs = Serializer.DeserializeFromXmlFile<List<x360ce.Engine.Data.Program>>(dialog.FileName);
				}
				ImportAndBindItems(programs);
			}
		}

		/// <summary>
		/// Export Programs (Default Game Settings) to external file.
		/// </summary>
		void ExportPrograms()
		{
			var dialog = ExportSaveFileDialog;
			dialog.DefaultExt = "*.xml";
			dialog.Filter = "Game Settings (*.xml)|*.xml|Compressed Game Settings (*.xml.gz)|*.gz|All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(dialog.FileName)) dialog.FileName = "x360ce_Games";
			if (string.IsNullOrEmpty(dialog.InitialDirectory))
				dialog.InitialDirectory = EngineHelper.AppDataPath;
			dialog.Title = "Export Games Settings File";
			var result = dialog.ShowDialog();
			if (result == true)
			{
				var programs = SettingsManager.Programs.Items.ToList();
				foreach (var item in programs)
				{
					item.EntityKey = null;
					item.FileProductName = EngineHelper.FixName(item.FileProductName, item.FileName);
				}
				if (dialog.FileName.EndsWith(".gz"))
				{
					var s = Serializer.SerializeToXmlString(programs, System.Text.Encoding.UTF8, true);
					var bytes = System.Text.Encoding.UTF8.GetBytes(s);
					var compressedBytes = EngineHelper.Compress(bytes);
					System.IO.File.WriteAllBytes(dialog.FileName, compressedBytes);
				}
				else
				{
					Serializer.SerializeToXmlFile(programs, dialog.FileName, System.Text.Encoding.UTF8, true);
				}
			}

		}

		/// <summary>
		/// Delete selected Programs (Default Game Settings) from current settings.
		/// </summary>
		void DeleteSelectedPrograms()
		{
			var grid = MainDataGrid;
			var itemsToDelete = grid.SelectedItems.Cast<Engine.Data.Program>().ToArray();
			var form = new MessageBoxWindow();
			string message;
			if (itemsToDelete.Length == 1)
			{
				var item = itemsToDelete[0];
				message = string.Format("Are you sure you want to delete default settings for?\r\n\r\n\tFile Name: {0}\r\n\tProduct Name: {1}",
					item.FileName,
					item.FileProductName);
			}
			else
			{
				message = string.Format("Delete {0} default setting(s)?", itemsToDelete.Length);
			}
			var result = form.ShowDialog(message, "Delete", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
			if (result == System.Windows.MessageBoxResult.OK)
			{
				foreach (var item in itemsToDelete)
				{
					SettingsManager.Programs.Items.Remove(item);
				}
				SettingsManager.Save();
			}
		}

		/// <summary>
		/// Merge supplied list of programs with current settings.
		/// </summary>
		/// <param name="items">Programs list to merge.</param>
		void ImportAndBindItems(IList<Engine.Data.Program> items)
		{
			var grid = MainDataGrid;
			//var key = nameof(Engine.Data.Program.FileName);
			var list = SettingsManager.Programs.Items;
			//var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<string>(grid, key);
			var newItems = items.ToArray();
			ControlsHelper.SetItemsSource(grid, null);
			foreach (var newItem in newItems)
			{
				// Try to find existing item inside the list.
				var existingItems = list.Where(x => x.FileName.ToLower() == newItem.FileName.ToLower()).ToArray();
				// Remove existing items.
				for (int i = 0; i < existingItems.Length; i++)
				{
					list.Remove(existingItems[i]);
				}
				// Fix product name.
				var fixedProductName = EngineHelper.FixName(newItem.FileProductName, newItem.FileName);
				newItem.FileProductName = fixedProductName;
				// If new item is missing XInputMask setting then...
				if (newItem.XInputMask == (int)XInputMask.None)
				{
					// Assign default.
					newItem.XInputMask = (int)XInputMask.XInput13_x86;
				}
				// Add new one.
				list.Add(newItem);
			}
			Global.HMan.SetBodyInfo("{0} {1}(s) loaded.", items.Count(), typeof(Engine.Data.Program).Name);
			ControlsHelper.SetItemsSource(grid, list);
			//JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, key, selection);
			SettingsManager.Save();
		}

		/// <summary>
		/// Refresh Programs (Default Game Settings) from the cloud.
		/// </summary>
		void RefreshProgramsListFromCloud()
		{
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			var o = SettingsManager.Options;
			ws.GetProgramsCompleted += ProgramsWebServiceClient_GetProgramsCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.GetProgramsAsync(o.GetProgramsIncludeEnabled, o.GetProgramsMinInstances);
			});
		}

		void ProgramsWebServiceClient_GetProgramsCompleted(object sender, SoapHttpClientEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			ControlsHelper.BeginInvoke(() =>
			{
				Global.HMan.AddTask(TaskName.GetPrograms);
				if (e.Error != null)
				{
					var error = e.Error.Message;
					if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
					Global.HMan.SetBodyError(error);
				}
				else if (e.Result == null)
				{
					Global.HMan.SetBodyError("No results were returned by the web service!");
				}
				else
				{
					var result = (List<x360ce.Engine.Data.Program>)e.Result;
					ImportAndBindItems(result);
				}
				Global.HMan.RemoveTask(TaskName.GetPrograms);
			});
		}

		private void MainDataGrid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Delete)
				DeleteSelectedPrograms();
			else if (e.Key == System.Windows.Input.Key.Insert)
				ImportPrograms();
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DeleteButton.IsEnabled = MainDataGrid.SelectedItems.Count > 0;
		}

		private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshProgramsListFromCloud();
		}

		private void ExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ExportPrograms();
		}

		private void ImportButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ImportPrograms();
		}

		private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			DeleteSelectedPrograms();
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			// Configure Programs.
			SettingsManager.Programs.Items.ListChanged += Programs_ListChanged;
			MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
			ControlsHelper.SetItemsSource(MainDataGrid, SettingsManager.Programs.Items);
			UpdateControlsFromPrograms();
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			var selectedItems = ControlsHelper.GetSelection<string>(MainDataGrid, nameof(Engine.Data.Program.FileName));
			MainDataGrid.SelectionChanged -= MainDataGrid_SelectionChanged;
			SettingsManager.Programs.Items.ListChanged -= Programs_ListChanged;
			ControlsHelper.SetItemsSource(MainDataGrid, null);
		}

	}
}
