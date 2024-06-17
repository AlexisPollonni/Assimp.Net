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

using System.Collections.Generic;
using Assimp.Unmanaged;

namespace Assimp;

/// <summary>
/// Describes vertex-based animations for a single mesh or a group of meshes. Meshes
/// carry the animation data for each frame. The purpose of this object is to define
/// keyframes, linking each mesh attachment to a particular point in a time.
/// </summary>
public sealed class MeshAnimationChannel : IMarshalable<MeshAnimationChannel, AiMeshAnim>
{
    /// <summary>
    /// Gets or sets the name of the mesh to be animated. Empty strings are not allowed,
    /// animation meshes need to be named (not necessarily uniquely, the name can basically
    /// serve as a wildcard to select a group of meshes with similar animation setup).
    /// </summary>
    public string MeshName { get; set; }

    /// <summary>
    /// Gets the number of meshkeys in this animation channel. There will always
    /// be at least one key.
    /// </summary>
    public int MeshKeyCount => MeshKeys.Count;

    /// <summary>
    /// Gets if this animation channel has mesh keys - this should always be true.
    /// </summary>
    public bool HasMeshKeys => MeshKeys.Count > 0;

    /// <summary>
    /// Gets the mesh keyframes of the animation. This should not be null.
    /// </summary>
    public List<MeshKey> MeshKeys { get; }

    /// <summary>
    /// Constructs a new instance of the <see cref="MeshAnimationChannel"/> class.
    /// </summary>
    public MeshAnimationChannel()
    {
        MeshName = string.Empty;
        MeshKeys = [];
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<MeshAnimationChannel, AiMeshAnim>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<MeshAnimationChannel, AiMeshAnim>.ToNative(nint thisPtr, out AiMeshAnim nativeValue)
    {
        nativeValue.Name = new(MeshName);
        nativeValue.NumKeys = (uint) MeshKeyCount;
        nativeValue.Keys = nint.Zero;

        if(nativeValue.NumKeys > 0)
            nativeValue.Keys = MemoryHelper.ToNativeArray(MeshKeys.ToArray());
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<MeshAnimationChannel, AiMeshAnim>.FromNative(in AiMeshAnim nativeValue)
    {
        MeshName = AiString.GetString(nativeValue.Name); //Avoid struct copy
        MeshKeys.Clear();

        if(nativeValue.NumKeys > 0 && nativeValue.Keys != nint.Zero)
            MeshKeys.AddRange(MemoryHelper.FromNativeArray<MeshKey>(nativeValue.Keys, (int) nativeValue.NumKeys));
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{MeshAnimationChannel, AiMeshAnim}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMeshAnim = MemoryHelper.Read<AiMeshAnim>(nativeValue);

        if(aiMeshAnim.NumKeys > 0 && aiMeshAnim.Keys != nint.Zero)
            MemoryHelper.FreeMemory(aiMeshAnim.Keys);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}