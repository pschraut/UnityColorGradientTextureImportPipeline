﻿//
// Color Gradient Texture Importer for Unity. Copyright (c) 2020-2022 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityColorGradientTextureImportPipeline
//
#pragma warning disable IDE1006, IDE0017
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Oddworm.EditorFramework
{
    [HelpURL("https://github.com/pschraut/UnityColorGradientTextureImportPipeline")]
    [CanEditMultipleObjects]
    [ScriptedImporter(version: 1, ext: ColorGradientTextureImporter.kFileExtension, importQueueOffset: 100)]
    public class ColorGradientTextureImporter : ScriptedImporter
    {
        [Tooltip("One or multiple gradients to encode as texture. If no gradients are specified, a placeholder RGB gradient is generated.")]
        [SerializeField]
        List<Gradient> m_Gradients = new List<Gradient>();

        [Tooltip("The gradient orientation.")]
        [SerializeField]
        Orientation m_Orientation = Orientation.Horizontal;

        [Tooltip("The gradient width.\n\nThe option depends on the orientation, it's the width in Horizontal orientation, otherwise the height.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_Steps")]
        [SerializeField]
        int m_Width = 256;

        [Tooltip("The height of a single gradient.\n\nThe option depends on the orientation, it's the height in Horizontal orientation, otherwise the width.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_Thickness")]
        [SerializeField]
        int m_Height = 4;

        [Tooltip("Reverse or flip the gradient.")]
        [SerializeField]
        bool m_Reverse = false;

        [Space]
        [Tooltip("Selects how the Texture behaves when tiled.")]
        [SerializeField]
        TextureWrapMode m_WrapMode = TextureWrapMode.Clamp;

        [Tooltip("Selects how the Texture is filtered when it gets stretched by 3D transformations.")]
        [SerializeField]
        FilterMode m_FilterMode = FilterMode.Bilinear;

        [Tooltip("Increases Texture quality when viewing the texture at a steep angle.\n0 = Disabled for all textures\n1 = Enabled for all textures in Quality Settings\n2..16 = Anisotropic filtering level")]
        [Range(0, 16)]
        [SerializeField]
        int m_AnisoLevel = 1;

        [Space]
        [Tooltip("(optional) If an output texture is specified, the generated gradient is written to the specified texture file as well.")]
        [SerializeField]
        Texture2D m_OutputTexture = null;

        [HideInInspector]
        [SerializeField] string m_SelfGuid = ""; // the asset guid of this importer

        /// <summary>
        /// The gradient orientation in the generated texture.
        /// </summary>
        public enum Orientation
        {
            Horizontal = 0,
            Vertical = 1
        }

        /// <summary>
        /// Texture coordinate wrapping mode.
        /// </summary>
        public TextureWrapMode wrapMode
        {
            get => m_WrapMode;
            set => m_WrapMode = value;
        }

        /// <summary>
        /// Filtering mode of the texture.
        /// </summary>
        public FilterMode filterMode
        {
            get => m_FilterMode;
            set => m_FilterMode = value;
        }

        /// <summary>
        /// Anisotropic filtering level of the texture.
        /// </summary>
        public int anisoLevel
        {
            get => m_AnisoLevel;
            set => m_AnisoLevel = value;
        }

        /// <summary>
        /// Gets or sets the gradients.
        /// </summary>
        public Gradient[] gradients
        {
            get => m_Gradients.ToArray();
            set => m_Gradients = new List<Gradient>(value);
        }

        /// <summary>
        /// Gets or sets the output texture.
        /// </summary>
        public Texture2D outputTexture
        {
            get => m_OutputTexture;
            set => m_OutputTexture = value;
        }

        public const string kFileExtension = "colorgradienttexture";

        void OnValidate()
        {
            m_Width = Mathf.Clamp(m_Width, 2, 1024 * 16);
            m_Height = Mathf.Clamp(m_Height, 1, 1024 * 16);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Texture2D texture = null;

            if (m_Orientation == Orientation.Horizontal)
                texture = CreateHorizontal(ctx);

            if (m_Orientation == Orientation.Vertical)
                texture = CreateVertical(ctx);

            ctx.AddObjectToAsset("MainAsset", texture);
            ctx.SetMainObject(texture);

            DetectDuplicateAsset(ctx);
            ExportToExternal(ctx, texture);
        }

        // If the asset gets duplicated, reset some fields that are unique to THIS asset only.
        void DetectDuplicateAsset(AssetImportContext ctx)
        {
            var guid = AssetDatabase.AssetPathToGUID(ctx.assetPath);

            // If the m_SelfGuid field contains a value already, but it's different from this importer guid,
            // it means the asset got duplicated. In this case, reset some fields.
            if (!string.IsNullOrEmpty(m_SelfGuid) && !string.Equals(guid, m_SelfGuid, System.StringComparison.OrdinalIgnoreCase))
            {
                m_OutputTexture = null;
            }

            m_SelfGuid = guid;
        }

        Texture2D CreateHorizontal(AssetImportContext ctx)
        {
            var gradients = GetGradientsSafe();
            var width = m_Width;
            var height = m_Height * gradients.Count;
            var texture = CreateTexture(width, height);
            var pixels = texture.GetPixels32();
            var line = 0;

            for (var n = gradients.Count - 1; n >= 0; --n)
            {
                var gradient = gradients[n];

                for (var x = 0; x < width; ++x)
                {
                    var time = x / (width - 1.0f);
                    if (m_Reverse)
                        time = 1 - time;

                    var value = (Color32)gradient.Evaluate(time);

                    for (var y = line; y < line + m_Height; ++y)
                        pixels[x + y * width] = value;
                }

                line += m_Height;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }

        Texture2D CreateVertical(AssetImportContext ctx)
        {
            var gradients = GetGradientsSafe();
            var width = m_Height * gradients.Count;
            var height = m_Width;
            var texture = CreateTexture(width, height);
            var pixels = texture.GetPixels32();
            var line = 0;

            for (var n = 0; n < gradients.Count; ++n)
            {
                var gradient = gradients[n];

                for (var y = 0; y < height; ++y)
                {
                    var time = y / (height - 1.0f);
                    if (!m_Reverse)
                        time = 1 - time;

                    var value = (Color32)gradient.Evaluate(time);

                    for (var x = line; x < line + m_Height; ++x)
                        pixels[x + y * width] = value;
                }

                line += m_Height;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }

        void ExportToExternal(AssetImportContext ctx, Texture2D texture)
        {
            if (m_OutputTexture == null)
                return;

            var outputPath = AssetDatabase.GetAssetPath(m_OutputTexture);
            if (string.IsNullOrEmpty(outputPath))
            {
                ctx.LogImportError("Cannot export color gradient texture, because the output texture path could not be found.", ctx.mainObject);
                return;
            }

            var type = Path.GetExtension(outputPath).ToLower();
            switch (type)
            {
                case ".png": File.WriteAllBytes(outputPath, texture.EncodeToPNG()); break;
                case ".tga": File.WriteAllBytes(outputPath, texture.EncodeToTGA()); break;
                //case ".jpg": File.WriteAllBytes(outputPath, texture.EncodeToJPG()); break;
                //case ".exr": File.WriteAllBytes(outputPath, texture.EncodeToEXR()); break;
                default:
                    ctx.LogImportError($"Cannot export as '{type}' ({outputPath}), .png and .tga exports are supported only.", ctx.mainObject);
                    break;
            }
        }

        Texture2D CreateTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
            texture.filterMode = m_FilterMode;
            texture.wrapMode = m_WrapMode;
            return texture;
        }

        /// <summary>
        /// Gets all gradients to encode in the texture. If no gradient has been specified,
        /// it adds a placeholder one, to avoid generating an 0 sized texture.
        /// </summary>
        List<Gradient> GetGradientsSafe()
        {
            var gradients = new List<Gradient>(m_Gradients);
            if (gradients.Count == 0)
            {
                var g = new Gradient();
                g.mode = GradientMode.Blend;
                g.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(1, 0, 0), 0.0f),
                        new GradientColorKey(new Color(0, 1, 0), 0.5f),
                        new GradientColorKey(new Color(0, 0, 1), 1.0f),
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1, 0.0f),
                        new GradientAlphaKey(1, 0.5f),
                        new GradientAlphaKey(1, 1.0f),
                    });

                gradients.Add(g);
            }

            return gradients;
        }

        [MenuItem("Assets/Create/Color Gradient Texture", priority = 325)]
        static void RegisterMenuItem()
        {
            // https://forum.unity.com/threads/how-to-implement-create-new-asset.759662/
            string directoryPath = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                directoryPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(directoryPath) && File.Exists(directoryPath))
                {
                    directoryPath = Path.GetDirectoryName(directoryPath);
                    break;
                }
            }
            directoryPath = directoryPath.Replace("\\", "/");
            if (directoryPath.Length > 0 && directoryPath[directoryPath.Length - 1] != '/')
                directoryPath += "/";
            if (string.IsNullOrEmpty(directoryPath))
                directoryPath = "Assets/";

            var fileName = $"New Color Gradient Texture.{kFileExtension}";
            directoryPath = AssetDatabase.GenerateUniqueAssetPath(directoryPath + fileName);

            ProjectWindowUtil.CreateAssetWithContent(directoryPath, "This file represents a ColorGradient Texture asset for Unity.\nYou need the 'ColorGradient Texture Import Pipeline' package available at https://github.com/pschraut/UnityColorGradientTextureImportPipeline to properly import this file in Unity.");
        }
    }
}
