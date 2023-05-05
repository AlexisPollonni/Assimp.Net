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
using Assimp.Unmanaged;

namespace Assimp;

/// <summary>
/// Represents an embedded texture. Some file formats directly embed texture assets.
/// Embedded textures may be uncompressed, where the data is given in an uncompressed format.
/// Or it may be compressed in a format like png or jpg. In the latter case, the raw
/// file bytes are given so the application must utilize an image decoder (e.g. DevIL) to
/// get access to the actual color data. This object represents both types, so some properties may or may not be valid depending
/// if it is compressed or not.
/// </summary>
public sealed class EmbeddedTexture : IMarshalable<EmbeddedTexture, AiTexture>
{
    //Uncompressed textures only

    //Compressed textures only

    /// <summary>
    /// Gets or sets the texture's original filename.
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// Gets if the texture is compressed or not.
    /// </summary>
    public bool IsCompressed { get; private set; }

    /// <summary>
    /// Gets the width of the texture in pixels. Only valid for non-compressed textures.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the texture in pixels. Only valid for non-compressed textures.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets if the texture has non-compressed texel data. Only valid for non-compressed textures.
    /// </summary>
    public bool HasNonCompressedData => NonCompressedData != null && NonCompressedData.Length != 0;

    /// <summary>
    /// Gets the size of the non-compressed texel data. Only valid for non-compressed textures.
    /// </summary>
    public int NonCompressedDataSize => NonCompressedData?.Length ?? 0;

    /// <summary>
    /// Gets the non-compressed texel data, the array is of size Width * Height. Only valid for non-compressed textures.
    /// </summary>
    public Texel[] NonCompressedData { get; private set; }

    /// <summary>
    /// Gets if the embedded texture has compressed data. Only valid for compressed textures.
    /// </summary>
    public bool HasCompressedData => CompressedData != null && CompressedData.Length != 0;

    /// <summary>
    /// Gets the size of the compressed data. Only valid for compressed textures.
    /// </summary>
    public int CompressedDataSize => CompressedData?.Length ?? 0;

    /// <summary>
    /// Gets the raw byte data representing the compressed texture. Only valid for compressed textures.
    /// </summary>
    public byte[] CompressedData { get; private set; }

    /// <summary>
    /// Gets the format hint to determine the type of compressed data. This hint
    /// is a three-character lower-case hint like "dds", "jpg", "png".
    /// </summary>
    public string CompressedFormatHint { get; private set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="EmbeddedTexture"/> class. Should use only if
    /// reading from a native value.
    /// </summary>
    public EmbeddedTexture()
    {
        IsCompressed = false;
    }


    /// <summary>
    /// Constructs a new instance of the <see cref="EmbeddedTexture"/> class. This creates a compressed
    /// embedded texture.
    /// </summary>
    /// <param name="compressedFormatHint">The 3 character format hint.</param>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="originalFileName">Optional file name for the texture.</param>
    public EmbeddedTexture(string compressedFormatHint, byte[] compressedData, string originalFileName = "")
    {
        Filename = originalFileName;
        CompressedFormatHint = compressedFormatHint;
        CompressedData = compressedData;

        IsCompressed = true;
        Width = 0;
        Height = 0;
        NonCompressedData = null;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="EmbeddedTexture"/> class. This creates an uncompressed
    /// embedded texture.
    /// </summary>
    /// <param name="width">Width of the texture</param>
    /// <param name="height">Height of the texture</param>
    /// <param name="uncompressedData">Color data</param>
    /// <param name="originalFileName">Optional file name for the texture.</param>
    /// <exception cref="ArgumentException">Thrown if the data size does not match width * height.</exception>
    public EmbeddedTexture(int width, int height, Texel[] uncompressedData, string originalFileName = "")
    {
        Filename = originalFileName;
        Width = width;
        Height = height;
        NonCompressedData = uncompressedData;

        if(Width * Height != NonCompressedDataSize)
            throw new ArgumentException("Texel data size does not match width * height.");

        IsCompressed = false;
        CompressedFormatHint = null;
        CompressedData = null;
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<EmbeddedTexture, AiTexture>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<EmbeddedTexture, AiTexture>.ToNative(nint thisPtr, out AiTexture nativeValue)
    {
        nativeValue.Filename = new(Filename);
            
        if(IsCompressed)
        {
            nativeValue.Width = (uint) CompressedDataSize;
            nativeValue.Height = 0;
            nativeValue.Data = nint.Zero;

            if(CompressedDataSize > 0)
                nativeValue.Data = MemoryHelper.ToNativeArray(CompressedData);

            nativeValue.SetFormatHint(CompressedFormatHint);
        }
        else
        {
            nativeValue.Width = (uint) Width;
            nativeValue.Height = (uint) Height;
            nativeValue.Data = nint.Zero;

            if(NonCompressedDataSize > 0)
                nativeValue.Data = MemoryHelper.ToNativeArray(NonCompressedData);

            nativeValue.SetFormatHint(null);
        }
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<EmbeddedTexture, AiTexture>.FromNative(in AiTexture nativeValue)
    {
        Filename = AiString.GetString(nativeValue.Filename); //Avoid struct copy;
        IsCompressed = nativeValue.Height == 0;

        if(IsCompressed)
        {
            Width = 0;
            Height = 0;
            NonCompressedData = null;
            CompressedData = null;

            if(nativeValue.Width > 0 && nativeValue.Data != nint.Zero)
                CompressedData = MemoryHelper.FromNativeArray<byte>(nativeValue.Data, (int) nativeValue.Width);

            CompressedFormatHint = AiTexture.GetFormatHint(nativeValue); //Avoid struct copy
        }
        else
        {
            CompressedData = null;
            CompressedFormatHint = null;
            NonCompressedData = null;

            Width = (int) nativeValue.Width;
            Height = (int) nativeValue.Height;

            var size = Width * Height;

            if(size > 0 && nativeValue.Data != nint.Zero)
                NonCompressedData = MemoryHelper.FromNativeArray<Texel>(nativeValue.Data, size);
        }
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{EmbeddedTexture, AiTexture}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiTexture = MemoryHelper.Read<AiTexture>(nativeValue);

        if(aiTexture.Width > 0 && aiTexture.Data != nint.Zero)
            MemoryHelper.FreeMemory(aiTexture.Data);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}