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

using System.Globalization;
using System.Numerics;
using Silk.NET.Assimp;

namespace Assimp;

/// <summary>
/// Represents a container for holding metadata, representing as key-value pairs.
/// </summary>
public sealed class Metadata : Dictionary<string, Metadata.Entry>, IMarshalable<Metadata, AiMetadata>
{
    /// <summary>
    /// Constructs a new instance of the <see cref="Metadata"/> class.
    /// </summary>
    public Metadata() { }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Metadata, AiMetadata>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<Metadata, AiMetadata>.ToNative(nint thisPtr, out AiMetadata nativeValue)
    {
        nativeValue = new()
        {
            MNumProperties = (uint) Count
        };

        var keys = new AssimpString[Count];
        var entries = new AiMetadataEntry[Count];
        var index = 0;
        foreach(var kv in this)
        {
            var entry = new AiMetadataEntry
            {
                MType = kv.Value.DataType
            };

            nint mData = 0;
            switch(kv.Value.DataType)
            {
                case MetadataType.Bool:
                    mData = MemoryHelper.AllocateMemory(sizeof(bool));
                    var boolValue = (bool) kv.Value.Data;
                    MemoryHelper.Write(mData, boolValue);
                    break;
                case MetadataType.Float:
                    mData = MemoryHelper.AllocateMemory(sizeof(float));
                    var floatValue = (float) kv.Value.Data;
                    MemoryHelper.Write(mData, floatValue);
                    break;
                case MetadataType.Double:
                    mData = MemoryHelper.AllocateMemory(sizeof(double));
                    var doubleValue = (double) kv.Value.Data;
                    MemoryHelper.Write(mData, doubleValue);
                    break;
                case MetadataType.Int32:
                    mData = MemoryHelper.AllocateMemory(sizeof(int));
                    var intValue = (int) kv.Value.Data;
                    MemoryHelper.Write(mData, intValue);
                    break;
                case MetadataType.Aistring:
                    mData = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<AssimpString>());
                    var aiStringValue = new AssimpString(kv.Value.Data as string);
                    MemoryHelper.Write(mData, aiStringValue);
                    break;
                case MetadataType.Uint64:
                    mData = MemoryHelper.AllocateMemory(sizeof(ulong));
                    var uint64Value = (ulong) kv.Value.Data;
                    MemoryHelper.Write(mData, uint64Value);
                    break;
                case MetadataType.Aivector3D:
                    mData = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<Vector3>());
                    var vectorValue = (Vector3) kv.Value.Data;
                    MemoryHelper.Write(mData, vectorValue);
                    break;
            }

            entry.MData = (void*)mData;

            keys[index] = new(kv.Key);
            entries[index] = entry;
            index++;
        }

        nativeValue.MKeys = MemoryHelper.ToNativeArray<AssimpString>(keys);
        nativeValue.MValues = MemoryHelper.ToNativeArray<AiMetadataEntry>(entries);
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<Metadata, AiMetadata>.FromNative(in AiMetadata nativeValue)
    {
        Clear();

        if(nativeValue.MNumProperties == 0 || nativeValue.MKeys == null || nativeValue.MValues == null)
            return;

        var keys = MemoryHelper.FromNativeArray(nativeValue.MKeys, (int) nativeValue.MNumProperties);
        var entries = MemoryHelper.FromNativeArray(nativeValue.MValues, (int) nativeValue.MNumProperties);

        for(var i = 0; i < nativeValue.MNumProperties; i++)
        {
            var key = keys[i].AsString;
            var entry = entries[i];

            if(string.IsNullOrEmpty(key) || entry.MData == null)
                continue;

            object data = null;
            var mData = (nint)entry.MData;
            switch(entry.MType)
            {
                case MetadataType.Bool:
                    data = MemoryHelper.Read<bool>(mData);
                    break;
                case MetadataType.Float:
                    data = MemoryHelper.Read<float>(mData);
                    break;
                case MetadataType.Double:
                    data = MemoryHelper.Read<double>(mData);
                    break;
                case MetadataType.Int32:
                    data = MemoryHelper.Read<int>(mData);
                    break;
                case MetadataType.Int64:
                    data = MemoryHelper.Read<long>(mData);
                    break;
                case MetadataType.Uint32:
                    data = MemoryHelper.Read<uint>(mData);
                    break;
                case MetadataType.Uint64:
                    data = MemoryHelper.Read<ulong>(mData);
                    break;
                case MetadataType.Aivector3D:
                    data = MemoryHelper.Read<Vector3>(mData);
                    break;
                case MetadataType.Aistring:
                    var aiString = MemoryHelper.Read<AssimpString>(mData);
                    data = aiString.AsString;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if(data != null)
                Add(key, new(entry.MType, data));
        }
    }
    
    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Metadata, AiMetadata}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMetadata = MemoryHelper.MarshalStructure<AiMetadata>(nativeValue);

        if(aiMetadata.MKeys != null)
            MemoryHelper.FreeMemory(aiMetadata.MKeys);

        if(aiMetadata.MValues != null)
        {
            var entries = MemoryHelper.FromNativeArray(aiMetadata.MValues, (int) aiMetadata.MNumProperties);

            foreach(var entry in entries)
            {
                if(entry.MData != null)
                    MemoryHelper.FreeMemory((nint)entry.MData);
            }

            MemoryHelper.FreeMemory(aiMetadata.MValues);
        }

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion

    /// <summary>
    /// Represents an entry in a metadata container.
    /// </summary>
    public readonly struct Entry : IEquatable<Entry>
    {
        /// <summary>
        /// Gets the type of metadata.
        /// </summary>
        public MetadataType DataType { get; }

        /// <summary>
        /// Gets the metadata data stored in this entry.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Constructs a new instance of the <see cref="Entry"/> struct.
        /// </summary>
        /// <param name="dataType">Type of the data.</param>
        /// <param name="data">The data.</param>
        public Entry(MetadataType dataType, object data)
        {
            DataType = dataType;
            Data = data;
        }

        /// <summary>
        /// Tests equality between two entries.
        /// </summary>
        /// <param name="a">First entry</param>
        /// <param name="b">Second entry</param>
        /// <returns>True if the entries are equal, false otherwise</returns>
        public static bool operator ==(Entry a, Entry b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Tests inequality between two entries.
        /// </summary>
        /// <param name="a">First entry</param>
        /// <param name="b">Second entry</param>
        /// <returns>True if the entries are not equal, false otherwise</returns>
        public static bool operator !=(Entry a, Entry b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Gets the data as the specified type. If it cannot be casted to the type, then null is returned.
        /// </summary>
        /// <typeparam name="T">Type to cast the data to.</typeparam>
        /// <returns>Casted data or null.</returns>
        public T? DataAs<T>() where T : struct
        {
            var dataTypeType = DataType switch
            {
                MetadataType.Bool => typeof(bool),
                MetadataType.Float => typeof(float),
                MetadataType.Double => typeof(double),
                MetadataType.Int32 => typeof(int),
                MetadataType.Aistring => typeof(string),
                MetadataType.Uint64 => typeof(ulong),
                MetadataType.Aivector3D => typeof(Vector3),
                _ => null
            };

            if(dataTypeType == typeof(T))
                return (T) Data;

            return null;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>True if the specified <see cref="System.Object" /> is equal to this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if(obj is Entry entry)
                return Equals(entry);

            return false;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>True if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(Entry other)
        {
            if(other.DataType != DataType)
                return false;

            return Equals(other.Data, Data);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Data.GetHashCode();
                hash = hash * 31 + (Data == null ? 0 : Data.GetHashCode());

                return hash;
            }
        }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "DataType: {0}, Data: {1}", [DataType.ToString(), Data == null ? "null" : Data.ToString()
            ]);
        }
    }
}