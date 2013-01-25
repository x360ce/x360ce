package diStorm3;

import diStorm3.Operand;
import diStorm3.Opcodes;

public class DecomposedInst {
	private class ImmVariant {
		private long mValue;
		private int mSize;

		public long getImm() {
			return mValue;
		}

		public int getSize() {
			return mSize;
		}
	}

	private class DispVariant {

		private long mDisplacement;
		private int mSize;

		public long getDisplacement() {
			return mDisplacement;
		}

		public int getSize() {
			return mSize;
		}
	}

	private long mAddr;
	private int mSize;
	private int mFlags;
	private int mSegment;
	private int mBase, mScale;
	private int mOpcode;
	public Operand[] mOperands;
	public DispVariant mDisp;
	public ImmVariant mImm;
	private int mUnusedPrefixesMask;
	private int mMeta;
	private int mRegistersMask;
	private int mModifiedFlagsMask;
	private int mTestedFlagsMask;
	private int mUndefinedFlagsMask;

	public long getAddress() {
		return mAddr;
	}
	public int getSize() {
		return mSize;
	}
	public OpcodeEnum getOpcode() {
		return Opcodes.lookup(mOpcode);
	}
	public int getSegment() {
		return mSegment & 0x7f;
	}
	public boolean isSegmentDefault() {
		return (mSegment & 0x80) == 0x80;
	}
	public int getBase() {
		return mBase;
	}
	public int getScale() {
		return mScale;
	}
	public int getUnusedPrefixesMask() {
		return mUnusedPrefixesMask;
	}
	public int getMeta() {
		return mMeta;
	}
	public int getRegistersMask() {
		return mRegistersMask;
	}
	public int getModifiedFlagsMask() {
		return mModifiedFlagsMask;
	}
	public int getTestedFlagsMask() {
		return mTestedFlagsMask;
	}
	public int getUndefinedFlagsMask() {
		return mUndefinedFlagsMask;
	}
}