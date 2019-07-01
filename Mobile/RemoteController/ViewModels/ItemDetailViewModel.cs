using System;

using JocysCom.x360ce.Mobile.RemoteController.Models;

namespace JocysCom.x360ce.Mobile.RemoteController.ViewModels
{
	public class ItemDetailViewModel : BaseViewModel
	{
		public Item Item { get; set; }
		public ItemDetailViewModel(Item item = null)
		{
			Title = item?.Text;
			Item = item;
		}
	}
}
