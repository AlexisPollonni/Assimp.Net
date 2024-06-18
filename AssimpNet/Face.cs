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

using System.Linq;

namespace Assimp;

/// <summary>
/// A single face in a mesh, referring to multiple vertices. This can be a triangle
/// if the index count is equal to three, or a polygon if the count is greater than three.
/// 
/// Since multiple primitive types can be contained in a single mesh, this approach
/// allows you to better examine how the mesh is constructed. If you use the <see cref="PostProcessSteps.SortByPrimitiveType"/>
/// post process step flag during import, then each mesh will be homogenous where primitive type is concerned.
/// </summary>
public sealed class Face : IMarshalable<Face, AiFace>
{
    /// <summary>
    /// Gets the number of indices defined in the face.
    /// </summary>
    public int IndexCount => Indices.Count;

    /// <summary>
    /// Gets if the face has faces (should always be true).
    /// </summary>
    public bool HasIndices => Indices.Count > 0;

    /// <summary>
    /// Gets or sets the indices that refer to positions of vertex data in the mesh's vertex 
    /// arrays.
    /// </summary>
    public List<int> Indices { get; }

    /// <summary>
    /// Constructs a new instance of the <see cref="Face"/> class.
    /// </summary>
    public Face()
    {
        Indices = [];
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Face"/> class.
    /// </summary>
    /// <param name="indices">Face indices</param>
    public Face(int[] indices)
    {
        Indices = [];

        if(indices != null)
            Indices.AddRange(indices);
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Face, AiFace>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<Face, AiFace>.ToNative(nint thisPtr, out AiFace nativeValue)
    {
        nativeValue.MNumIndices = (uint) IndexCount;
        nativeValue.MIndices = null;

        if(nativeValue.MNumIndices > 0)
            nativeValue.MIndices = MemoryHelper.ToNativeArray<uint>(Indices.Select(i => (uint)i).ToArray()); //TODO: Span cast
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<Face, AiFace>.FromNative(in AiFace nativeValue)
    {
        Indices.Clear();

        if(nativeValue.MNumIndices > 0 && nativeValue.MIndices != null)
            Indices.AddRange(MemoryHelper.FromNativeArray(nativeValue.MIndices, (int) nativeValue.MNumIndices).Select(i => (int)i));
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Face, AiFace}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiFace = MemoryHelper.Read<AiFace>(nativeValue);

        if(aiFace.MNumIndices > 0 && aiFace.MIndices != null)
            MemoryHelper.FreeMemory(aiFace.MIndices);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}