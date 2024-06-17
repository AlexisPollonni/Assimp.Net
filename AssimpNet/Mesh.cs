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
using System.Numerics;
using System.Runtime.InteropServices;
using Assimp.Unmanaged;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using AiMesh = Silk.NET.Assimp.Mesh;

namespace Assimp;

/// <summary>
/// A mesh represents geometry with a single material.
/// </summary>
public sealed class Mesh : IMarshalable<Mesh, AiMesh>
{
    /// <summary>
    /// Gets or sets the mesh name. This tends to be used
    /// when formats name nodes and meshes independently,
    /// vertex animations refer to meshes by their names,
    /// or importers split meshes up, each mesh will reference
    /// the same (dummy) name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the primitive type. This may contain more than one
    /// type unless if <see cref="PostProcessSteps.SortByPrimitiveType"/>
    /// option is not set.
    /// </summary>
    public PrimitiveType PrimitiveType { get; set; }

    /// <summary>
    /// Gets or sets the index of the material associated with this mesh.
    /// </summary>
    public int MaterialIndex { get; set; }

    /// <summary>
    /// Gets the number of vertices in this mesh. This is the count that all
    /// per-vertex lists should be the size of.
    /// </summary>
    public int VertexCount => Vertices.Count;

    /// <summary>
    /// Gets if the mesh has a vertex array. This should always return
    /// true provided no special scene flags are set.
    /// </summary>
    public bool HasVertices => Vertices.Count > 0;

    /// <summary>
    /// Gets the vertex position list.
    /// </summary>
    public List<Vector3> Vertices { get; }

    /// <summary>
    /// Gets if the mesh as normals. If it does exist, the count should be the same as the vertex count.
    /// </summary>
    public bool HasNormals => Normals.Count > 0;

    /// <summary>
    /// Gets the vertex normal list.
    /// </summary>
    public List<Vector3> Normals { get; }

    /// <summary>
    /// Gets if the mesh has tangents and bitangents. It is not
    /// possible for one to be without the other. If it does exist, the count should be the same as the vertex count.
    /// </summary>
    public bool HasTangentBasis => Tangents.Count > 0 && BiTangents.Count > 0;

    /// <summary>
    /// Gets the vertex tangent list.
    /// </summary>
    public List<Vector3> Tangents { get; }

    /// <summary>
    /// Gets the vertex bitangent list.
    /// </summary>
    public List<Vector3> BiTangents { get; }

    /// <summary>
    /// Gets the number of faces contained in the mesh.
    /// </summary>
    public int FaceCount => Faces.Count;

    /// <summary>
    /// Gets if the mesh contains faces. If no special
    /// scene flags are set, this should always return true.
    /// </summary>
    public bool HasFaces => Faces.Count > 0;

    /// <summary>
    /// Gets the mesh's faces. Each face will contain indices
    /// to the vertices.
    /// </summary>
    public List<Face> Faces { get; }

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
    /// Gets the array that contains each vertex color channels, by default all are lists of zero (but can be set to null). Each index
    /// in the array corresponds to the texture coordinate channel. The length of the array corresponds to Assimp's maximum vertex color channel limit.
    /// </summary>
    public List<Color4D>[] VertexColorChannels { get; }

    /// <summary>
    /// Gets the array that contains each texture coordinate channel, by default all are lists of zero (but can be set to null). Each index
    /// in the array corresponds to the texture coordinate channel. The length of the array corresponds to Assimp's maximum UV channel limit.
    /// </summary>
    public List<Vector3>[] TextureCoordinateChannels { get; }

    /// <summary>
    /// Gets the array that contains the count of UV(W) components for each texture coordinate channel, usually 2 (UV) or 3 (UVW). A component
    /// value of zero means the texture coordinate channel does not exist. The channel index (index in the array) corresponds
    /// to the texture coordinate channel index.
    /// </summary>
    public int[] UVComponentCount { get; }

    /// <summary>
    /// Gets the number of bones that influence this mesh.
    /// </summary>
    public int BoneCount => Bones.Count;

    /// <summary>
    /// Gets if this mesh has bones.
    /// </summary>
    public bool HasBones => Bones.Count > 0;

    /// <summary>
    /// Gets the bones that influence this mesh.
    /// </summary>
    public List<Bone> Bones { get; }

    /// <summary>
    /// Gets the number of mesh animation attachments that influence this mesh.
    /// </summary>
    public int MeshAnimationAttachmentCount => MeshAnimationAttachments.Count;

    /// <summary>
    /// Gets if this mesh has mesh animation attachments.
    /// </summary>
    public bool HasMeshAnimationAttachments => MeshAnimationAttachments.Count > 0;

    /// <summary>
    /// Gets the mesh animation attachments that influence this mesh.
    /// </summary>
    public List<MeshAnimationAttachment> MeshAnimationAttachments { get; }

    /// <summary>
    /// Gets or sets the morph method used when animation attachments are used.
    /// </summary>
    public MorphingMethod MorphMethod { get; set; }

    /// <summary>
    /// Gets or sets the axis aligned bounding box that contains the extents of the mesh.
    /// </summary>
    public Box3D<float> BoundingBox { get; set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="Mesh"/> class.
    /// </summary>
    public Mesh() : this(string.Empty, PrimitiveType.Triangle) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="Mesh"/> class.
    /// </summary>
    /// <param name="name">Name of the mesh.</param>
    public Mesh(string name) : this(name, PrimitiveType.Triangle) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="Mesh"/> class.
    /// </summary>
    /// <param name="primType">Primitive types contained in the mesh.</param>
    public Mesh(PrimitiveType primType) : this(string.Empty, primType) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="Mesh"/> class.
    /// </summary>
    /// <param name="name">Name of the mesh</param>
    /// <param name="primType">Primitive types contained in the mesh.</param>
    public Mesh(string name, PrimitiveType primType)
    {
        Name = name;
        PrimitiveType = primType;
        MaterialIndex = 0;
        MorphMethod = MorphingMethod.Unknown;

        Vertices = [];
        Normals = [];
        Tangents = [];
        BiTangents = [];
        VertexColorChannels = new List<Color4D>[AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS];

        for(var i = 0; i < VertexColorChannels.Length; i++)
        {
            VertexColorChannels[i] = [];
        }

        TextureCoordinateChannels = new List<Vector3>[AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS];

        for(var i = 0; i < TextureCoordinateChannels.Length; i++)
        {
            TextureCoordinateChannels[i] = [];
        }

        UVComponentCount = new int[AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS];
        Bones = [];
        Faces = [];
        MeshAnimationAttachments = [];
    }

    /// <summary>
    /// Checks if the mesh has vertex colors for the specified channel. This returns false if the list
    /// is null or empty. The channel, if it exists, should contain the same number of entries as <see cref="VertexCount"/>.
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
    /// Checks if the mesh has texture coordinates for the specified channel. This returns false if the list
    /// is null or empty. The channel, if it exists, should contain the same number of entries as <see cref="VertexCount"/>.
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

    /// <summary>
    /// Convienence method for setting this meshe's face list from an index buffer.
    /// </summary>
    /// <param name="indices">Index buffer</param>
    /// <param name="indicesPerFace">Indices per face</param>
    /// <returns>True if the operation succeeded, false otherwise (e.g. not enough data)</returns>
    public bool SetIndices(int[] indices, int indicesPerFace)
    {
        if(indices == null || indices.Length == 0 || indices.Length % indicesPerFace != 0)
            return false;

        Faces.Clear();

        var numFaces = indices.Length / indicesPerFace;
        var index = 0;

        for(var i = 0; i < numFaces; i++)
        {
            var face = new Face();
            for(var j = 0; j < indicesPerFace; j++)
            {
                face.Indices.Add(indices[index]);
                index++;
            }
            Faces.Add(face);
        }

        return true;
    }

    /// <summary>
    /// Convienence method for accumulating all face indices into a single
    /// index array.
    /// </summary>
    /// <returns>int index array</returns>
    public int[] GetIndices()
    {
        if(HasFaces)
        {
            var indices = new List<int>();
            foreach(var face in Faces)
            {
                if(face.IndexCount > 0 && face.Indices != null)
                {
                    indices.AddRange(face.Indices);
                }
            }
            return indices.ToArray();
        }
        return null;
    }

    /// <summary>
    /// Convienence method for accumulating all face indices into a single index
    /// array as unsigned integers (the default from Assimp, if you need them).
    /// </summary>
    /// <returns>uint index array</returns>
    public uint[] GetUnsignedIndices()
    {
        if(HasFaces)
        {
            var indices = new List<uint>();
            foreach(var face in Faces)
            {
                if(face.IndexCount > 0 && face.Indices != null)
                {
                    foreach(uint index in face.Indices)
                    {
                        indices.Add(index);
                    }
                }
            }

            return indices.ToArray();
        }

        return null;
    }

    /// <summary>
    /// Convienence method for accumulating all face indices into a single
    /// index array.
    /// </summary>
    /// <returns>short index array</returns>
    public short[] GetShortIndices()
    {
        if(HasFaces)
        {
            var indices = new List<short>();
            foreach(var face in Faces)
            {
                if(face.IndexCount > 0 && face.Indices != null)
                {
                    foreach(uint index in face.Indices)
                    {
                        indices.Add((short) index);
                    }
                }
            }

            return indices.ToArray();
        }

        return null;
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

        for(var i = 0; i < UVComponentCount.Length; i++)
        {
            UVComponentCount[i] = 0;
        }

        Bones.Clear();
        Faces.Clear();
        MeshAnimationAttachments.Clear();
    }

    private Vector3[] CopyTo(List<Vector3> list, Vector3[] copy)
    {
        list.CopyTo(copy);

        return copy;
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Mesh, AiMesh>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<Mesh, AiMesh>.ToNative(nint thisPtr, out AiMesh nativeValue)
    {
        nativeValue.MName = new(Name);
        nativeValue.MVertices = null;
        nativeValue.MNormals = null;
        nativeValue.MTangents = null;
        nativeValue.MBitangents = null;
        nativeValue.MAnimMeshes = null;
        nativeValue.MBones = null;
        nativeValue.MFaces = null;
        nativeValue.MColors = new();
        nativeValue.MTextureCoords = new();
        nativeValue.MPrimitiveTypes = (uint)PrimitiveType;
        nativeValue.MMaterialIndex = (uint) MaterialIndex;
        nativeValue.MNumVertices = (uint) VertexCount;
        nativeValue.MNumBones = (uint) BoneCount;
        nativeValue.MNumFaces = (uint) FaceCount;
        nativeValue.MNumAnimMeshes = (uint) MeshAnimationAttachmentCount;
        nativeValue.MMethod = MorphMethod;
        nativeValue.MAABB = BoundingBox;
        nativeValue.MTextureCoordsNames = null;

        if(nativeValue.MNumVertices > 0)
        {

            //Since we can have so many buffers of Vector3 with same length, lets re-use a buffer
            var copy = new Vector3[nativeValue.MNumVertices];

            nativeValue.MVertices = MemoryHelper.ToNativeArray<Vector3>(CopyTo(Vertices, copy));

            if(HasNormals)
                nativeValue.MNormals = MemoryHelper.ToNativeArray<Vector3>(CopyTo(Normals, copy));

            if(HasTangentBasis)
            {
                nativeValue.MTangents = MemoryHelper.ToNativeArray<Vector3>(CopyTo(Tangents, copy));
                nativeValue.MBitangents = MemoryHelper.ToNativeArray<Vector3>(CopyTo(BiTangents, copy));
            }

            //Vertex Color channels
            for(var i = 0; i < VertexColorChannels.Length; i++)
            {
                var list = VertexColorChannels[i];

                if(list == null || list.Count == 0)
                {
                    nativeValue.MColors[i] = null;
                }
                else
                {
                    nativeValue.MColors[i] = (Vector4*)MemoryHelper.ToNativeArray<Color4D>(CollectionsMarshal.AsSpan(list));
                }
            }

            //Texture coordinate channels
            for(var i = 0; i < TextureCoordinateChannels.Length; i++)
            {
                var list = TextureCoordinateChannels[i];

                if(list == null || list.Count == 0)
                {
                    nativeValue.MTextureCoords[i] = null;
                }
                else
                {
                    nativeValue.MTextureCoords[i] = MemoryHelper.ToNativeArray<Vector3>(CopyTo(list, copy));
                }
            }

            //UV components for each tex coordinate channel
            for(var i = 0; i < UVComponentCount.Length; i++)
            {
                nativeValue.MNumUVComponents[i] = (uint) UVComponentCount[i];
            }
        }

        //Faces
        if(nativeValue.MNumFaces > 0)
            nativeValue.MFaces = MemoryHelper.ToNativeArray<Face, AiFace>(Faces.ToArray());

        //Bones
        if(nativeValue.MNumBones > 0)
            nativeValue.MBones = MemoryHelper.ToNativeArray<Bone, AiBone>(Bones.ToArray(), true);

        //Attachment meshes
        if(nativeValue.MNumAnimMeshes > 0)
            nativeValue.MAnimMeshes = MemoryHelper.ToNativeArray<MeshAnimationAttachment, AiAnimMesh>(MeshAnimationAttachments.ToArray());
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<Mesh, AiMesh>.FromNative(in AiMesh nativeValue)
    {
        ClearBuffers();

        var vertexCount = (int) nativeValue.MNumVertices;
        Name = nativeValue.MName; //Avoid struct copy
        MaterialIndex = (int) nativeValue.MMaterialIndex;
        MorphMethod = nativeValue.MMethod;
        BoundingBox = nativeValue.MAABB;
        PrimitiveType = (PrimitiveType)nativeValue.MPrimitiveTypes;       

        //Load Per-vertex components
        if(vertexCount > 0)
        {

            //Positions
            if(nativeValue.MVertices != null)
                Vertices.AddRange(MemoryHelper.FromNativeArray(nativeValue.MVertices, vertexCount));

            //Normals
            if(nativeValue.MNormals != null)
                Normals.AddRange(MemoryHelper.FromNativeArray(nativeValue.MNormals, vertexCount));

            //Tangents
            if(nativeValue.MTangents != null)
                Tangents.AddRange(MemoryHelper.FromNativeArray(nativeValue.MTangents, vertexCount));

            //BiTangents
            if(nativeValue.MBitangents != null)
                BiTangents.AddRange(MemoryHelper.FromNativeArray(nativeValue.MBitangents, vertexCount));

            //Vertex Color channels
            for(var i = 0; i < 8; i++) //TODO: Maybe find a constant somewhere to use to replace
            {
                var colorPtr = nativeValue.MColors[i];

                if(colorPtr != null)
                    VertexColorChannels[i].AddRange(MemoryHelper.FromNativeArray((Color4D*)colorPtr, vertexCount));
            }

            //Texture coordinate channels
            for(var i = 0; i < 8; i++)
            {
                var texCoordsPtr = nativeValue.MTextureCoords[i];

                if(texCoordsPtr != null)
                    TextureCoordinateChannels[i].AddRange(MemoryHelper.FromNativeArray(texCoordsPtr, vertexCount));
            }

            //UV components for each tex coordinate channel
            for(var i = 0; i < 8; i++)
            {
                UVComponentCount[i] = (int) nativeValue.MNumUVComponents[i];
            }
        }

        //Faces
        if(nativeValue.MNumFaces > 0 && nativeValue.MFaces != null)
            Faces.AddRange(MemoryHelper.FromNativeArray<Face, AiFace>((nint)nativeValue.MFaces, (int) nativeValue.MNumFaces));

        //Bones
        if(nativeValue.MNumBones > 0 && nativeValue.MBones != null)
            Bones.AddRange(MemoryHelper.FromNativeArray<Bone, AiBone>((nint)nativeValue.MBones, (int) nativeValue.MNumBones, true));

        //Attachment meshes
        if(nativeValue.MNumAnimMeshes > 0 && nativeValue.MAnimMeshes != null)
            MeshAnimationAttachments.AddRange(MemoryHelper.FromNativeArray<MeshAnimationAttachment, AiAnimMesh>((nint)nativeValue.MAnimMeshes, (int) nativeValue.MNumAnimMeshes, true));
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Mesh, AiMesh}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == null)
            return;

        var aiMesh = MemoryHelper.Read<AiMesh>(nativeValue);

        if(aiMesh.MNumVertices > 0)
        {
            if(aiMesh.MVertices != null)
                MemoryHelper.FreeMemory(aiMesh.MVertices);

            if(aiMesh.MNormals != null)
                MemoryHelper.FreeMemory(aiMesh.MNormals);

            if(aiMesh.MTangents != null)
                MemoryHelper.FreeMemory(aiMesh.MTangents);

            if(aiMesh.MBitangents != null)
                MemoryHelper.FreeMemory(aiMesh.MBitangents);

            //Vertex Color channels
            for(var i = 0; i < 8; i++)
            {
                var colorPtr = aiMesh.MColors[i];

                if(colorPtr != null)
                    MemoryHelper.FreeMemory(colorPtr);
            }

            //Texture coordinate channels
            for(var i = 0; i < 8; i++)
            {
                var texCoordsPtr = aiMesh.MTextureCoords[i];

                if(texCoordsPtr != null)
                    MemoryHelper.FreeMemory(texCoordsPtr);
            }
        }

        //Faces
        if(aiMesh.MNumFaces > 0 && aiMesh.MFaces != null)
            MemoryHelper.FreeNativeArray<AiFace>(aiMesh.MFaces, (int) aiMesh.MNumFaces, Face.FreeNative);

        //Bones
        if(aiMesh.MNumBones > 0 && aiMesh.MBones != null)
            MemoryHelper.FreeNativeArray<AiBone>(aiMesh.MBones, (int) aiMesh.MNumBones, Bone.FreeNative, true);

        //Attachment meshes
        if(aiMesh.MNumAnimMeshes > 0 && aiMesh.MAnimMeshes != null)
            MemoryHelper.FreeNativeArray<AiAnimMesh>(aiMesh.MAnimMeshes, (int) aiMesh.MNumAnimMeshes, MeshAnimationAttachment.FreeNative, true);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}