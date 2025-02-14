using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace GameFramework
{
    public class GameAssetPostProcess : AssetPostprocessor
    {
        static string[] validPlatforms = new string[] { UnityEditor.Build.NamedBuildTarget.Android.TargetName,
                                                        UnityEditor.Build.NamedBuildTarget.iOS.TargetName };
        static HashSet<int> validSizeSet = new HashSet<int>() { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        /// <summary>
        /// 图片导入前设置
        /// </summary>
        private void OnPreprocessTexture()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            foreach (var applyPath in GameEditorConfig.AssetImportConfig.applyPaths)
            {
                if (importer.assetPath.Contains(applyPath))
                {
                    AssetImportRule rule = GameEditorConfig.AssetImportConfig.GetAssetImportRule(applyPath);
                    importer.textureType = rule.texture.textureImporterType;
                    importer.spriteImportMode = rule.texture.spriteImportMode;
                    importer.isReadable = rule.texture.isReadable;
                    importer.mipmapEnabled = rule.texture.generateMipMaps;
                    importer.alphaSource = rule.texture.alphaSource;
                    importer.alphaIsTransparency = rule.texture.alphaIsTransparency;
                    importer.wrapMode = rule.texture.wrapMode;
                    importer.filterMode = rule.texture.filterMode;

                    int maxTextureSize;
                    TextureImporterFormat format;

                    foreach (string platform in validPlatforms)
                    {
                        bool hasSetting = importer.GetPlatformTextureSettings(platform, out maxTextureSize, out format);
                        bool changeSetting = false;
                        if (hasSetting)
                        {
                            bool isFormatInvalid = GameEditorConfig.AssetImportConfig.validTextureFormats.IndexOf(format) < 0;
                            if (isFormatInvalid)
                            {
                                changeSetting = true;
                            }
                        }
                        else
                        {
                            changeSetting = true;
                        }

                        if (maxTextureSize != rule.texture.maxTextureSize)
                        {
                            changeSetting = true;
                            maxTextureSize = rule.texture.maxTextureSize;
                        }

                        if (changeSetting)
                        {
                            TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings
                            {
                                name = platform,
                                overridden = true,
                                maxTextureSize = maxTextureSize,
                                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                                format = rule.texture.textureFormat,
                                compressionQuality = 100,
                            };

                            importer.SetPlatformTextureSettings(platformSetting);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 资源导入前设置，因为Atlas图集没有专门的通知方法，只能先在这个里面进行筛选
        /// </summary>
        void OnPreprocessAsset()
        {
            if (assetImporter is SpriteAtlasImporter importer)
            {
                foreach (var applyPath in GameEditorConfig.AssetImportConfig.applyPaths)
                {
                    if (importer.assetPath.Contains(applyPath))
                    {
                        AssetImportRule rule = GameEditorConfig.AssetImportConfig.GetAssetImportRule(applyPath);

                        var packSettings = new SpriteAtlasPackingSettings();
                        packSettings.enableRotation = rule.atlas.allowRotation;
                        packSettings.enableTightPacking = rule.atlas.tightPacking;
                        packSettings.enableAlphaDilation = rule.atlas.alphaDilation;
                        packSettings.padding = rule.atlas.padding;

                        importer.packingSettings = packSettings;
                        var textureSettings = new SpriteAtlasTextureSettings();
                        textureSettings.readable = rule.atlas.isReadable;
                        textureSettings.generateMipMaps = rule.atlas.generateMipMaps;
                        textureSettings.sRGB = rule.atlas.sRGB;
                        textureSettings.filterMode = rule.atlas.filterMode;
                        importer.textureSettings = textureSettings;

                        foreach (string platform in validPlatforms)
                        {
                            TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings
                            {
                                name = platform,
                                overridden = true,
                                maxTextureSize = rule.atlas.maxTextureSize,
                                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                                format = rule.atlas.textureFormat,
                                compressionQuality = 100,
                            };
                            importer.SetPlatformSettings(platformSetting);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 图片导入后设置
        /// </summary>
        /// <param name="texture"></param>
        void OnPostprocessTexture(Texture2D texture)
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            foreach(var applyPath in GameEditorConfig.AssetImportConfig.applyPaths)
            {
                if (importer.assetPath.Contains(applyPath))
                {
                    AssetImportRule rule = GameEditorConfig.AssetImportConfig.GetAssetImportRule(applyPath);   
                    if (rule.texture.checkSizeIsPowerOf2)
                    {
                        if (validSizeSet.Contains(texture.width) == false || validSizeSet.Contains(texture.height) == false)
                        {
                            UnityEngine.Debug.LogError($"Texture {importer.assetPath} 的尺寸 {texture.width}x{texture.height} 不是2的幂", texture);
                        }
                    }
                }
            }
        }

    }
}
