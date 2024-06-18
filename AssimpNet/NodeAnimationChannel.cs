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
/// Describes the animation of a single node. The name specifies the bone/node which is affected by
/// this animation chanenl. The keyframes are given in three separate seties of values,
/// one for each position, rotation, and scaling. The transformation matrix is computed from
/// these values and replaces the node's original transformation matrix at a specific time.
/// <para>This means all keys are absolute and not relative to the bone default pose.
/// The order which the transformations are to be applied is scaling, rotation, and translation (SRT).</para>
/// <para>Keys are in chronological order and duplicate keys do not pass the validation step. There most likely will be no
/// negative time values, but they are not forbidden.</para>
/// </summary>
public sealed class NodeAnimationChannel : IMarshalable<NodeAnimationChannel, AiNodeAnim>
{
    /// <summary>
    /// Gets or sets the name of the node affected by this animation. It must <c>exist</c> and it <c>must</c>
    /// be unique.
    /// </summary>
    public string NodeName { get; set; }

    /// <summary>
    /// Gets the number of position keys in the animation channel.
    /// </summary>
    public int PositionKeyCount => PositionKeys.Count;

    /// <summary>
    /// Gets if this animation channel contains position keys.
    /// </summary>
    public bool HasPositionKeys => PositionKeys.Count > 0;

    /// <summary>
    /// Gets the position keys of this animation channel. Positions are
    /// specified as a 3D vector. If there are position keys, there should
    /// also be -at least- one scaling and one rotation key.
    /// </summary>
    public List<VectorKey> PositionKeys { get; }

    /// <summary>
    /// Gets the number of rotation keys in the animation channel.
    /// </summary>
    public int RotationKeyCount => RotationKeys.Count;

    /// <summary>
    /// Gets if the animation channel contains rotation keys.
    /// </summary>
    public bool HasRotationKeys => RotationKeys.Count > 0;

    /// <summary>
    /// Gets the rotation keys of this animation channel. Rotations are
    /// given as quaternions. If this exists, there should be -at least- one
    /// scaling and one position key.
    /// </summary>
    public List<QuaternionKey> RotationKeys { get; }

    /// <summary>
    /// Gets the number of scaling keys in the animation channel.
    /// </summary>
    public int ScalingKeyCount => ScalingKeys.Count;

    /// <summary>
    /// Gets if the animation channel contains scaling keys.
    /// </summary>
    public bool HasScalingKeys => ScalingKeys.Count > 0;

    /// <summary>
    /// Gets the scaling keys of this animation channel. Scalings are
    /// specified in a 3D vector. If there are scaling keys, there should
    /// also be -at least- one position and one rotation key.
    /// </summary>
    public List<VectorKey> ScalingKeys { get; }

    /// <summary>
    /// Gets or sets how the animation behaves before the first key is encountered. By default the original
    /// transformation matrix of the affected node is used.
    /// </summary>
    public AnimBehaviour PreState { get; set; }

    /// <summary>
    /// Gets or sets how the animation behaves after the last key was processed. By default the original
    /// transformation matrix of the affected node is taken.
    /// </summary>
    public AnimBehaviour PostState { get; set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="NodeAnimationChannel"/> class.
    /// </summary>
    public NodeAnimationChannel()
    {
        NodeName = string.Empty;
        PreState = AnimBehaviour.Default;
        PostState = AnimBehaviour.Default;

        PositionKeys = [];
        RotationKeys = [];
        ScalingKeys = [];
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<NodeAnimationChannel, AiNodeAnim>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<NodeAnimationChannel, AiNodeAnim>.ToNative(nint thisPtr, out AiNodeAnim nativeValue)
    {
        nativeValue.MNodeName = new(NodeName);
        nativeValue.MPreState = PreState;
        nativeValue.MPostState = PostState;

        nativeValue.MNumPositionKeys = (uint) PositionKeys.Count;
        nativeValue.MPositionKeys = null;

        if(nativeValue.MNumPositionKeys > 0)
            nativeValue.MPositionKeys = (AiVectorKey*)MemoryHelper.ToNativeArray<VectorKey>(PositionKeys.ToArray());


        nativeValue.MNumRotationKeys = (uint) RotationKeys.Count;
        nativeValue.MRotationKeys = null;

        if(nativeValue.MNumRotationKeys > 0)
            nativeValue.MRotationKeys = (QuatKey*)MemoryHelper.ToNativeArray<QuaternionKey>(RotationKeys.ToArray());


        nativeValue.MNumScalingKeys = (uint) ScalingKeys.Count;
        nativeValue.MScalingKeys = null;

        if(nativeValue.MNumScalingKeys > 0)
            nativeValue.MScalingKeys = (AiVectorKey*)MemoryHelper.ToNativeArray<VectorKey>(ScalingKeys.ToArray());
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<NodeAnimationChannel, AiNodeAnim>.FromNative(in AiNodeAnim nativeValue)
    {
        NodeName = nativeValue.MNodeName;
        PreState = nativeValue.MPreState;
        PostState = nativeValue.MPostState;

        PositionKeys.Clear();
        RotationKeys.Clear();
        ScalingKeys.Clear();

        if(nativeValue.MNumPositionKeys > 0 && nativeValue.MPositionKeys != null)
            PositionKeys.AddRange(MemoryHelper.FromNativeArray((VectorKey*)nativeValue.MPositionKeys, (int) nativeValue.MNumPositionKeys));

        if(nativeValue.MNumRotationKeys > 0 && nativeValue.MRotationKeys != null)
            RotationKeys.AddRange(MemoryHelper.FromNativeArray((QuaternionKey*)nativeValue.MRotationKeys, (int) nativeValue.MNumRotationKeys));

        if(nativeValue.MNumScalingKeys > 0 && nativeValue.MScalingKeys != null)
            ScalingKeys.AddRange(MemoryHelper.FromNativeArray((VectorKey*)nativeValue.MScalingKeys, (int) nativeValue.MNumScalingKeys));
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{NodeAnimationChannel, AiNodeAnim}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiNodeAnim = MemoryHelper.Read<AiNodeAnim>(nativeValue);

        if(aiNodeAnim.MNumPositionKeys > 0 && aiNodeAnim.MPositionKeys != null)
            MemoryHelper.FreeMemory(aiNodeAnim.MPositionKeys);

        if(aiNodeAnim.MNumRotationKeys > 0 && aiNodeAnim.MRotationKeys != null)
            MemoryHelper.FreeMemory(aiNodeAnim.MRotationKeys);

        if(aiNodeAnim.MNumScalingKeys > 0 && aiNodeAnim.MScalingKeys != null)
            MemoryHelper.FreeMemory(aiNodeAnim.MScalingKeys);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}