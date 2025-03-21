using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    [Serializable]
    public class ResourceConfig
    {
        public float autoGenerateDelay = 1;
    }

    [Serializable]
    public class AssetImportRule
    {
        public TextureImportConfig texture;
        public AtlasImportConfig atlas;
    }

    [Serializable]
    public class TextureImportConfig
    {
        public TextureImporterFormat textureFormat = TextureImporterFormat.ASTC_6x6;
        public TextureImporterType textureImporterType = TextureImporterType.Sprite;
        public SpriteImportMode spriteImportMode = SpriteImportMode.Multiple;
        public bool isReadable = false;
        public bool generateMipMaps = false;
        public TextureImporterAlphaSource alphaSource = TextureImporterAlphaSource.FromInput;
        public bool alphaIsTransparency = true;
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        public FilterMode filterMode = FilterMode.Bilinear;
        public int maxTextureSize = 4096;
        public bool checkSizeIsPowerOf2 = true;
    }

    [Serializable]
    public class AtlasImportConfig
    {
        public bool includeInBuild = true;
        public bool allowRotation = false;
        public bool tightPacking = false;
        public bool alphaDilation = false;
        public int padding = 4;

        public bool isReadable = false;
        public bool generateMipMaps = false;
        public bool sRGB = true;
        public FilterMode filterMode = FilterMode.Bilinear;

        public TextureImporterFormat textureFormat = TextureImporterFormat.ASTC_6x6;
        public int maxTextureSize = 4096;
    }

    [Serializable]
    public class ImportRuleOverwritePair
    {
        public string applyPath;
        public AssetImportRule config;
    }

    [Serializable]
    public class AssetImportConfig
    {
        public List<TextureImporterFormat> validTextureFormats = new List<TextureImporterFormat>();
        public AssetImportRule basicRule = new AssetImportRule();
        public List<string> applyPaths = new List<string>();
        public List<ImportRuleOverwritePair> overwriteRules = new List<ImportRuleOverwritePair>();

        public AssetImportRule GetAssetImportRule(string path)
        {
            foreach (var pair in overwriteRules)
            {
                if (pair.applyPath.Contains(path))
                {
                    return pair.config;
                }
            }
            return basicRule;
        }
    }

    [Serializable]
    public class UIAtlasConfig
    {
        /// <summary>
        /// 散图存放路径
        /// </summary>
        public string spritePath = "ResourcesRaw/Atlas";
        /// <summary>
        /// 图集存放路径
        /// </summary>
        public string atlasPath = "Resources/UIAtlas";
    }

    [Serializable]
    public class Utf8JsonCastTypePath
    {
        public string CastFormaterOutPath;
        public List<string> CastTypePath = new List<string>();
    }

    public class GameEditorConfig : ScriptableObject
    {
        private static GameEditorConfig _instance;

        [SerializeField]
        private ExcelPipeline.TableBuildData dataTableConfig = new ExcelPipeline.TableBuildData();
        [SerializeField]
        private string[] additionalDataTableEnumField = new string[0];
        [SerializeField]
        private ExcelPipeline.TableBuildData textTableConfig = new ExcelPipeline.TableBuildData();
        [SerializeField]
        private ResourceConfig resourceConfig = new ResourceConfig();
        [SerializeField]
        private AssetImportConfig assetImportConfig = new AssetImportConfig();
        [SerializeField]
        private UIAtlasConfig atlasConfig = new UIAtlasConfig();
        [SerializeField]
        private Utf8JsonCastTypePath utf8JsonCastTypePathConfig = new Utf8JsonCastTypePath();

        private static GameEditorConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Check();
                }
                return _instance;
            }
        }

        public static ExcelPipeline.TableBuildData DataTableConfig { get { return Instance.dataTableConfig; } }

        public static string[] AdditionalDataTableEnumField { get { return Instance.additionalDataTableEnumField; } }

        public static ExcelPipeline.TableBuildData TextTableConfig { get { return Instance.textTableConfig; } }

        public static ResourceConfig ResourceConfig { get { return Instance.resourceConfig; } }

        public static AssetImportConfig AssetImportConfig { get { return Instance.assetImportConfig; } }

        public static UIAtlasConfig AtlasConfig { get { return Instance.atlasConfig; } }

        public static Utf8JsonCastTypePath Utf8JsonCastTypePath { get { return Instance.utf8JsonCastTypePathConfig; } }

        public static GameEditorConfig Check()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(GameEditorConfig).Name);
            if (guids.Length == 0)
            {
                //上面检查资源是否存在不一定靠谱，因为在Unity未完全导入完成前，有可能文件存在，但资源不存在
                //检查文件是否存在
                if (File.Exists("Assets/GameFramework/GameEditorConfig.asset"))
                {
                    Debug.LogError("GameEditorConfig存在，但是资源尚未导入完成，请在资源全部导入完成后重新执行刚才的操作");
                    return null;
                }
                else
                {
                    return Create();
                }
            }
            else if (guids.Length == 1)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GameEditorConfig>(path);
            }
            else
            {
                Debug.LogError("工程中存在多份 GameEditorConfig，请检查并删除多余的，路径如下：");
                foreach (var guid in guids)
                {
                    Debug.LogError(AssetDatabase.GUIDToAssetPath(guid));
                }
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GameEditorConfig>(path);
            }
        }

        private static GameEditorConfig Create()
        {
            var config = ScriptableObject.CreateInstance<GameEditorConfig>();
            config.NewDefaultDataTableConfig();
            config.NewDefaultTextTableConfig();
            config.NewDefaultAssetImportConfig();
            config.NewDefaultUtf8JsonCastTypePathConfig();
            AssetDatabase.CreateAsset(config, "Assets/GameFramework/GameEditorConfig.asset");
            return config;
        }

        private void NewDefaultDataTableConfig()
        {
            dataTableConfig.sourceDirectory = "../../datatable";
            dataTableConfig.exportResourceDirectory = "Assets/Resources/TableData";
            dataTableConfig.exportScriptDirectory = "Assets/Scripts/TableData";
        }

        private void NewDefaultTextTableConfig()
        {
            textTableConfig.sourceDirectory = "../../localization";
            textTableConfig.exportResourceDirectory = "Assets/Resources/TextData";
            textTableConfig.exportScriptDirectory = "Assets/Scripts/TextData";
        }

        private void NewDefaultAssetImportConfig()
        {
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ETC2_RGBA8);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ETC2_RGBA8Crunched);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ASTC_4x4);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ASTC_5x5);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ASTC_6x6);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ASTC_8x8);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ASTC_10x10);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.ASTC_12x12);
            assetImportConfig.validTextureFormats.Add(TextureImporterFormat.RGBA32);

            assetImportConfig.applyPaths.Add("Resources/Atlas");
            assetImportConfig.applyPaths.Add("ResourcesRaw/VFX");
            assetImportConfig.applyPaths.Add("ResourcesRaw/Spine");
            assetImportConfig.applyPaths.Add("ResourcesRaw/Texture");

            assetImportConfig.overwriteRules.Add(new ImportRuleOverwritePair() { applyPath = "ResourcesRaw/VFX", config = new AssetImportRule() { texture = new TextureImportConfig() { maxTextureSize = 256 } } });
            assetImportConfig.overwriteRules.Add(new ImportRuleOverwritePair() { applyPath = "ResourcesRaw/Spine", config = new AssetImportRule() { texture = new TextureImportConfig() { alphaIsTransparency = false } } });
        }

        public static bool IsAdditionalEnum(string text)
        {
            return Array.IndexOf(AdditionalDataTableEnumField, text) >= 0;
        }

        private void NewDefaultUtf8JsonCastTypePathConfig()
        {
            utf8JsonCastTypePathConfig.CastFormaterOutPath = "Scripts/Game/Utf8JsonFormatterResolver/GeneratedResolver.cs";
            utf8JsonCastTypePathConfig.CastTypePath.Add("Scripts/Game/DataTable");
            utf8JsonCastTypePathConfig.CastTypePath.Add("Scripts/Game/TextTable");
            utf8JsonCastTypePathConfig.CastTypePath.Add("Scripts/Game/Utf8JsonSource");
            utf8JsonCastTypePathConfig.CastTypePath.Add("Scripts/GameFramework/ExcelPipeline/Runtime/CustomDataType");
        }
    }
}