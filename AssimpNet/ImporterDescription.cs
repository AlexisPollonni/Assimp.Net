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
using Silk.NET.Assimp;

namespace Assimp;

/// <summary>
/// Metadata and feature support information for a given importer.
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class ImporterDescription
{
    /// <summary>
    /// Gets the name of the importer (e.g. Blender3D Importer)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the original author (blank if unknown or assimp team).
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets the name of the current maintainer, if empty then the author maintains.
    /// </summary>
    public string Maintainer { get; }

    /// <summary>
    /// Gets any implementation comments.
    /// </summary>
    public string Comments { get; }

    /// <summary>
    /// Gets the features supported by the importer.
    /// </summary>
    public ImporterFlags FeatureFlags { get; }

    /// <summary>
    /// Gets the minimum version of the file format supported. If no version scheme, forwards compatible, or importer doesn't care, major/min will be zero.
    /// </summary>
    public Version MinVersion { get; }

    /// <summary>
    /// Gets the maximum version of the file format supported. If no version scheme, forwards compatible, or importer doesn't care, major/min will be zero.
    /// </summary>
    public Version MaxVersion { get; }

    /// <summary>
    /// Gets the list of file extensions the importer can handle. All entries are lower case and do NOT have a leading dot.
    /// </summary>
    public string[] FileExtensions { get; }

    internal unsafe ImporterDescription(in AiImporterDesc descr)
    {
        Name = Marshal.PtrToStringAnsi((nint)descr.MName);
        Author = Marshal.PtrToStringAnsi((nint)descr.MAuthor);
        Maintainer = Marshal.PtrToStringAnsi((nint)descr.MMaintainer);
        Comments = Marshal.PtrToStringAnsi((nint)descr.MComments);
        FeatureFlags = (ImporterFlags)descr.MFlags;
        MinVersion = new((int) descr.MMinMajor, (int) descr.MMinMinor);
        MaxVersion = new((int) descr.MMaxMajor, (int) descr.MMaxMajor);

        var fileExts = Marshal.PtrToStringAnsi((nint)descr.MFileExtensions);
        FileExtensions = string.IsNullOrEmpty(fileExts) ? [] : fileExts.Split(' ');
    }
}