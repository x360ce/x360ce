Microsoft Research Detours Package, Express Version 2.1 Build_216.

DISCLAIMER AND LICENSE:
=======================
The entire Detours package is covered by copyright law.
Copyright (c) Microsoft Corporation.  All rights reserved.
Portions may be covered by patents owned by Microsoft Corporation.

Usage of the Detours package is covered under the End User License Agreement.
Your usage of Detours implies your acceptance of the End User License Agreement.

If you distribute programs which use Detours, you must also distribute a
copy of DETOURED.DLL, which is required for your program to execute.
DETOURED.DLL is built when you build the libraries.

A complete list of redistributable files is in REDIST.TXT.


1. INTRODUCTION:
================
This document describes the installation and usage of this version of the
Detours package.  In particular, it provides an updated API table.

Complete documentation for the Detours package, including a detailed API
reference can be found in the Detours.chm file.


2. BUILD INSTRUCTIONS:
======================
To build the libraries and the sample applications, type "nmake".


3. VERIFYING THE INSTALL AND BUILD:
===================================
After building the libraries and sample applications, you can verify that
the Detours packet works on your Windows OS by type "nmake test" in the
samples\slept directory.  The output of "namke test" should be similar
to that contained in the file samples\slept\NORMAL.TXT.


4. CHANGES IN VERSION 2.1:
==========================
The following major changes were made in Detours 2.1 from Detours 2.0:
 * Addition of support for 64-bit code on Itanium 2 processors, using the
   IA64 instruction set.
 * Correction to disassembly table for X86 for indirection instructions
   with either 8-bit or 32-bit constant operands.

The following major changes were made in Detours 2.0 from Detours 1.5:
 * Complete API documentation.
 * Support for 64-bit code on X64 processors.
 * Addition of a transactional model for attaching and detaching detours.
 * Addition of code for updating peer threads when adjusting detours.
 * Replaced trampoline pointers with target pointers in the API to simplify usage.
 * Support for detection of detoured processes.
 * Significant compatibility fixes in the DetourCreateProcessWithDll API.
 * Removed the DetourContinueProcessWithDll API.
 * Added DetourCopyPayloadToProcess API to copy payloads to target processes.



4.1. COMPLETE API DOCUMENTATION:
================================
Detours 2.1 includes extensive online documentation in the Detours.chm file.
The documentation includes a technical overview of the Detours package, an
extensive API reference, descriptions of all of the Detours samples with
cross-links to the relevant APIs, and a list of Frequently Asked Questions
(FAQ) and answers.


4.2. 64-BIT SUPPORT:
====================
Detours 2.1 adds support 64-bit execution on X64 and IA64 processors.
Detours understands the new 64-bit instructions of the X64 and IA64 and can
detour 64-bit code when used in a 64-bit process.  However, Detours does not
support cross-compatibility between 32-bit and 64-bit code.  For example,
32-bit detours can be applied only to 32-bit code, and 64-bit detours can be
applied only to 64-bit code.


4.3. TRANSACTIONAL MODEL AND THREAD UPDATE:
===========================================
Typically, a developer uses the Detours package to detour a family of
functions.  Race conditions can be introduced into the detour code as the
target functions are detoured one by one.  Also, the developer typically
wants a error model in which all target functions are detours entirely or
none of the target functions are detoured if a particular function can't
be detoured.  In previous version of Detours, programmers either ignored
these race and error conditions, or attempted to avoid them by carefully
timing the insertion and deletion of detours.

To simplify the development model, Detours 2.1 use a transactional model for
attaching and detaching detours.  Your code should call DetourTransactionBegin
to begin a transaction, issue a group of DetourAttach or DetourDetach calls to
affect the desired target functions, call DetourUpdateThread to mark threads
which may be effected by the updates, and then call DetourTransactionCommit to
complete the operation.

When DetourTranactionCommit is called, Detours suspends all effected
threads (except the calling thread), insert or removes the detours as
specified, updates the program counter for any threads that were running
inside the effected functions, then resumes the effected threads.  If an
error occurs during the transaction, or if DetourTransactioAbort is
called, Detours safely aborts all of the operations within the transaction.
From the perspective of all threads marks for update, the entire
transaction is atomic, either all threads and functions are modified,
or none or modified.


4.4. REPLACED TRAMPOLINE POINTERS WITH TARGET POINTERS IN THE API:
====================================================================
A trampoline is a small block of code modified by Detours to contain the
instructions of the target function moved to insert the detour and a jump
to the remainder of the target function.  In previous versions of Detours,
trampolines where managed by the developer.  Detours made this as easy
as possibly by providing C macros to statically create new trampolines,
but developer code was prone to undetected mismatches in function
signatures between target functions, detour functions and trampolines. In
addition, the developers were forced to use different APIs for statically
and dynamically available functions.  With Detours 2.1, the allocation,
construction, and management of trampolines is controlled completely by
Detours.

Instead of directly using trampolines, developers should now use target
pointers to refer to target functions.  Initially, the target pointer
should point to the target function.  When a detour is attached to the
target function, Detours will allocate a trampoline function, and update
the target pointer to point to the trampoline.  When the detour is
detached from the target function, Detours will restore the target pointer
to the target function and release the trampoline.  Thanks to common C/C++
syntax, target pointers can be used exactly like functions.

The most important benefit of using target pointers, instead of trampolines
directly, is that C and C++ compiler check the check the equality of calling
conventions on function pointer assignment.  As a result, any discrepancy
between the calling conventions of a target function and a detour function
will be detected at compile time, rather than appear at runtime as mysterous
bugs caused by stack misalignment.

Another benefit of using target pointers is the reduction in the Detours APIs
as the same APIs cab be used regardless of whether the address of a target
function is available at link time or must be derived dynamically.


4.5. SUPPORT FOR DETECTION OF DETOURED PROCESSES:
=================================================
Detours loads the detoured.dll shared library stub into any process which has
been modified by the insertion of a detour.  This allows the Microsoft Customer
Support Services (CSS) and the Microsoft Online Crash Analysis (OCA) teams to
quickly and accurately determine that the behavior of a process has been
altered by a detour.  CSS does not provide customer assistance on detoured
products.


4.6. SIGNIFICANT COMPATIBILITY FIXES IN THE DETOURCREATEPROCESSWITHDLL API:
===========================================================================
The DetourCreateProcessWithDll API has been completely rewritten.  The
previous version of the API used a code injection mechanism to create a
call to LoadLibrary in the target process.  The code injection mechanism
could fail silently without any diagnostic information and was susceptible
to changes in the underlying loader in Windows.  The new implementation
modifies the DLL import table in the target process to cause the the
Windows loader to load the DLL as if it where listed in the program's
import table.  As a result, the code is much more robust and never
fails silently.


4.7. REMOVED THE DETOURCONTINUEPROCESSWITHDLL API:
=====================================================
The DetourContinueProcessWithDll API has been removed and is no longer
supported.  It was removed because there is no supported or reliable
mechanism to inject a DLL into a running process.


4.8. ADDED DETOURCOPYPAYLOADTOPROCESS API:
==========================================
The DetourCopyPayloadToProcess API copies a block of memory directly into
a payload in a target process.  DetourCopyPayloadToProcess is particular
useful for copying information from a parent process to a child process
created using the DetourCreateProcessWithDll API.


5. API SUMMARY:
===============

5.1. APIS FOR DETOURING TARGET FUNCTIONS:
=========================================
DetourTransactionBegin()    - Begin a new detour transaction.

DetourUpdateThread()        - Mark a thread that should be included in the
                              current detour transaction.

DetourAttach()              - Attach a detour to a target function as part
                              of the current detour transaction.

DetourAttachEx()            - Attach a detour to a target function and
                              retrieved additional detail about the ultimate
                              target as part of the current detour transaction.

DetourDetach()              - Detach a detour from a traget function as part
                              of the current detour transaction.

DetourSetIgnoreTooSmall()   - Set the flag to determine if failure to detour
                              a target function that is too small for detouring
                              is sufficient error to cause abort of the current
                              detour transaction.

DetourTransactionAbort()    - Abort the current detour transaction.

DetourTransactionCommit()   - Attempt to commit the current detour transaction.

DetourTransactionCommitEx() - Attempt to commit the current transaction, if
                              transaction fails, retrieve error information.


5.2. APIS FOR FINDING TARGETS:
==============================
DetourFindFunction()        - Tries to retrieve a function pointer for a named
                              function through the dynamic linking export
                              tables for the named module and then, if that
                              fails, through debugging symbols if available.

DetourCodeFromPointer()     - Give a function pointer, returns a pointer to the
                              code implementing the function.  Skips over extra
                              code often inserted by linkers or compilers for
                              cross-DLL calls.


5.3. APIS FOR FINDING ACCESSING LOADED BINARIES AND PAYLOADS:
=============================================================
DetourEnumerateModules()    - Enumerates all of the PE binaries loaded into a
                              process.

DetourGetEntryPoint()       - Returns a pointer the entry point for a module.

DetourGetModuleSize()       - Returns the load size of a module.

DetourEnumerateExports()    - Enumerates all exports from a module.

DetourFindPayload()         - Finds the address of the specified payload
                              within a module.

DetourGetSizeOfPayloads()   - Returns the size of all payloads within a
                              module.


5.4. APIS FOR MODIFYING BINARIES:
=================================
DetourBinaryOpen()          - Open a binary for in-memory update.

DetourBinaryEnumeratePayloads() - Enumerats all of the payloads in a binary.

DetourBinaryFindPayload()   - Finds a specific payload within a binary.

DetourBinarySetPayload()    - Attaches a payload to a binary.

DetourBinaryDeletePayload() - Removes a payload from a binary.

DetourBinaryPurgePayloads() - Removes all payloads from a binary.

DetourBinaryEditImports()   - Edits the import tables of a binary.

DetourBinaryResetImports()  - Removes all edits to the import tables of a
                              binary including any edits made by previous
                              programs using the Detours package.

DetourBinaryWrite()         - Writes the updated binary to a file.

DetourBinaryClose()         - Release the in-memory updates for a binary.

DetourBinaryBind()          - Binds the DLL imports for a named binary file.


5.5. APIS FOR INSERTING DLLS INTO PROCESSES:
============================================
DetourCreateProcessWithDll() - Creates a new process with the specified
                               DLL inserted into it.
DetourRestoreAfterWith()     - Restores the contents in memory import table
                               after a process was started with
                               DetourCreateProcessWithDll.
DetourGetDetouredMarker()    - Returns the handle of the detoured.dll
                               DLL loaded to mark this process as detoured.

6. COMPATIBILITY:
=================
All Detours functions are compatible with all versions of Windows NT,
Windows 2000, Windows XP, and Windows Server 2003.

Detours does not support Windows 95, Windows 98, or Windows ME.


7. MANIFEST:
============
The Detours package current consists of the Detours library (with or without
source code) and a number of sample programs.  Descriptions of the sample
programs can be found in samples\README.TXT


8. NOTES:
=========
When writing detour functions, it is imperative that the binary-calling
convention of the detour and trampoline functions match exactly the
binary-calling convention of the target function.

In a few cases, when the sizeof() a return value is smaller than sizeof(int),
C or C++ compilers will generate non-compatible binary-calling conventions by
not widening the return value to an int as is customary for small return values.
The result is a syntactically-identical, but not binary-compatible, detour
function.  In most cases, the problem can be fixed be having the detour function
return a value widened to a sizeof(int) type.  Developers are urged to exercise
caution, and should insure that correct code is generated by their C or C++
compiler for detour functions with small return values.

When attaching a DLL to a binary with Detours DLL import APIs, the DLL must
export one procedure with export ordinal 1.  The exported procedure is not
called by the application, but it used as the import target.

Detours requires a compiler compatible with Visual C++.NET or later.


9. BUG REPORTS:
===============
Please send detailed bug reports to detours@microsoft.com.  Submitted bug
reports may be used to fix bugs in future versions of the Detours package.
Please include the text "DETOURS BUG REPORT" in the subject line. Please
also include the first line of this README.TXT file containing the full
version description information.  The detours@microsoft.com email address
is not a product support line.


10. COMMERCIAL LICENSE REQUESTS:
================================
Detours is available under both a restricted non-commerial/research license
and under a commerical license.  To inquiry about acquiring a commercial
license to the Detours Package, send email to Microsoft's Intellectual
Property and Licensing Group at iplg@microsoft.com.  Please include the text
"DETOURS LICENSE REQUEST" in the subject line.

