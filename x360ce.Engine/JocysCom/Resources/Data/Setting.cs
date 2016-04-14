using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Resources.Data
{
	public partial class Setting
	{
		public Setting()
		{
			Id = Guid.Empty;
			CultureCode = "en";
			DateCreated = DateTime.Now;
			DateUpdated = DateCreated;
			IsEnabled = true;
			Description = "";
			IsExpression = false;
			IsSerialized = false;
			Replacement = "";
		}

		private static List<Type> _Primitives;
		public static List<Type> Primitives
		{
			get
			{
				if (_Primitives == null)
				{
					_Primitives = typeof(int).Assembly.GetTypes().Where(x => x.IsPrimitive).ToList();
					_Primitives.Add(typeof(string));
					_Primitives.Add(typeof(decimal));
					_Primitives.Add(typeof(Version));
					_Primitives = _Primitives.OrderBy(x => x.FullName).ToList();
				}

				return _Primitives;
			}
		}

		public static Dictionary<DataPriority, Color> DataPriorityBack;
		public static Dictionary<DataPriority, Color> DataPriorityFore;
		public static Dictionary<DataPriority, Color> DataPrioritySelectionBack;
		public static Dictionary<DataPriority, Color> DataPrioritySelectionFore;

		private static object DataPriorityLock = new object();
		public static void UpdateDataPriorityColors()
		{
			lock (DataPriorityLock)
			{
				// If all data is available then...
				if (DataPriorityBack != null && DataPriorityFore != null && DataPrioritySelectionBack != null && DataPrioritySelectionFore != null)
				{
					// Simply return.
					return;
				}
				// Reset lists.
				DataPriorityBack = new Dictionary<DataPriority, Color>();
				DataPriorityFore = new Dictionary<DataPriority, Color>();
				DataPrioritySelectionBack = new Dictionary<DataPriority, Color>();
				DataPrioritySelectionFore = new Dictionary<DataPriority, Color>();
				// Fill with default colors.
				DataPriorityBack.Add(DataPriority.Highest, Color.Red);
				DataPriorityBack.Add(DataPriority.VeryHigh, Color.Orange);
				DataPriorityBack.Add(DataPriority.High, Color.Yellow);
				DataPriorityBack.Add(DataPriority.AboveNormal, Color.LightYellow);
				DataPriorityBack.Add(DataPriority.Normal, Color.White);
				DataPriorityBack.Add(DataPriority.BelowNormal, Color.LightGreen);
				DataPriorityBack.Add(DataPriority.Low, Color.Green);
				DataPriorityBack.Add(DataPriority.VeryLow, Color.Blue);
				DataPriorityBack.Add(DataPriority.Lowest, Color.Violet);
				// Clone default values.
				foreach (var key in DataPriorityBack.Keys)
				{
					DataPriorityFore.Add(key, Color.Black);
					DataPrioritySelectionBack.Add(key, DataPriorityBack[key]);
					DataPrioritySelectionFore.Add(key, Color.Black);
				}
				// Get data priority colours from database.
				var colours = GetTransforms("en", null, ResourceSection.DataPriority.ToString());
				foreach (var c in colours)
				{
					DataPriority dp = default(DataPriority);
					if (Enum.TryParse(c.KeyName, out dp))
					{
						var all = c.KeyValue.Split(',');
						if (all.Length > 0)
							DataPriorityBack[dp] = ColorTranslator.FromHtml(all[0]);
						if (all.Length > 1)
							DataPriorityFore[dp] = ColorTranslator.FromHtml(all[1]);
						if (all.Length > 2)
							DataPrioritySelectionBack[dp] = ColorTranslator.FromHtml(all[2]);
						if (all.Length > 3)
							DataPrioritySelectionFore[dp] = ColorTranslator.FromHtml(all[3]);
					}
				}
			}
		}

		public static string GetValue(string category, string section, string key)
		{
			var db = new ResourcesModelContainer();
			db.Settings.MergeOption = System.Data.Objects.MergeOption.OverwriteChanges;
			var item = db.Settings.FirstOrDefault(x => x.CultureCode == "en" & x.Category == category & x.Section == section & x.KeyName == key);
			return item == null ? null : item.KeyValue;
		}

		public static void SetValue(string category, string section, string key, object value)
		{
			var db = new ResourcesModelContainer();
			db.Settings.MergeOption = System.Data.Objects.MergeOption.OverwriteChanges;
			var item = db.Settings.FirstOrDefault(x => x.CultureCode == "en" & x.Category == category & x.Section == section & x.KeyName == key);
			if (item == null)
			{
				item = new Setting();
				item.Category = category;
				item.Section = section;
				item.KeyName = key;
				db.Settings.AddObject(item);
			}
			item.KeyValue = value.ToString();
			if (value == null)
			{
				item.KeyType = "";
			}
			else
			{
				item.KeyType = value.GetType().Name;
			}
			db.SaveChanges();
		}

		private Regex m_RegexValue;
		[XmlIgnore()]
		public Regex RegexValue
		{
			get
			{
				if (m_RegexValue == null)
				{
					RegexOptions options = RegexOptions.None;
					if (this.Options != null)
					{
						options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
					}
					m_RegexValue = new Regex(this.KeyValue, options);
				}
				return m_RegexValue;
			}
		}

		public static List<Setting> GetTransforms(string culture, string category, string section)
		{
			culture = culture ?? "";
			category = category ?? "";
			section = section ?? "";
			var db = new ResourcesModelContainer();
			var query = from item in db.Settings where ((culture == string.Empty) || item.CultureCode == culture) && ((category == string.Empty) || item.Category == category) && ((section == string.Empty) || item.Section == section) orderby item.KeyName select item;
			return query.ToList();
		}

		/// <summary>
		/// Indicates whether the list contains a match in the input string.
		/// </summary>
		/// <param name="s">The string to search for a match.</param>
		/// <param name="list">List of Transforms.</param>
		/// <returns> true if the Transform finds a match; otherwise, false.</returns>
		public static bool IsMatch(string s, List<Setting> list)
		{
			return IndexOf(s, list) > -1;
		}

		/// <summary>
		/// Indicates whether the list contains a match in the input string.
		/// </summary>
		/// <param name="s">The string to search for a match.</param>
		/// <param name="list">List of Transforms.</param>
		/// <returns>Index of match; otherwise, -1.</returns>
		public static int IndexOf(string s, List<Setting> list)
		{
			Setting item = null;
			for (int i = 0; i <= list.Count - 1; i++)
			{
				item = list[i];
				var found = false;
				if (item.IsExpression)
				{
					found = item.RegexValue.IsMatch(s);
				}
				else
				{
					found = s == item.KeyValue;
				}
				if (found)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Within a specified input string, replaces all strings that match a 
		/// Transform pattern with a specified replacement string.
		/// </summary>
		/// <param name="s">The string to search for a match.</param>
		/// <param name="list">Transform List.</param>
		/// <returns></returns>
		public static string Replace(string s, List<Setting> list)
		{
			Setting item = null;
			for (int i = 0; i <= list.Count - 1; i++)
			{
				item = list[i];
				if (item.IsExpression)
				{
					s = item.RegexValue.Replace(s, item.Replacement);
				}
				else
				{
					s = s.Replace(item.KeyValue, item.Replacement);
				}
			}
			return s;
		}


	}
}
