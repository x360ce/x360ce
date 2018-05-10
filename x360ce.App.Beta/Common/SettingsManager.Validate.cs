using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.App
{
    public partial class SettingsManager
    {

        // Check game settings against folder.
        public GameRefreshStatus GetDllAndIniStatus(x360ce.Engine.Data.UserGame game, bool fix = false)
        {
            var status = GameRefreshStatus.OK;
            var fi = new FileInfo(game.FullPath);
            // Check if game file exists.
            if (!fi.Exists)
            {
                return GameRefreshStatus.ExeNotExist;
            }
            // Check if game is not enabled.
            else if (!game.IsEnabled)
            {
                return status;
            }
            else
            {
                var iniDirectory = Path.GetDirectoryName(game.FullPath);
                var di = new DirectoryInfo(iniDirectory);
                var iniFullName = Path.Combine(iniDirectory, IniFileName);
                // Create dictionary from XInput type and XInput file name.
                var xiValues = ((XInputMask[])Enum.GetValues(typeof(XInputMask))).Where(x => x != XInputMask.None).ToArray();
                var dic = new Dictionary<XInputMask, string>();
                foreach (var value in xiValues)
                {
                    dic.Add(value, Attributes.GetDescription(value));
                }
                // Get file names: xinput9_1_0.dll, xinput1_1.dll, xinput1_2.dll, xinput1_3.dll, xinput1_4.dll
                var xiFileNames = dic.Values.Distinct();
                // If game DLL emulation is not enabled then...
                if (!game.IsLibrary)
                {
                    // If INI file exists then...
                    if (File.Exists(iniFullName))
                    {
                        if (fix)
                        {
                            // Remove INI files.
                            File.Delete(iniFullName);
                        }
                        else
                        {
                            status |= GameRefreshStatus.IniExist;
                        }
                    }
                    // Loop through XInput DLL files.
                    foreach (var xiFileName in xiFileNames)
                    {
                        // Get information about XInput DLL file.
                        var xiFullPath = System.IO.Path.Combine(fi.Directory.FullName, xiFileName);
                        var xiFileInfo = new System.IO.FileInfo(xiFullPath);
                        var isCustom = EngineHelper.IsCustomLibrarry(xiFileInfo.FullName);
                        // If DLL file was made by X360CE then...
                        if (isCustom.HasValue && isCustom.Value)
                        {
                            if (fix)
                            {
                                // Remove DLL file.
                                File.Delete(xiFileInfo.FullName);
                                // Revert original XInput file if found.
                                var orgFile = di.GetFiles(xiFileName + ".*").FirstOrDefault();
                                // If original file name was found then...
                                if (orgFile != null)
                                {
                                    var orgIsCustom = EngineHelper.IsCustomLibrarry(orgFile.FullName);
                                    // If DLL file is not made by X360CE then...
                                    if (!orgIsCustom.HasValue || !orgIsCustom.Value)
                                    {
                                        // Rename.
                                        orgFile.MoveTo(xiFileName);
                                    }
                                }
                            }
                            else
                            {
                                status |= GameRefreshStatus.XInputFilesUnnecessary;
                            }
                        }
                    }
                }
                else
                {
                    var content = GetIniContent(game);
                    var bytes = SettingsHelper.GetFileConentBytes(content, Encoding.Unicode);
                    var isDifferent = SettingsHelper.IsDifferent(iniFullName, bytes);
                    if (isDifferent)
                    {
                        if (fix)
                        {
                            File.WriteAllBytes(iniFullName, bytes);
                        }
                        else
                        {
                            var iniExists = File.Exists(iniFullName);
                            status |= (iniExists ? GameRefreshStatus.IniDifferent : GameRefreshStatus.IniNotExist);
                        }
                    }
                    // Loop through XInput DLL files.
                    foreach (var xiFileName in xiFileNames)
                    {
                        // Get enumeration linked to 64-bit version of the file.
                        var x64Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x64")).Key;
                        // Get enumeration linked to 32-bit version of the file.
                        var x86Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x86")).Key;
                        // Get information about XInput DLL file.
                        var xiFullPath = System.IO.Path.Combine(fi.Directory.FullName, xiFileName);
                        var xiFileInfo = new System.IO.FileInfo(xiFullPath);
                        // Determine required architecture.
                        var requiredArchitecture = ProcessorArchitecture.None;
                        var x64Enabled = ((XInputMask)game.XInputMask).HasFlag(x64Value);
                        var x86Enabled = ((XInputMask)game.XInputMask).HasFlag(x86Value);
                        if (x86Enabled && x64Enabled) requiredArchitecture = ProcessorArchitecture.MSIL;
                        else if (x86Enabled) requiredArchitecture = ProcessorArchitecture.X86;
                        else if (x64Enabled) requiredArchitecture = ProcessorArchitecture.Amd64;
                        // Get embedded resource name for current XInput file.
                        var resourceName = EngineHelper.GetXInputResoureceName(requiredArchitecture);
                        // If both XInput file CheckBoxes are disabled then...
                        if (requiredArchitecture == ProcessorArchitecture.None)
                        {
                            // If XInput file exists then...
                            if (xiFileInfo.Exists)
                            {
                                if (fix)
                                {
                                    // Delete unnecessary XInput file.
                                    xiFileInfo.Delete();
                                }
                                else
                                {
                                    status |= GameRefreshStatus.XInputFilesUnnecessary;
                                }
                            }
                        }
                        // If XInput file doesn't exists then...
                        else if (!xiFileInfo.Exists)
                        {
                            // Create XInput file.
                            if (fix)
                            {
                                AppHelper.WriteFile(resourceName, xiFileInfo.FullName);
                            }
                            else
                            {
                                status |= GameRefreshStatus.XInputFilesNotExist;
                            }
                        }
                        else
                        {
                            // Get current architecture of XInput DLL.
                            var xiCurrentArchitecture = PEReader.GetProcessorArchitecture(xiFullPath);
                            // If processor architectures doesn't match then...
                            if (requiredArchitecture != xiCurrentArchitecture)
                            {
                                // Create XInput file.
                                if (fix)
                                {
                                    AppHelper.WriteFile(resourceName, xiFileInfo.FullName);
                                }
                                else
                                {
                                    status |= GameRefreshStatus.XInputFilesWrongPlatform;
                                }
                            }
                            else
                            {
                                // Determine if file was created by Microsoft.
                                bool byMicrosoft;
                                // Get version of XInput DLL on the disk.
                                var dllVersion = EngineHelper.GetDllVersion(xiFullPath, out byMicrosoft);
                                // Get version of embedded XInput DLL.
                                var embededVersion = EngineHelper.GetEmbeddedDllVersion(xiCurrentArchitecture);
                                // If file on disk is older then...
                                if (dllVersion < embededVersion)
                                {
                                    // Overwrite XInput file.
                                    if (fix)
                                    {
                                        AppHelper.WriteFile(resourceName, xiFileInfo.FullName);
                                    }
                                    else
                                    {
                                        status |= GameRefreshStatus.XInputFilesOlderVersion;
                                    }
                                }
                                else if (dllVersion > embededVersion)
                                {
                                    // Allow new version.
                                    // return GameRefreshStatus.XInputFileNewerVersion;
                                }
                            }
                        }
                    }
                }
            }
            return status;
        }

        Regex BackupFileRx = new Regex("(?<name>.*).(?<hash>[0-9A-F]{8})(?<ext>.[0-9A-Z]+)");

        /// <summary>
        /// Get backup file.
        /// Backup file for "xinput3_1.dll" file name will be "xinput3_1.HHHHHHHH.dll", where HHHHHHHH is CRC32 checksum in hex.
        /// </summary>
        /// <param name="fileName">Non backup file name.</param>
        FileInfo[] GetBackupFiles(string fileName)
        {
            var list = new List<FileInfo>();
            // Create pattern.
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var pattern = string.Format("{0}.????????{1}", name, ext);
            var di = new FileInfo(fileName).Directory;
            // If directory do not exists then 
            if (!di.Exists)
                return list.ToArray();
            var files = di.GetFiles(pattern);
            foreach (var file in files)
            {
                var match = BackupFileRx.Match(file.Name);
                // If pattern is incorrect then skip.
                if (!match.Success)
                    continue;
                var hash = JocysCom.ClassLibrary.Security.CRC32Helper.GetHashFromFileAsString(file.FullName);
                // If file is damaged then skip.
                if (match.Groups["hash"].Value != hash)
                    continue;
                var orgIsCustom = EngineHelper.IsCustomLibrarry(file.FullName);
                // If DLL file is made by X360CE then skip.
                if (orgIsCustom.HasValue && orgIsCustom.Value)
                    continue;
                // Add backup file to the list.
                list.Add(file);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Backup non X360CE file.
        /// Backup file for "xinput3_1.dll" file name will be "xinput3_1.HHHHHHHH.dll", where HHHHHHHH is CRC32 checksum in hex.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        void BackupFile(string fileName, bool move = false)
        {
            // Create pattern.
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var fi = new FileInfo(fileName);
            // If file do not exists then 
            if (!fi.Exists)
                return;
            var hash = JocysCom.ClassLibrary.Security.CRC32Helper.GetHashFromFileAsString(fi.FullName);
            var newName = string.Format("{0}.{1}{2}", name, hash, ext);
            var fullName = System.IO.Path.Combine(fi.Directory.FullName, newName);
            // If backup already exists then return.
            if (File.Exists(fullName))
                return;
            if (move)
                fi.MoveTo(fullName);
            else
                fi.CopyTo(fullName);
        }

    }
}
