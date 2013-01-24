/**
 * Mologie Detours
 * Copyright (c) 2011 Oliver Kuckertz <oliver.kuckertz@mologie.de>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 * @file	detours.h
 *
 * @brief	Declares the detours class.
 *
 * @todo	Implement MS hotpatching
 * @todo	Complete DetourImport class (add IAT parser, maybe ELF support)
 * @todo	Expand relative opcodes which can not be relocated
 * @todo	Other detour types, maybe use/write a mutation engine
 */
#ifdef _MSC_VER
#pragma warning(disable:4244)
#endif

#ifndef INCLUDED_LIB_MOLOGIE_DETOURS_DETOURS_H
#define INCLUDED_LIB_MOLOGIE_DETOURS_DETOURS_H

#include <stdint.h>
#include "hde.h"
#include <stdexcept>
#include <cstring>

#ifdef WIN32
#  include <Windows.h>
#  define MOLOGIE_DETOURS_MEMORY_UNPROTECT(ADDRESS, SIZE, OLDPROT) (VirtualProtect((LPVOID)(ADDRESS), (SIZE_T)(SIZE), PAGE_EXECUTE_READWRITE, &OLDPROT) == TRUE)
#  define MOLOGIE_DETOURS_MEMORY_REPROTECT(ADDRESS, SIZE, OLDPROT) (VirtualProtect((LPVOID)(ADDRESS), (SIZE_T)(SIZE), OLDPROT, &OLDPROT) == TRUE)
#  define MOLOGIE_DETOURS_MEMORY_WINDOWS_INIT(NAME) DWORD NAME
#else
#  include <sys/mman.h>
#  include <unistd.h>
#  define MOLOGIE_DETOURS_MEMORY_POSIX_PAGEPROTECT(ADDRESS, SIZE, NEWPROT) \
	( \
		mprotect((void*)((((unsigned int)(ADDRESS) + pageSize_ - 1) & ~(pageSize_ - 1)) - pageSize_), pageSize_, NEWPROT) == 0 \
	&&	( \
			((((unsigned int)(ADDRESS) + pageSize_ - 1) & ~(pageSize_ - 1)) - pageSize_) == ((((unsigned int)(ADDRESS) + (SIZE) + pageSize_ - 1) & ~(pageSize_ - 1)) - pageSize_) \
		||	mprotect((void*)((((unsigned int)(ADDRESS) + (SIZE) + pageSize_ - 1) & ~(pageSize_ - 1)) - pageSize_), pageSize_, NEWPROT) == 0 \
		) \
	)
#  define MOLOGIE_DETOURS_MEMORY_UNPROTECT(ADDRESS, SIZE, OLDPROT) MOLOGIE_DETOURS_MEMORY_POSIX_PAGEPROTECT((ADDRESS), (SIZE), PROT_READ | PROT_WRITE | PROT_EXEC)
#  define MOLOGIE_DETOURS_MEMORY_REPROTECT(ADDRESS, SIZE, OLDPROT) MOLOGIE_DETOURS_MEMORY_POSIX_PAGEPROTECT((ADDRESS), (SIZE), PROT_READ | PROT_EXEC)
#  define MOLOGIE_DETOURS_MEMORY_WINDOWS_INIT(NAME)
#endif
#define MOLOGIE_DETOURS_DETOUR_SIZE (1 + sizeof(void*))

/**
 * @namespace	MologieDetours
 *
 * @brief	Used to store library-specific classes.
 */
namespace MologieDetours
{
	/**
	 * @typedef	address_type
	 *
	 * @brief	Defines an alias representing type of an address.
	 */
#if defined(MOLOGIE_DETOURS_HDE_32)
	typedef uint32_t address_type;
#elif defined(MOLOGIE_DETOURS_HDE_64)
	typedef uint64_t address_type;
#endif

	/**
	 * @typedef	address_pointer_type
	 *
	 * @brief	Defines an alias representing type of a pointerto an address.
	 */
#if defined(MOLOGIE_DETOURS_HDE_32)
	typedef uint32_t* address_pointer_type;
#elif defined(MOLOGIE_DETOURS_HDE_64)
	typedef uint64_t* address_pointer_type;
#endif

	/**
	 * @class	DetourException
	 *
	 * @brief	Exception for signalling detour errors.
	 *
	 * @author	Oliver Kuckertz
	 * @date	14.05.2011
	 */
	class DetourException : public std::runtime_error
	{
	public:
		typedef std::runtime_error _Mybase;
		explicit DetourException(const std::string& _Message) : _Mybase(_Message.c_str()) { }
		explicit DetourException(const char* _Message) : _Mybase(_Message) { }
	};

	/**
	 * @class	DetourPageProtectionException
	 *
	 * @brief	Exception for signalling detour page protection errors.
	 *
	 * @author	Oliver Kuckertz
	 * @date	16.05.2011
	 */
	class DetourPageProtectionException : public DetourException
	{
	public:
		typedef DetourException _Mybase;
		explicit DetourPageProtectionException(const std::string& _Message, const void* errorAddress) : _Mybase(_Message.c_str()), errorAddress_(errorAddress) { }
		explicit DetourPageProtectionException(const char* _Message, const void* errorAddress) : _Mybase(_Message), errorAddress_(errorAddress) { }
		const void* GetErrorAddress() { return errorAddress_; }
	private:
		const void* errorAddress_;
	};

	/**
	 * @class	DetourDisassemblerException
	 *
	 * @brief	Exception for signalling detour disassembler errors.
	 *
	 * @author	Oliver Kuckertz
	 * @date	16.05.2011
	 */
	class DetourDisassemblerException : public DetourException
	{
	public:
		typedef DetourException _Mybase;
		explicit DetourDisassemblerException(const std::string& _Message) : _Mybase(_Message.c_str()) { }
		explicit DetourDisassemblerException(const char* _Message) : _Mybase(_Message) { }
	};

	/**
	 * @class	DetourRelocationException
	 *
	 * @brief	Exception for signalling detour relocation errors.
	 *
	 * @author	Oliver Kuckertz
	 * @date	16.05.2011
	 */
	class DetourRelocationException : public DetourException
	{
	public:
		typedef DetourException _Mybase;
		explicit DetourRelocationException(const std::string& _Message) : _Mybase(_Message.c_str()) { }
		explicit DetourRelocationException(const char* _Message) : _Mybase(_Message) { }
	};

	/**
	 * @class	Detour
	 *
	 * @brief	Used for creating detours using detour trampolines.
	 *
	 * @author	Oliver Kuckertz
	 * @date	14.05.2011
	 */
	template <typename function_type> class Detour
	{
	public:
		/**
		 * @fn	Detour::Detour(function_type pSource, function_type pDetour)
		 *
		 * @brief	Creates a new local detour using a given function address.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @param	pSource	The source function.
		 * @param	pDetour	The detour function.
		 */
		Detour(function_type pSource, function_type pDetour)
			: pSource_(pSource), pDetour_(pDetour), instructionCount_(0)
		{
			CreateDetour();
		}

		/**
		 * @fn	Detour::Detour(function_type pSource, function_type pDetour, size_t instructionCount)
		 *
		 * @brief	Creates a new local detour using a given function address and a predefined
		 * 			instruction count.
		 *
		 * @author	Oliver Kuckertz
		 * @date	22.05.2011
		 *
		 * @param	pSource				The source function.
		 * @param	pDetour				The detour function.
		 * @param	instructionCount	Size of instructions to replace, must be >=
		 * 								MOLOGIE_DETOURS_DETOUR_SIZE.
		 */
		Detour(function_type pSource, function_type pDetour, size_t instructionCount)
			: pSource_(pSource), pDetour_(pDetour), instructionCount_(instructionCount)
		{
			CreateDetour();
		}
#ifdef WIN32
		/**
		 * @fn	Detour::Detour(const char* moduleName, const char* lpProcName, function_type pDetour)
		 *
		 * @brief	Creates a new local detour on an exported function.
		 *
		 * @author	Kai Uwe Jesussek
		 * @date	06.11.2011
		 *
		 * @param	moduleName  The Name of the module.
		 * @param	lpProcName	Name of the pointer to a proc.
		 * @param	pDetour   	The detour.
		 */
		Detour(const char* moduleName, const char* lpProcName, function_type pDetour)
			: pSource_(reinterpret_cast<function_type>(GetProcAddress(GetModuleHandle(moduleName), lpProcName))), pDetour_(pDetour), instructionCount_(0)
		{
			CreateDetour();
		}
		/**
		 * @fn	Detour::Detour(HMODULE module, const char* lpProcName, function_type pDetour)
		 *
		 * @brief	Creates a new local detour on an exported function.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @param	module	  	The module.
		 * @param	lpProcName	Name of the pointer to a proc.
		 * @param	pDetour   	The detour.
		 */
		Detour(HMODULE module, const char* lpProcName, function_type pDetour)
			: pSource_(reinterpret_cast<function_type>(GetProcAddress(module, lpProcName))), pDetour_(pDetour), instructionCount_(0)
		{
			CreateDetour();
		}
#endif

		/**
		 * @fn	Detour::~Detour()
		 *
		 * @brief	Destroys the detour. If reverting the changes fails, the detour is removed by making
		 * 			the trampoline redirect to the original code, eg. remove the detour from the call
		 * 			chain.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @exception	DetourPageProtectionException	Thrown when the page protection of detour-related
		 * 												memory can not be changed.
		 */
		virtual ~Detour()
		{
			try
			{
				// Attempt to revert
				Revert();
			}
			catch(DetourException &)
			{
				// Reverting failed, redirect trampoline to original code instead
				*reinterpret_cast<address_pointer_type>(trampoline_ + 1) = backupOriginalCode_ - trampoline_ - MOLOGIE_DETOURS_DETOUR_SIZE;
			}

			// Free the detour code backup used by Revert()
			delete[] backupDetour_;
		}

		/**
		 * @fn	size_t Detour::GetInstructionCount()
		 *
		 * @brief	Gets the size of the code replaced.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @return	Returns the size of the code replaced.
		 */
		size_t GetInstructionCount()
		{
			return instructionCount_;
		}

		/**
		 * @fn	function_type Detour::GetSource()
		 *
		 * @brief	Gets the source.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @return	Returns the address of the detoured target function.
		 */
		function_type GetSource()
		{
			return pSource_;
		}

		/**
		 * @fn	function_type Detour::GetDetour()
		 *
		 * @brief	Gets the detour.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @return	Returns the address of the detour.
		 */
		function_type GetDetour()
		{
			return pDetour_;
		}

		/**
		 * @fn	function_type Detour::GetOriginalFunction()
		 *
		 * @brief	Gets the original function.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @return	Returns a function pointer which can be used to execute the original function.
		 */
		function_type GetOriginalFunction()
		{
			return reinterpret_cast<function_type>(backupOriginalCode_);
		}

	private:
		/**
		 * @fn	virtual void Detour::CreateDetour()
		 *
		 * @brief	Creates the detour.
		 *
		 * @author	Oliver Kuckertz
		 * @date	14.05.2011
		 *
		 * @exception	DetourDisassemblerException	   	Thrown when the disassembler returns an error or
		 * 												an unexpected result.
		 * @exception	DetourPageProtectionException	Thrown when the page protection of detour-related
		 * 												memory can not be changed.
		 */
		virtual void CreateDetour()
		{
#ifndef WIN32
			// Get page size on POSIX systems
			pageSize_ = sysconf(_SC_PAGESIZE);
#endif
			// Used for storing the original page protection flags on Windows
			MOLOGIE_DETOURS_MEMORY_WINDOWS_INIT(dwProt);

			// Make things simple
			uint8_t* targetFunction = reinterpret_cast<uint8_t*>(pSource_);
#ifdef WIN32
			// Check whether the function starts with a relative short jump(- sizeof detour) and assume a hotpatched function
			if(targetFunction[0] == 0xEB && static_cast<int8_t>(targetFunction[1]) == - static_cast<int8_t>(MOLOGIE_DETOURS_DETOUR_SIZE) - 2)
			{
				// Place our detour after the relative jmp
				// This will result in the hotpatch being called first, however we won't break things here
				// Use the DetourHotpatch class to create a hotpatch instead.
				pSource_ = reinterpret_cast<function_type>(reinterpret_cast<address_type>(pSource_) + 2);
				targetFunction = reinterpret_cast<uint8_t*>(pSource_);
			}
#endif
			// Used for finding the instruction count
			uint8_t* pbCurOp = targetFunction;

			// Find the required instruction count
			while(instructionCount_ < MOLOGIE_DETOURS_DETOUR_SIZE)
			{
				if(*pbCurOp == 0xC3) // Abort if a RET instruction is hit
				{
					throw DetourDisassemblerException("The target function is too short. Strictly refusing to detour it.");
				}

				size_t i = GetInstructionSize(pbCurOp);

				if(i == 0)
				{
					throw DetourDisassemblerException("Disassembler returned invalid opcode length");
				}

				instructionCount_ += i;
				pbCurOp += i;
			}

			// Backup the original code
			backupOriginalCode_ = new uint8_t[instructionCount_ + MOLOGIE_DETOURS_DETOUR_SIZE];
			memcpy(backupOriginalCode_, targetFunction, instructionCount_);

			// Fix relative jmps to point to the correct location
			RelocateCode(targetFunction, backupOriginalCode_, instructionCount_);

			// Jump back to original function after executing replaced code
			uint8_t* jmpBack = backupOriginalCode_ + instructionCount_;
			jmpBack[0] = 0xE9;
			*reinterpret_cast<address_pointer_type>(jmpBack + 1) = reinterpret_cast<address_type>(pSource_) + instructionCount_ - reinterpret_cast<address_type>(jmpBack) - MOLOGIE_DETOURS_DETOUR_SIZE;

			// Make backupOriginalCode_ executable
			if(!MOLOGIE_DETOURS_MEMORY_UNPROTECT(backupOriginalCode_, instructionCount_ + MOLOGIE_DETOURS_DETOUR_SIZE, dwProt))
			{
				throw DetourPageProtectionException("Failed to make copy of original code executable", backupOriginalCode_);
			}

			// Create a new trampoline which points at the detour
			trampoline_ = new uint8_t[MOLOGIE_DETOURS_DETOUR_SIZE];
			trampoline_[0] = 0xE9;
			*reinterpret_cast<address_pointer_type>(trampoline_ + 1) = reinterpret_cast<address_type>(pDetour_) - reinterpret_cast<address_type>(trampoline_) - MOLOGIE_DETOURS_DETOUR_SIZE;

			// Make trampoline_ executable
			if(!MOLOGIE_DETOURS_MEMORY_UNPROTECT(trampoline_, MOLOGIE_DETOURS_DETOUR_SIZE, dwProt))
			{
				throw DetourPageProtectionException("Failed to make trampoline executable", trampoline_);
			}

			// Unprotect original function
			if(!MOLOGIE_DETOURS_MEMORY_UNPROTECT(targetFunction, MOLOGIE_DETOURS_DETOUR_SIZE, dwProt))
			{
				throw DetourPageProtectionException("Failed to change page protection of original function", reinterpret_cast<void*>(targetFunction));
			}

			// Redirect original function to trampoline
			targetFunction[0] = 0xE9;
			*reinterpret_cast<address_pointer_type>(targetFunction + 1) = reinterpret_cast<address_type>(trampoline_) - reinterpret_cast<address_type>(targetFunction) - MOLOGIE_DETOURS_DETOUR_SIZE;

			// Create backup of detour
			backupDetour_ = new uint8_t[MOLOGIE_DETOURS_DETOUR_SIZE];
			memcpy(backupDetour_, targetFunction, MOLOGIE_DETOURS_DETOUR_SIZE);

			// Reprotect original function
			if(!MOLOGIE_DETOURS_MEMORY_REPROTECT(targetFunction, MOLOGIE_DETOURS_DETOUR_SIZE, dwProt))
			{
			    throw DetourPageProtectionException("Failed to change page protection of original function", reinterpret_cast<void*>(targetFunction));
			}

			// Flush instruction cache on Windows
#ifdef WIN32
			FlushInstructionCache(GetCurrentProcess(), (const void*) pSource_, MOLOGIE_DETOURS_DETOUR_SIZE);
#endif
		}

		/**
		 * @fn	void Detour::Revert()
		 *
		 * @brief	Reverts any changes made and restores the original code.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @exception	DetourException				 	Thrown when the target function has been modified.
		 * @exception	DetourPageProtectionException	Thrown when the target function's page protection
		 * 												can't be changed.
		 */
		void Revert()
		{
			// Used for storing the original page protection flags on Windows
			MOLOGIE_DETOURS_MEMORY_WINDOWS_INIT(dwProt);

			// Make sure the modified function is left as-is
			if(memcmp(reinterpret_cast<void*>(pSource_), backupDetour_, MOLOGIE_DETOURS_DETOUR_SIZE) != 0)
			{
				throw DetourException("Function has been modified, can not revert.");
			}

			// Unprotect original function
			if(!MOLOGIE_DETOURS_MEMORY_UNPROTECT(pSource_, MOLOGIE_DETOURS_DETOUR_SIZE, dwProt))
			{
				throw DetourPageProtectionException("Failed to change page protection of original function", reinterpret_cast<void*>(pSource_));
			}

			// Restore original code
			memcpy(reinterpret_cast<void*>(pSource_), backupOriginalCode_, MOLOGIE_DETOURS_DETOUR_SIZE);

			// Fix relative jmps to point to the correct location
			RelocateCode(backupOriginalCode_, reinterpret_cast<uint8_t*>(pSource_), instructionCount_);

			// Reprotect original function
			if(!MOLOGIE_DETOURS_MEMORY_REPROTECT(pSource_, MOLOGIE_DETOURS_DETOUR_SIZE, dwProt))
			{
			    throw DetourPageProtectionException("Failed to change page protection of original function", trampoline_);
			}

			// Free memory allocated for trampoline and original code
			delete[] trampoline_;
			delete[] backupOriginalCode_;
		}

		/**
		 * @fn	void Detour::RelocateCode(uint8_t* baseOld, uint8_t* baseNew, size_t size)
		 *
		 * @brief	This function relocates the copied code of another function. Only works with code
		 * 			that HDE (or the custom disassembler backend) can actually parse.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @exception	DetourRelocationException	Thrown when a relocation error occures.
		 *
		 * @param [in,out]	baseOld	The old base.
		 * @param [in,out]	baseNew	The new base.
		 * @param	size		   	The code's size.
		 */
		void RelocateCode(uint8_t* baseOld, uint8_t* baseNew, size_t size)
		{
			uint8_t* pbCurOp = baseNew;
			address_type delta = baseOld - baseNew;

			while(pbCurOp < baseNew + size)
			{
#if defined(MOLOGIE_DETOURS_HDE_32)
				hde32s hs = { 0 };
				uint8_t i = hde32_disasm(pbCurOp, &hs);
#elif defined(MOLOGIE_DETOURS_HDE_64)
				hde64s hs = { 0 };
				uint8_t i = hde64_disasm(pbCurOp, &hs);
#endif
				if(i == 0)
				{
					// Unknown instruction. Let's hope we don't break anything here and continue anyway.
					return;
				}

				if(hs.flags & F_RELATIVE)
				{
#if defined(MOLOGIE_DETOURS_HDE_32)
					if((hs.flags & F_IMM8) || (hs.flags & F_IMM16))
#elif defined(MOLOGIE_DETOURS_HDE64)
					if((hs.flags & F_IMM8) || (hs.flags & F_IMM16) || (hs.flags & F_IMM32))
#endif
					{
						// Oh noes! We shouldn't continue here.
						throw DetourRelocationException("The target function starts with a relative jmp instruction which can not be patched.");
					}

#if defined(MOLOGIE_DETOURS_HDE_32)
					if(hs.flags & F_IMM32)
					{
						unsigned char offset = (hs.opcode == 0x0F) ? 2 : 1;
						*reinterpret_cast<uint32_t*>(pbCurOp + offset) += delta;
					}
#elif defined(MOLOGIE_DETOURS_HDE_64)
					if(hs.flags & F_IMM64)
					{
						unsigned char offset = (hs.opcode == 0x0F) ? 2 : 1;
						*reinterpret_cast<uint64_t*>(pbCurOp + offset) += delta;
					}
#endif
				}

				pbCurOp += i;
			}
		}

		/**
		 * @fn	size_t Detour::GetInstructionSize(const void* code)
		 *
		 * @brief	Gets an instruction's size.
		 *
		 * @author	Oliver Kuckertz
		 * @date	14.05.2011
		 *
		 * @param	code	The instruction.
		 *
		 * @return	The instruction size.
		 */
		size_t GetInstructionSize(const void* code)
		{
#if defined(MOLOGIE_DETOURS_HDE_32)
			hde32s hs = { 0 };
			return hde32_disasm(code, &hs);
#elif defined(MOLOGIE_DETOURS_HDE_64)
			hde64s hs = { 0 };
			return hde64_disasm(code, &hs);
#endif
		}

		function_type pSource_; // Pointer to target function
		function_type pDetour_; // Pointer to detour function
		uint8_t* backupOriginalCode_; // Pointer to the original code
		uint8_t* backupDetour_; // Backup of the detour code for Revert()
		uint8_t* trampoline_; // Trampoline which points to either the detour or the backed up code
		size_t instructionCount_; // Size of code replaced
#ifndef WIN32
		long int pageSize_; // Size of a single memory page
#endif
	};

	/**
	 * @class	DetourImport
	 *
	 * @brief	Used for creating detours on an import of a single module.
	 *
	 * @author	Oliver Kuckertz
	 * @date	16.05.2011
	 */
	template <typename function_type> class DetourImport
	{
	public:

		/**
		 * @fn	DetourImport::DetourImport(address_type pSource, function_type pDetour)
		 *
		 * @brief	Creates a new local detour using a given import.
		 *
		 * @author	Oliver Kuckertz
		 * @date	09.05.2011
		 *
		 * @exception	DetourPageProtectionException	Thrown when detourpageprotection.
		 *
		 * @param	pSource	The import.
		 * @param	pDetour	The detour function.
		 */
		DetourImport(address_type pSource, function_type pDetour)
			: pSource_(reinterpret_cast<function_type*>(pSource)), pDetour_(pDetour)
		{
#ifndef WIN32
			// Get page size on POSIX systems
			pageSize_ = sysconf(_SC_PAGESIZE);
#endif
			// Used for storing the page protection flags on Windows
			MOLOGIE_DETOURS_MEMORY_WINDOWS_INIT(dwProt);

			pSourceBackup_ = *pSource_;

			if(!MOLOGIE_DETOURS_MEMORY_UNPROTECT(pSource_, sizeof(pSource_), dwProt))
			{
				throw DetourPageProtectionException("Failed to change page protection of IAT", reinterpret_cast<void*>(pSource_));
			}

			*pSource_ = pDetour_;

			if(!MOLOGIE_DETOURS_MEMORY_REPROTECT(pSource_, sizeof(pSource_), dwProt))
			{
				throw DetourPageProtectionException("Failed to change page protection of IAT", reinterpret_cast<void*>(pSource_));
			}
		}

		/**
		 * @fn	DetourImport::~DetourImport()
		 *
		 * @brief	Finaliser.
		 *
		 * @author	Oliver Kuckertz
		 * @date	16.05.2011
		 *
		 * @exception	DetourPageProtectionException	Thrown when the page protection of the IAT table
		 * 												can not be changed.
		 */
		~DetourImport()
		{
			// Only continue if another application did not modify the IAT after us.
			// This should not happen, usually.
			if(!IsValid())
			{
				// Mhm
				return;
			}

			// Used for storing the original page protection flags on Windows
			MOLOGIE_DETOURS_MEMORY_WINDOWS_INIT(dwProt);

			if(!MOLOGIE_DETOURS_MEMORY_UNPROTECT(pSource_, sizeof(pSource_), dwProt))
			{
				//FIXME: Raising exception inside the destructor is illegal 
				//throw DetourPageProtectionException("Failed to change page protection of IAT", reinterpret_cast<void*>(pSource_));
			}

			*pSource_ = pSourceBackup_;

			if(!MOLOGIE_DETOURS_MEMORY_REPROTECT(pSource_, sizeof(pSource_), dwProt))
			{
				//FIXME: Raising exception inside the destructor is illegal 
				//throw DetourPageProtectionException("Failed to change page protection of IAT", reinterpret_cast<void*>(pSource_));
			}
		}

		/**
		 * @fn	bool DetourImport::IsValid()
		 *
		 * @brief	Query if the detour is still applied.
		 *
		 * @author	Oliver Kuckertz
		 * @date	16.05.2011
		 *
		 * @return	true if valid, false if not.
		 */
		bool IsValid()
		{
			return (*pSource_ == pDetour_);
		}

	private:
		function_type* pSource_;
		function_type pSourceBackup_;
		function_type pDetour_;
#ifndef WIN32
        long int pageSize_;
#endif
	};

#ifdef WIN32
	/**
	 * @class	DetourHotpatch
	 *
	 * @brief	Creates a new local detour using hotpatching.
	 *
	 * @author	Oliver Kuckertz
	 * @date	16.05.2011
	 */
	template <typename function_type> class DetourHotpatch
		: public Detour<function_type>
	{
	public:
		/**
		 * @fn	DetourHotpatch::DetourHotpatch()
		 *
		 * @brief	Default constructor.
		 *
		 * @author	Oliver Kuckertz
		 * @date	16.05.2011
		 */
		DetourHotpatch()
		{
		}

		/**
		 * @fn	DetourHotpatch::~DetourHotpatch()
		 *
		 * @brief	Finaliser.
		 *
		 * @author	Oliver Kuckertz
		 * @date	16.05.2011
		 */
		~DetourHotpatch()
		{
		}

	private:
		/**
		 * @fn	static bool Detour::IsHotpatchable()
		 *
		 * @brief	Query if the target function is hotpatchable.
		 *
		 * @author	Oliver Kuckertz
		 * @date	16.05.2011
		 *
		 * @return	true if hotpatchable, false if not.
		 */
		bool IsHotpatchable()
		{
			const uint8_t movEdiEdi[] = { 0x8B, 0xFF };

			bool haveNops = true;
			bool haveSpace = (memcmp(reinterpret_cast<void*>(this->pSource_), movEdiEdi, sizeof(movEdiEdi)) == 0);

			uint8_t* pbCode = reinterpret_cast<uint8_t*>(this->pSource_) - MOLOGIE_DETOURS_DETOUR_SIZE;

			for(size_t i = 0; i < MOLOGIE_DETOURS_DETOUR_SIZE; i++)
			{
				if(pbCode[i] != 0x90)
				{
					haveNops = false;
					break;
				}
			}

			return (haveNops && haveSpace);
		}
	};
#endif
}

#endif // !INCLUDED_LIB_MOLOGIE_DETOURS_DETOURS_H
