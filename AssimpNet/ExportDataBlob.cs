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

using System.IO;
using System.Text;
using Assimp.Unmanaged;

namespace Assimp;

/// <summary>
/// Describes a blob of exported scene data. Blobs can be nested - each blob may reference another blob, which in
/// turn can reference another and so on. This is used to allow exporters to write more than one output for a given
/// scene, such as material files. Existence of such files depends on the format.
/// </summary>
/// <remarks>
/// The stream representation of an ExportDataBlob is as follows:
/// <code>
/// String: Name of the Blob
/// int: Length of Binary Data
/// byte[]: Binary Data
/// bool: If has next data blob
///     String: Name of nested blob
///     int: Length of nested blob binary data
///     byte[]: Nested blob binary data
///     bool: If nested blob has next data blob
///     ....
/// </code>
/// </remarks>
public sealed class ExportDataBlob
{
    /// <summary>
    /// Gets the name of the blob. The first and primary blob always has an empty string for a name. Auxillary files
    /// that are nested will have names.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Get the blob data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the next data blob.
    /// </summary>
    public ExportDataBlob NextBlob { get; private set; }

    /// <summary>
    /// Gets if the blob data is valid.
    /// </summary>
    public bool HasData => Data is {Length: > 0};

    /// <summary>
    /// Creates a new ExportDataBlob.
    /// </summary>
    /// <param name="dataBlob">Unmanaged structure.</param>
    internal ExportDataBlob(ref AiExportDataBlob dataBlob)
    {
        Name = dataBlob.Name.GetString();

        if(dataBlob.Size.ToUInt32() > 0 && dataBlob.Data != nint.Zero)
            Data = MemoryHelper.FromNativeArray<byte>(dataBlob.Data, (int) dataBlob.Size.ToUInt32());

        NextBlob = null;

        if(dataBlob.NextBlob != nint.Zero)
        {
            var nextBlob = MemoryHelper.MarshalStructure<AiExportDataBlob>(dataBlob.NextBlob);
            NextBlob = new(ref nextBlob);
        }
    }

    /// <summary>
    /// Creates a new ExportDataBlob.
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="data">Data</param>
    internal ExportDataBlob(string name, byte[] data)
    {
        Name = name;
        Data = data;
        NextBlob = null;
    }

    /// <summary>
    /// Writes the data blob to the specified stream.
    /// </summary>
    /// <param name="stream">Output stream</param>
    public void ToStream(Stream stream)
    {
        var memStream = new MemoryStream();

        using var writer = new BinaryWriter(memStream);
        WriteBlob(this, writer);

        memStream.Position = 0;
        memStream.WriteTo(stream);
    }

    /// <summary>
    /// Reads a data blob from the specified stream.
    /// </summary>
    /// <param name="stream">Input stream</param>
    /// <returns>Data blob</returns>
    public static ExportDataBlob FromStream(Stream stream)
    {
        if(stream == null || !stream.CanRead)
            return null;

        //Reader set to leave the stream open
        using var reader = new BlobBinaryReader(stream);
        return ReadBlob(reader);

    }

    private static void WriteBlob(ExportDataBlob blob, BinaryWriter writer)
    {
        if(blob == null || writer == null)
            return;

        var hasNext = blob.NextBlob != null;

        writer.Write(blob.Name);
        writer.Write(blob.Data.Length);
        writer.Write(blob.Data);
        writer.Write(hasNext);

        if(hasNext)
            WriteBlob(blob.NextBlob, writer);
    }

    private static ExportDataBlob ReadBlob(BinaryReader reader)
    {
        if(reader == null)
            return null;

        var name = reader.ReadString();
        var count = reader.ReadInt32();
        var data = reader.ReadBytes(count);
        var hasNext = reader.ReadBoolean();

        var blob = new ExportDataBlob(name, data);

        if(hasNext)
            blob.NextBlob = ReadBlob(reader);

        return blob;
    }

    //Special binary reader, which will -not- dispose of underlying stream. Compatible with all the different .net versions
    private class BlobBinaryReader : BinaryReader
    {
        public BlobBinaryReader(Stream stream)
            : base(stream, Encoding.UTF8) { }

        protected override void Dispose(bool disposing)
        {
            //.Net <4.5 does not have "leave open" flag, so workaround we can dispose with false. Stream is not closed and everything
            //is set to null
            base.Dispose(false);
        }
    }
}