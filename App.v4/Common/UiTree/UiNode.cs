using System.Collections.Generic;
using System.Runtime.Serialization;

namespace x360ce.App.UiTree
{
	/// <summary>One element of the interface, as a screen reader or an automation tool sees it.</summary>
	/// <remarks>
	/// Written to <c>docs/ui-tree.json</c> by the program itself, so the answer always describes
	/// the build it came from rather than a document somebody remembered to update.
	/// </remarks>
	[DataContract]
	public class UiNode
	{
		/// <summary>Name the element is found by, from AccessibleName, then Text, then the field name.</summary>
		[DataMember(Order = 1)]
		public string Name { get; set; }

		/// <summary>What the element is for, from AccessibleDescription.</summary>
		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string Description { get; set; }

		/// <summary>What kind of thing it is: Tab, Button, CheckBox, Grid, and so on.</summary>
		[DataMember(Order = 3)]
		public string Role { get; set; }

		/// <summary>Field name in the source, so a reader can find the element in the code.</summary>
		[DataMember(Order = 4, EmitDefaultValue = false)]
		public string Id { get; set; }

		/// <summary>Type that supplies this element, when it is a control the program defines.</summary>
		[DataMember(Order = 5, EmitDefaultValue = false)]
		public string Type { get; set; }

		/// <summary>Name of the shared control this node stands for, in place of repeating it.</summary>
		/// <remarks>
		/// A control placed four times - one per controller - is described once under Controls and
		/// referred to here, so the document says each thing once.
		/// </remarks>
		[DataMember(Order = 6, EmitDefaultValue = false)]
		public string SameAs { get; set; }

		/// <summary>Lowest value the element accepts, when it holds a number.</summary>
		[DataMember(Order = 7, EmitDefaultValue = false)]
		public int? Min { get; set; }

		/// <summary>Highest value the element accepts, when it holds a number.</summary>
		/// <remarks>
		/// Left empty where one setting is offered through controls that disagree - a slider in per
		/// cent beside a box in raw units - because naming one of the two would misdescribe the other.
		/// </remarks>
		[DataMember(Order = 8, EmitDefaultValue = false)]
		public int? Max { get; set; }

		/// <summary>False when the element is present but not shown in this state of the program.</summary>
		[DataMember(Order = 9, EmitDefaultValue = false)]
		public bool Hidden { get; set; }

		/// <summary>Elements inside this one.</summary>
		[DataMember(Order = 10, EmitDefaultValue = false)]
		public List<UiNode> Items { get; set; }

		public void Add(UiNode child)
		{
			if (Items == null)
				Items = new List<UiNode>();
			Items.Add(child);
		}

		public override string ToString()
		{
			return Role + " " + Name;
		}
	}
}
