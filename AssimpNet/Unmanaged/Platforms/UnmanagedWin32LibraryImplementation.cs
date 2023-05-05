/*
* Copyright (c) 2012-2020 AssimpNet - Nicholas Woodfield
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
*/

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Assimp.Unmanaged;

internal sealed partial class UnmanagedWin32LibraryImplementation : UnmanagedLibraryImplementation
{
    public override string DllExtension => ".dll";

    public UnmanagedWin32LibraryImplementation(string defaultLibName, Type[] unmanagedFunctionDelegateTypes)
        : base(defaultLibName, unmanagedFunctionDelegateTypes)
    {
    }

    protected override nint NativeLoadLibrary(string path)
    {
        var libraryHandle = WinNativeLoadLibrary(path);

        if(libraryHandle == nint.Zero && ThrowOnLoadFailure)
        {
            Exception innerException = null;

            //Keep the try-catch in case we're running on Mono. We're providing our own implementation of "Marshal.GetHRForLastWin32Error" which is NOT implemented
            //in mono, but let's just be cautious.
            try
            {
                var hr = GetHRForLastWin32Error();
                innerException = Marshal.GetExceptionForHR(hr);
            }
            catch(Exception) { }

            if(innerException != null)
                throw new AssimpException(
                    $"Error loading unmanaged library from path: {path}\n\n{innerException.Message}", innerException);
            throw new AssimpException($"Error loading unmanaged library from path: {path}");
        }

        return libraryHandle;
    }

    protected override nint NativeGetProcAddress(nint handle, string functionName)
    {
        return GetProcAddress(handle, functionName);
    }

    protected override void NativeFreeLibrary(nint handle)
    {
        FreeLibrary(handle);
    }

    private int GetHRForLastWin32Error()
    {
        //Mono, for some reason, throws in Marshal.GetHRForLastWin32Error(), but it should implement GetLastWin32Error, which is recommended than
        //p/invoking it ourselves when SetLastError is set in DllImport
        var dwLastError = Marshal.GetLastWin32Error();

        if((dwLastError & 0x80000000) == 0x80000000)
            return dwLastError;
        return (dwLastError & 0x0000FFFF) | unchecked((int) 0x80070000);
    }

    #region Native Methods

    [LibraryImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    private static partial nint WinNativeLoadLibrary(string fileName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeLibrary(nint hModule);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetProcAddress(nint hModule, string procName);

    #endregion
}