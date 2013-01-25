package diStorm3;

import java.nio.ByteBuffer;

public class CodeInfo {
	public CodeInfo(long codeOffset, ByteBuffer code, distorm3.DecodeType dt, int features) {
		mCodeOffset = codeOffset;
		mCode = code;
		mDecodeType = dt.ordinal();
		mFeatures = features;
	}

	public CodeInfo(long codeOffset, byte[] rawCode, distorm3.DecodeType dt, int features) {
		mCode = ByteBuffer.allocateDirect(rawCode.length);
		mCode.put(rawCode);

		mCodeOffset = codeOffset;
		mDecodeType = dt.ordinal();
		mFeatures = features;
	}

	private long mCodeOffset;
	private long mNextOffset;
	private ByteBuffer mCode;
	private int mDecodeType;
	private int mFeatures;
}