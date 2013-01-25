package diStorm3;

public class DecomposedResult {
	public DecomposedResult(int maxInstructions) {
		mMaxInstructions = maxInstructions;
		mInstructions = null;
	}

	public DecomposedInst[] mInstructions;
	private int mMaxInstructions;
}