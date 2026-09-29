using System;

namespace Nefarius.ViGEm.Client
{
	/// <summary>What an answer from the bus means for the controller it was about.</summary>
	public enum BusAnswer
	{
		/// <summary>The bus did what was asked.</summary>
		Fine,
		/// <summary>The controller or the bus went away. It is made again, and nobody needs telling.</summary>
		Gone,
		/// <summary>Any other refusal, codes <see cref="VIGEM_ERROR"/> does not name included.</summary>
		Sick,
	}

	/// <summary>Reads the bus's answers: what each one means, and how a person quotes it.</summary>
	public static class BusAnswers
	{
		/// <summary>An answer as a person can quote it: the code's name without its prefix, or its number.</summary>
		/// <remarks>
		/// The log and the Issues tab quote an answer the same way, so support sees one code for one
		/// answer. Newer bus libraries answer with codes the enum does not name, and the number is what
		/// can be looked up. VIGEM_ERROR_NONE reads "none given": a failure recorded without an answer.
		/// Called when an answer is written, never per report.
		/// </remarks>
		public static string Name(VIGEM_ERROR code)
		{
			if (code == VIGEM_ERROR.VIGEM_ERROR_NONE)
				return "none given";
			return Enum.IsDefined(typeof(VIGEM_ERROR), code)
				? code.ToString().Substring("VIGEM_ERROR_".Length)
				: "0x" + ((uint)code).ToString("X8");
		}

		/// <summary>What an answer from the bus means.</summary>
		/// <remarks>
		/// Every answer but a plain yes is a refusal. The bus library has more codes than the enum names,
		/// and a refusal read as success leaves a controller that does nothing, with nothing saying so.
		///
		/// A switch on a number: nothing is made and nothing waited for, so the engine can read an answer
		/// on every pass.
		/// </remarks>
		public static BusAnswer Of(VIGEM_ERROR error)
		{
			switch (error)
			{
				case VIGEM_ERROR.VIGEM_ERROR_NONE:
					return BusAnswer.Fine;
				case VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND:
				case VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET:
				case VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN:
					return BusAnswer.Gone;
				default:
					return BusAnswer.Sick;
			}
		}
	}
}
