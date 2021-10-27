using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using System.Windows.Controls;
using System;
using System.Windows.Media;
using System.IO;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Xml;
using System.Data;
using System.Windows.Controls.Primitives;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class ControlsHelper
	{
		public static void EnableWithDelay(UIElement control)
		{
			Task.Run(async delegate
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

		private static bool? _IsDesignModeWPF;

		public static bool IsDesignMode(UIElement component)
		{
			if (!_IsDesignModeWPF.HasValue)
				_IsDesignModeWPF = IsDesignMode1(component);
			return _IsDesignModeWPF.Value;
		}

		private static bool IsDesignMode1(UIElement component)
		{
			// Check 1.
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

		public static T Clone<T>(T o)
		{
			var sb = new System.Text.StringBuilder();
			var writer = XmlWriter.Create(sb, new XmlWriterSettings
			{
				Indent = true,
				ConformanceLevel = ConformanceLevel.Fragment,
				OmitXmlDeclaration = true,
				NamespaceHandling = NamespaceHandling.OmitDuplicates,
			});
			var manager = new System.Windows.Markup.XamlDesignerSerializationManager(writer);
			manager.XamlWriterMode = System.Windows.Markup.XamlWriterMode.Expression;
			System.Windows.Markup.XamlWriter.Save(o, manager);
			var stringReader = new StringReader(sb.ToString());
			var xmlReader = XmlReader.Create(stringReader);
			var item = System.Windows.Markup.XamlReader.Load(xmlReader);
			if (item == null)
				throw new ArgumentNullException("Could not be cloned.");
			return (T)item;
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
		public static void SetText(GroupBox control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Header as string != text)
				control.Header = text;
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
				? format ?? ""
				: string.Format(format ?? "", args);
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
				? format ?? ""
				: string.Format(format ?? "", args);
			if (control.Text != text)
				control.Text = text;
		}

		public static void SetTextFromResource(RichTextBox box, byte[] rtf)
		{
			var ms = new MemoryStream(rtf);
			var textRange = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);
			textRange.Load(ms, DataFormats.Rtf);
			ms.Dispose();
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
		///	<remarks>
		///	Requires NuGet Package on .NET Core: System.Drawing.Common or...
		///	set property <UseWindowsForms>true</UseWindowsForms> inside the project.
		///	</remarks>
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
		/// Get parent control of specific type.
		/// </summary>
		public static T GetParent<T>(DependencyObject control, bool includeTop = false) where T : class
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var parent = control;
			while (parent != null)
			{
				if (parent is T && (includeTop || parent != control))
					return (T)(object)parent;
				var p = VisualTreeHelper.GetParent(parent);
				if (p == null)
					p = LogicalTreeHelper.GetParent(parent);
				parent = p;
			}
			return null;
		}


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
			var visual = control as Visual;
			if (visual != null)
			{
				// If control contains visual children then...
				var childrenCount = VisualTreeHelper.GetChildrenCount(control);
				for (int i = 0; i < childrenCount; i++)
				{
					var child = VisualTreeHelper.GetChild(control, i);
					var children = GetAll(child, null, true);
					controls.AddRange(children);
				}
			}
			// Get logical children.
			var logicalChildren = LogicalTreeHelper.GetChildren(control).OfType<DependencyObject>().ToList();
			for (int i = 0; i < logicalChildren.Count; i++)
			{
				var child = logicalChildren[i];
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

		public static void GetActiveControl(FrameworkElement control, out FrameworkElement activeControl, out string activePath)
		{
			string _activePath = null;
			Invoke(() =>
			{
				_activePath = string.Format("/{0}", control?.Name);
			});
			activePath = _activePath;
			// Return current control by default.
			activeControl = control;
			// If control can contains active controls.
			var container = control as DependencyObject;
			while (container != null)
			{
				control = System.Windows.Input.FocusManager.GetFocusedElement(control) as FrameworkElement;
				if (control == null)
					break;
				Invoke(() =>
				{
					_activePath = string.Format("/{0}", control?.Name);
				});

				activePath += _activePath;
				activeControl = control;
				container = control;
			}
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

		#region Center Window

		public static void CenterWindowOnApplication(Window window)
		{
			// Get WFF window first.
			var win = System.Windows.Application.Current?.MainWindow;
			System.Drawing.Rectangle? r = null;
			var isNormal = false;
			if (win != null)
			{
				r = new System.Drawing.Rectangle((int)win.Left, (int)win.Top, (int)win.Width, (int)win.Height);
				isNormal = win.WindowState == WindowState.Normal;
			}
			else
			{
				// Try to get top windows form.
				var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().FirstOrDefault();
				if (form != null)
				{
					double l;
					double t;
					double w;
					double h;
					TransformToUnits(form.Left, form.Top, out l, out t);
					TransformToUnits(form.Width, form.Height, out w, out h);
					r = new System.Drawing.Rectangle((int)l, (int)t, (int)w, (int)h);
					isNormal = form.WindowState == System.Windows.Forms.FormWindowState.Normal;
				}
			}
			if (r.HasValue)
			{
				if (isNormal)
				{
					window.Left = r.Value.X + ((r.Value.Width - window.ActualWidth) / 2);
					window.Top = r.Value.Y + ((r.Value.Height - window.ActualHeight) / 2);
				}
				else
				{
					// Get the form screen.
					var screen = System.Windows.Forms.Screen.FromRectangle(r.Value);
					double screenWidth = screen.WorkingArea.Width;
					double screenHeight = screen.WorkingArea.Height;
					window.Left = (screenWidth / 2) - (window.Width / 2);
					window.Top = (screenHeight / 2) - (window.Height / 2);
				}
			}
		}

		/// <summary>
		/// Transforms device independent units (1/96 of an inch) to pixels.
		/// </summary>
		private static void TransformToPixels(double unitX, double unitY, out int pixelX, out int pixelY)
		{
			using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
			{
				pixelX = (int)((g.DpiX / 96) * unitX);
				pixelY = (int)((g.DpiY / 96) * unitY);
			}
		}

		/// <summary>
		/// Transforms device pixels to independent units (1/96 of an inch).
		/// </summary>
		private static void TransformToUnits(int pixelX, int pixelY, out double unitX, out double unitY)
		{
			using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
			{
				unitX = (double)pixelX / (g.DpiX / 96);
				unitY = (double)pixelY / (g.DpiX / 96);
			}
		}

		public static bool GetMainFormTopMost()
		{
			var win = System.Windows.Application.Current?.MainWindow;
			if (win != null)
				return win.Topmost;
			var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().FirstOrDefault();
			if (form != null)
				return form.TopMost;
			return false;
		}

		#endregion

		#region Data Grid Functions

		/// <summary>
		/// Get list of primary keys of items selected in the grid.
		/// </summary>
		/// <typeparam name="T">Type of Primary key.</typeparam>
		/// <param name="grid">Grid for getting selection</param>
		/// <param name="primaryKeyPropertyName">Primary key name.</param>
		public static List<T> GetSelection<T>(DataGrid grid, string keyPropertyName = null)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));
			var list = new List<T>();
			var items = grid.SelectedItems.Cast<object>().ToArray();
			// If nothing selected then try to get rows from cells.
			if (items.Length == 0)
				items = grid.SelectedCells.Cast<DataGridCellInfo>().Select(x => x.Item).Distinct().ToArray();
			// If nothing selected then return.
			if (items.Length == 0)
				return list;
			var pi = GetPropertyInfo(keyPropertyName, items[0]);
			for (var i = 0; i < items.Length; i++)
			{
				var value = GetValue<T>(items[i], keyPropertyName, pi);
				list.Add(value);
			}
			return list;
		}

		public static void RestoreSelection<T>(DataGrid grid, string keyPropertyName, List<T> list, bool selectFirst = true)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			var items = grid.Items.Cast<object>().ToArray();
			// Return if grid is empty.
			if (items.Length == 0)
				return;
			// If something to restore then...
			if (list.Count > 0)
			{
				var selectedItems = new List<object>();
				var pi = GetPropertyInfo(keyPropertyName, items[0]);
				for (var i = 0; i < items.Length; i++)
				{
					var item = items[i];
					var val = GetValue<T>(item, keyPropertyName, pi);
					if (list.Contains(val))
						selectedItems.Add(item);
				}
				if (grid.SelectionMode == DataGridSelectionMode.Single)
				{
					grid.SelectedItem = selectedItems.FirstOrDefault();
				}
				else
				{
					// Remove items which should not be selected.
					var itemsToUnselect = grid.SelectedItems.Cast<object>().Except(selectedItems);
					foreach (var item in itemsToUnselect)
						grid.SelectedItems.Remove(item);
					var itemsToSelect = selectedItems.Except(grid.SelectedItems.Cast<object>());
					foreach (var item in itemsToSelect)
						grid.SelectedItems.Add(item);
				}
			}
			// If must select first row and nothing is selected then...
			if (selectFirst && grid.SelectedItems.Count == 0)
				grid.SelectedItem = items[0];
		}

		#endregion

		#region TextBoxBase

		public static VerticalAlignment GetScrollVerticalAlignment(System.Windows.Controls.Primitives.TextBoxBase control)
		{
			// Vertical scroll position.
			var offset = control.VerticalOffset;
			// Vertical size of the scrollable content area.
			var height = control.ViewportHeight;
			// Vertical size of the visible content area.
			var visibleView = control.ExtentHeight;
			// Allow flexibility of 2 pixels.
			var flex = 2;
			if (offset + height - visibleView < flex)
				return VerticalAlignment.Bottom;
			if (offset < flex)
				return VerticalAlignment.Top;
			return VerticalAlignment.Center;
		}

		private static void AutoScroll(TextBoxBase control)
		{
			var scrollPosition = GetScrollVerticalAlignment(control);
			if (scrollPosition == VerticalAlignment.Bottom && control.IsVisible)
				control.ScrollToEnd();
		}

		public static void EnableAutoScroll(TextBoxBase control, bool enable = true)
		{
			control.TextChanged -= TextBoxBase_TextChanged;
			control.IsVisibleChanged -= TextBoxBase_IsVisibleChanged;
			control.Unloaded -= TextBoxBase_Unloaded;
			if (enable)
			{
				control.TextChanged += TextBoxBase_TextChanged;
				control.IsVisibleChanged += TextBoxBase_IsVisibleChanged;
				control.Unloaded += TextBoxBase_Unloaded;
			}
		}

		private static void TextBoxBase_Unloaded(object sender, RoutedEventArgs e)
			=> EnableAutoScroll((TextBox)sender, false);

		private static void TextBoxBase_TextChanged(object sender, TextChangedEventArgs e)
			=> AutoScroll((TextBox)sender);

		private static void TextBoxBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
			=> AutoScroll((TextBox)sender);


		#endregion

	}
}
