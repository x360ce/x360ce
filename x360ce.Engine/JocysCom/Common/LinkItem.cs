using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.WebSites.Engine
{
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

		public ItemType Type { get { return _Type; } set { _Type = value; OnPropertyChanged(); } }
		[NonSerialized]
		ItemType _Type;

		public Guid Id { get { return _Id; } set { _Id = value; OnPropertyChanged(); } }
		[NonSerialized]
		Guid _Id;

		public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged(); } }
		[NonSerialized]
		string _Name;

		public static readonly LinkItem Empty = new LinkItem();

		public bool IsEmpty { get { return Id == Guid.Empty && string.IsNullOrEmpty(Name) && Type == ItemType.None; } }

		#region IEquatable

		public static bool operator ==(LinkItem a, LinkItem b)
		{
			// If both are null, or both are same instance, return true.
			if (ReferenceEquals(a, b))
				return true;
			// If one is null, but not both, return false.
			if (a == null || b == null)
				return false;
			// Return true if the fields match:
			return a.Id == b.Id && a.Name == b.Name && a.Type == b.Type;
		}

		public static bool operator !=(LinkItem a, LinkItem b)
		{
			return !(a == b);
		}

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
			if (o == null)
				return false;
			return this == o as LinkItem;
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var ev = PropertyChanged;
			if (ev == null)
				return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}

