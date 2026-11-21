using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce Layouts</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_Layouts")]
	[DataContract(IsReference = true)]
	public partial class Layout: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Id (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid Id {
			get => _Id;
			set => SetProperty(ref _Id, value);
		}
		private Guid _Id;

		/// <summary>Name</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string Name {
			get => _Name;
			set => SetProperty(ref _Name, value);
		}
		private string _Name;

		/// <summary>ButtonA</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonA {
			get => _ButtonA;
			set => SetProperty(ref _ButtonA, value);
		}
		private string _ButtonA;

		/// <summary>ButtonB</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonB {
			get => _ButtonB;
			set => SetProperty(ref _ButtonB, value);
		}
		private string _ButtonB;

		/// <summary>Button Back</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonBack {
			get => _ButtonBack;
			set => SetProperty(ref _ButtonBack, value);
		}
		private string _ButtonBack;

		/// <summary>Button Guide</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonGuide {
			get => _ButtonGuide;
			set => SetProperty(ref _ButtonGuide, value);
		}
		private string _ButtonGuide;

		/// <summary>Button Start</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonStart {
			get => _ButtonStart;
			set => SetProperty(ref _ButtonStart, value);
		}
		private string _ButtonStart;

		/// <summary>ButtonX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonX {
			get => _ButtonX;
			set => SetProperty(ref _ButtonX, value);
		}
		private string _ButtonX;

		/// <summary>ButtonY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string ButtonY {
			get => _ButtonY;
			set => SetProperty(ref _ButtonY, value);
		}
		private string _ButtonY;

		/// <summary>D Pad</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string DPad {
			get => _DPad;
			set => SetProperty(ref _DPad, value);
		}
		private string _DPad;

		/// <summary>D Pad Down</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string DPadDown {
			get => _DPadDown;
			set => SetProperty(ref _DPadDown, value);
		}
		private string _DPadDown;

		/// <summary>D Pad Left</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string DPadLeft {
			get => _DPadLeft;
			set => SetProperty(ref _DPadLeft, value);
		}
		private string _DPadLeft;

		/// <summary>D Pad Right</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string DPadRight {
			get => _DPadRight;
			set => SetProperty(ref _DPadRight, value);
		}
		private string _DPadRight;

		/// <summary>D Pad Up</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string DPadUp {
			get => _DPadUp;
			set => SetProperty(ref _DPadUp, value);
		}
		private string _DPadUp;

		/// <summary>Left Shoulder</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftShoulder {
			get => _LeftShoulder;
			set => SetProperty(ref _LeftShoulder, value);
		}
		private string _LeftShoulder;

		/// <summary>Left Thumb AxisX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbAxisX {
			get => _LeftThumbAxisX;
			set => SetProperty(ref _LeftThumbAxisX, value);
		}
		private string _LeftThumbAxisX;

		/// <summary>Left Thumb AxisY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbAxisY {
			get => _LeftThumbAxisY;
			set => SetProperty(ref _LeftThumbAxisY, value);
		}
		private string _LeftThumbAxisY;

		/// <summary>Left Thumb Button</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbButton {
			get => _LeftThumbButton;
			set => SetProperty(ref _LeftThumbButton, value);
		}
		private string _LeftThumbButton;

		/// <summary>Left Thumb Down</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbDown {
			get => _LeftThumbDown;
			set => SetProperty(ref _LeftThumbDown, value);
		}
		private string _LeftThumbDown;

		/// <summary>Left Thumb Left</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbLeft {
			get => _LeftThumbLeft;
			set => SetProperty(ref _LeftThumbLeft, value);
		}
		private string _LeftThumbLeft;

		/// <summary>Left Thumb Right</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbRight {
			get => _LeftThumbRight;
			set => SetProperty(ref _LeftThumbRight, value);
		}
		private string _LeftThumbRight;

		/// <summary>Left Thumb Up</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftThumbUp {
			get => _LeftThumbUp;
			set => SetProperty(ref _LeftThumbUp, value);
		}
		private string _LeftThumbUp;

		/// <summary>Left Trigger</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string LeftTrigger {
			get => _LeftTrigger;
			set => SetProperty(ref _LeftTrigger, value);
		}
		private string _LeftTrigger;

		/// <summary>Right Shoulder</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightShoulder {
			get => _RightShoulder;
			set => SetProperty(ref _RightShoulder, value);
		}
		private string _RightShoulder;

		/// <summary>Right Thumb AxisX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbAxisX {
			get => _RightThumbAxisX;
			set => SetProperty(ref _RightThumbAxisX, value);
		}
		private string _RightThumbAxisX;

		/// <summary>Right Thumb AxisY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbAxisY {
			get => _RightThumbAxisY;
			set => SetProperty(ref _RightThumbAxisY, value);
		}
		private string _RightThumbAxisY;

		/// <summary>Right Thumb Button</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbButton {
			get => _RightThumbButton;
			set => SetProperty(ref _RightThumbButton, value);
		}
		private string _RightThumbButton;

		/// <summary>Right Thumb Down</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbDown {
			get => _RightThumbDown;
			set => SetProperty(ref _RightThumbDown, value);
		}
		private string _RightThumbDown;

		/// <summary>Right Thumb Left</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbLeft {
			get => _RightThumbLeft;
			set => SetProperty(ref _RightThumbLeft, value);
		}
		private string _RightThumbLeft;

		/// <summary>Right Thumb Right</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbRight {
			get => _RightThumbRight;
			set => SetProperty(ref _RightThumbRight, value);
		}
		private string _RightThumbRight;

		/// <summary>Right Thumb Up</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightThumbUp {
			get => _RightThumbUp;
			set => SetProperty(ref _RightThumbUp, value);
		}
		private string _RightThumbUp;

		/// <summary>Right Trigger</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(32)]
		public string RightTrigger {
			get => _RightTrigger;
			set => SetProperty(ref _RightTrigger, value);
		}
		private string _RightTrigger;

	}
}
