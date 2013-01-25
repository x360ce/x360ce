/*
 *  diStorm3 JNI
 *  Gil Dabah, Sep 2010
 *
 */
package diStorm3;
import diStorm3.CodeInfo;
import diStorm3.DecodedResult;
import diStorm3.DecomposedResult;
import diStorm3.Opcodes;

public class distorm3 {

	public enum DecodeType {
		Decode16Bits, Decode32Bits, Decode64Bits
	}

	public static native void Decompose(CodeInfo ci, DecomposedResult dr);
	public static native void Decode(CodeInfo ci, DecodedResult dr);
	public static native DecodedInst Format(CodeInfo ci, DecomposedInst di);

	public enum Registers {
		RAX, RCX, RDX, RBX, RSP, RBP, RSI, RDI, R8, R9, R10, R11, R12, R13, R14, R15,
		EAX, ECX, EDX, EBX, ESP, EBP, ESI, EDI, R8D, R9D, R10D, R11D, R12D, R13D, R14D, R15D,
		AX, CX, DX, BX, SP, BP, SI, DI, R8W, R9W, R10W, R11W, R12W, R13W, R14W, R15W,
		AL, CL, DL, BL, AH, CH, DH, BH, R8B, R9B, R10B, R11B, R12B, R13B, R14B, R15B,
		SPL, BPL, SIL, DIL,
		ES, CS, SS, DS, FS, GS,
		RIP,
		ST0, ST1, ST2, ST3, ST4, ST5, ST6, ST7,
		MM0, MM1, MM2, MM3, MM4, MM5, MM6, MM7,
		XMM0, XMM1, XMM2, XMM3, XMM4, XMM5, XMM6, XMM7, XMM8, XMM9, XMM10, XMM11, XMM12, XMM13, XMM14, XMM15,
		YMM0, YMM1, YMM2, YMM3, YMM4, YMM5, YMM6, YMM7, YMM8, YMM9, YMM10, YMM11, YMM12, YMM13, YMM14, YMM15,
		CR0, UNUSED0, CR2, CR3, CR4, UNUSED1, UNUSED2, UNUSED3, CR8,
		DR0, DR1, DR2, DR3, UNUSED4, UNUSED5, DR6, DR7
	};

	static {
		System.loadLibrary("jdistorm");
	}
}
