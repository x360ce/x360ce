using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace x360ce.App.Recording
{
	/// <summary>What a recording says about a window that stops answering, and about the PC while it records.</summary>
	/// <remarks>
	/// A window that stops answering says nothing about why. So the recording names the call the interface thread is in,
	/// the last window message it took, whether it uses the processor or waits, and what Windows sees it wait on. Every
	/// ten seconds it notes the PC's free memory, its load, .NET's memory clean-ups and the power source, and its own
	/// thread notes waking late, which happens when the whole PC is held up rather than x360ce alone. All of it is code
	/// names and numbers: nothing that names the person or their files.
	/// </remarks>
	public sealed partial class ReportRecorder
	{
		#region What the interface thread runs

		/// <summary>The posted call the interface thread runs now, as the delegate or task it was posted as; null between calls.</summary>
		/// <remarks>Written by the interface thread around each call, read by the writer only when the window stops answering.</remarks>
		static object _uiWork;

		/// <summary>The last window message the interface thread took from its queue.</summary>
		static int _uiMessage;

		/// <summary>Runs every call posted to the interface thread through Windows Forms' own context, and keeps which one runs.</summary>
		/// <remarks>
		/// Installed on the interface thread before the main window is made, as the posting helpers take the context the
		/// first time they are used. Windows Forms puts its own context in place only where there is none or a plain one,
		/// so this one stays. A call costs two field writes more than it did.
		/// </remarks>
		public sealed class WatchedContext : SynchronizationContext
		{
			static readonly SendOrPostCallback RunCall = Run;

			/// <summary>Windows Forms' context for the interface thread, which runs the calls.</summary>
			readonly WindowsFormsSynchronizationContext _forms;

			/// <summary>Makes the context for the thread it is made on, which must be the interface thread.</summary>
			public WatchedContext()
			{
				_forms = new WindowsFormsSynchronizationContext();
			}

			public override void Post(SendOrPostCallback d, object state)
			{
				_forms.Post(RunCall, new Call(d, state));
			}

			public override void Send(SendOrPostCallback d, object state)
			{
				_forms.Send(RunCall, new Call(d, state));
			}

			/// <summary>The same context: it belongs to the interface thread, and a copy would run the calls unwatched.</summary>
			public override SynchronizationContext CreateCopy()
			{
				return this;
			}

			static void Run(object o)
			{
				var call = (Call)o;
				var was = Volatile.Read(ref _uiWork);
				Volatile.Write(ref _uiWork, call.State as Task ?? (object)call.Callback);
				try
				{
					call.Callback(call.State);
				}
				finally
				{
					Volatile.Write(ref _uiWork, was);
				}
			}

			sealed class Call
			{
				public Call(SendOrPostCallback callback, object state)
				{
					Callback = callback;
					State = state;
				}

				public readonly SendOrPostCallback Callback;
				public readonly object State;
			}
		}

		/// <summary>Keeps the last message the interface thread takes from its queue, and lets every message through.</summary>
		sealed class MessageWatch : IMessageFilter
		{
			public bool PreFilterMessage(ref Message m)
			{
				_uiMessage = m.Msg;
				return false;
			}
		}

		/// <summary>Whether the interface thread's messages are watched: from the first recording on, as only that thread can stop watching them.</summary>
		static bool _watchingMessages;

		/// <summary>Starts watching what the interface thread does. Interface thread.</summary>
		void WatchInterfaceThread()
		{
			_uiThreadId = GetCurrentThreadId();
			if (_uiThread == IntPtr.Zero)
				_uiThread = OpenThread(ThreadQueryLimitedInformation, false, _uiThreadId);
			if (_watchingMessages)
				return;
			_watchingMessages = true;
			Application.AddMessageFilter(new MessageWatch());
		}

		/// <summary>The native id of the interface thread, and a handle to read its processor time with.</summary>
		static int _uiThreadId;
		static IntPtr _uiThread;

		/// <summary>The interface thread's processor time when the window stopped answering, in 100 ns units.</summary>
		long _stallCpuStart;

		/// <summary>When the window was last described while it does not answer, in timestamp ticks.</summary>
		long _stallNoted;

		/// <summary>Says what the interface thread is in while the window does not answer. Writer thread.</summary>
		/// <param name="first">True when the window has just stopped answering: the processor time is counted from here.</param>
		/// <param name="stalled">How long the window has not answered, in timestamp ticks.</param>
		string DescribeStall(bool first, long stalled)
		{
			var cpu = ThreadCpu(_uiThread);
			if (first)
				_stallCpuStart = cpu;
			var parts = new System.Collections.Generic.List<string>();
			var work = WorkName(Volatile.Read(ref _uiWork));
			parts.Add(work == null ? "in no posted call" : "in " + work);
			parts.Add("last message " + MessageName(_uiMessage));
			if (cpu >= 0 && _stallCpuStart >= 0)
			{
				var used = (cpu - _stallCpuStart) / 10000.0 / 1000.0;
				// Busy when it used most of the time it did not answer; a thread that waits uses next to none.
				var seconds = (double)stalled / Stopwatch.Frequency;
				parts.Add("its thread used " + used.ToString("0.0", CultureInfo.InvariantCulture) + " s of processor time since"
					+ (first ? "" : used > seconds / 2 ? " (busy)" : " (waiting)"));
			}
			var wait = WaitChain(_uiThreadId);
			if (wait != null)
				parts.Add(wait);
			return string.Join("; ", parts.ToArray());
		}

		/// <summary>The method a posted call runs, as Type.Method, looking through the helpers that wrap it; null for none.</summary>
		/// <remarks>Read by reflection, and only when the window does not answer.</remarks>
		static string WorkName(object work)
		{
			if (work == null)
				return null;
			var d = work as Delegate;
			var task = work as Task;
			if (task != null)
				d = TaskActionField?.GetValue(task) as Delegate;
			for (var depth = 0; d != null && depth < 4; depth++)
			{
				var method = d.Method;
				var type = method.DeclaringType;
				// The posting helpers' own wrappers: the call they run is held by the wrapper's closure.
				var inner = Inner(d.Target);
				if (inner != null && IsWrapper(type))
				{
					d = inner;
					continue;
				}
				return CodeName(type, method.Name);
			}
			return work.GetType().Name;
		}

		static readonly FieldInfo TaskActionField = typeof(Task).GetField("m_action", BindingFlags.Instance | BindingFlags.NonPublic);

		static bool IsWrapper(Type type)
		{
			while (type != null && type.DeclaringType != null)
				type = type.DeclaringType;
			return type == typeof(JocysCom.ClassLibrary.Controls.ControlsHelper);
		}

		/// <summary>The first delegate a closure holds, or null.</summary>
		static Delegate Inner(object target)
		{
			if (target == null)
				return null;
			foreach (var field in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				var d = field.GetValue(target) as Delegate;
				if (d != null)
					return d;
			}
			return null;
		}

		/// <summary>Type.Method, with a compiler-made lambda named after the method it was written in.</summary>
		static string CodeName(Type type, string method)
		{
			while (type != null && type.DeclaringType != null && type.Name.StartsWith("<", StringComparison.Ordinal))
				type = type.DeclaringType;
			if (method.StartsWith("<", StringComparison.Ordinal))
			{
				var end = method.IndexOf('>');
				method = (end > 1 ? method.Substring(1, end - 1) : method) + " (a lambda in it)";
			}
			return (type == null ? "" : type.Name + ".") + method;
		}

		static string MessageName(int message)
		{
			switch (message)
			{
				case 0: return "none yet";
				case 0x000F: return "WM_PAINT";
				case 0x0014: return "WM_ERASEBKGND";
				case 0x001A: return "WM_SETTINGCHANGE";
				case 0x0100: return "WM_KEYDOWN";
				case 0x0113: return "WM_TIMER";
				case 0x0200: return "WM_MOUSEMOVE";
				case 0x0201: return "WM_LBUTTONDOWN";
				case 0x0202: return "WM_LBUTTONUP";
				case 0x0219: return "a device change";
				case 0x0312: return "WM_HOTKEY";
				case 0x031A: return "WM_THEMECHANGED";
			}
			return message >= 0xC000
				// Windows Forms runs posted calls on a message it registers.
				? "a registered message 0x" + message.ToString("X4", CultureInfo.InvariantCulture) + " (a posted call)"
				: "0x" + message.ToString("X4", CultureInfo.InvariantCulture);
		}

		const int ThreadQueryLimitedInformation = 0x0800;

		[DllImport("kernel32.dll")]
		static extern int GetCurrentThreadId();

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr OpenThread(int access, bool inherit, int threadId);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetThreadTimes(IntPtr thread, out long creation, out long exit, out long kernel, out long user);

		/// <summary>A thread's processor time in 100 ns units, or -1 when Windows does not give it.</summary>
		static long ThreadCpu(IntPtr thread)
		{
			long creation, exit, kernel, user;
			if (thread == IntPtr.Zero || !GetThreadTimes(thread, out creation, out exit, out kernel, out user))
				return -1;
			return kernel + user;
		}

		#endregion

		#region What Windows sees the thread wait on

		[StructLayout(LayoutKind.Explicit, Size = 280)]
		struct WaitNode
		{
			[FieldOffset(0)] public int ObjectType;
			[FieldOffset(4)] public int ObjectStatus;
			[FieldOffset(8)] public int ProcessId;
			[FieldOffset(12)] public int ThreadId;
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern IntPtr OpenThreadWaitChainSession(int flags, IntPtr callback);

		[DllImport("advapi32.dll")]
		static extern void CloseThreadWaitChainSession(IntPtr session);

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool GetThreadWaitChain(IntPtr session, IntPtr context, int flags, int threadId, ref int nodeCount, [Out] WaitNode[] nodes, out bool isCycle);

		const int ThreadNode = 8;
		const int StatusBlocked = 3;

		/// <summary>What Windows sees the thread wait on, in order, or null when it does not say.</summary>
		/// <remarks>
		/// Windows names the waits it knows: a critical section, a window message sent to another thread, a mutex, a call
		/// to a Windows service, COM, a thread or a process. A .NET lock or event is not among them, so a thread blocked on
		/// one shows as blocked on nothing named. Windows also says when the waits go round in a circle: a deadlock.
		/// </remarks>
		static string WaitChain(int threadId)
		{
			if (threadId == 0)
				return null;
			var session = OpenThreadWaitChainSession(0, IntPtr.Zero);
			if (session == IntPtr.Zero)
				return null;
			try
			{
				var nodes = new WaitNode[16];
				var count = nodes.Length;
				bool cycle;
				if (!GetThreadWaitChain(session, IntPtr.Zero, 0, threadId, ref count, nodes, out cycle) || count < 1)
					return null;
				var self = Process.GetCurrentProcess().Id;
				var parts = new System.Collections.Generic.List<string>();
				for (var i = 1; i < count && i < nodes.Length; i++)
				{
					var node = nodes[i];
					parts.Add(node.ObjectType == ThreadNode
						? (node.ProcessId == self ? "an x360ce thread" : "a thread of another program")
						: WaitName(node.ObjectType));
				}
				var text = parts.Count == 0
					? (nodes[0].ObjectStatus == StatusBlocked ? "Windows sees it blocked on nothing it can name (a .NET lock or event)" : "Windows sees it running")
					: "Windows sees it wait on " + string.Join(", held by ", parts.ToArray());
				return cycle ? text + ", in a circle: a deadlock" : text;
			}
			finally
			{
				CloseThreadWaitChainSession(session);
			}
		}

		static string WaitName(int type)
		{
			switch (type)
			{
				case 1: return "a critical section";
				case 2: return "a window message sent to another thread";
				case 3: return "a mutex";
				case 4: return "a call to a Windows service";
				case 5: return "COM";
				case 6: return "a thread";
				case 7: return "a process";
				case 9: return "COM activation";
				case 11: return "a network socket";
				case 12: return "a file share";
			}
			return "something Windows does not name";
		}

		#endregion

		#region The PC

		[StructLayout(LayoutKind.Sequential)]
		struct MemoryStatus
		{
			public int Length;
			public int MemoryLoad;
			public ulong TotalPhys;
			public ulong AvailPhys;
			public ulong TotalPageFile;
			public ulong AvailPageFile;
			public ulong TotalVirtual;
			public ulong AvailVirtual;
			public ulong AvailExtendedVirtual;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

		[StructLayout(LayoutKind.Sequential)]
		struct PowerStatus
		{
			public byte ACLineStatus;
			public byte BatteryFlag;
			public byte BatteryLifePercent;
			public byte SystemStatusFlag;
			public int BatteryLifeTime;
			public int BatteryFullLifeTime;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetSystemPowerStatus(out PowerStatus status);

		/// <summary>Device changes Windows announced to the main window, counted there and read every ten seconds.</summary>
		static int _deviceChanges;

		/// <summary>Counts a device change Windows announced to the main window. Interface thread.</summary>
		public static void CountDeviceChange()
		{
			Interlocked.Increment(ref _deviceChanges);
		}

		long _pcTicks;
		long _pcIdle = -1;
		long _pcBusy;
		long _pcProcess;
		int _pcDeviceChanges;
		readonly int[] _pcCollections = new int[3];

		/// <summary>The PC's free memory, load, .NET clean-ups, power and device changes since the last sample. Writer thread.</summary>
		/// <remarks>Memory short of what is asked makes Windows page to disk, and a PC paging can hold every program up for seconds.</remarks>
		string DescribePc(Process process)
		{
			var now = Stopwatch.GetTimestamp();
			var parts = new System.Collections.Generic.List<string>();
			var memory = new MemoryStatus { Length = Marshal.SizeOf(typeof(MemoryStatus)) };
			if (GlobalMemoryStatusEx(ref memory))
				parts.Add("memory " + Gb(memory.AvailPhys) + " of " + Gb(memory.TotalPhys) + " GB free, committed "
					+ Gb(memory.TotalPageFile - memory.AvailPageFile) + " of " + Gb(memory.TotalPageFile) + " GB");
			long idle, kernel, user;
			var cpu = process.TotalProcessorTime.Ticks;
			if (GetSystemTimes(out idle, out kernel, out user) && _pcIdle >= 0)
			{
				var busy = kernel + user;
				var total = busy - _pcBusy;
				var idleDelta = idle - _pcIdle;
				var wall = (double)(now - _pcTicks) / Stopwatch.Frequency * 10000000.0 * Environment.ProcessorCount;
				if (total > 0 && wall > 0)
					parts.Add("processors " + Percent((total - idleDelta) / (double)total) + " busy, x360ce " + Percent((cpu - _pcProcess) / wall));
			}
			_pcIdle = idle;
			_pcBusy = kernel + user;
			_pcProcess = cpu;
			_pcTicks = now;
			var clean = new string[3];
			for (var g = 0; g < 3; g++)
			{
				var count = GC.CollectionCount(g);
				clean[g] = (count - _pcCollections[g]).ToString(CultureInfo.InvariantCulture);
				_pcCollections[g] = count;
			}
			parts.Add(".NET clean-ups " + string.Join("/", clean));
			PowerStatus power;
			if (GetSystemPowerStatus(out power))
				parts.Add(power.ACLineStatus == 1 ? "on mains"
					: power.ACLineStatus == 0 ? "on battery" + (power.BatteryLifePercent <= 100 ? " " + power.BatteryLifePercent + " %" : "")
					: "power source unknown");
			var changes = Volatile.Read(ref _deviceChanges);
			parts.Add((changes - _pcDeviceChanges).ToString(CultureInfo.InvariantCulture) + " device changes");
			_pcDeviceChanges = changes;
			return "PC: " + string.Join(", ", parts.ToArray());
		}

		static string Gb(ulong bytes)
		{
			return (bytes / 1024.0 / 1024 / 1024).ToString("0.0", CultureInfo.InvariantCulture);
		}

		static string Percent(double share)
		{
			return Math.Max(0, Math.Min(100, share * 100)).ToString("0", CultureInfo.InvariantCulture) + " %";
		}

		#endregion
	}
}
