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

using Silk.NET.Assimp;

namespace Assimp;

/// <summary>
/// Defines a stream to some file input or output source. This object is responsible for reading/writing data
/// that is used by Assimp.
/// </summary>
public abstract class IOStream : IDisposable
{
  // Don't delete these, holding onto the callbacks prevent them from being GC'ed inappropiately
  private AiFileReadProc m_writeProc;
  private AiFileReadProc m_readProc;
  private AiFileTellProc m_tellProc;
  private AiFileTellProc m_fileSizeProc;
  private AiFileSeek m_seekProc;
  private AiFileFlushProc m_flushProc;
  private byte[] m_byteBuffer;

  /// <summary>
  /// Gets whether or not this IOStream has been disposed.
  /// </summary>
  public bool IsDisposed { get; private set; }

  /// <summary>
  /// Gets the original path to file given by Assimp.
  /// </summary>
  public string PathToFile { get; }

  /// <summary>
  /// Gets the original desired file access mode.
  /// </summary>
  public FileIOMode FileMode { get; }

  /// <summary>
  /// Gets whether the stream is in fact valid - that is, the input/output has been
  /// properly located and can be read/written.
  /// </summary>
  public abstract bool IsValid
  {
    get;
  }

  internal unsafe AiFile* AiFile { get; private set; }

  /// <summary>
  /// Constructs a new IOStream.
  /// </summary>
  /// <param name="pathToFile">Path to file given by Assimp</param>
  /// <param name="fileMode">Desired file access mode</param>
  public IOStream(string pathToFile, FileIOMode fileMode) : this(pathToFile, fileMode, true) { }

  /// <summary>
  /// Constructs a new IOStream.
  /// </summary>
  /// <param name="pathToFile">Path to file given by Assimp</param>
  /// <param name="fileMode">Desired file access mode</param>
  /// <param name="initialize">True if initialize should be immediately called with the default callbacks. Set this to false
  /// if your subclass requires a different way to setup the function pointers.</param>
  protected unsafe IOStream(string pathToFile, FileIOMode fileMode, bool initialize = true)
  {
    PathToFile = pathToFile;
    FileMode = fileMode;

    if (initialize)
      Initialize(OnAiFileWriteProc, OnAiFileReadProc, OnAiFileTellProc, OnAiFileSizeProc, OnAiFileSeekProc, OnAiFileFlushProc);
  }

  /// <summary>
  /// Initializes the system by setting up native pointers for Assimp to the specified functions. A reference to each
  /// supplied callback is held on to avoid it being GC'ed.
  /// </summary>
  /// <param name="aiFileWriteProc">Handles write requests.</param>
  /// <param name="aiFileReadProc">Handles read requests.</param>
  /// <param name="aiFileTellProc">Handles tell requests.</param>
  /// <param name="aiFileSizeProc">Handles size requests.</param>
  /// <param name="aiFileSeek">Handles seek requests.</param>
  /// <param name="aiFileFlushProc">Handles flush requests.</param>
  /// <param name="userData">Additional user data, if any.</param>
  protected unsafe void Initialize(AiFileReadProc aiFileWriteProc, AiFileReadProc aiFileReadProc, AiFileTellProc aiFileTellProc, AiFileTellProc aiFileSizeProc, AiFileSeek aiFileSeek, AiFileFlushProc aiFileFlushProc, nint userData = default)
  {
    m_writeProc = aiFileWriteProc;
    m_readProc = aiFileReadProc;
    m_tellProc = aiFileTellProc;
    m_fileSizeProc = aiFileSizeProc;
    m_seekProc = aiFileSeek;
    m_flushProc = aiFileFlushProc;

    AiFile file;
    file.WriteProc = aiFileWriteProc;
    file.ReadProc = aiFileReadProc;
    file.TellProc = aiFileTellProc;
    file.FileSizeProc = aiFileSizeProc;
    file.SeekProc = aiFileSeek;
    file.FlushProc = aiFileFlushProc;
    file.UserData = (byte*)userData;

    AiFile = (AiFile*)MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<AiFile>());
    Marshal.StructureToPtr(file, (nint)AiFile, false);
  }

  /// <summary>
  /// Finalizes an instance of the <see cref="IOStream"/> class.
  /// </summary>
  ~IOStream()
  {
    Dispose(false);
  }

  /// <summary>
  /// Disposes of resources held by the IOStream.
  /// </summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Releases unmanaged and - optionally - managed resources.
  /// </summary>
  /// <param name="disposing">True to release both managed and unmanaged resources; False to release only unmanaged resources.</param>
  protected virtual unsafe void Dispose(bool disposing)
  {
    if (!IsDisposed)
    {
      if (AiFile != null)
      {
        MemoryHelper.FreeMemory(AiFile);
        AiFile = null;
      }

      if (disposing)
      {
        m_writeProc = null;
        m_readProc = null;
        m_tellProc = null;
        m_fileSizeProc = null;
        m_seekProc = null;
        m_flushProc = null;
      }

      IsDisposed = true;
    }
  }

  /// <summary>
  /// Writes data to the stream.
  /// </summary>
  /// <param name="dataToWrite">Data to write</param>
  /// <param name="count">Number of bytes to write</param>
  /// <returns>Number of bytes actually written. Should be equal to the specified count, unless if EoF was hit or an error occured.</returns>
  public abstract long Write(byte[] dataToWrite, long count);

  /// <summary>
  /// Reads data from the stream.
  /// </summary>
  /// <param name="dataRead">Byte buffer to store the read data in</param>
  /// <param name="count">Number of bytes to read</param>
  /// <returns>Number of bytes actually read. Should be equal to the specified count, unless if EoF was hit or an error occured.</returns>
  public abstract long Read(byte[] dataRead, long count);

  /// <summary>
  /// Sets the current file position pointer.
  /// </summary>
  /// <param name="offset">Offset in bytes from the origin</param>
  /// <param name="seekOrigin">Origin reference</param>
  /// <returns>ReturnCode indicating success or failure.</returns>
  public abstract Return Seek(long offset, Origin seekOrigin);

  /// <summary>
  /// Gets the current file position pointer (in bytes).
  /// </summary>
  /// <returns>Current file position pointer (in bytes)</returns>
  public abstract long GetPosition();

  /// <summary>
  /// Gets the total file size (in bytes).
  /// </summary>
  /// <returns>File size in bytes</returns>
  public abstract long GetFileSize();

  /// <summary>
  /// Flushes all data currently in the stream buffers.
  /// </summary>
  public abstract void Flush();

  /// <summary>
  /// Closes the stream - flushing any data not yet read/written and disposes of resources.
  /// </summary>
  public virtual void Close()
  {
    Flush();
    Dispose();
  }

  /// <summary>
  /// Callback for Assimp that handles writes.
  /// </summary>
  /// <param name="file"></param>
  /// <param name="dataToWrite"></param>
  /// <param name="sizeOfElemInBytes"></param>
  /// <param name="numElements"></param>
  /// <returns></returns>
  protected unsafe nuint OnAiFileWriteProc(AiFile* file, byte* dataToWrite, nuint sizeOfElemInBytes, nuint numElements)
  {
    if (AiFile != file)
      return nuint.Zero;

    var longSize = (long) sizeOfElemInBytes.ToUInt64();
    var longNum = (long) numElements.ToUInt64();
    var count = longSize * longNum;

    if (count == 0)
      return nuint.Zero;

    var byteBuffer = GetByteBuffer(longSize, longNum);
    MemoryHelper.Read(dataToWrite, byteBuffer, 0, (int) count);

    long actualCount = 0;

    try
    {
      actualCount = Write(byteBuffer, count);
    }
    catch (Exception) { /*Assimp will report an IO error*/ }

    return new((ulong) actualCount / (ulong) longSize);
  }

  /// <summary>
  /// Callback for Assimp that handles reads.
  /// </summary>
  /// <param name="file"></param>
  /// <param name="dataRead"></param>
  /// <param name="sizeOfElemInBytes"></param>
  /// <param name="numElements"></param>
  /// <returns></returns>
  protected unsafe nuint OnAiFileReadProc(AiFile* file, byte* dataRead, nuint sizeOfElemInBytes, nuint numElements)
  {
    if (AiFile != file)
      return nuint.Zero;

    var longSize = (long) sizeOfElemInBytes.ToUInt64();
    var longNum = (long) numElements.ToUInt64();
    var count = longSize * longNum;

    var byteBuffer = GetByteBuffer(longSize, longNum);

    long actualCount = 0;

    try
    {
      actualCount = Read(byteBuffer, count);

      if (actualCount > 0)
        MemoryHelper.Write<byte>((nint)dataRead, byteBuffer, 0, (int) actualCount);
    }
    catch (Exception) { /*Assimp will report an IO error*/ }

    return new((ulong) actualCount / (ulong) longSize);
  }

  /// <summary>
  /// Callback for Assimp that handles tell requests.
  /// </summary>
  /// <param name="file"></param>
  /// <returns></returns>
  protected unsafe nuint OnAiFileTellProc(AiFile* file)
  {
    if (AiFile != file)
      return nuint.Zero;

    long pos = 0;

    try
    {
      pos = GetPosition();
    }
    catch (Exception) { /*Assimp will report an IO error*/ }

    return new((ulong) pos);
  }

  /// <summary>
  /// Callback for Assimp that handles size requests.
  /// </summary>
  /// <param name="file"></param>
  /// <returns></returns>
  protected unsafe nuint OnAiFileSizeProc(AiFile* file)
  {
    if (AiFile != file)
      return nuint.Zero;

    long fileSize = 0;

    try
    {
      fileSize = GetFileSize();
    }
    catch (Exception) { /*Assimp will report an IO error*/ }

    return new((ulong) fileSize);
  }

  /// <summary>
  /// Callback for Assimp that handles seeks.
  /// </summary>
  /// <param name="file"></param>
  /// <param name="offset"></param>
  /// <param name="seekOrigin"></param>
  /// <returns></returns>
  protected unsafe Return OnAiFileSeekProc(AiFile* file, nuint offset, Origin seekOrigin)
  {
    if (AiFile != file)
      return Return.Failure;

    var code = Return.Failure;

    try
    {
      code = Seek((long) offset.ToUInt64(), seekOrigin);
    }
    catch (Exception) { /*Assimp will report an IO error*/ }

    return code;
  }

  /// <summary>Callback for Assimp that handles flushes.</summary>
  /// <param name="file"></param>
  protected unsafe void OnAiFileFlushProc(AiFile* file)
  {
    if (AiFile != file)
      return;

    try
    {
      Flush();
    }
    catch (Exception) { }
  }

  private byte[] GetByteBuffer(long sizeOfElemInBytes, long numElements)
  {
    //Only create a new buffer if we need it to grow or first time, otherwise re-use it
    if (m_byteBuffer == null || m_byteBuffer.Length < sizeOfElemInBytes * numElements)
      m_byteBuffer = new byte[sizeOfElemInBytes * numElements];

    return m_byteBuffer;
  }
}