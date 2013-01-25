// TitaniumHooks.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "distorm.h"
#include <Tlhelp32.h>
#include <Winternl.h>
#include <psapi.h>
#include <vector>

// Converted TitanEngine source
// Global.Engine:
LPVOID hListThread = 0;
CONTEXT DBGContext = {};
PROCESS_INFORMATION dbgProcessInformation = {};
char engineDisassembledInstruction[128];
// Global.Engine.Hooks:
DWORD buffPatchedEntrySize = 0x3000;
void* CwpBuffPatchedEntry;
void* buffPatchedEntry;
std::vector<HOOK_ENTRY> hookEntry;

// Global.Handle.functions:
bool EngineCloseHandle(HANDLE myHandle){

	DWORD HandleFlags;

	if(GetHandleInformation(myHandle, &HandleFlags)){
		if(CloseHandle(myHandle)){
			return(true);
		}else{
			return(false);
		}
	}else{
		return(false);
	}
}
// Global.Mapping.functions:
bool MapFileEx(char* szFileName, DWORD ReadOrWrite, LPHANDLE FileHandle, LPDWORD FileSize, LPHANDLE FileMap, LPVOID FileMapVA, DWORD SizeModifier){

	HANDLE hFile = 0;
	DWORD FileAccess = 0;
	DWORD FileMapType = 0;
	DWORD FileMapViewType = 0;
	DWORD mfFileSize = 0;
	HANDLE mfFileMap = 0;
	LPVOID mfFileMapVA = 0;

	if(ReadOrWrite == UE_ACCESS_READ){
		FileAccess = GENERIC_READ;
		FileMapType = 2;
		FileMapViewType = 4;
	}else if(ReadOrWrite == UE_ACCESS_WRITE){
		FileAccess = GENERIC_WRITE;
		FileMapType = 4;
		FileMapViewType = 2;
	}else if(ReadOrWrite == UE_ACCESS_ALL){
		FileAccess = GENERIC_READ+GENERIC_WRITE;
		FileMapType = 4;
		FileMapViewType = 2;
	}else{
		FileAccess = GENERIC_READ+GENERIC_WRITE;
		FileMapType = 4;
		FileMapViewType = 2;
	}

	hFile = CreateFileA(szFileName, FileAccess, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if(hFile != INVALID_HANDLE_VALUE){
		*FileHandle = hFile;
		mfFileSize = GetFileSize(hFile,NULL);
		mfFileSize = mfFileSize + SizeModifier;
		*FileSize = mfFileSize;
		mfFileMap = CreateFileMappingA(hFile, NULL, FileMapType, NULL, mfFileSize, NULL);
		if(mfFileMap != NULL){
			*FileMap = mfFileMap;
			mfFileMapVA = MapViewOfFile(mfFileMap, FileMapViewType, NULL, NULL, NULL);
			if(mfFileMapVA != NULL){
				RtlMoveMemory(FileMapVA, &mfFileMapVA, sizeof ULONG_PTR);
				return(true);
			}
		}
		RtlZeroMemory(FileMapVA, sizeof ULONG_PTR);
		*FileHandle = NULL;
		*FileSize = NULL;
		EngineCloseHandle(hFile);
	}else{
		RtlZeroMemory(FileMapVA, sizeof ULONG_PTR);
	}
	return(false);
}

void UnMapFileEx(HANDLE FileHandle, DWORD FileSize, HANDLE FileMap, ULONG_PTR FileMapVA){

	LPVOID ufFileMapVA = (void*)FileMapVA;

	if(UnmapViewOfFile(ufFileMapVA)){
		EngineCloseHandle(FileMap);
		SetFilePointer(FileHandle,FileSize,NULL,FILE_BEGIN);
		SetEndOfFile(FileHandle);
		EngineCloseHandle(FileHandle);
	}
}
// Global.Engine.functions:
long EngineHashString(char* szStringToHash){

	int i = NULL;
	DWORD HashValue = NULL;

	if(szStringToHash != NULL){
		for(i = 0; i < lstrlenA(szStringToHash); i++){
			HashValue = (((HashValue << 7) | (HashValue >> (32 - 7))) ^ szStringToHash[i]);
		}
	}
	return(HashValue);
}
bool EngineValidateHeader(ULONG_PTR FileMapVA, HANDLE hFileProc, LPVOID ImageBase, PIMAGE_DOS_HEADER DOSHeader, bool IsFile){

	MODULEINFO ModuleInfo;
	DWORD MemorySize = NULL;
	PIMAGE_NT_HEADERS32 PEHeader32;
	IMAGE_NT_HEADERS32 RemotePEHeader32;
	MEMORY_BASIC_INFORMATION MemoryInfo;
	ULONG_PTR NumberOfBytesRW = NULL;

	if(IsFile){
		if(hFileProc == NULL){
			VirtualQueryEx(GetCurrentProcess(), (LPVOID)FileMapVA, &MemoryInfo, sizeof MEMORY_BASIC_INFORMATION);
			VirtualQueryEx(GetCurrentProcess(), MemoryInfo.AllocationBase, &MemoryInfo, sizeof MEMORY_BASIC_INFORMATION);
			MemorySize = (DWORD)((ULONG_PTR)MemoryInfo.AllocationBase + (ULONG_PTR)MemoryInfo.RegionSize - (ULONG_PTR)FileMapVA);
		}else{
			MemorySize = GetFileSize(hFileProc, NULL);
		}
		__try{
			if(DOSHeader->e_magic == 0x5A4D){
				if(DOSHeader->e_lfanew + sizeof IMAGE_DOS_HEADER + sizeof IMAGE_NT_HEADERS64 < MemorySize){
					PEHeader32 = (PIMAGE_NT_HEADERS32)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
					if(PEHeader32->Signature != 0x4550){
						return(false);
					}else{
						return(true);
					}
				}else{
					return(false);
				}
			}else{
				return(false);
			}
		}__except(EXCEPTION_EXECUTE_HANDLER){
			return(false);
		}
	}else{
		RtlZeroMemory(&ModuleInfo, sizeof MODULEINFO);
		GetModuleInformation(hFileProc, (HMODULE)ImageBase, &ModuleInfo, sizeof MODULEINFO);
		__try{
			if(DOSHeader->e_magic == 0x5A4D){
				if(DOSHeader->e_lfanew + sizeof IMAGE_DOS_HEADER + sizeof IMAGE_NT_HEADERS64 < ModuleInfo.SizeOfImage){
					if(ReadProcessMemory(hFileProc, (LPVOID)((ULONG_PTR)ImageBase + DOSHeader->e_lfanew), &RemotePEHeader32, sizeof IMAGE_NT_HEADERS32, &NumberOfBytesRW)){
						PEHeader32 = (PIMAGE_NT_HEADERS32)(&RemotePEHeader32);
						if(PEHeader32->Signature != 0x4550){
							return(false);
						}else{
							return(true);
						}
					}else{
						return(false);
					}
				}else{
					return(false);
				}
			}else{
				return(false);
			}
		}__except(EXCEPTION_EXECUTE_HANDLER){
			return(false);
		}
	}
}
// TitanEngine.Dumper.functions:
long long __stdcall ConvertVAtoFileOffset(ULONG_PTR FileMapVA, ULONG_PTR AddressToConvert, bool ReturnType){

	PIMAGE_DOS_HEADER DOSHeader;
	PIMAGE_NT_HEADERS32 PEHeader32;
	PIMAGE_NT_HEADERS64 PEHeader64;
	PIMAGE_SECTION_HEADER PESections;
	DWORD SectionNumber = 0;
	ULONG_PTR ConvertedAddress = 0;
	ULONG_PTR ConvertAddress = 0;
	BOOL FileIs64;

	if(FileMapVA != NULL){
		DOSHeader = (PIMAGE_DOS_HEADER)FileMapVA;
		if(EngineValidateHeader(FileMapVA, NULL, NULL, DOSHeader, true)){
			PEHeader32 = (PIMAGE_NT_HEADERS32)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			PEHeader64 = (PIMAGE_NT_HEADERS64)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			if(PEHeader32->OptionalHeader.Magic == 0x10B){
				FileIs64 = false;
			}else if(PEHeader32->OptionalHeader.Magic == 0x20B){
				FileIs64 = true;
			}else{
				return(0);
			}
			if(!FileIs64){
				ConvertAddress = (DWORD)((DWORD)AddressToConvert - PEHeader32->OptionalHeader.ImageBase);
				if(ConvertAddress < PEHeader32->OptionalHeader.SectionAlignment){
					ConvertedAddress = ConvertAddress;
				}
				PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PEHeader32 + PEHeader32->FileHeader.SizeOfOptionalHeader + sizeof(IMAGE_FILE_HEADER) + 4);
				SectionNumber = PEHeader32->FileHeader.NumberOfSections;
				__try{
					while(SectionNumber > 0){
						if(PESections->VirtualAddress <= ConvertAddress && ConvertAddress <= PESections->VirtualAddress + PESections->Misc.VirtualSize){
							ConvertedAddress = PESections->PointerToRawData + (ConvertAddress - PESections->VirtualAddress);
						}
						PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PESections + IMAGE_SIZEOF_SECTION_HEADER);
						SectionNumber--;
					}
					if(ReturnType){
						if(ConvertedAddress != NULL){
							ConvertedAddress = ConvertedAddress + FileMapVA;
						}else if(ConvertAddress == NULL){
							ConvertedAddress = FileMapVA;
						}
					}
					return(ConvertedAddress);
				}__except(EXCEPTION_EXECUTE_HANDLER){
					return(0);
				}
			}else{
				ConvertAddress = (DWORD)(AddressToConvert - PEHeader64->OptionalHeader.ImageBase);
				if(ConvertAddress < PEHeader64->OptionalHeader.SectionAlignment){
					ConvertedAddress = ConvertAddress;
				}
				PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PEHeader64 + PEHeader64->FileHeader.SizeOfOptionalHeader + sizeof(IMAGE_FILE_HEADER) + 4);			
				SectionNumber = PEHeader64->FileHeader.NumberOfSections;
				__try{
					while(SectionNumber > 0){
						if(PESections->VirtualAddress <= ConvertAddress && ConvertAddress <= PESections->VirtualAddress + PESections->Misc.VirtualSize){
							ConvertedAddress = PESections->PointerToRawData + (ConvertAddress - PESections->VirtualAddress);
						}
						PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PESections + IMAGE_SIZEOF_SECTION_HEADER);
						SectionNumber--;
					}
					if(ReturnType){
						if(ConvertedAddress != NULL){
							ConvertedAddress = ConvertedAddress + FileMapVA;
						}else if(ConvertAddress == NULL){
							ConvertedAddress = FileMapVA;
						}
					}
					return(ConvertedAddress);
				}__except(EXCEPTION_EXECUTE_HANDLER){
					return(0);
				}
			}
		}else{
			return(0);		
		}
	}
	return(0);
}
long long __stdcall ConvertVAtoFileOffsetEx(ULONG_PTR FileMapVA, DWORD FileSize, ULONG_PTR ImageBase, ULONG_PTR AddressToConvert, bool AddressIsRVA, bool ReturnType){

	PIMAGE_DOS_HEADER DOSHeader;
	PIMAGE_NT_HEADERS32 PEHeader32;
	PIMAGE_NT_HEADERS64 PEHeader64;
	PIMAGE_SECTION_HEADER PESections;
	DWORD SectionNumber = 0;
	ULONG_PTR ConvertedAddress = 0;
	ULONG_PTR ConvertAddress = 0;
	BOOL FileIs64;

	if(FileMapVA != NULL){
		DOSHeader = (PIMAGE_DOS_HEADER)FileMapVA;
		if(EngineValidateHeader(FileMapVA, NULL, NULL, DOSHeader, true)){
			PEHeader32 = (PIMAGE_NT_HEADERS32)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			PEHeader64 = (PIMAGE_NT_HEADERS64)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			if(PEHeader32->OptionalHeader.Magic == 0x10B){
				FileIs64 = false;
			}else if(PEHeader32->OptionalHeader.Magic == 0x20B){
				FileIs64 = true;
			}else{
				return(0);
			}
			if(!FileIs64){
				if(!AddressIsRVA){
					if(ImageBase == NULL){
						ConvertAddress = (DWORD)((DWORD)AddressToConvert - PEHeader32->OptionalHeader.ImageBase);
					}else{
						ConvertAddress = (DWORD)((DWORD)AddressToConvert - ImageBase);
					}
				}else{
					ConvertAddress = (DWORD)AddressToConvert;
				}
				if(ConvertAddress < PEHeader32->OptionalHeader.SectionAlignment){
					ConvertedAddress = ConvertAddress;
				}
				PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PEHeader32 + PEHeader32->FileHeader.SizeOfOptionalHeader + sizeof(IMAGE_FILE_HEADER) + 4);
				SectionNumber = PEHeader32->FileHeader.NumberOfSections;
				__try{
					while(SectionNumber > 0){
						if(PESections->VirtualAddress <= ConvertAddress && ConvertAddress <= PESections->VirtualAddress + PESections->Misc.VirtualSize){
							ConvertedAddress = PESections->PointerToRawData + (ConvertAddress - PESections->VirtualAddress);
						}
						PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PESections + IMAGE_SIZEOF_SECTION_HEADER);
						SectionNumber--;
					}
					if(ReturnType){
						if(ConvertedAddress != NULL){
							ConvertedAddress = ConvertedAddress + FileMapVA;
						}
					}
					if(ReturnType){
						if(ConvertedAddress >= FileMapVA && ConvertedAddress <= FileMapVA + FileSize){
							return((ULONG_PTR)ConvertedAddress);
						}else{
							return(NULL);
						}
					}else{
						if(ConvertedAddress > NULL && ConvertedAddress <= FileSize){
							return((ULONG_PTR)ConvertedAddress);
						}else{
							return(NULL);
						}
					}
				}__except(EXCEPTION_EXECUTE_HANDLER){
					return(NULL);
				}
			}else{
				if(!AddressIsRVA){
					if(ImageBase == NULL){
						ConvertAddress = (DWORD)(AddressToConvert - PEHeader64->OptionalHeader.ImageBase);
					}else{
						ConvertAddress = (DWORD)(AddressToConvert - ImageBase);
					}
				}else{
					ConvertAddress = (DWORD)AddressToConvert;
				}
				if(ConvertAddress < PEHeader64->OptionalHeader.SectionAlignment){
					ConvertedAddress = ConvertAddress;
				}
				PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PEHeader64 + PEHeader64->FileHeader.SizeOfOptionalHeader + sizeof(IMAGE_FILE_HEADER) + 4);			
				SectionNumber = PEHeader64->FileHeader.NumberOfSections;
				__try{
					while(SectionNumber > 0){
						if(PESections->VirtualAddress <= ConvertAddress && ConvertAddress <= PESections->VirtualAddress + PESections->Misc.VirtualSize){
							ConvertedAddress = PESections->PointerToRawData + (ConvertAddress - PESections->VirtualAddress);
						}
						PESections = (PIMAGE_SECTION_HEADER)((ULONG_PTR)PESections + IMAGE_SIZEOF_SECTION_HEADER);
						SectionNumber--;
					}
					if(ReturnType){
						if(ConvertedAddress != NULL){
							ConvertedAddress = ConvertedAddress + FileMapVA;
						}
					}
					if(ReturnType){
						if(ConvertedAddress >= FileMapVA && ConvertedAddress <= FileMapVA + FileSize){
							return((ULONG_PTR)ConvertedAddress);
						}else{
							return(NULL);
						}
					}else{
						if(ConvertedAddress > NULL && ConvertedAddress <= FileSize){
							return((ULONG_PTR)ConvertedAddress);
						}else{
							return(NULL);
						}
					}
				}__except(EXCEPTION_EXECUTE_HANDLER){
					return(NULL);
				}
			}
		}else{
			return(0);		
		}
	}
	return(0);
}
// TitanEngine.Debugger.functions:
long __stdcall StaticLengthDisassemble(LPVOID DisassmAddress){
	
	_DecodeResult DecodingResult;
	_DecodedInst DecodedInstructions[MAX_DECODE_INSTRUCTIONS];	
	unsigned int DecodedInstructionsCount = 0;
#if !defined(_WIN64)
	_DecodeType DecodingType = Decode32Bits;
#else
	_DecodeType DecodingType = Decode64Bits;
#endif
	MEMORY_BASIC_INFORMATION MemInfo;
	DWORD MaxDisassmSize;

	VirtualQueryEx(GetCurrentProcess(), DisassmAddress, &MemInfo, sizeof MEMORY_BASIC_INFORMATION);
	if(MemInfo.State == MEM_COMMIT){
		if((ULONG_PTR)MemInfo.BaseAddress + (ULONG_PTR)MemInfo.RegionSize - (ULONG_PTR)DisassmAddress <= MAXIMUM_INSTRUCTION_SIZE){
			MaxDisassmSize = (DWORD)((ULONG_PTR)MemInfo.BaseAddress + (ULONG_PTR)MemInfo.RegionSize - (ULONG_PTR)DisassmAddress - 1);
			VirtualQueryEx(GetCurrentProcess(), (LPVOID)((ULONG_PTR)DisassmAddress + (ULONG_PTR)MemInfo.RegionSize), &MemInfo, sizeof MEMORY_BASIC_INFORMATION);
			if(MemInfo.State == MEM_COMMIT){
				MaxDisassmSize = MAXIMUM_INSTRUCTION_SIZE;
			}
		}else{
			MaxDisassmSize = MAXIMUM_INSTRUCTION_SIZE;
		}
		DecodingResult = distorm_decode(NULL, (const unsigned char*)DisassmAddress, MaxDisassmSize, DecodingType, DecodedInstructions, MAX_DECODE_INSTRUCTIONS, &DecodedInstructionsCount);
		return(DecodedInstructions[0].size);
	}else{
		return(NULL);
	}
}
long long __stdcall GetContextDataEx(HANDLE hActiveThread, DWORD IndexOfRegister){

	RtlZeroMemory(&DBGContext, sizeof CONTEXT);
	DBGContext.ContextFlags = CONTEXT_ALL | CONTEXT_DEBUG_REGISTERS;
#if defined(_WIN64)	
	GetThreadContext(hActiveThread, &DBGContext);
	if(IndexOfRegister == UE_EAX){
		return((DWORD)DBGContext.Rax);
	}else if(IndexOfRegister == UE_EBX){
		return((DWORD)DBGContext.Rbx);
	}else if(IndexOfRegister == UE_ECX){
		return((DWORD)DBGContext.Rcx);
	}else if(IndexOfRegister == UE_EDX){
		return((DWORD)DBGContext.Rdx);
	}else if(IndexOfRegister == UE_EDI){
		return((DWORD)DBGContext.Rdi);
	}else if(IndexOfRegister == UE_ESI){
		return((DWORD)DBGContext.Rsi);
	}else if(IndexOfRegister == UE_EBP){
		return((DWORD)DBGContext.Rbp);
	}else if(IndexOfRegister == UE_ESP){
		return((DWORD)DBGContext.Rsp);
	}else if(IndexOfRegister == UE_EIP){
		return((DWORD)DBGContext.Rip);
	}else if(IndexOfRegister == UE_EFLAGS){
		return((DWORD)DBGContext.EFlags);
	}else if(IndexOfRegister == UE_RAX){
		return(DBGContext.Rax);
	}else if(IndexOfRegister == UE_RBX){
		return(DBGContext.Rbx);
	}else if(IndexOfRegister == UE_RCX){
		return(DBGContext.Rcx);
	}else if(IndexOfRegister == UE_RDX){
		return(DBGContext.Rdx);
	}else if(IndexOfRegister == UE_RDI){
		return(DBGContext.Rdi);
	}else if(IndexOfRegister == UE_RSI){
		return(DBGContext.Rsi);
	}else if(IndexOfRegister == UE_RBP){
		return(DBGContext.Rbp);
	}else if(IndexOfRegister == UE_RSP){
		return(DBGContext.Rsp);
	}else if(IndexOfRegister == UE_RIP){
		return(DBGContext.Rip);
	}else if(IndexOfRegister == UE_RFLAGS){
		return(DBGContext.EFlags);
	}else if(IndexOfRegister == UE_DR0){
		return(DBGContext.Dr0);
	}else if(IndexOfRegister == UE_DR1){
		return(DBGContext.Dr1);
	}else if(IndexOfRegister == UE_DR2){
		return(DBGContext.Dr2);
	}else if(IndexOfRegister == UE_DR3){
		return(DBGContext.Dr3);
	}else if(IndexOfRegister == UE_DR6){
		return(DBGContext.Dr6);
	}else if(IndexOfRegister == UE_DR7){
		return(DBGContext.Dr7);
	}else if(IndexOfRegister == UE_R8){
		return(DBGContext.R8);
	}else if(IndexOfRegister == UE_R9){
		return(DBGContext.R9);
	}else if(IndexOfRegister == UE_R10){
		return(DBGContext.R10);
	}else if(IndexOfRegister == UE_R11){
		return(DBGContext.R11);
	}else if(IndexOfRegister == UE_R12){
		return(DBGContext.R12);
	}else if(IndexOfRegister == UE_R13){
		return(DBGContext.R13);
	}else if(IndexOfRegister == UE_R14){
		return(DBGContext.R14);
	}else if(IndexOfRegister == UE_R15){
		return(DBGContext.R15);
	}else if(IndexOfRegister == UE_CIP){
		return(DBGContext.Rip);
	}else if(IndexOfRegister == UE_CSP){
		return(DBGContext.Rsp);
	}
#else
	GetThreadContext(hActiveThread, &DBGContext);
	if(IndexOfRegister == UE_EAX){
		return(DBGContext.Eax);
	}else if(IndexOfRegister == UE_EBX){
		return(DBGContext.Ebx);
	}else if(IndexOfRegister == UE_ECX){
		return(DBGContext.Ecx);
	}else if(IndexOfRegister == UE_EDX){
		return(DBGContext.Edx);
	}else if(IndexOfRegister == UE_EDI){
		return(DBGContext.Edi);
	}else if(IndexOfRegister == UE_ESI){
		return(DBGContext.Esi);
	}else if(IndexOfRegister == UE_EBP){
		return(DBGContext.Ebp);
	}else if(IndexOfRegister == UE_ESP){
		return(DBGContext.Esp);
	}else if(IndexOfRegister == UE_EIP){
		return(DBGContext.Eip);
	}else if(IndexOfRegister == UE_EFLAGS){
		return(DBGContext.EFlags);
	}else if(IndexOfRegister == UE_DR0){
		return(DBGContext.Dr0);
	}else if(IndexOfRegister == UE_DR1){
		return(DBGContext.Dr1);
	}else if(IndexOfRegister == UE_DR2){
		return(DBGContext.Dr2);
	}else if(IndexOfRegister == UE_DR3){
		return(DBGContext.Dr3);
	}else if(IndexOfRegister == UE_DR6){
		return(DBGContext.Dr6);
	}else if(IndexOfRegister == UE_DR7){
		return(DBGContext.Dr7);
	}else if(IndexOfRegister == UE_CIP){
		return(DBGContext.Eip);
	}else if(IndexOfRegister == UE_CSP){
		return(DBGContext.Esp);
	}
#endif
	return(-1);
}
long long __stdcall GetJumpDestinationEx(HANDLE hProcess, ULONG_PTR InstructionAddress, bool JustJumps){

	LPVOID ReadMemory;
	MEMORY_BASIC_INFORMATION MemInfo;
	ULONG_PTR ueNumberOfBytesRead = NULL;
	PMEMORY_CMP_HANDLER CompareMemory;
	ULONG_PTR TargetedAddress = NULL;
	DWORD CurrentInstructionSize;
	int ReadMemData = NULL;
	BYTE ReadByteData = NULL;

	if(hProcess != NULL){
		VirtualQueryEx(hProcess, (LPVOID)InstructionAddress, &MemInfo, sizeof MEMORY_BASIC_INFORMATION);
		if(MemInfo.RegionSize > NULL){
			ReadMemory = VirtualAlloc(NULL, MAXIMUM_INSTRUCTION_SIZE, MEM_COMMIT, PAGE_READWRITE);
			if(ReadProcessMemory(hProcess, (LPVOID)InstructionAddress, ReadMemory, MAXIMUM_INSTRUCTION_SIZE, &ueNumberOfBytesRead)){
				CompareMemory = (PMEMORY_CMP_HANDLER)ReadMemory;
				CurrentInstructionSize = StaticLengthDisassemble(ReadMemory);
				if(CompareMemory->DataByte[0] == 0xE9 && CurrentInstructionSize == 5){
					RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)ReadMemory + 1), 4);
					TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] == 0xEB && CurrentInstructionSize == 2){
					RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)ReadMemory + 1), 1);
					if(ReadByteData > 0x7F){
						ReadByteData = 0xFF - ReadByteData;
						ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
					}else{
						ReadMemData = ReadByteData;
					}
					TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] == 0xE3 && CurrentInstructionSize == 2){
					RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)ReadMemory + 1), 1);
					if(ReadByteData > 0x7F){
						ReadByteData = 0xFF - ReadByteData;
						ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
					}else{
						ReadMemData = ReadByteData;
					}
					TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] >= 0x71 && CompareMemory->DataByte[0] <= 0x7F && CurrentInstructionSize == 2){
					RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)ReadMemory + 1), 1);
					if(ReadByteData > 0x7F){
						ReadByteData = 0xFF - ReadByteData;
						ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
					}
					TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] >= 0xE0 && CompareMemory->DataByte[0] <= 0xE2 && CurrentInstructionSize == 2){
					RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)ReadMemory + 1), 1);
					if(ReadByteData > 0x7F){
						ReadByteData = 0xFF - ReadByteData;
						ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
					}else{
						ReadMemData = ReadByteData;
					}
					TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] == 0x0F && CompareMemory->DataByte[1] >= 0x81 && CompareMemory->DataByte[1] <= 0x8F && CurrentInstructionSize == 6){
					RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)ReadMemory + 2), 4);
					TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] == 0x0F && CompareMemory->DataByte[1] >= 0x81 && CompareMemory->DataByte[1] <= 0x8F && CurrentInstructionSize == 4){
					RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)ReadMemory + 2), 2);
					TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] == 0xE8 && CurrentInstructionSize == 5 && JustJumps == false){
					RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)ReadMemory + 1), 4);
					TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
				}else if(CompareMemory->DataByte[0] == 0xFF && CompareMemory->DataByte[1] == 0x25 && CurrentInstructionSize == 6 && JustJumps == false){
					RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)ReadMemory + 2), 4);
					TargetedAddress = ReadMemData;
					if(sizeof HANDLE == 8){
						TargetedAddress = TargetedAddress + InstructionAddress;
					}
				}else if(CompareMemory->DataByte[0] == 0xFF && CompareMemory->DataByte[1] == 0x15 && CurrentInstructionSize == 6 && JustJumps == false){
					RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)ReadMemory + 2), 4);
					TargetedAddress = ReadMemData;
					if(sizeof HANDLE == 8){
						TargetedAddress = TargetedAddress + InstructionAddress;
					}
				}
			}
			VirtualFree(ReadMemory, NULL, MEM_RELEASE);
			return((ULONG_PTR)TargetedAddress);
		}
		return(NULL);
	}else{
		CompareMemory = (PMEMORY_CMP_HANDLER)InstructionAddress;
		CurrentInstructionSize = StaticLengthDisassemble((LPVOID)InstructionAddress);
		if(CompareMemory->DataByte[0] == 0xE9 && CurrentInstructionSize == 5){
			RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)InstructionAddress + 1), 4);
			TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] == 0xEB && CurrentInstructionSize == 2){
			RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)InstructionAddress + 1), 1);
			if(ReadByteData > 0x7F){
				ReadByteData = 0xFF - ReadByteData;
				ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
			}else{
				ReadMemData = ReadByteData;
			}
			TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] == 0xE3 && CurrentInstructionSize == 2){
			RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)InstructionAddress + 1), 1);
			if(ReadByteData > 0x7F){
				ReadByteData = 0xFF - ReadByteData;
				ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
			}else{
				ReadMemData = ReadByteData;
			}
			TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] >= 0x71 && CompareMemory->DataByte[0] <= 0x7F && CurrentInstructionSize == 2){
			RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)InstructionAddress + 1), 1);
			if(ReadByteData > 0x7F){
				ReadByteData = 0xFF - ReadByteData;
				ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
			}
			TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] >= 0xE0 && CompareMemory->DataByte[0] <= 0xE2 && CurrentInstructionSize == 2){
			RtlMoveMemory(&ReadByteData, (LPVOID)((ULONG_PTR)InstructionAddress + 1), 1);
			if(ReadByteData > 0x7F){
				ReadByteData = 0xFF - ReadByteData;
				ReadMemData = NULL - ReadByteData - CurrentInstructionSize + 1;
			}else{
				ReadMemData = ReadByteData;
			}
			TargetedAddress = InstructionAddress + ReadMemData + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] == 0x0F && CompareMemory->DataByte[1] >= 0x81 && CompareMemory->DataByte[1] <= 0x8F && CurrentInstructionSize == 6){
			RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)InstructionAddress + 2), 4);
			TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] == 0x0F && CompareMemory->DataByte[1] >= 0x81 && CompareMemory->DataByte[1] <= 0x8F && CurrentInstructionSize == 4){
			RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)InstructionAddress + 2), 2);
			TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] == 0xE8 && CurrentInstructionSize == 5 && JustJumps == false){
			RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)InstructionAddress + 1), 4);
			TargetedAddress = ReadMemData + InstructionAddress + CurrentInstructionSize;
		}else if(CompareMemory->DataByte[0] == 0xFF && CompareMemory->DataByte[1] == 0x25 && CurrentInstructionSize == 6 && JustJumps == false){
			RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)InstructionAddress + 2), 4);
			TargetedAddress = ReadMemData;
			if(sizeof HANDLE == 8){
				TargetedAddress = TargetedAddress + InstructionAddress;
			}
		}else if(CompareMemory->DataByte[0] == 0xFF && CompareMemory->DataByte[1] == 0x15 && CurrentInstructionSize == 6 && JustJumps == false){
			RtlMoveMemory(&ReadMemData, (LPVOID)((ULONG_PTR)InstructionAddress + 2), 4);
			TargetedAddress = ReadMemData;
			if(sizeof HANDLE == 8){
				TargetedAddress = TargetedAddress + InstructionAddress;
			}
		}
		return((ULONG_PTR)TargetedAddress);
	}
	return(NULL);
}
long long __stdcall GetJumpDestination(HANDLE hProcess, ULONG_PTR InstructionAddress){
	return((ULONG_PTR)GetJumpDestinationEx(hProcess, InstructionAddress, false));
}
// TitanEngine.Threader.functions:
bool __stdcall ThreaderImportRunningThreadData(DWORD ProcessId){

	HANDLE hSnapShot;
	THREADENTRY32 ThreadEntry = {};
	PTHREAD_ITEM_DATA hListThreadPtr = NULL;

	if(dbgProcessInformation.hProcess == NULL && ProcessId != NULL){
		if(hListThread == NULL){
			hListThread = VirtualAlloc(NULL, MAX_DEBUG_DATA * sizeof THREAD_ITEM_DATA, MEM_COMMIT, PAGE_READWRITE);
		}
		ThreadEntry.dwSize = sizeof THREADENTRY32;
		hListThreadPtr = (PTHREAD_ITEM_DATA)hListThread;
		hSnapShot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, ProcessId);
		if(hSnapShot != INVALID_HANDLE_VALUE){
			if(Thread32First(hSnapShot, &ThreadEntry)){
				do{
					if(ThreadEntry.th32OwnerProcessID == ProcessId){
						hListThreadPtr->dwThreadId = ThreadEntry.th32ThreadID;
						hListThreadPtr->hThread = OpenThread(THREAD_GET_CONTEXT+THREAD_SET_CONTEXT+THREAD_QUERY_INFORMATION+THREAD_SUSPEND_RESUME, false, hListThreadPtr->dwThreadId);
						hListThreadPtr = (PTHREAD_ITEM_DATA)((ULONG_PTR)hListThreadPtr + sizeof THREAD_ITEM_DATA);
					}
				}while(Thread32Next(hSnapShot, &ThreadEntry));
			}
			EngineCloseHandle(hSnapShot);
			return(true);
		}
	}
	return(false);
}
// TitanEngine.Relocater.functions:
bool __stdcall RelocaterRelocateMemoryBlock(ULONG_PTR FileMapVA, ULONG_PTR MemoryLocation, void* RelocateMemory, DWORD RelocateMemorySize, ULONG_PTR CurrentLoadedBase, ULONG_PTR RelocateBase){

	BOOL FileIs64;
	DWORD RelocSize;
	ULONG_PTR RelocData;
	PIMAGE_DOS_HEADER DOSHeader;
	PIMAGE_NT_HEADERS32 PEHeader32;
	PIMAGE_NT_HEADERS64 PEHeader64;
	DWORD CompareDummy = NULL;
	DWORD RelocDelta = NULL;
	DWORD RelocDeltaSize = NULL;
	WORD RelocAddressData = NULL;
	ULONG_PTR RelocWriteAddress = NULL;
	ULONG_PTR RelocWriteData = NULL;
	DWORD64 RelocWriteData64 = NULL;

	DOSHeader = (PIMAGE_DOS_HEADER)FileMapVA;
	MemoryLocation = MemoryLocation - CurrentLoadedBase;
	if(EngineValidateHeader(FileMapVA, NULL, NULL, DOSHeader, true)){
		PEHeader32 = (PIMAGE_NT_HEADERS32)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
		PEHeader64 = (PIMAGE_NT_HEADERS64)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
		if(PEHeader32->OptionalHeader.Magic == 0x10B){
			FileIs64 = false;
		}else if(PEHeader32->OptionalHeader.Magic == 0x20B){
			FileIs64 = true;
		}else{
			return(false);
		}
		if(!FileIs64){
			if(PEHeader32->OptionalHeader.ImageBase == (DWORD)RelocateBase){
				return(true);				
			}
			RelocData = (ULONG_PTR)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)(PEHeader32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].VirtualAddress + PEHeader32->OptionalHeader.ImageBase), true);
			RelocSize = PEHeader32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].Size;
		}else{
			if((ULONG_PTR)PEHeader64->OptionalHeader.ImageBase == RelocateBase){
				return(true);				
			}
			RelocData = (ULONG_PTR)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)(PEHeader64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].VirtualAddress + PEHeader64->OptionalHeader.ImageBase), true);
			RelocSize = PEHeader64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].Size;
		}
		__try{
			while(memcmp((LPVOID)RelocData, &CompareDummy, 4)){
				RtlMoveMemory(&RelocDelta, (LPVOID)RelocData, 4);
				RtlMoveMemory(&RelocDeltaSize, (LPVOID)((ULONG_PTR)RelocData + 4), 4);
				RelocDeltaSize = RelocDeltaSize - 8;
				RelocData = RelocData + 8;
				while(RelocDeltaSize > NULL){
					RtlMoveMemory(&RelocAddressData, (LPVOID)RelocData, 2);
					if(RelocAddressData != NULL){
						if(RelocAddressData & 0x8000){
							RelocAddressData = RelocAddressData ^ 0x8000;
							if(RelocAddressData >= MemoryLocation && RelocAddressData < MemoryLocation + RelocateMemorySize){
								RelocWriteAddress = (ULONG_PTR)(RelocAddressData + RelocDelta - MemoryLocation + (ULONG_PTR)RelocateMemory);
								RtlMoveMemory(&RelocWriteData64, (LPVOID)RelocWriteAddress, 8);
								RelocWriteData64 = RelocWriteData64 - (DWORD64)PEHeader64->OptionalHeader.ImageBase + (DWORD64)RelocateBase;
								RtlMoveMemory((LPVOID)RelocWriteAddress, &RelocWriteData64, 8);
							}
						}else if(RelocAddressData & 0x3000){
							RelocAddressData = RelocAddressData ^ 0x3000;
							if(RelocAddressData >= MemoryLocation && RelocAddressData < MemoryLocation + RelocateMemorySize){
								RelocWriteAddress = (ULONG_PTR)(RelocAddressData + RelocDelta - MemoryLocation + (ULONG_PTR)RelocateMemory);
								RtlMoveMemory(&RelocWriteData, (LPVOID)RelocWriteAddress, 4);
								RelocWriteData = RelocWriteData - PEHeader32->OptionalHeader.ImageBase + RelocateBase;
								RtlMoveMemory((LPVOID)RelocWriteAddress, &RelocWriteData, 4);
							}
						}
					}
					RelocDeltaSize = RelocDeltaSize - 2;
					RelocData = RelocData + 2;
				}
			}
			return(true);
		}__except(EXCEPTION_EXECUTE_HANDLER){
			return(false);
		}
	}else{
		return(false);
	}
	return(false);
}
// Internal.Engine.Hook.functions:
bool ProcessHookScanAddNewHook(PHOOK_ENTRY HookDetails, void* ptrOriginalInstructions, PLIBRARY_ITEM_DATA ModuleInformation, DWORD SizeOfImage){

	HOOK_ENTRY MyhookEntry = {};

	RtlMoveMemory(&MyhookEntry, HookDetails, sizeof HOOK_ENTRY);
	hookEntry.push_back(MyhookEntry);
	return(true);
}
// Global.Engine.Hook.functions:
 bool __stdcall HooksSafeTransitionEx(LPVOID HookAddressArray, int NumberOfHooks, bool TransitionStart){

	int i;
	ULONG_PTR CurrentIP;
	ULONG_PTR HookAddress;
	PTHREAD_ITEM_DATA hListThreadPtr = (PTHREAD_ITEM_DATA)hListThread;
	PMEMORY_COMPARE_HANDLER myHookAddressArray;

	if(dbgProcessInformation.hProcess == NULL){
		if(!TransitionStart || ThreaderImportRunningThreadData(GetCurrentProcessId())){
			hListThreadPtr = (PTHREAD_ITEM_DATA)hListThread;
			if(hListThreadPtr != NULL){
				while(hListThreadPtr->hThread != NULL){
					if(hListThreadPtr->hThread != INVALID_HANDLE_VALUE){
						if(TransitionStart){
							if(hListThreadPtr->dwThreadId != GetCurrentThreadId()){
								SuspendThread(hListThreadPtr->hThread);
								CurrentIP = (ULONG_PTR)GetContextDataEx(hListThreadPtr->hThread, UE_CIP);
								myHookAddressArray = (PMEMORY_COMPARE_HANDLER)HookAddressArray;
								for(i = 0; i < NumberOfHooks; i++){
									#if defined (_WIN64)
										HookAddress = (ULONG_PTR)myHookAddressArray->Array.qwArrayEntry[0];
										myHookAddressArray = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)myHookAddressArray + sizeof ULONG_PTR);
									#else
										HookAddress = (ULONG_PTR)myHookAddressArray->Array.dwArrayEntry[0];
										myHookAddressArray = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)myHookAddressArray + sizeof ULONG_PTR);
									#endif
									while(CurrentIP >= (ULONG_PTR)HookAddress && CurrentIP <= (ULONG_PTR)HookAddress + 5){
										ResumeThread(hListThreadPtr->hThread);
										Sleep(5);
										SuspendThread(hListThreadPtr->hThread);
										CurrentIP = (ULONG_PTR)GetContextDataEx(hListThreadPtr->hThread, UE_CIP);
										i = 0;
									}
								}
							}
						}else{
							ResumeThread(hListThreadPtr->hThread);
							EngineCloseHandle(hListThreadPtr->hThread);
						}
					}
					hListThreadPtr = (PTHREAD_ITEM_DATA)((ULONG_PTR)hListThreadPtr + sizeof THREAD_ITEM_DATA);
				}
				if(!TransitionStart){
					VirtualFree(hListThread, NULL, MEM_RELEASE);
					hListThread = NULL;
				}
				return(true);
			}			
		}else{
			return(false);
		}
	}
	return(false);
}
 bool __stdcall HooksSafeTransition(LPVOID HookAddress, bool TransitionStart){

	void* aHookAddress[1];
	aHookAddress[0] = HookAddress;

	return(HooksSafeTransitionEx(&aHookAddress[0], sizeof aHookAddress, TransitionStart));
}
 bool __stdcall HooksIsAddressRedirected(LPVOID HookAddress){

	for(unsigned int i = 0; i < hookEntry.size(); i++){
		if(hookEntry[i].HookAddress == HookAddress && hookEntry[i].IATHook == false && hookEntry[i].HookIsEnabled == true){
			return(true);
		}
	}
	return(false);
}
 void* __stdcall HooksGetTrampolineAddress(LPVOID HookAddress){

	for(unsigned int i = 0; i < hookEntry.size(); i++){
		if(hookEntry[i].HookAddress == HookAddress){
			return(hookEntry[i].PatchedEntry);
		}
	}
	return(NULL);
}
 void* __stdcall HooksGetHookEntryDetails(LPVOID HookAddress){

	for(unsigned int i = 0; i < hookEntry.size(); i++){
		if(hookEntry[i].HookAddress == HookAddress){
			return(&hookEntry[i]);
		}
	}
	return(NULL);
}
 bool __stdcall HooksInsertNewRedirection(LPVOID HookAddress, LPVOID RedirectTo, int HookType){

#if !defined(_WIN64)
	int j;
	unsigned int i;
#endif
	HOOK_ENTRY myHook = {};
	DWORD CalculatedRealingJump;
	ULONG_PTR x64CalculatedRealingJump;
	ULONG_PTR RealignAddressTarget;
	int ProcessedBufferSize = NULL;
	int CurrentInstructionSize = NULL;
	PMEMORY_COMPARE_HANDLER WriteMemory = (PMEMORY_COMPARE_HANDLER)CwpBuffPatchedEntry;
	PMEMORY_COMPARE_HANDLER CompareMemory;
#if !defined(_WIN64)
	PMEMORY_COMPARE_HANDLER RelocateMemory;
#endif
	void* cHookAddress = HookAddress;
	DWORD OldProtect = PAGE_READONLY;
	void* TempBuffPatchedEntry;
	bool returnData;

	x64CalculatedRealingJump = NULL;
	if(buffPatchedEntry == NULL || (ULONG_PTR)CwpBuffPatchedEntry - (ULONG_PTR)buffPatchedEntry + TEE_MAXIMUM_HOOK_SIZE > buffPatchedEntrySize){
		buffPatchedEntrySize = buffPatchedEntrySize + 0x1000;
		CwpBuffPatchedEntry = (void*)((ULONG_PTR)CwpBuffPatchedEntry - (ULONG_PTR)buffPatchedEntry);
		TempBuffPatchedEntry = VirtualAlloc(NULL, buffPatchedEntrySize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
		if(TempBuffPatchedEntry != NULL){
			if(hookEntry.size() > NULL){
				RtlMoveMemory(TempBuffPatchedEntry, buffPatchedEntry, (ULONG_PTR)CwpBuffPatchedEntry);
			}
	#if !defined(_WIN64)
			for(i = 0; i < hookEntry.size(); i++){
				hookEntry[i].PatchedEntry = (void*)((ULONG_PTR)hookEntry[i].PatchedEntry - (ULONG_PTR)buffPatchedEntry + (ULONG_PTR)TempBuffPatchedEntry);
				CalculatedRealingJump = (DWORD)((ULONG_PTR)hookEntry[i].PatchedEntry - (ULONG_PTR)hookEntry[i].HookAddress - 5);
				RtlMoveMemory(&hookEntry[i].HookBytes[1], &CalculatedRealingJump, 4);
				if(hookEntry[i].RelocationCount > NULL){
					for(j = 0; j < hookEntry[i].RelocationCount; j++){
						CompareMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)buffPatchedEntry + hookEntry[i].RelocationInfo[j]);
						RelocateMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)TempBuffPatchedEntry + hookEntry[i].RelocationInfo[j]);
						CurrentInstructionSize = StaticLengthDisassemble((void*)CompareMemory);
						RealignAddressTarget = (ULONG_PTR)GetJumpDestination(GetCurrentProcess(), (ULONG_PTR)CompareMemory);
						if(RealignAddressTarget != NULL){	
							if(CompareMemory->Array.bArrayEntry[0] == 0xE9 && CurrentInstructionSize == 5){
								CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)RelocateMemory - CurrentInstructionSize);
								RtlMoveMemory(&RelocateMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
							}else if(CompareMemory->Array.bArrayEntry[0] >= 0x70 && CompareMemory->Array.bArrayEntry[0] <= 0x7F && CurrentInstructionSize == 2){
								CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)RelocateMemory - CurrentInstructionSize);
								RtlMoveMemory(&RelocateMemory->Array.bArrayEntry[2], &CalculatedRealingJump, sizeof CalculatedRealingJump);
							}else if(CompareMemory->Array.bArrayEntry[0] == 0x0F && CompareMemory->Array.bArrayEntry[1] >= 0x80 && CompareMemory->Array.bArrayEntry[1] <= 0x8F && CurrentInstructionSize == 6){
								CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)RelocateMemory - CurrentInstructionSize);
								RtlMoveMemory(&RelocateMemory->Array.bArrayEntry[2], &CalculatedRealingJump, sizeof CalculatedRealingJump);
							}else if(CompareMemory->Array.bArrayEntry[0] == 0xE8 && CurrentInstructionSize == 5){
								CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)RelocateMemory - CurrentInstructionSize);
								RtlMoveMemory(&RelocateMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
							}
						}
					}
				}
			}
	#endif
			if(hookEntry.size() > NULL){
				VirtualFree(buffPatchedEntry, NULL, MEM_RELEASE);
			}
			CwpBuffPatchedEntry = (void*)((ULONG_PTR)CwpBuffPatchedEntry + (ULONG_PTR)TempBuffPatchedEntry);
			WriteMemory = (PMEMORY_COMPARE_HANDLER)CwpBuffPatchedEntry;
			buffPatchedEntry = TempBuffPatchedEntry;
		}
	}
	while(ProcessedBufferSize < TEE_MAXIMUM_HOOK_INSERT_SIZE){
		CompareMemory = (PMEMORY_COMPARE_HANDLER)cHookAddress;
		CurrentInstructionSize = StaticLengthDisassemble(cHookAddress);
		RealignAddressTarget = (ULONG_PTR)GetJumpDestination(GetCurrentProcess(), (ULONG_PTR)cHookAddress);
		if(RealignAddressTarget != NULL){
			if(CompareMemory->Array.bArrayEntry[0] == 0xE9 && CurrentInstructionSize == 5){
				if(cHookAddress == HookAddress){
					if(HooksIsAddressRedirected(HookAddress)){
						if(HooksRemoveRedirection(HookAddress, false)){
							returnData = HooksInsertNewRedirection(HookAddress, RedirectTo, HookType);
							if(returnData){
								return(true);
							}else{
								return(false);
							}
						}
					}
				}
				CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)WriteMemory - CurrentInstructionSize);
				WriteMemory->Array.bArrayEntry[0] = 0xE9;
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
				myHook.RelocationInfo[myHook.RelocationCount] = (DWORD)((ULONG_PTR)WriteMemory - (ULONG_PTR)buffPatchedEntry);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + CurrentInstructionSize);
				myHook.RelocationCount++;
			}else if(CompareMemory->Array.bArrayEntry[0] == 0xEB && CurrentInstructionSize == 2){
				CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)WriteMemory - 5);
				WriteMemory->Array.bArrayEntry[0] = 0xE9;
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
				myHook.RelocationInfo[myHook.RelocationCount] = (DWORD)((ULONG_PTR)WriteMemory - (ULONG_PTR)buffPatchedEntry);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + 5);
				myHook.RelocationCount++;
			}else if(CompareMemory->Array.bArrayEntry[0] >= 0x70 && CompareMemory->Array.bArrayEntry[0] <= 0x7F && CurrentInstructionSize == 2){
	#if !defined(_WIN64)
				CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)WriteMemory - 6);
				WriteMemory->Array.bArrayEntry[0] = 0x0F;
				WriteMemory->Array.bArrayEntry[1] = CompareMemory->Array.bArrayEntry[0] + 0x10;
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[2], &CalculatedRealingJump, sizeof CalculatedRealingJump);
				myHook.RelocationInfo[myHook.RelocationCount] = (DWORD)((ULONG_PTR)WriteMemory - (ULONG_PTR)buffPatchedEntry);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + 6);
				myHook.RelocationCount++;
	#else
				x64CalculatedRealingJump = RealignAddressTarget;
				WriteMemory->Array.bArrayEntry[0] = CompareMemory->Array.bArrayEntry[0];
				WriteMemory->Array.bArrayEntry[1] = 0x02;
				WriteMemory->Array.bArrayEntry[2] = 0xEB;
				WriteMemory->Array.bArrayEntry[3] = 0x0E;
				WriteMemory->Array.bArrayEntry[4] = 0xFF;
				WriteMemory->Array.bArrayEntry[5] = 0x25;
				RtlZeroMemory(&WriteMemory->Array.bArrayEntry[6], 4);
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[10], &x64CalculatedRealingJump, sizeof x64CalculatedRealingJump);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + 18);
	#endif
			}else if(CompareMemory->Array.bArrayEntry[0] == 0x0F && CompareMemory->Array.bArrayEntry[1] >= 0x80 && CompareMemory->Array.bArrayEntry[1] <= 0x8F && CurrentInstructionSize == 6){
	#if !defined(_WIN64)
				CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)WriteMemory - CurrentInstructionSize);
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[0], &CompareMemory->Array.bArrayEntry[0], 2);
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[2], &CalculatedRealingJump, sizeof CalculatedRealingJump);
				myHook.RelocationInfo[myHook.RelocationCount] = (DWORD)((ULONG_PTR)WriteMemory - (ULONG_PTR)buffPatchedEntry);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + CurrentInstructionSize);
				myHook.RelocationCount++;
	#else
				x64CalculatedRealingJump = RealignAddressTarget;
				WriteMemory->Array.bArrayEntry[0] = CompareMemory->Array.bArrayEntry[0];
				WriteMemory->Array.bArrayEntry[1] = CompareMemory->Array.bArrayEntry[1];
				WriteMemory->Array.bArrayEntry[2] = 0x02;
				WriteMemory->Array.bArrayEntry[3] = 0x00;
				WriteMemory->Array.bArrayEntry[4] = 0x00;
				WriteMemory->Array.bArrayEntry[5] = 0x00;
				WriteMemory->Array.bArrayEntry[6] = 0xEB;
				WriteMemory->Array.bArrayEntry[7] = 0x0E;
				WriteMemory->Array.bArrayEntry[8] = 0xFF;
				WriteMemory->Array.bArrayEntry[9] = 0x25;
				RtlZeroMemory(&WriteMemory->Array.bArrayEntry[10], 4);
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[14], &x64CalculatedRealingJump, sizeof x64CalculatedRealingJump);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + 22);
	#endif
			}else if(CompareMemory->Array.bArrayEntry[0] == 0xE8 && CurrentInstructionSize == 5){
				CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)WriteMemory - CurrentInstructionSize);
				WriteMemory->Array.bArrayEntry[0] = 0xE8;
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
				myHook.RelocationInfo[myHook.RelocationCount] = (DWORD)((ULONG_PTR)WriteMemory - (ULONG_PTR)buffPatchedEntry);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + CurrentInstructionSize);
				myHook.RelocationCount++;
	#if defined(_WIN64)
			}else if(CompareMemory->Array.bArrayEntry[0] == 0xFF && (CompareMemory->Array.bArrayEntry[1] == 0x15 || CompareMemory->Array.bArrayEntry[1] == 0x25) && CurrentInstructionSize == 6){
				CalculatedRealingJump = (DWORD)((ULONG_PTR)RealignAddressTarget - (ULONG_PTR)WriteMemory - CurrentInstructionSize);
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[0], &CompareMemory->Array.bArrayEntry[0], 2);
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[2], &CalculatedRealingJump, sizeof CalculatedRealingJump);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + CurrentInstructionSize);
	#endif
			}else{
				RtlMoveMemory(&WriteMemory->Array.bArrayEntry[0], cHookAddress, CurrentInstructionSize);
				WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + CurrentInstructionSize);
			}
		}else{
			RtlMoveMemory(&WriteMemory->Array.bArrayEntry[0], cHookAddress, CurrentInstructionSize);
			WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + CurrentInstructionSize);
		}
		cHookAddress = (void*)((ULONG_PTR)cHookAddress + CurrentInstructionSize);
		ProcessedBufferSize = ProcessedBufferSize + CurrentInstructionSize;
	}
	if(ProcessedBufferSize >= TEE_MAXIMUM_HOOK_INSERT_SIZE){
		WriteMemory->Array.bArrayEntry[0] = 0xFF;
		WriteMemory->Array.bArrayEntry[1] = 0x25;
	#if !defined(_WIN64)
			CalculatedRealingJump = (DWORD)((ULONG_PTR)WriteMemory + 6);
	#else
			CalculatedRealingJump = NULL;
	#endif
		RtlMoveMemory(&WriteMemory->Array.bArrayEntry[2], &CalculatedRealingJump, sizeof CalculatedRealingJump);
		RtlMoveMemory(&WriteMemory->Array.bArrayEntry[6], &cHookAddress, sizeof CalculatedRealingJump);
		WriteMemory = (PMEMORY_COMPARE_HANDLER)((ULONG_PTR)WriteMemory + 6 + sizeof ULONG_PTR);
		myHook.HookIsEnabled = true;
		myHook.HookType = (BYTE)HookType;
		myHook.HookAddress = HookAddress;
		myHook.RedirectionAddress = RedirectTo;
		myHook.PatchedEntry = CwpBuffPatchedEntry;
		myHook.HookSize = TEE_MAXIMUM_HOOK_SIZE;
		RtlMoveMemory(&myHook.OriginalBytes[0], HookAddress, TEE_MAXIMUM_HOOK_SIZE);
		CalculatedRealingJump = (DWORD)((ULONG_PTR)RedirectTo - (ULONG_PTR)HookAddress);
		CwpBuffPatchedEntry = (void*)((ULONG_PTR)WriteMemory);
		WriteMemory = (PMEMORY_COMPARE_HANDLER)HookAddress;
		if(HookType == TEE_HOOK_NRM_JUMP){
			#if !defined(_WIN64)
				CalculatedRealingJump = CalculatedRealingJump - 5;
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					WriteMemory->Array.bArrayEntry[0] = 0xE9;
					RtlMoveMemory(&WriteMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
					RtlMoveMemory(&myHook.HookBytes[0], HookAddress, TEE_MAXIMUM_HOOK_SIZE);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.push_back(myHook);
					return(true);
				}
			#else
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					WriteMemory->Array.bArrayEntry[0] = 0xFF;
					WriteMemory->Array.bArrayEntry[1] = 0x25;
					RtlZeroMemory(&WriteMemory->Array.bArrayEntry[2], 4);
					RtlMoveMemory(&WriteMemory->Array.bArrayEntry[6], &RedirectTo, sizeof RedirectTo);
					RtlMoveMemory(&myHook.HookBytes[0], HookAddress, TEE_MAXIMUM_HOOK_SIZE);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.push_back(myHook);
					return(true);
				}
			#endif
		}else if(HookType == TEE_HOOK_NRM_CALL){
			#if !defined(_WIN64)
				CalculatedRealingJump = CalculatedRealingJump - 5;
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					WriteMemory->Array.bArrayEntry[0] = 0xE8;
					RtlMoveMemory(&WriteMemory->Array.bArrayEntry[1], &CalculatedRealingJump, sizeof CalculatedRealingJump);
					RtlMoveMemory(&myHook.HookBytes[0], HookAddress, TEE_MAXIMUM_HOOK_SIZE);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.push_back(myHook);
					return(true);
				}
			#else
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					WriteMemory->Array.bArrayEntry[0] = 0xFF;
					WriteMemory->Array.bArrayEntry[1] = 0x15;
					RtlZeroMemory(&WriteMemory->Array.bArrayEntry[2], 4);
					RtlMoveMemory(&WriteMemory->Array.bArrayEntry[6], &RedirectTo, sizeof RedirectTo);
					RtlMoveMemory(&myHook.HookBytes[0], HookAddress, TEE_MAXIMUM_HOOK_SIZE);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.push_back(myHook);
					return(true);
				}
			#endif
		}
	}
	return(false);
}
 bool __stdcall HooksInsertNewIATRedirectionEx(ULONG_PTR FileMapVA, ULONG_PTR LoadedModuleBase, char* szHookFunction, LPVOID RedirectTo){

	PIMAGE_DOS_HEADER DOSHeader;
	PIMAGE_NT_HEADERS32 PEHeader32;
	PIMAGE_NT_HEADERS64 PEHeader64;
	PIMAGE_IMPORT_DESCRIPTOR ImportIID;
	PIMAGE_THUNK_DATA32 ThunkData32;
	PIMAGE_THUNK_DATA64 ThunkData64;
	DWORD OldProtect = PAGE_READONLY;
	ULONG_PTR CurrentThunk;
	HOOK_ENTRY myHook = {};
	BOOL FileIs64;

	if(FileMapVA != NULL && LoadedModuleBase != NULL){
		myHook.IATHook = true;
		myHook.HookIsEnabled = true;
		myHook.HookType = TEE_HOOK_IAT;
		myHook.HookSize = sizeof ULONG_PTR;
		myHook.RedirectionAddress = RedirectTo;
		myHook.IATHookModuleBase = (void*)LoadedModuleBase;
		myHook.IATHookNameHash = EngineHashString(szHookFunction);
		DOSHeader = (PIMAGE_DOS_HEADER)FileMapVA;
		if(EngineValidateHeader(FileMapVA, NULL, NULL, DOSHeader, true)){
			PEHeader32 = (PIMAGE_NT_HEADERS32)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			PEHeader64 = (PIMAGE_NT_HEADERS64)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			if(PEHeader32->OptionalHeader.Magic == 0x10B){
				FileIs64 = false;
			}else if(PEHeader32->OptionalHeader.Magic == 0x20B){
				FileIs64 = true;
			}else{
				return(false);
			}
			if(!FileIs64){
				if(PEHeader32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress != NULL){
					ImportIID = (PIMAGE_IMPORT_DESCRIPTOR)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)(PEHeader32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress + PEHeader32->OptionalHeader.ImageBase), true);
					__try{
						while(ImportIID->FirstThunk != NULL){
							ThunkData32 = (PIMAGE_THUNK_DATA32)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)((ULONG_PTR)ImportIID->FirstThunk + PEHeader32->OptionalHeader.ImageBase), true);
							CurrentThunk = (ULONG_PTR)ImportIID->FirstThunk;
							while(ThunkData32->u1.AddressOfData != NULL){
								if(!(ThunkData32->u1.Ordinal & IMAGE_ORDINAL_FLAG32)){
									if(lstrcmpiA((char*)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)((ULONG_PTR)ThunkData32->u1.AddressOfData + 2 + PEHeader32->OptionalHeader.ImageBase), true), szHookFunction) == NULL){
										myHook.HookAddress = (void*)(CurrentThunk + LoadedModuleBase);
										if(VirtualProtect(myHook.HookAddress, myHook.HookSize, PAGE_EXECUTE_READWRITE, &OldProtect)){
											RtlMoveMemory(&myHook.OriginalBytes[0], myHook.HookAddress, myHook.HookSize);
											RtlMoveMemory(&myHook.HookBytes[0], &myHook.RedirectionAddress, myHook.HookSize);
											RtlMoveMemory(myHook.HookAddress, &myHook.RedirectionAddress, myHook.HookSize);
											VirtualProtect(myHook.HookAddress, myHook.HookSize, OldProtect, &OldProtect);
										}
										hookEntry.push_back(myHook);
									}
								}
								CurrentThunk = CurrentThunk + 4;
								ThunkData32 = (PIMAGE_THUNK_DATA32)((ULONG_PTR)ThunkData32 + sizeof IMAGE_THUNK_DATA32);
							}
							ImportIID = (PIMAGE_IMPORT_DESCRIPTOR)((ULONG_PTR)ImportIID + sizeof IMAGE_IMPORT_DESCRIPTOR);
						}
						return(true);
					}__except(EXCEPTION_EXECUTE_HANDLER){
						return(false);
					}
				}
			}else{
				if(PEHeader64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress != NULL){
					ImportIID = (PIMAGE_IMPORT_DESCRIPTOR)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)(PEHeader64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress + PEHeader64->OptionalHeader.ImageBase), true);
					__try{
						while(ImportIID->FirstThunk != NULL){
							ThunkData64 = (PIMAGE_THUNK_DATA64)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)((ULONG_PTR)ImportIID->FirstThunk + PEHeader64->OptionalHeader.ImageBase), true);
							CurrentThunk = (ULONG_PTR)ImportIID->FirstThunk;
							while(ThunkData64->u1.AddressOfData != NULL){
								if(!(ThunkData64->u1.Ordinal & IMAGE_ORDINAL_FLAG64)){
									if(lstrcmpiA((char*)ConvertVAtoFileOffset(FileMapVA, (ULONG_PTR)((ULONG_PTR)ThunkData64->u1.AddressOfData + 2 + PEHeader64->OptionalHeader.ImageBase), true), szHookFunction) == NULL){
										myHook.HookAddress = (void*)(CurrentThunk + LoadedModuleBase);
										if(VirtualProtect(myHook.HookAddress, myHook.HookSize, PAGE_EXECUTE_READWRITE, &OldProtect)){
											RtlMoveMemory(&myHook.OriginalBytes[0], myHook.HookAddress, myHook.HookSize);
											RtlMoveMemory(&myHook.HookBytes[0], &myHook.RedirectionAddress, myHook.HookSize);
											RtlMoveMemory(myHook.HookAddress, &myHook.RedirectionAddress, myHook.HookSize);
											VirtualProtect(myHook.HookAddress, myHook.HookSize, OldProtect, &OldProtect);
										}
										hookEntry.push_back(myHook);
									}
								}
								CurrentThunk = CurrentThunk + 8;
								ThunkData64 = (PIMAGE_THUNK_DATA64)((ULONG_PTR)ThunkData64 + sizeof IMAGE_THUNK_DATA64);
							}
							ImportIID = (PIMAGE_IMPORT_DESCRIPTOR)((ULONG_PTR)ImportIID + sizeof IMAGE_IMPORT_DESCRIPTOR);
						}
						return(true);
					}__except(EXCEPTION_EXECUTE_HANDLER){
						return(false);
					}
				}
			}
		}else{
			return(false);		
		}
	}else{
		return(false);
	}
	return(false);
}
 bool __stdcall HooksInsertNewIATRedirection(char* szModuleName, char* szHookFunction, LPVOID RedirectTo){
	
	HANDLE FileHandle;
	DWORD FileSize;
	HANDLE FileMap;
	ULONG_PTR FileMapVA;
	DWORD NewSectionVO = NULL;
	DWORD NewSectionFO = NULL;
	HMODULE SelectedModule = NULL;
	
	SelectedModule = GetModuleHandleA(szModuleName);
	if(SelectedModule != NULL){
		if(MapFileEx(szModuleName, UE_ACCESS_READ, &FileHandle, &FileSize, &FileMap, &FileMapVA, NULL)){
			if(HooksInsertNewIATRedirectionEx(FileMapVA, (ULONG_PTR)SelectedModule, szHookFunction, RedirectTo)){
				UnMapFileEx(FileHandle, FileSize, FileMap, FileMapVA);
				return(true);
			}else{
				UnMapFileEx(FileHandle, FileSize, FileMap, FileMapVA);
			}
		}
	}
	return(false);
}
 bool __stdcall HooksRemoveRedirection(LPVOID HookAddress, bool RemoveAll){

	DWORD OldProtect = PAGE_READONLY;

	if(!RemoveAll){
		for(unsigned int i = 0; i < hookEntry.size(); i++){
			if(hookEntry[i].HookAddress == HookAddress && hookEntry[i].IATHook == false){
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.erase(hookEntry.begin() + i);
					return(true);
				}
			}
		}
		return(false);
	}else{
		for(unsigned int i = 0; i < hookEntry.size(); i++){
			if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
				RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
				VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
			}
		}
		hookEntry.clear();
		return(true);
	}
}
 bool __stdcall HooksRemoveRedirectionsForModule(HMODULE ModuleBase){

	int j = NULL;
	unsigned int i = (unsigned int)hookEntry.size();
	DWORD OldProtect = PAGE_READONLY;
	MODULEINFO RemoteModuleInfo;

	if(GetModuleInformation(GetCurrentProcess(), ModuleBase, &RemoteModuleInfo, sizeof MODULEINFO)){
		while(i > NULL){
			if((ULONG_PTR)hookEntry[i].HookAddress >= (ULONG_PTR)ModuleBase && (ULONG_PTR)hookEntry[i].HookAddress <= (ULONG_PTR)ModuleBase + RemoteModuleInfo.SizeOfImage){
				if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
					VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.erase(hookEntry.begin() + i);
					j++;
				}
			}
			i--;
		}
		if(j == NULL){
			return(false);
		}
	}else{
		return(false);
	}
	return(true);
}
 bool __stdcall HooksRemoveIATRedirection(char* szModuleName, char* szHookFunction, bool RemoveAll){
	
	unsigned int i = (unsigned int)hookEntry.size() - 1;
	DWORD OldProtect = PAGE_READONLY;
	HMODULE ModuleBase = GetModuleHandleA(szModuleName);
	DWORD FunctionNameHash = EngineHashString(szHookFunction);

	if(ModuleBase != NULL){
		while(i > 0){
			if((hookEntry[i].IATHookModuleBase == (void*)ModuleBase && RemoveAll == true) || (hookEntry[i].IATHookNameHash == FunctionNameHash && hookEntry[i].IATHook == true)){
				if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
					VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry.erase(hookEntry.begin() + i);
				}
			}
			i--;
		}
	}
	return(false);
}
 bool __stdcall HooksDisableRedirection(LPVOID HookAddress, bool DisableAll){

	DWORD OldProtect = PAGE_READONLY;

	if(!DisableAll){
		for(unsigned int i = 0; i < hookEntry.size(); i++){
			if(hookEntry[i].HookAddress == HookAddress && hookEntry[i].HookIsEnabled == true){
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry[i].HookIsEnabled = false;
					return(true);
				}
			}
		}
		return(false);
	}else{
		for(unsigned int i = 0; i < hookEntry.size(); i++){
			if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
				RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
				VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
				hookEntry[i].HookIsEnabled = false;
			}
		}
		return(true);
	}
}
 bool __stdcall HooksDisableRedirectionsForModule(HMODULE ModuleBase){

	int j = NULL;
	unsigned int i = (unsigned int)hookEntry.size();
	DWORD OldProtect = PAGE_READONLY;
	MODULEINFO RemoteModuleInfo;

	if(GetModuleInformation(GetCurrentProcess(), ModuleBase, &RemoteModuleInfo, sizeof MODULEINFO)){
		while(i > NULL){
			if((ULONG_PTR)hookEntry[i].HookAddress >= (ULONG_PTR)ModuleBase && (ULONG_PTR)hookEntry[i].HookAddress <= (ULONG_PTR)ModuleBase + RemoteModuleInfo.SizeOfImage){
				if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
					VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry[i].HookIsEnabled = false;
					j++;
				}
			}
			i--;
		}
		if(j == NULL){
			return(false);
		}
	}else{
		return(false);
	}
	return(true);
}
 bool __stdcall HooksDisableIATRedirection(char* szModuleName, char* szHookFunction, bool DisableAll){
	
	unsigned int i = (unsigned int)hookEntry.size() - 1;
	DWORD OldProtect = PAGE_READONLY;
	HMODULE ModuleBase = GetModuleHandleA(szModuleName);
	DWORD FunctionNameHash = EngineHashString(szHookFunction);

	if(ModuleBase != NULL){
		while(i > 0){
			if((hookEntry[i].IATHookModuleBase == (void*)ModuleBase && DisableAll == true) || (hookEntry[i].IATHookNameHash == FunctionNameHash && hookEntry[i].IATHook == true)){
				if(hookEntry[i].HookIsEnabled){
					if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
						RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].OriginalBytes, hookEntry[i].HookSize);
						VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
						hookEntry[i].HookIsEnabled = false;
					}
				}
			}
			i--;
		}
	}
	return(false);
}
 bool __stdcall HooksEnableRedirection(LPVOID HookAddress, bool EnableAll){

	DWORD OldProtect = PAGE_READONLY;

	if(!EnableAll){
		for(unsigned int i = 0; i < hookEntry.size(); i++){
			if(hookEntry[i].HookAddress == HookAddress && hookEntry[i].HookIsEnabled == false){
				if(VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(HookAddress, &hookEntry[i].HookBytes, hookEntry[i].HookSize);
					VirtualProtect(HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry[i].HookIsEnabled = true;
					return(true);
				}
			}
		}
		return(false);
	}else{
		for(unsigned int i = 0; i < hookEntry.size(); i++){
			if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
				RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].HookBytes, hookEntry[i].HookSize);
				VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
				hookEntry[i].HookIsEnabled = true;
			}
		}
		return(true);
	}
}
 bool __stdcall HooksEnableRedirectionsForModule(HMODULE ModuleBase){

	int j = NULL;
	unsigned int i = (unsigned int)hookEntry.size();
	DWORD OldProtect = PAGE_READONLY;
	MODULEINFO RemoteModuleInfo;

	if(GetModuleInformation(GetCurrentProcess(), ModuleBase, &RemoteModuleInfo, sizeof MODULEINFO)){
		while(i > NULL){
			if((ULONG_PTR)hookEntry[i].HookAddress >= (ULONG_PTR)ModuleBase && (ULONG_PTR)hookEntry[i].HookAddress <= (ULONG_PTR)ModuleBase + RemoteModuleInfo.SizeOfImage){
				if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
					RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].HookBytes, hookEntry[i].HookSize);
					VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
					hookEntry[i].HookIsEnabled = true;
					j++;
				}
			}
			i--;
		}
		if(j == NULL){
			return(false);
		}
	}else{
		return(false);
	}
	return(true);
}
 bool __stdcall HooksEnableIATRedirection(char* szModuleName, char* szHookFunction, bool EnableAll){
	
	unsigned int i = (unsigned int)hookEntry.size() - 1;
	DWORD OldProtect = PAGE_READONLY;
	HMODULE ModuleBase = GetModuleHandleA(szModuleName);
	DWORD FunctionNameHash = EngineHashString(szHookFunction);

	if(ModuleBase != NULL){
		while(i > 0){
			if((hookEntry[i].IATHookModuleBase == (void*)ModuleBase && EnableAll == true) || (hookEntry[i].IATHookNameHash == FunctionNameHash && hookEntry[i].IATHook == true)){
				if(!hookEntry[i].HookIsEnabled){
					if(VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, PAGE_EXECUTE_READWRITE, &OldProtect)){
						RtlMoveMemory(hookEntry[i].HookAddress, &hookEntry[i].HookBytes, hookEntry[i].HookSize);
						VirtualProtect(hookEntry[i].HookAddress, TEE_MAXIMUM_HOOK_SIZE, OldProtect, &OldProtect);
						hookEntry[i].HookIsEnabled = true;
					}
				}
			}
			i--;
		}
	}
	return(false);
}
 void __stdcall HooksScanModuleMemory(HMODULE ModuleBase, LPVOID CallBack){

	unsigned int i;
	bool FileIs64 = false;
	bool FileError = false;
	void* pOriginalInstruction;
	bool ManuallyMapped = false;
	PIMAGE_DOS_HEADER DOSHeader;
	PIMAGE_NT_HEADERS32 PEHeader32;
	PIMAGE_NT_HEADERS64 PEHeader64;
	PIMAGE_EXPORT_DIRECTORY PEExports;
	HANDLE hProcess = GetCurrentProcess();
	LIBRARY_ITEM_DATA RemoteLibInfo = {};
	PLIBRARY_ITEM_DATA pRemoteLibInfo = NULL;// = (PLIBRARY_ITEM_DATA)LibrarianGetLibraryInfoEx((void*)ModuleBase); Since we dont use TE!
	typedef bool(__stdcall *fEnumCallBack)(PHOOK_ENTRY HookDetails, void* ptrOriginalInstructions, PLIBRARY_ITEM_DATA ModuleInformation, DWORD SizeOfImage);
	fEnumCallBack myEnumCallBack = (fEnumCallBack)CallBack;
	BYTE CheckHookMemory[TEE_MAXIMUM_HOOK_SIZE];	
	PMEMORY_COMPARE_HANDLER ExportedFunctions;
	PMEMORY_COMPARE_HANDLER FunctionMemory;
	ULONG_PTR lpNumberOfBytesWritten;
	HOOK_ENTRY MyhookEntry = {};
	ULONG_PTR HookDestination;
	MODULEINFO ModuleInfo;
	BYTE HookType = NULL;
	DWORD hSize;

	if(pRemoteLibInfo == NULL){
		RemoteLibInfo.BaseOfDll = (void*)ModuleBase;
		GetModuleBaseNameA(hProcess, ModuleBase, &RemoteLibInfo.szLibraryName[0], MAX_PATH);
		GetModuleFileNameExA(hProcess, ModuleBase, &RemoteLibInfo.szLibraryPath[0], MAX_PATH);
		RemoteLibInfo.hFile = CreateFileA(RemoteLibInfo.szLibraryPath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if(RemoteLibInfo.hFile != INVALID_HANDLE_VALUE){
			RemoteLibInfo.hFileMapping = CreateFileMappingA(RemoteLibInfo.hFile, NULL, 2, NULL, GetFileSize(RemoteLibInfo.hFile, NULL), NULL);
			if(RemoteLibInfo.hFileMapping != NULL){
				RemoteLibInfo.hFileMappingView = MapViewOfFile(RemoteLibInfo.hFileMapping, 4, NULL, NULL, NULL);
				if(RemoteLibInfo.hFileMappingView == NULL){
					CloseHandle(RemoteLibInfo.hFile);
					CloseHandle(RemoteLibInfo.hFileMapping);
					FileError = true;
				}else{
					ManuallyMapped = true;
				}
			}else{
				CloseHandle(RemoteLibInfo.hFile);
				FileError = true;
			}
		}else{
			FileError = true;
		}
	}else{
		RtlMoveMemory(&RemoteLibInfo, pRemoteLibInfo, sizeof LIBRARY_ITEM_DATA);
	}
	if(!FileError){
		hSize = GetFileSize(RemoteLibInfo.hFile, NULL);
		GetModuleInformation(hProcess, ModuleBase, &ModuleInfo, sizeof MODULEINFO);
		DOSHeader = (PIMAGE_DOS_HEADER)RemoteLibInfo.hFileMappingView;
		__try{
			PEHeader32 = (PIMAGE_NT_HEADERS32)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			PEHeader64 = (PIMAGE_NT_HEADERS64)((ULONG_PTR)DOSHeader + DOSHeader->e_lfanew);
			if(PEHeader32->OptionalHeader.Magic == 0x10B){
				FileIs64 = false;
			}else if(PEHeader32->OptionalHeader.Magic == 0x20B){
				FileIs64 = true;
			}else{
				FileError = true;
			}
		}__except(EXCEPTION_EXECUTE_HANDLER){
			FileError = true;
		}
		if(!FileError){
			FunctionMemory = (PMEMORY_COMPARE_HANDLER)&CheckHookMemory[0];
			if(!FileIs64){
				__try{
					if(PEHeader32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress != NULL){
						PEExports = (PIMAGE_EXPORT_DIRECTORY)ConvertVAtoFileOffsetEx((ULONG_PTR)RemoteLibInfo.hFileMappingView, hSize, PEHeader32->OptionalHeader.ImageBase, PEHeader32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress, true, true);
						if(PEExports != NULL){
							ExportedFunctions = (PMEMORY_COMPARE_HANDLER)(ConvertVAtoFileOffsetEx((ULONG_PTR)RemoteLibInfo.hFileMappingView, hSize, PEHeader32->OptionalHeader.ImageBase, PEExports->AddressOfFunctions, true, true));
							for(i = 0; i < PEExports->NumberOfFunctions; i++){
								if(ReadProcessMemory(hProcess, (void*)((ULONG_PTR)RemoteLibInfo.BaseOfDll + ExportedFunctions->Array.dwArrayEntry[i]), &CheckHookMemory[0], TEE_MAXIMUM_HOOK_SIZE, &lpNumberOfBytesWritten)){
									if(FunctionMemory->Array.bArrayEntry[0] == 0xE9 || FunctionMemory->Array.bArrayEntry[0] == 0xE8){
										HookDestination = (ULONG_PTR)GetJumpDestination(hProcess, (ULONG_PTR)RemoteLibInfo.BaseOfDll + ExportedFunctions->Array.dwArrayEntry[i]);
										if(HookDestination >= (ULONG_PTR)RemoteLibInfo.BaseOfDll && HookDestination <= (ULONG_PTR)RemoteLibInfo.BaseOfDll + (ULONG_PTR)ModuleInfo.SizeOfImage){
											if(CallBack != NULL){
												if(FunctionMemory->Array.bArrayEntry[0] == 0xE9){
													HookType = TEE_HOOK_NRM_JUMP;
												}else{
													HookType = TEE_HOOK_NRM_CALL;
												}
												MyhookEntry.HookSize = 5;
												MyhookEntry.HookType = HookType;
												MyhookEntry.HookIsEnabled = true;
												MyhookEntry.RedirectionAddress = (void*)HookDestination;
												MyhookEntry.HookAddress = (void*)((ULONG_PTR)RemoteLibInfo.BaseOfDll + ExportedFunctions->Array.dwArrayEntry[i]);
												pOriginalInstruction = (void*)ConvertVAtoFileOffsetEx((ULONG_PTR)RemoteLibInfo.hFileMappingView, hSize, PEHeader32->OptionalHeader.ImageBase, ExportedFunctions->Array.dwArrayEntry[i], true, true);
												RtlZeroMemory(&MyhookEntry.HookBytes[0], TEE_MAXIMUM_HOOK_SIZE);
												RtlMoveMemory(&MyhookEntry.HookBytes[0], &CheckHookMemory[0], MyhookEntry.HookSize);
												RtlZeroMemory(&MyhookEntry.OriginalBytes[0], TEE_MAXIMUM_HOOK_SIZE);
												RtlMoveMemory(&MyhookEntry.OriginalBytes[0], pOriginalInstruction, MyhookEntry.HookSize);
												RelocaterRelocateMemoryBlock((ULONG_PTR)RemoteLibInfo.hFileMappingView, (ULONG_PTR)PEHeader32->OptionalHeader.ImageBase + ExportedFunctions->Array.dwArrayEntry[i], &MyhookEntry.OriginalBytes[0], MyhookEntry.HookSize, (ULONG_PTR)PEHeader32->OptionalHeader.ImageBase, (ULONG_PTR)RemoteLibInfo.BaseOfDll);
												if(!myEnumCallBack(&MyhookEntry, pOriginalInstruction, &RemoteLibInfo, ModuleInfo.SizeOfImage)){
													break;
												}
											}
										}
									}
								}
							}
						}
					}
				}__except(EXCEPTION_EXECUTE_HANDLER){

				}
			}else{
				__try{
					if(PEHeader64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress != NULL){
						PEExports = (PIMAGE_EXPORT_DIRECTORY)ConvertVAtoFileOffsetEx((ULONG_PTR)RemoteLibInfo.hFileMappingView, hSize, (ULONG_PTR)PEHeader64->OptionalHeader.ImageBase, PEHeader64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress, true, true);
						if(PEExports != NULL){
							ExportedFunctions = (PMEMORY_COMPARE_HANDLER)(ConvertVAtoFileOffsetEx((ULONG_PTR)RemoteLibInfo.hFileMappingView, hSize, (ULONG_PTR)PEHeader64->OptionalHeader.ImageBase, PEExports->AddressOfFunctions, true, true));
							for(i = 0; i < PEExports->NumberOfFunctions; i++){
								if(ReadProcessMemory(hProcess, (void*)((ULONG_PTR)RemoteLibInfo.BaseOfDll + ExportedFunctions->Array.dwArrayEntry[i]), &CheckHookMemory[0], TEE_MAXIMUM_HOOK_SIZE, &lpNumberOfBytesWritten)){
									if(FunctionMemory->Array.bArrayEntry[0] == 0xE9 || FunctionMemory->Array.bArrayEntry[0] == 0xE8){
										HookDestination = (ULONG_PTR)GetJumpDestination(hProcess, (ULONG_PTR)RemoteLibInfo.BaseOfDll + ExportedFunctions->Array.dwArrayEntry[i]);
										if(HookDestination >= (ULONG_PTR)RemoteLibInfo.BaseOfDll && HookDestination <= (ULONG_PTR)RemoteLibInfo.BaseOfDll + (ULONG_PTR)ModuleInfo.SizeOfImage){
											if(CallBack != NULL){
												if(FunctionMemory->Array.bArrayEntry[0] == 0xE9){
													HookType = TEE_HOOK_NRM_JUMP;
												}else{
													HookType = TEE_HOOK_NRM_CALL;
												}
												MyhookEntry.HookSize = 5;
												MyhookEntry.HookType = HookType;
												MyhookEntry.HookIsEnabled = true;
												MyhookEntry.RedirectionAddress = (void*)HookDestination;
												MyhookEntry.HookAddress = (void*)((ULONG_PTR)RemoteLibInfo.BaseOfDll + ExportedFunctions->Array.dwArrayEntry[i]);
												pOriginalInstruction = (void*)ConvertVAtoFileOffsetEx((ULONG_PTR)RemoteLibInfo.hFileMappingView, hSize, (ULONG_PTR)PEHeader64->OptionalHeader.ImageBase, ExportedFunctions->Array.dwArrayEntry[i], true, true);
												RtlZeroMemory(&MyhookEntry.HookBytes[0], TEE_MAXIMUM_HOOK_SIZE);
												RtlMoveMemory(&MyhookEntry.HookBytes[0], &CheckHookMemory[0], MyhookEntry.HookSize);
												RtlZeroMemory(&MyhookEntry.OriginalBytes[0], TEE_MAXIMUM_HOOK_SIZE);
												RtlMoveMemory(&MyhookEntry.OriginalBytes[0], pOriginalInstruction, MyhookEntry.HookSize);
												RelocaterRelocateMemoryBlock((ULONG_PTR)RemoteLibInfo.hFileMappingView, (ULONG_PTR)PEHeader64->OptionalHeader.ImageBase + ExportedFunctions->Array.dwArrayEntry[i], &MyhookEntry.OriginalBytes[0], MyhookEntry.HookSize, (ULONG_PTR)PEHeader64->OptionalHeader.ImageBase, (ULONG_PTR)RemoteLibInfo.BaseOfDll);
												if(!myEnumCallBack(&MyhookEntry, pOriginalInstruction, &RemoteLibInfo, ModuleInfo.SizeOfImage)){
													break;
												}
											}
										}
									}
								}
							}
						}
					}
				}__except(EXCEPTION_EXECUTE_HANDLER){

				}
			}
		}
		if(ManuallyMapped){
			if(UnmapViewOfFile(RemoteLibInfo.hFileMappingView)){
				CloseHandle(RemoteLibInfo.hFileMapping);
				CloseHandle(RemoteLibInfo.hFile);
			}
		}
	}
}
 void __stdcall HooksScanEntireProcessMemory(LPVOID CallBack){

	unsigned int i;
	DWORD ModulesLoaded;
	HMODULE EnumeratedModules[1024];

	hookEntry.clear();
	if(EnumProcessModules(GetCurrentProcess(), &EnumeratedModules[0], sizeof EnumeratedModules, &ModulesLoaded)){
		ModulesLoaded = ModulesLoaded / sizeof HANDLE;
		for(i = 1; i < ModulesLoaded; i++){
			HooksScanModuleMemory(EnumeratedModules[i], CallBack);
		}
	}
}
 void __stdcall HooksScanEntireProcessMemoryEx(){
	HooksScanEntireProcessMemory(&ProcessHookScanAddNewHook);
}
