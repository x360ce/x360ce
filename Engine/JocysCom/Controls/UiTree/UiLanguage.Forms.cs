#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>Records the English of Windows Forms controls, shows them in the language chosen, and gives the English back.</summary>
	public static partial class UiLanguage
	{
		/// <summary>One property of one element that shows a text: its key, its English, and what was last put on it.</summary>
		sealed class Slot
		{
			internal string Property;
			internal string Key;
			/// <summary>The key of this one instance of a control used in several places, which a translation may use when the places differ.</summary>
			internal string InstanceKey;
			internal string English;
			/// <summary>The English the designer gave, which the keys were translated from.</summary>
			internal string Original;
			internal string Applied;
			internal ToolTip Tip;
			/// <summary>False once the program put words of its own on the element: from then on they are the program's to change.</summary>
			internal bool Owned = true;
		}

		/// <summary>One text as found, for the sync.</summary>
		sealed class Found
		{
			internal string English;
			internal string InstanceKey;
			internal WeakReference Element;
			internal string Role;
		}

		static readonly ConditionalWeakTable<object, List<Slot>> Slots = new ConditionalWeakTable<object, List<Slot>>();
		static readonly List<WeakReference> Recorded = new List<WeakReference>();
		static readonly Dictionary<string, List<Found>> Texts = new Dictionary<string, List<Found>>(StringComparer.Ordinal);

		/// <summary>
		/// Records the English of everything inside a control and shows it in the language chosen. Call it once the designer
		/// has built the control and before the program puts words of its own on it; calling it again records only what is new.
		/// </summary>
		public static void Register(Control root)
		{
			if (root == null)
				return;
			var owner = OwnerOf(root.Parent);
			Register(root, owner, null, owner == null ? null : TipsOf(OwnerControlOf(root.Parent)));
		}

		/// <summary>Marks an element whose words are the same in every language, such as a language's name in itself, so it is never recorded.</summary>
		public static void Keep(object element)
		{
			List<Slot> slots;
			if (!Slots.TryGetValue(element, out slots))
				Slots.Add(element, slots = new List<Slot>());
			slots.Add(new Slot { Property = KeepProperty, Owned = false });
		}

		/// <summary>The property of the slot <see cref="Keep"/> adds, which stands for every property.</summary>
		const string KeepProperty = "*";

		/// <summary>Records menu entries that hang off no control, such as the menu of a tray icon.</summary>
		public static void Register(ToolStripItemCollection items, Type owner)
		{
			RegisterItems(items, owner, null);
		}

		static void Register(Control control, Type owner, string instance, List<ToolTip> tips)
		{
			if (IsOwnComposite(control))
			{
				// A control placed in several places is keyed by its type, and each place by where it is as well.
				instance = owner != null && !string.IsNullOrEmpty(control.Name) ? owner.Name + "." + control.Name : null;
				owner = control.GetType();
				tips = TipsOf(control);
				if (control is Form)
					Add(control, "Text", owner.Name + ".Text", null, null);
			}
			else if (owner != null && !string.IsNullOrEmpty(control.Name))
			{
				if (ShowsText(control) && UiText.LiveNameFor(control) == null)
					Add(control, "Text", Key(owner, control.Name, "Text"), Key(instance, control.Name, "Text"), null);
				if (tips != null)
					foreach (var tip in tips)
						Add(control, "ToolTip", Key(owner, control.Name, "ToolTip"), Key(instance, control.Name, "ToolTip"), tip);
				var grid = control as DataGridView;
				if (grid != null)
					foreach (DataGridViewColumn column in grid.Columns)
						if (!string.IsNullOrEmpty(column.Name))
						{
							Add(column, "HeaderText", Key(owner, column.Name, "HeaderText"), Key(instance, column.Name, "HeaderText"), null);
							Add(column, "ToolTipText", Key(owner, column.Name, "ToolTipText"), Key(instance, column.Name, "ToolTipText"), null);
						}
			}
			foreach (Control child in control.Controls)
				Register(child, owner, instance, tips);
			var strip = control as ToolStrip;
			if (strip != null && owner != null)
				RegisterItems(strip.Items, owner, instance);
			if (control.ContextMenuStrip != null && owner != null)
				RegisterItems(control.ContextMenuStrip.Items, owner, instance);
		}

		static void RegisterItems(ToolStripItemCollection items, Type owner, string instance)
		{
			foreach (ToolStripItem item in items)
			{
				if (string.IsNullOrEmpty(item.Name))
					continue;
				// A box on a bar holds a value, and a reading on a bar follows the program's state.
				if (!(item is ToolStripControlHost) && UiText.LiveNameFor(item) == null)
					Add(item, "Text", Key(owner, item.Name, "Text"), Key(instance, item.Name, "Text"), null);
				Add(item, "ToolTipText", Key(owner, item.Name, "ToolTipText"), Key(instance, item.Name, "ToolTipText"), null);
				var parent = item as ToolStripDropDownItem;
				if (parent != null)
					RegisterItems(parent.DropDownItems, owner, instance);
			}
		}

		static string Key(Type owner, string field, string property)
		{
			return owner.Name + "." + field + "." + property;
		}

		static string Key(string instance, string field, string property)
		{
			return instance == null ? null : instance + "." + field + "." + property;
		}

		/// <summary>Records one property, when it shows words, and puts the language chosen on it.</summary>
		static void Add(object element, string property, string key, string instanceKey, ToolTip tip)
		{
			List<Slot> slots;
			if (Slots.TryGetValue(element, out slots))
				foreach (var existing in slots)
					if (existing.Property == KeepProperty || (existing.Property == property && existing.Tip == tip))
						return;
			var english = Get(element, property, tip);
			if (!IsWords(english, element))
				return;
			if (slots == null)
			{
				slots = new List<Slot>();
				Slots.Add(element, slots);
				Recorded.Add(new WeakReference(element));
			}
			var slot = new Slot { Property = property, Key = key, InstanceKey = instanceKey, English = english, Original = english, Applied = english, Tip = tip };
			slots.Add(slot);
			List<Found> found;
			if (!Texts.TryGetValue(key, out found))
				Texts[key] = found = new List<Found>();
			found.Add(new Found { English = english, InstanceKey = instanceKey, Element = new WeakReference(element), Role = element.GetType().Name });
			Show(element, slot);
		}

		/// <summary>True for text a person reads: it has a letter, and it is not the field name a designer leaves on a new control.</summary>
		static bool IsWords(string text, object element)
		{
			if (string.IsNullOrWhiteSpace(text) || !text.Any(char.IsLetter))
				return false;
			var name = (element as Control)?.Name ?? (element as ToolStripItem)?.Name ?? (element as DataGridViewColumn)?.Name;
			return !string.Equals(text, name, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>The kinds of control that show their Text as words: not a box, whose text is a value, nor a panel, which shows none.</summary>
		static bool ShowsText(Control control)
		{
			return control is Label || control is ButtonBase || control is GroupBox || control is TabPage;
		}

		/// <summary>Puts the language chosen on one property, or its English back.</summary>
		static void Show(object element, Slot slot)
		{
			if (!slot.Owned)
				return;
			// The program put words of its own on it since, which a language change must not overwrite.
			if (!string.Equals(Get(element, slot.Property, slot.Tip), slot.Applied, StringComparison.Ordinal))
			{
				slot.Owned = false;
				return;
			}
			var text = Find(slot.InstanceKey, slot.English) ?? Find(slot.Key, slot.English) ?? slot.English;
			if (string.Equals(text, slot.Applied, StringComparison.Ordinal))
				return;
			Set(element, slot.Property, slot.Tip, text);
			slot.Applied = text;
		}

		static bool IsDisposed(object element)
		{
			var control = element as Control;
			if (control != null)
				return control.IsDisposed;
			var item = element as ToolStripItem;
			if (item != null)
				return item.IsDisposed;
			var column = element as DataGridViewColumn;
			return column != null && column.DataGridView != null && column.DataGridView.IsDisposed;
		}

		static partial void Reapply()
		{
			for (var i = Recorded.Count - 1; i >= 0; i--)
			{
				var element = Recorded[i].Target;
				List<Slot> slots;
				// A control that is gone shows nothing any more, and is forgotten.
				if (element == null || IsDisposed(element) || !Slots.TryGetValue(element, out slots))
				{
					Recorded.RemoveAt(i);
					continue;
				}
				foreach (var slot in slots)
					Show(element, slot);
			}
		}

		static readonly MethodInfo ComboUpdateText = typeof(ComboBox).GetMethod("UpdateText", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Words the items of lists again, for lists that word them through <see cref="T"/> in their Format event, once the
		/// language changed. The same item stays chosen, though each list raises its selection events again.
		/// </summary>
		public static void Relist(params ListControl[] lists)
		{
			foreach (var list in lists)
			{
				if (!list.FormattingEnabled || list.IsDisposed)
					continue;
				// A list makes its items' words again whenever formatting is switched.
				list.FormattingEnabled = false;
				list.FormattingEnabled = true;
				// A ComboBox whose handle is not created yet keeps the previous language's text in WindowText until UpdateText runs.
				if (list is ComboBox && list.SelectedIndex >= 0)
					ComboUpdateText?.Invoke(list, null);
			}
		}

		/// <summary>
		/// Puts English the program chose on a recorded element, such as a caption read from data, and shows it in the
		/// language chosen when it is the English the element's key was translated from. The element keeps following a
		/// change of language; an element never recorded is given the English.
		/// </summary>
		public static void SetText(object element, string property, string english)
		{
			List<Slot> slots;
			Slot slot = null;
			if (Slots.TryGetValue(element, out slots))
				slot = slots.FirstOrDefault(x => x.Property == property && x.Tip == null);
			if (slot == null)
			{
				Set(element, property, null, english);
				return;
			}
			// The designer's words with an access key mark the same words without it.
			var original = english == slot.Original || english != WithoutAccessKey(slot.Original) ? english : slot.Original;
			var text = Find(slot.InstanceKey, original) ?? Find(slot.Key, original) ?? english;
			Set(element, property, null, text);
			slot.English = english;
			slot.Applied = text;
			slot.Owned = true;
		}

		/// <summary>Text without the &amp; that marks an access key, &amp;&amp; kept as one &amp;.</summary>
		static string WithoutAccessKey(string text)
		{
			return text == null ? null : text.Replace("&&", "\0").Replace("&", "").Replace("\0", "&");
		}

		/// <summary>
		/// The English of what an element shows, for a reader that must not depend on the language chosen, such as the
		/// interface description and an AI assistant's search: the English recorded when the element shows its
		/// translation, otherwise what it shows.
		/// </summary>
		public static string SourceOf(object element, string property, string shown)
		{
			List<Slot> slots;
			if (element == null || Culture == SourceCulture || !Slots.TryGetValue(element, out slots))
				return shown;
			foreach (var slot in slots)
				if (slot.Property == property && slot.Tip == null && slot.Owned && string.Equals(shown, slot.Applied, StringComparison.Ordinal))
					return slot.English;
			return shown;
		}

		/// <summary>
		/// The texts recorded, for the sync: one entry for each key, and one for each place where a control used in several
		/// places shows other English in each. Each carries the kind of element and what it is for, for the translator.
		/// </summary>
		public static List<UiLanguageEntry> RecordedTexts()
		{
			var list = new List<UiLanguageEntry>();
			foreach (var pair in Texts)
			{
				var englishes = pair.Value.Select(x => x.English).Distinct(StringComparer.Ordinal).Count();
				if (englishes == 1)
				{
					list.Add(Describe(pair.Key, pair.Value[0]));
					continue;
				}
				foreach (var found in pair.Value)
					if (found.InstanceKey != null)
						list.Add(Describe(found.InstanceKey, found));
			}
			return list;
		}

		static UiLanguageEntry Describe(string key, Found found)
		{
			return new UiLanguageEntry { Key = key, Source = found.English, Role = found.Role, Purpose = PurposeOf(found.Element.Target) };
		}

		/// <summary>What an element is for: its own description, or that of the nearest control around it that has one.</summary>
		static string PurposeOf(object element)
		{
			var item = element as ToolStripItem;
			if (item != null && !string.IsNullOrEmpty(item.AccessibleDescription))
				return item.AccessibleDescription;
			var column = element as DataGridViewColumn;
			var control = element as Control ?? (item == null ? null : (Control)item.Owner) ?? (column == null ? null : column.DataGridView);
			for (; control != null; control = control.Parent)
				if (!string.IsNullOrEmpty(control.AccessibleDescription))
					return control.AccessibleDescription;
			return null;
		}

		/// <summary>
		/// The name or purpose <see cref="UiText"/> gave an element, in the language chosen, for where it is shown to a
		/// person, such as a window's help line. The element itself keeps the English, which a screen reader and an AI
		/// assistant's search read.
		/// </summary>
		/// <param name="property">"AccessibleName" or "AccessibleDescription": the key is the catalog's key and this.</param>
		public static string HelpOf(object element, string property, string english)
		{
			var key = CatalogKeyOf(element);
			return key == null ? english : Find(key + "." + property, english) ?? english;
		}

		/// <summary>The key <see cref="UiText"/> describes an element under: its bare type name for a panel the program defines, otherwise its owner's and its field name.</summary>
		static string CatalogKeyOf(object element)
		{
			var item = element as ToolStripItem;
			var control = element as Control;
			if (item != null)
			{
				var owner = OwnerOf(item.Owner);
				return owner == null || string.IsNullOrEmpty(item.Name) ? null : owner.Name + "." + item.Name;
			}
			if (control == null)
				return null;
			if (IsOwnComposite(control))
				return control.GetType().Name;
			var type = OwnerOf(control);
			return type == null || string.IsNullOrEmpty(control.Name) ? null : type.Name + "." + control.Name;
		}

		#region Properties

		static string Get(object element, string property, ToolTip tip)
		{
			if (tip != null)
				return tip.GetToolTip((Control)element);
			var column = element as DataGridViewColumn;
			if (column != null)
				return property == "HeaderText" ? column.HeaderText : column.ToolTipText;
			var item = element as ToolStripItem;
			if (item != null)
				return property == "Text" ? item.Text : item.ToolTipText;
			return ((Control)element).Text;
		}

		static void Set(object element, string property, ToolTip tip, string text)
		{
			if (tip != null)
			{
				tip.SetToolTip((Control)element, text);
				return;
			}
			var column = element as DataGridViewColumn;
			if (column != null)
			{
				if (property == "HeaderText")
					column.HeaderText = text;
				else
					column.ToolTipText = text;
				return;
			}
			var item = element as ToolStripItem;
			if (item != null)
			{
				if (property == "Text")
					item.Text = text;
				else
					item.ToolTipText = text;
				return;
			}
			((Control)element).Text = text;
		}

		#endregion

		#region Owners

		/// <summary>The type a control's fields belong to: the nearest panel or window the program defines, itself included.</summary>
		static Type OwnerOf(Control control)
		{
			var owner = OwnerControlOf(control);
			return owner == null ? null : owner.GetType();
		}

		static Control OwnerControlOf(Control control)
		{
			for (var walk = control; walk != null; walk = walk.Parent)
				if (IsOwnComposite(walk))
					return walk;
			return null;
		}

		static bool IsOwnComposite(Control control)
		{
			return (control is UserControl || control is Form) && UiTreeWalker.IsOwnType(control.GetType());
		}

		/// <summary>The hint components a panel or window made in its designer, which hold the hints of the controls on it.</summary>
		static List<ToolTip> TipsOf(Control composite)
		{
			if (composite == null)
				return null;
			List<ToolTip> tips = null;
			const BindingFlags fields = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			for (var type = composite.GetType(); type != null && UiTreeWalker.IsOwnType(type); type = type.BaseType)
				foreach (var field in type.GetFields(fields))
					if (typeof(ToolTip).IsAssignableFrom(field.FieldType))
					{
						var tip = field.GetValue(composite) as ToolTip;
						if (tip != null)
							(tips ?? (tips = new List<ToolTip>())).Add(tip);
					}
			return tips;
		}

		#endregion
	}
}
