using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using System.Windows.Controls;
using System;
using System.Windows.Media;
using System.IO;
using System.Windows.Documents;
using System.Reflection;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class ControlsHelper
	{
		public static void EnableWithDelay(UIElement control)
		{
			Task.Run(async () =>
			{
				await Task.Delay(500).ConfigureAwait(true);
				control.Dispatcher.Invoke(() => control.IsEnabled = true);
			});
		}

		/// <summary>
		/// Set form TopMost if one of the application forms is top most.
		/// </summary>
		/// <param name="win"></param>
		public static void CheckTopMost(Window win)
		{
			// If this form is not set as TopMost but one of the application forms is on TopMost then...
			// Make this dialog form TopMost too or user won't be able to access it.
			if (!win.Topmost && System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Any(x => x.TopMost))
				win.Topmost = true;
		}

		public static void AutoSizeByOpenForms(Window win, int addSize = -64)
		{
			var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().First();
			win.Width = form.Width + addSize;
			win.Height = form.Height + addSize;
			win.Top = form.Top - addSize / 2;
			win.Left = form.Left - addSize / 2;
		}

		public static bool IsDesignMode(UIElement component)
		{
			if (!_IsDesignMode.HasValue)
				_IsDesignMode = IsDesignMode1(component);
			return _IsDesignMode.Value;
		}

		private static bool IsDesignMode1(UIElement component)
		{
			if (DesignerProperties.GetIsInDesignMode(component))
				return true;
			//If WPF hosted in WinForms.
			var ea = System.Reflection.Assembly.GetEntryAssembly();
			if (ea != null && ea.Location.Contains("VisualStudio"))
				return true;
			//If WPF hosted in WinForms.
			ea = System.Reflection.Assembly.GetExecutingAssembly();
			if (ea != null && ea.Location.Contains("VisualStudio"))
				return true;
			return false;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(Label control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Content as string != text)
				control.Content = text;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(TextBox control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Text != text)
				control.Text = text;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(TextBlock control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Text != text)
				control.Text = text;
		}

		public static void SetTextFromResource(RichTextBox box, string resourceName)
		{
			var rtf = Helper.FindResource<byte[]>(Assembly.GetEntryAssembly(), resourceName);
			var ms = new MemoryStream(rtf);
			var textRange = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);
			textRange.Load(ms, DataFormats.Rtf);
			ms.Dispose();
			//var xdoc = new System.Xml.XmlDocument();
			//xdoc.LoadXml(System.Windows.Markup.XamlWriter.Save(box.Document));
			//var xml = xdoc.OuterXml;
			//xdoc.Save("document.xml");
			box.Document.PagePadding = new Thickness(8);
			box.IsDocumentEnabled = true;
			HookHyperlinks(box, null);
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetChecked(System.Windows.Controls.Primitives.ToggleButton control, bool check)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			if (control.IsChecked != check)
				control.IsChecked = check;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		public static void SetEnabled(UIElement control, bool enabled)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			if (control.IsEnabled != enabled)
				control.IsEnabled = enabled;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		public static void SetVisible(UIElement control, bool enabled)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
			if (control.Visibility != visibility)
				control.Visibility = visibility;
		}

		/// <summary>
		/// Convert Bitmap to image source.
		/// </summary>
		public static ImageSource GetImageSource(System.Drawing.Bitmap bitmap)
		{
			var bi = new System.Windows.Media.Imaging.BitmapImage();
			var ms = new MemoryStream();
			bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			bi.BeginInit();
			bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
			bi.StreamSource = ms;
			bi.EndInit();
			ms.Dispose();
			return bi;
		}

		private static void HookHyperlinks(object sender, TextChangedEventArgs e)
		{
			var doc = (sender as RichTextBox).Document;
			for (var position = doc.ContentStart;
				position != null && position.CompareTo(doc.ContentEnd) <= 0;
				position = position.GetNextContextPosition(LogicalDirection.Forward))
			{
				if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
				{
					if (position.Parent is Hyperlink link)
						link.RequestNavigate += link_RequestNavigate;
					else if (position.Parent is Span span)
					{
						var range = new TextRange(span.ContentStart, span.ContentEnd);
						if (Uri.TryCreate(range.Text, UriKind.Absolute, out var uriResult))
						{
							if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
							{
								var h = new Hyperlink(range.Start, range.End);
								h.RequestNavigate += link_RequestNavigate;
								h.NavigateUri = new Uri(range.Text);
								h.Cursor = System.Windows.Input.Cursors.Hand;
							}
						}
					}
				}
			}
		}
		private static void link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			var link = (Hyperlink)sender;
			OpenUrl(link.NavigateUri.AbsoluteUri);
			e.Handled = true;
		}

		#region IsVisibleToUser

		public static Point[] GetPoints(Control control, bool relative = false)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var pos = relative
				? new Point(0, 0)
				// Get control position on the screen
				: control.PointToScreen(new Point(0, 0));
			var pointsToCheck =
				new Point[]
					{
						// Top-Left.
						pos,
						// Top-Right.
						new Point(pos.X + control.ActualWidth - 1, pos.Y),
						// Bottom-Left.
						new Point(pos.X, pos.Y + control.ActualHeight - 1),
						// Bottom-Right.
						new Point(pos.X + control.ActualWidth - 1, pos.Y + control.ActualHeight - 1),
						// Middle-Centre.
						new Point(pos.X + control.ActualWidth/2, pos.Y + control.ActualHeight/2)
					};
			return pointsToCheck;
		}

		/*
		public static bool IsControlVisibleToUser(Control control)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var handle = (PresentationSource.FromVisual(control) as System.Windows.Interop.HwndSource)?.Handle;
			if (!handle.HasValue)
				return false;
			var children = GetAll<DependencyObject>(control, true);
			// Return true if any of the controls is visible.
			var pointsToCheck = GetPoints(control, true);
			foreach (var p in pointsToCheck)
			{
				//var hwnd = NativeMethods.WindowFromPoint(p);
				//if (hwnd == IntPtr.Zero)
				//	continue;
				var result = VisualTreeHelper.HitTest(control, p);
				if (result == null)
					continue;
				if (children.Contains(result.VisualHit))
					return true;
				//var other = Control.FromChildHandle(hwnd);
				//if (other == null)
				//	continue;
				//if (GetAll(control, null, true).Contains(other))
			}
			return false;
		}
		*/

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<DependencyObject> GetAll(DependencyObject control, Type type = null, bool includeTop = false)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			// Create new list.
			var controls = new List<DependencyObject>();
			// Add top control if required.
			if (includeTop)
				controls.Add(control);
			// If control contains children then...
			var childrenCount = VisualTreeHelper.GetChildrenCount(control);
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(control, i);
				var children = GetAll(child, null, true);
				controls.AddRange(children);
			}
			// If type filter is not set then...
			return (type == null)
				? controls
				: controls.Where(x => type.IsInterface ? x.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.GetType()));
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(Control control, bool includeTop = false)
		{
			if (control == null)
				return new T[0];
			return GetAll(control, typeof(T), includeTop).Cast<T>().ToArray();
		}

		#endregion

		#region Apply Grid Border Style

		public static void ApplyBorderStyle(DataGrid grid, bool updateEnabledProperty = false)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));
			grid.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
			//grid.BorderThickness = BorderStyle.None;
			//grid.EnableHeadersVisualStyles = false;
			//grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			//grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			//grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
			//grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			//grid.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			//grid.BackColor = SystemColors.Window;
			//grid.DefaultCellStyle.BackColor = SystemColors.Window;
			//grid.CellPainting += Grid_CellPainting;
			//grid.SelectionChanged += Grid_SelectionChanged;
			//grid.CellFormatting += Grid_CellFormatting;
			//if (updateEnabledProperty)
			//	grid.CellClick += Grid_CellClick;
		}

		/*
		private static void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If add new record row.
			if (grid.AllowUserToAddRows && e.RowIndex + 1 == grid.Rows.Count)
				return;
			var column = grid.Columns[e.ColumnIndex];
			var item = grid.Rows[e.RowIndex].DataBoundItem;
			if (column.DataPropertyName == "Enabled" || column.DataPropertyName == "IsEnabled")
			{
				SetEnabled(item, !GetEnabled(item));
				grid.Invalidate();
			}
		}

		private static void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If add new record row.
			if (grid.AllowUserToAddRows && e.RowIndex + 1 == grid.Rows.Count)
				return;
			var row = grid.Rows[e.RowIndex];
			if (e.RowIndex > -1 && e.ColumnIndex > -1)
			{
				var item = row.DataBoundItem;
				// If grid is virtual then...
				if (item == null)
				{
					var list = grid.DataSource as IBindingList;
					if (list != null)
						item = list[e.RowIndex];
				}
				var enabled = true;
				if (item != null)
					enabled = GetEnabled(item);
				var fore = enabled ? grid.DefaultCellStyle.ForeColor : SystemColors.ControlDark;
				var selectedBack = enabled ? grid.DefaultCellStyle.SelectionBackColor : SystemColors.ControlDark;
				// Apply style to row header.
				if (row.HeaderCell.Style.ForeColor != fore)
					row.HeaderCell.Style.ForeColor = fore;
				if (row.HeaderCell.Style.SelectionBackColor != selectedBack)
					row.HeaderCell.Style.SelectionBackColor = selectedBack;
				// Apply style to cell
				var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
				if (cell.Style.ForeColor != fore)
					cell.Style.ForeColor = fore;
				if (cell.Style.SelectionBackColor != selectedBack)
					cell.Style.SelectionBackColor = selectedBack;
			}
		}

		private static void Grid_SelectionChanged(object sender, EventArgs e)
		{
			// Sort issue with paint artifcats.
			var grid = (DataGridView)sender;
			grid.Invalidate();
		}

		private static void SetEnabled(object item, bool enabled)
		{
			var enabledProperty = item.GetType().GetProperties().FirstOrDefault(x => x.Name == "Enabled" || x.Name == "IsEnabled");
			if (enabledProperty != null)
			{
				enabledProperty.SetValue(item, enabled, null);
			}
		}

		private static bool GetEnabled(object item)
		{
			var enabledProperty = item.GetType().GetProperties().FirstOrDefault(x => x.Name == "Enabled" || x.Name == "IsEnabled");
			var enabled = enabledProperty == null ? true : (bool)enabledProperty.GetValue(item, null);
			return enabled;
		}

		private static void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Header and cell borders must be set to "Single" style.
			var grid = (DataGridView)sender;
			var firstVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Min(x => x.Index);
			var lastVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Max(x => x.Index);
			var selected = e.RowIndex > -1 ? grid.Rows[e.RowIndex].Selected : false;
			e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
			var bounds = e.CellBounds;
			var tl = new Point(bounds.X, bounds.Y);
			var tr = new Point(bounds.X + bounds.Width - 1, bounds.Y);
			var bl = new Point(bounds.X, bounds.Y + bounds.Height - 1);
			var br = new Point(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1);
			Color backColor;
			// If top left corner and column header then...
			if (e.RowIndex == -1)
			{
				backColor = selected
					? grid.ColumnHeadersDefaultCellStyle.SelectionBackColor
					: grid.ColumnHeadersDefaultCellStyle.BackColor;
			}
			// If row header then...
			else if (e.ColumnIndex == -1 && e.RowIndex > -1)
			{
				var row = grid.Rows[e.RowIndex];
				backColor = selected
					? row.HeaderCell.Style.SelectionBackColor
					: grid.RowHeadersDefaultCellStyle.BackColor;
			}
			// If normal cell then...
			else
			{
				var row = grid.Rows[e.RowIndex];
				var cell = row.Cells[e.ColumnIndex];
				backColor = selected
					? cell.InheritedStyle.SelectionBackColor
					: cell.InheritedStyle.BackColor;
			}
			// Cell background colour.
			var back = new Pen(backColor, 1);
			// Border colour.
			var border = new Pen(SystemColors.Control, 1);
			// Do not draw borders for selected device.
			Pen c;
			// Top
			e.Graphics.DrawLine(back, tl, tr);
			// Left (only if not first)
			c = !selected && e.ColumnIndex > firstVisibleColumn ? border : back;
			e.Graphics.DrawLine(c, bl, tl);
			// Right (always)
			c = back;
			e.Graphics.DrawLine(c, tr, br);
			// Bottom (always)
			c = border;
			e.Graphics.DrawLine(c, bl, br);
			back.Dispose();
			border.Dispose();
			e.Handled = true;
		}

		*/

		#endregion

	}
}
