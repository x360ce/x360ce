/* PE Header Reader
 * 
 * Copyright (C) 2009-2011 Jeroen Frijters, jeroen@frijters.net
 *
 * 2014-11-11 Modified by Evaldas Jocys, evaldas@jocys.com
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace x360ce.App.Win32
{
    public class PEReader
    {

        private MSDOS_HEADER msdos = new MSDOS_HEADER();
        private IMAGE_NT_HEADERS headers = new IMAGE_NT_HEADERS();
        private IMAGE_SECTION_HEADER[] sections;
        private IMAGE_COR20_HEADER cliHeader = new IMAGE_COR20_HEADER();

        public void Read(BinaryReader br)
        {
            msdos.signature = br.ReadUInt16();
            br.BaseStream.Seek(58, SeekOrigin.Current);
            msdos.peSignatureOffset = br.ReadUInt32();
            if (msdos.signature != MSDOS_HEADER.IMAGE_DOS_SIGNATURE)
            {
                throw new BadImageFormatException();
            }
            br.BaseStream.Seek(msdos.peSignatureOffset, SeekOrigin.Begin);
            headers.Read(br);
            sections = new IMAGE_SECTION_HEADER[headers.FileHeader.NumberOfSections];
            for (int i = 0; i < sections.Length; i++)
            {
                sections[i] = new IMAGE_SECTION_HEADER();
                sections[i].Read(br);
            }
            br.BaseStream.Seek(RvaToFileOffset(GetComDescriptorVirtualAddress()), SeekOrigin.Begin);
            cliHeader.Read(br);
        }

        public IMAGE_COR20_HEADER CliHeader
        {
            get { return cliHeader; }
        }

        public IMAGE_FILE_HEADER FileHeader
        {
            get { return headers.FileHeader; }
        }

        public IMAGE_OPTIONAL_HEADER OptionalHeader
        {
            get { return headers.OptionalHeader; }
        }

        public uint GetComDescriptorVirtualAddress()
        {
            return headers.OptionalHeader.DataDirectory[14].VirtualAddress;
        }

        void GetDataDirectoryEntry(int index, out int rva, out int length)
        {
            rva = (int)headers.OptionalHeader.DataDirectory[index].VirtualAddress;
            length = (int)headers.OptionalHeader.DataDirectory[index].Size;
        }

        internal long RvaToFileOffset(UInt32 rva)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                if (rva >= sections[i].VirtualAddress && rva < sections[i].VirtualAddress + sections[i].VirtualSize)
                {
                    return sections[i].PointerToRawData + rva - sections[i].VirtualAddress;
                }
            }
            throw new BadImageFormatException();
        }
    }

    public class MSDOS_HEADER
    {
        public const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;      // MZ
        public ushort signature; // 'MZ'
        // skip 58 bytes
        public uint peSignatureOffset;
    }

    public class IMAGE_NT_HEADERS
    {
        public const uint IMAGE_NT_SIGNATURE = 0x00004550; // "PE\0\0"
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader = new IMAGE_FILE_HEADER();
        public IMAGE_OPTIONAL_HEADER OptionalHeader = new IMAGE_OPTIONAL_HEADER();
        public void Read(BinaryReader br)
        {
            Signature = br.ReadUInt32();
            if (Signature != IMAGE_NT_HEADERS.IMAGE_NT_SIGNATURE)
            {
                throw new BadImageFormatException();
            }
            FileHeader.Read(br);
            OptionalHeader.Read(br);
        }
    }

    /// <summary>File header format.</summary>
    public class IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;

        public void Read(BinaryReader br)
        {
            Machine = br.ReadUInt16();
            NumberOfSections = br.ReadUInt16();
            TimeDateStamp = br.ReadUInt32();
            PointerToSymbolTable = br.ReadUInt32();
            NumberOfSymbols = br.ReadUInt32();
            SizeOfOptionalHeader = br.ReadUInt16();
            Characteristics = br.ReadUInt16();
        }

        public const ushort IMAGE_SIZEOF_FILE_HEADER = 20;

        public const ushort IMAGE_FILE_RELOCS_STRIPPED = 0x0001;  // Relocation info stripped from file.
        public const ushort IMAGE_FILE_EXECUTABLE_IMAGE = 0x0002;  // File is executable  (i.e. no unresolved external references).
        public const ushort IMAGE_FILE_LINE_NUMS_STRIPPED = 0x0004;  // Line nunbers stripped from file.
        public const ushort IMAGE_FILE_LOCAL_SYMS_STRIPPED = 0x0008;  // Local symbols stripped from file.
        public const ushort IMAGE_FILE_AGGRESIVE_WS_TRIM = 0x0010;  // Aggressively trim working set
        public const ushort IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020;  // App can handle >2gb addresses
        public const ushort IMAGE_FILE_BYTES_REVERSED_LO = 0x0080;  // Bytes of machine word are reversed.
        public const ushort IMAGE_FILE_32BIT_MACHINE = 0x0100;  // 32 bit word machine.
        public const ushort IMAGE_FILE_DEBUG_STRIPPED = 0x0200;  // Debugging info stripped from file in .DBG file
        public const ushort IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = 0x0400;  // If Image is on removable media, copy and run from the swap file.
        public const ushort IMAGE_FILE_NET_RUN_FROM_SWAP = 0x0800;  // If Image is on Net, copy and run from the swap file.
        public const ushort IMAGE_FILE_SYSTEM = 0x1000;  // System File.
        public const ushort IMAGE_FILE_DLL = 0x2000;  // File is a DLL.
        public const ushort IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000;  // File should only be run on a UP machine
        public const ushort IMAGE_FILE_BYTES_REVERSED_HI = 0x8000;  // Bytes of machine word are reversed.

        public const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0;
        public const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;  // Intel 386.
        public const ushort IMAGE_FILE_MACHINE_R3000 = 0x0162;  // MIPS little-endian, 0x160 big-endian
        public const ushort IMAGE_FILE_MACHINE_R4000 = 0x0166;  // MIPS little-endian
        public const ushort IMAGE_FILE_MACHINE_R10000 = 0x0168;  // MIPS little-endian
        public const ushort IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x0169;  // MIPS little-endian WCE v2
        public const ushort IMAGE_FILE_MACHINE_ALPHA = 0x0184;  // Alpha_AXP
        public const ushort IMAGE_FILE_MACHINE_SH3 = 0x01a2;  // SH3 little-endian
        public const ushort IMAGE_FILE_MACHINE_SH3DSP = 0x01a3;
        public const ushort IMAGE_FILE_MACHINE_SH3E = 0x01a4;  // SH3E little-endian
        public const ushort IMAGE_FILE_MACHINE_SH4 = 0x01a6;  // SH4 little-endian
        public const ushort IMAGE_FILE_MACHINE_SH5 = 0x01a8;  // SH5
        public const ushort IMAGE_FILE_MACHINE_ARM = 0x01c0;  // ARM Little-Endian
        public const ushort IMAGE_FILE_MACHINE_THUMB = 0x01c2;  // ARM Thumb/Thumb-2 Little-Endian
        public const ushort IMAGE_FILE_MACHINE_ARMNT = 0x01c4;  // ARM Thumb-2 Little-Endian
        public const ushort IMAGE_FILE_MACHINE_AM33 = 0x01d3;
        public const ushort IMAGE_FILE_MACHINE_POWERPC = 0x01F0;  // IBM PowerPC Little-Endian
        public const ushort IMAGE_FILE_MACHINE_POWERPCFP = 0x01f1;
        public const ushort IMAGE_FILE_MACHINE_IA64 = 0x0200;  // Intel 64
        public const ushort IMAGE_FILE_MACHINE_MIPS16 = 0x0266;  // MIPS
        public const ushort IMAGE_FILE_MACHINE_ALPHA64 = 0x0284;  // ALPHA64
        public const ushort IMAGE_FILE_MACHINE_AXP64 = 0x0284;
        public const ushort IMAGE_FILE_MACHINE_MIPSFPU = 0x0366;  // MIPS
        public const ushort IMAGE_FILE_MACHINE_MIPSFPU16 = 0x0466;  // MIPS
        public const ushort IMAGE_FILE_MACHINE_TRICORE = 0x0520;  // Infineon
        public const ushort IMAGE_FILE_MACHINE_CEF = 0x0CEF;
        public const ushort IMAGE_FILE_MACHINE_EBC = 0x0EBC;  // EFI Byte Code
        public const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;  // AMD64 (K8)
        public const ushort IMAGE_FILE_MACHINE_M32R = 0x9041;  // M32R little-endian
        public const ushort IMAGE_FILE_MACHINE_CEE = 0xC0EE;

    }

    /// <summary>Optional header format.</summary>
    public class IMAGE_OPTIONAL_HEADER
    {
        // Standard fields.
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        // NT additional fields.
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY[] DataDirectory;

        public void Read(BinaryReader br)
        {
            Magic = br.ReadUInt16();
            if (Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC && Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC)
            {
                throw new BadImageFormatException();
            }
            MajorLinkerVersion = br.ReadByte();
            MinorLinkerVersion = br.ReadByte();
            SizeOfCode = br.ReadUInt32();
            SizeOfInitializedData = br.ReadUInt32();
            SizeOfUninitializedData = br.ReadUInt32();
            AddressOfEntryPoint = br.ReadUInt32();
            BaseOfCode = br.ReadUInt32();
            if (Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                BaseOfData = br.ReadUInt32();
                ImageBase = br.ReadUInt32();
            }
            else
            {
                ImageBase = br.ReadUInt64();
            }
            SectionAlignment = br.ReadUInt32();
            FileAlignment = br.ReadUInt32();
            MajorOperatingSystemVersion = br.ReadUInt16();
            MinorOperatingSystemVersion = br.ReadUInt16();
            MajorImageVersion = br.ReadUInt16();
            MinorImageVersion = br.ReadUInt16();
            MajorSubsystemVersion = br.ReadUInt16();
            MinorSubsystemVersion = br.ReadUInt16();
            Win32VersionValue = br.ReadUInt32();
            SizeOfImage = br.ReadUInt32();
            SizeOfHeaders = br.ReadUInt32();
            CheckSum = br.ReadUInt32();
            Subsystem = br.ReadUInt16();
            DllCharacteristics = br.ReadUInt16();
            if (Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                SizeOfStackReserve = br.ReadUInt32();
                SizeOfStackCommit = br.ReadUInt32();
                SizeOfHeapReserve = br.ReadUInt32();
                SizeOfHeapCommit = br.ReadUInt32();
            }
            else
            {
                SizeOfStackReserve = br.ReadUInt64();
                SizeOfStackCommit = br.ReadUInt64();
                SizeOfHeapReserve = br.ReadUInt64();
                SizeOfHeapCommit = br.ReadUInt64();
            }
            LoaderFlags = br.ReadUInt32();
            NumberOfRvaAndSizes = br.ReadUInt32();
            DataDirectory = new IMAGE_DATA_DIRECTORY[NumberOfRvaAndSizes];
            for (uint i = 0; i < NumberOfRvaAndSizes; i++)
            {
                DataDirectory[i] = new IMAGE_DATA_DIRECTORY();
                DataDirectory[i].Read(br);
            }
        }

        public const ushort IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b;
        public const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b;

        public const ushort IMAGE_SUBSYSTEM_WINDOWS_GUI = 2;
        public const ushort IMAGE_SUBSYSTEM_WINDOWS_CUI = 3;

        public const ushort IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE = 0x0040;
        public const ushort IMAGE_DLLCHARACTERISTICS_NX_COMPAT = 0x0100;
        public const ushort IMAGE_DLLCHARACTERISTICS_NO_SEH = 0x0400;
        public const ushort IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE = 0x8000;

    }

    /// <summary>Directory format.</summary>
    public struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
        public void Read(BinaryReader br)
        {
            VirtualAddress = br.ReadUInt32();
            Size = br.ReadUInt32();
        }
        public const ushort IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;
    }

    /// <summary>Section header format.</summary>
    public class IMAGE_SECTION_HEADER
    {
        public const uint IMAGE_SIZEOF_SHORT_NAME = 8;

        public string Name; // 8 byte UTF8 encoded 0-padded
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;

        public void Read(BinaryReader br)
        {
            char[] name = new char[IMAGE_SIZEOF_SHORT_NAME];
            int len = 8;
            for (int i = 0; i < 8; i++)
            {
                byte b = br.ReadByte();
                name[i] = (char)b;
                if (b == 0 && len == 8)
                {
                    len = i;
                }
            }
            Name = new String(name, 0, len);
            VirtualSize = br.ReadUInt32();
            VirtualAddress = br.ReadUInt32();
            SizeOfRawData = br.ReadUInt32();
            PointerToRawData = br.ReadUInt32();
            PointerToRelocations = br.ReadUInt32();
            PointerToLinenumbers = br.ReadUInt32();
            NumberOfRelocations = br.ReadUInt16();
            NumberOfLinenumbers = br.ReadUInt16();
            Characteristics = br.ReadUInt32();
        }
    }

    /// <summary>CLR 2.0 header structure.</summary>
    public class IMAGE_COR20_HEADER
    {

        // COM+ Header entry point flags.
        const uint COMIMAGE_FLAGS_ILONLY = 0x00000001;
        const uint COMIMAGE_FLAGS_32BITREQUIRED = 0x00000002;
        const uint COMIMAGE_FLAGS_IL_LIBRARY = 0x00000004;
        const uint COMIMAGE_FLAGS_STRONGNAMESIGNED = 0x00000008;
        const uint COMIMAGE_FLAGS_NATIVE_ENTRYPOINT = 0x00000010;
        const uint COMIMAGE_FLAGS_TRACKDEBUGDATA = 0x00010000;
        const uint COMIMAGE_FLAGS_32BITPREFERRED = 0x00020000;

        // Header versioning
        uint cb = 0x48;
        ushort MajorRuntimeVersion;
        ushort MinorRuntimeVersion;
        // Symbol table and startup information
        IMAGE_DATA_DIRECTORY MetaData;
        uint Flags;
        uint EntryPointToken;
        // Binding information
        IMAGE_DATA_DIRECTORY Resources;
        IMAGE_DATA_DIRECTORY StrongNameSignature;
        // Regular fixup and binding information
        IMAGE_DATA_DIRECTORY CodeManagerTable;
        IMAGE_DATA_DIRECTORY VTableFixups;
        IMAGE_DATA_DIRECTORY ExportAddressTableJumps;
        // Precompiled image info (internal use only - set to zero)
        IMAGE_DATA_DIRECTORY ManagedNativeHeader;

        public void Read(BinaryReader br)
        {
            cb = br.ReadUInt32();
            MajorRuntimeVersion = br.ReadUInt16();
            MinorRuntimeVersion = br.ReadUInt16();
            MetaData = new IMAGE_DATA_DIRECTORY();
            MetaData.Read(br);
            Flags = br.ReadUInt32();
            EntryPointToken = br.ReadUInt32();
            Resources = new IMAGE_DATA_DIRECTORY();
            Resources.Read(br);
            StrongNameSignature = new IMAGE_DATA_DIRECTORY();
            StrongNameSignature.Read(br);
            CodeManagerTable = new IMAGE_DATA_DIRECTORY();
            CodeManagerTable.Read(br);
            VTableFixups = new IMAGE_DATA_DIRECTORY();
            VTableFixups.Read(br);
            ExportAddressTableJumps = new IMAGE_DATA_DIRECTORY();
            ExportAddressTableJumps.Read(br);
            ManagedNativeHeader = new IMAGE_DATA_DIRECTORY();
            ManagedNativeHeader.Read(br);
        }

        public bool COR_IS_32BIT_REQUIRED()
        {
            return (((Flags) & (COMIMAGE_FLAGS_32BITREQUIRED | COMIMAGE_FLAGS_32BITPREFERRED)) == (COMIMAGE_FLAGS_32BITREQUIRED));
        }

        public bool COR_IS_32BIT_PREFERRED()
        {
            return (((Flags) & (COMIMAGE_FLAGS_32BITREQUIRED | COMIMAGE_FLAGS_32BITPREFERRED)) == (COMIMAGE_FLAGS_32BITREQUIRED | COMIMAGE_FLAGS_32BITPREFERRED));
        }
    }

}
