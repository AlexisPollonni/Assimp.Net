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
/// A node in the imported model hierarchy.
/// </summary>
public sealed class Node : IMarshalable<Node, AiNode>
{
    /// <summary>
    /// Gets or sets the name of the node.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the transformation of the node relative to its parent.
    /// </summary>
    public Matrix4x4 Transform { get; set; }

    /// <summary>
    /// Gets the node's parent, if it exists. 
    /// </summary>
    public Node Parent { get; private set; }

    /// <summary>
    /// Gets the number of children that is owned by this node.
    /// </summary>
    public int ChildCount => Children.Count;

    /// <summary>
    /// Gets if the node contains children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Gets the node's children.
    /// </summary>
    public NodeCollection Children { get; }

    /// <summary>
    /// Gets the number of meshes referenced by this node.
    /// </summary>
    public int MeshCount => MeshIndices.Count;

    /// <summary>
    /// Gets if the node contains mesh references.
    /// </summary>
    public bool HasMeshes => MeshIndices.Count > 0;

    /// <summary>
    /// Gets the indices of the meshes referenced by this node. Meshes can be
    /// shared between nodes, so there is a mesh collection owned by the scene
    /// that each node can reference.
    /// </summary>
    public List<int> MeshIndices { get; }

    /// <summary>
    /// Gets the node's metadata container.
    /// </summary>
    public Metadata Metadata { get; }

    /// <summary>
    /// Constructs a new instance of the <see cref="Node"/> class.
    /// </summary>
    public Node()
    {
        Name = string.Empty;
        Transform = Matrix4x4.Identity;
        Parent = null;
        Children = new(this);
        MeshIndices = new();
        Metadata = new();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="name">Name of the node</param>
    public Node(string name)
        : this()
    {
        Name = name;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Node"/> class.
    /// </summary>
    /// <param name="name">Name of the node</param>
    /// <param name="parent">Parent of the node</param>
    public Node(string name, Node parent)
        : this()
    {
        Name = name;
        Parent = parent;
    }

    //Internal use - sets the node parent in NodeCollection
    internal void SetParent(Node parent)
    {
        Parent = parent;
    }

    /// <summary>
    /// Finds a node with the specific name, which may be this node
    /// or any children or children's children, and so on, if it exists.
    /// </summary>
    /// <param name="name">Node name</param>
    /// <returns>The node or null if it does not exist</returns>
    public Node FindNode(string name)
    {
        if(name.Equals(Name))
            return this;

        if(HasChildren)
        {
            foreach(var child in Children)
            {
                var found = child.FindNode(name);
                if(found != null)
                    return found;
            }
        }
        //No child found
        return null;
    }

    private nint ToNativeRecursive(nint parentPtr, Node node)
    {
        if(node == null)
            return nint.Zero;

        var sizeofNative = MemoryHelper.SizeOf<AiNode>();

        //Allocate the memory that will hold the node
        var nodePtr = MemoryHelper.AllocateMemory(sizeofNative);

        //First fill the native struct
        AiNode nativeValue;
        nativeValue.Name = new(node.Name);
        nativeValue.Transformation = node.Transform;
        nativeValue.Parent = parentPtr;

        nativeValue.NumMeshes = (uint) node.MeshIndices.Count;
        nativeValue.Meshes = MemoryHelper.ToNativeArray(node.MeshIndices.ToArray());
        nativeValue.MetaData = nint.Zero;

        //If has metadata, create it, otherwise it should be NULL
        if (Metadata.Count > 0)
            nativeValue.MetaData = MemoryHelper.ToNativePointer<Metadata, AiMetadata>(Metadata);

        //Now descend through the children
        nativeValue.NumChildren = (uint) node.Children.Count;

        var numChildren = (int) nativeValue.NumChildren;
        var stride = nint.Size;

        var childrenPtr = nint.Zero;

        if(numChildren > 0)
        {
            childrenPtr = MemoryHelper.AllocateMemory(numChildren * nint.Size);

            for(var i = 0; i < numChildren; i++)
            {
                var currPos = MemoryHelper.AddIntPtr(childrenPtr, stride * i);
                var child = node.Children[i];

                var childPtr = nint.Zero;

                //Recursively create the children and its children
                if(child != null)
                {
                    childPtr = ToNativeRecursive(nodePtr, child);
                }

                //Write the child's node ptr to our array
                MemoryHelper.Write(currPos, childPtr);
            }
        }

        //Finall finish writing to the native struct, and write the whole thing to the memory we allocated previously
        nativeValue.Children = childrenPtr;
        MemoryHelper.Write(nodePtr, nativeValue);

        return nodePtr;
    }

    #region IMarshalable Implemention

    /// <summary>
    /// Gets a value indicating whether this instance is native blittable.
    /// </summary>
    bool IMarshalable<Node, AiNode>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<Node, AiNode>.ToNative(nint thisPtr, out AiNode nativeValue)
    {
        nativeValue.Name = new(Name);
        nativeValue.Transformation = Transform;
        nativeValue.Parent = nint.Zero;

        nativeValue.NumMeshes = (uint) MeshIndices.Count;
        nativeValue.Meshes = nint.Zero;
        nativeValue.MetaData = nint.Zero;

        //If has metadata, create it, otherwise it should be NULL
        if(Metadata.Count > 0)
            nativeValue.MetaData = MemoryHelper.ToNativePointer<Metadata, AiMetadata>(Metadata);

        if(nativeValue.NumMeshes > 0)
            nativeValue.Meshes = MemoryHelper.ToNativeArray(MeshIndices.ToArray());

        //Now descend through the children
        nativeValue.NumChildren = (uint) Children.Count;

        var numChildren = (int) nativeValue.NumChildren;
        var stride = nint.Size;

        var childrenPtr = nint.Zero;

        if(numChildren > 0)
        {
            childrenPtr = MemoryHelper.AllocateMemory(numChildren * nint.Size);

            for(var i = 0; i < numChildren; i++)
            {
                var currPos = MemoryHelper.AddIntPtr(childrenPtr, stride * i);
                var child = Children[i];

                var childPtr = nint.Zero;

                //Recursively create the children and its children
                if(child != null)
                {
                    childPtr = ToNativeRecursive(thisPtr, child);
                }

                //Write the child's node ptr to our array
                MemoryHelper.Write(currPos, childPtr);
            }
        }

        //Finally finish writing to the native struct
        nativeValue.Children = childrenPtr;
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<Node, AiNode>.FromNative(in AiNode nativeValue)
    {
        Name = AiString.GetString(nativeValue.Name); //Avoid struct copy
        Transform = nativeValue.Transformation;
        Parent = null;
        Children.Clear();
        MeshIndices.Clear();
        Metadata.Clear();

        if(nativeValue.MetaData != nint.Zero)
        {
            var data = MemoryHelper.FromNativePointer<Metadata, AiMetadata>(nativeValue.MetaData);
            foreach(var kv in data)
                Metadata.Add(kv.Key, kv.Value);
        }

        if(nativeValue.NumMeshes > 0 && nativeValue.Meshes != nint.Zero)
            MeshIndices.AddRange(MemoryHelper.FromNativeArray<int>(nativeValue.Meshes, (int) nativeValue.NumMeshes));

        if(nativeValue.NumChildren > 0 && nativeValue.Children != nint.Zero)
            Children.AddRange(MemoryHelper.FromNativeArray<Node, AiNode>(nativeValue.Children, (int) nativeValue.NumChildren, true));
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Node, AiNode}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiNode = MemoryHelper.Read<AiNode>(nativeValue);

        if(aiNode.NumMeshes > 0 && aiNode.Meshes != nint.Zero)
            MemoryHelper.FreeMemory(aiNode.Meshes);

        if(aiNode.NumChildren > 0 && aiNode.Children != nint.Zero)
            MemoryHelper.FreeNativeArray<AiNode>(aiNode.Children, (int) aiNode.NumChildren, FreeNative, true);

        if(aiNode.MetaData != nint.Zero)
            Metadata.FreeNative(aiNode.MetaData, true);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}