/*
 * diStorm3 JNI wrapper.
 * Gil Dabah, October 2010.
 */

#include "jdistorm.h"
#include "../../include/distorm.h"

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#pragma comment(lib, "../../distorm.lib")

static struct _CodeInfoIds {
	jclass jCls;
	jfieldID ID_CodeOffset;
	jfieldID ID_Code;
	jfieldID ID_DecodeType;
	jfieldID ID_Features;
} g_CodeInfoIds;

static struct _DecodedResultIds {
	jclass jCls;
	jfieldID ID_Instructions;
	jfieldID ID_MaxInstructions;
} g_DecodedResultIds;

static struct _DecodedInstIds {
	jclass jCls;
	jfieldID ID_Mnemonic;
	jfieldID ID_Operands;
	jfieldID ID_Hex;
	jfieldID ID_Size;
	jfieldID ID_Offset;
} g_DecodedInstIds;

static struct _DecomposedResultIds {
	jclass jCls;
	jfieldID ID_Instructions;
	jfieldID ID_MaxInstructions;
} g_DecomposedResultIds;

static struct _DecomposedInstIds {
	jclass jCls;
	jfieldID ID_Address;
	jfieldID ID_Size;
	jfieldID ID_Flags;
	jfieldID ID_Segment;
	jfieldID ID_Base;
	jfieldID ID_Scale;
	jfieldID ID_Opcode;
	jfieldID ID_Operands;
	jfieldID ID_Disp;
	jfieldID ID_Imm;
	jfieldID ID_UnusedPrefixesMask;
	jfieldID ID_Meta;
	jfieldID ID_RegistersMask;
	jfieldID ID_ModifiedFlagsMask;
	jfieldID ID_TestedFlagsMask;
	jfieldID ID_UndefinedFlagsMask;
} g_DecomposedInstIds;

static struct _OperandIds {
	jclass jCls;
	jfieldID ID_Type;
	jfieldID ID_Index;
	jfieldID ID_Size;
} g_OperandIds;

static struct _ImmIds {
	jclass jCls;
	jfieldID ID_Value;
	jfieldID ID_Size;
} g_ImmIds;

static struct _DispIds {
	jclass jCls;
	jfieldID ID_Displacement;
	jfieldID ID_Size;
} g_DispIds;

void JThrowByName(JNIEnv* env, const char *name, const char* msg)
{
    jclass cls = (*env)->FindClass(env, name);
    if (cls != NULL) {
        (*env)->ThrowNew(env, cls, msg);
    }
    (*env)->DeleteLocalRef(env, cls);
}

_CodeInfo* AcquireCodeInfoStruct(JNIEnv *env, jobject jciObj)
{
	jobject jCodeObj = NULL;
	_CodeInfo* ci = (_CodeInfo*)malloc(sizeof(_CodeInfo));
	if (ci == NULL) {
		JThrowByName(env, "java/lang/OutOfMemoryError", NULL);
		return NULL;
	}
	memset(ci, 0, sizeof(_CodeInfo));

	ci->codeOffset = (*env)->GetLongField(env, jciObj, g_CodeInfoIds.ID_CodeOffset);

	jCodeObj = (*env)->GetObjectField(env, jciObj, g_CodeInfoIds.ID_Code);
	ci->code = (uint8_t*) (*env)->GetDirectBufferAddress(env, jCodeObj);
	ci->codeLen = (int)(*env)->GetDirectBufferCapacity(env, jCodeObj);

	ci->dt = (*env)->GetIntField(env, jciObj, g_CodeInfoIds.ID_DecodeType);

	ci->features = (*env)->GetIntField(env, jciObj, g_CodeInfoIds.ID_Features);

	return ci;
}

jobject CreateDecodedInstObj(JNIEnv* env, const _DecodedInst* inst)
{
	jobject jInst = (*env)->AllocObject(env, g_DecodedInstIds.jCls);
	if (jInst == NULL) return NULL;
	(*env)->SetObjectField(env, jInst, g_DecodedInstIds.ID_Mnemonic, (*env)->NewStringUTF(env, (const char*)inst->mnemonic.p));
	(*env)->SetObjectField(env, jInst, g_DecodedInstIds.ID_Operands, (*env)->NewStringUTF(env, (const char*)inst->operands.p));
	(*env)->SetObjectField(env, jInst, g_DecodedInstIds.ID_Hex, (*env)->NewStringUTF(env, (const char*)inst->instructionHex.p));
	(*env)->SetIntField(env, jInst, g_DecodedInstIds.ID_Size, inst->size);
	(*env)->SetLongField(env, jInst, g_DecodedInstIds.ID_Offset, inst->offset);
	return jInst;
}

JNIEXPORT void JNICALL Java_diStorm3_distorm3_Decode
  (JNIEnv *env, jobject thiz, jobject jciObj, jobject jdrObj)
{
	jarray jInsts = NULL;
	jobject jInst = NULL;
	_CodeInfo* ci = NULL;
	_DecodedInst* insts = NULL;
	jint maxInstructions = 0;
	unsigned int usedInstructionsCount = 0, i = 0;

	thiz; /* Unused. */

	ci = AcquireCodeInfoStruct(env, jciObj);
	if (ci == NULL) {
		JThrowByName(env, "java/lang/OutOfMemoryError", NULL);
		return;
	}

	maxInstructions = (*env)->GetIntField(env, jdrObj, g_DecodedResultIds.ID_MaxInstructions);

	insts = (_DecodedInst*)malloc(maxInstructions * sizeof(_DecodedInst));
	if (insts == NULL) goto Cleanup;

	distorm_decode(ci->codeOffset, ci->code, ci->codeLen, ci->dt, insts, maxInstructions, &usedInstructionsCount);

	jInsts = (*env)->NewObjectArray(env, usedInstructionsCount, g_DecodedInstIds.jCls, NULL);
	if (jInsts == NULL) goto Cleanup;

	for (i = 0; i < usedInstructionsCount; i++) {
		jInst = CreateDecodedInstObj(env, &insts[i]);
		if (jInst == NULL) goto Cleanup;
		(*env)->SetObjectArrayElement(env, jInsts, i, jInst);
	}

	(*env)->SetObjectField(env, jdrObj, g_DecodedResultIds.ID_Instructions, jInsts);

Cleanup:
	/* In case of an error, jInsts will get cleaned automatically. */
	if (ci != NULL) free(ci);
	if (insts != NULL) free(insts);
}

JNIEXPORT void JNICALL Java_diStorm3_distorm3_Decompose
  (JNIEnv *env, jobject thiz, jobject jciObj, jobject jdrObj)
{
	jarray jInsts = NULL, jOperands = NULL;
	jobject jInst = NULL, jOperand = NULL, jImm = NULL, jDisp = NULL;
	_CodeInfo* ci = NULL;
	_DInst* insts = NULL;
	jint maxInstructions = 0;
	unsigned int usedInstructionsCount = 0, i = 0, j = 0, operandsNo = 0;
	int success = 0;

	thiz; /* Unused. */

	ci = AcquireCodeInfoStruct(env, jciObj);
	if (ci == NULL) {
		JThrowByName(env, "java/lang/OutOfMemoryError", NULL);
		return;
	}

	maxInstructions = (*env)->GetIntField(env, jdrObj, g_DecomposedResultIds.ID_MaxInstructions);

	insts = (_DInst*)malloc(maxInstructions * sizeof(_DInst));
	if (insts == NULL) goto Cleanup;

	distorm_decompose(ci, insts, maxInstructions, &usedInstructionsCount);

	jInsts = (*env)->NewObjectArray(env, usedInstructionsCount, g_DecomposedInstIds.jCls, NULL);
	if (jInsts == NULL) goto Cleanup;

	for (i = 0; i < usedInstructionsCount; i++) {
		jInst = (*env)->AllocObject(env, g_DecomposedInstIds.jCls);
		if (jInst == NULL) goto Cleanup;

		/* Simple fields: */
		(*env)->SetLongField(env, jInst, g_DecomposedInstIds.ID_Address, insts[i].addr);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Flags, insts[i].flags);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Size, insts[i].size);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Segment, insts[i].segment);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Base, insts[i].base);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Scale, insts[i].scale);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Opcode, insts[i].opcode);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_UnusedPrefixesMask, insts[i].unusedPrefixesMask);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_Meta, insts[i].meta);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_RegistersMask, insts[i].usedRegistersMask);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_ModifiedFlagsMask, insts[i].modifiedFlagsMask);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_TestedFlagsMask, insts[i].testedFlagsMask);
		(*env)->SetIntField(env, jInst, g_DecomposedInstIds.ID_UndefinedFlagsMask, insts[i].undefinedFlagsMask);

		/* Immediate variant. */
		jImm = (*env)->AllocObject(env, g_ImmIds.jCls);
		if (jImm == NULL) goto Cleanup;
		(*env)->SetLongField(env, jImm, g_ImmIds.ID_Value, insts[i].imm.qword);
		/* The size of the immediate is in one of the operands, if at all. Look for it below. Zero by default. */
		(*env)->SetIntField(env, jImm, g_ImmIds.ID_Size, 0);

		/* Count operands. */
		for (operandsNo = 0; operandsNo < OPERANDS_NO; operandsNo++) {
			if (insts[i].ops[operandsNo].type == O_NONE) break;
		}

		jOperands = (*env)->NewObjectArray(env, operandsNo, g_OperandIds.jCls, NULL);
		if (jOperands == NULL) goto Cleanup;

		for (j = 0; j < operandsNo; j++) {
			if (insts[i].ops[j].type == O_IMM) {
				/* Set the size of the immediate operand. */
				(*env)->SetIntField(env, jImm, g_ImmIds.ID_Size, insts[i].ops[j].size);
			}

			jOperand = (*env)->AllocObject(env, g_OperandIds.jCls);
			if (jOperand == NULL) goto Cleanup;
			(*env)->SetIntField(env, jOperand, g_OperandIds.ID_Type, insts[i].ops[j].type);
			(*env)->SetIntField(env, jOperand, g_OperandIds.ID_Index, insts[i].ops[j].index);
			(*env)->SetIntField(env, jOperand, g_OperandIds.ID_Size, insts[i].ops[j].size);
			(*env)->SetObjectArrayElement(env, jOperands, j, jOperand);
		}
		(*env)->SetObjectField(env, jInst, g_DecomposedInstIds.ID_Operands, jOperands);

		/* Attach the immediate variant. */
		(*env)->SetObjectField(env, jInst, g_DecomposedInstIds.ID_Imm, jImm);

		/* Displacement variant. */
		jDisp = (*env)->AllocObject(env, g_DispIds.jCls);
		if (jDisp == NULL) goto Cleanup;
		(*env)->SetLongField(env, jDisp, g_DispIds.ID_Displacement, insts[i].disp);
		(*env)->SetIntField(env, jDisp, g_DispIds.ID_Size, insts[i].dispSize);
		(*env)->SetObjectField(env, jInst, g_DecomposedInstIds.ID_Disp, jDisp);

		(*env)->SetObjectArrayElement(env, jInsts, i, jInst);
	}

	(*env)->SetObjectField(env, jdrObj, g_DecodedResultIds.ID_Instructions, jInsts);

Cleanup:
	/* In case of an error, jInsts will get cleaned automatically. */
	if (ci != NULL) free(ci);
	if (insts != NULL) free(insts);
}

JNIEXPORT jobject JNICALL Java_diStorm3_distorm3_Format
  (JNIEnv *env, jobject thiz, jobject jciObj, jobject jdiObj)
{
	_CodeInfo* ci = NULL;
	_DInst input = {0};
	_DecodedInst output = {0};
	jobject ret = NULL, jOperands = NULL, jOp = NULL, jTmp = NULL;
	jsize i, opsCount;

	thiz; /* Unused. */

	ci = AcquireCodeInfoStruct(env, jciObj);
	if (ci == NULL) {
		JThrowByName(env, "java/lang/OutOfMemoryError", NULL);
		return NULL;
	}

	input.addr = (*env)->GetLongField(env, jdiObj, g_DecomposedInstIds.ID_Address);
	input.flags = (uint16_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Flags);
	input.size = (uint8_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Size);
	input.segment = (uint8_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Segment);
	input.base = (uint8_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Base);
	input.scale = (uint8_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Scale);
	input.opcode = (uint16_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Opcode);
	/* unusedPrefixesMask is unused indeed, lol. */
	input.meta = (uint8_t) (*env)->GetIntField(env, jdiObj, g_DecomposedInstIds.ID_Meta);
	/* Nor usedRegistersMask. */

	jOperands = (*env)->GetObjectField(env, jdiObj, g_DecomposedInstIds.ID_Operands);
	if (jOperands != NULL) {
		opsCount = (*env)->GetArrayLength(env, jOperands);
		for (i = 0; i < opsCount; i++) {
			jOp = (*env)->GetObjectArrayElement(env, jOperands, i);
			if (jOp != NULL) {
				input.ops[i].index = (uint8_t) (*env)->GetIntField(env, jOp, g_OperandIds.ID_Index);
				input.ops[i].type = (uint8_t) (*env)->GetIntField(env, jOp, g_OperandIds.ID_Type);
				input.ops[i].size = (uint16_t) (*env)->GetIntField(env, jOp, g_OperandIds.ID_Size);
			}
		}
	}

	jTmp = (*env)->GetObjectField(env, jdiObj, g_DecomposedInstIds.ID_Imm);
	if (jTmp != NULL) {
		input.imm.qword = (uint64_t) (*env)->GetLongField(env, jTmp, g_ImmIds.ID_Value);
	}

	jTmp = (*env)->GetObjectField(env, jdiObj, g_DecomposedInstIds.ID_Disp);
	if (jTmp != NULL) {
		input.disp = (uint64_t) (*env)->GetLongField(env, jTmp, g_DispIds.ID_Displacement);
		input.dispSize = (uint8_t) (*env)->GetIntField(env, jTmp, g_DispIds.ID_Size);
	}

	distorm_format(ci, &input, &output);

	ret = CreateDecodedInstObj(env, &output);

	if (ci != NULL) free(ci);
	return ret;
}

/* Cache all ID's and classes! Release in unload. */
jint JNI_OnLoad(JavaVM *vm, void *reserved)
{
	jclass jCls = NULL;
	JNIEnv* env = NULL;

	if ((*vm)->GetEnv(vm, (void**)&env, JNI_VERSION_1_6) != JNI_OK) {
		return JNI_VERSION_1_6;
	}

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "CodeInfo");
	g_CodeInfoIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_CodeInfoIds.ID_CodeOffset = (*env)->GetFieldID(env, jCls, "mCodeOffset", "J");
	g_CodeInfoIds.ID_Code = (*env)->GetFieldID(env, jCls, "mCode", "Ljava/nio/ByteBuffer;");
	g_CodeInfoIds.ID_DecodeType = (*env)->GetFieldID(env, jCls, "mDecodeType", "I");
	g_CodeInfoIds.ID_Features = (*env)->GetFieldID(env, jCls, "mFeatures", "I");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "DecodedResult");
	g_DecodedResultIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_DecodedResultIds.ID_MaxInstructions = (*env)->GetFieldID(env, jCls, "mMaxInstructions", "I");
	g_DecodedResultIds.ID_Instructions = (*env)->GetFieldID(env, jCls, "mInstructions", "[L" PACKAGE_PREFIX "DecodedInst;");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "DecodedInst");
	g_DecodedInstIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_DecodedInstIds.ID_Mnemonic = (*env)->GetFieldID(env, jCls, "mMnemonic", "Ljava/lang/String;");
	g_DecodedInstIds.ID_Operands = (*env)->GetFieldID(env, jCls, "mOperands", "Ljava/lang/String;");
	g_DecodedInstIds.ID_Hex = (*env)->GetFieldID(env, jCls, "mHex", "Ljava/lang/String;");
	g_DecodedInstIds.ID_Size = (*env)->GetFieldID(env, jCls, "mSize", "I");
	g_DecodedInstIds.ID_Offset = (*env)->GetFieldID(env, jCls, "mOffset", "J");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "DecomposedResult");
	g_DecomposedResultIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_DecomposedResultIds.ID_Instructions = (*env)->GetFieldID(env, jCls, "mInstructions", "[L" PACKAGE_PREFIX "DecomposedInst;");
	g_DecomposedResultIds.ID_MaxInstructions = (*env)->GetFieldID(env, jCls, "mMaxInstructions", "I");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "DecomposedInst");
	g_DecomposedInstIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_DecomposedInstIds.ID_Address = (*env)->GetFieldID(env, jCls, "mAddr", "J");
	g_DecomposedInstIds.ID_Size = (*env)->GetFieldID(env, jCls, "mSize", "I");
	g_DecomposedInstIds.ID_Flags = (*env)->GetFieldID(env, jCls, "mFlags", "I");
	g_DecomposedInstIds.ID_Segment = (*env)->GetFieldID(env, jCls, "mSegment", "I");
	g_DecomposedInstIds.ID_Base = (*env)->GetFieldID(env, jCls, "mBase", "I");
	g_DecomposedInstIds.ID_Scale = (*env)->GetFieldID(env, jCls, "mScale", "I");
	g_DecomposedInstIds.ID_Opcode = (*env)->GetFieldID(env, jCls, "mOpcode", "I");
	g_DecomposedInstIds.ID_Operands = (*env)->GetFieldID(env, jCls, "mOperands", "[L" PACKAGE_PREFIX "Operand;");
	g_DecomposedInstIds.ID_Disp = (*env)->GetFieldID(env, jCls, "mDisp", "L" PACKAGE_PREFIX "DecomposedInst$DispVariant;");
	g_DecomposedInstIds.ID_Imm = (*env)->GetFieldID(env, jCls, "mImm", "L" PACKAGE_PREFIX "DecomposedInst$ImmVariant;");
	g_DecomposedInstIds.ID_UnusedPrefixesMask = (*env)->GetFieldID(env, jCls, "mUnusedPrefixesMask", "I");
	g_DecomposedInstIds.ID_Meta = (*env)->GetFieldID(env, jCls, "mMeta", "I");
	g_DecomposedInstIds.ID_RegistersMask = (*env)->GetFieldID(env, jCls, "mRegistersMask", "I");
	g_DecomposedInstIds.ID_ModifiedFlagsMask = (*env)->GetFieldID(env, jCls, "mModifiedFlagsMask", "I");
	g_DecomposedInstIds.ID_TestedFlagsMask = (*env)->GetFieldID(env, jCls, "mTestedFlagsMask", "I");
	g_DecomposedInstIds.ID_UndefinedFlagsMask = (*env)->GetFieldID(env, jCls, "mUndefinedFlagsMask", "I");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "Operand");
	g_OperandIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_OperandIds.ID_Type = (*env)->GetFieldID(env, jCls, "mType", "I");
	g_OperandIds.ID_Index = (*env)->GetFieldID(env, jCls, "mIndex", "I");
	g_OperandIds.ID_Size = (*env)->GetFieldID(env, jCls, "mSize", "I");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "DecomposedInst$ImmVariant");
	g_ImmIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_ImmIds.ID_Value = (*env)->GetFieldID(env, jCls, "mValue", "J");
	g_ImmIds.ID_Size = (*env)->GetFieldID(env, jCls, "mSize", "I");

	jCls = (*env)->FindClass(env, PACKAGE_PREFIX "DecomposedInst$DispVariant");
	g_DispIds.jCls = (*env)->NewWeakGlobalRef(env, jCls);
	g_DispIds.ID_Displacement = (*env)->GetFieldID(env, jCls, "mDisplacement", "J");
	g_DispIds.ID_Size = (*env)->GetFieldID(env, jCls, "mSize", "I");

	return JNI_VERSION_1_6;
}


JNIEXPORT void JNICALL JNI_OnUnload(JavaVM *vm, void *reserved)
{
	/* Free global weak refs. */
}