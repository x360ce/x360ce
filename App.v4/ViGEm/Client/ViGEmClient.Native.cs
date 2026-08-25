using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Nefarius.ViGEm.Client
{
    using PVIGEM_CLIENT = IntPtr;
    using PVIGEM_TARGET = IntPtr;
    using PVIGEM_TARGET_ADD_RESULT = IntPtr;

    [SuppressUnmanagedCodeSecurity]
    partial class ViGEmClient
    {

        internal delegate void PVIGEM_X360_NOTIFICATION(
            PVIGEM_CLIENT Client,
            PVIGEM_TARGET Target,
            byte LargeMotor,
            byte SmallMotor,
            byte LedNumber);

        internal class NativeMethods
        {

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern PVIGEM_CLIENT vigem_alloc();

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void vigem_free(PVIGEM_CLIENT vigem);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern VIGEM_ERROR vigem_connect(PVIGEM_CLIENT vigem);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void vigem_disconnect(PVIGEM_CLIENT vigem);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void vigem_target_free(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern VIGEM_ERROR vigem_target_add(PVIGEM_CLIENT vigem, PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern VIGEM_ERROR vigem_target_add_async(PVIGEM_CLIENT vigem, PVIGEM_TARGET target, PVIGEM_TARGET_ADD_RESULT result);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern VIGEM_ERROR vigem_target_remove(PVIGEM_CLIENT vigem, PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void vigem_target_set_vid(PVIGEM_TARGET target, ushort vid);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void vigem_target_set_pid(PVIGEM_TARGET target, ushort pid);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            static extern ushort vigem_target_get_vid(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            static extern ushort vigem_target_get_pid(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            static extern uint vigem_target_get_index(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            static extern VIGEM_TARGET_TYPE vigem_target_get_type(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            static extern bool vigem_target_is_attached(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern VIGEM_ERROR vigem_target_x360_update(PVIGEM_CLIENT vigem, PVIGEM_TARGET target, XUSB_REPORT report);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern VIGEM_ERROR vigem_target_x360_register_notification(PVIGEM_CLIENT vigem, PVIGEM_TARGET target, PVIGEM_X360_NOTIFICATION notification);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void vigem_target_x360_unregister_notification(PVIGEM_TARGET target);

            [DllImport("vigemclient.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern PVIGEM_TARGET vigem_target_x360_alloc();

        }

    }
}
