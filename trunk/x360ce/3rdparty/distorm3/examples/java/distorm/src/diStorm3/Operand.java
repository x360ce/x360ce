package diStorm3;

public class Operand {

	public enum OperandType {
		None, Reg, Imm, Imm1, Imm2, Disp, Smem, Mem, Pc, Ptr
	}

	private int mType;
	private int mIndex;
	private int mSize;

	public OperandType getType() {
		return OperandType.values()[mType];
	}

	public int getIndex() {
		return mIndex;
	}

	public int getSize() {
		return mSize;
	}
}