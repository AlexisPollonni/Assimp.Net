﻿/*
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

namespace Assimp.Unmanaged;

internal sealed partial class UnmanagedMacLibraryImplementation : UnmanagedLibraryImplementation
{
    public override string DllExtension => ".dylib";

    public override string DllPrefix => "lib";

    public UnmanagedMacLibraryImplementation(string defaultLibName, Type[] unmanagedFunctionDelegateTypes)
        : base(defaultLibName, unmanagedFunctionDelegateTypes)
    {
    }

    protected override nint NativeLoadLibrary(string path)
    {
        var libraryHandle = dlopen(path, RTLD_NOW);

        if(libraryHandle == nint.Zero && ThrowOnLoadFailure)
        {
            var errPtr = dlerror();
            var msg = Marshal.PtrToStringAnsi(errPtr);
            if(!string.IsNullOrEmpty(msg))
                throw new AssimpException($"Error loading unmanaged library from path: {path}\n\n{msg}");
            throw new AssimpException($"Error loading unmanaged library from path: {path}");
        }

        return libraryHandle;
    }

    protected override nint NativeGetProcAddress(nint handle, string functionName)
    {
        return dlsym(handle, functionName);
    }

    protected override void NativeFreeLibrary(nint handle)
    {
        dlclose(handle);
    }

    #region Native Methods

    [LibraryImport("libSystem.B.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint dlopen(string fileName, int flags);

    [LibraryImport("libSystem.B.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint dlsym(nint handle, string functionName);

    [LibraryImport("libSystem.B.dylib")]
    private static partial int dlclose(nint handle);

    [LibraryImport("libSystem.B.dylib")]
    private static partial nint dlerror();

    private const int RTLD_NOW = 2;

    #endregion
}