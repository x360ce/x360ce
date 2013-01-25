#import distorm
from pyasm import *
from distorm3 import *

_REGS = ["RAX", "RCX", "RDX", "RBX", "RSP", "RBP", "RSI", "RDI", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15",
	"EAX", "ECX", "EDX", "EBX", "ESP", "EBP", "ESI", "EDI", "R8D", "R9D", "R10D", "R11D", "R12D", "R13D", "R14D", "R15D",
	"AX", "CX", "DX", "BX", "SP", "BP", "SI", "DI", "R8W", "R9W", "R10W", "R11W", "R12W", "R13W", "R14W", "R15W",
	"AL", "CL", "DL", "BL", "AH", "CH", "DH", "BH", "R8B", "R9B", "R10B", "R11B", "R12B", "R13B", "R14B", "R15B",
	"SPL", "BPL", "SIL", "DIL",
	"ES", "CS", "SS", "DS", "FS", "GS",
	"RIP",
	"ST0", "ST1", "ST2", "ST3", "ST4", "ST5", "ST6", "ST7",
	"MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7",
	"XMM0", "XMM1", "XMM2", "XMM3", "XMM4", "XMM5", "XMM6", "XMM7", "XMM8", "XMM9", "XMM10", "XMM11", "XMM12", "XMM13", "XMM14", "XMM15",
	"YMM0", "YMM1", "YMM2", "YMM3", "YMM4", "YMM5", "YMM6", "YMM7", "YMM8", "YMM9", "YMM10", "YMM11", "YMM12", "YMM13", "YMM14", "YMM15",
	"CR0", "", "CR2", "CR3", "CR4", "", "", "", "CR8",
	"DR0", "DR1", "DR2", "DR3", "", "", "DR6", "DR7"]

def decode(x, mode = 1):
	sizes = [16, 32, 64]
	x = Assemble(x, sizes[mode])
	print x.encode('hex')
	#print distorm.Decode(0, x, mode)
	print Decode(0, x, mode)

#decode("bswap ecx", 1)
#distorm3.Decode(0, "480fc3c0".decode('hex'), 2)


def xxx(x):
	buf = "".join(map(lambda txt: Assemble(txt, 32), x.split("\n")))
	print ",0x".join(map(lambda x: "%02x" % ord(x), buf))
	return Decode(0, buf, Decode32Bits)[0]

def yyy(inst):
	print "%x (%d): " % (inst["addr"], inst["size"])
	print inst
	ops = filter(lambda x:x is not None, inst["ops"])
	for o in ops:
		if o["type"] == O_REG:
			print _REGS[o["index"]]
		elif o["type"] == O_IMM:
			print hex(inst["imm"])
		elif o["type"] == O_MEM:
			print "[",
			if inst["base"] != R_NONE:
				print _REGS[inst["base"]],
				print "+",
			print _REGS[o["index"]],
			if inst["scale"] != 0:
				print "*%d" % inst["scale"],
			if inst["dispSize"] != 0:
				print " + 0x%x" % (inst["disp"]),
			print "]"
		elif o["type"] == O_SMEM:
			print "[%s" % (_REGS[o["index"]]),
			if inst["dispSize"] != 0:
				print " + 0x%x" % (inst["disp"]),
			print "]"
		elif o["type"] == O_DISP:
			print "[0x%x]" % inst["disp"]
		elif o["type"] == O_PC:
			print hex(inst["imm"])

#yyy(Decode(0, "0fae38".decode('hex'), Decode32Bits)[0])
yyy(xxx("mov eax, [ebp*4]"))
