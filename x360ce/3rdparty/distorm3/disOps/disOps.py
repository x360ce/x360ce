#
# disOps.py v 1.0.0
#
# Copyright (C) 2003-2012 Gil Dabah, http://ragestorm.net/distorm/
#
# disOps is a part of the diStorm project, but can be used for anything.
# The generated output is tightly coupled with diStorm data structures which can be found at instructions.h.
# The code in diStorm that actually walks these structures is found at instructions.c.
#
# Since the DB was built purposely for diStorm, there are some
# Known issues:
#   1. ARPL/MOVSXD information in DB is stored as ARPL.
#      Since ARPL and MOVSXD share the same opcode this DB doesn't support this mix.
#      Therefore, if you use this DB for x64 instructions, you have to take care of this one.
#
#   2. SSE CMP pseudo instructions have the DEFAULT suffix letters of its type in the second mnemonic,
#      the third operand, Imm8 which is responsible for determining the suffix,
#      doesn't appear in the operands list but rather an InstFlag.PSEUDO_OPCODE implies this behavior.
#
#   3. The WAIT instruction is a bit problematic from a static DB point of view, read the comments in init_FPU in x86sets.py.
#
#   4. The OpLen.OL_33, [0x66, 0x0f, 0x78, 0x0], ["EXTRQ"] is very problematic as well.
#      Since there's another 8 group table after the 0x78 byte in this case, but it's already a Prefixed table.
#      Therefore, we will handle it as a normal 0x78 instruction with a mandatory prefix of 0x66.
#      But the REG (=0) field of the ModRM byte will be checked in the decoder by a flag that states so.
#      Otherwise, another normal table after Prefixed table really complicates matters,
#      and doesn't worth the hassle for one exceptional instruction.
#
#   5. The NOP (0x90) instruction is really set in the DB as xchg rAX, rAX. Rather than true NOP, this is because of x64 behavior.
#      Hence, it will be decided in runtime when decoding streams according to the mode.
#
#   6. The PAUSE (0xf3, 0x90) instruction isn't found in the DB, it will be returned directly by diStorm.
#      This is because the 0xf3 in this case is not a mandatory prefix, and we don't want it to be built as part of a prefixed table.
#
#   7. The IO String instructions don't have explicit form and they don't support segments.
#      It's up to diStorm to decide what to do with the operands and which segment is default and overrided.
#
#   8. Since opcodeId is an offset into the mnemonics table, the psuedo compare mnemonics needs a helper table to fix the offset.
#      Psuedo compare instructions work in such a way that only the first instruction is defined in the DB.
#      The rest are found using the third operand (that's why they are psuedo).
#
# To maximize the usage of this DB, one should learn the documentation of diStorm regarding the InstFlag and Operands Types.
#

import time

import x86sets
import x86db
from x86header import *

mnemonicsIds = {} # mnemonic : offset to mnemonics table of strings.
idsCounter = len("undefined") + 2 # Starts immediately after this one.

SSECmpTypes = ["EQ", "LT", "LE", "UNORD", "NEQ", "NLT", "NLE", "ORD"]
AVXCmpTypes = ["EQ", "LT", "LE", "UNORD", "NEQ", "NLT", "NLE", "ORD",
		"EQ_UQ", "NGE", "NGT", "FALSE", "NEQ_OQ", "GE", "GT", "TRUE",
		"EQ_OS", "LT_OQ", "LE_OQ", "UNORD_S", "NEQ_US", "NLT_UQ", "NLE_UQ", "ORD_S",
		"EQ_US", "NGE_UQ", "NGT_UQ", "FALSE_OS", "NEQ_OS", "GE_OQ", "GT_OQ", "TRUE_US"]

# Support SSE pseudo compare instructions. We will have to add them manually.
def FixPseudo(mnems):
	return [mnems[0] + i + mnems[1] for i in SSECmpTypes]

# Support AVX pseudo compare instructions. We will have to add them manually.
def FixPseudo2(mnems):
	return [mnems[0] + i + mnems[1] for i in AVXCmpTypes]

def TranslateMnemonics(pseudoClassType, mnems):
	global mnemonicsIds
	global idsCounter
	l = []
	if pseudoClassType == ISetClass.SSE or pseudoClassType == ISetClass.SSE2:
		mnems = FixPseudo(mnems)
	elif pseudoClassType == ISetClass.AVX:
		mnems = FixPseudo2(mnems)
	for i in mnems:
		if len(i) == 0:
			# Some mnemonics are empty on purpose because they're not used.
			# Set them to zero to keep the order of the list.
			l.append(0) # Undefined instruction.
			continue
		if mnemonicsIds.has_key(i):
			l.append(str(mnemonicsIds[i]))
		else:
			mnemonicsIds[i] = idsCounter
			l.append(str(idsCounter))
			idsCounter += len(i) + 2 # For len/null chars.
			if idsCounter >= 2**16:
				raise "opcodeId is too big to fit into uint16_t"
	return l

# All VIAL and diStorm3 code are based on the order of this list, do NOT edit!
REGISTERS = [
	"RAX", "RCX", "RDX", "RBX", "RSP", "RBP", "RSI", "RDI", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15", "XX",
	"EAX", "ECX", "EDX", "EBX", "ESP", "EBP", "ESI", "EDI", "R8D", "R9D", "R10D", "R11D", "R12D", "R13D", "R14D", "R15D", "XX",
	"AX", "CX", "DX", "BX", "SP", "BP", "SI", "DI", "R8W", "R9W", "R10W", "R11W", "R12W", "R13W", "R14W", "R15W", "XX",
	"AL", "CL", "DL", "BL", "AH", "CH", "DH", "BH", "R8B", "R9B", "R10B", "R11B", "R12B", "R13B", "R14B", "R15B", "XX",
	"SPL", "BPL", "SIL", "DIL", "XX",
	"ES", "CS", "SS", "DS", "FS", "GS", "XX",
	"RIP", "XX",
	"ST0", "ST1", "ST2", "ST3", "ST4", "ST5", "ST6", "ST7", "XX",
	"MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", "XX",
	"XMM0", "XMM1", "XMM2", "XMM3", "XMM4", "XMM5", "XMM6", "XMM7", "XMM8", "XMM9", "XMM10", "XMM11", "XMM12", "XMM13", "XMM14", "XMM15", "XX",
	"YMM0", "YMM1", "YMM2", "YMM3", "YMM4", "YMM5", "YMM6", "YMM7", "YMM8", "YMM9", "YMM10", "YMM11", "YMM12", "YMM13", "YMM14", "YMM15", "XX",
	"CR0", "", "CR2", "CR3", "CR4", "", "", "", "CR8", "XX",
	"DR0", "DR1", "DR2", "DR3", "", "", "DR6", "DR7"]

def CreatePythonDict(mnemonicsIds):
	""" Create the opcodes dictionary for Python. """
	s = "\n"
	for i in mnemonicsIds:
		s += "0x%x: \"%s\", " % (mnemonicsIds[i], i)
		if len(s) - s.rfind("\n") >= 76:
			s = s[:-1] + "\n"
	print s

def CreateJavaDict(mnemonicsIds):
	""" Create the opcodes dictionary/enum for Java. """
	s = "\nmOpcodes.put(0, OpcodeEnum.UNDEFINED);\n"
	for i in mnemonicsIds:
		s += "mOpcodes.put(0x%x, OpcodeEnum.%s);\n" % (mnemonicsIds[i], i.replace(" ", "_").replace(",", ""))
	print s

	s = "\nUNDEFINED, "
	for i in mnemonicsIds:
		s += "%s, " % (i.replace(" ", "_").replace(",", ""))
		if len(s) - s.rfind("\n") >= 76:
			s = s[:-1] + "\n"
	print s

def DumpMnemonics():
	global mnemonicsIds

	f = open("defs.txt", "w")

	f.write("typedef enum {\n\tI_UNDEFINED = 0, ")
	pos = 0
	l2 = sorted(mnemonicsIds.keys())
	for i in l2:
		s = "I_%s = %d" % (i.replace(" ", "_").replace(",", ""), mnemonicsIds[i])
		if i != l2[-1]:
			s += ","
		pos += len(s)
		if pos >= 70:
			s += "\n\t"
			pos = 0
		elif i != l2[-1]:
			s += " "
		f.write(s)
	f.write("\n} _InstructionType;\n\n")

	regsText = "const _WRegister _REGISTERS[] = {\n\t"
	regsEnum = "typedef enum {\n\t"
	old = "*"
	unused = 0
	for i in REGISTERS:
		if old != "*":
			if old == "XX":
				regsText += "\n\t"
				regsEnum += "\n\t"
				old = i
				continue
			else:
				regsText += "{%d, \"%s\"}," % (len(old), old)
				if len(old):
					regsEnum += "R_%s," % old
				else:
					regsEnum += "R_UNUSED%d," % unused
					unused += 1
				if i != "XX":
					regsText += " "
					regsEnum += " "
		old = i
	regsText += "{%d, \"%s\"}\n};\n" % (len(old), old)
	regsEnum += "R_" + old + "\n} _RegisterType;\n"

	f.write(regsEnum + "\n")

	# Mnemonics are sorted by insertion order. (Psuedo mnemonics depend on this!)
	s = "const unsigned char _MNEMONICS[] =\n\"\\x09\" \"UNDEFINED\\0\" "
	l = zip(mnemonicsIds.keys(), mnemonicsIds.values())
	l.sort(lambda x, y: x[1] - y[1])
	for i in l:
		s += "\"\\x%02x\" \"%s\\0\" " % (len(i[0]), i[0])
		if len(s) - s.rfind("\n") >= 76:
			s += "\\\n"
	s = s[:-1] + ";\n\n" # Ignore last space.
	f.write(s)

	f.write(regsText + "\n")
	f.close()

	# Used for Python/Java dictionary of opcodeIds-->mnemonics.
	#CreatePythonDict(mnemonicsIds)
	CreateJavaDict(mnemonicsIds)

O_NONE = 0
# REG standalone
O_REG = 1
# IMM standalone
O_IMM = 2
# IMM_1 standalone
O_IMM_1 = 4
# IMM_2 standalone
O_IMM_2 = 5
# DISP standlone
O_DISP = 3
# MEM uses DISP
O_MEM = 3
# PC uses IMM
O_PC = 2
# PTR uses IMM
O_PTR = 2

_OPT2T = {OperandType.NONE : O_NONE,
	OperandType.IMM8 : O_IMM,
	OperandType.IMM16 : O_IMM,
	OperandType.IMM_FULL : O_IMM,
	OperandType.IMM32 : O_IMM,
	OperandType.SEIMM8 : O_IMM,
	OperandType.IMM16_1 : O_IMM_1,
	OperandType.IMM8_1 : O_IMM_1,
	OperandType.IMM8_2 : O_IMM_2,
	OperandType.REG8 : O_REG,
	OperandType.REG16 : O_REG,
	OperandType.REG_FULL : O_REG,
	OperandType.REG32 : O_REG,
	OperandType.REG32_64 : O_REG,
	OperandType.FREG32_64_RM : O_REG,
	OperandType.RM8 : O_MEM,
	OperandType.RM16 : O_MEM,
	OperandType.RM_FULL : O_MEM,
	OperandType.RM32_64 : O_MEM,
	OperandType.RM16_32 : O_MEM,
	OperandType.FPUM16 : O_MEM,
	OperandType.FPUM32 : O_MEM,
	OperandType.FPUM64 : O_MEM,
	OperandType.FPUM80 : O_MEM,
	OperandType.R32_M8 : O_MEM,
	OperandType.R32_M16 : O_MEM,
	OperandType.R32_64_M8 : O_MEM,
	OperandType.R32_64_M16 : O_MEM,
	OperandType.RFULL_M16 : O_MEM,
	OperandType.CREG : O_REG,
	OperandType.DREG : O_REG,
	OperandType.SREG : O_REG,
	OperandType.SEG : O_REG,
	OperandType.ACC8 : O_REG,
	OperandType.ACC16 : O_REG,
	OperandType.ACC_FULL : O_REG,
	OperandType.ACC_FULL_NOT64 : O_REG,
	OperandType.MEM16_FULL : O_MEM,
	OperandType.PTR16_FULL : O_PTR,
	OperandType.MEM16_3264 : O_MEM,
	OperandType.RELCB : O_PC,
	OperandType.RELC_FULL : O_PC,
	OperandType.MEM : O_MEM,
	OperandType.MEM_OPT : O_MEM,
	OperandType.MEM32 : O_MEM,
	OperandType.MEM32_64 : O_MEM,
	OperandType.MEM64 : O_MEM,
	OperandType.MEM128 : O_MEM,
	OperandType.MEM64_128 : O_MEM,
	OperandType.MOFFS8 : O_MEM,
	OperandType.MOFFS_FULL : O_MEM,
	OperandType.CONST1 : O_IMM,
	OperandType.REGCL : O_REG,
	OperandType.IB_RB : O_REG,
	OperandType.IB_R_FULL : O_REG,
	OperandType.REGI_ESI : O_MEM,
	OperandType.REGI_EDI : O_MEM,
	OperandType.REGI_EBXAL : O_MEM,
	OperandType.REGI_EAX : O_MEM,
	OperandType.REGDX : O_REG,
	OperandType.REGECX : O_REG,
	OperandType.FPU_SI : O_REG,
	OperandType.FPU_SSI : O_REG,
	OperandType.FPU_SIS : O_REG,
	OperandType.MM : O_REG,
	OperandType.MM_RM : O_REG,
	OperandType.MM32 : O_MEM,
	OperandType.MM64 : O_MEM,
	OperandType.XMM : O_REG,
	OperandType.XMM_RM : O_REG,
	OperandType.XMM16 : O_MEM,
	OperandType.XMM32 : O_MEM,
	OperandType.XMM64 : O_MEM,
	OperandType.XMM128 : O_MEM,
	OperandType.REGXMM0 : O_REG,
	OperandType.RM32 : O_MEM,
	OperandType.REG32_64_M8 : O_MEM,
	OperandType.REG32_64_M16 : O_MEM,
	OperandType.WREG32_64 : O_REG,
	OperandType.WRM32_64 : O_REG,
	OperandType.WXMM32_64 : O_MEM,
	OperandType.VXMM : O_REG,
	OperandType.XMM_IMM : O_IMM,
	OperandType.YXMM : O_REG,
	OperandType.YXMM_IMM : O_REG,
	OperandType.YMM : O_REG,
	OperandType.YMM256 : O_MEM,
	OperandType.VYMM : O_REG,
	OperandType.VYXMM : O_REG,
	OperandType.YXMM64_256 : O_MEM,
	OperandType.YXMM128_256 : O_MEM,
	OperandType.LXMM64_128 : O_MEM,
	OperandType.LMEM128_256 : O_MEM
	}

def CheckOTCollisions(ii):
	""" Checks whether an instruction has two or more operands that use the same fields in the diStorm3 structure.
	E.G: ENTER 0x10, 0x1 --> This instruction uses two OT_IMM, which will cause a collision and use the same field twice which is bougs. """
	types = map(lambda x: _OPT2T[x], ii.operands)
	# Regs cannot cause a collision, since each register is stored inside the operand itself.
	for i in types:
		if i != O_REG and types.count(i) > 1:
			print "**WARNING: Operand type collision for instruction: " + ii.mnemonics[0], ii.tag
			break

# This fucntion for certain flow control related instructions will set their type.
def CheckForFlowControl(ii):
	if ii.mnemonics[0].find("CMOV") == 0:
		ii.flowControl = FlowControl.CMOV
		return

	# Should I include SYSCALL ?
	pairs = [
		(["INT", "INT1", "INT 3", "INTO", "UD2"], FlowControl.INT),
		(["CALL", "CALL FAR"], FlowControl.CALL),
		(["RET", "IRET", "RETF"], FlowControl.RET),
		(["SYSCALL", "SYSENTER", "SYSRET", "SYSEXIT"], FlowControl.SYS),
		(["JMP", "JMP FAR"], FlowControl.UNC_BRANCH),
		(["JCXZ", "JO", "JNO", "JB", "JAE", "JZ", "JNZ", "JBE", "JA", "JS", "JNS", "JP", "JNP", "JL", "JGE", "JLE", "JG", "LOOP", "LOOPZ", "LOOPNZ"], FlowControl.CND_BRANCH)
	]
	ii.flowControl = 0
	for p in pairs:
		if ii.mnemonics[0] in p[0]:
			ii.flowControl = p[1]
			return

def CheckWritableDestinationOperand(ii):
	prefixes = ["MOV", "SET", "CMOV", "CMPXCHG"]
	for i in prefixes:
		if ii.mnemonics[0].find(i) == 0:
			ii.flags |= InstFlag.DST_WR
			return

	mnemonics = [
		"ADD", "OR", "ADC", "SBB", "AND", "SUB", "XOR", "INC", "DEC", "LEA", "XCHG",
		"ROL", "ROR", "RCL", "RCR", "SHL", "SHR", "SAL", "SAR", "SHLD", "SHRD",
		"NEG", "NOT", "MUL", "IMUL", "DIV", "IDIV",
		"POP", "BTR", "BTS", "BTC", "XADD", "BSWAP",
		"LZCNT", "MOVBE", "POPCNT", "CRC32", "SMSW"
	]
	for i in mnemonics:
		if ii.mnemonics[0] in i:
			ii.flags |= InstFlag.DST_WR
			return

def SetInstructionAffectedFlags(ii, flagsTuple):
	""" Helper routine to set the m/t/u flags for an instruction info. """
	# Pad tuple for fast access.
	if not isinstance(flagsTuple, type(())):
		flagsTuple = (flagsTuple,)
	flagsTuple += (0,) * (3 - len(flagsTuple))
	ii.modifiedFlags = flagsTuple[0]
	ii.testedFlags = flagsTuple[1]
	ii.undefinedFlags = flagsTuple[2]

def GetTestedFlagsForCondition(cond):
	OF, SF, ZF, AF, PF, CF, IF, DF = CPUFlags.OF, CPUFlags.SF, CPUFlags.ZF, CPUFlags.AF, CPUFlags.PF, CPUFlags.CF, CPUFlags.IF, CPUFlags.DF
	Conditions = {
		"O": OF,
		"NO": OF,
		"B": CF,
		"AE": CF,
		"Z": ZF,
		"NZ": ZF,
		"BE": CF | ZF,
		"A": CF | ZF,
		"S": SF,
		"NS": SF,
		"P": PF,
		"NP": PF,
		"L": SF | OF,
		"GE": SF | OF,
		"LE": SF | OF | ZF,
		"G": SF | OF | ZF,
		# Special for FCMOV
		"U": PF,
		"NU": PF,
		"E": ZF,
		"NE": ZF,
		"NB": CF,
		"NBE": CF | ZF
	}
	# Return tested flags only.
	return (0, Conditions[cond], 0)

def CheckInstructionAffectedFlags(ii):
	"""
	Add flags for each instruction that is in the following table. We add modified/tested/undefined flags.
	Note that some instruction reset specific flags, but we don't record that here, we only care about actually modified ones.
	"""
	# MNEM: MODIFIED, TEST, UNDEFINED.
	OF, SF, ZF, AF, PF, CF, IF, DF = CPUFlags.OF, CPUFlags.SF, CPUFlags.ZF, CPUFlags.AF, CPUFlags.PF, CPUFlags.CF, CPUFlags.IF, CPUFlags.DF
	InstByMnem = {
		"AAA": (AF | CF, AF, OF | SF | ZF | PF),
		"AAS": (AF | CF, AF, OF | SF | ZF | PF),
		"AAD": (SF | ZF | PF, 0, OF | AF | CF),
		"AAM": (SF | ZF | PF, 0, OF | AF | CF),
		"ADC": (OF | SF | ZF | AF | PF | CF, CF),
		"ADD": (OF | SF | ZF | AF | PF | CF),
		"AND": (OF | SF | ZF | PF | CF, 0, AF),
		"ARPL": (ZF),
		"BSF": (ZF, 0, OF | SF | ZF | AF | PF | CF),
		"BSR": (ZF, 0, OF | SF | ZF | AF | PF | CF),
		"BT": (CF, 0, OF | SF | ZF | AF | PF),
		"BTS": (CF, 0, OF | SF | ZF | AF | PF),
		"BTR": (CF, 0, OF | SF | ZF | AF | PF),
		"BTC": (CF, 0, OF | SF | ZF | AF | PF),
		"CLC": (CF),
		"CLD": (DF),
		"CLI": (IF),
		"CMC": (CF),
		"CMP": (OF | SF | ZF | AF | PF | CF),
		"CMPXCHG": (OF | SF | ZF | AF | PF | CF),
		"CMPXCHG8B": (ZF),
		"CMPXCHG16B": (ZF), # Same inst as previous.
		"COMSID": (ZF | PF | CF),
		"COMISS": (ZF | PF | CF),
		"DAA": (SF | ZF | AF | PF | CF, AF | CF, OF),
		"DAS": (SF | ZF | AF | PF | CF, AF | CF, OF),
		"DEC": (OF | SF | ZF | AF | PF),
		"DIV": (0, 0, OF | SF | ZF | AF | PF | CF),
		"FCOMI":  (ZF | PF | CF),
		"FCOMIP": (ZF | PF | CF),
		"FUCOMI": (ZF | PF | CF),
		"FUCOMIP": (ZF | PF | CF),
		"IDIV": (0, 0, OF | SF | ZF | AF | PF | CF),
		"IMUL": (OF | CF, 0, SF | ZF | AF | PF),
		"INC": (OF | SF | ZF | AF | PF),
		"UCOMSID": (ZF | PF | CF),
		"UCOMISS": (ZF | PF | CF),
		"IRET": (OF | SF | ZF | AF | PF | CF | IF | DF),
		"LAR": (ZF),
		"LOOPZ": (0, ZF),
		"LOOPNZ": (0, ZF),
		"LSL": (ZF),
		"LZCNT": (ZF | CF, 0, OF | SF | AF | PF),
		"MUL": (OF | CF, 0, SF | ZF | AF | PF),
		"NEG": (OF | SF | ZF | AF | PF | CF),
		"OR": (SF | ZF | PF, AF),
		"POPCNT": (ZF),
		"POPF": (OF | SF | ZF | AF | PF | CF | IF | DF),
		"RSM": (OF | SF | ZF | AF | PF | CF | IF | DF),
		"SAHF": (SF | ZF | AF | PF | CF),
		"SBB": (OF | SF | ZF | AF | PF | CF, CF),
		"STC": (CF),
		"STD": (DF),
		"STI": (IF),
		"SUB": (OF | SF | ZF | AF | PF | CF),
		"TEST": (SF | ZF | PF, 0, AF),
		"VERR": (ZF),
		"VERW": (ZF),
		"XADD": (OF | SF | ZF | AF | PF | CF),
		"XOR": (SF | ZF | PF, 0, AF),
		# IO/String instructions:
		"MOVS": (0, DF),
		"LODS": (0, DF),
		"STOS": (0, DF),
		"CMPS":	(OF | SF | ZF | AF | PF | CF, DF),
		"SCAS": (OF | SF | ZF | AF | PF | CF, DF),
		"INS": (0, DF),
		"OUTS": (0, DF)
	}
	# Check for mnemonics in the above table.
	for i in ii.mnemonics:
		if InstByMnem.has_key(i) and (ii.flags & InstFlag.PSEUDO_OPCODE) == 0:
			SetInstructionAffectedFlags(ii, InstByMnem[i])
			return

	# Look carefuly for SETcc or Jcc instructions.
	for i in ["SET", "CMOV", "FCMOV"]:
		if ii.mnemonics[0].find(i) == 0:
			SetInstructionAffectedFlags(ii, GetTestedFlagsForCondition(ii.mnemonics[0][len(i):]))
			return
	# See if it's a Jcc instruction.
	if ii.mnemonics[0][:1] == "J" and ii.mnemonics[0][:2] not in ["JM", "JC", "JE", "JR"]:
		SetInstructionAffectedFlags(ii, GetTestedFlagsForCondition(ii.mnemonics[0][1:]))
		return

	# Still no match, try special shift/rotate instructions.
	# Special shift/rotate instruction that with constant 1 have different flag affections:
	# First tuple is with constant 1, second tuple is with any count (CL).
	Shifts = [
		(["RCL", "RCR"], (OF | CF, CF), (CF, CF, OF)),
		(["ROL", "ROR"], (OF | CF), (CF, 0, OF)),
		(["SAL" "SAR", "SHL", "SHR"], (OF | SF | ZF | PF | CF, 0, AF), (SF | ZF | PF | CF, 0, OF | AF)),
		(["SHLD", "SHRD"], (OF | SF | ZF | PF | CF, 0, AF), (SF | ZF | PF | CF, 0, OF | AF))
	]
	for i in Shifts:
		for j in i[0]:
			if ii.mnemonics[0] == j:
				flags = i[1] if ii.operands[1] == OperandType.CONST1 else i[2]
				SetInstructionAffectedFlags(ii, flags)
				return

	# The instruction doesn't affect any flags...
	return

# Table to hold shared inst-info.
sharedInfoDict = {}
# Table to hold shared flags.
flagsDict = {}

def FormatInstruction(ii):
	""" Formats a string with all information relevant for diStorm InstInfo structure
	or the InstInfoEx. These are the internal structures diStorm uses for holding the instructions' information.
	Using this structure diStorm knows how to format an opcode when it reads it from the stream.

	An instruction information structure is found by its byte codes with a prefix of "II_".
	So for example ADD EAX, Imm32 instruction is II_00.
	Since there are several types of instructions information structures,
	the tables which point to these non-default InstInfo structures, will have to cast the pointer. """

	# There might be optional fields, if there's a 3rd operand or a second/third mnemonic.
	optFields = ""
	# Default type of structure is InstInfo.
	type = "_InstInfo"

	# Make sure the instruction can be fully represented using the diStorm3 _DecodeInst structure.
	CheckOTCollisions(ii)

	# Add flags for flow control instructions.
	CheckForFlowControl(ii)

	# Add flags for writable destination operand.
	CheckWritableDestinationOperand(ii)

	# Add affected modified/tested/undefined flags for instruction.
	CheckInstructionAffectedFlags(ii)

	# Pad mnemonics to three, in case EXMNEMONIC/2 isn't used (so we don't get an exception).
	mnems = TranslateMnemonics([None, ii.classType][(ii.flags & InstFlag.PSEUDO_OPCODE) == InstFlag.PSEUDO_OPCODE], ii.mnemonics) + ["0", "0"]

	# Pad operands to atleast three (so we don't get an exception too, since there might be instructions with no operands at all).
	ops = ii.operands + [OperandType.NONE, OperandType.NONE, OperandType.NONE, OperandType.NONE]

	# Is it an extended structure?
	if ii.flags & InstFlag.EXTENDED:
		# Since there's a second and/or a third mnemonic, use the the InstInfoEx structure.
		type = "_InstInfoEx"
		flagsEx = 0
		# Fix flagsEx to have the VEX flags, except PRE_VEX.
		if ii.flags & InstFlag.PRE_VEX:
			flagsEx = ii.flags >> InstFlag.FLAGS_EX_START_INDEX
		# If there's a third operand, use it, otherwise NONE.
		op3 = [OperandType.NONE, ops[2]][(ii.flags & InstFlag.USE_OP3) == InstFlag.USE_OP3]
		op4 = [OperandType.NONE, ops[3]][(ii.flags & InstFlag.USE_OP4) == InstFlag.USE_OP4]
		if flagsEx >= 256: # Assert the size of flagsEx is enough to holds this value.
			raise "FlagsEx exceeded its 8 bits. Change flagsEx of _InstInfoEx to be uint16!"
		# Concat the mnemonics and the third operand.
		optFields = ", 0x%x, %d, %d, %s, %s" % (flagsEx, op3, op4, mnems[1], mnems[2])

	# Notice we filter out internal bits from flags.
	flags = ii.flags & ((1 << InstFlag.FLAGS_EX_START_INDEX)-1)
	# Allocate a slot for this flag if needed.
	if not flagsDict.has_key(flags):
		flagsDict[flags] = len(flagsDict)
	# Get the flags-index.
	flagsIndex = flagsDict[flags]
	if flagsIndex >= 256:
		raise "FlagsIndex exceeded its 8 bits. Change flags of _InstInfo to be uint16!"

	# InstSharedInfo:
	sharedInfo = (flagsIndex, ops[1], ops[0], (ii.classType << 3) | ii.flowControl, ii.modifiedFlags, ii.testedFlags, ii.undefinedFlags)
	if not sharedInfoDict.has_key(sharedInfo):
		sharedInfoDict[sharedInfo] = len(sharedInfoDict)
	# Get the shared-info-index.
	sharedInfoIndex = sharedInfoDict[sharedInfo]
	if sharedInfoIndex >= 2**16:
		raise "SharedInfoIndex exceeded its 16 bits. Change type of sharedInfoIndex in _InstInfo!"

	fields = "0x%x, %s" % (sharedInfoIndex, mnems[0])
	# "Structure-Name" = II_Bytes-Code {Fields + Optional-Fields}.
	return ("\t/*II%s*/ {%s%s}" % (ii.tag, fields, optFields), (ii.flags & InstFlag.EXTENDED) != 0)

def FilterTable(table):
	# All tables must go to output.
	return True

def GeneratePseudoMnemonicOffsets():
	"""
	Generate the static offset tables for psuedo compare instructions both for SSE and AVX.
	The table is built in such a way that each cell holds the offset from the first pseudo mnemonic
	to the indexed one.
	"""
	# Lengths of pesudo mnemonics (SSE=CMPxxxYY + null + lengthByte)
	lengths = map(lambda x: 3 + len(x) + 2 + 2, SSECmpTypes)
	s = "uint16_t CmpMnemonicOffsets[8] = {\n" + ", ".join([str(sum(lengths[:i] or [0])) for i in xrange(len(lengths))]) + "\n};\n";

	# (AVX=VCMPxxxYY + null + lengthByte).
	lengths = map(lambda x: 4 + len(x) + 2 + 2, AVXCmpTypes)
	s += "uint16_t VCmpMnemonicOffsets[32] = {\n" + ", ".join([str(sum(lengths[:i] or [0])) for i in xrange(len(lengths))]) + "\n};";
	return s

def CreateTables(db):
	""" This is the new tables generator code as for May 2011.
	Its purpose is to return all tables and structures ready to use at once by diStorm.

	The information is divided into 3 categories (arrays):
	1) The InstructionsTree root table, which holds all id's (InstNode) and refelects a tree, inside a flat array.
	2) The InstInfos table, which holds all Instruction-Information structures - the actual (basic) info per instruction.
	3) The InstInfosEx table, which holds all extended Instruction-Information structures.

	Each array should be flat one defined only once. This means that we need to serialize all instruction-set tables into a single
	table of pointers, kinda. This pointer is now a InstNode, which is really a 16 bits structure.
	The low 13 bits are an index. The upper 3 bits are the type of what the index points to.
	So basically, an index can be up to 8192 which is good enough as for now, cause we only have around ~5k entries in the tree.
	However, it can be an index into the InstInfos or InstInfosEx tables, depends on the type.

	A note from Feb 2007 - This new data layout in contrast with the old data layout saves more memory space (~12KB).
	This new serialization should even save around 25kb! Because now we don't use real pointers anymore, only this tiny formatted InstNode.

	The new method uses the last method, but instead of dividing the tree into many id's and pointer's tables,
	it will now concatenate them all into the relevant tables. And instead of a real pointer to an Instruction-Information structure,
	we will use an index into each table.

	For example, say we have the following instructions table (byte code and mnemonic):
	0 - AND
	1 - XOR
	2 - OR
	3 - EMPTY (NO-INSTRUCTION-IS-ENCODED)
	4 - EMPTY
	5 - SHL
	6 - SHR
	7 - EMPTY

	Old Documentation:
	------------------
	So instead of generating the following old data layout:
	{&II_00, &II_01, &II_02, NULL, NULL, &II_05, &II_06, NULL}
	(Actually the old layout is a bit more complicated and consumes another byte for indicating the type of node.)
	
	Anyways, we can generate the follow table:
	{1, 2, 3, 0, 0, 4, 5, 0}
	This time the table is in bytes, a byte is enough to index 256 instructions (which is a Full sized table).
	However, an id's table is not enough, we need another table, the pointers table, which will look like this (following the above example):
	{NULL, &II_00, &II_01, &II_02, &II_05, &II_06}

	Note that if there are no EMPTY instructions in the table the first NULL entry will be omitted!

	Assuming most of the space we managed to spare goes for telling diStorm "hey, this instruction is not encoded", we spared around 12KB.
	So all empty instructions points to the same first entry inside its corresponding pointers table.
	This way we pay another array of bytes for each table, but eliminate all NULL's.

	So the actual node looks something like this:
	{8, &table_00_ids, &table_00_pointers}
	Which costs another dereference inside diStorm decoder.

	New Documentation:
	------------------
	As you can see, I did a pass back in 2007 to spare some empty entries in the tables.
	But I kept using real pointers, which took lots of space. This time,
	I am going to use a flat array which will represent the whole tree.
	And combine all data into arrays, and spare even the old InstNode which was a small structure that says
	what's the type of the table it points to. This type stuff will now be embedded inside the InstNode integer.

	The new tables look like this (according to the above example):
	InstInfo InstInfos[] = {
		{AND info...},
		{XOR info...},
		{OR info...},
		{SHL info...},
		{SHR info...}
	};

	And another InstNodes table:
	InstNode InstructionsTree[] = {
		0 | INSTINFO << 13,
		1 | INSTINFO << 13,
		2 | INSTINFO << 13,
		-1,
		-1,
		3 | INSTINFO << 13,
		4 | INSTINFO << 13,
		-1,
	};

	The example happened to be a single table.
	But suppose there's another index which points to another table in the tree, it would look like:
	{TableIndexInInstructionsTree | TABLE << 13}
	This way we know to read another byte and follow the next table...

	:!:NOTE:!: You MUST iterate a table with GenBlock wrapper, otherwise you might NOT get all instructions from the DB!
		   Refer to x86db.py-class GenBlock for more information. """
	indexShift = 13 # According to InstNode in instructions.h.
	InstInfos = []
	InstInfosEx = []
	InstructionsTree = []
	externTables = []
	nextTableIndex = 256 # Root tree takes 256 nodes by default, so skip them.
	# Scan all tables in the DB.
	for x in db.GenerateTables(FilterTable):
		# Don't make static definitions for specific exported tables.
		if x.tag in ["_0F_0F", "_0F", "_0F_3A", "_0F_38"]:
			# Store the index of these special tables, they are used directly in instructions.c.
			externTables.append((x.tag, len(InstructionsTree)))
		# Notice we use GenBlock for the special instructions, this is a must, otherwise we miss instructions from the DB.
		for i in x86db.GenBlock(x):
			if isinstance(i, x86db.InstructionInfo):
				formattedII, isExtended = FormatInstruction(i)
				if isExtended:
					InstInfosEx.append(formattedII)
					index = len(InstInfosEx) - 1
					InstructionsTree.append((NodeType.INFOEX << indexShift | index, i.tag))
				else:
					InstInfos.append(formattedII)
					index = len(InstInfos) - 1
					InstructionsTree.append((NodeType.INFO << indexShift | index, i.tag))
			elif isinstance(i, x86db.InstructionsTable):
				InstructionsTree.append(((i.type << indexShift) | nextTableIndex, i.tag))
				nextTableIndex += i.size # This assumes we walk on the instructions tables in BFS order!
			else:
				# False indicates this entry points nothing.
				InstructionsTree.append((0, ""))
	s = ["\n".join(["_InstInfo II_%s =%s;" % (i.mnemonics[0] if i.mnemonics[0][0] != '_' else i.mnemonics[0][1:], FormatInstruction(i)[0]) for i in db.getExportedInstructions()]),
		"_iflags FlagsTable[%d] = {\n%s\n};" % (len(flagsDict), ",\n".join(["0x%x" % i[1] for i in sorted(zip(flagsDict.values(), flagsDict.keys()))])),
		"\n".join(["_InstNode Table%s = %d;" % (i[0], i[1]) for i in externTables]),
		"_InstInfo InstInfos[%d] = {\n%s\n};" % (len(InstInfos), ",\n".join(InstInfos)),
		"_InstInfoEx InstInfosEx[%d] = {\n%s\n};" % (len(InstInfosEx), ",\n".join(InstInfosEx)),
		"_InstNode InstructionsTree[%d] = {\n%s\n};" % (len(InstructionsTree), ",\n".join(["/* %x - %s */  %s" % (i[0], i[1][1], "0" if i[1][0] == 0 else "0x%x" % i[1][0]) for i in enumerate(InstructionsTree)])),
		# sharedInfoDict must be evaluated last, since the exported instructions above add items to it!
		"_InstSharedInfo InstSharedInfoTable[%d] = {\n%s\n};" % (len(sharedInfoDict), ",\n".join(["{%s}" % str(i[1])[1:-1] for i in sorted(zip(sharedInfoDict.values(), sharedInfoDict.keys()))])),
		GeneratePseudoMnemonicOffsets()]
	return "\n\n".join(s)

def main():
	# Init the 80x86/x64 instructions sets DB.
	db = x86db.InstructionsDB()
	x86InstructionsSet = x86sets.Instructions(db.SetInstruction)

	# Open file for output.
	f = open("output.txt", "w")

	f.write("/*\n * GENERATED BY disOps at %s\n */\n\n" % time.asctime())

	# Generate all tables of id's and pointers with the instructions themselves.
	lists = CreateTables(db)
	# Write them to the file also.
	f.write(lists)
	f.close()

	DumpMnemonics()

	print "The file output.txt was written successfully"
main()
