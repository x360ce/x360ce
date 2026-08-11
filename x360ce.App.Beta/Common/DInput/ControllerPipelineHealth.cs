using System;

namespace x360ce.App.DInput
{
	public sealed class ControllerPipelineHealth
	{
		public bool PhysicalInputOk { get; set; }
		public bool MappingOk { get; set; }
		public bool VirtualBusOk { get; set; }
		public bool VirtualTargetConnected { get; set; }
		public bool StateSubmitOk { get; set; }
		public string LastError { get; set; }
		public DateTime UpdatedUtc { get; set; }

		public bool IsHealthy =>
			PhysicalInputOk && MappingOk && VirtualBusOk &&
			VirtualTargetConnected && StateSubmitOk;

		public ControllerPipelineHealth Clone() => (ControllerPipelineHealth)MemberwiseClone();
	}
}
