using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Security.Principal;
using SharpDX.DirectInput;
using System.Security.AccessControl;
using x360ce.App.Controls;
using System.Diagnostics;
using System.Linq;
using SharpDX.XInput;

namespace x360ce.App
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        DeviceDetector detector;

        public static MainForm Current { get; set; }

        string dllFile0 = "xinput9_1_0.dll";
        string dllFile1 = "xinput1_1.dll";
        string dllFile2 = "xinput1_2.dll";
        string dllFile3 = "xinput1_3.dll";
        // Will be set to default values.
        string dllFile;

        public int oldIndex;

        public int ControllerIndex
        {
            get
            {
                int newIndex = -1;
                if (MainTabControl.SelectedTab == Pad1TabPage) newIndex = 0;
                if (MainTabControl.SelectedTab == Pad2TabPage) newIndex = 1;
                if (MainTabControl.SelectedTab == Pad3TabPage) newIndex = 2;
                if (MainTabControl.SelectedTab == Pad4TabPage) newIndex = 3;
                return newIndex;
            }
        }

        public Controls.AboutControl ControlAbout;
        public Controls.PadControl[] ControlPads;
        public TabPage[] ControlPages;

        public System.Timers.Timer UpdateTimer;
        public System.Timers.Timer SettingsTimer;
        public System.Timers.Timer CleanStatusTimer;

        public Controller[] GamePads = new Controller[4];

        void MainForm_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 4; i++)
            {
                GamePads[i] = new Controller((UserIndex)i);
            }
            UpdateTimer = new System.Timers.Timer();
            UpdateTimer.AutoReset = false;
            UpdateTimer.SynchronizingObject = this;
            UpdateTimer.Interval = 50;
            UpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(UpdateTimer_Elapsed);
            SettingsTimer = new System.Timers.Timer();
            SettingsTimer.AutoReset = false;
            SettingsTimer.SynchronizingObject = this;
            SettingsTimer.Interval = 500;
            SettingsTimer.Elapsed += new System.Timers.ElapsedEventHandler(SettingsTimer_Elapsed);
            CleanStatusTimer = new System.Timers.Timer();
            CleanStatusTimer.AutoReset = false;
            CleanStatusTimer.SynchronizingObject = this;
            CleanStatusTimer.Interval = 3000;
            CleanStatusTimer.Elapsed += new System.Timers.ElapsedEventHandler(CleanStatusTimer_Elapsed);
            // Start Timers.
            UpdateTimer.Start();
        }

        bool formLoaded = false;

        void LoadForm()
        {
            formLoaded = true;
            detector = new DeviceDetector(false);
            detector.DeviceChanged += new DeviceDetector.DeviceDetectorEventHandler(detector_DeviceChanged);
            BusyLoadingCircle.Visible = false;
            BusyLoadingCircle.Top = HeaderPictureBox.Top;
            BusyLoadingCircle.Left = HeaderPictureBox.Left;
            defaultBody = HelpBodyLabel.Text;
            //if (DesignMode) return;
            // init default
            DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
            // Fill FakeWmi ComboBox.
            var fakeModeOptions = new List<KeyValuePair>();
            var fakeModeTypes = (HookMode[])Enum.GetValues(typeof(HookMode));
            foreach (var item in fakeModeTypes) fakeModeOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
            FakeModeComboBox.DataSource = fakeModeOptions;
            FakeModeComboBox.DisplayMember = "Key";
            FakeModeComboBox.ValueMember = "Value";
            // Set status.
            StatusSaveLabel.Visible = false;
            StatusEventsLabel.Visible = false;
            // Set default cXinputFile.
            if (System.IO.File.Exists(dllFile3)) dllFile = dllFile3;
            else if (System.IO.File.Exists(dllFile2)) dllFile = dllFile2;
            else if (System.IO.File.Exists(dllFile1)) dllFile = dllFile1;
            else if (System.IO.File.Exists(dllFile0)) dllFile = dllFile0;
            else dllFile = dllFile3;
            // Load Tab pages.
            ControlPages = new TabPage[4];
            ControlPages[0] = Pad1TabPage;
            ControlPages[1] = Pad2TabPage;
            ControlPages[2] = Pad3TabPage;
            ControlPages[3] = Pad4TabPage;
            BuletImageList.Images.Add("bullet_square_glass_blue.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_blue.png")));
            BuletImageList.Images.Add("bullet_square_glass_green.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_green.png")));
            BuletImageList.Images.Add("bullet_square_glass_grey.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_grey.png")));
            BuletImageList.Images.Add("bullet_square_glass_red.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_red.png")));
            BuletImageList.Images.Add("bullet_square_glass_yellow.png", new Bitmap(Helper.GetResource("Images.bullet_square_glass_yellow.png")));
            foreach (var item in ControlPages) item.ImageKey = "bullet_square_glass_grey.png";
            // Hide status values.
            StatusDllLabel.Text = dllFile;
            MainStatusStrip.Visible = false;
            // Check if ini and dll is on disk.
            if (!CheckFiles(true)) return;
            CheckEncoding(SettingManager.TmpFileName);
            CheckEncoding(SettingManager.IniFileName);
            // Show status values.
            MainStatusStrip.Visible = true;
            // Load PAD controls.
            ControlPads = new Controls.PadControl[4];
            for (int i = 0; i < ControlPads.Length; i++)
            {
                ControlPads[i] = new Controls.PadControl(i);
                ControlPads[i].Name = string.Format("ControlPad{0}", i + 1);
                ControlPads[i].Dock = DockStyle.Fill;
                ControlPages[i].Controls.Add(ControlPads[i]);
                ControlPads[i].InitPadControl();
                // Init presets. Execute only after name of cIniFile is set.
                ControlPads[i].InitPresets();
            }
            // Allow events after PAD control are loaded.
            MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
            // Load about control.
            ControlAbout = new Controls.AboutControl();
            ControlAbout.Dock = DockStyle.Fill;
            AboutTabPage.Controls.Add(ControlAbout);
            // Update settings map.
            UpdateSettingsMap();
            ReloadXinputSettings();
            Version v = new Version(Application.ProductVersion);
            this.Text += " " + Application.ProductVersion;
            // Version = major.minor.build.revision
            switch (v.Build)
            {
                case 0: this.Text += " Alpha"; break;  // Alpha Release (AR)
                case 1: this.Text += " Beta 1"; break; // Master Beta (MB)
                case 2: this.Text += " Beta 2"; break; // Feature Complete (FC)
                case 3: this.Text += " Beta 3"; break; // Technical Preview (TP)
                case 4: this.Text += " RC"; break;     // Release Candidate (RC)
                // case 5: this.Text += " RTM"; break; // Release to Manufacturing (RTM)
                // case 6: this.Text += " GM"; break;  // General Availability (GA) / Gold
            }
            ////InitDirectInputTab();
            //// Timer will execute ReloadXInputLibrary();
            ////XInput.ReLoadLibrary(cXinput3File);
            ////XInput.ReLoadLibrary(cXinput3File);
            //// start capture events.
            if (Win32.WinAPI.IsVista && Win32.WinAPI.IsElevated() && Win32.WinAPI.IsInAdministratorRole) this.Text += " (Administrator)";
            ////ReloadXInputLibrary();
        }

        void detector_DeviceChanged(object sender, DeviceDetectorEventArgs e)
        {
            forceRecountDevices = true;
        }

        /// <summary>
        /// Link control with INI key. Value/Text of controll will be automatically tracked and INI file updated.
        /// </summary>
        void UpdateSettingsMap()
        {
            // INI setting keys with controls.
            var sm = SettingManager.Current.SettingsMap;
            SettingManager.Current.ConfigSaved += new EventHandler<SettingEventArgs>(Current_ConfigSaved);
            SettingManager.Current.ConfigLoaded += new EventHandler<SettingEventArgs>(Current_ConfigLoaded);
            string section = @"Options\";
            sm.Add(section + SettingName.UseInitBeep, UseInitBeepCheckBox);
            sm.Add(section + SettingName.DebugMode, DebugModeCheckBox);
            sm.Add(section + SettingName.Log, EnableLoggingCheckBox);
            sm.Add(section + SettingName.Console, ConsoleCheckBox);
            sm.Add(section + SettingName.InternetDatabaseUrl, InternetDatabaseUrlTextBox);
            sm.Add(section + SettingName.InternetFeatures, InternetCheckBox);
            sm.Add(section + SettingName.InternetAutoload, InternetAutoloadCheckBox);
            sm.Add(section + SettingName.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
            section = @"InputHook\";
            sm.Add(section + SettingName.HookMode, FakeModeComboBox);
            for (int i = 0; i < ControlPads.Length; i++)
            {
                var map = ControlPads[i].SettingsMap;
                foreach (var key in map.Keys) sm.Add(key, map[key]);
            }
        }

        void Current_ConfigSaved(object sender, SettingEventArgs e)
        {
            StatusSaveLabel.Text = string.Format("S {0}", e.Count);
        }

        void Current_ConfigLoaded(object sender, SettingEventArgs e)
        {
            StatusTimerLabel.Text = string.Format("'{0}' loaded.", e.Name);
        }

        public void CopyElevated(string source, string dest)
        {
            if (!Win32.WinAPI.IsVista)
            {
                System.IO.File.Copy(source, dest);
                return;
            }
            var di = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(dest));
            var security = di.GetAccessControl();
            var fi = new FileInfo(dest);
            var fileSecurity = fi.GetAccessControl();
            // Allow Users to Write.
            //SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            //fileSecurity.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Write, AccessControlType.Allow));
            //fi.SetAccessControl(fileSecurity);
            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            string message = string.Empty;
            foreach (var myacc in rules)
            {
                var acc = (FileSystemAccessRule)myacc;
                message += string.Format("IdentityReference: {0}\r\n", acc.IdentityReference.Value);
                message += string.Format("Access Control Type: {0}\r\n", acc.AccessControlType.ToString());
                message += string.Format("\t{0}\r\n", acc.FileSystemRights.ToString());
                //if ((acc.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl)
                //{
                //    Console.Write("FullControl");
                //}
                //if ((acc.FileSystemRights & FileSystemRights.ReadData) == FileSystemRights.ReadData)
                //{
                //    Console.Write("ReadData");
                //}
                //if ((acc.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData)
                //{
                //    Console.Write("WriteData");
                //}
                //if ((acc.FileSystemRights & FileSystemRights.ListDirectory) == FileSystemRights.ListDirectory)
                //{
                //    Console.Write("ListDirectory");
                //}
                //if ((acc.FileSystemRights & FileSystemRights.ExecuteFile) == FileSystemRights.ExecuteFile)
                //{
                //    Console.Write("ExecuteFile");
                //}
            }
            MessageBox.Show(message);
            //WindowsIdentity self = System.Security.Principal.WindowsIdentity.GetCurrent();
            //			 FileSystemAccessRule rule = new FileSystemAccessRule(
            //    self.Name, 
            //    FileSystemRights.FullControl,
            //    AccessControlType.Allow);
        }

        void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            for (int i = 0; i < ControlPads.Length; i++)
            {
                // Stop recording on all panels.
                if (e.KeyCode == System.Windows.Forms.Keys.Escape && ControlPads[i].Recording)
                {
                    ControlPads[i].RecordingStop(null);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            StatusTimerLabel.Text = "";
        }

        void CleanStatusTimer_Elapsed(object sender, EventArgs e)
        {
            if (Program.IsClosing) return;
            StatusTimerLabel.Text = "";
        }

        #region Setting Events

        public void LoadPreset(string name)
        {
            if (ControllerIndex == -1) return;
            // exit if "Presets:" or "Embedded:".
            if (name.Contains(":")) return;
            var prefix = System.IO.Path.GetFileNameWithoutExtension(SettingManager.IniFileName);
            var ext = System.IO.Path.GetExtension(SettingManager.IniFileName);
            string resourceName = string.Format("{0}.{1}{2}", prefix, name, ext);
            var resource = Helper.GetResource("Presets." + resourceName);
            // If internal preset was found.
            if (resource != null)
            {
                // Export file.
                var sr = new StreamReader(resource);
                System.IO.File.WriteAllText(resourceName, sr.ReadToEnd());
            }
            SuspendEvents();
            // preset will be stored in inside [PAD1] section;
            SettingManager.Current.ReadPadSettings(resourceName, "PAD1", ControllerIndex);
            ResumeEvents();
            // Save setting and notify if vaue changed.
            if (SettingManager.Current.SaveSettings()) NotifySettingsChange();
            // remove file if it was from resource.
            if (resource != null) System.IO.File.Delete(resourceName);
            //CleanStatusTimer.Start();
        }

        int resumed = 0;
        int suspended = 0;

        public void SuspendEvents()
        {
            StatusEventsLabel.Text = "OFF...";
            // Don't allow controls to fire events.
            foreach (var control in SettingManager.Current.SettingsMap.Values)
            {
                if (control is TrackBar) ((TrackBar)control).ValueChanged -= new EventHandler(Control_ValueChanged);
                if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged -= new EventHandler(Control_ValueChanged);
                if (control is CheckBox) ((CheckBox)control).CheckedChanged -= new EventHandler(Control_CheckedChanged);
                if (control is ComboBox) ((ComboBox)control).SelectedIndexChanged -= new EventHandler(this.Control_TextChanged);
                if (control is ComboBox) control.TextChanged -= new System.EventHandler(this.Control_TextChanged);
                // || control is TextBox
            }
            suspended++;
            StatusEventsLabel.Text = string.Format("OFF {0} {1}", suspended, resumed);
        }

        public void ResumeEvents()
        {
            StatusEventsLabel.Text = "ON...";
            // Allow controls to fire events.
            foreach (var control in SettingManager.Current.SettingsMap.Values)
            {
                if (control is TrackBar) ((TrackBar)control).ValueChanged += new EventHandler(Control_ValueChanged);
                if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged += new EventHandler(Control_ValueChanged);
                if (control is CheckBox) ((CheckBox)control).CheckedChanged += new EventHandler(Control_CheckedChanged);
                if (control is ComboBox) ((ComboBox)control).SelectedIndexChanged += new EventHandler(this.Control_TextChanged);
                if (control is ComboBox) control.TextChanged += new System.EventHandler(this.Control_TextChanged);
                //  || control is TextBox
            }
            resumed++;
            StatusEventsLabel.Text = string.Format("ON {0} {1}", suspended, resumed);
        }

        /// <summary>
        /// Delay settings trough timer so interface will be more responsive on TrackBars.
        /// Or fast changes. Library will be reloaded as soon as user calms down (no setting changes in 500ms).
        /// </summary>
        public void NotifySettingsChange()
        {
            UpdateTimer.Stop();
            SettingsTimer.Stop();
            // Timer will be started inside Settings timer.
            SettingsTimer.Start();
        }

        void Control_TextChanged(object sender, EventArgs e)
        {
            // Save setting and notify if vaue changed.
            if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
        }

        void Control_ValueChanged(object sender, EventArgs e)
        {
            // Save setting and notify if vaue changed.
            if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
        }

        void Control_CheckedChanged(object sender, EventArgs e)
        {
            // Save setting and notify if vaue changed.
            if (SettingManager.Current.SaveSetting((Control)sender)) NotifySettingsChange();
        }

        public void ReloadXinputSettings()
        {
            SuspendEvents();
            SettingManager.Current.ReadSettings();
            ResumeEvents();
        }

        #endregion

        public static object XInputLock = new object();

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.IsClosing = true;
            UpdateTimer.Stop();
            // Disable force feedback effect before closing app.
            try
            {
                lock (XInputLock)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (ControlPads[i].LeftMotorTestTrackBar.Value > 0 || ControlPads[i].RightMotorTestTrackBar.Value > 0)
                        {
                            var gamePad = GamePads[i];
                            if (XInput.IsLoaded && gamePad.IsConnected)
                            {
                                gamePad.SetVibration(new Vibration());
                            }
                        }

                    }
                    XInput.FreeLibrary();
                }
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception) { }
            var tmp = new FileInfo(SettingManager.TmpFileName);
            var ini = new FileInfo(SettingManager.IniFileName);
            if (tmp.Exists)
            {
                // Before renaming file check for changes.
                var changed = false;
                if (tmp.Length != ini.Length) { changed = true; }
                else
                {
                    var tmpChecksum = Helper.GetFileChecksum(tmp.FullName);
                    var iniChecksum = Helper.GetFileChecksum(ini.FullName);
                    changed = !tmpChecksum.Equals(iniChecksum);
                }
                if (changed)
                {
                    var form = new MessageBoxForm();
                    form.StartPosition = FormStartPosition.CenterParent;
                    var result = form.ShowForm(
                    "Do you want to save changes you made to configuration?",
                    "Save Changes?",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        // Do nothing since ini contains latest updates.
                    }
                    else if (result == System.Windows.Forms.DialogResult.No)
                    {
                        // Rename temp to ini.
                        tmp.CopyTo(SettingManager.IniFileName, true);
                    }
                    else if (result == System.Windows.Forms.DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                // delete temp.
                tmp.Delete();
            }
        }

        #region Timer


        List<DeviceInstance> _diInstancesOld;
        List<DeviceInstance> diInstancesOld
        {
            get { return _diInstancesOld = _diInstancesOld ?? new List<DeviceInstance>(); }
            set { _diInstancesOld = value; }
        }

        List<DeviceInstance> _diInstances;
        List<DeviceInstance> diInstances
        {
            get { return _diInstances = _diInstances ?? new List<DeviceInstance>(); }
            set { _diInstances = value; }
        }

        List<Joystick> _diDevices;
        List<Joystick> diDevices
        {
            get { return _diDevices = _diDevices ?? new List<Joystick>(); }
            set { _diDevices = value; }
        }

        int _diCount = -1;

        bool forceRecountDevices = true;
        int deviceCount = 0;

        public DirectInput Manager = new DirectInput();

        /// <summary>
        /// Access this only insite Timer_Click!
        /// </summary>
        bool RefreshCurrentInstances()
        {

            // If you encouter "LoaderLock was detected" Exception when debugging then:
            // Make sure that you have reference to Microsoft.Directx.dll. 
            bool instancesChanged = false;
            IList<DeviceInstance> devices = null;
            //var types = DeviceType.Driving | DeviceType.Flight | DeviceType.Gamepad | DeviceType.Joystick | DeviceType.FirstPerson;
            if (forceRecountDevices)
            {
                devices = Manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices);
                deviceCount = devices.Count;
                forceRecountDevices = false;
            }
            //Populate All devices
            if (deviceCount != _diCount)
            {
                _diCount = deviceCount;
                if (devices == null) devices = Manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices);
                var instances = devices;
                // Dispose from previous list of devices.
                for (int i = 0; i < diDevices.Count; i++)
                {
                    // Dispose current device.
                    diDevices[i].Unacquire();
                    diDevices[i].Dispose();
                }
                diDevices.Clear();
                // Create new list of devices.
                for (int i = 0; i < instances.Count; i++)
                {
                    var ig = instances[i].InstanceGuid;
                    var device = new Joystick(Manager, ig);
                    //device.SetCooperativeLevel(this, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                    //device.Acquire();
                    diDevices.Add(device);
                }
                onlineUserControl1.BindDevices(instances);
                onlineUserControl1.BindFiles();
                // Assign new list of instances.
                diInstancesOld.Clear();
                diInstancesOld.AddRange(diInstances.ToArray());
                diInstances.Clear();
                diInstances.AddRange(instances.ToArray());
                instancesChanged = true;
            }
            // Return true if instances changed.
            return instancesChanged;
        }

        void SettingsTimer_Elapsed(object sender, EventArgs e)
        {
            if (Program.IsClosing) return;
            settingsChanged = true;
            UpdateTimer.Start();
        }

        bool settingsChanged = false;
        State emptyState = new State();

        bool[] cleanPadStatus = new bool[4];

        void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Program.IsClosing) return;
            Program.TimerCount++;
            if (!formLoaded) LoadForm();
            bool instancesChanged = RefreshCurrentInstances();
            // Load direct input data.
            for (int i = 0; i < 4; i++)
            {
                var currentPadControl = ControlPads[i];
                var currentDevice = i < diDevices.Count ? diDevices[i] : null;
                // If current device is empty then..
                if (currentDevice == null)
                {
                    // but form contains data then...
                    if (!cleanPadStatus[i])
                    {
                        // Clear all settings.
                        SuspendEvents();
                        SettingManager.Current.ClearPadSettings(i);
                        ResumeEvents();
                        cleanPadStatus[i] = true;
                    }
                }
                else
                {
                    cleanPadStatus[i] = false;
                }
                currentPadControl.UpdateFromDirectInput(currentDevice);
            }
            // If settings changed or directInput instances changed then...
            if (settingsChanged || instancesChanged)
            {
                if (instancesChanged)
                {
                    var updated = SettingManager.Current.CheckSettings(diInstances, diInstancesOld);
                    if (updated) SettingManager.Current.SaveSettings();
                }
                ReloadLibrary();
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    // DInput instance is ON.
                    var diOn = i < diInstances.Count;
                    // XInput instance is ON.
                    //XInput.Controllers[i].PollState();
                    var xiOn = false;
                    State currentPad = emptyState;
                    var currentPadControl = ControlPads[i];
                    lock (XInputLock)
                    {
                        var gamePad = GamePads[i];
                        if (XInput.IsLoaded && gamePad.IsConnected)
                        {
                            currentPad = gamePad.GetState();
                            xiOn = true;
                        }
                    }
                    currentPadControl.UpdateFromXInput(currentPad, xiOn);
                    // Update LED of gamepad state.
                    string image = diOn
                        // di ON, xi ON 
                        ? xiOn ? "green"
                        // di ON, xi OFF
                        : "red"
                        // di OFF, xi ON
                        : xiOn ? "yellow"
                        // di OFF, xi OFF
                        : "grey";
                    string bullet = string.Format("bullet_square_glass_{0}.png", image);
                    if (ControlPages[i].ImageKey != bullet) ControlPages[i].ImageKey = bullet;
                }
                UpdateStatus("");
            }
            UpdateTimer.Start();
        }



        Version _dllVersion;
        Version dllVersion
        {
            get
            {
                if (_dllVersion != null) return _dllVersion;
                bool byMicrosoft;
                _dllVersion = GetDllVersion(dllFile, out byMicrosoft);
                return _dllVersion;
            }
            set { _dllVersion = value; }
        }


        Version GetDllVersion(string fileName, out bool byMicrosoft)
        {
            var dllInfo = new System.IO.FileInfo(fileName);
            byMicrosoft = false;
            if (dllInfo.Exists)
            {
                var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllInfo.FullName);
                byMicrosoft = !string.IsNullOrEmpty(vi.CompanyName) && vi.CompanyName.Contains("Microsoft");
                return new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
            }
            return new Version(0, 0, 0, 0);
        }

        Version GetEmbeddedDllVersion()
        {
            // There must be an easier way to check embeded non managed dll version.
            var assembly = Assembly.GetExecutingAssembly();
            var sr = assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + dllFile3);
            string tempPath = Path.GetTempPath();
            FileStream sw = null;
            var tempFile = Path.Combine(Path.GetTempPath(), dllFile);
            sw = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
            var buffer = new byte[1024];
            while (true)
            {
                var count = sr.Read(buffer, 0, buffer.Length);
                if (count == 0) break;
                sw.Write(buffer, 0, count);
            }
            sr.Close();
            sw.Close();
            var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(tempFile);
            var v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
            System.IO.File.Delete(tempFile);
            return v;
        }

        public void ReloadLibrary()
        {
            Program.ReloadCount++;
            settingsChanged = false;
            var dllInfo = new System.IO.FileInfo(dllFile);
            if (dllInfo.Exists)
            {
                bool byMicrosoft;
                dllVersion = GetDllVersion(dllInfo.FullName, out byMicrosoft);
                StatusDllLabel.Text = dllFile + " " + dllVersion.ToString() + (byMicrosoft ? " (Microsoft)" : "");
                // If fast reload od settings is supported then...
                lock (XInputLock)
                {
                    if (XInput.IsResetSupported)
                    {
                        XInput.Reset();
                    }
                    // Slow: Reload whole x360ce.dll.
                    XInput.ReLoadLibrary(dllFile);
                    if (!XInput.IsLoaded)
                    {
                        var msg = string.Format("Failed to load '{0}'", dllFile);
                        var form = new MessageBoxForm();
                        form.StartPosition = FormStartPosition.CenterParent;
                        form.ShowForm(msg, msg, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public void UpdateStatus(string message)
        {
            StatusTimerLabel.Text = string.Format("Count: {0}, Reloads: {1}, Errors: {2} {3}",
                Program.TimerCount, Program.ReloadCount, Program.ErrorCount, message);
        }
        #endregion

        bool HelpInit = false;

        void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MainTabControl.SelectedTab == HelpTabPage && !HelpInit)
            {
                // Move this here so interface will load one second faster.
                HelpInit = true;
                var stream = Helper.GetResource("Documents.Help.htm");
                var sr = new StreamReader(stream);
                NameValueCollection list = new NameValueCollection();
                list.Add("font-name-default", "'Microsoft Sans Serif'");
                list.Add("font-size-default", "16");
                HelpRichTextBox.Rtf = Html2Rtf.Converter.Html2Rtf(sr.ReadToEnd(), list);
                HelpRichTextBox.SelectAll();
                HelpRichTextBox.SelectionIndent = 8;
                HelpRichTextBox.SelectionRightIndent = 8;
                HelpRichTextBox.DeselectAll();
            }
            else if (MainTabControl.SelectedTab == SettingsDatabaseTabPage)
            {
                if (InternetCheckBox.Checked && InternetAutoloadCheckBox.Checked)
                {
                    onlineUserControl1.RefreshGrid(true);
                }
            }
            UpdateHelpHeader();
        }

        void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var cbx = (CheckBox)sender;
            if (!cbx.Checked) Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
            else Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
        }

        void XInputEnableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            lock (XInputLock)
            {
                XInput.XInputEnable(XInputEnableCheckBox.Checked);
            }
        }

        #region Help Header

        string defaultBody;

        public void UpdateHelpHeader(string message, MessageBoxIcon icon)
        {
            HelpSubjectLabel.Text = MainTabControl.SelectedTab.Text;
            if (ControllerIndex > -1)
            {
                var currentPadControl = ControlPads[ControllerIndex];
                HelpSubjectLabel.Text += " - " + currentPadControl.PadTabControl.SelectedTab.Text;
            }
            HelpBodyLabel.Text = string.IsNullOrEmpty(message) ? defaultBody : message;
            if (icon == MessageBoxIcon.Error) HelpBodyLabel.ForeColor = System.Drawing.Color.DarkRed;
            else if (icon == MessageBoxIcon.Information) HelpBodyLabel.ForeColor = System.Drawing.Color.DarkGreen;
            else HelpBodyLabel.ForeColor = System.Drawing.SystemColors.ControlText;
        }

        public void UpdateHelpHeader(string message)
        {
            UpdateHelpHeader(message, MessageBoxIcon.None);
        }

        public void UpdateHelpHeader()
        {
            UpdateHelpHeader(defaultBody, MessageBoxIcon.None);
        }

        #endregion

        #region Check Files

        bool CheckFiles(bool createIfNotExist)
        {

            if (createIfNotExist)
            {
                // If ini file doesn't exists.
                if (!System.IO.File.Exists(SettingManager.IniFileName))
                {
                    if (!CreateFile(SettingManager.IniFileName, null)) return false;
                }
                // If xinput file doesn't exists.
                var embeddedDllVersion = GetEmbeddedDllVersion();
                if (!System.IO.File.Exists(dllFile))
                {
                    if (!CreateFile(dllFile, null)) return false;
                }
                else if (dllVersion < embeddedDllVersion)
                {
                    CreateFile(dllFile, embeddedDllVersion);
                    return true;
                }
            }
            // Can't run witout ini.
            if (!File.Exists(SettingManager.IniFileName))
            {
                var form = new MessageBoxForm();
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowForm(
                string.Format("Configuration file '{0}' is required for application to run!", SettingManager.IniFileName),
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return false;
            }
            // If temp file exist then.
            FileInfo iniTmp = new FileInfo(SettingManager.TmpFileName);
            if (iniTmp.Exists)
            {
                // It means that application crashed. Restore ini from temp.
                if (!CopyFile(iniTmp.FullName, SettingManager.IniFileName)) return false;
            }
            else
            {
                // Create temp file to store original settings.
                if (!CopyFile(SettingManager.IniFileName, SettingManager.TmpFileName)) return false;
            }
            // Set status labels.
            StatusIsAdminLabel.Text = Win32.WinAPI.IsVista
                ? string.Format("Elevated: {0}", Win32.WinAPI.IsElevated())
                : "";
            StatusIniLabel.Text = SettingManager.IniFileName;
            return true;
        }

        void CheckEncoding(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            var sr = new StreamReader(path, true);
            var content = sr.ReadToEnd();
            sr.Close();
            if (sr.CurrentEncoding != System.Text.Encoding.Unicode)
            {
                System.IO.File.WriteAllText(path, content, System.Text.Encoding.Unicode);
            }
        }

        bool IsFileSame(string fileName)
        {
            return false;
            //if (!System.IO.File.Exists(fileName)) return false;
            //var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //StreamReader sr;
            //// Get MD5 of file on the disk.
            //sr = new StreamReader(fileName);
            //var dMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
            //// Get MD5 of resource file.
            //if (fileName == dllFile0) fileName = dllFile;
            //if (fileName == dllFile1) fileName = dllFile;
            //if (fileName == dllFile2) fileName = dllFile;
            //if (fileName == dllFile3) fileName = dllFile;
            //var assembly = Assembly.GetExecutingAssembly();
            //sr = new StreamReader(assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName));
            //var rMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
            //// return result.
            //return rMd5.Equals(dMd5);
        }

        bool CopyFile(string sourceFileName, string destFileName)
        {
            try
            {
                File.Copy(sourceFileName, destFileName, true);
            }
            catch (Exception)
            {
                Elevate();
                return false;
            }
            return true;
        }

        bool CreateFile(string fileName, Version newVersion)
        {
            DialogResult answer;
            var form = new MessageBoxForm();
            form.StartPosition = FormStartPosition.CenterParent;
            if (newVersion == null)
            {
                answer = form.ShowForm(
                    string.Format("'{0}' was not found.\r\nThis file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", new System.IO.FileInfo(fileName).FullName),
                    string.Format("'{0}' was not found.", fileName),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            }
            else
            {
                answer = form.ShowForm(
                    string.Format("New version of this file is available:\r\n{0}\r\n\r\nOld version: {1}\r\nNew version: {2}\r\n\r\nDo you want to update this file?", new System.IO.FileInfo(fileName).FullName, dllVersion, newVersion),
                    string.Format("New version of '{0}' file is available.", fileName),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            }
            if (answer == DialogResult.Yes)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var sr = assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName);
                FileStream sw = null;
                try
                {
                    sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                }
                catch (Exception)
                {
                    Elevate();
                    return false;
                }
                var buffer = new byte[1024];
                while (true)
                {
                    var count = sr.Read(buffer, 0, buffer.Length);
                    if (count == 0) break;
                    sw.Write(buffer, 0, count);
                }
                sr.Close();
                sw.Close();
            }
            return true;
        }

        void Elevate()
        {
            // If this is Vista/7 and is not elevated then elevate.
            if (x360ce.App.Win32.WinAPI.IsVista && !x360ce.App.Win32.WinAPI.IsElevated()) x360ce.App.Win32.WinAPI.RunElevated();
        }

        #endregion

        #region WebService loading circle

        public bool LoadingCircle
        {
            get { return BusyLoadingCircle.Active; }
            set
            {
                if (value)
                {
                    BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
                    BusyLoadingCircle.InnerCircleRadius = 12;
                    BusyLoadingCircle.NumberSpoke = 100;
                    BusyLoadingCircle.OuterCircleRadius = 18;
                    BusyLoadingCircle.RotationSpeed = 10;
                    BusyLoadingCircle.SpokeThickness = 3;
                    //this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
                    BusyLoadingCircle.Active = value;
                    BusyLoadingCircle.Visible = value;
                }
                else
                {
                    LoadinngCircleTimeout.Enabled = true;
                }
            }
        }

        void LoadinngCircleTimeout_Tick(object sender, EventArgs e)
        {
            LoadinngCircleTimeout.Enabled = false;
            BusyLoadingCircle.Active = false;
            BusyLoadingCircle.Visible = false;
        }

        #endregion

        void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            InternetAutoloadCheckBox.Enabled = InternetCheckBox.Checked;
        }

        void CreateDllFile(bool create, string file)
        {
            if (create)
            {
                if (!System.IO.File.Exists(file)) System.IO.File.Copy(dllFile, file, true);
            }
            else
            {
                if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            }
        }

        #region Allow only one copy of Application at a time

        /// <summary>Stores the unique windows message id from the RegisterWindowMessage call.</summary>
        int _WindowMessage;
        /// <summary>Used to determine if the application is already open.</summary>
        System.Threading.Mutex _Mutex;

        public const int wParam_Restore = 1;
        public const int wParam_Close = 2;

        /// <summary>
        /// Broadcast message to other instances of this application.
        /// </summary>
        /// <param name="wParam">Send parameter to other instances of this application.</param>
        /// <returns>True - other instances exists; False - other instances doesn't exist.</returns>
        public bool BroadcastMessage(int wParam)
        {
            // Check for previous instance of this app.
            var uid = Application.ProductName;
            _Mutex = new System.Threading.Mutex(false, uid);
            // Register the windows message
            _WindowMessage = Win32.NativeMethods.RegisterWindowMessage(uid);
            var firsInstance = _Mutex.WaitOne(1, true);
            // If this is not the first instance then...
            if (!firsInstance)
            {
                // Brodcast a message with parameters to another instance.
                var recipients = (int)Win32.BSM.BSM_APPLICATIONS;
                var flags = Win32.BSF.BSF_IGNORECURRENTTASK | Win32.BSF.BSF_POSTMESSAGE;
                var ret = Win32.NativeMethods.BroadcastSystemMessage((int)flags, ref recipients, _WindowMessage, wParam, 0);
            }
            return !firsInstance;
        }

        /// <summary>
        /// NOTE you must be careful with this method. This is handeling all the
        /// windows messages that are coming to the form...
        /// </summary>
        /// <param name="m"></param>
        /// <remarks>This overrides the windows messaging processing</remarks>
        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            // If message value was found then...
            if (m.Msg == _WindowMessage)
            {
                // Show currently running instance.
                if (m.WParam.ToInt32() == wParam_Restore)
                {
                    // Note: FormWindowState.Minimized and FormWindowState.Normal was used to make sure that Activate() wont fail because of this:
                    // Windows NT 5.0 and later: An application cannot force a window to the foreground while the user is working with another window.
                    // Instead, SetForegroundWindow will activate the window (see SetActiveWindow) and call theFlashWindowEx function to notify the user.
                    if (WindowState != FormWindowState.Minimized) WindowState = FormWindowState.Minimized;
                    this.Activate();
                    if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
                }
                //  Close currently running instance.
                if (m.WParam.ToInt32() == wParam_Close) Close();
            }
            // Let the normal windows messaging process it.
            base.DefWndProc(ref m);
        }

        #endregion

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                _Mutex.Dispose();
                Manager.Dispose();
                Manager = null;
                components.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}
