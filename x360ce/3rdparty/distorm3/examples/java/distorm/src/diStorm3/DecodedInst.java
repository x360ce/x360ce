package diStorm3;

public class DecodedInst {
	DecodedInst()
	{
	}
	private String mMnemonic;
	private String mOperands;
	private String mHex;
	private int mSize;
	private long mOffset;

	public String getMnemonic() {
		return mMnemonic;
	}
	
	public String getOperands() {
		return mOperands;
	}
	
	public String getHex() {
		return mHex;
	}
	
	public int getSize() {
		return mSize;
	}
	
	public long getOffset() {
		return mOffset;
	}
}