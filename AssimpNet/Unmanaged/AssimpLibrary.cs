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

namespace Assimp.Unmanaged;

/// <summary>
/// Singleton that governs access to the unmanaged Assimp library functions.
/// </summary>
public sealed class AssimpLibrary : UnmanagedLibrary
{
    private static readonly object s_sync = new();

    /// <summary>
    /// Default name of the unmanaged library. Based on runtime implementation the prefix ("lib" on non-windows) and extension (.dll, .so, .dylib) will be appended automatically.
    /// </summary>
    private const string DefaultLibName = "assimp";

    private static AssimpLibrary s_instance;

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

    private AssimpLibrary(string defaultLibName, Type[] unmanagedFunctionDelegateTypes)
        : base(defaultLibName, unmanagedFunctionDelegateTypes) { }

    private static AssimpLibrary CreateInstance()
    {
        return new(DefaultLibName, PlatformHelper.GetNestedTypes(typeof(Functions)));
    }

    #region Import Methods

    /// <summary>
    /// Imports a file.
    /// </summary>
    /// <param name="file">Valid filename</param>
    /// <param name="flags">Post process flags specifying what steps are to be run after the import.</param>
    /// <param name="propStore">Property store containing config name-values, may be null.</param>
    /// <returns>Pointer to the unmanaged data structure.</returns>
    public nint ImportFile(string file, PostProcessSteps flags, nint propStore)
    {
        return ImportFile(file, flags, nint.Zero, propStore);
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
    public nint ImportFile(string file, PostProcessSteps flags, nint fileIO, nint propStore)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiImportFileExWithProperties>(FunctionNames.aiImportFileExWithProperties);

        return func(file, (uint) flags, fileIO, propStore);
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
    public nint ImportFileFromStream(Stream stream, PostProcessSteps flags, string formatHint, nint propStore)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiImportFileFromMemoryWithProperties>(FunctionNames.aiImportFileFromMemoryWithProperties);

        var buffer = MemoryHelper.ReadStreamFully(stream, 0);

        return func(buffer, (uint) buffer.Length, (uint) flags, formatHint, propStore);
    }

    /// <summary>
    /// Releases the unmanaged scene data structure. This should NOT be used for unmanaged scenes that were marshaled
    /// from the managed scene structure - only for scenes whose memory was allocated by the native library!
    /// </summary>
    /// <param name="scene">Pointer to the unmanaged scene data structure.</param>
    public void ReleaseImport(nint scene)
    {
        LoadIfNotLoaded();

        if(scene == nint.Zero)
            return;

        var func = GetFunction<Functions.aiReleaseImport>(FunctionNames.aiReleaseImport);

        func(scene);
    }

    /// <summary>
    /// Applies a post-processing step on an already imported scene.
    /// </summary>
    /// <param name="scene">Pointer to the unmanaged scene data structure.</param>
    /// <param name="flags">Post processing steps to run.</param>
    /// <returns>Pointer to the unmanaged scene data structure.</returns>
    public nint ApplyPostProcessing(nint scene, PostProcessSteps flags)
    {
        LoadIfNotLoaded();

        if(scene == nint.Zero)
            return nint.Zero;

        var func = GetFunction<Functions.aiApplyPostProcessing>(FunctionNames.aiApplyPostProcessing);

        return func(scene, (uint) flags);
    }

    #endregion

    #region Export Methods

    /// <summary>
    /// Gets all supported export formats.
    /// </summary>
    /// <returns>Array of supported export formats.</returns>
    public ExportFormatDescription[] GetExportFormatDescriptions()
    {
        LoadIfNotLoaded();

        var count = (int) GetFunction<Functions.aiGetExportFormatCount>(FunctionNames.aiGetExportFormatCount)().ToUInt32();

        if(count == 0)
            return [];

        var descriptions = new ExportFormatDescription[count];

        var func = GetFunction<Functions.aiGetExportFormatDescription>(FunctionNames.aiGetExportFormatDescription);
        var releaseFunc = GetFunction<Functions.aiReleaseExportFormatDescription>(FunctionNames.aiReleaseExportFormatDescription);

        for(var i = 0; i < count; i++)
        {
            var formatDescPtr = func(new((uint) i));
            if(formatDescPtr != nint.Zero)
            {
                var desc = MemoryHelper.Read<AiExportFormatDesc>(formatDescPtr);
                descriptions[i] = new(desc);

                releaseFunc(formatDescPtr);
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
    public ExportDataBlob ExportSceneToBlob(nint scene, string formatId, PostProcessSteps preProcessing)
    {
        LoadIfNotLoaded();

        if(string.IsNullOrEmpty(formatId) || scene == nint.Zero)
            return null;

        var exportBlobFunc = GetFunction<Functions.aiExportSceneToBlob>(FunctionNames.aiExportSceneToBlob);
        var releaseExportBlobFunc = GetFunction<Functions.aiReleaseExportBlob>(FunctionNames.aiReleaseExportBlob);

        var blobPtr = exportBlobFunc(scene, formatId, (uint) preProcessing);

        if(blobPtr == nint.Zero)
            return null;

        var blob = MemoryHelper.Read<AiExportDataBlob>(blobPtr);
        var dataBlob = new ExportDataBlob(ref blob);
        releaseExportBlobFunc(blobPtr);

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
    public ReturnCode ExportScene(nint scene, string formatId, string fileName, PostProcessSteps preProcessing)
    {
        return ExportScene(scene, formatId, fileName, nint.Zero, preProcessing);
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
    public ReturnCode ExportScene(nint scene, string formatId, string fileName, nint fileIO, PostProcessSteps preProcessing)
    {
        LoadIfNotLoaded();

        if(string.IsNullOrEmpty(formatId) || scene == nint.Zero)
            return ReturnCode.Failure;

        var exportFunc = GetFunction<Functions.aiExportSceneEx>(FunctionNames.aiExportSceneEx);

        return exportFunc(scene, formatId, fileName, fileIO, (uint) preProcessing);
    }

    /// <summary>
    /// Creates a modifyable copy of a scene, useful for copying the scene that was imported so its topology can be modified
    /// and the scene be exported.
    /// </summary>
    /// <param name="sceneToCopy">Valid scene to be copied</param>
    /// <returns>Modifyable copy of the scene</returns>
    public nint CopyScene(nint sceneToCopy)
    {
        if(sceneToCopy == nint.Zero)
            return nint.Zero;

        var func = GetFunction<Functions.aiCopyScene>(FunctionNames.aiCopyScene);

        func(sceneToCopy, out var copiedScene);

        return copiedScene;
    }

    #endregion

    #region Logging Methods

    /// <summary>
    /// Attaches a log stream callback to catch Assimp messages.
    /// </summary>
    /// <param name="logStreamPtr">Pointer to an instance of AiLogStream.</param>
    public void AttachLogStream(nint logStreamPtr)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiAttachLogStream>(FunctionNames.aiAttachLogStream);

        func(logStreamPtr);
    }

    /// <summary>
    /// Enables verbose logging.
    /// </summary>
    /// <param name="enable">True if verbose logging is to be enabled or not.</param>
    public void EnableVerboseLogging(bool enable)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiEnableVerboseLogging>(FunctionNames.aiEnableVerboseLogging);

        func(enable);

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
    public ReturnCode DetachLogStream(nint logStreamPtr)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiDetachLogStream>(FunctionNames.aiDetachLogStream);

        return func(logStreamPtr);
    }

    /// <summary>
    /// Detaches all logstream callbacks currently attached to Assimp.
    /// </summary>
    public void DetachAllLogStreams()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiDetachAllLogStreams>(FunctionNames.aiDetachAllLogStreams);

        func();
    }

    #endregion

    #region Import Properties Setters

    /// <summary>
    /// Create an empty property store. Property stores are used to collect import settings.
    /// </summary>
    /// <returns>Pointer to property store</returns>
    public nint CreatePropertyStore()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiCreatePropertyStore>(FunctionNames.aiCreatePropertyStore);

        return func();
    }

    /// <summary>
    /// Deletes a property store.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    public void ReleasePropertyStore(nint propertyStore)
    {
        LoadIfNotLoaded();

        if(propertyStore == nint.Zero)
            return;

        var func = GetFunction<Functions.aiReleasePropertyStore>(FunctionNames.aiReleasePropertyStore);

        func(propertyStore);
    }

    /// <summary>
    /// Sets an integer property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public void SetImportPropertyInteger(nint propertyStore, string name, int value)
    {
        LoadIfNotLoaded();

        if(propertyStore == nint.Zero || string.IsNullOrEmpty(name))
            return;

        var func = GetFunction<Functions.aiSetImportPropertyInteger>(FunctionNames.aiSetImportPropertyInteger);

        func(propertyStore, name, value);
    }

    /// <summary>
    /// Sets a float property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public void SetImportPropertyFloat(nint propertyStore, string name, float value)
    {
        LoadIfNotLoaded();

        if(propertyStore == nint.Zero || string.IsNullOrEmpty(name))
            return;

        var func = GetFunction<Functions.aiSetImportPropertyFloat>(FunctionNames.aiSetImportPropertyFloat);

        func(propertyStore, name, value);
    }

    /// <summary>
    /// Sets a string property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public void SetImportPropertyString(nint propertyStore, string name, string value)
    {
        LoadIfNotLoaded();

        if(propertyStore == nint.Zero || string.IsNullOrEmpty(name))
            return;

        var func = GetFunction<Functions.aiSetImportPropertyString>(FunctionNames.aiSetImportPropertyString);

        var str = new AiString();
        if(str.SetString(value))
            func(propertyStore, name, ref str);
    }

    /// <summary>
    /// Sets a matrix property value.
    /// </summary>
    /// <param name="propertyStore">Pointer to property store</param>
    /// <param name="name">Property name</param>
    /// <param name="value">Property value</param>
    public void SetImportPropertyMatrix(nint propertyStore, string name, Matrix4x4 value)
    {
        LoadIfNotLoaded();

        if(propertyStore == nint.Zero || string.IsNullOrEmpty(name))
            return;

        var func = GetFunction<Functions.aiSetImportPropertyMatrix>(FunctionNames.aiSetImportPropertyMatrix);
        func(propertyStore, name, ref value);
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
    public Color4D GetMaterialColor(ref AiMaterial mat, string key, TextureType texType, uint texIndex)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialColor>(FunctionNames.aiGetMaterialColor);

        var ptr = nint.Zero;
        try
        {
            ptr = MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<Color4D>());
            var code = func(ref mat, key, (uint) texType, texIndex, ptr);
            var color = new Color4D();
            if(code == ReturnCode.Success && ptr != nint.Zero)
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
    public float[] GetMaterialFloatArray(ref AiMaterial mat, string key, TextureType texType, uint texIndex, uint floatCount)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialFloatArray>(FunctionNames.aiGetMaterialFloatArray);

        var ptr = nint.Zero;
        try
        {
            ptr = MemoryHelper.AllocateMemory(nint.Size);
            var code = func(ref mat, key, (uint) texType, texIndex, ptr, ref floatCount);
            float[] array = null;
            if(code == ReturnCode.Success && floatCount > 0)
            {
                array = new float[floatCount];
                MemoryHelper.Read(ptr, array, 0, (int) floatCount);
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
    public int[] GetMaterialIntegerArray(ref AiMaterial mat, string key, TextureType texType, uint texIndex, uint intCount)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialIntegerArray>(FunctionNames.aiGetMaterialIntegerArray);

        var ptr = nint.Zero;
        try
        {
            ptr = MemoryHelper.AllocateMemory(nint.Size);
            var code = func(ref mat, key, (uint) texType, texIndex, ptr, ref intCount);
            int[] array = null;
            if(code == ReturnCode.Success && intCount > 0)
            {
                array = new int[intCount];
                MemoryHelper.Read(ptr, array, 0, (int) intCount);
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
    public AiMaterialProperty GetMaterialProperty(ref AiMaterial mat, string key, TextureType texType, uint texIndex)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialProperty>(FunctionNames.aiGetMaterialProperty);

        var code = func(ref mat, key, (uint) texType, texIndex, out var ptr);
        var prop = new AiMaterialProperty();
        if(code == ReturnCode.Success && ptr != nint.Zero)
            prop = MemoryHelper.Read<AiMaterialProperty>(ptr);

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
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialString>(FunctionNames.aiGetMaterialString);

        var code = func(ref mat, key, (uint) texType, texIndex, out var str);
        if(code == ReturnCode.Success)
            return str.GetString();

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
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialTextureCount>(FunctionNames.aiGetMaterialTextureCount);

        return func(ref mat, type);
    }

    /// <summary>
    /// Gets the texture filepath contained in the material.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="type">Texture type semantic</param>
    /// <param name="index">Texture index</param>
    /// <returns>The texture filepath, if it exists. If not an empty string is returned.</returns>
    public string GetMaterialTextureFilePath(ref AiMaterial mat, TextureType type, uint index)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialTexture>(FunctionNames.aiGetMaterialTexture);

        TextureMapping mapping;
        uint uvIndex;
        float blendFactor;
        TextureOperation texOp;
        var wrapModes = new TextureWrapMode[2];
        uint flags;

        var code = func(ref mat, type, index, out var str, out mapping, out uvIndex, out blendFactor, out texOp, wrapModes, out flags);

        if(code == ReturnCode.Success)
        {
            return str.GetString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets all values pertaining to a particular texture from a material.
    /// </summary>
    /// <param name="mat">Material to retrieve the data from</param>
    /// <param name="type">Texture type semantic</param>
    /// <param name="index">Texture index</param>
    /// <returns>Returns the texture slot struct containing all the information.</returns>
    public TextureSlot GetMaterialTexture(ref AiMaterial mat, TextureType type, uint index)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMaterialTexture>(FunctionNames.aiGetMaterialTexture);

        var wrapModes = new TextureWrapMode[2];

        var code = func(ref mat, type, index, out var str, out var mapping, out var uvIndex, out var blendFactor, out var texOp, wrapModes, out var flags);

        return new(str.GetString(), type, (int) index, mapping, (int) uvIndex, blendFactor, texOp, wrapModes[0], wrapModes[1], (int) flags);
    }

    #endregion

    #region Error and Info Methods

    /// <summary>
    /// Gets the last error logged in Assimp.
    /// </summary>
    /// <returns>The last error message logged.</returns>
    public string GetErrorString()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetErrorString>(FunctionNames.aiGetErrorString);

        var ptr = func();

        if(ptr == nint.Zero)
            return string.Empty;

        return Marshal.PtrToStringAnsi(ptr);
    }

    /// <summary>
    /// Checks whether the model format extension is supported by Assimp.
    /// </summary>
    /// <param name="extension">Model format extension, e.g. ".3ds"</param>
    /// <returns>True if the format is supported, false otherwise.</returns>
    public bool IsExtensionSupported(string extension)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiIsExtensionSupported>(FunctionNames.aiIsExtensionSupported);

        return func(extension);
    }

    /// <summary>
    /// Gets all the model format extensions that are currently supported by Assimp.
    /// </summary>
    /// <returns>Array of supported format extensions</returns>
    public string[] GetExtensionList()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetExtensionList>(FunctionNames.aiGetExtensionList);

        var aiString = new AiString();
        func(ref aiString);
        return aiString.GetString().Split(new[] { "*", ";*" }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Gets a collection of importer descriptions that detail metadata and feature support for each importer.
    /// </summary>
    /// <returns>Collection of importer descriptions</returns>
    public ImporterDescription[] GetImporterDescriptions()
    {
        LoadIfNotLoaded();

        var funcGetCount = GetFunction<Functions.aiGetImportFormatCount>(FunctionNames.aiGetImportFormatCount);
        var funcGetDescr = GetFunction<Functions.aiGetImportFormatDescription>(FunctionNames.aiGetImportFormatDescription);

        var count = (int) funcGetCount().ToUInt32();
        var descrs = new ImporterDescription[count];

        for(var i = 0; i < count; i++)
        {
            var descrPtr = funcGetDescr(new((uint) i));
            if(descrPtr != nint.Zero)
            {
                ref var descr = ref MemoryHelper.AsRef<AiImporterDesc>(descrPtr);
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
    public AiMemoryInfo GetMemoryRequirements(nint scene)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetMemoryRequirements>(FunctionNames.aiGetMemoryRequirements);

        var info = new AiMemoryInfo();
        if(scene != nint.Zero)
        {
            func(scene, ref info);
        }

        return info;
    }

    #endregion

    #region Math Methods

    /// <summary>
    /// Creates a quaternion from the 3x3 rotation matrix.
    /// </summary>
    /// <param name="quat">Quaternion struct to fill</param>
    /// <param name="mat">Rotation matrix</param>
    public void CreateQuaternionFromMatrix(out Quaternion quat, ref Matrix3x3 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiCreateQuaternionFromMatrix>(FunctionNames.aiCreateQuaternionFromMatrix);

        func(out quat, ref mat);
    }

    /// <summary>
    /// Decomposes a 4x4 matrix into its scaling, rotation, and translation parts.
    /// </summary>
    /// <param name="mat">4x4 Matrix to decompose</param>
    /// <param name="scaling">Scaling vector</param>
    /// <param name="rotation">Quaternion containing the rotation</param>
    /// <param name="position">Translation vector</param>
    public void DecomposeMatrix(ref Matrix4x4 mat, out Vector3D scaling, out Quaternion rotation, out Vector3D position)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiDecomposeMatrix>(FunctionNames.aiDecomposeMatrix);

        func(ref mat, out scaling, out rotation, out position);
    }

    /// <summary>
    /// Transposes the 4x4 matrix.
    /// </summary>
    /// <param name="mat">Matrix to transpose</param>
    public void TransposeMatrix4(ref Matrix4x4 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiTransposeMatrix4>(FunctionNames.aiTransposeMatrix4);

        func(ref mat);
    }

    /// <summary>
    /// Transposes the 3x3 matrix.
    /// </summary>
    /// <param name="mat">Matrix to transpose</param>
    public void TransposeMatrix3(ref Matrix3x3 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiTransposeMatrix3>(FunctionNames.aiTransposeMatrix3);

        func(ref mat);
    }

    /// <summary>
    /// Transforms the vector by the 3x3 rotation matrix.
    /// </summary>
    /// <param name="vec">Vector to transform</param>
    /// <param name="mat">Rotation matrix</param>
    public void TransformVecByMatrix3(ref Vector3D vec, ref Matrix3x3 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiTransformVecByMatrix3>(FunctionNames.aiTransformVecByMatrix3);

        func(ref vec, ref mat);
    }

    /// <summary>
    /// Transforms the vector by the 4x4 matrix.
    /// </summary>
    /// <param name="vec">Vector to transform</param>
    /// <param name="mat">Matrix transformation</param>
    public void TransformVecByMatrix4(ref Vector3D vec, ref Matrix4x4 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiTransformVecByMatrix4>(FunctionNames.aiTransformVecByMatrix4);

        func(ref vec, ref mat);
    }

    /// <summary>
    /// Multiplies two 4x4 matrices. The destination matrix receives the result.
    /// </summary>
    /// <param name="dst">First input matrix and is also the Matrix to receive the result</param>
    /// <param name="src">Second input matrix, to be multiplied with "dst".</param>
    public void MultiplyMatrix4(ref Matrix4x4 dst, ref Matrix4x4 src)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiMultiplyMatrix4>(FunctionNames.aiMultiplyMatrix4);

        func(ref dst, ref src);
    }

    /// <summary>
    /// Multiplies two 3x3 matrices. The destination matrix receives the result.
    /// </summary>
    /// <param name="dst">First input matrix and is also the Matrix to receive the result</param>
    /// <param name="src">Second input matrix, to be multiplied with "dst".</param>
    public void MultiplyMatrix3(ref Matrix3x3 dst, ref Matrix3x3 src)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiMultiplyMatrix3>(FunctionNames.aiMultiplyMatrix3);

        func(ref dst, ref src);
    }

    /// <summary>
    /// Creates a 3x3 identity matrix.
    /// </summary>
    /// <param name="mat">Matrix to hold the identity</param>
    public void IdentityMatrix3(out Matrix3x3 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiIdentityMatrix3>(FunctionNames.aiIdentityMatrix3);

        func(out mat);
    }

    /// <summary>
    /// Creates a 4x4 identity matrix.
    /// </summary>
    /// <param name="mat">Matrix to hold the identity</param>
    public void IdentityMatrix4(out Matrix4x4 mat)
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiIdentityMatrix4>(FunctionNames.aiIdentityMatrix4);

        func(out mat);
    }

    #endregion

    #region Version Info

    /// <summary>
    /// Gets the Assimp legal info.
    /// </summary>
    /// <returns>String containing Assimp legal info.</returns>
    public string GetLegalString()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetLegalString>(FunctionNames.aiGetLegalString);

        var ptr = func();

        if(ptr == nint.Zero)
            return string.Empty;

        return Marshal.PtrToStringAnsi(ptr);
    }

    /// <summary>
    /// Gets the native Assimp DLL's minor version number.
    /// </summary>
    /// <returns>Assimp minor version number</returns>
    public uint GetVersionMinor()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetVersionMinor>(FunctionNames.aiGetVersionMinor);

        return func();
    }

    /// <summary>
    /// Gets the native Assimp DLL's major version number.
    /// </summary>
    /// <returns>Assimp major version number</returns>
    public uint GetVersionMajor()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetVersionMajor>(FunctionNames.aiGetVersionMajor);

        return func();
    }

    /// <summary>
    /// Gets the native Assimp DLL's revision version number.
    /// </summary>
    /// <returns>Assimp revision version number</returns>
    public uint GetVersionRevision()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetVersionRevision>(FunctionNames.aiGetVersionRevision);

        return func();
    }

    /// <summary>
    /// Returns the branchname of the Assimp runtime.
    /// </summary>
    /// <returns>The current branch name.</returns>
    public string GetBranchName()
    {
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetBranchName>(FunctionNames.aiGetBranchName);

        var ptr = func();

        if(ptr == nint.Zero)
            return string.Empty;

        return Marshal.PtrToStringAnsi(ptr);
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
        LoadIfNotLoaded();

        var func = GetFunction<Functions.aiGetCompileFlags>(FunctionNames.aiGetCompileFlags);

        return (CompileFlags) func();
    }

    #endregion

    #region Function names 

    /// <summary>
    /// Defines all the unmanaged assimp C-function names.
    /// </summary>
    internal static class FunctionNames
    {

        #region Import Function Names

        public const string aiImportFile = "aiImportFile";
        public const string aiImportFileEx = "aiImportFileEx";
        public const string aiImportFileExWithProperties = "aiImportFileExWithProperties";
        public const string aiImportFileFromMemory = "aiImportFileFromMemory";
        public const string aiImportFileFromMemoryWithProperties = "aiImportFileFromMemoryWithProperties";
        public const string aiReleaseImport = "aiReleaseImport";
        public const string aiApplyPostProcessing = "aiApplyPostProcessing";

        #endregion

        #region Export Function Names

        public const string aiGetExportFormatCount = "aiGetExportFormatCount";
        public const string aiGetExportFormatDescription = "aiGetExportFormatDescription";
        public const string aiReleaseExportFormatDescription = "aiReleaseExportFormatDescription";
        public const string aiExportSceneToBlob = "aiExportSceneToBlob";
        public const string aiReleaseExportBlob = "aiReleaseExportBlob";
        public const string aiExportScene = "aiExportScene";
        public const string aiExportSceneEx = "aiExportSceneEx";
        public const string aiCopyScene = "aiCopyScene";

        #endregion

        #region Logging Function Names

        public const string aiAttachLogStream = "aiAttachLogStream";
        public const string aiEnableVerboseLogging = "aiEnableVerboseLogging";
        public const string aiDetachLogStream = "aiDetachLogStream";
        public const string aiDetachAllLogStreams = "aiDetachAllLogStreams";

        #endregion

        #region Import Properties Function Names

        public const string aiCreatePropertyStore = "aiCreatePropertyStore";
        public const string aiReleasePropertyStore = "aiReleasePropertyStore";
        public const string aiSetImportPropertyInteger = "aiSetImportPropertyInteger";
        public const string aiSetImportPropertyFloat = "aiSetImportPropertyFloat";
        public const string aiSetImportPropertyString = "aiSetImportPropertyString";
        public const string aiSetImportPropertyMatrix = "aiSetImportPropertyMatrix";

        #endregion

        #region Material Getters Function Names

        public const string aiGetMaterialColor = "aiGetMaterialColor";
        public const string aiGetMaterialFloatArray = "aiGetMaterialFloatArray";
        public const string aiGetMaterialIntegerArray = "aiGetMaterialIntegerArray";
        public const string aiGetMaterialProperty = "aiGetMaterialProperty";
        public const string aiGetMaterialString = "aiGetMaterialString";
        public const string aiGetMaterialTextureCount = "aiGetMaterialTextureCount";
        public const string aiGetMaterialTexture = "aiGetMaterialTexture";

        #endregion

        #region Error and Info Function Names

        public const string aiGetErrorString = "aiGetErrorString";
        public const string aiIsExtensionSupported = "aiIsExtensionSupported";
        public const string aiGetExtensionList = "aiGetExtensionList";
        public const string aiGetImportFormatCount = "aiGetImportFormatCount";
        public const string aiGetImportFormatDescription = "aiGetImportFormatDescription";
        public const string aiGetMemoryRequirements = "aiGetMemoryRequirements";

        #endregion

        #region Math Function Names

        public const string aiCreateQuaternionFromMatrix = "aiCreateQuaternionFromMatrix";
        public const string aiDecomposeMatrix = "aiDecomposeMatrix";
        public const string aiTransposeMatrix4 = "aiTransposeMatrix4";
        public const string aiTransposeMatrix3 = "aiTransposeMatrix3";
        public const string aiTransformVecByMatrix3 = "aiTransformVecByMatrix3";
        public const string aiTransformVecByMatrix4 = "aiTransformVecByMatrix4";
        public const string aiMultiplyMatrix4 = "aiMultiplyMatrix4";
        public const string aiMultiplyMatrix3 = "aiMultiplyMatrix3";
        public const string aiIdentityMatrix3 = "aiIdentityMatrix3";
        public const string aiIdentityMatrix4 = "aiIdentityMatrix4";

        #endregion

        #region Version Info Function Names

        public const string aiGetLegalString = "aiGetLegalString";
        public const string aiGetVersionMinor = "aiGetVersionMinor";
        public const string aiGetVersionMajor = "aiGetVersionMajor";
        public const string aiGetVersionRevision = "aiGetVersionRevision";
        public const string aiGetCompileFlags = "aiGetCompileFlags";
        public const string aiGetBranchName = "aiGetBranchName";

        #endregion
    }

    #endregion

    #region Function delegates

    /// <summary>
    /// Defines all of the delegates that represent the unmanaged assimp functions.
    /// </summary>
    internal static class Functions
    {

        #region Import Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiImportFile)]
        public delegate nint aiImportFile([In, MarshalAs(UnmanagedType.LPStr)] string file, uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiImportFileEx)]
        public delegate nint aiImportFileEx([In, MarshalAs(UnmanagedType.LPStr)] string file, uint flags, nint fileIO);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiImportFileExWithProperties)]
        public delegate nint aiImportFileExWithProperties([In, MarshalAs(UnmanagedType.LPStr)] string file, uint flag, nint fileIO, nint propStore);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiImportFileFromMemory)]
        public delegate nint aiImportFileFromMemory(byte[] buffer, uint bufferLength, uint flags, [In, MarshalAs(UnmanagedType.LPStr)] string formatHint);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiImportFileFromMemoryWithProperties)]
        public delegate nint aiImportFileFromMemoryWithProperties(byte[] buffer, uint bufferLength, uint flags, [In, MarshalAs(UnmanagedType.LPStr)] string formatHint, nint propStore);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiReleaseImport)]
        public delegate void aiReleaseImport(nint scene);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiApplyPostProcessing)]
        public delegate nint aiApplyPostProcessing(nint scene, uint Flags);

        #endregion

        #region Export Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetExportFormatCount)]
        public delegate nuint aiGetExportFormatCount();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetExportFormatDescription)]
        public delegate nint aiGetExportFormatDescription(nuint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiReleaseExportFormatDescription)]
        public delegate void aiReleaseExportFormatDescription(nint desc);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiExportSceneToBlob)]
        public delegate nint aiExportSceneToBlob(nint scene, [In, MarshalAs(UnmanagedType.LPStr)] string formatId, uint preProcessing);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiReleaseExportBlob)]
        public delegate void aiReleaseExportBlob(nint blobData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiExportScene)]
        public delegate ReturnCode aiExportScene(nint scene, [In, MarshalAs(UnmanagedType.LPStr)] string formatId, [In, MarshalAs(UnmanagedType.LPStr)] string fileName, uint preProcessing);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiExportSceneEx)]
        public delegate ReturnCode aiExportSceneEx(nint scene, [In, MarshalAs(UnmanagedType.LPStr)] string formatId, [In, MarshalAs(UnmanagedType.LPStr)] string fileName, nint fileIO, uint preProcessing);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiCopyScene)]
        public delegate void aiCopyScene(nint sceneIn, out nint sceneOut);

        #endregion

        #region Logging Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiAttachLogStream)]
        public delegate void aiAttachLogStream(nint logStreamPtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiEnableVerboseLogging)]
        public delegate void aiEnableVerboseLogging([In, MarshalAs(UnmanagedType.Bool)] bool enable);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiDetachLogStream)]
        public delegate ReturnCode aiDetachLogStream(nint logStreamPtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiDetachAllLogStreams)]
        public delegate void aiDetachAllLogStreams();

        #endregion

        #region Property Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiCreatePropertyStore)]
        public delegate nint aiCreatePropertyStore();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiReleasePropertyStore)]
        public delegate void aiReleasePropertyStore(nint propertyStore);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiSetImportPropertyInteger)]
        public delegate void aiSetImportPropertyInteger(nint propertyStore, [In, MarshalAs(UnmanagedType.LPStr)] string name, int value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiSetImportPropertyFloat)]
        public delegate void aiSetImportPropertyFloat(nint propertyStore, [In, MarshalAs(UnmanagedType.LPStr)] string name, float value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiSetImportPropertyString)]
        public delegate void aiSetImportPropertyString(nint propertyStore, [In, MarshalAs(UnmanagedType.LPStr)] string name, ref AiString value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiSetImportPropertyMatrix)]
        public delegate void aiSetImportPropertyMatrix(nint propertyStore, [In, MarshalAs(UnmanagedType.LPStr)] string name, ref Matrix4x4 value);

        #endregion

        #region Material Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialColor)]
        public delegate ReturnCode aiGetMaterialColor(ref AiMaterial mat, [In, MarshalAs(UnmanagedType.LPStr)] string key, uint texType, uint texIndex, nint colorOut);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialFloatArray)]
        public delegate ReturnCode aiGetMaterialFloatArray(ref AiMaterial mat, [In, MarshalAs(UnmanagedType.LPStr)] string key, uint texType, uint texIndex, nint ptrOut, ref uint valueCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialIntegerArray)]
        public delegate ReturnCode aiGetMaterialIntegerArray(ref AiMaterial mat, [In, MarshalAs(UnmanagedType.LPStr)] string key, uint texType, uint texIndex, nint ptrOut, ref uint valueCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialProperty)]
        public delegate ReturnCode aiGetMaterialProperty(ref AiMaterial mat, [In, MarshalAs(UnmanagedType.LPStr)] string key, uint texType, uint texIndex, out nint propertyOut);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialString)]
        public delegate ReturnCode aiGetMaterialString(ref AiMaterial mat, [In, MarshalAs(UnmanagedType.LPStr)] string key, uint texType, uint texIndex, out AiString str);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialTexture)]
        public delegate ReturnCode aiGetMaterialTexture(ref AiMaterial mat, TextureType type, uint index, out AiString path, out TextureMapping mapping, out uint uvIndex, out float blendFactor, out TextureOperation textureOp, [In, Out] TextureWrapMode[] wrapModes, out uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMaterialTextureCount)]
        public delegate uint aiGetMaterialTextureCount(ref AiMaterial mat, TextureType type);

        #endregion

        #region Math Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiCreateQuaternionFromMatrix)]
        public delegate void aiCreateQuaternionFromMatrix(out Quaternion quat, ref Matrix3x3 mat);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiDecomposeMatrix)]
        public delegate void aiDecomposeMatrix(ref Matrix4x4 mat, out Vector3D scaling, out Quaternion rotation, out Vector3D position);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiTransposeMatrix4)]
        public delegate void aiTransposeMatrix4(ref Matrix4x4 mat);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiTransposeMatrix3)]
        public delegate void aiTransposeMatrix3(ref Matrix3x3 mat);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiTransformVecByMatrix3)]
        public delegate void aiTransformVecByMatrix3(ref Vector3D vec, ref Matrix3x3 mat);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiTransformVecByMatrix4)]
        public delegate void aiTransformVecByMatrix4(ref Vector3D vec, ref Matrix4x4 mat);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiMultiplyMatrix4)]
        public delegate void aiMultiplyMatrix4(ref Matrix4x4 dst, ref Matrix4x4 src);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiMultiplyMatrix3)]
        public delegate void aiMultiplyMatrix3(ref Matrix3x3 dst, ref Matrix3x3 src);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiIdentityMatrix3)]
        public delegate void aiIdentityMatrix3(out Matrix3x3 mat);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiIdentityMatrix4)]
        public delegate void aiIdentityMatrix4(out Matrix4x4 mat);

        #endregion

        #region Error and Info Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetErrorString)]
        public delegate nint aiGetErrorString();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetExtensionList)]
        public delegate void aiGetExtensionList(ref AiString extensionsOut);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetMemoryRequirements)]
        public delegate void aiGetMemoryRequirements(nint scene, ref AiMemoryInfo memoryInfo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiIsExtensionSupported)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool aiIsExtensionSupported([In, MarshalAs(UnmanagedType.LPStr)] string extension);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetImportFormatCount)]
        public delegate nuint aiGetImportFormatCount();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetImportFormatDescription)]
        public delegate nint aiGetImportFormatDescription(nuint index);

        #endregion

        #region Version Info Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetLegalString)]
        public delegate nint aiGetLegalString();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetVersionMinor)]
        public delegate uint aiGetVersionMinor();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetVersionMajor)]
        public delegate uint aiGetVersionMajor();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetVersionRevision)]
        public delegate uint aiGetVersionRevision();
            
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetBranchName)]
        public delegate nint aiGetBranchName();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), UnmanagedFunctionName(FunctionNames.aiGetCompileFlags)]
        public delegate uint aiGetCompileFlags();

        #endregion
    }

    #endregion
}