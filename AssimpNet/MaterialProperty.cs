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
using System.Text;
using Silk.NET.Assimp;
using TextureType = Silk.NET.Assimp.TextureType;

namespace Assimp;

/// <summary>
/// A key-value pairing that represents some material property.
/// </summary>
public sealed class MaterialProperty : IMarshalable<MaterialProperty, AiMaterialProperty>
{
    private string _name;
    private TextureType _texType;
    private int _texIndex;
    private string _fullyQualifiedName;
    private bool _fullQualifiedNameNeedsUpdate = true;

    /// <summary>
    /// Gets or sets the property key name. E.g. $tex.file. This corresponds to the
    /// "AiMatKeys" base name constants.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            _fullQualifiedNameNeedsUpdate = true;

            AssertIsBaseName();
        }
    }

    /// <summary>
    /// Gets or sets the type of property.
    /// </summary>
    public PropertyTypeInfo PropertyType { get; set; }

    /// <summary>
    /// Gets the raw byte data count.
    /// </summary>
    public int ByteCount => RawData?.Length ?? 0;

    /// <summary>
    /// Checks if the property has data.
    /// </summary>
    public bool HasRawData => RawData != null;

    /// <summary>
    /// Gets the raw byte data. To modify/read this data, see the Get/SetXXXValue methods.
    /// </summary>
    public byte[] RawData { get; private set; }

    /// <summary>
    /// Gets or sets the texture type semantic, for non-texture properties this is always <see cref="Silk.NET.Assimp.TextureType.None"/>.
    /// </summary>
    public TextureType TextureType
    {
        get => _texType;
        set
        {
            _texType = value;
            _fullQualifiedNameNeedsUpdate = true;
        }
    }

    /// <summary>
    /// Gets or sets the texture index, for non-texture properties this is always zero.
    /// </summary>
    public int TextureIndex
    {
        get => _texIndex;
        set
        {
            _texIndex = value;
            _fullQualifiedNameNeedsUpdate = true;
        }
    }

    /// <summary>
    /// Gets the property's fully qualified name. Format: "{base name},{texture type semantic},{texture index}". E.g. "$clr.diffuse,0,0". This
    /// is the key that is used to index the property in the material property map.
    /// </summary>
    public string FullyQualifiedName
    {
        get
        {
            if(_fullQualifiedNameNeedsUpdate)
            {
                _fullyQualifiedName = Material.CreateFullyQualifiedName(_name, _texType, _texIndex);
                _fullQualifiedNameNeedsUpdate = false;
            }

            return _fullyQualifiedName;
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class.
    /// </summary>
    public MaterialProperty()
    {
        _name = string.Empty;
        PropertyType = PropertyTypeInfo.Buffer;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Constructs a buffer property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="buffer">Property value</param>
    public MaterialProperty(string baseName, byte[] buffer)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Buffer;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = buffer;

        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Constructs a float property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="value">Property value</param>
    public MaterialProperty(string baseName, float value)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Float;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetFloatValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Constructs an integer property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="value">Property value</param>
    public MaterialProperty(string baseName, int value)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Integer;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetIntegerValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Constructs a boolean property.
    /// </summary>
    /// <param name="baseName">Name of the property</param>
    /// <param name="value">Property value</param>
    public MaterialProperty(string baseName, bool value)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Integer;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetBooleanValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Creates a string property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="value">Property value</param>
    public MaterialProperty(string baseName, string value)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.String;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetStringValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Creates a texture property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="value">Property value</param>
    /// <param name="texType">Texture type</param>
    /// <param name="textureIndex">Texture index</param>
    public MaterialProperty(string baseName, string value, TextureType texType, int textureIndex)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.String;
        _texIndex = textureIndex;
        _texType = texType;
        RawData = null;

        SetStringValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Creates a float array property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="values">Property values</param>
    public MaterialProperty(string baseName, float[] values)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Float;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetFloatArrayValue(values);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Creates a int array property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="values">Property values</param>
    public MaterialProperty(string baseName, int[] values)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Integer;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetIntegerArrayValue(values);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Creates a Color3D property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="value">Property value</param>
    public MaterialProperty(string baseName, Color3D value)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Float;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetColor3DValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MaterialProperty"/> class. Creates a Color4D property.
    /// </summary>
    /// <param name="baseName">Base name of the property</param>
    /// <param name="value">Property value</param>
    public MaterialProperty(string baseName, Color4D value)
    {
        _name = baseName;
        PropertyType = PropertyTypeInfo.Float;
        _texIndex = 0;
        _texType = TextureType.None;
        RawData = null;

        SetColor4DValue(value);
        AssertIsBaseName();
    }

    /// <summary>
    /// Gets the property raw data as a float.
    /// </summary>
    /// <returns>Float</returns>
    public float GetFloatValue()
    {
        if(PropertyType is PropertyTypeInfo.Float or PropertyTypeInfo.Integer)
            return GetValueAs<float>();

        return 0;
    }

    /// <summary>
    /// Sets the property raw data with a float.
    /// </summary>
    /// <param name="value">Float.</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetFloatValue(float value)
    {
        if(PropertyType != PropertyTypeInfo.Float && PropertyType != PropertyTypeInfo.Integer)
            return false;

        return SetValueAs(value);
    }

    /// <summary>
    /// Gets the property raw data as a double.
    /// </summary>
    /// <returns>Double</returns>
    public double GetDoubleValue()
    {
        if(PropertyType == PropertyTypeInfo.Double)
            return GetValueAs<double>();
            
        return 0;
    }

    /// <summary>
    /// Sets the property raw data with a double.
    /// </summary>
    /// <param name="value">Double.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool SetDoubleValue(double value)
    {
        if(PropertyType != PropertyTypeInfo.Double)
            return false;

        return SetValueAs(value);
    }

    /// <summary>
    /// Gets the property raw data as an integer.
    /// </summary>
    /// <returns>Integer</returns>
    public int GetIntegerValue()
    {
        if(PropertyType is PropertyTypeInfo.Float or PropertyTypeInfo.Integer)
            return GetValueAs<int>();

        return 0;
    }

    /// <summary>
    /// Sets the property raw data as an integer.
    /// </summary>
    /// <param name="value">Integer</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetIntegerValue(int value)
    {
        if(PropertyType != PropertyTypeInfo.Float && PropertyType != PropertyTypeInfo.Integer)
            return false;

        return SetValueAs(value);
    }

    /// <summary>
    /// Gets the property raw data as a string.
    /// </summary>
    /// <returns>String</returns>
    public string GetStringValue()
    {
        if(PropertyType != PropertyTypeInfo.String)
            return null;

        return GetMaterialString(RawData);
    }

    /// <summary>
    /// Sets the property raw data as string.
    /// </summary>
    /// <param name="value">String</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetStringValue(string value)
    {
        if(PropertyType != PropertyTypeInfo.String)
            return false;

        RawData = SetMaterialString(value, RawData);
        return true;
    }

    /// <summary>
    /// Gets the property raw data as a float array.
    /// </summary>
    /// <param name="count">Number of elements to get</param>
    /// <returns>Float array</returns>
    public float[] GetFloatArrayValue(int count)
    {
        if(PropertyType is PropertyTypeInfo.Float or PropertyTypeInfo.Integer)
            return GetValueArrayAs<float>(count);

        return null;
    }

    /// <summary>
    /// Gets the property raw data as a float array.
    /// </summary>
    /// <returns>Float array</returns>
    public float[] GetFloatArrayValue()
    {
        if(PropertyType is PropertyTypeInfo.Float or PropertyTypeInfo.Integer)
        {
            var count = ByteCount / sizeof(float);
            return GetValueArrayAs<float>(count);
        }

        return null;
    }

    /// <summary>
    /// Sets the property raw data as a float array.
    /// </summary>
    /// <param name="values">Values to set</param>
    /// <returns>True if successful, otherwise false</returns>
    public bool SetFloatArrayValue(float[] values)
    {
        if(PropertyType != PropertyTypeInfo.Float && PropertyType != PropertyTypeInfo.Integer)
            return false;

        return SetValueArrayAs(values);
    }

    /// <summary>
    /// Gets the property raw data as a double array.
    /// </summary>
    /// <returns>Double array</returns>
    public double[] GetDoubleArrayValue()
    {
        if(PropertyType == PropertyTypeInfo.Double)
        {
            var count = ByteCount / sizeof(double);
            return GetValueArrayAs<double>(count);
        }

        return null;
    }

    /// <summary>
    /// Sets the property raw data as a double array.
    /// </summary>
    /// <param name="values">Values to set</param>
    /// <returns>True if successful, otherwise false</returns>
    public bool SetDoubleArrayValue(double[] values)
    {
        if(PropertyType != PropertyTypeInfo.Double)
            return false;

        return SetValueArrayAs(values);
    }

    /// <summary>
    /// Gets the property raw data as an integer array.
    /// </summary>
    /// <param name="count">Number of elements to get</param>
    /// <returns>Integer array</returns>
    public int[] GetIntegerArrayValue(int count)
    {
        if(PropertyType is PropertyTypeInfo.Float or PropertyTypeInfo.Integer)
            return GetValueArrayAs<int>(count);

        return null;
    }

    /// <summary>
    /// Gets the property raw data as an integer array.
    /// </summary>
    /// <returns>Integer array</returns>
    public int[] GetIntegerArrayValue()
    {
        if(PropertyType is PropertyTypeInfo.Float or PropertyTypeInfo.Integer)
        {
            var count = ByteCount / sizeof(int);
            return GetValueArrayAs<int>(count);
        }

        return null;
    }

    /// <summary>
    /// Sets the property raw data as an integer array.
    /// </summary>
    /// <param name="values">Values to set</param>
    /// <returns>True if successful, otherwise false</returns>
    public bool SetIntegerArrayValue(int[] values)
    {
        if(PropertyType != PropertyTypeInfo.Float && PropertyType != PropertyTypeInfo.Integer)
            return false;

        return SetValueArrayAs(values);
    }

    /// <summary>
    /// Gets the property raw data as a boolean.
    /// </summary>
    /// <returns>Boolean</returns>
    public bool GetBooleanValue()
    {
        return GetIntegerValue() != 0;
    }

    /// <summary>
    /// Sets the property raw data as a boolean.
    /// </summary>
    /// <param name="value">Boolean value</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetBooleanValue(bool value)
    {
        return SetIntegerValue(value == false ? 0 : 1);
    }

    /// <summary>
    /// Gets the property raw data as a Color3D.
    /// </summary>
    /// <returns>Color3D</returns>
    public Color3D GetColor3DValue()
    {
        if(PropertyType != PropertyTypeInfo.Float)
            return new();

        return GetValueAs<Color3D>();
    }

    /// <summary>
    /// Sets the property raw data as a Color3D.
    /// </summary>
    /// <param name="value">Color3D</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetColor3DValue(Color3D value)
    {
        if(PropertyType != PropertyTypeInfo.Float)
            return false;

        return SetValueAs(value);
    }

    /// <summary>
    /// Gets the property raw data as a Color4D.
    /// </summary>
    /// <returns>Color4D</returns>
    public Color4D GetColor4DValue()
    {
        if(PropertyType != PropertyTypeInfo.Float || RawData == null)
            return new();

        //We may have a Color that's RGB, so still read it and set alpha to 1.0
        unsafe
        {
            fixed(byte* ptr = RawData)
            {

                if(RawData.Length >= MemoryHelper.SizeOf<Color4D>())
                {
                    return MemoryHelper.Read<Color4D>(new(ptr));
                }

                if(RawData.Length >= MemoryHelper.SizeOf<Color3D>())
                {
                    return new(MemoryHelper.Read<Color3D>(new(ptr)), 1.0f);
                }

            }
        }

        return new();
    }

    /// <summary>
    /// Sets the property raw data as a Color4D.
    /// </summary>
    /// <param name="value">Color4D</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SetColor4DValue(Color4D value)
    {
        if(PropertyType != PropertyTypeInfo.Float)
            return false;

        return SetValueAs(value);
    }

    private unsafe T[] GetValueArrayAs<T>(int count) where T : unmanaged
    {
        var size = MemoryHelper.SizeOf<T>();

        if(RawData != null && RawData.Length >= size * count)
        {
            var array = new T[count];
            fixed(byte* ptr = RawData)
            {
                MemoryHelper.Read((T*)ptr, array, 0, count);
            }

            return array;
        }

        return null;
    }

    private unsafe T GetValueAs<T>() where T : struct
    {
        var size = MemoryHelper.SizeOf<T>();

        if(RawData != null && RawData.Length >= size)
        {
            fixed(byte* ptr = RawData)
            {
                return MemoryHelper.Read<T>(new(ptr));
            }
        }

        return default(T);
    }

    private unsafe bool SetValueArrayAs<T>(T[] data) where T : struct
    {
        if(data == null || data.Length == 0)
            return false;

        var size = MemoryHelper.SizeOf(data);

        //Resize byte array if necessary
        if(RawData == null || RawData.Length != size)
            RawData = new byte[size];

        fixed(byte* ptr = RawData)
            MemoryHelper.Write<T>(new(ptr), data, 0, data.Length);

        return true;
    }

    private unsafe bool SetValueAs<T>(T value) where T : struct
    {
        var size = MemoryHelper.SizeOf<T>();

        //Resize byte array if necessary
        if(RawData == null || RawData.Length != size)
            RawData = new byte[size];

        fixed(byte* ptr = RawData)
            MemoryHelper.Write(new(ptr), value);

        return true;
    }

    private static unsafe string GetMaterialString(byte[] matPropData)
    {
        if(matPropData == null)
            return string.Empty;

        fixed(byte* ptr = &matPropData[0])
        {
            //String is stored as 32 bit length prefix THEN followed by zero-terminated UTF8 data (basically need to reconstruct an AiString)
            AssimpString aiString;
            aiString.Length = (uint) MemoryHelper.Read<int>(new(ptr));

            //Memcpy starting at dataPtr + sizeof(int) for length + 1 (to account for null terminator)
            MemoryHelper.CopyMemory(new(aiString.Data), MemoryHelper.AddIntPtr(new(ptr), sizeof(int)), (int) aiString.Length + 1);

            return aiString.AsString;
        }
    }

    private static unsafe byte[] SetMaterialString(string value, byte[] existing)
    {
        if(string.IsNullOrEmpty(value))
            return null;

        var stringSize = Encoding.UTF8.GetByteCount(value);

        if(stringSize < 0)
            return null;

        var size = stringSize + 1 + sizeof(int);
        var data = existing;

        if(existing == null || existing.Length != size)
            data = new byte[size];

        fixed(byte* bytePtr = &data[0])
        {
            MemoryHelper.Write(new(bytePtr), stringSize);
            var utfBytes = Encoding.UTF8.GetBytes(value);
            MemoryHelper.Write<byte>(new(bytePtr + sizeof(int)), utfBytes, 0, utfBytes.Length);
            //Last byte should be zero
        }

        return data;
    }

    [Conditional("DEBUG")]
    private void AssertIsBaseName()
    {
        if (_name == null)
            return;

        Debug.Assert(!_name.Contains(","));
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<MaterialProperty, AiMaterialProperty>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    unsafe void IMarshalable<MaterialProperty, AiMaterialProperty>.ToNative(nint thisPtr, out AiMaterialProperty nativeValue)
    {
        nativeValue.MKey = new(_name);
        nativeValue.MType = PropertyType;
        nativeValue.MIndex = (uint) _texIndex;
        nativeValue.MSemantic = (uint)_texType;
        nativeValue.MData = null;
        nativeValue.MDataLength = 0;

        if(RawData != null)
        {
            nativeValue.MDataLength = (uint) RawData.Length;
            nativeValue.MData = MemoryHelper.ToNativeArray<byte>(RawData);
        }
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    unsafe void IMarshalable<MaterialProperty, AiMaterialProperty>.FromNative(in AiMaterialProperty nativeValue)
    {
        _name = nativeValue.MKey; //Avoid struct copy
        PropertyType = nativeValue.MType;
        _texIndex = (int) nativeValue.MIndex;
        _texType = (TextureType)nativeValue.MSemantic;
        RawData = null;

        if(nativeValue.MDataLength > 0 && nativeValue.MData != null)
            RawData = MemoryHelper.FromNativeArray(nativeValue.MData, (int) nativeValue.MDataLength);
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{MaterialProperty, AiMaterialProperty}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static unsafe void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMatProp = MemoryHelper.Read<AiMaterialProperty>(nativeValue);

        if(aiMatProp.MDataLength > 0 && aiMatProp.MData != null)
            MemoryHelper.FreeMemory(aiMatProp.MData);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}