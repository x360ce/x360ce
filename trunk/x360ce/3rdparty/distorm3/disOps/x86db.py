#
# x86db.py
#
# Copyright (C) 2012 Gil Dabah, http://ragestorm.net/disops/
#

from x86header import *

# The mandatory prefix bytes list:
_MandatoryPrefixesList = [0x9b, 0x66, 0xf3, 0xf2]
# Map the mandatory prefix to its corresponding entry number in the PREFIXED table.
# Note that no-prefix is first entry.
_MandatoryPrefixToEntry = {0x9b: 1, 0x66: 1, 0xf3: 2, 0xf2: 3}

#
# Prefixed Table (12 entries):
#
# Normal optional prefix:
# 0 - NONE, 1 - 66/9b, 2 - f3, 3 - f2
#
# With VEX prefix (if based on MOD, then MOD=11):
# 4 - NONE, 5 - 66/9b, 6 - f3, 7 - f2
#
# With VEX prefix, (MOD!=11):
# 8 - NONE, 9 - 66/9b, 10 - f3, 11 - f2
#
# Note: VEX.L and VEX.W affections can be described in a single InstInfo structure and thus we don't need to split them too.
#       The problem with instructions that are encodable with or without VEX.VVVV
#       is that a single InstInfo structure doesn't have a few sets of operands.

class DBException(Exception):
	""" Used in order to throw an exception when an error occurrs in the DB. """
	pass


class InstructionInfo:
	""" Instruction Info holds all information relevant for an instruction.
	another string member, self.tag, will be initialized in runtime to have the bytes of the opcode (I.E: 0f_0f_a7). """
	def __init__(self, classType, OL, pos, isModRMIncluded, mnemonics, operands, flags):
		self.tag = ""
		self.classType = classType
		# Check for special mandatory-prefixed instruction.
		if pos[0] in _MandatoryPrefixesList:
			self.pos = pos[1:]
			self.prefix = pos[0]
			self.OL = OpcodeLength.NextOL[OL]
			self.prefixed = True
			self.entryNo = _MandatoryPrefixToEntry[pos[0]]
		else:
			self.pos = pos
			self.prefix = 0
			self.OL = OL
			self.prefixed = False
			self.entryNo = 0
		self.mnemonics = mnemonics
		self.operands = operands
		self.flags = flags
		# CPU affected flags by instruction:
		self.modifiedFlags = 0
		self.testedFlags = 0
		self.undefinedFlags = 0
		if len(self.operands) == 3:
			self.flags |= InstFlag.USE_OP3
		elif len(self.operands) == 4:
			self.flags |= InstFlag.USE_OP3 | InstFlag.USE_OP4
		if isModRMIncluded:
			self.flags |= InstFlag.MODRM_INCLUDED
		# Does it use any of the VEX.vvvv field to describe an operand?
		if len(filter(lambda x: x in [OperandType.VXMM, OperandType.VYMM, OperandType.VYXMM], self.operands)) == 0:
			self.flags |= InstFlag.VEX_V_UNUSED
		self.VEXtag = ""
		# Special treatment for VEX instructions:
		if self.flags & InstFlag.PRE_VEX:
			# We assume that for now VEX.vvvv and MODRR are coupled in some instructions, it might not be the case in the future.
			# VEX instructions have 3 options (only latter two can collide), so we will have to split them:
			# 1) Normal VEX instruction is mapped to range 4 - 8 by default.
			# 2) Instruction which is MODRR based and uses VEX.vvvv is mapped to range 4 - 8.
			# 3) Instruction which is MODRR based and doesn't use VEX.vvvv is mapped to range 8 - 12.
			self.entryNo += 4 # For VEX
			self.VEXtag = "V"
			if self.flags & (InstFlag.MODRR_BASED | InstFlag.VEX_V_UNUSED) == (InstFlag.MODRR_BASED | InstFlag.VEX_V_UNUSED):
				self.entryNo += 4 # MODRR without VEX.VVVV
				self.VEXtag += "RR"
		# Assert that OL is < OL_33:
		if self.OL >= OpcodeLength.OL_33:
			raise DBException("Instruction OL is bigger than OL_33.")

class InstructionsTable:
	""" A table contains all instructions under its index. The number of instructions varyies and depends on its type.
	Note that a table be nested in itself.
	Every table has its position beginning in the db.root.
	So all opcodes that begin with first byte with the value of 0x0f, will be in the 0x0f table (which has a tag "0f"). """
	Full = 256
	Divided = 72
	Group = 8
	Prefixed = 12

	def __init__(self, size, tag, pos):
		self.list = {}
		self.size = size
		# The type of the table is determined by its actual size.
		# Set up a limit number for the table, used in the iterator.
		if size == self.Full:
			self.type = NodeType.LIST_FULL
			self.limit = self.Full
		elif size == self.Divided:
			# Since the ranges of Divided instructions are 0x0-0x7 and 0xc0-0xff, we limit it as a full sized table.
			self.type = NodeType.LIST_DIVIDED
			self.limit = self.Full
		elif size == self.Group:
			self.type = NodeType.LIST_GROUP
			self.limit = size
		elif size == self.Prefixed:
			self.type = NodeType.LIST_PREFIXED
			self.limit = size
		self.tag = tag
		self.pos = pos

	def __iter__(self):
		""" This is the "ctor" of the iterator. """
		# Begin with the first opcode.
		self.__iterIndex = -1
		return self

	def next(self):
		""" This is the core of the iterator, return the next instruction or halt. """
		# Get next instruction.
		self.__iterIndex += 1
		# Optimization: skip unused entries in Divided tables.
		if self.type == NodeType.LIST_DIVIDED and self.__iterIndex == 8:
			# Actually it must be done, because the iterator-user assumes Divided table is 72 entries long.
			# So it happens that len(list(divided-table)) == 72 !
			self.__iterIndex = 0xc0
		# Stop the iteration in case we reached the limit.
		if self.__iterIndex == self.limit:
			raise StopIteration
		# If we have the key return its corresponding opcode,
		# it might be that we return an object of another nested InstructionTable as well.
		if self.list.has_key(self.__iterIndex):
			item = self.list[self.__iterIndex]
			return item
		# In case no InstructionInfo or InstructionsTable were found, return None (this doesn't stop the iteration!).
		return None

class GenBlock:
	""" There are some special instructions which have the operand encoded in the code byte itself.
	For instance: 40: INC EAX 41: ECX. push/pop/dec, etc...
	Therefore, these instructions can be treated specially in the tables, so instead of generating a unique instruction-info per such instruction.
	We "cheat" by making some entries in the table point to the same instruction-info.
	Following the last example, all instructions in the range of 0x40-0x47 point to the instruction-info 0x40, which means INC <REG-FROM-SAME-BYTE>.
	This means that we don't call SetInstruction for the range 0x40-0x47, only a single set instruction per this block
	(8 instructions which their REG field is extracted from their own byte code).
	So in order to simulate the real case where there are actually 8 instructions that were set using SetInstruction,
	this class handles this special flag and returns the same first instruction for its corresponding block at runtime. """

	# Number of generated instructions in a row.
	Block = 8

	def __init__(self, list):
		if isinstance(list, InstructionsTable) == False:
			raise DBException("List must be InstructionsTable object")
		self.list = list

	def __iter__(self):
		""" This is the "ctor" of the iterator. """
		# Count up to Block instructions.
		self.counter = 0
		# This is the item we're going to return every iteration.
		self.item = None
		# Start the InstructionsTable internal iterator as well.
		self.list.__iter__()
		return self

	def next(self):
		# Get next item from internal iterator.
		i = self.list.next()
		# If there's an item set, it means we hit the special opcode before.
		if self.item != None:
			# Countup
			self.counter += 1
			# If we reached the limit, stop.
			if self.counter == self.Block:
				self.counter = 0
				self.item = None
		# See if the item we read is an opcode and whether it has the GEN_BLOCK.
		if isinstance(i, InstructionInfo) and i.flags & InstFlag.GEN_BLOCK:
			# Store this instruction for next 8 iterations.
			self.item = i
			return i
		elif i == None and self.item != None:
			# Return the stored item, only if there's no opcode set.
			# Sometimes, there are GEN_BLOCK instructions and the next instruction is also set (means it has its own OpcodeInfo)
			# so we have to return real instructions first, and then generated instructions.
			return self.item
		# Return the instruction we read from the real list.
		return i

class InstructionsDB:
	""" The Instructions Data Base holds all instructions under it.
	The self.root is where all instructions begin, so instructions that are 1 byte long, will be set directly there.
	But instructions that are 2 instructions long, will be set under another InstructionsTable nested inside the self.root.

	The DB is actually the root of a Trie. (For more info about Trie see diStorm's instructions.h). """
	def __init__(self):
		# Root contains a Full InstructionsTable with 256 entries.
		self.root = InstructionsTable(InstructionsTable.Full, "", [])
		# Special instructions that diStorm has to use manually and can look them up using the tables.
		self.exportedInstructions = []

	def getExportedInstructions(self):
		return self.exportedInstructions

	def HandleMandatoryPrefix(self, type, o, pos, ii, tag):
		if ii.prefixed:
			ii.tag = "_%02X%s" % (ii.prefix, ii.tag)
		if ii.flags & InstFlag.PRE_VEX:
			ii.tag = "_%s%s" % (ii.VEXtag, ii.tag)
		# If there is nothing at this index, create a prefixed table.
		if o.list.has_key(pos[0]) == False:
			o.list[pos[0]] = InstructionsTable(InstructionsTable.Prefixed, tag, "")
		# If there's a table constructred already (doesn't matter if by last line).
		if isinstance(o.list[pos[0]], InstructionsTable) and o.list[pos[0]].type == NodeType.LIST_PREFIXED:
			# Check for obvious collision.
			if o.list[pos[0]].list.has_key(ii.entryNo):
				raise DBException("Collision in prefix table.")
			# Link the instruction to its index.
			o.list[pos[0]].list[ii.entryNo] = ii
		# The index is taken and it's not a prefixed table, we will have to convert it then.
		else:
			# Store current instruction which we are going to replace with a table.
			tmp = o.list[pos[0]]
			# Check for collision first, if it's a prefixed instruction at all.
			if (not ii.prefixed and ii.pos[0] != 0x0f) or (tmp.entryNo == ii.entryNo):
				msg = "Instruction Collision: %s" % str(o.list[pos[0]])
				raise DBException(msg)
			# Create a prefixed table.
			o.list[pos[0]] = InstructionsTable(InstructionsTable.Prefixed, tag, "")
			# Link the previous instruction.
			o.list[pos[0]].list[tmp.entryNo] = tmp
			# Link new instruction.
			o.list[pos[0]].list[ii.entryNo] = ii

	def CreateSet(self, type, o, pos, ii, tag = "", level = 0):
		""" This is the most improtant function in the whole project.
		It builds and links a new InstructionsTable if required and
		afterwards sets the given InstructionInfo object in its correct place.
		It knows to generate the nested lists dynamically, building a Trie DB.

		The algorithm for building the nested tables is as follows:
		See if you got to the last byte code of the instruction, if so, link the instruction info and exit.
		Try to enter the first index in the list, if it doesn't exist, create it.
		If it exists, take off the first index from its array, (since we already entered it), and RECURSE with the new(/existing) list now.

		In practice it's a bit more complex since there are 3 types of tables we can create, and we have to take care of it.
		Let's see two examples of how it really works with the following input (assuming root is empty):
		0: OL_3, root, [0x67, 0x69, 0x6c], II_INST

		1: Create Table - with size of 256 at index 0x67
		Recurse - OL_2, root[0x67], [0x69, 0x6c], II_INST

		2: Create Table - with size of 256 at index 0x69
		Recurse - OL_1, root[0x67][0x69], [0x6c], II_INST

		3: Link Instruction Information - at index 0x6c, since type is OL_1
		root[0x67][0x69][0x6c] = II_INST
		exit

		Second example:
		0: OL_23, root, [0x0f, 0xb0, 0x03], II_INST2

		1: Create Table - with size of 256 at index 0x0f
		Recurse - OL_13, root[0x0f], [0xb0, 0x03], II_INST2

		2: Create Table - with size of 8(GROUP) at index 0xb0, since input type is OL_13
		Recurse - OL_1, root[0x0f][0xb0], [0x03], II_INST2

		3: Link Instruction Information - at index 0x03, since type is OL_1
		root[0x0f][0xb0][0x03] = II_INST2
		exit

		Every table we create is usually a Full sized table (256 entries), since it can point to next 256 instructions.
		If the input type is OL_13 or OL_1d we know we have to create a Group sized table or Divided sized table, correspondingly.
		OL_13/OL_1d means its the last table to build in the sequence of byte codes of the given instruction.

		OL_1 always means that we just have to link the instruction information and that all tables are built already.
		Therefore the "next" of OL_13/OL_1d is always OL_1.
		
		Special case for mandatory prefixed instructions:
		If the instruction's first opcode byte is a mandatory prefix (0x66, 0xf2, 0xf3), then we will skip it in the root.
		However, it will be set in the same table of that instruction without the prefix byte.
		Therefore if there are a few instructions that the only difference among them is the mandatory prefix byte,
		they will share a special table. This "PREFIXED" table points to the Instruction Information of those possible instructions.
		Also the information for the same instruction without any mandatory prefix will be stored in this table.
		Entries order: None, 0x66, 0xf2, 0xf3.

		Example: [0x0f, 0x2a], ["CVTPI2PS"]
				 [0x66, 0x0f, 0x2a], ["CVTPI2PD"]
				 [0xf3, 0x0f, 0x2a], ["CVTSI2SS"]

		When there is a collision with the same instruction, we will know to change it into a PREFIXED table.
		"""

		# Keep track of the index we scan.
		tag += "_%02X" % pos[0]
		# If the type is OL_1, it means we have to set the InstructionInfo in o.list, which is the last table we need.
		if type == OpcodeLength.OL_1:
			# Set the tag (position formatted in text) of the Instruction itself.
			ii.tag = tag
			# If the instruction is already set, there's something wrong with the DB initializer,
			# probably a collision inside x86sets.py.
			if ii.prefixed:
				self.HandleMandatoryPrefix(type, o, pos, ii, tag)
				return
			if o.list.has_key(pos[0]) == True:
				self.HandleMandatoryPrefix(type, o, pos, ii, tag)
				return
			# Link the instruction info in its place.
			o.list[pos[0]] = ii
			# Stop recursion.
			return
		# See whether we have to create a nested table.
		if o.list.has_key(pos[0]) == False:
			# All tables are full sized.
			tableType = InstructionsTable.Full
			if type == OpcodeLength.OL_13:
				# Except 1.3 - Create a Group table.
				tableType = InstructionsTable.Group
			elif type == OpcodeLength.OL_1d:
				# And except 1.d - Create a Divided table.
				tableType = InstructionsTable.Divided
			# Create and link the new table at the same time in its place.
			o.list[pos[0]] = InstructionsTable(tableType, tag, ii.pos[:-1])
		# This is the tricky recursive call,
		# 1) Get the next OL we need, so we know what to do next, place an instruction info, or create another table.
		# 2) Return the current table, which must be created, because it's either was just created or exists from before.
		# 3) Since pos is a list with the indexes of the instruction, and we just used pos[0], we move to the next indexes,
		#    by removing the first item.
		# 4) The instruction info to set when we reach its place.
		# 5) The updated tag with the indexes of the instruction.
		self.CreateSet(OpcodeLength.NextOL[type], o.list[pos[0]], pos[1:], ii, tag, level + 1)

	def SetInstruction(self, *args):
		""" This function is used in order to insert an instruction info into the DB. """
		if (args[4] & InstFlag.EXPORTED) != 0:
			ii = InstructionInfo(args[0], OpcodeLength.OL_1, [0], False, args[2], args[3], args[4])
			self.exportedInstructions.append(ii)
			return
		# *args = ISetClass, OL, pos, mnemonics, operands, flags
		# Construct an Instruction Info object with the info given in args.
		opcode = args[1].replace(" ", "").split(",")
		# The number of bytes is the base length, now we need to check the last entry.
		pos = [int(i[:2], 16) for i in opcode]
		last = opcode[-1][2:] # Skip hex of last full byte
		isModRMIncluded = False # Indicates whether 3 bits of the REG field in the ModRM byte were used.
		if last[:2] == "//": # Divided Instruction
			pos.append(int(last[2:], 16))
			isModRMIncluded = True
			try:
				OL = {1:OpcodeLength.OL_1d, 2:OpcodeLength.OL_2d}[len(opcode)]
			except KeyError:
				raise DBException("Invalid divided instruction opcode")
		elif last[:1] == "/": # Group Instruction
			isModRMIncluded = True
			pos.append(int(last[1:], 16))
			try:
				OL = {1:OpcodeLength.OL_13, 2:OpcodeLength.OL_23, 3:OpcodeLength.OL_33}[len(opcode)]
			except KeyError:
				raise DBException("Invalid group instruction opcode")
		elif len(last) != 0:
			raise DBException("Invalid last byte in opcode")
			# Normal full bytes instruction
		else:
			try:
				OL = {1:OpcodeLength.OL_1, 2:OpcodeLength.OL_2, 3:OpcodeLength.OL_3, 4:OpcodeLength.OL_4}[len(opcode)]
			except KeyError:
				raise DBException("Invalid normal instruction opcode")
		ii = InstructionInfo(args[0], OL, pos, isModRMIncluded, args[2], args[3], args[4])
		# Insert the instruction into the table, take care of nested tables, etc...
		self.CreateSet(ii.OL, self.root, ii.pos, ii)

	def GenerateTables(self, filter):
		""" GenerateTables is a generator function that iterates over an InstructionsTable,
		it returns all nested tables in the DB.
		The tables are returned in BFS order!
		If you pass a filter, that filter will be called for every table and
		should return True for letting the generator return it. """
		# Start with the root, if no list was specified.
		list = self.root
		list.tag = "ROOT"
		# Return the root first.
		stack = [list]
		while len(stack) > 0:
			list = stack.pop(0)
			yield list
			for i in list:
				if isinstance(i, InstructionsTable):
					if filter is not None:
						# If a filter is set, call it with the table.
						if filter(i):
							# If the return value was True, return this same InstructionInfo.
							stack.append(i)
					else:
						# If no filter was set, just push this table.
						stack.append(i)
