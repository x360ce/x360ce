namespace Microsoft.Xna.Framework
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    internal static class Helpers
    {
        //public const uint DiscardedRenderTargetClearColor = 0xff442288;
        //public const int Guide_MessageBox_MaxButtons = 3;
        //public const uint InvalidHandle = uint.MaxValue;
        //public const int MaximumStringLength = 260;
        //public const int MaxNumberOfSignedInPlayers = 1;

        //public static byte[] CheckAndReadStream(Stream stream, int numberBytes, out int bytesRead)
        //{
        //    int num2;
        //    bool canRead = stream.CanRead;
        //    int count = 0;
        //    if (numberBytes == -1)
        //    {
        //        count = (int) (stream.Length - stream.Position);
        //    }
        //    else
        //    {
        //        count = numberBytes;
        //    }
        //    bytesRead = count;
        //    byte[] buffer = new byte[count];
        //    for (int i = 0; count > 0; i += num2)
        //    {
        //        num2 = stream.Read(buffer, i, count);
        //        if (num2 <= 0)
        //        {
        //            throw new EndOfStreamException(FrameworkResources.DidNotReadEnoughData);
        //        }
        //        count -= num2;
        //    }
        //    return buffer;
        //}

        //public static void CheckDisposed(object obj, IntPtr pComPtr)
        //{
        //    if (pComPtr == IntPtr.Zero)
        //    {
        //        throw new ObjectDisposedException(obj.GetType().Name);
        //    }
        //}

        //public static bool Failed(ErrorCodes error)
        //{
        //    return (error < ErrorCodes.Success);
        //}

        //public static bool Failed(int error)
        //{
        //    return (error < 0);
        //}

        //public static Exception GetExceptionFromResult(uint result)
        //{
        //    switch (result)
        //    {
        //        case 0x80040256:
        //            return new InvalidOperationException(FrameworkResources.NoAudioPlaybackDevicesFound);

        //        case 0x80070005:
        //            return new UnauthorizedAccessException();

        //        case 0x8007000e:
        //            throw new OutOfMemoryException();

        //        case 0x80004001:
        //            return new NotImplementedException();

        //        case 0x80004004:
        //            throw new InvalidOperationException(FrameworkResources.ResourceInUse);

        //        case 0x88760818:
        //            return new ArgumentException(FrameworkResources.WrongTextureFormat);

        //        case 0x88760819:
        //            return new ArgumentException(FrameworkResources.UnsupportedColorOperation);

        //        case 0x8876081a:
        //            return new ArgumentException(FrameworkResources.UnsupportedColorArg);

        //        case 0x8876081b:
        //            return new ArgumentException(FrameworkResources.UnsupportedAlphaOperation);

        //        case 0x8876081d:
        //            return new InvalidOperationException(FrameworkResources.TooManyOperations);

        //        case 0x8876081e:
        //            return new ArgumentException(FrameworkResources.ConflictingTextureFilter);

        //        case 0x8876081f:
        //            return new ArgumentException(FrameworkResources.UnsupportedFactorValue);

        //        case 0x88760821:
        //            return new ArgumentException(FrameworkResources.ConflictingRenderState);

        //        case 0x88760822:
        //            return new ArgumentException(FrameworkResources.UnsupportedTextureFilter);

        //        case 0x88760827:
        //            return new DriverInternalErrorException();

        //        case 0x8876017c:
        //            return new OutOfVideoMemoryException();

        //        case 0x80070057:
        //            return new ArgumentException();

        //        case 0x88760866:
        //            return new ArgumentException(FrameworkResources.NotFound);

        //        case 0x88760867:
        //            return new ArgumentException(FrameworkResources.MoreData);

        //        case 0x88760868:
        //            return new DeviceLostException();

        //        case 0x88760869:
        //            return new DeviceNotResetException();

        //        case 0x88760870:
        //            return new DeviceNotSupportedException();

        //        case 0x88760871:
        //            return new ArgumentException(FrameworkResources.InvalidDeviceType);

        //        case 0x88760872:
        //            return new InvalidOperationException(FrameworkResources.InvalidCall);

        //        case 0x8ac70003:
        //            return new InvalidOperationException(FrameworkResources.Expired);

        //        case 0x8ac70006:
        //            return new InvalidOperationException(FrameworkResources.InvalidUsage);

        //        case 0x8ac70007:
        //            return new ArgumentException(FrameworkResources.InvalidContentVersion);

        //        case 0x8ac70008:
        //        case 0x8bad0001:
        //            return new InstancePlayLimitException();

        //        case 0x8ac7000a:
        //            return new IndexOutOfRangeException(FrameworkResources.InvalidVariableIndex);

        //        case 0x8ac7000b:
        //            return new ArgumentException(FrameworkResources.InvalidCategory);

        //        case 0x8ac7000c:
        //            return new IndexOutOfRangeException(FrameworkResources.InvalidCue);

        //        case 0x8ac7000d:
        //            return new IndexOutOfRangeException(FrameworkResources.InvalidWaveIndex);

        //        case 0x8ac7000e:
        //            return new IndexOutOfRangeException(FrameworkResources.InvalidTrackIndex);

        //        case 0x8ac7000f:
        //            return new IndexOutOfRangeException(FrameworkResources.InvalidSoundOffsetOrIndex);

        //        case 0x8ac70010:
        //            return new IOException(FrameworkResources.XactReadFile);

        //        case 0x8ac70012:
        //            return new InvalidOperationException(FrameworkResources.InCallback);

        //        case 0x8ac70013:
        //            return new InvalidOperationException(FrameworkResources.NoWaveBank);

        //        case 0x8ac70014:
        //            return new InvalidOperationException(FrameworkResources.SelectVariation);

        //        case 0x8ac70016:
        //            return new InvalidOperationException(FrameworkResources.WaveBankNotPrepared);

        //        case 0x8ac70017:
        //            return new NoAudioHardwareException();

        //        case 0x8ac70018:
        //            return new ArgumentException(FrameworkResources.InvalidEntryCount);

        //        case 0:
        //            return null;
        //    }
        //    return new InvalidOperationException(FrameworkResources.UnexpectedError);
        //}

        //public static uint GetSizeOf<T>() where T: struct
        //{
        //    return (uint) Marshal.SizeOf(typeof(T));
        //}

        public static unsafe int SmartGetHashCode(object obj)
        {
            int num3;
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            try
            {
                int num4 = Marshal.SizeOf(obj);
                int num2 = 0;
                int num = 0;
                for (int* numPtr = (int*)handle.AddrOfPinnedObject().ToPointer(); (num2 + 4) <= num4; numPtr++)
                {
                    num ^= numPtr[0];
                    num2 += 4;
                }
                num3 = (num == 0) ? 0x7fffffff : num;
            }
            finally
            {
                handle.Free();
            }
            return num3;
        }

        //public static bool Succeeded(ErrorCodes error)
        //{
        //    return (error >= ErrorCodes.Success);
        //}

        //public static bool Succeeded(int error)
        //{
        //    return (error >= 0);
        //}

        //public static void ThrowExceptionFromErrorCode(ErrorCodes error)
        //{
        //    if (Failed(error))
        //    {
        //        throw GetExceptionFromResult((uint) error);
        //    }
        //}

        //public static void ThrowExceptionFromErrorCode(int error)
        //{
        //    if (Failed(error))
        //    {
        //        throw GetExceptionFromResult((uint) error);
        //    }
        //}

        //public static void ThrowExceptionFromResult(uint result)
        //{
        //    if (result != 0)
        //    {
        //        throw GetExceptionFromResult(result);
        //    }
        //}

        //public static void ValidateCopyParameters<T>(T[] data, int dataIndex, int elementCount) where T: struct
        //{
        //    if (dataIndex >= 0)
        //    {
        //        int num1 = data.Length;
        //    }
        //    int length = data.Length;
        //    int num3 = elementCount + dataIndex;
        //}
    }
}

