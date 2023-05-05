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
using System.Globalization;
using Assimp.Unmanaged;

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
    void IMarshalable<Metadata, AiMetadata>.ToNative(nint thisPtr, out AiMetadata nativeValue)
    {
        nativeValue = new()
        {
            NumProperties = (uint) Count
        };

        var keys = new AiString[Count];
        var entries = new AiMetadataEntry[Count];
        var index = 0;
        foreach(var kv in this)
        {
            var entry = new AiMetadataEntry
            {
                DataType = kv.Value.DataType
            };

            switch(kv.Value.DataType)
            {
                case MetaDataType.Bool:
                    entry.Data = MemoryHelper.AllocateMemory(sizeof(bool));
                    var boolValue = (bool) kv.Value.Data;
                    MemoryHelper.Write(entry.Data, boolValue);
                    break;
                case MetaDataType.Float:
                    entry.Data = MemoryHelper.AllocateMemory(sizeof(float));
                    var floatValue = (float) kv.Value.Data;
                    MemoryHelper.Write(entry.Data, floatValue);
                    break;
                case MetaDataType.Double:
                    entry.Data = MemoryHelper.AllocateMemory(sizeof(double));
                    var doubleValue = (double) kv.Value.Data;
                    MemoryHelper.Write(entry.Data, doubleValue);
                    break;
                case MetaDataType.Int32:
                    entry.Data = MemoryHelper.AllocateMemory(sizeof(int));
                    var intValue = (int) kv.Value.Data;
                    MemoryHelper.Write(entry.Data, intValue);
                    break;
                case MetaDataType.String:
                    entry.Data = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<AiString>());
                    var aiStringValue = new AiString(kv.Value.Data as string);
                    MemoryHelper.Write(entry.Data, aiStringValue);
                    break;
                case MetaDataType.UInt64:
                    entry.Data = MemoryHelper.AllocateMemory(sizeof(ulong));
                    var uint64Value = (ulong) kv.Value.Data;
                    MemoryHelper.Write(entry.Data, uint64Value);
                    break;
                case MetaDataType.Vector3D:
                    entry.Data = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<Vector3D>());
                    var vectorValue = (Vector3D) kv.Value.Data;
                    MemoryHelper.Write(entry.Data, vectorValue);
                    break;
            }

            keys[index] = new(kv.Key);
            entries[index] = entry;
            index++;
        }

        nativeValue.keys = MemoryHelper.ToNativeArray(keys);
        nativeValue.Values = MemoryHelper.ToNativeArray(entries);
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<Metadata, AiMetadata>.FromNative(in AiMetadata nativeValue)
    {
        Clear();

        if(nativeValue.NumProperties == 0 || nativeValue.keys == nint.Zero || nativeValue.Values == nint.Zero)
            return;

        var keys = MemoryHelper.FromNativeArray<AiString>(nativeValue.keys, (int) nativeValue.NumProperties);
        var entries = MemoryHelper.FromNativeArray<AiMetadataEntry>(nativeValue.Values, (int) nativeValue.NumProperties);

        for(var i = 0; i < nativeValue.NumProperties; i++)
        {
            var key = keys[i].GetString();
            var entry = entries[i];

            if(string.IsNullOrEmpty(key) || entry.Data == nint.Zero)
                continue;

            object data = null;
            switch(entry.DataType)
            {
                case MetaDataType.Bool:
                    data = MemoryHelper.Read<bool>(entry.Data);
                    break;
                case MetaDataType.Float:
                    data = MemoryHelper.Read<float>(entry.Data);
                    break;
                case MetaDataType.Double:
                    data = MemoryHelper.Read<double>(entry.Data);
                    break;
                case MetaDataType.Int32:
                    data = MemoryHelper.Read<int>(entry.Data);
                    break;
                case MetaDataType.String:
                    var aiString = MemoryHelper.Read<AiString>(entry.Data);
                    data = aiString.GetString();
                    break;
                case MetaDataType.UInt64:
                    data = MemoryHelper.Read<ulong>(entry.Data);
                    break;
                case MetaDataType.Vector3D:
                    data = MemoryHelper.Read<Vector3D>(entry.Data);
                    break;
            }

            if(data != null)
                Add(key, new(entry.DataType, data));
        }
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Metadata, AiMetadata}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMetadata = MemoryHelper.MarshalStructure<AiMetadata>(nativeValue);

        if(aiMetadata.keys != nint.Zero)
            MemoryHelper.FreeMemory(aiMetadata.keys);

        if(aiMetadata.Values != nint.Zero)
        {
            var entries = MemoryHelper.FromNativeArray<AiMetadataEntry>(aiMetadata.Values, (int) aiMetadata.NumProperties);

            foreach(var entry in entries)
            {
                if(entry.Data != nint.Zero)
                    MemoryHelper.FreeMemory(entry.Data);
            }

            MemoryHelper.FreeMemory(aiMetadata.Values);
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
        public MetaDataType DataType { get; }

        /// <summary>
        /// Gets the metadata data stored in this entry.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Constructs a new instance of the <see cref="Entry"/> struct.
        /// </summary>
        /// <param name="dataType">Type of the data.</param>
        /// <param name="data">The data.</param>
        public Entry(MetaDataType dataType, object data)
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
                MetaDataType.Bool => typeof(bool),
                MetaDataType.Float => typeof(float),
                MetaDataType.Double => typeof(double),
                MetaDataType.Int32 => typeof(int),
                MetaDataType.String => typeof(string),
                MetaDataType.UInt64 => typeof(ulong),
                MetaDataType.Vector3D => typeof(Vector3D),
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
            return string.Format(CultureInfo.CurrentCulture, "DataType: {0}, Data: {1}", new object[] { DataType.ToString(), Data == null ? "null" : Data.ToString() });
        }
    }
}