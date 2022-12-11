//
// Color Gradient Texture Importer for Unity. Copyright (c) 2020-2022 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityColorGradientTextureImportPipeline
//
#pragma warning disable IDE1006, IDE0017
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

namespace Oddworm.EditorFramework
{
    /// <summary>
    /// The ColorGradientTextureImporter is able to export itself to an external asset.
    /// In order for Unity to show that generated file, we check if Unity imported a color gradient texture asset
    /// and then we trigger an asset database refresh to make the generated external asset visible.
    /// </summary>
    class ColorGradientTextureAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var refresh = false;

            foreach (var assetPath in importedAssets)
            {
                if (assetPath.EndsWith(ColorGradientTextureImporter.kFileExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    refresh = true;
                    break;
                }
            }

            if (refresh)
            {
                EditorApplication.delayCall += delegate ()
                {
                    AssetDatabase.Refresh();
                };
            }
        }
    }
}
