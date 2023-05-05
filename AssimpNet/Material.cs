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
using Assimp.Unmanaged;

namespace Assimp;

/// <summary>
/// A material contains all the information that describes how to render a mesh. E.g. textures, colors, and render states. Internally
/// all this information is stored as key-value pair properties. The class contains many convienence methods and properties for
/// accessing non-texture/texture properties without having to know the Assimp material key names. Not all properties may be present,
/// and if they aren't a default value will be returned.
/// </summary>
public sealed class Material : IMarshalable<Material, AiMaterial>
{
    private readonly Dictionary<string, MaterialProperty> m_properties;

    /// <summary>
    /// Gets the number of properties contained in the material.
    /// </summary>
    public int PropertyCount => m_properties.Count;

    #region Convienent non-texture properties

    /// <summary>
    /// Checks if the material has a name property.
    /// </summary>
    public bool HasName => HasProperty(AiMatKeys.NAME);

    /// <summary>
    /// Gets the material name value, if any. Default value is an empty string.
    /// </summary>
    public string Name
    {
        get
        {
            var prop = GetProperty(AiMatKeys.NAME);
            if(prop != null)
                return prop.GetStringValue();

            return string.Empty;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.NAME);

            if(prop == null)
            {
                prop = new(AiMatKeys.NAME_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetStringValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a two-sided property.
    /// </summary>
    public bool HasTwoSided => HasProperty(AiMatKeys.TWOSIDED);

    /// <summary>
    /// Gets if the material should be rendered as two-sided. Default value is false.
    /// </summary>
    public bool IsTwoSided
    {
        get
        {
            var prop = GetProperty(AiMatKeys.TWOSIDED);
            if(prop != null)
                return prop.GetBooleanValue();

            return false;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.TWOSIDED);

            if(prop == null)
            {
                prop = new(AiMatKeys.TWOSIDED_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetBooleanValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a shading-mode property.
    /// </summary>
    public bool HasShadingMode => HasProperty(AiMatKeys.SHADING_MODEL);

    /// <summary>
    /// Gets the shading mode. Default value is <see cref="Assimp.ShadingMode.None"/>, meaning it is not defined.
    /// </summary>
    public ShadingMode ShadingMode
    {
        get
        {
            var prop = GetProperty(AiMatKeys.SHADING_MODEL);
            if(prop != null)
                return (ShadingMode) prop.GetIntegerValue();

            return ShadingMode.None;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.SHADING_MODEL);

            if(prop == null)
            {
                prop = new(AiMatKeys.SHADING_MODEL_BASE, (int) value);
                AddProperty(prop);
            }
            else
            {
                prop.SetIntegerValue((int) value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a wireframe property.
    /// </summary>
    public bool HasWireFrame => HasProperty(AiMatKeys.ENABLE_WIREFRAME);

    /// <summary>
    /// Gets if wireframe should be enabled. Default value is false.
    /// </summary>
    public bool IsWireFrameEnabled
    {
        get
        {
            var prop = GetProperty(AiMatKeys.ENABLE_WIREFRAME);
            if(prop != null)
                return prop.GetBooleanValue();

            return false;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.ENABLE_WIREFRAME);

            if(prop == null)
            {
                prop = new(AiMatKeys.ENABLE_WIREFRAME_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetBooleanValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a blend mode property.
    /// </summary>
    public bool HasBlendMode => HasProperty(AiMatKeys.BLEND_FUNC);

    /// <summary>
    /// Gets the blending mode. Default value is <see cref="Assimp.BlendMode.Default"/>.
    /// </summary>
    public BlendMode BlendMode
    {
        get
        {
            var prop = GetProperty(AiMatKeys.BLEND_FUNC);
            if(prop != null)
                return (BlendMode) prop.GetIntegerValue();

            return BlendMode.Default;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.BLEND_FUNC);

            if(prop == null)
            {
                prop = new(AiMatKeys.BLEND_FUNC_BASE, (int) value);
                AddProperty(prop);
            }
            else
            {
                prop.SetIntegerValue((int) value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has an opacity property.
    /// </summary>
    public bool HasOpacity => HasProperty(AiMatKeys.OPACITY);

    /// <summary>
    /// Gets the opacity. Default value is 1.0f.
    /// </summary>
    public float Opacity
    {
        get
        {
            var prop = GetProperty(AiMatKeys.OPACITY);
            if(prop != null)
                return prop.GetFloatValue();

            return 1.0f;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.OPACITY);

            if(prop == null)
            {
                prop = new(AiMatKeys.OPACITY_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetFloatValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a transparency factor property.
    /// </summary>
    public bool HasTransparencyFactor => HasProperty(AiMatKeys.TRANSPARENCYFACTOR);

    /// <summary>
    /// Gets the transparency factor.  This is used to make a surface more or less opaque (0 = opaque, 1 = transparent). Default value is 0.0f.
    /// </summary>
    public float TransparencyFactor
    {
        get
        {
            var prop = GetProperty(AiMatKeys.TRANSPARENCYFACTOR);
            if(prop != null)
                return prop.GetFloatValue();

            return 0.0f;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.TRANSPARENCYFACTOR);

            if(prop == null)
            {
                prop = new(AiMatKeys.TRANSPARENCYFACTOR_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetFloatValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a bump scaling property.
    /// </summary>
    public bool HasBumpScaling => HasProperty(AiMatKeys.BUMPSCALING);

    /// <summary>
    /// Gets the bump scaling. Default value is 0.0f;
    /// </summary>
    public float BumpScaling
    {
        get
        {
            var prop = GetProperty(AiMatKeys.BUMPSCALING);
            if(prop != null)
                return prop.GetFloatValue();

            return 0.0f;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.BUMPSCALING);

            if(prop == null)
            {
                prop = new(AiMatKeys.BUMPSCALING_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetFloatValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a shininess property.
    /// </summary>
    public bool HasShininess => HasProperty(AiMatKeys.SHININESS);

    /// <summary>
    /// Gets the shininess. Default value is 0.0f;
    /// </summary>
    public float Shininess
    {
        get
        {
            var prop = GetProperty(AiMatKeys.SHININESS);
            if(prop != null)
                return prop.GetFloatValue();

            return 0.0f;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.SHININESS);

            if(prop == null)
            {
                prop = new(AiMatKeys.SHININESS_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetFloatValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a shininess strength property.
    /// </summary>
    public bool HasShininessStrength => HasProperty(AiMatKeys.SHININESS_STRENGTH);

    /// <summary>
    /// Gets the shininess strength. Default vaulue is 1.0f.
    /// </summary>
    public float ShininessStrength
    {
        get
        {
            var prop = GetProperty(AiMatKeys.SHININESS_STRENGTH);
            if(prop != null)
                return prop.GetFloatValue();

            return 1.0f;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.SHININESS_STRENGTH);

            if(prop == null)
            {
                prop = new(AiMatKeys.SHININESS_STRENGTH_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetFloatValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a reflectivty property.
    /// </summary>
    public bool HasReflectivity => HasProperty(AiMatKeys.REFLECTIVITY);


    /// <summary>
    /// Gets the reflectivity. Default value is 0.0f;
    /// </summary>
    public float Reflectivity
    {
        get
        {
            var prop = GetProperty(AiMatKeys.REFLECTIVITY);
            if(prop != null)
                return prop.GetFloatValue();

            return 0.0f;
        }
        set
        {
            var prop = GetProperty(AiMatKeys.REFLECTIVITY);

            if(prop == null)
            {
                prop = new(AiMatKeys.REFLECTIVITY_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetFloatValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a color diffuse property.
    /// </summary>
    public bool HasColorDiffuse => HasProperty(AiMatKeys.COLOR_DIFFUSE);

    /// <summary>
    /// Gets the color diffuse. Default value is white.
    /// </summary>
    public Color4D ColorDiffuse
    {
        get
        {
            var prop = GetProperty(AiMatKeys.COLOR_DIFFUSE);
            if(prop != null)
                return prop.GetColor4DValue();

            return new(1.0f, 1.0f, 1.0f, 1.0f);
        }
        set
        {
            var prop = GetProperty(AiMatKeys.COLOR_DIFFUSE);

            if(prop == null)
            {
                prop = new(AiMatKeys.COLOR_DIFFUSE_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetColor4DValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a color ambient property.
    /// </summary>
    public bool HasColorAmbient => HasProperty(AiMatKeys.COLOR_AMBIENT);

    /// <summary>
    /// Gets the color ambient. Default value is (.2f, .2f, .2f, 1.0f).
    /// </summary>
    public Color4D ColorAmbient
    {
        get
        {
            var prop = GetProperty(AiMatKeys.COLOR_AMBIENT);
            if(prop != null)
                return prop.GetColor4DValue();

            return new(.2f, .2f, .2f, 1.0f);
        }
        set
        {
            var prop = GetProperty(AiMatKeys.COLOR_AMBIENT);

            if(prop == null)
            {
                prop = new(AiMatKeys.COLOR_AMBIENT_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetColor4DValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a color specular property.
    /// </summary>
    public bool HasColorSpecular => HasProperty(AiMatKeys.COLOR_SPECULAR);

    /// <summary>
    /// Gets the color specular. Default value is black.
    /// </summary>
    public Color4D ColorSpecular
    {
        get
        {
            var prop = GetProperty(AiMatKeys.COLOR_SPECULAR);
            if(prop != null)
                return prop.GetColor4DValue();

            return new(0, 0, 0, 1.0f);
        }
        set
        {
            var prop = GetProperty(AiMatKeys.COLOR_SPECULAR);

            if(prop == null)
            {
                prop = new(AiMatKeys.COLOR_SPECULAR_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetColor4DValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a color emissive property.
    /// </summary>
    public bool HasColorEmissive => HasProperty(AiMatKeys.COLOR_EMISSIVE);

    /// <summary>
    /// Gets the color emissive. Default value is black.
    /// </summary>
    public Color4D ColorEmissive
    {
        get
        {
            var prop = GetProperty(AiMatKeys.COLOR_EMISSIVE);
            if(prop != null)
                return prop.GetColor4DValue();

            return new(0, 0, 0, 1.0f);
        }
        set
        {
            var prop = GetProperty(AiMatKeys.COLOR_EMISSIVE);

            if(prop == null)
            {
                prop = new(AiMatKeys.COLOR_EMISSIVE_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetColor4DValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a color transparent property.
    /// </summary>
    public bool HasColorTransparent => HasProperty(AiMatKeys.COLOR_TRANSPARENT);

    /// <summary>
    /// Gets the color transparent. Default value is black.
    /// </summary>
    public Color4D ColorTransparent
    {
        get
        {
            var prop = GetProperty(AiMatKeys.COLOR_TRANSPARENT);
            if(prop != null)
                return prop.GetColor4DValue();

            return new(0, 0, 0, 1.0f);
        }
        set
        {
            var prop = GetProperty(AiMatKeys.COLOR_TRANSPARENT);

            if(prop == null)
            {
                prop = new(AiMatKeys.COLOR_TRANSPARENT_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetColor4DValue(value);
            }
        }
    }

    /// <summary>
    /// Checks if the material has a color reflective property.
    /// </summary>
    public bool HasColorReflective => HasProperty(AiMatKeys.COLOR_REFLECTIVE);

    /// <summary>
    /// Gets the color reflective. Default value is black.
    /// </summary>
    public Color4D ColorReflective
    {
        get
        {
            var prop = GetProperty(AiMatKeys.COLOR_REFLECTIVE);
            if(prop != null)
                return prop.GetColor4DValue();

            return new(0, 0, 0, 1.0f);
        }
        set
        {
            var prop = GetProperty(AiMatKeys.COLOR_REFLECTIVE);

            if(prop == null)
            {
                prop = new(AiMatKeys.COLOR_REFLECTIVE_BASE, value);
                AddProperty(prop);
            }
            else
            {
                prop.SetColor4DValue(value);
            }
        }
    }

    #endregion

    #region Convienent texture properties

    /// <summary>
    /// Gets if the material has a diffuse texture in the first texture index.
    /// </summary>
    public bool HasTextureDiffuse => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Diffuse, 0);

    /// <summary>
    /// Gets or sets diffuse texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureDiffuse
    {
        get
        {
            GetMaterialTexture(TextureType.Diffuse, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Diffuse})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a specular texture in the first texture index.
    /// </summary>
    public bool HasTextureSpecular => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Specular, 0);

    /// <summary>
    /// Gets or sets specular texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureSpecular
    {
        get
        {
            GetMaterialTexture(TextureType.Specular, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Specular})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a ambient texture in the first texture index.
    /// </summary>
    public bool HasTextureAmbient => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Ambient, 0);

    /// <summary>
    /// Gets or sets ambient texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureAmbient
    {
        get
        {
            GetMaterialTexture(TextureType.Ambient, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Ambient})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a emissive texture in the first texture index.
    /// </summary>
    public bool HasTextureEmissive => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Emissive, 0);

    /// <summary>
    /// Gets or sets emissive texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureEmissive
    {
        get
        {
            GetMaterialTexture(TextureType.Emissive, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Emissive})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a height texture in the first texture index.
    /// </summary>
    public bool HasTextureHeight => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Height, 0);

    /// <summary>
    /// Gets or sets height texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureHeight
    {
        get
        {
            GetMaterialTexture(TextureType.Height, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Height})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a normal texture in the first texture index.
    /// </summary>
    public bool HasTextureNormal => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Normals, 0);

    /// <summary>
    /// Gets or sets normal texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureNormal
    {
        get
        {
            GetMaterialTexture(TextureType.Normals, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Normals})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has an opacity texture in the first texture index.
    /// </summary>
    public bool HasTextureOpacity => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Opacity, 0);

    /// <summary>
    /// Gets or sets opacity texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureOpacity
    {
        get
        {
            GetMaterialTexture(TextureType.Opacity, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Opacity})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a displacement texture in the first texture index.
    /// </summary>
    public bool HasTextureDisplacement => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Displacement, 0);

    /// <summary>
    /// Gets or sets displacement texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureDisplacement
    {
        get
        {
            GetMaterialTexture(TextureType.Displacement, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Displacement})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a light map texture in the first texture index.
    /// </summary>
    public bool HasTextureLightMap => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Lightmap, 0);

    /// <summary>
    /// Gets or sets light map texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureLightMap
    {
        get
        {
            GetMaterialTexture(TextureType.Lightmap, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Lightmap})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has an ambient occlusion map in in the first texture index.
    /// </summary>
    public bool HasTextureAmbientOcclusion => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.AmbientOcclusion, 0);

    /// <summary>
    /// Gets or sets ambient occlusion texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureAmbientOcclusion
    {
        get
        {
            GetMaterialTexture(TextureType.AmbientOcclusion, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.AmbientOcclusion})
                AddMaterialTexture(value);
        }
    }

    /// <summary>
    /// Gets if the material has a reflection texture in the first texture index.
    /// </summary>
    public bool HasTextureReflection => HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Reflection, 0);

    /// <summary>
    /// Gets or sets reflection texture properties in the first texture index.
    /// </summary>
    public TextureSlot TextureReflection
    {
        get
        {
            GetMaterialTexture(TextureType.Reflection, 0, out var tex);

            return tex;
        }
        set
        {
            if(value is {TextureIndex: 0, TextureType: TextureType.Reflection})
                AddMaterialTexture(value);
        }
    }

    #endregion

    #region PBR Properties

    /// <summary>
    /// Determines if the material is part of a PBR workflow or not.
    /// </summary>
    public bool IsPBRMaterial
    {
        get
        {
            var pbr = PBR;
            return pbr.HasTextureBaseColor || pbr.HasTextureMetalness || pbr.HasTextureRoughness || pbr.HasTextureNormalCamera || pbr.HasTextureEmissionColor;
        }
    }

    /// <summary>
    /// Gets a group accessor for any PBR properties in the material.
    /// </summary>
    public PBRMaterialProperties PBR { get; }

    #endregion

    #region Shader Properties

    /// <summary>
    /// Gets if the material has embedded shader source code.
    /// </summary>
    public bool HasShaders
    {
        get
        {
            var shaders = Shaders;
            return shaders.HasShaderLanguageType && (shaders.HasVertexShader || shaders.HasFragmentShader || shaders.HasGeometryShader || shaders.HasTesselationShader || shaders.HasPrimitiveShader || shaders.HasComputeShader);
        }
    }

    /// <summary>
    /// Gets a group accessor for any embedded shader source code in the material.
    /// </summary>
    public ShaderMaterialProperties Shaders { get; }

    #endregion

    /// <summary>
    /// Constructs a new instance of the <see cref="Material"/> class.
    /// </summary>
    public Material()
    {
        m_properties = new();
        PBR = new(this);
        Shaders = new(this);
    }

    /// <summary>
    /// Helper method to construct a fully qualified name from the input parameters. All the input parameters are combined into the fully qualified name: {baseName},{texType},{texIndex}. E.g.
    /// "$clr.diffuse,0,0" or "$tex.file,1,0". This is the name that is used as the material dictionary key.
    /// </summary>
    /// <param name="baseName">Key basename, this must not be null or empty</param>
    /// <param name="texType">Texture type; non-texture properties should leave this <see cref="TextureType.None"/></param>
    /// <param name="texIndex">Texture index; non-texture properties should leave this zero.</param>
    /// <returns>The fully qualified name</returns>
    public static string CreateFullyQualifiedName(string baseName, TextureType texType, int texIndex)
    {
        if(string.IsNullOrEmpty(baseName))
            return string.Empty;

        return $"{baseName},{(int) texType},{texIndex}";
    }

    /// <summary>
    /// Gets the non-texture properties contained in this Material. The name should be
    /// the "base name", as in it should not contain texture type/texture index information. E.g. "$clr.diffuse" rather than "$clr.diffuse,0,0". The extra
    /// data will be filled in automatically.
    /// </summary>
    /// <param name="baseName">Key basename</param>
    /// <returns>The material property, if it exists</returns>
    public MaterialProperty GetNonTextureProperty(string baseName)
    {
        if(string.IsNullOrEmpty(baseName))
        {
            return null;
        }
        var fullyQualifiedName = CreateFullyQualifiedName(baseName, TextureType.None, 0);
        return GetProperty(fullyQualifiedName);
    }

    /// <summary>
    /// Gets the material property. All the input parameters are combined into the fully qualified name: {baseName},{texType},{texIndex}. E.g.
    /// "$clr.diffuse,0,0" or "$tex.file,1,0".
    /// </summary>
    /// <param name="baseName">Key basename</param>
    /// <param name="texType">Texture type; non-texture properties should leave this <see cref="TextureType.None"/></param>
    /// <param name="texIndex">Texture index; non-texture properties should leave this zero.</param>
    /// <returns>The material property, if it exists</returns>
    public MaterialProperty GetProperty(string baseName, TextureType texType, int texIndex)
    {
        if(string.IsNullOrEmpty(baseName))
        {
            return null;
        }
        var fullyQualifiedName = CreateFullyQualifiedName(baseName, texType, texIndex);
        return GetProperty(fullyQualifiedName);
    }

    /// <summary>
    /// Gets the material property by its fully qualified name. The format is: {baseName},{texType},{texIndex}. E.g.
    /// "$clr.diffuse,0,0" or "$tex.file,1,0".
    /// </summary>
    /// <param name="fullyQualifiedName">Fully qualified name of the property</param>
    /// <returns>The material property, if it exists</returns>
    public MaterialProperty GetProperty(string fullyQualifiedName)
    {
        if(string.IsNullOrEmpty(fullyQualifiedName))
        {
            return null;
        }

        if(!m_properties.TryGetValue(fullyQualifiedName, out var prop))
        {
            return null;
        }
        return prop;
    }

    /// <summary>
    /// Checks if the material has the specified non-texture property. The name should be
    /// the "base name", as in it should not contain texture type/texture index information. E.g. "$clr.diffuse" rather than "$clr.diffuse,0,0". The extra
    /// data will be filled in automatically.
    /// </summary>
    /// <param name="baseName">Key basename</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    public bool HasNonTextureProperty(string baseName)
    {
        if(string.IsNullOrEmpty(baseName))
        {
            return false;
        }
        var fullyQualifiedName = CreateFullyQualifiedName(baseName, TextureType.None, 0);
        return HasProperty(fullyQualifiedName);
    }

    /// <summary>
    /// Checks if the material has the specified property. All the input parameters are combined into the fully qualified name: {baseName},{texType},{texIndex}. E.g.
    /// "$clr.diffuse,0,0" or "$tex.file,1,0".
    /// </summary>
    /// <param name="baseName">Key basename</param>
    /// <param name="texType">Texture type; non-texture properties should leave this <see cref="TextureType.None"/></param>
    /// <param name="texIndex">Texture index; non-texture properties should leave this zero.</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    public bool HasProperty(string baseName, TextureType texType, int texIndex)
    {
        if(string.IsNullOrEmpty(baseName))
        {
            return false;
        }

        var fullyQualifiedName = CreateFullyQualifiedName(baseName, texType, texIndex);
        return HasProperty(fullyQualifiedName);
    }

    /// <summary>
    /// Checks if the material has the specified property by looking up its fully qualified name. The format is: {baseName},{texType},{texIndex}. E.g.
    /// "$clr.diffuse,0,0" or "$tex.file,1,0".
    /// </summary>
    /// <param name="fullyQualifiedName">Fully qualified name of the property</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    public bool HasProperty(string fullyQualifiedName)
    {
        if(string.IsNullOrEmpty(fullyQualifiedName))
        {
            return false;
        }
        return m_properties.ContainsKey(fullyQualifiedName);
    }

    /// <summary>
    /// Adds a property to this material.
    /// </summary>
    /// <param name="matProp">Material property</param>
    /// <returns>True if the property was successfully added, false otherwise (e.g. null or key already present).</returns>
    public bool AddProperty(MaterialProperty matProp)
    {
        if(matProp == null || string.IsNullOrEmpty(matProp.FullyQualifiedName))
            return false;

        if(m_properties.ContainsKey(matProp.FullyQualifiedName))
            return false;

        m_properties.Add(matProp.FullyQualifiedName, matProp);

        return true;
    }

    /// <summary>
    /// Removes a non-texture property from the material.
    /// </summary>
    /// <param name="baseName">Property name</param>
    /// <returns>True if the property was removed, false otherwise</returns>
    public bool RemoveNonTextureProperty(string baseName)
    {
        if(string.IsNullOrEmpty(baseName))
            return false;

        return RemoveProperty(CreateFullyQualifiedName(baseName, TextureType.None, 0));
    }

    /// <summary>
    /// Removes a property from the material.
    /// </summary>
    /// <param name="baseName">Name of the property</param>
    /// <param name="texType">Property texture type</param>
    /// <param name="texIndex">Property texture index</param>
    /// <returns>True if the property was removed, false otherwise</returns>
    public bool RemoveProperty(string baseName, TextureType texType, int texIndex)
    {
        if(string.IsNullOrEmpty(baseName))
            return false;

        return RemoveProperty(CreateFullyQualifiedName(baseName, texType, texIndex));
    }

    /// <summary>
    /// Removes a property from the material.
    /// </summary>
    /// <param name="fullyQualifiedName">Fully qualified name of the property ({basename},{texType},{texIndex})</param>
    /// <returns>True if the property was removed, false otherwise</returns>
    public bool RemoveProperty(string fullyQualifiedName)
    {
        if(string.IsNullOrEmpty(fullyQualifiedName))
            return false;

        return m_properties.Remove(fullyQualifiedName);
    }

    /// <summary>
    /// Removes all properties from the material;
    /// </summary>
    public void Clear()
    {
        m_properties.Clear();
    }

    /// <summary>
    /// Gets -all- properties contained in the Material.
    /// </summary>
    /// <returns>All properties in the material property map.</returns>
    public MaterialProperty[] GetAllProperties()
    {
        var matProps = new MaterialProperty[m_properties.Values.Count];
        m_properties.Values.CopyTo(matProps, 0);

        return matProps;
    }

    /// <summary>
    /// Gets all the number of textures that are of the specified texture type.
    /// </summary>
    /// <param name="texType">Texture type</param>
    /// <returns>Texture count</returns>
    public int GetMaterialTextureCount(TextureType texType)
    {
        var count = 0;
        foreach(var kv in m_properties)
        {
            var matProp = kv.Value;

            if(matProp.Name.StartsWith(AiMatKeys.TEXTURE_BASE) && matProp.TextureType == texType)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Adds a texture to the material - this bulk creates a property for each field. This will
    /// either create properties or overwrite existing properties. If the texture has no
    /// file path, nothing is added.
    /// </summary>
    /// <param name="texture">Texture to add</param>
    /// <returns>True if the texture properties were added or modified</returns>
    public bool AddMaterialTexture(in TextureSlot texture)
    {
        return AddMaterialTexture(in texture, false);
    }

    /// <summary>
    /// Adds a texture to the material - this bulk creates a property for each field. This will
    /// either create properties or overwrite existing properties. If the texture has no
    /// file path, nothing is added.
    /// </summary>
    /// <param name="texture">Texture to add</param>
    /// <param name="onlySetFilePath">True to only set the texture's file path, false otherwise</param>
    /// <returns>True if the texture properties were added or modified</returns>
    public bool AddMaterialTexture(in TextureSlot texture, bool onlySetFilePath)
    {
        if(string.IsNullOrEmpty(texture.FilePath))
            return false;

        var texType = texture.TextureType;
        var texIndex = texture.TextureIndex;

        var texName = CreateFullyQualifiedName(AiMatKeys.TEXTURE_BASE, texType, texIndex);

        var texNameProp = GetProperty(texName);

        if(texNameProp == null)
            AddProperty(new(AiMatKeys.TEXTURE_BASE, texture.FilePath, texType, texIndex));
        else
            texNameProp.SetStringValue(texture.FilePath);

        if(onlySetFilePath)
            return true;

        var mappingName = CreateFullyQualifiedName(AiMatKeys.MAPPING_BASE, texType, texIndex);
        var uvIndexName = CreateFullyQualifiedName(AiMatKeys.UVWSRC_BASE, texType, texIndex);
        var blendFactorName = CreateFullyQualifiedName(AiMatKeys.TEXBLEND_BASE, texType, texIndex);
        var texOpName = CreateFullyQualifiedName(AiMatKeys.TEXOP_BASE, texType, texIndex);
        var uMapModeName = CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_U_BASE, texType, texIndex);
        var vMapModeName = CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_V_BASE, texType, texIndex);
        var texFlagsName = CreateFullyQualifiedName(AiMatKeys.TEXFLAGS_BASE, texType, texIndex);

        var mappingNameProp = GetProperty(mappingName);
        var uvIndexNameProp = GetProperty(uvIndexName);
        var blendFactorNameProp = GetProperty(blendFactorName);
        var texOpNameProp = GetProperty(texOpName);
        var uMapModeNameProp = GetProperty(uMapModeName);
        var vMapModeNameProp = GetProperty(vMapModeName);
        var texFlagsNameProp = GetProperty(texFlagsName);

        if(mappingNameProp == null)
            AddProperty(new(AiMatKeys.MAPPING_BASE, (int) texture.Mapping));
        else
            mappingNameProp.SetIntegerValue((int) texture.Mapping);

        if(uvIndexNameProp == null)
            AddProperty(new(AiMatKeys.MAPPING_BASE, (int) texture.Mapping));
        else
            uvIndexNameProp.SetIntegerValue(texture.UVIndex);

        if(blendFactorNameProp == null)
            AddProperty(new(AiMatKeys.TEXBLEND_BASE, texture.BlendFactor));
        else
            blendFactorNameProp.SetFloatValue(texture.BlendFactor);

        if(texOpNameProp == null)
            AddProperty(new(AiMatKeys.TEXOP_BASE, (int) texture.Operation));
        else
            texOpNameProp.SetIntegerValue((int) texture.Operation);

        if(uMapModeNameProp == null)
            AddProperty(new(AiMatKeys.MAPPINGMODE_U_BASE, (int) texture.WrapModeU));
        else
            uMapModeNameProp.SetIntegerValue((int) texture.WrapModeU);

        if(vMapModeNameProp == null)
            AddProperty(new(AiMatKeys.MAPPINGMODE_V_BASE, (int) texture.WrapModeV));
        else
            vMapModeNameProp.SetIntegerValue((int) texture.WrapModeV);

        if(texFlagsNameProp == null)
            AddProperty(new(AiMatKeys.TEXFLAGS_BASE, texture.Flags));
        else
            texFlagsNameProp.SetIntegerValue(texture.Flags);

        return true;
    }

    /// <summary>
    /// Removes a texture from the material - this bulk removes a property for each field.
    /// If the texture has no file path, nothing is removed
    /// </summary>
    /// <param name="texture">Texture to remove</param>
    /// <returns>True if the texture was removed, false otherwise.</returns>
    public bool RemoveMaterialTexture(in TextureSlot texture)
    {
        if(string.IsNullOrEmpty(texture.FilePath))
            return false;

        var texType = texture.TextureType;
        var texIndex = texture.TextureIndex;

        var texName = CreateFullyQualifiedName(AiMatKeys.TEXTURE_BASE, texType, texIndex);
        var mappingName = CreateFullyQualifiedName(AiMatKeys.MAPPING_BASE, texType, texIndex);
        var uvIndexName = CreateFullyQualifiedName(AiMatKeys.UVWSRC_BASE, texType, texIndex);
        var blendFactorName = CreateFullyQualifiedName(AiMatKeys.TEXBLEND_BASE, texType, texIndex);
        var texOpName = CreateFullyQualifiedName(AiMatKeys.TEXOP_BASE, texType, texIndex);
        var uMapModeName = CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_U_BASE, texType, texIndex);
        var vMapModeName = CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_V_BASE, texType, texIndex);
        var texFlagsName = CreateFullyQualifiedName(AiMatKeys.TEXFLAGS_BASE, texType, texIndex);

        RemoveProperty(texName);
        RemoveProperty(mappingName);
        RemoveProperty(uvIndexName);
        RemoveProperty(blendFactorName);
        RemoveProperty(texOpName);
        RemoveProperty(uMapModeName);
        RemoveProperty(vMapModeName);
        RemoveProperty(texFlagsName);

        return true;
    }

    /// <summary>
    /// Gets a texture that corresponds to the type/index.
    /// </summary>
    /// <param name="texType">Texture type</param>
    /// <param name="texIndex">Texture index</param>
    /// <param name="texture">Texture description</param>
    /// <returns>True if the texture was found in the material</returns>
    public bool GetMaterialTexture(TextureType texType, int texIndex, out TextureSlot texture)
    {
        texture = new();

        var texName = CreateFullyQualifiedName(AiMatKeys.TEXTURE_BASE, texType, texIndex);

        var texNameProp = GetProperty(texName);

        //This one is necessary, the rest are optional
        if(texNameProp == null)
            return false;

        var mappingName = CreateFullyQualifiedName(AiMatKeys.MAPPING_BASE, texType, texIndex);
        var uvIndexName = CreateFullyQualifiedName(AiMatKeys.UVWSRC_BASE, texType, texIndex);
        var blendFactorName = CreateFullyQualifiedName(AiMatKeys.TEXBLEND_BASE, texType, texIndex);
        var texOpName = CreateFullyQualifiedName(AiMatKeys.TEXOP_BASE, texType, texIndex);
        var uMapModeName = CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_U_BASE, texType, texIndex);
        var vMapModeName = CreateFullyQualifiedName(AiMatKeys.MAPPINGMODE_V_BASE, texType, texIndex);
        var texFlagsName = CreateFullyQualifiedName(AiMatKeys.TEXFLAGS_BASE, texType, texIndex);

        var mappingNameProp = GetProperty(mappingName);
        var uvIndexNameProp = GetProperty(uvIndexName);
        var blendFactorNameProp = GetProperty(blendFactorName);
        var texOpNameProp = GetProperty(texOpName);
        var uMapModeNameProp = GetProperty(uMapModeName);
        var vMapModeNameProp = GetProperty(vMapModeName);
        var texFlagsNameProp = GetProperty(texFlagsName);

        texture.FilePath = texNameProp.GetStringValue();
        texture.TextureType = texType;
        texture.TextureIndex = texIndex;
        texture.Mapping = mappingNameProp != null ? (TextureMapping) mappingNameProp.GetIntegerValue() : TextureMapping.FromUV;
        texture.UVIndex = uvIndexNameProp?.GetIntegerValue() ?? 0;
        texture.BlendFactor = blendFactorNameProp?.GetFloatValue() ?? 0.0f;
        texture.Operation = texOpNameProp != null ? (TextureOperation) texOpNameProp.GetIntegerValue() : 0;
        texture.WrapModeU = uMapModeNameProp != null ? (TextureWrapMode) uMapModeNameProp.GetIntegerValue() : TextureWrapMode.Wrap;
        texture.WrapModeV = vMapModeNameProp != null ? (TextureWrapMode) vMapModeNameProp.GetIntegerValue() : TextureWrapMode.Wrap;
        texture.Flags = texFlagsNameProp?.GetIntegerValue() ?? 0;

        return true;
    }

    /// <summary>
    /// Gets all textures that correspond to the type.
    /// </summary>
    /// <param name="type">Texture type</param>
    /// <returns>The array of textures</returns>
    public TextureSlot[] GetMaterialTextures(TextureType type)
    {
        var count = GetMaterialTextureCount(type);

        if(count == 0)
            return Array.Empty<TextureSlot>();

        var textures = new TextureSlot[count];

        for(var i = 0; i < count; i++)
        {
            GetMaterialTexture(type, i, out var tex);
            textures[i] = tex;
        }

        return textures;
    }

    /// <summary>
    /// Gets all textures in the material.
    /// </summary>
    /// <returns>The array of textures</returns>
    public TextureSlot[] GetAllMaterialTextures()
    {
        var textures = new List<TextureSlot>();
        var types = Enum.GetValues(typeof(TextureType)) as TextureType[];

        foreach(var texType in types)
        {
            textures.AddRange(GetMaterialTextures(texType));
        }

        return textures.ToArray();
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Material, AiMaterial>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<Material, AiMaterial>.ToNative(nint thisPtr, out AiMaterial nativeValue)
    {
        nativeValue.NumAllocated = nativeValue.NumProperties = (uint) m_properties.Count;
        nativeValue.Properties = nint.Zero;

        if(m_properties.Count > 0)
        {
            var matProps = new MaterialProperty[m_properties.Values.Count];
            m_properties.Values.CopyTo(matProps, 0);

            nativeValue.Properties = MemoryHelper.ToNativeArray<MaterialProperty, AiMaterialProperty>(matProps, true);
        }
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<Material, AiMaterial>.FromNative(in AiMaterial nativeValue)
    {
        Clear();

        if(nativeValue.NumProperties > 0 && nativeValue.Properties != nint.Zero)
        {
            var matProps = MemoryHelper.FromNativeArray<MaterialProperty, AiMaterialProperty>(nativeValue.Properties, (int) nativeValue.NumProperties, true);

            foreach(var matProp in matProps)
            {
                AddProperty(matProp);
            }
        }
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Material, AiMaterial}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue == nint.Zero)
            return;

        var aiMaterial = MemoryHelper.Read<AiMaterial>(nativeValue);

        if(aiMaterial.NumAllocated > 0 && aiMaterial.Properties != nint.Zero)
            MemoryHelper.FreeNativeArray<AiMaterialProperty>(aiMaterial.Properties, (int) aiMaterial.NumProperties, MaterialProperty.FreeNative, true);

        if(freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion


    #region Property Group Accessors

    /// <summary>
    /// Groups all PBR workflow properties into a single accessor.
    /// </summary>
    public sealed class PBRMaterialProperties
    {
        private readonly Material m_parent;

        /// <summary>
        /// Gets if the material has a base color map (albedo/diffuse) texture in the first texture index.
        /// </summary>
        public bool HasTextureBaseColor => m_parent.HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.BaseColor, 0);

        /// <summary>
        /// Gets or sets the base color map (albedo/diffuse) texture properties in the first texture index.
        /// </summary>
        public TextureSlot TextureBaseColor
        {
            get
            {
                m_parent.GetMaterialTexture(TextureType.BaseColor, 0, out var tex);

                return tex;
            }
            set
            {
                if(value is {TextureIndex: 0, TextureType: TextureType.BaseColor})
                    m_parent.AddMaterialTexture(value);
            }
        }

        /// <summary>
        /// Gets if the material has a normal map texture in the first texture index.
        /// </summary>
        public bool HasTextureNormalCamera => m_parent.HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.NormalCamera, 0);

        /// <summary>
        /// Gets or sets the normal map texture properties in the first texture index.
        /// </summary>
        public TextureSlot TextureNormalCamera
        {
            get
            {
                m_parent.GetMaterialTexture(TextureType.NormalCamera, 0, out var tex);

                return tex;
            }
            set
            {
                if(value is {TextureIndex: 0, TextureType: TextureType.NormalCamera})
                    m_parent.AddMaterialTexture(value);
            }
        }

        /// <summary>
        /// Gets if the material has an emission color map texture in the first texture index.
        /// </summary>
        public bool HasTextureEmissionColor => m_parent.HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.EmissionColor, 0);

        /// <summary>
        /// Gets or sets the emission color map texture properties in the first texture index.
        /// </summary>
        public TextureSlot TextureEmissionColor
        {
            get
            {
                m_parent.GetMaterialTexture(TextureType.EmissionColor, 0, out var tex);

                return tex;
            }
            set
            {
                if(value is {TextureIndex: 0, TextureType: TextureType.EmissionColor})
                    m_parent.AddMaterialTexture(value);
            }
        }

        /// <summary>
        /// Gets if the material has a metalness map texture in the first texture index.
        /// </summary>
        public bool HasTextureMetalness => m_parent.HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Metalness, 0);

        /// <summary>
        /// Gets or sets the metalness map texture properties in the first texture index.
        /// </summary>
        public TextureSlot TextureMetalness
        {
            get
            {
                m_parent.GetMaterialTexture(TextureType.Metalness, 0, out var tex);

                return tex;
            }
            set
            {
                if(value is {TextureIndex: 0, TextureType: TextureType.Metalness})
                    m_parent.AddMaterialTexture(value);
            }
        }

        /// <summary>
        /// Gets if the material has a roughness map texture in the first texture index.
        /// </summary>
        public bool HasTextureRoughness => m_parent.HasProperty(AiMatKeys.TEXTURE_BASE, TextureType.Roughness, 0);

        /// <summary>
        /// Gets or sets the roughness map texture properties in the first texture index.
        /// </summary>
        public TextureSlot TextureRoughness
        {
            get
            {
                m_parent.GetMaterialTexture(TextureType.Roughness, 0, out var tex);

                return tex;
            }
            set
            {
                if(value is {TextureIndex: 0, TextureType: TextureType.Roughness})
                    m_parent.AddMaterialTexture(value);
            }
        }

        /// <summary>
        /// Constructs new property group accessor.
        /// </summary>
        /// <param name="parent">Material</param>
        internal PBRMaterialProperties(Material parent)
        {
            m_parent = parent;
        }
    }

    /// <summary>
    /// Groups all the properties for shader sources in a single accessor.
    /// </summary>
    public sealed class ShaderMaterialProperties
    {
        private readonly Material m_parent;

        /// <summary>
        /// Gets if the material has a property for shader language type.
        /// </summary>
        public bool HasShaderLanguageType => m_parent.HasProperty(AiMatKeys.GLOBAL_SHADERLANG);

        /// <summary>
        /// Gets or sets what language (HLSL, GLSL, etc) any shader source code in this material is of.
        /// </summary>
        public string ShaderLanguageType
        {
            get => GetStringProperty(AiMatKeys.GLOBAL_SHADERLANG);
            set => SetStringProperty(AiMatKeys.GLOBAL_SHADERLANG, AiMatKeys.GLOBAL_SHADERLANG_BASE, value);
        }

        /// <summary>
        /// Gets if the material has a property for vertex shader source code.
        /// </summary>
        public bool HasVertexShader => m_parent.HasProperty(AiMatKeys.SHADER_VERTEX);

        /// <summary>
        /// Gets or sets vertex shader source code.
        /// </summary>
        public string VertexShader
        {
            get => GetStringProperty(AiMatKeys.SHADER_VERTEX);
            set => SetStringProperty(AiMatKeys.SHADER_VERTEX, AiMatKeys.SHADER_VERTEX_BASE, value);
        }

        /// <summary>
        /// Gets if the material has a property for fragment (pixel) shader source code.
        /// </summary>
        public bool HasFragmentShader => m_parent.HasProperty(AiMatKeys.SHADER_FRAGMENT);

        /// <summary>
        /// Gets or sets fragment (pixel) shader source code.
        /// </summary>
        public string FragmentShader
        {
            get => GetStringProperty(AiMatKeys.SHADER_FRAGMENT);
            set => SetStringProperty(AiMatKeys.SHADER_FRAGMENT, AiMatKeys.SHADER_FRAGMENT_BASE, value);
        }

        /// <summary>
        /// Gets if the material has a property for geometry shader source code.
        /// </summary>
        public bool HasGeometryShader => m_parent.HasProperty(AiMatKeys.SHADER_GEO);

        /// <summary>
        /// Gets or sets geometry shader source code.
        /// </summary>
        public string GeometryShader
        {
            get => GetStringProperty(AiMatKeys.SHADER_GEO);
            set => SetStringProperty(AiMatKeys.SHADER_GEO, AiMatKeys.SHADER_GEO_BASE, value);
        }

        /// <summary>
        /// Gets if the material has a property for tesselation shader source code.
        /// </summary>
        public bool HasTesselationShader => m_parent.HasProperty(AiMatKeys.SHADER_TESSELATION);

        /// <summary>
        /// Gets or sets tesselation shader source code.
        /// </summary>
        public string TesselationShader
        {
            get => GetStringProperty(AiMatKeys.SHADER_TESSELATION);
            set => SetStringProperty(AiMatKeys.SHADER_TESSELATION, AiMatKeys.SHADER_TESSELATION_BASE, value);
        }

        /// <summary>
        /// Gets if the material has a property for primitive (domain) shader source code.
        /// </summary>
        public bool HasPrimitiveShader => m_parent.HasProperty(AiMatKeys.SHADER_PRIMITIVE);

        /// <summary>
        /// Gets or sets primitive (domain) shader source code.
        /// </summary>
        public string PrimitiveShader
        {
            get => GetStringProperty(AiMatKeys.SHADER_PRIMITIVE);
            set => SetStringProperty(AiMatKeys.SHADER_PRIMITIVE, AiMatKeys.SHADER_PRIMITIVE_BASE, value);
        }

        /// <summary>
        /// Gets if the material has a property for compute shader source code.
        /// </summary>
        public bool HasComputeShader => m_parent.HasProperty(AiMatKeys.SHADER_COMPUTE);

        /// <summary>
        /// Gets or sets compute shader source code.
        /// </summary>
        public string ComputeShader
        {
            get => GetStringProperty(AiMatKeys.SHADER_COMPUTE);
            set => SetStringProperty(AiMatKeys.SHADER_COMPUTE, AiMatKeys.SHADER_COMPUTE_BASE, value);
        }

        /// <summary>
        /// Constructs new property group accessor.
        /// </summary>
        /// <param name="parent">Material</param>
        internal ShaderMaterialProperties(Material parent)
        {
            m_parent = parent;
        }

        private string GetStringProperty(string fullName)
        {
            var prop = m_parent.GetProperty(fullName);
            if(prop != null)
                return prop.GetStringValue();

            return string.Empty;
        }

        private void SetStringProperty(string fullName, string baseName, string value)
        {
            if(string.IsNullOrEmpty(value))
                value = string.Empty;

            var prop = m_parent.GetProperty(fullName);
            if(prop == null)
            {
                prop = new(baseName, value);
                m_parent.AddProperty(prop);
            }
            else
            {
                prop.SetStringValue(value);
            }
        }
    }

    #endregion
}