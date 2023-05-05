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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Assimp.Unmanaged;

internal abstract class UnmanagedLibraryImplementation : IDisposable
{
    private readonly Type[] m_unmanagedFunctionDelegateTypes;
    private readonly Dictionary<string, Delegate> m_nameToUnmanagedFunction;
    private nint m_libraryHandle;

    public bool IsLibraryLoaded => m_libraryHandle != nint.Zero;

    public bool IsDisposed { get; private set; }

    public string DefaultLibraryName { get; }

    public bool ThrowOnLoadFailure { get; set; }

    public abstract string DllExtension { get; }

    public virtual string DllPrefix => string.Empty;

    public UnmanagedLibraryImplementation(string defaultLibName, Type[] unmanagedFunctionDelegateTypes)
    {
        DefaultLibraryName = DllPrefix + Path.ChangeExtension(defaultLibName, DllExtension);

        m_unmanagedFunctionDelegateTypes = unmanagedFunctionDelegateTypes;

        m_nameToUnmanagedFunction = new();
        IsDisposed = false;
        m_libraryHandle = nint.Zero;

        ThrowOnLoadFailure = true;
    }

    ~UnmanagedLibraryImplementation()
    {
        Dispose(false);
    }

    public T GetFunction<T>(string functionName) where T : class
    {
        if(string.IsNullOrEmpty(functionName))
            return null;

        if(!m_nameToUnmanagedFunction.TryGetValue(functionName, out var function))
            return null;

        var obj = (object) function;

        return (T) obj;
    }

    public bool LoadLibrary(string path)
    {
        FreeLibrary(true);

        m_libraryHandle = NativeLoadLibrary(path);

        if(m_libraryHandle != nint.Zero)
            LoadFunctions();

        return m_libraryHandle != nint.Zero;
    }

    public bool FreeLibrary()
    {
        return FreeLibrary(true);
    }

    private bool FreeLibrary(bool clearFunctions)
    {
        if(m_libraryHandle != nint.Zero)
        {
            NativeFreeLibrary(m_libraryHandle);
            m_libraryHandle = nint.Zero;

            if(clearFunctions)
                m_nameToUnmanagedFunction.Clear();

            return true;
        }

        return false;
    }

    private void LoadFunctions()
    {
        foreach(var funcType in m_unmanagedFunctionDelegateTypes)
        {
            var funcName = GetUnmanagedName(funcType);
            if(string.IsNullOrEmpty(funcName))
            {
                Debug.Assert(false,
                    $"No UnmanagedFunctionNameAttribute on {funcType.AssemblyQualifiedName} type.");
                continue;
            }

            var procAddr = NativeGetProcAddress(m_libraryHandle, funcName);
            if(procAddr == nint.Zero)
            {
                Debug.Assert(false,
                    $"No unmanaged function found for {funcType.AssemblyQualifiedName} type.");
                continue;
            }

            if(!m_nameToUnmanagedFunction.TryGetValue(funcName, out var function))
            {
                function = PlatformHelper.GetDelegateForFunctionPointer(procAddr, funcType);
                m_nameToUnmanagedFunction.Add(funcName, function);
            }
        }
    }

    private string GetUnmanagedName(Type funcType)
    {
        var attributes = PlatformHelper.GetCustomAttributes(funcType, typeof(UnmanagedFunctionNameAttribute), false);
        foreach(var attr in attributes)
        {
            if(attr is UnmanagedFunctionNameAttribute attribute)
                return attribute.UnmanagedFunctionName;
        }

        return null;
    }

    protected abstract nint NativeLoadLibrary(string path);
    protected abstract void NativeFreeLibrary(nint handle);
    protected abstract nint NativeGetProcAddress(nint handle, string functionName);

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if(!IsDisposed)
        {
            FreeLibrary(isDisposing);

            IsDisposed = true;
        }
    }
}