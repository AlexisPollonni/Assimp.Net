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

using System.Diagnostics;
using System.Linq;

namespace Assimp;

/// <summary>
/// Binds a morph animation mesh to a specific point in time.
/// </summary>
public sealed class MeshMorphKey : IMarshalable<MeshMorphKey, AiMeshMorphKey>
{
    /// <summary>
    /// Gets or sets the time of this keyframe.
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    /// Gets the values at the time of this keyframe. Number of values must equal number of weights.
    /// </summary>
    public List<int> Values { get; }

    /// <summary>
    /// Gets the weights at the time of this keyframe. Number of weights must equal number of values.
    /// </summary>
    public List<double> Weights { get; }

    /// <summary>
    /// Constructs a new instance of the <see cref="MeshMorphKey"/> class.
    /// </summary>
    public MeshMorphKey()
    {
        Time = 0.0;
        Values = [];
        Weights = [];
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<MeshMorphKey, AiMeshMorphKey>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<MeshMorphKey, AiMeshMorphKey>.ToNative(nint thisPtr, out AiMeshMorphKey nativeValue)
    {
        nativeValue.MTime = Time;
        nativeValue.MNumValuesAndWeights = (uint) Weights.Count;
        nativeValue.MValues = null;
        nativeValue.MWeights = null;

        Debug.Assert(Weights.Count == Values.Count);
        if(Weights.Count == Values.Count)
        {
            if(Weights.Count > 0)
            {
                nativeValue.MValues = MemoryHelper.ToNativeArray<uint>(Values.Select(i => (uint)i).ToArray());
                nativeValue.MWeights = MemoryHelper.ToNativeArray<double>(Weights.ToArray());
            }
        }
        else
        {
            //If both lists are not the same length then do not write anything out
            nativeValue.MNumValuesAndWeights = 0;
        }
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<MeshMorphKey, AiMeshMorphKey>.FromNative(in AiMeshMorphKey nativeValue)
    {
        Time = nativeValue.MTime;

        Values.Clear();
        Weights.Clear();

        if(nativeValue.MNumValuesAndWeights > 0)
        {
            if (nativeValue.MValues != null)
                Values.AddRange(MemoryHelper.FromNativeArray(nativeValue.MValues, (int) nativeValue.MNumValuesAndWeights).Select(i => (int)i));

            if(nativeValue.MWeights != null)
                Weights.AddRange(MemoryHelper.FromNativeArray(nativeValue.MWeights, (int) nativeValue.MNumValuesAndWeights));
        }
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{MeshMorphKey, AiMeshMorphKey}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMeshMorphKey = MemoryHelper.Read<AiMeshMorphKey>(nativeValue);

        if(aiMeshMorphKey.MNumValuesAndWeights > 0)
        {
            if(aiMeshMorphKey.MValues != null)
                MemoryHelper.FreeMemory(aiMeshMorphKey.MValues);

            if(aiMeshMorphKey.MWeights != null)
                MemoryHelper.FreeMemory(aiMeshMorphKey.MWeights);
        }

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}