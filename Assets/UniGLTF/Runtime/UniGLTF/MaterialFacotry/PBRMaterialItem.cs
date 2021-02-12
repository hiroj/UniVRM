﻿using System.Threading.Tasks;
using UnityEngine;

namespace UniGLTF
{
    /// StandardShader variables
    ///
    /// _Color
    /// _MainTex
    /// _Cutoff
    /// _Glossiness
    /// _Metallic
    /// _MetallicGlossMap
    /// _BumpScale
    /// _BumpMap
    /// _Parallax
    /// _ParallaxMap
    /// _OcclusionStrength
    /// _OcclusionMap
    /// _EmissionColor
    /// _EmissionMap
    /// _DetailMask
    /// _DetailAlbedoMap
    /// _DetailNormalMapScale
    /// _DetailNormalMap
    /// _UVSec
    /// _EmissionScaleUI
    /// _EmissionColorUI
    /// _Mode
    /// _SrcBlend
    /// _DstBlend
    /// _ZWrite
    public static class PBRMaterialItem
    {
        public const string ShaderName = "Standard";

        private enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent
        }

        public static async Task<Material> CreateAsync(int i, glTFMaterial src, GetTextureAsyncFunc getTexture)
        {
            if (getTexture == null)
            {
                getTexture = _ => Task.FromResult<Texture2D>(null);
            }

            var material = MaterialItemBase.CreateMaterial(i, src, ShaderName);

            // PBR material
            if (src != null)
            {
                if (src.pbrMetallicRoughness != null)
                {
                    if (src.pbrMetallicRoughness.baseColorFactor != null && src.pbrMetallicRoughness.baseColorFactor.Length == 4)
                    {
                        var color = src.pbrMetallicRoughness.baseColorFactor;
                        material.color = (new Color(color[0], color[1], color[2], color[3])).gamma;
                    }

                    if (src.pbrMetallicRoughness.baseColorTexture != null && src.pbrMetallicRoughness.baseColorTexture.index != -1)
                    {
                        material.mainTexture = await getTexture(GetTextureParam.Create(src.pbrMetallicRoughness.baseColorTexture.index));

                        // Texture Offset and Scale
                        MaterialItemBase.SetTextureOffsetAndScale(material, src.pbrMetallicRoughness.baseColorTexture, "_MainTex");
                    }

                    if (src.pbrMetallicRoughness.metallicRoughnessTexture != null && src.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                    {
                        material.EnableKeyword("_METALLICGLOSSMAP");

                        var texture = await getTexture(GetTextureParam.CreateMetallic(
                            src.pbrMetallicRoughness.metallicRoughnessTexture.index,
                            src.pbrMetallicRoughness.metallicFactor));
                        if (texture != null)
                        {
                            material.SetTexture(GetTextureParam.METALLIC_GLOSS_PROP, texture);
                        }

                        material.SetFloat("_Metallic", 1.0f);
                        // Set 1.0f as hard-coded. See: https://github.com/dwango/UniVRM/issues/212.
                        material.SetFloat("_GlossMapScale", 1.0f);

                        // Texture Offset and Scale
                        MaterialItemBase.SetTextureOffsetAndScale(material, src.pbrMetallicRoughness.metallicRoughnessTexture, "_MetallicGlossMap");
                    }
                    else
                    {
                        material.SetFloat("_Metallic", src.pbrMetallicRoughness.metallicFactor);
                        material.SetFloat("_Glossiness", 1.0f - src.pbrMetallicRoughness.roughnessFactor);
                    }
                }

                if (src.normalTexture != null && src.normalTexture.index != -1)
                {
                    material.EnableKeyword("_NORMALMAP");
                    var texture = await getTexture(GetTextureParam.CreateNormal(src.normalTexture.index));
                    if (texture != null)
                    {
                        material.SetTexture(GetTextureParam.NORMAL_PROP, texture);
                        material.SetFloat("_BumpScale", src.normalTexture.scale);
                    }

                    // Texture Offset and Scale
                    MaterialItemBase.SetTextureOffsetAndScale(material, src.normalTexture, "_BumpMap");
                }

                if (src.occlusionTexture != null && src.occlusionTexture.index != -1)
                {
                    var texture = await getTexture(GetTextureParam.CreateOcclusion(src.occlusionTexture.index));
                    if (texture != null)
                    {
                        material.SetTexture(GetTextureParam.OCCLUSION_PROP, texture);
                        material.SetFloat("_OcclusionStrength", src.occlusionTexture.strength);
                    }

                    // Texture Offset and Scale
                    MaterialItemBase.SetTextureOffsetAndScale(material, src.occlusionTexture, "_OcclusionMap");
                }

                if (src.emissiveFactor != null
                    || (src.emissiveTexture != null && src.emissiveTexture.index != -1))
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                    if (src.emissiveFactor != null && src.emissiveFactor.Length == 3)
                    {
                        material.SetColor("_EmissionColor", new Color(src.emissiveFactor[0], src.emissiveFactor[1], src.emissiveFactor[2]));
                    }

                    if (src.emissiveTexture != null && src.emissiveTexture.index != -1)
                    {
                        var texture = await getTexture(GetTextureParam.Create(src.emissiveTexture.index));
                        if (texture != null)
                        {
                            material.SetTexture("_EmissionMap", texture);
                        }

                        // Texture Offset and Scale
                        MaterialItemBase.SetTextureOffsetAndScale(material, src.emissiveTexture, "_EmissionMap");
                    }
                }

                BlendMode blendMode = BlendMode.Opaque;
                // https://forum.unity.com/threads/standard-material-shader-ignoring-setfloat-property-_mode.344557/#post-2229980
                switch (src.alphaMode)
                {
                    case "BLEND":
                        blendMode = BlendMode.Fade;
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                        break;

                    case "MASK":
                        blendMode = BlendMode.Cutout;
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.SetFloat("_Cutoff", src.alphaCutoff);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 2450;

                        break;

                    default: // OPAQUE
                        blendMode = BlendMode.Opaque;
                        material.SetOverrideTag("RenderType", "");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;
                        break;
                }

                material.SetFloat("_Mode", (float)blendMode);
            }

            return material;
        }
    }
}
