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
    public List<Vector3D> Vertices { get; }

    /// <summary>
    /// Gets if the mesh as normals. If it does exist, the count should be the same as the vertex count.
    /// </summary>
    public bool HasNormals => Normals.Count > 0;

    /// <summary>
    /// Gets the vertex normal list.
    /// </summary>
    public List<Vector3D> Normals { get; }

    /// <summary>
    /// Gets if the mesh has tangents and bitangents. It is not
    /// possible for one to be without the other. If it does exist, the count should be the same as the vertex count.
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
    public List<Vector3D>[] TextureCoordinateChannels { get; }

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
    public MeshMorphingMethod MorphMethod { get; set; }

    /// <summary>
    /// Gets or sets the axis aligned bounding box that contains the extents of the mesh.
    /// </summary>
    public BoundingBox BoundingBox { get; set; }

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
        MorphMethod = MeshMorphingMethod.None;

        Vertices = new();
        Normals = new();
        Tangents = new();
        BiTangents = new();
        VertexColorChannels = new List<Color4D>[AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS];

        for(var i = 0; i < VertexColorChannels.Length; i++)
        {
            VertexColorChannels[i] = new();
        }

        TextureCoordinateChannels = new List<Vector3D>[AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS];

        for(var i = 0; i < TextureCoordinateChannels.Length; i++)
        {
            TextureCoordinateChannels[i] = new();
        }

        UVComponentCount = new int[AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS];
        Bones = new();
        Faces = new();
        MeshAnimationAttachments = new();
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
                VertexColorChannels[i] = new();
            else
                colors.Clear();
        }

        for(var i = 0; i < TextureCoordinateChannels.Length; i++)
        {
            var texCoords = TextureCoordinateChannels[i];

            if(texCoords == null)
                TextureCoordinateChannels[i] = new();
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

    private Vector3D[] CopyTo(List<Vector3D> list, Vector3D[] copy)
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
    void IMarshalable<Mesh, AiMesh>.ToNative(nint thisPtr, out AiMesh nativeValue)
    {
        nativeValue.Name = new(Name);
        nativeValue.Vertices = nint.Zero;
        nativeValue.Normals = nint.Zero;
        nativeValue.Tangents = nint.Zero;
        nativeValue.BiTangents = nint.Zero;
        nativeValue.AnimMeshes = nint.Zero;
        nativeValue.Bones = nint.Zero;
        nativeValue.Faces = nint.Zero;
        nativeValue.Colors = new();
        nativeValue.TextureCoords = new();
        nativeValue.NumUVComponents = new();
        nativeValue.PrimitiveTypes = PrimitiveType;
        nativeValue.MaterialIndex = (uint) MaterialIndex;
        nativeValue.NumVertices = (uint) VertexCount;
        nativeValue.NumBones = (uint) BoneCount;
        nativeValue.NumFaces = (uint) FaceCount;
        nativeValue.NumAnimMeshes = (uint) MeshAnimationAttachmentCount;
        nativeValue.MorphMethod = MorphMethod;
        nativeValue.AABB = BoundingBox;
        nativeValue.TextureCoordsNames = nint.Zero;

        if(nativeValue.NumVertices > 0)
        {

            //Since we can have so many buffers of Vector3D with same length, lets re-use a buffer
            var copy = new Vector3D[nativeValue.NumVertices];

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

            //UV components for each tex coordinate channel
            for(var i = 0; i < UVComponentCount.Length; i++)
            {
                nativeValue.NumUVComponents[i] = (uint) UVComponentCount[i];
            }
        }

        //Faces
        if(nativeValue.NumFaces > 0)
            nativeValue.Faces = MemoryHelper.ToNativeArray<Face, AiFace>(Faces.ToArray());

        //Bones
        if(nativeValue.NumBones > 0)
            nativeValue.Bones = MemoryHelper.ToNativeArray<Bone, AiBone>(Bones.ToArray(), true);

        //Attachment meshes
        if(nativeValue.NumAnimMeshes > 0)
            nativeValue.AnimMeshes = MemoryHelper.ToNativeArray<MeshAnimationAttachment, AiAnimMesh>(MeshAnimationAttachments.ToArray());
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<Mesh, AiMesh>.FromNative(in AiMesh nativeValue)
    {
        ClearBuffers();

        var vertexCount = (int) nativeValue.NumVertices;
        Name = AiString.GetString(nativeValue.Name); //Avoid struct copy
        MaterialIndex = (int) nativeValue.MaterialIndex;
        MorphMethod = nativeValue.MorphMethod;
        BoundingBox = nativeValue.AABB;
        PrimitiveType = nativeValue.PrimitiveTypes;       

        //Load Per-vertex components
        if(vertexCount > 0)
        {

            //Positions
            if(nativeValue.Vertices != nint.Zero)
                Vertices.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.Vertices, vertexCount));

            //Normals
            if(nativeValue.Normals != nint.Zero)
                Normals.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.Normals, vertexCount));

            //Tangents
            if(nativeValue.Tangents != nint.Zero)
                Tangents.AddRange(MemoryHelper.FromNativeArray<Vector3D>(nativeValue.Tangents, vertexCount));

            //BiTangents
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

            //UV components for each tex coordinate channel
            for(var i = 0; i < nativeValue.NumUVComponents.Length; i++)
            {
                UVComponentCount[i] = (int) nativeValue.NumUVComponents[i];
            }
        }

        //Faces
        if(nativeValue.NumFaces > 0 && nativeValue.Faces != nint.Zero)
            Faces.AddRange(MemoryHelper.FromNativeArray<Face, AiFace>(nativeValue.Faces, (int) nativeValue.NumFaces));

        //Bones
        if(nativeValue.NumBones > 0 && nativeValue.Bones != nint.Zero)
            Bones.AddRange(MemoryHelper.FromNativeArray<Bone, AiBone>(nativeValue.Bones, (int) nativeValue.NumBones, true));

        //Attachment meshes
        if(nativeValue.NumAnimMeshes > 0 && nativeValue.AnimMeshes != nint.Zero)
            MeshAnimationAttachments.AddRange(MemoryHelper.FromNativeArray<MeshAnimationAttachment, AiAnimMesh>(nativeValue.AnimMeshes, (int) nativeValue.NumAnimMeshes, true));
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Mesh, AiMesh}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMesh = MemoryHelper.Read<AiMesh>(nativeValue);

        if(aiMesh.NumVertices > 0)
        {
            if(aiMesh.Vertices != nint.Zero)
                MemoryHelper.FreeMemory(aiMesh.Vertices);

            if(aiMesh.Normals != nint.Zero)
                MemoryHelper.FreeMemory(aiMesh.Normals);

            if(aiMesh.Tangents != nint.Zero)
                MemoryHelper.FreeMemory(aiMesh.Tangents);

            if(aiMesh.BiTangents != nint.Zero)
                MemoryHelper.FreeMemory(aiMesh.BiTangents);

            //Vertex Color channels
            for(var i = 0; i < aiMesh.Colors.Length; i++)
            {
                var colorPtr = aiMesh.Colors[i];

                if(colorPtr != nint.Zero)
                    MemoryHelper.FreeMemory(colorPtr);
            }

            //Texture coordinate channels
            for(var i = 0; i < aiMesh.TextureCoords.Length; i++)
            {
                var texCoordsPtr = aiMesh.TextureCoords[i];

                if(texCoordsPtr != nint.Zero)
                    MemoryHelper.FreeMemory(texCoordsPtr);
            }
        }

        //Faces
        if(aiMesh.NumFaces > 0 && aiMesh.Faces != nint.Zero)
            MemoryHelper.FreeNativeArray<AiFace>(aiMesh.Faces, (int) aiMesh.NumFaces, Face.FreeNative);

        //Bones
        if(aiMesh.NumBones > 0 && aiMesh.Bones != nint.Zero)
            MemoryHelper.FreeNativeArray<AiBone>(aiMesh.Bones, (int) aiMesh.NumBones, Bone.FreeNative, true);

        //Attachment meshes
        if(aiMesh.NumAnimMeshes > 0 && aiMesh.AnimMeshes != nint.Zero)
            MemoryHelper.FreeNativeArray<AiAnimMesh>(aiMesh.AnimMeshes, (int) aiMesh.NumAnimMeshes, MeshAnimationAttachment.FreeNative, true);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}