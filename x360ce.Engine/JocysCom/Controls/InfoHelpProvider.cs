using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace JocysCom.ClassLibrary.Controls
{
	public class InfoHelpProvider
	{
		/// <summary>
		/// Dictionary to hold controls and their corresponding help texts.
		/// </summary>
		public Dictionary<UIElement, object[]> Controls { get; set; } = new Dictionary<UIElement, object[]>();

		/// <summary>
		/// Event to trigger when mouse enters any control from `Controls`.
		/// </summary>
		public event EventHandler OnMouseEnter;

		/// <summary>
		/// Event to trigger when mouse leaves all controls from `Control`.
		/// </summary>
		public event EventHandler OnMouseLeave;

		public string GetHelpHead(UIElement control)
		{
			if (!Controls.ContainsKey(control))
				return "";
			return (string)Controls[control][0];
		}

		public MessageBoxImage GetHelpImage(UIElement control)
		{
			if (!Controls.ContainsKey(control))
				return MessageBoxImage.Information;
			return (MessageBoxImage)Controls[control][2];
		}

		public string GetHelpBody(UIElement control, int? maxLength = null, bool removeMultispace = false)
		{
			if (!Controls.ContainsKey(control))
				return "";
			var body = (string)Controls[control][1];
			if (removeMultispace)
				body = RemoveMultispace(body);
			if (maxLength != null)
				body = CropText(body, maxLength);
			return body;
		}

		// Method to add control and its help text to HelpControls and attach MouseEnter and MouseLeave events
		public void Add(UIElement control, string helpHead, string helpBody = "", MessageBoxImage image = MessageBoxImage.Information)
		{
			if (Controls.ContainsKey(control))
				Controls.Remove(control);
			Controls.Add(control, new object[] { helpHead, helpBody, image });
			control.MouseEnter += (s, e) => OnMouseEnter?.Invoke(s, e);
			control.MouseLeave += (s, e) =>
			{
				bool isMouseOutsideAll = true;
				foreach (var item in Controls.Keys)
				{
					if (item.IsMouseOver)
					{
						isMouseOutsideAll = false;
						break;
					}
				}
				if (isMouseOutsideAll) OnMouseLeave?.Invoke(s, e);
			};
		}

		// Method to remove control from HelpControls and detach MouseEnter and MouseLeave events
		public void Remove(UIElement control)
		{
			if (!Controls.ContainsKey(control))
				return;
			control.MouseEnter -= (s, e) => OnMouseEnter?.Invoke(s, e);
			control.MouseLeave -= (s, e) =>
			{
				bool isMouseOutsideAll = true;
				foreach (var item in Controls.Keys)
				{
					if (item.IsMouseOver)
					{
						isMouseOutsideAll = false;
						break;
					}
				}
				if (isMouseOutsideAll) OnMouseLeave?.Invoke(s, e);
			};
			Controls.Remove(control);
		}

		public const int CropTextDefauldMaxLength = 128;

		/// <summary>
		/// if maxLength == -1, return string.Empty
		/// if maxLength == 0 return s
		/// </summary>
		/// <param name="s"></param>
		/// <param name="maxLength"></param>
		/// <returns></returns>
		public static string CropText(object so, int? maxLength = 0)
		{
			var s = string.Format("{0}", so);
			if (!maxLength.HasValue)
				maxLength = CropTextDefauldMaxLength;
			if (string.IsNullOrEmpty(s) || maxLength == -1)
				return "";
			if (maxLength == 0)
				return s;
			if (maxLength == 0)
				maxLength = CropTextDefauldMaxLength;
			if (s.Length > maxLength)
			{
				s = s.Substring(0, maxLength.Value - 3);
				// Find last separator and crop there...
				var ls = s.LastIndexOf(' ');
				if (ls > 0) s = s.Substring(0, ls);
				s += "...";
			}
			return s;
		}

		public static readonly Regex RxMultiSpace = new Regex("[ \u00A0\r\n\t]+", RegexOptions.Multiline);

		public static string RemoveMultispace(string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;
			// Replace multiple spaces.
			s = RxMultiSpace.Replace(s, " ");
			return s;
		}



	}
}
