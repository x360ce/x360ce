#
# diStorm (Linux Port) / Demo Application Makefile
#

TARGET	= disasm
CC	= gcc
CFLAGS	= -Wall -O2 -I. -o

all:	disasm

disasm:
	${CC} ${CFLAGS} ${TARGET} main.cpp ../distorm64.a

clean:
	/bin/rm -rf *.o ${TARGET} 
