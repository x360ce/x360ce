using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;
using System.Reflection;
using System.Xml.Serialization;
using JocysCom.ClassLibrary.Configuration;
using System.Threading.Tasks;

namespace x360ce.Engine.Data
{
    public partial class UserGame : IDisplayName, IChecksum, IDateTime, IProgram
    {

        public UserGame()
        {
            DateCreated = DateTime.Now;
            DateUpdated = DateCreated;
            Timeout = -1;
        }

        [XmlIgnore]
        public string DisplayName
        {
            get { return string.Join(" - ", new string[] { FileName, FileProductName }); }
        }

        public void LoadDefault(Program program, bool skipXInputMask = false)
        {
            if (program == null) return;
            HookMask = program.HookMask;
            if (!skipXInputMask)
            {
                XInputMask = program.XInputMask;
            }
            if (string.IsNullOrEmpty(FileProductName) && !string.IsNullOrEmpty(program.FileProductName))
            {
                FileProductName = program.FileProductName;
            }
        }

        [XmlIgnore]
        public bool Is64Bit
        {
            get
            {
                return
                    ProcessorArchitecture == (int)System.Reflection.ProcessorArchitecture.Amd64 ||
                    ProcessorArchitecture == (int)System.Reflection.ProcessorArchitecture.IA64 ||
                    ProcessorArchitecture == (int)System.Reflection.ProcessorArchitecture.MSIL;
            }
        }

        [XmlIgnore]
        public bool Is32Bit
        {
            get
            {
                return
                    ProcessorArchitecture == (int)System.Reflection.ProcessorArchitecture.X86 ||
                    ProcessorArchitecture == (int)System.Reflection.ProcessorArchitecture.MSIL;
            }
        }

        //#region Do not serialize default values

        //bool notDefault<T>(T value, T defaultValue = default(T))
        //{
        //	if (value is string && Equals(value, ""))
        //		return false;
        //	if (Equals(value, default(T)))
        //		return false;
        //	if (Equals(value, defaultValue))
        //		return false;
        //	return true;
        //}

        //public bool ShouldSerializeGameId() { return notDefault(GameId); }
        //public bool ShouldSerializeDiskDriveId() { return notDefault(DiskDriveId); }
        //public bool ShouldSerializeFileName() { return notDefault(FileName); }
        //public bool ShouldSerializeFileProductName() { return notDefault(FileProductName); }
        //public bool ShouldSerializeFileVersion() { return notDefault(FileVersion); }
        //public bool ShouldSerializeFullPath() { return notDefault(FullPath); }
        //public bool ShouldSerializeCompanyName() { return notDefault(CompanyName); }
        //public bool ShouldSerializeHookMask() { return notDefault(HookMask); }
        //public bool ShouldSerializeXInputMask() { return notDefault(XInputMask); }
        //public bool ShouldSerializeComment() { return notDefault(Comment); }
        //public bool ShouldSerializeIsEnabled() { return notDefault(IsEnabled); }
        //public bool ShouldSerializeateCreated() { return notDefault(DateCreated); }
        //public bool ShouldSerializeProcessorArchitecture() { return notDefault(ProcessorArchitecture); }
        //public bool ShouldSerializeDInputMask() { return notDefault(DInputMask); }
        //public bool ShouldSerializeDInputFile() { return notDefault(DInputFile); }
        //public bool ShouldSerializeFakeVID() { return notDefault(FakeVID); }
        //public bool ShouldSerializeFakePID() { return notDefault(FakePID); }
        //public bool ShouldSerializeTimeout() { return notDefault(Timeout); }
        //public bool ShouldSerializeWeight() { return notDefault(Weight); }
        //public bool ShouldSerializeAutoMapMask() { return notDefault(AutoMapMask); }

        //#endregion

    }
}
