#
# diStorm3 (Mac Port)
#

TARGET	= libdistorm3.dylib
PYTHON_BUILD_DIR = ../../Python/macosx-x86
COBJS	= ../../src/mnemonics.o ../../src/wstring.o ../../src/textdefs.o ../../src/prefix.o ../../src/operands.o ../../src/insts.o ../../src/instructions.o ../../src/distorm.o ../../src/decoder.o
CC	= gcc
CFLAGS	= -arch i386 -arch x86_64 -O2 -Wall -fPIC -DSUPPORT_64BIT_OFFSET -DDISTORM_DYNAMIC

all: clib

clean:
	/bin/rm -rf ../../src/*.o ${TARGET} ../../libdistorm3.dylib ../../distorm3.a  ../../*.a

clib: ${COBJS}
	${CC} ${CFLAGS} ${VERSION} ${COBJS} -fPIC -dynamiclib -o ${TARGET}
	[ -d ${PYTHON_BUILD_DIR} ] && rm -rf ${PYTHON_BUILD_DIR} || true
	mkdir ${PYTHON_BUILD_DIR}
	cp ${TARGET} ${PYTHON_BUILD_DIR}/
	#ar rs ../../distorm3.a ${COBJS}

.c.o:
	${CC} ${CFLAGS} ${VERSION} -c $< -o $@
