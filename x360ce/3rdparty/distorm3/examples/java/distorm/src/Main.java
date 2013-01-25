import java.nio.ByteBuffer;

import diStorm3.distorm3.*;
import diStorm3.CodeInfo;
import diStorm3.DecodedInst;
import diStorm3.OpcodeEnum;
import diStorm3.distorm3;
import diStorm3.DecodedResult;
import diStorm3.DecomposedResult;
import diStorm3.DecomposedInst;

public class Main {

	public static void main(String[] args) {
		byte[] buf = new byte[4];
		buf[0] = (byte)0xc3;
		buf[1] = (byte)0x33;
		buf[2] = (byte)0xc0;
		buf[3] = (byte)0xc3;
		CodeInfo ci = new CodeInfo((long)0x1000, buf, DecodeType.Decode32Bits, 0);
		DecodedResult dr = new DecodedResult(10);
		distorm3.Decode(ci, dr);

		for (DecodedInst x : dr.mInstructions) {
			String s = String.format("%x %s %s", x.getOffset(), x.getMnemonic(), x.getOperands());
			System.out.println(s);
		}

		DecomposedResult dr2 = new DecomposedResult(10);
		distorm3.Decompose(ci, dr2);

		for (DecomposedInst y: dr2.mInstructions) {
			if (y.getOpcode() != OpcodeEnum.RET) {
				DecodedInst x = distorm3.Format(ci, y);
				String s = String.format("%x %s %s", x.getOffset(), x.getMnemonic(), x.getOperands());
				System.out.println(s);
			}
		}

	}


}
