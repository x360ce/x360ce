using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JocysCom.WebSites.Engine
{
	[Serializable]
	public partial class LinkItem : IEquatable<LinkItem>
	{

		public LinkItem(ItemType type, Guid id, string name)
		{
			InitEmpty();
			_Id = id;
			_Name = name;
			_Type = type;
		}

		private ItemType _Type;

		public ItemType Type
		{
			get { return _Type; }
			set { _Type = value; }
		}

		private Guid _Id;

		public Guid Id
		{
			get { return _Id; }
			set { _Id = value; }
		}


		private string _Name;

		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}

		public LinkItem()
		{
			InitEmpty();
		}

		private void InitEmpty()
		{
			_Id = Guid.Empty;
			_Name = string.Empty;
			_Type = ItemType.None;
		}


		public override int GetHashCode()
		{
			return base.GetHashCode();
		}


		public static LinkItem Empty
		{
			get { return new LinkItem(); }
		}

		public bool IsEmpty
		{
			get { return Equals(LinkItem.Empty); }
		}

		#region IEquatable

		public bool Equals(LinkItem item)
		{
			return _Id.Equals(item.Id) && _Name.Equals(item.Name) && _Type == item.Type;
		}

		public override bool Equals(object o)
		{
			return Equals((LinkItem)o);
		}

		#endregion

	}
}

