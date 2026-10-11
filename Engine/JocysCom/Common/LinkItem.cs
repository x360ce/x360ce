#nullable disable

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.ClassLibrary
{
	/// <summary>
	/// Represents a reference to a named business object, identified by a GUID and categorized by ItemType.
	/// Supports change notification for data binding.
	/// </summary>
	[Serializable]
	public partial class LinkItem : IEquatable<LinkItem>, INotifyPropertyChanged
	{

		public LinkItem()
		{
			InitEmpty();
		}

		public LinkItem(ItemType type, Guid id, string name)
		{
			Type = type;
			Id = id;
			Name = name;
		}

		private void InitEmpty()
		{
			Id = Guid.Empty;
			Name = string.Empty;
			Type = ItemType.None;
		}

		/// <summary>Business object category this LinkItem refers to (e.g., Book, Member).</summary>
		public ItemType Type { get { return _Type; } set { _Type = value; OnPropertyChanged(); } }
		[NonSerialized]
		ItemType _Type;

		/// <summary>Unique identifier (GUID) of the referenced business object.</summary>
		public Guid Id { get { return _Id; } set { _Id = value; OnPropertyChanged(); } }
		[NonSerialized]
		Guid _Id;

		/// <summary>Display name of the referenced business object.</summary>
		public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged(); } }
		[NonSerialized]
		string _Name;

		/// <summary>Read-only empty LinkItem instance (no Id, Name, or Type).</summary>
		public static readonly LinkItem Empty = new LinkItem();

		/// <summary>True if this instance represents an empty/default link (no Id, Name, or Type).</summary>
		public bool IsEmpty { get { return Id == Guid.Empty && string.IsNullOrEmpty(Name) && Type == ItemType.None; } }

		#region IEquatable

		/// <summary>Determines whether two LinkItem instances are equal based on Id, Name, and Type, handling nulls.</summary>
		public static bool operator ==(LinkItem a, LinkItem b)
		{
			// If both are null, or both are same instance, return true.
			if (ReferenceEquals(a, b))
				return true;
			// If one is null, but not both, return false.
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			// Return true if the fields match:
			return a.Id == b.Id && a.Name == b.Name && a.Type == b.Type;
		}

		/// <summary>Determines whether two LinkItem instances are not equal.</summary>
		public static bool operator !=(LinkItem a, LinkItem b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Returns a hash code for this instance using default reference-based implementation.
		/// </summary>
		/// <remarks>
		/// This implementation does not consider Id, Name, or Type; two LinkItem instances considered equal by == may produce different hash codes, leading to inconsistent behavior in hash-based collections.
		/// </remarks>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool Equals(LinkItem item)
		{
			return this == item;
		}

		public override bool Equals(object o)
		{
			if (o is null)
				return false;
			return this == o as LinkItem;
		}

		#endregion

		#region INotifyPropertyChanged

		// SUPPRESS: CWE-502: Deserialization of Untrusted Data
		// Fix: Apply [field: NonSerialized] attribute to an event inside class with [Serializable] attribute.
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}