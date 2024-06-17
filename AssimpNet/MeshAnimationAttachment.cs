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
/// A mesh attachment store per-vertex animations for a particular frame. You may
/// think of this as a 'patch' for the host mesh, since the mesh attachment replaces only certain
/// vertex data streams at a particular time. Each mesh stores 'n' attached meshes. The actual
/// relationship between the time line and mesh attachments is established by the mesh animation channel,
/// which references singular mesh attachments by their ID and binds them to a time offset.
/// </summary>
public sealed class MeshAnimationAttachment : IMarshalable<MeshAnimationAttachment, AiAnimMesh>
{
    /// <summary>
    /// Gets or sets the mesh animation name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the number of vertices in this mesh. This is a replacement
    /// for the host mesh's vertex count. Likewise, a mesh attachment
    /// cannot add or remove per-vertex attributes, therefore the existance
    /// of vertex data will match the existance of data in the mesh.
    /// </summary>
    public int VertexCount => Vertices.Count;

    /// <summary>
    /// Checks whether the attachment mesh overrides the vertex positions
    /// of its host mesh.
    /// </summary>
    public bool HasVertices => Vertices.Count > 0;

    /// <summary>
    /// Gets the vertex position list.
    /// </summary>
    public List<Vector3D> Vertices { get; }

    /// <summary>
    /// Checks whether the attachment mesh overrides the vertex normals of
    /// its host mesh.
    /// </summary>
    public bool HasNormals => Normals.Count > 0;

    /// <summary>
    /// Gets the vertex normal list.
    /// </summary>
    public List<Vector3D> Normals { get; }

    /// <summary>
    /// Checks whether the attachment mesh overrides the vertex
    /// tangents and bitangents of its host mesh.
    /// </summary>
    public bool HasTangentBasis => Tangents.Count > 0 && BiTangents.Count > 0;

    /// <summary>
    /// Gets the vertex tangent list.
    /// </summary>
    public List<Vector3D> Tangents { get; }

    /// <summary>
    /// Gets the vertex bitangent list.
    /// </summary>
    public List<Vector3D> BiTangents { get; }

    /// <summary>
    /// Gets the number of valid vertex color channels contained in the
    /// mesh (list is not empty/not null). This can be a value between zero and the maximum vertex color count. Each individual channel
    /// should be the size of <see cref="VertexCount"/>.
    /// </summary>
    public int VertexColorChannelCount
    {
        get
        {
            var count = 0;
            for(var i = 0; i < VertexColorChannels.Length; i++)
            {
                if(HasVertexColors(i))
                    count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets the number of valid texture coordinate channels contained
    /// in the mesh (list is not empty/not null). This can be a value between zero and the maximum texture coordinate count.
    /// Each individual channel should be the size of <see cref="VertexCount"/>.
    /// </summary>
    public int TextureCoordinateChannelCount
    {
        get
        {
            var count = 0;
            for(var i = 0; i < TextureCoordinateChannels.Length; i++)
            {
                if(HasTextureCoords(i))
                    count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets the array that contains each vertex color channels that override a specific channel in the host mesh, by default all are lists of zero (but can be set to null). 
    /// Each index in the array corresponds to the texture coordinate channel. The length of the array corresponds to Assimp's maximum vertex color channel limit.
    /// </summary>
    public List<Color4D>[] VertexColorChannels { get; }

    /// <summary>
    /// Gets the array that contains each texture coordinate channel that override a specific channel in the host mesh, by default all are lists of zero (but can be set to null).
    /// Each index in the array corresponds to the texture coordinate channel. The length of the array corresponds to Assimp's maximum UV channel limit.
    /// </summary>
    public List<Vector3D>[] TextureCoordinateChannels { get; }

    /// <summary>
    /// Gets or sets the weight of the mesh animation.
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="MeshAnimationAttachment"/> class.
    /// </summary>
    public MeshAnimationAttachment()
    {
        Vertices = [];
        Normals = [];
        Tangents = [];
        BiTangents = [];
        Weight = 0.0f;

        VertexColorChannels = new List<Color4D>[AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS];

        for(var i = 0; i < VertexColorChannels.Length; i++)
        {
            VertexColorChannels[i] = [];
        }

        TextureCoordinateChannels = new List<Vector3D>[AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS];

        for(var i = 0; i < TextureCoordinateChannels.Length; i++)
        {
            TextureCoordinateChannels[i] = [];
        }
    }

    /// <summary>
    /// Checks if the mesh attachment overrides a particular set of vertex colors on
    /// the host mesh. This returns false if the list is null or empty. The index is between 
    /// zero and the maximumb number of vertex color channels.
    /// </summary>
    /// <param name="channelIndex">Channel index</param>
    /// <returns>True if vertex colors are present in the channel.</returns>
    public bool HasVertexColors(int channelIndex)
    {
        if(channelIndex >= VertexColorChannels.Length || channelIndex < 0)
            return false;

        var colors = VertexColorChannels[channelIndex];

        if(colors != null)
            return colors.Count > 0;

        return false;
    }

    /// <summary>
    /// Checks if the mesh attachment overrides a particular set of texture coordinates on
    /// the host mesh. This returns false if the list is null or empty. The index is 
    /// between zero and the maximum number of texture coordinate channels.
    /// </summary>
    /// <param name="channelIndex">Channel index</param>
    /// <returns>True if texture coordinates are present in the channel.</returns>
    public bool HasTextureCoords(int channelIndex)
    {
        if(channelIndex >= TextureCoordinateChannels.Length || channelIndex < 0)
            return false;

        var texCoords = TextureCoordinateChannels[channelIndex];

        if(texCoords != null)
            return texCoords.Count > 0;

        return false;
    }

    private void ClearBuffers()
    {
        Vertices.Clear();
        Normals.Clear();
        Tangents.Clear();
        BiTangents.Clear();

        for(var i = 0; i < VertexColorChannels.Length; i++)
        {
            var colors = VertexColorChannels[i];

            if(colors == null)
                VertexColorChannels[i] = [];
            else
                colors.Clear();
        }

        for(var i = 0; i < TextureCoordinateChannels.Length; i++)
        {
            var texCoords = TextureCoordinateChannels[i];

            if(texCoords == null)
                TextureCoordinateChannels[i] = [];
            else
                texCoords.Clear();
        }
    }

    private Vector3D[] CopyTo(List<Vector3D> list, Vector3D[] copy)
    {
        list.CopyTo(copy);

        return copy;
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<MeshAnimationAttachment, AiAnimMesh>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<MeshAnimationAttachment, AiAnimMesh>.ToNative(nint thisPtr, out AiAnimMesh nativeValue)
    {
        nativeValue.Name = new(Name);
        nativeValue.Vertices = nint.Zero;
        nativeValue.Normals = nint.Zero;
        nativeValue.Tangents = nint.Zero;
        nativeValue.BiTangents = nint.Zero;
        nativeValue.Colors = new();
        nativeValue.TextureCoords = new();
        nativeValue.NumVertices = (uint) VertexCount;
        nativeValue.Weight = Weight;

        if(VertexCount > 0)
        {

            //Since we can have so many buffers of Vector3D with same length, lets re-use a buffer
            var copy = new Vector3D[VertexCount];

            nativeValue.Vertices = MemoryHelper.ToNativeArray(CopyTo(Vertices, copy));

            if(HasNormals)
                nativeValue.Normals = MemoryHelper.ToNativeArray(CopyTo(Normals, copy));

            if(HasTangentBasis)
            {
                nativeValue.Tangents = MemoryHelper.ToNativeArray(CopyTo(Tangents, copy));
                nativeValue.BiTangents = MemoryHelper.ToNativeArray(CopyTo(BiTangents, copy));
            }

            //Vertex Color channels
            for(var i = 0; i < VertexColorChannels.Length; i++)
            {
                var list = VertexColorChannels[i];

                if(list == null || list.Count == 0)
                {
                    nativeValue.Colors[i] = nint.Zero;
                }
                else
                {
                    nativeValue.Colors[i] = MemoryHelper.ToNativeArray(list.ToArray());
                }
            }

            //Texture coordinate channels
            for(var i = 0; i < TextureCoordinateChannels.Length; i++)
            {
                var list = TextureCoordinateChannels[i];

                if(list == null || list.Count == 0)
                {
                    nativeValue.TextureCoords[i] = nint.Zero;
                }
                else
                {
                    nativeValue.TextureCoords[i] = MemoryHelper.ToNativeArray(CopyTo(list, copy));
                }
            }
        }
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<MeshAnimationAttachment, AiAnimMesh>.FromNative(in AiAnimMesh nativeValue)
    {
        ClearBuffers();

        Name = AiString.GetString(nativeValue.Name); //Avoid struct copy
            
        var vertexCount = (int) nativeValue.NumVertices;
        Weight = nativeValue.Weight;

        if(vertexCount > 0)
        {

            if(nativeValue.Vertices != nint.Zero)
                Vertices.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.Vertices, vertexCount));

            if(nativeValue.Normals != nint.Zero)
                Normals.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.Normals, vertexCount));

            if(nativeValue.Tangents != nint.Zero)
                Tangents.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.Tangents, vertexCount));

            if(nativeValue.BiTangents != nint.Zero)
                BiTangents.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.BiTangents, vertexCount));

            //Vertex Color channels
            for(var i = 0; i < nativeValue.Colors.Length; i++)
            {
                var colorPtr = nativeValue.Colors[i];

                if(colorPtr != nint.Zero)
                    VertexColorChannels[i].AddRange(MemoryHelper.FromNativeArray<Color4D>(colorPtr, vertexCount));
            }

            //Texture coordinate channels
            for(var i = 0; i < nativeValue.TextureCoords.Length; i++)
            {
                var texCoordsPtr = nativeValue.TextureCoords[i];

                if(texCoordsPtr != nint.Zero)
                    TextureCoordinateChannels[i].AddRange(MemoryHelper.FromNativeArray<Vector3D>(texCoordsPtr, vertexCount));
            }
        }
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{MeshAnimationAttachment, AiAnimMesh}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiAnimMesh = MemoryHelper.Read<AiAnimMesh>(nativeValue);

        if(aiAnimMesh.NumVertices > 0)
        {
            if(aiAnimMesh.Vertices != nint.Zero)
                MemoryHelper.FreeMemory(aiAnimMesh.Vertices);

            if(aiAnimMesh.Normals != nint.Zero)
                MemoryHelper.FreeMemory(aiAnimMesh.Normals);

            if(aiAnimMesh.Tangents != nint.Zero)
                MemoryHelper.FreeMemory(aiAnimMesh.Tangents);

            if(aiAnimMesh.BiTangents != nint.Zero)
                MemoryHelper.FreeMemory(aiAnimMesh.BiTangents);

            //Vertex Color channels
            for(var i = 0; i < aiAnimMesh.Colors.Length; i++)
            {
                var colorPtr = aiAnimMesh.Colors[i];

                if(colorPtr != nint.Zero)
                    MemoryHelper.FreeMemory(colorPtr);
            }

            //Texture coordinate channels
            for(var i = 0; i < aiAnimMesh.TextureCoords.Length; i++)
            {
                var texCoordsPtr = aiAnimMesh.TextureCoords[i];

                if(texCoordsPtr != nint.Zero)
                    MemoryHelper.FreeMemory(texCoordsPtr);
            }
        }

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}