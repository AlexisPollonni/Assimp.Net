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

namespace Assimp;

/// <summary>
/// An animation consists of keyframe data for a number of nodes. For
/// each node affected by the animation, a separate series of data is given.
/// </summary>
public sealed class Animation : IMarshalable<Animation, AiAnimation>
{
    /// <summary>
    /// Gets or sets the name of the animation. If the modeling package the
    /// data was exported from only supports a single animation channel, this
    /// name is usually empty.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the duration of the animation in number of ticks.
    /// </summary>
    public double DurationInTicks { get; set; }

    /// <summary>
    /// Gets or sets the number of ticks per second. It may be zero
    /// if it is not specified in the imported file.
    /// </summary>
    public double TicksPerSecond { get; set; }

    /// <summary>
    /// Gets if the animation has node animation channels.
    /// </summary>
    public bool HasNodeAnimations => NodeAnimationChannels.Count > 0;

    /// <summary>
    /// Gets the number of node animation channels where each channel
    /// affects a single node.
    /// </summary>
    public int NodeAnimationChannelCount => NodeAnimationChannels.Count;

    /// <summary>
    /// Gets the node animation channels.
    /// </summary>
    public List<NodeAnimationChannel> NodeAnimationChannels { get; }

    /// <summary>
    /// Gets if the animation has mesh animations.
    /// </summary>
    public bool HasMeshAnimations => MeshAnimationChannels.Count > 0;

    /// <summary>
    /// Gets the number of mesh animation channels.
    /// </summary>
    public int MeshAnimationChannelCount => MeshAnimationChannels.Count;

    /// <summary>
    /// Gets the number of mesh morph animation channels.
    /// </summary>
    public int MeshMorphAnimationChannelCount => MeshMorphAnimationChannels.Count;

    /// <summary>
    /// Gets the mesh animation channels.
    /// </summary>
    public List<MeshAnimationChannel> MeshAnimationChannels { get; }

    /// <summary>
    /// Gets the mesh morph animation channels.
    /// </summary>
    public List<MeshMorphAnimationChannel> MeshMorphAnimationChannels { get; }

    /// <summary>
    /// Constructs a new instance of the <see cref="Animation"/> class.
    /// </summary>
    public Animation()
    {
        Name = string.Empty;
        DurationInTicks = 0;
        TicksPerSecond = 0;
        NodeAnimationChannels = [];
        MeshAnimationChannels = [];
        MeshMorphAnimationChannels = [];
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Animation, AiAnimation>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<Animation, AiAnimation>.ToNative(nint thisPtr, out AiAnimation nativeValue)
    {
        nativeValue.MName = new(Name);
        nativeValue.MDuration = DurationInTicks;
        nativeValue.MTicksPerSecond = TicksPerSecond;
        nativeValue.MNumChannels = (uint) NodeAnimationChannelCount;
        nativeValue.MNumMeshChannels = (uint) MeshAnimationChannelCount;
        nativeValue.MNumMorphMeshChannels = (uint) MeshMorphAnimationChannelCount;
        nativeValue.MChannels = null;
        nativeValue.MMeshChannels = null;
        nativeValue.MMorphMeshChannels = null;

        if(nativeValue.MNumChannels > 0)
            nativeValue.MChannels = MemoryHelper.ToNativeArrayOfPtr<NodeAnimationChannel, AiNodeAnim>(NodeAnimationChannels.ToArray());

        if(nativeValue.MNumMeshChannels > 0)
            nativeValue.MMeshChannels = MemoryHelper.ToNativeArrayOfPtr<MeshAnimationChannel, AiMeshAnim>(MeshAnimationChannels.ToArray());

        if(nativeValue.MNumMorphMeshChannels > 0)
            nativeValue.MMorphMeshChannels = MemoryHelper.ToNativeArrayOfPtr<MeshMorphAnimationChannel, AiMeshMorphAnim>(MeshMorphAnimationChannels.ToArray());
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<Animation, AiAnimation>.FromNative(in AiAnimation nativeValue)
    {
        NodeAnimationChannels.Clear();
        MeshAnimationChannels.Clear();
        MeshMorphAnimationChannels.Clear();

        Name = nativeValue.MName; //Avoid struct copy
        DurationInTicks = nativeValue.MDuration;
        TicksPerSecond = nativeValue.MTicksPerSecond;

        if(nativeValue.MNumChannels > 0 && nativeValue.MChannels != null)
            NodeAnimationChannels.AddRange(MemoryHelper.FromNativeArray<NodeAnimationChannel, AiNodeAnim>((nint)nativeValue.MChannels, (int) nativeValue.MNumChannels, true));

        if(nativeValue.MNumMeshChannels > 0 && nativeValue.MMeshChannels != null)
            MeshAnimationChannels.AddRange(MemoryHelper.FromNativeArray<MeshAnimationChannel, AiMeshAnim>((nint)nativeValue.MMeshChannels, (int) nativeValue.MNumMeshChannels, true));

        if(nativeValue.MNumMorphMeshChannels > 0 && nativeValue.MMorphMeshChannels != null)
            MeshMorphAnimationChannels.AddRange(MemoryHelper.FromNativeArray<MeshMorphAnimationChannel, AiMeshMorphAnim>((nint)nativeValue.MMorphMeshChannels, (int) nativeValue.MNumMorphMeshChannels, true));
    }


    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Animation, AiAnimation}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiAnimation = MemoryHelper.Read<AiAnimation>(nativeValue);

        if(aiAnimation.MNumChannels > 0 && aiAnimation.MChannels != null)
            MemoryHelper.FreeNativeArray(aiAnimation.MChannels, (int) aiAnimation.MNumChannels, NodeAnimationChannel.FreeNative);

        if(aiAnimation.MNumMeshChannels > 0 && aiAnimation.MMeshChannels != null)
            MemoryHelper.FreeNativeArray(aiAnimation.MMeshChannels, (int) aiAnimation.MNumMeshChannels, MeshAnimationChannel.FreeNative);

        if(aiAnimation.MNumMorphMeshChannels > 0 && aiAnimation.MMorphMeshChannels != null)
            MemoryHelper.FreeNativeArray(aiAnimation.MMorphMeshChannels, (int) aiAnimation.MNumMorphMeshChannels, MeshMorphAnimationChannel.FreeNative);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}