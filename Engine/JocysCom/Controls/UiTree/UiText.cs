#nullable disable
using System;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>The name and purpose of each part of the interface, in one place.</summary>
	/// <remarks>
	/// These are written onto the accessible name and description - AccessibleName and
	/// AccessibleDescription in Windows Forms, AutomationProperties.Name and HelpText in WPF - which
	/// is what a screen reader announces and what an automation tool searches by. The same two
	/// properties are what the exported navigation tree is built from, so a control described here
	/// is described everywhere at once, and there is one place to correct a wrong word.
	///
	/// Keys are "OwningType.FieldName", the same pair a developer sees in the designer. A control
	/// already named where it is built keeps that name; nothing here overwrites a deliberate one.
	/// Each program supplies its own list through <see cref="Catalog"/>. Windows Forms controls are
	/// written in UiText.Forms.cs and WPF elements in UiText.WPF.cs.
	/// </remarks>
	public static partial class UiText
	{
		/// <summary>What one element is called and what it is for.</summary>
		public struct Text
		{
			public Text(string name, string purpose, bool documentOnly = false)
			{
				Name = name;
				Purpose = purpose;
				DocumentOnly = documentOnly;
			}

			public readonly string Name;
			public readonly string Purpose;

			/// <summary>True where the name describes the element but must not be written onto it.</summary>
			public readonly bool DocumentOnly;
		}

		/// <summary>
		/// An element whose text is its value rather than its label: a reading on the status bar,
		/// the help header.
		/// </summary>
		/// <remarks>
		/// A label carries no value of its own, so what a screen reader reads out is its name. Give
		/// one a fixed name and the reading disappears behind it - "Controller rate" announced over
		/// and over while the number it is announcing can no longer be heard at all. So the name is
		/// kept for the exported document, where a fixed name is what is wanted, and the element
		/// itself is given only its purpose.
		/// </remarks>
		public static Text Live(string name, string purpose)
		{
			return new Text(name, purpose, true);
		}

		/// <summary>Builds the program's list, keyed "OwningType.FieldName". Set once at start; read the first time it is needed.</summary>
		public static Func<Dictionary<string, Text>> Catalog
		{
			get { return _catalog; }
			// A new list replaces the one already read, so a test can give its own.
			set { _catalog = value; _items = null; }
		}

		static Func<Dictionary<string, Text>> _catalog;

		static Dictionary<string, Text> _items;

		static Dictionary<string, Text> Items
		{
			get
			{
				if (_items == null)
					_items = _catalog == null ? new Dictionary<string, Text>() : _catalog();
				return _items;
			}
		}

		static bool Find(Type owner, string field, string key, out Text text)
		{
			text = default(Text);
			if (!string.IsNullOrEmpty(key))
				return Items.TryGetValue(key, out text);
			if (owner == null || string.IsNullOrEmpty(field))
				return false;
			return Items.TryGetValue(owner.Name + "." + field, out text);
		}
	}
}
