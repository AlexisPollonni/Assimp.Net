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

using System.IO;
using System.Numerics;
using Silk.NET.Assimp;

namespace Assimp.Unmanaged;

/// <summary>
/// Singleton that governs access to the unmanaged Assimp library functions.
/// </summary>
public sealed class AssimpLibrary : IDisposable
{
    private static readonly object s_sync = new();

    /// <summary>
    /// Default name of the unmanaged library. Based on runtime implementation the prefix ("lib" on non-windows) and extension (.dll, .so, .dylib) will be appended automatically.
    /// </summary>
    private const string DefaultLibName = "assimp";

    private static AssimpLibrary s_instance;

    public event EventHandler LibraryFreed;
    
    private readonly Silk.NET.Assimp.Assimp _api = Silk.NET.Assimp.Assimp.GetApi();
    private bool m_enableVerboseLogging;

    /// <summary>
    /// Gets the AssimpLibrary instance.
    /// </summary>
    public static AssimpLibrary Instance
    {
        get
        {
            lock(s_sync)
            {
                return s_instance ??= CreateInstance();
            }
        }
    }

    /// <summary>
    /// Gets if the Assimp unmanaged library supports multithreading. If it was compiled for single threading only,
    /// then it will not utilize multiple threads during import.
    /// </summary>
    public bool IsMultithreadingSupported => !((GetCompileFlags() & CompileFlags.SingleThreaded) == CompileFlags.SingleThreaded);

    private static AssimpLibrary CreateInstance() => new();

    #region Import Methods

    /// <summary>
    /// Imports a file.
    /// </summary>
    /// <param name="file">Valid filename</param>
    /// <param name="flags">Post process flags specifying what steps are to be run after the import.</param>
    /// <param name="propStore">Property store containing config name-values, may be null.</param>
    /// <returns>Pointer to the unmanaged data structure.</returns>
    public unsafe AiScene* ImportFile(string file, PostProcessSteps flags, PropertyStore* propStore)
    {
        return ImportFile(file, flags, null, propStore);
    }

    /// <summary>
    /// Imports a file.
    /// </summary>
    /// <param name="file">Valid filename</param>
    /// <param name="flags">Post process flags specifying what steps are to be run after the import.</param>
    /// <param name="fileIO">Pointer to an instance of AiFileIO, a custom file IO system used to open the model and 
    /// any associated file the loader needs to open, passing NULL uses the default implementation.</param>
    /// <param name="propStore">Property store containing config name-values, may be null.</param>
    /// <returns>Pointer to the unmanaged data structure.</returns>
    public unsafe AiScene* ImportFile(string file, PostProcessSteps flags, AiFileIO* fileIO, PropertyStore* propStore)
    {
        return _api.ImportFileExWithProperties(file, (uint)flags, fileIO, propStore);
    }

    /// <summary>
    /// Imports a scene from a stream. This uses the "aiImportFileFromMemory" function. The stream can be from anyplace,
    /// not just a memory stream. It is up to the caller to dispose of the stream.
    /// </summary>
    /// <param name="stream">Stream containing the scene data</param>
    /// <param name="flags">Post processing flags</param>
    /// <param name="formatHint">A hint to Assimp to decide which importer to use to process the data</param>
    /// <param name="propStore">Property store containing the config name-values, may be null.</param>
    /// <returns>Pointer to the unmanaged data structure.</returns>
    public unsafe AiScene* ImportFileFromStream(Stream stream, PostProcessSteps flags, string formatHint, PropertyStore* propStore)
    {
        var buffer = MemoryHelper.ReadStreamFully(stream, 0);

        fixed (byte* ptr = buffer)
            return _api.ImportFileFromMemoryWithProperties(ptr, (uint)buffer.Length, (uint)flags, formatHint,
                propStore);
    }

    /// <summary>
    /// Releases the unmanaged scene data structure. This should NOT be used for unmanaged scenes that were marshaled
    /// from the managed scene structure - only for scenes whose memory was allocated by the native library!
    /// </summary>
    /// <param name="scene">Pointer to the unmanaged scene data structure.</param>
    public unsafe void ReleaseImport(AiScene* scene)
    {
        if(scene == null)
            return;

        _api.ReleaseImport(scene);
    }

    /// <summary>
    /// Applies a post-processing step on an already imported scene.
    /// </summary>
    /// <param name="scene">Pointer to the unmanaged scene data structure.</param>
    /// <param name="flags">Post processing steps to run.</param>
    /// <returns>Pointer to the unmanaged scene data structure.</returns>
    public unsafe AiScene* ApplyPostProcessing(AiScene* scene, PostProcessSteps flags)
    {
        if(scene == null)
            return null;

        return _api.ApplyPostProcessing(scene, (uint)flags);
    }

    #endregion

    #region Export Methods

    /// <summary>
    /// Gets all supported export formats.
    /// </summary>
    /// <returns>Array of supported export formats.</returns>
    public unsafe ExportFormatDescription[] GetExportFormatDescriptions()
    {
        var count = (int) _api.GetExportFormatCount();

        if(count == 0)
            return [];

        var descriptions = new ExportFormatDescription[count];
        
        for(var i = 0; i < count; i++)
        {
            var formatDescPtr = _api.GetExportFormatDescription((nuint)i);
            if(formatDescPtr != null)
            {
                var desc = MemoryHelper.Read<AiExportFormatDesc>((nint)formatDescPtr);
                descriptions[i] = new(desc);

                _api.ReleaseExportFormatDescription(formatDescPtr);
            }
        }

        return descriptions;
    }


    /// <summary>
    /// Exports the given scene to a chosen file format. Returns the exported data as a binary blob which you can embed into another data structure or file.
    /// </summary>
    /// <param name="scene">Scene to export, it is the responsibility of the caller to free this when finished.</param>
    /// <param name="formatId">Format id describing which format to export to.</param>
    /// <param name="preProcessing">Pre processing flags to operate on the scene during the export.</param>
    /// <returns>Exported binary blob, or null if there was an error.</returns>
    public unsafe ExportDataBlob ExportSceneToBlob(AiScene* scene, string formatId, PostProcessSteps preProcessing)
    {
        

        if(string.IsNullOrEmpty(formatId) || scene == null)
            return null;

        var blobPtr = _api.ExportSceneToBlob(scene, formatId, (uint)preProcessing);

        if(blobPtr == null)
            return null;

        var blob = MemoryHelper.Read<AiExportDataBlob>((nint)blobPtr);
        var dataBlob = new ExportDataBlob(ref blob);
        
        _api.ReleaseExportBlob(blobPtr);

        return dataBlob;
    }

    /// <summary>
    /// Exports the given scene to a chosen file format and writes the result file(s) to disk.
    /// </summary>
    /// <param name="scene">The scene to export, which needs to be freed by the caller. The scene is expected to conform to Assimp's Importer output format. In short,
    /// this means the model data should use a right handed coordinate system, face winding should be counter clockwise, and the UV coordinate origin assumed to be upper left. If the input is different, specify the pre processing flags appropiately.</param>
    /// <param name="formatId">Format id describing which format to export to.</param>
    /// <param name="fileName">Output filename to write to</param>
    /// <param name="preProcessing">Pre processing flags - accepts any post processing step flag. In reality only a small subset are actually supported, e.g. to ensure the input
    /// conforms to the standard Assimp output format. Some may be redundant, such as triangulation, which some exporters may have to enforce due to the export format.</param>
    /// <returns>Return code specifying if the operation was a success.</returns>
    public unsafe Return ExportScene(AiScene* scene, string formatId, string fileName, PostProcessSteps preProcessing)
    {
        return ExportScene(scene, formatId, fileName, null, preProcessing);
    }

    /// <summary>
    /// Exports the given scene to a chosen file format and writes the result file(s) to disk.
    /// </summary>
    /// <param name="scene">The scene to export, which needs to be freed by the caller. The scene is expected to conform to Assimp's Importer output format. In short,
    /// this means the model data should use a right handed coordinate system, face winding should be counter clockwise, and the UV coordinate origin assumed to be upper left. If the input is different, specify the pre processing flags appropiately.</param>
    /// <param name="formatId">Format id describing which format to export to.</param>
    /// <param name="fileName">Output filename to write to</param>
    /// <param name="fileIO">Pointer to an instance of AiFileIO, a custom file IO system used to open the model and 
    /// any associated file the loader needs to open, passing NULL uses the default implementation.</param>
    /// <param name="preProcessing">Pre processing flags - accepts any post processing step flag. In reality only a small subset are actually supported, e.g. to ensure the input
    /// conforms to the standard Assimp output format. Some may be redundant, such as triangulation, which some exporters may have to enforce due to the export format.</param>
    /// <returns>Return code specifying if the operation was a success.</returns>
    public unsafe Return ExportScene(AiScene* scene, string formatId, string fileName, AiFileIO* fileIO, PostProcessSteps preProcessing)
    {
        if(string.IsNullOrEmpty(formatId) || scene == null)
            return Return.Failure;

        return _api.ExportSceneEx(scene, formatId, fileName, fileIO, (uint)preProcessing);
    }

    /// <summary>
    /// Creates a modifyable copy of a scene, useful for copying the scene that was imported so its topology can be modified
    /// and the scene be exported.
    /// </summary>
    /// <param name="sceneToCopy">Valid scene to be copied</param>
    /// <returns>Modifyable copy of the scene</returns>
    public unsafe AiScene* CopyScene(AiScene* sceneToCopy)
    {
        if(sceneToCopy == null)
            return null;
        
        AiScene* copiedScene = null;
        _api.CopyScene(sceneToCopy, ref copiedScene);
        return copiedScene;
    }

    #endregion

    #region Logging Methods

    /// <summary>
    /// Attaches a log stream callback to catch Assimp messages.
    /// </summary>
    /// <param name="logStreamPtr">Pointer to an instance of AiLogStream.</param>
    public unsafe void AttachLogStream(AiLogStream* logStreamPtr)
    {
        _api.AttachLogStream(logStreamPtr);
    }

    /// <summary>
    /// Enables verbose logging.
    /// </summary>
    /// <param name="enable">True if verbose logging is to be enabled or not.</param>
    public void EnableVerboseLogging(bool enable)
    {
        _api.EnableVerboseLogging(enable ? 1 : 0);

        m_enableVerboseLogging = enable;
    }

    /// <summary>
    /// Gets if verbose logging is enabled.
    /// </summary>
    /// <returns>True if verbose logging is enabled, false otherwise.</returns>
    public bool GetVerboseLoggingEnabled()
    {
        return m_enableVerboseLogging;
    }

    /// <summary>
    /// Detaches a logstream callback.
    /// </summary>
    /// <param name="logStreamPtr">Pointer to an instance of AiLogStream.</param>
    /// <returns>A return code signifying if the function was successful or not.</returns>
    public unsafe Return DetachLogStream(AiLogStream* logStreamPtr)
    {
        return _api.DetachLogStream(logStreamPtr);
    }

    /// <summary>
    /// Detaches all logstream callbacks currently attached to Assimp.
    /// </summary>
    public void DetachAllLogStreams()
    {
        _api.DetachAllLogStreams();
    }

    #endregion

    #region Import Properties Setters

    /// <summary>
    /// Create an empty property store. Property stores are used to collect import settings.
    /// </summary>
    /// <returns>Pointer to property store</returns>
    public unsafe PropertyStore* CreatePropertyStore()
    {
        return _api.CreatePropertyStore();
    }

    /// <summary>
    /// Deletes a property store.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    public unsafe void ReleasePropertyStore(PropertyStore* propertyStore)
    {
        if(propertyStore == null)
            return;

        _api.ReleasePropertyStore(propertyStore);
    }

    /// <summary>
    /// Sets an integer property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public unsafe void SetImportPropertyInteger(PropertyStore* propertyStore, string name, int value)
    {
        if(propertyStore == null || string.IsNullOrEmpty(name))
            return;

        _api.SetImportPropertyInteger(propertyStore, name, value);
    }

    /// <summary>
    /// Sets a float property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public unsafe void SetImportPropertyFloat(PropertyStore* propertyStore, string name, float value)
    {
        if(propertyStore == null || string.IsNullOrEmpty(name))
            return;

        _api.SetImportPropertyFloat(propertyStore, name, value);
    }

    /// <summary>
    /// Sets a string property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public unsafe void SetImportPropertyString(PropertyStore* propertyStore, string name, string value)
    {
        if(propertyStore == null|| string.IsNullOrEmpty(name))
            return;

        var str = new AssimpString(value);
        _api.SetImportPropertyString(propertyStore, name, str); 
    }

    /// <summary>
    /// Sets a matrix property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public unsafe void SetImportPropertyMatrix(PropertyStore* propertyStore, string name, in Matrix4x4 value)
    {
        if(propertyStore == null|| string.IsNullOrEmpty(name))
            return;

        _api.SetImportPropertyMatrix(propertyStore, name, value);
    }

    #endregion

    #region Material Getters

    /// <summary>
    /// Retrieves a color value from the material property table.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="key">Ai mat key (base) name to search for</param>
    /// <param name="texType">Texture Type semantic, always zero for non-texture properties</param>
    /// <param name="texIndex">Texture index, always zero for non-texture properties</param>
    /// <returns>The color if it exists. If not, the default Color4D value is returned.</returns>
    public unsafe Color4D GetMaterialColor(ref AiMaterial mat, string key, TextureType texType, uint texIndex)
    {
        var ptr = nint.Zero;
        try
        {
            ptr = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<Color4D>());
            var code = _api.GetMaterialColor(mat, key, (uint) texType, texIndex, (Vector4*)ptr);
            var color = new Color4D();
            if(code == Return.Success && ptr != nint.Zero)
                color = MemoryHelper.Read<Color4D>(ptr);

            return color;
        }
        finally
        {
            if(ptr != nint.Zero)
                MemoryHelper.FreeMemory(ptr);
        }
    }

    /// <summary>
    /// Retrieves an array of float values with the specific key from the material.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="key">Ai mat key (base) name to search for</param>
    /// <param name="texType">Texture Type semantic, always zero for non-texture properties</param>
    /// <param name="texIndex">Texture index, always zero for non-texture properties</param>
    /// <param name="floatCount">The maximum number of floats to read. This may not accurately describe the data returned, as it may not exist or be smaller. If this value is less than
    /// the available floats, then only the requested number is returned (e.g. 1 or 2 out of a 4 float array).</param>
    /// <returns>The float array, if it exists</returns>
    public unsafe float[] GetMaterialFloatArray(ref AiMaterial mat, string key, TextureType texType, uint texIndex, uint floatCount)
    {
        var ptr = nint.Zero;
        try
        {
            ptr = MemoryHelper.AllocateMemory(nint.Size);
            var code = _api.GetMaterialFloatArray(mat, key, (uint) texType, texIndex, (float*)ptr, ref floatCount);
            float[] array = null;
            if(code == Return.Success && floatCount > 0)
            {
                array = new float[floatCount];
                MemoryHelper.Read((float*)ptr, array, 0, (int) floatCount);
            }
            return array;
        }
        finally
        {
            if(ptr != nint.Zero)
            {
                MemoryHelper.FreeMemory(ptr);
            }
        }
    }

    /// <summary>
    /// Retrieves an array of integer values with the specific key from the material.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="key">Ai mat key (base) name to search for</param>
    /// <param name="texType">Texture Type semantic, always zero for non-texture properties</param>
    /// <param name="texIndex">Texture index, always zero for non-texture properties</param>
    /// <param name="intCount">The maximum number of integers to read. This may not accurately describe the data returned, as it may not exist or be smaller. If this value is less than
    /// the available integers, then only the requested number is returned (e.g. 1 or 2 out of a 4 float array).</param>
    /// <returns>The integer array, if it exists</returns>
    public unsafe int[] GetMaterialIntegerArray(ref AiMaterial mat, string key, TextureType texType, uint texIndex, uint intCount)
    {
        var ptr = nint.Zero;
        try
        {
            ptr = MemoryHelper.AllocateMemory(nint.Size); //TODO: This dont look right, to test
            var code = _api.GetMaterialIntegerArray(mat, key, (uint) texType, texIndex, (int*)ptr, ref intCount);
            int[] array = null;
            if(code == Return.Success && intCount > 0)
            {
                array = new int[intCount];
                MemoryHelper.Read((int*)ptr, array, 0, (int) intCount);
            }
            return array;
        }
        finally
        {
            if(ptr != nint.Zero)
            {
                MemoryHelper.FreeMemory(ptr);
            }
        }
    }

    /// <summary>
    /// Retrieves a material property with the specific key from the material.
    /// </summary>
    /// <param name="mat">Material to retrieve the property from</param>
    /// <param name="key">Ai mat key (base) name to search for</param>
    /// <param name="texType">Texture Type semantic, always zero for non-texture properties</param>
    /// <param name="texIndex">Texture index, always zero for non-texture properties</param>
    /// <returns>The material property, if found.</returns>
    public unsafe AiMaterialProperty GetMaterialProperty(ref AiMaterial mat, string key, TextureType texType, uint texIndex)
    {
        AiMaterialProperty* ptr = null;
        var code = _api.GetMaterialProperty(mat, key, (uint) texType, texIndex, &ptr);
        
        var prop = new AiMaterialProperty();
        if(code == Return.Success && ptr != null)
            prop = MemoryHelper.Read<AiMaterialProperty>((nint)ptr);

        return prop;
    }

    /// <summary>
    /// Retrieves a string from the material property table.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="key">Ai mat key (base) name to search for</param>
    /// <param name="texType">Texture Type semantic, always zero for non-texture properties</param>
    /// <param name="texIndex">Texture index, always zero for non-texture properties</param>
    /// <returns>The string, if it exists. If not, an empty string is returned.</returns>
    public string GetMaterialString(ref AiMaterial mat, string key, TextureType texType, uint texIndex)
    {
        var str = new AssimpString();
        var code = _api.GetMaterialString(mat, key, (uint)texType, texIndex, ref str);        

        if(code == Return.Success)
            return str.AsString;

        return string.Empty;
    }

    /// <summary>
    /// Gets the number of textures contained in the material for a particular texture type.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="type">Texture Type semantic</param>
    /// <returns>The number of textures for the type.</returns>
    public uint GetMaterialTextureCount(ref AiMaterial mat, TextureType type)
    {
        return _api.GetMaterialTextureCount(mat, type);
    }

    /// <summary>
    /// Gets the texture filepath contained in the material.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="type">Texture type semantic</param>
    /// <param name="index">Texture index</param>
    /// <returns>The texture filepath, if it exists. If not an empty string is returned.</returns>
    public unsafe string GetMaterialTextureFilePath(ref AiMaterial mat, TextureType type, uint index)
    {
        TextureMapping mapping;
        uint uvIndex = 0;
        float blendFactor;
        TextureOp texOp;
        var mapModes = stackalloc TextureMapMode[2];
        uint flags = 0;

        var str = new AssimpString();

        var code = _api.GetMaterialTexture(mat, type, index, ref str, &mapping, ref uvIndex, &blendFactor, &texOp, mapModes, &flags);

        if(code == Return.Success)
            return str;

        return string.Empty;
    }

    /// <summary>
    /// Gets all values pertaining to a particular texture from a material.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="type">Texture type semantic</param>
    /// <param name="index">Texture index</param>
    /// <returns>Returns the texture slot struct containing all the information.</returns>
    public unsafe TextureSlot GetMaterialTexture(ref AiMaterial mat, TextureType type, uint index)
    {
        TextureMapping mapping;
        uint uvIndex = 0;
        float blendFactor;
        TextureOp texOp;
        var mapModes = stackalloc TextureMapMode[2];
        uint flags = 0;

        var str = new AssimpString();

        var code = _api.GetMaterialTexture(mat, type, index, ref str, &mapping, ref uvIndex, &blendFactor, &texOp, mapModes, &flags);

        return new(str.AsString, type, (int) index, mapping, (int) uvIndex, blendFactor, texOp, (TextureWrapMode)mapModes[0], (TextureWrapMode)mapModes[1], (int) flags);
    }

    #endregion

    #region Error and Info Methods

    /// <summary>
    /// Gets the last error logged in Assimp.
    /// </summary>
    /// <returns>The last error message logged.</returns>
    public string GetErrorString()
    {
        return _api.GetErrorStringS();
    }

    /// <summary>
    /// Checks whether the model format extension is supported by Assimp.
    /// </summary>
    /// <param name="extension">Model format extension, e.g. ".3ds"</param>
    /// <returns>True if the format is supported, false otherwise.</returns>
    public bool IsExtensionSupported(string extension)
    {
        return _api.IsExtensionSupported(extension) > 0;
    }

    /// <summary>
    /// Gets all the model format extensions that are currently supported by Assimp.
    /// </summary>
    /// <returns>Array of supported format extensions</returns>
    public string[] GetExtensionList()
    {
        var aiString = new AssimpString();
        _api.GetExtensionList(ref aiString);
        return aiString.AsString.Split(["*", ";*"], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Gets a collection of importer descriptions that detail metadata and feature support for each importer.
    /// </summary>
    /// <returns>Collection of importer descriptions</returns>
    public unsafe ImporterDescription[] GetImporterDescriptions()
    {
        var count = (int)_api.GetImportFormatCount();
        var descrs = new ImporterDescription[count];

        for(var i = 0; i < count; i++)
        {
            var descrPtr = _api.GetImportFormatDescription((nuint)i);
            if(descrPtr != null)
            {
                ref var descr = ref MemoryHelper.AsRef<AiImporterDesc>((nint)descrPtr);
                descrs[i] = new(descr);
            }
        }

        return descrs;
    }

    /// <summary>
    /// Gets the memory requirements of the scene.
    /// </summary>
    /// <param name="scene">Pointer to the unmanaged scene data structure.</param>
    /// <returns>The memory information about the scene.</returns>
    public unsafe AiMemoryInfo GetMemoryRequirements(AiScene* scene)
    {
        var info = new AiMemoryInfo();
        _api.GetMemoryRequirements(scene, ref info);
        return info;
    }

    #endregion

    #region Math Methods

    /// <summary>
    /// Creates a quaternion from the 3x3 rotation matrix.
    /// </summary>
    /// <param name="quat">Quaternion struct to fill</param>
    /// <param name="mat">Rotation matrix</param>
    public void CreateQuaternionFromMatrix(out Quaternion quat, in Matrix3x3 mat)
    {
        var q = new AssimpQuaternion();
        _api.CreateQuaternionFromMatrix(ref q, mat);

        quat = q;
    }

    /// <summary>
    /// Decomposes a 4x4 matrix into its scaling, rotation, and translation parts.
    /// </summary>
    /// <param name="mat">4x4 Matrix to decompose</param>
    /// <param name="scaling">Scaling vector</param>
    /// <param name="rotation">Quaternion containing the rotation</param>
    /// <param name="position">Translation vector</param>
    public void DecomposeMatrix(in Matrix4x4 mat, out Vector3 scaling, out Quaternion rotation, out Vector3 position)
    {
        scaling = new();
        position = new();

        var r = new AssimpQuaternion();
        _api.DecomposeMatrix(mat, ref scaling, ref r, ref position);
        rotation = r;
    }

    /// <summary>
    /// Transposes the 4x4 matrix.
    /// </summary>
    /// <param name="mat">Matrix to transpose</param>
    public void TransposeMatrix4(ref Matrix4x4 mat)
    {
        _api.TransposeMatrix4(ref mat);
    }

    /// <summary>
    /// Transposes the 3x3 matrix.
    /// </summary>
    /// <param name="mat">Matrix to transpose</param>
    public void TransposeMatrix3(ref Matrix3x3 mat)
    {
        _api.TransposeMatrix3(ref mat);
    }

    /// <summary>
    /// Transforms the vector by the 3x3 rotation matrix.
    /// </summary>
    /// <param name="vec">Vector to transform</param>
    /// <param name="mat">Rotation matrix</param>
    public void TransformVecByMatrix3(ref Vector3 vec, in Matrix3x3 mat)
    {
        _api.TransformVecByMatrix3(ref vec, mat);
    }

    /// <summary>
    /// Transforms the vector by the 4x4 matrix.
    /// </summary>
    /// <param name="vec">Vector to transform</param>
    /// <param name="mat">Matrix transformation</param>
    public void TransformVecByMatrix4(ref Vector3 vec, in Matrix4x4 mat)
    {
        _api.TransformVecByMatrix4(ref vec, mat);
    }

    /// <summary>
    /// Multiplies two 4x4 matrices. The destination matrix receives the result.
    /// </summary>
    /// <param name="dst">First input matrix and is also the Matrix to receive the result</param>
    /// <param name="src">Second input matrix, to be multiplied with "dst".</param>
    public void MultiplyMatrix4(ref Matrix4x4 dst, in Matrix4x4 src)
    {
        _api.MultiplyMatrix4(ref dst, in src);
    }

    /// <summary>
    /// Multiplies two 3x3 matrices. The destination matrix receives the result.
    /// </summary>
    /// <param name="dst">First input matrix and is also the Matrix to receive the result</param>
    /// <param name="src">Second input matrix, to be multiplied with "dst".</param>
    public void MultiplyMatrix3(ref Matrix3x3 dst, in Matrix3x3 src)
    {
        _api.MultiplyMatrix3(ref dst, src);
    }

    /// <summary>
    /// Creates a 3x3 identity matrix.
    /// </summary>
    /// <param name="mat">Matrix to hold the identity</param>
    public void IdentityMatrix3(out Matrix3x3 mat)
    {
        mat = new();
        _api.IdentityMatrix3(ref mat);
    }

    /// <summary>
    /// Creates a 4x4 identity matrix.
    /// </summary>
    /// <param name="mat">Matrix to hold the identity</param>
    public void IdentityMatrix4(out Matrix4x4 mat)
    {
        mat = new();
        _api.IdentityMatrix4(ref mat);
    }

    #endregion

    #region Version Info

    /// <summary>
    /// Gets the Assimp legal info.
    /// </summary>
    /// <returns>String containing Assimp legal info.</returns>
    public string GetLegalString()
    {
        return _api.GetLegalStringS();
    }

    /// <summary>
    /// Gets the native Assimp DLL's minor version number.
    /// </summary>
    /// <returns>Assimp minor version number</returns>
    public uint GetVersionMinor()
    {
        return _api.GetVersionMinor();
    }

    /// <summary>
    /// Gets the native Assimp DLL's major version number.
    /// </summary>
    /// <returns>Assimp major version number</returns>
    public uint GetVersionMajor()
    {
        return _api.GetVersionMajor();
    }

    /// <summary>
    /// Gets the native Assimp DLL's revision version number.
    /// </summary>
    /// <returns>Assimp revision version number</returns>
    public uint GetVersionRevision()
    {
        return _api.GetVersionRevision();
    }

    /// <summary>
    /// Returns the branchname of the Assimp runtime.
    /// </summary>
    /// <returns>The current branch name.</returns>
    public string GetBranchName()
    {
        return _api.GetBranchNameS();
    }

    /// <summary>
    /// Gets the native Assimp DLL's current version number as "major.minor.revision" string. This is the
    /// version of Assimp that this wrapper is currently using.
    /// </summary>
    /// <returns>Unmanaged DLL version</returns>
    public string GetVersion()
    {
        var major = GetVersionMajor();
        var minor = GetVersionMinor();
        var rev = GetVersionRevision();

        return $"{major.ToString()}.{minor.ToString()}.{rev.ToString()}";
    }

    /// <summary>
    /// Gets the native Assimp DLL's current version number as a .NET version object.
    /// </summary>
    /// <returns>Unmanaged DLL version</returns>
    public Version GetVersionAsVersion()
    {
        return new((int) GetVersionMajor(), (int) GetVersionMinor(), 0, (int) GetVersionRevision());
    }

    /// <summary>
    /// Get the compilation flags that describe how the native Assimp DLL was compiled.
    /// </summary>
    /// <returns>Compilation flags</returns>
    public CompileFlags GetCompileFlags()
    {
        return (CompileFlags) _api.GetCompileFlags();
    }

    #endregion

    public void Dispose()
    {
        LibraryFreed?.Invoke(this, EventArgs.Empty);
        _api?.Dispose();
    }

    ~AssimpLibrary() => Dispose();
}

/// <summary>
/// Enumerates how the native Assimp DLL was compiled
/// </summary>
public enum CompileFlags
{
    /// <summary>
    /// Assimp compiled as a shared object (Windows: DLL);
    /// </summary>
    Shared = 0x1,

    /// <summary>
    /// Assimp was compiled against STLport
    /// </summary>
    STLport = 0x2,

    /// <summary>
    /// Assimp was compiled as a debug build
    /// </summary>
    Debug = 0x4,

    /// <summary>
    /// Assimp was compiled with the boost work around.
    /// </summary>
    NoBoost = 0x8,

    /// <summary>
    /// Assimp was compiled built to run single threaded.
    /// </summary>
    SingleThreaded = 0x10
}