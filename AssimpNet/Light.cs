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
using Assimp.Unmanaged;

namespace Assimp;

/// <summary>
/// Describes a light source in the scene. Assimp supports multiple light sources
/// including spot, point, and directional lights. All are defined by a single structure
/// and distinguished by their parameters. Lights have corresponding nodes in the scenegraph.
/// <para>Some file formats such as 3DS and ASE export a "target point", e.g. the point
/// a spot light is looking at (it can even be animated). Assimp writes the target point as a subnode
/// of a spotlight's main node called "spotName.Target". However, this is just additional information
/// then, the transform tracks of the main node make the spot light already point in the right direction.</para>
/// </summary>
public sealed class Light : IMarshalable<Light, AiLight>
{
    /// <summary>
    /// Gets or sets the name of the light source. This corresponds to a node present in the scenegraph.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of light source. This should never be undefined.
    /// </summary>
    public LightSourceType LightType { get; set; }

    /// <summary>
    /// Gets or sets the inner angle of a spot light's light cone. The spot light has
    /// maximum influence on objects inside this angle. The angle is given in radians, it
    /// is 2PI for point lights and defined for directional lights.
    /// </summary>
    public float AngleInnerCone { get; set; }

    /// <summary>
    /// Gets or sets the outer angle of a spot light's light cone. The spot light does not affect objects outside
    /// this angle. The angle is given in radians. It is 2PI for point lights and undefined for
    /// directional lights. The outer angle must be greater than or equal to the inner angle.
    /// </summary>
    public float AngleOuterCone { get; set; }

    /// <summary>
    /// Gets or sets the constant light attenuation factor. The intensity of the light source
    /// at a given distance 'd' from the light position is <code>Atten = 1 / (att0 + att1 * d + att2 * d*d)</code>.
    /// <para>This member corresponds to the att0 variable in the equation and is undefined for directional lights.</para>
    /// </summary>
    public float AttenuationConstant { get; set; }

    /// <summary>
    /// Gets or sets the linear light attenuation factor. The intensity of the light source
    /// at a given distance 'd' from the light position is <code>Atten = 1 / (att0 + att1 * d + att2 * d*d)</code>
    /// <para>This member corresponds to the att1 variable in the equation and is undefined for directional lights.</para>
    /// </summary>
    public float AttenuationLinear { get; set; }

    /// <summary>
    /// Gets or sets the quadratic light attenuation factor. The intensity of the light source
    /// at a given distance 'd' from the light position is <code>Atten = 1 / (att0 + att1 * d + att2 * d*d)</code>.
    /// <para>This member corresponds to the att2 variable in the equation and is undefined for directional lights.</para>
    /// </summary>
    public float AttenuationQuadratic { get; set; }

    /// <summary>
    /// Gets or sets the position of the light source in space, relative to the
    /// transformation of the node corresponding to the light. This is undefined for
    /// directional lights.
    /// </summary>
    public Vector3D Position { get; set; }

    /// <summary>
    /// Gets or sets the direction of the light source in space, relative to the transformation
    /// of the node corresponding to the light. This is undefined for point lights.
    /// </summary>
    public Vector3D Direction { get; set; }

    /// <summary>
    /// Gets or sets the up vector of the light source in space, relative to the transformation of the node corresponding to the light.
    /// This is undefined for point lights.
    /// </summary>
    public Vector3D Up { get; set; }

    /// <summary>
    /// Gets or sets the diffuse color of the light source.  The diffuse light color is multiplied with
    /// the diffuse material color to obtain the final color that contributes to the diffuse shading term.
    /// </summary>
    public Color3D ColorDiffuse { get; set; }

    /// <summary>
    /// Gets or sets the specular color of the light source. The specular light color is multiplied with the
    /// specular material color to obtain the final color that contributes to the specular shading term.
    /// </summary>
    public Color3D ColorSpecular { get; set; }

    /// <summary>
    /// Gets or sets the ambient color of the light source. The ambient light color is multiplied with the ambient
    /// material color to obtain the final color that contributes to the ambient shading term.
    /// </summary>
    public Color3D ColorAmbient { get; set; }

    /// <summary>
    /// Gets or sets the Width (X) and Height (Y) of the area that represents an <see cref="LightSourceType.Area"/> light.
    /// </summary>
    public Vector2D AreaSize { get; set; }

    /// <summary>
    /// Constructs a new instance of the <see cref="Light"/> class.
    /// </summary>
    public Light()
    {
        LightType = LightSourceType.Undefined;
        AttenuationConstant = 0.0f;
        AttenuationLinear = 1.0f;
        AttenuationQuadratic = 0.0f;
        AngleInnerCone = (float) Math.PI * 2.0f;
        AngleOuterCone = (float) Math.PI * 2.0f;
        AreaSize = new(0.0f, 0.0f);
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Light, AiLight>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<Light, AiLight>.ToNative(nint thisPtr, out AiLight nativeValue)
    {
        nativeValue.Name = new(Name);
        nativeValue.Type = LightType;
        nativeValue.AngleInnerCone = AngleInnerCone;
        nativeValue.AngleOuterCone = AngleOuterCone;
        nativeValue.AttenuationConstant = AttenuationConstant;
        nativeValue.AttenuationLinear = AttenuationLinear;
        nativeValue.AttenuationQuadratic = AttenuationQuadratic;
        nativeValue.ColorAmbient = ColorAmbient;
        nativeValue.ColorDiffuse = ColorDiffuse;
        nativeValue.ColorSpecular = ColorSpecular;
        nativeValue.Direction = Direction;
        nativeValue.Up = Up;
        nativeValue.Position = Position;
        nativeValue.AreaSize = AreaSize;
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<Light, AiLight>.FromNative(in AiLight nativeValue)
    {
        Name = AiString.GetString(nativeValue.Name); //Avoid struct copy
        LightType = nativeValue.Type;
        AngleInnerCone = nativeValue.AngleInnerCone;
        AngleOuterCone = nativeValue.AngleOuterCone;
        AttenuationConstant = nativeValue.AttenuationConstant;
        AttenuationLinear = nativeValue.AttenuationLinear;
        AttenuationQuadratic = nativeValue.AttenuationQuadratic;
        Position = nativeValue.Position;
        Direction = nativeValue.Direction;
        Up = nativeValue.Up;
        ColorDiffuse = nativeValue.ColorDiffuse;
        ColorSpecular = nativeValue.ColorSpecular;
        ColorAmbient = nativeValue.ColorAmbient;
        AreaSize = nativeValue.AreaSize;
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Light, AiLight}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue != nint.Zero && freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}