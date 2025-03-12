// This file just contains a bunch of data structs and classes used in animation baking

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AnimCooker
{
    public enum VtxAnimTexType : byte { Pos, PosAndNml, PosNmlAndTan }

    public struct MeshStats
    {
        public int VertexCount;
        public int BoneMeshCount;
        public string MakeString() { return $"VertexCount: {VertexCount}, BoneMeshCount: {BoneMeshCount}"; }
    }

    public struct ClipStats
    {
        public float SmallestFps;
        public float TotalClipLength;
        public int TotalClipCount;
        public string MakeString() { return $"TotalClipCount: {TotalClipCount}, SmallestFps: {SmallestFps}, TotalClipLength: {TotalClipLength}"; }
    }

    public struct FrameStats
    {
        public int FrameCount;
        public int PointCount;
        public string MakeString() { return $"FrameCount: {FrameCount}, PointCount: {PointCount}"; }
    }

    // a buffer of these structs is passed to the compute shader
    // IMPORTANT - note that this must correspond EXACTLY with PixelInfo in VtxAnimTextureGen.compute.
    // (the sizes must be the same for allocation purposes)
    public struct PixelInfo
    {
        public float4 Position;
        public float4 Normal;
        public float4 Tangent;
    }

    // holds the result of a save-cooked-files operation
    public class SaveSkinResult
    {
        public string MeshPath;
        public string MatPath;
        public string PosTexPath;
        public string NmlTexPath;
        public string TanTexPath;
    }

    public class SaveResult
    {
        public string Message;
        public string PlayShaderName;
        public List<SaveSkinResult> Skins = new List<SaveSkinResult>();
    }

    public class SkinResult
    {
        public SkinnedMeshRenderer SkinRenderer; // the skinned mesh renderer used for this skin
        public Mesh FixedMesh; // the fixed mesh for this skin
        public RenderTexture PositionRenderTex; // this skin's position render texture
        public RenderTexture NormalRenderTex; // this skin's normal render texture
        public RenderTexture TangentRenderTex; // this skin's tangent render texture
        public List<SkinClipEntry> SkinClips = new List<SkinClipEntry>();
        public float Interval; // the sample interval used for this skin (1/fr) (all clips are the same)
        public byte FrameRate; // the frame rate used for this skin (1/interval) (all clips are the same)
        public byte Pow2; // the power of two for width for this skin (all clips are the same)
        public PointRange VertStats; // each skin has its own min/max values (used in compression)
        public int VertexCount; // the number of vertexes the model has
    }

    // holds the result of a skin bake operation
    public class BakeResult
    {
        public bool EnableR11G10B11 = true;
        public string Message = "";
        public List<SkinResult> SkinResults = new List<SkinResult>();
        public GameObject Originalprefab;
    }

    // Holds the complete list of all clips as well as options for each one
    // such as an overridable name and a bit to enable/disable the clip during bake.
    // This struct must be marked as serialiable because it resides in BakeOptions.
    [System.Serializable]
    public class ClipOption
    {
        public string Name = "";
        public bool IsEnabled = true;
        public AnimationClip Clip = null;

        public ClipOption(string name, bool enable, AnimationClip clip)
        {
            Name = name;
            IsEnabled = enable;
            Clip = clip;
        }

        public void SetEnable(bool e) { IsEnabled = e; }
        public void SetName(string str) { Name = str; }
        public void SetClip(AnimationClip c) { Clip = c; }
    }

    // Options that get passed to the bake function.
    // It must be marked as serializable so it can be used as a serializable member of a GUI.
    [System.Serializable]
    public class BakeOptions
    {
        public List<SkinOption> SkinOpts = new List<SkinOption>();

        // This holds options and clip information for each clip.
        // Normally, I would have used a List<ClipOption>, but
        // the unity editor won't serialize a List<> after a domain reload
        // (like when you change code and go back to the editor)
        public List<ClipOption> ClipOpts = new List<ClipOption>();

        public bool EnableBoneAdjust = false;
        public bool IgnoreBoneMeshes = false;
        public bool EnableResetPositionBeforeBake = false;
        public bool EnableResetRotationBeforeBake = false;
        public bool EnableResetScaleBeforeBake = false;
        public bool EnableEnumDeclaration = true;
        public bool EnableLogFile = true;
        public bool EnablePostDomainReload = true;
        public bool EnableCopyTexturesToOutput = true;
        public bool EnableR11G10B11 = true;

        public string MakeReport()
        {
            string ret = "";
            ret += EnableEnumDeclaration ? "\nEnum Declaration: enabled" : "\nEnum Declaration: disabled";
            ret += EnableResetRotationBeforeBake ? "\nReset Rotation Before Bake: enabled" : "\nReset Rotation Before Bake: disabled";
            ret += EnableResetPositionBeforeBake ? "\nReset Position Before Bake: enabled" : "\nReset Position Before Bake: disabled";
            ret += EnableResetScaleBeforeBake ? "\nReset Scale Before Bake: enabled" : "\nReset Scale Before Bake: disabled";
            ret += IgnoreBoneMeshes ? "\nIgnore Bone Meshes: enabled" : "\nIgnore Bone Meshes: disabled";
            ret += EnableBoneAdjust ? "\nBone Adjust: enabled" : "\nBone Adjust: disabled\n";
            ret += EnablePostDomainReload ? "\nPost Domain Reload: enabled" : "\nPost Domain Reload: disabled\n";
            ret += EnableR11G10B11 ? "\nR10G11B11: enabled" : "\nR10G11B11: disabled\n";
            return ret;
        }

        public ClipStats CalculateSelectedClipStats()
        {
            ClipStats stats;
            stats.SmallestFps = int.MaxValue;
            stats.TotalClipLength = 0f;
            stats.TotalClipCount = 0;
            for (int i = 0; i < ClipOpts.Count; i++) {
                ClipOption clipOpt = ClipOpts[i];
                if (clipOpt.IsEnabled) {
                    if (clipOpt.Clip == null) { UnityEngine.Debug.Log($"clip {i} is null"); }
                    if (clipOpt.Clip.frameRate < stats.SmallestFps) { stats.SmallestFps = clipOpt.Clip.frameRate; }
                    stats.TotalClipLength += clipOpt.Clip.length;
                    stats.TotalClipCount++;
                }
            }
            return stats;
        }
    }

    // struct used in calculating possible frame-rates
    [System.Serializable]
    public class TexSampleFormat
    {
        public byte FrameRate = 0;
        public int Height = 0; // texture height
        public int Width = 0; // texture width

        public TexSampleFormat() { }

        public TexSampleFormat(byte fr, int w, int h)
        {
            FrameRate = fr;
            Height = h;
            Width = w;
        }

        public int CalculateBytes(VtxAnimTexType type)
        {
            int singleTexSize = Height * Width * 8;
            switch (type) {
                case VtxAnimTexType.PosAndNml: return singleTexSize * 2;
                case VtxAnimTexType.PosNmlAndTan: return singleTexSize * 3;
            }
            return singleTexSize; // default - position only
        }

        public string MakeString(VtxAnimTexType type)
        {
            return $"{FrameRate}fps, {Width}x{Height}, {(int)((float)CalculateBytes(type) * 0.001f)}KB";
        }

        public TexSampleFormat Clone()
        {
            return new TexSampleFormat(FrameRate, Width, Height);
        }
    }

    // keeps track of min and max position values
    // (mainly used in packing/unpacking rgba values)
    public struct PointRange
    {
        public float MinPos;
        public float MaxPos;
        public float MinNml;
        public float MaxNml;
        public float MinTan;
        public float MaxTan;
        public static PointRange Default => new PointRange { MinPos = float.MaxValue, MaxPos = float.MinValue, MinNml = float.MaxValue, MaxNml = float.MinValue, MinTan = float.MaxValue, MaxTan = float.MinValue };

        public void UpdateNml(float3 nml) { UpdateVal(nml, ref MinNml, ref MaxNml); }
        public void UpdateTan(float4 tan) { UpdateVal(tan.xyz, ref MinTan, ref MaxTan); }
        public void UpdatePos(float3 pos) { UpdateVal(pos, ref MinPos, ref MaxPos); }

        void UpdateVal(float3 val, ref float min, ref float max)
        {
            if (val.x < min) { min = val.x; }
            if (val.y < min) { min = val.y; }
            if (val.z < min) { min = val.z; }
            if (val.x > max) { max = val.x; }
            if (val.y > max) { max = val.y; }
            if (val.z > max) { max = val.z; }
        }
    }

    public struct BoneInfo
    {
        public float Scale;
        public Vector3 Offset;
    }

    public class SkinInfo
    {
        public SkinnedMeshRenderer Skin = null;
        //public int SkinIndex = -1;
        public List<MeshFilter> BoneMeshes = null;
    }

    public enum ReselectMode : byte { None, Fps, Dimension }

    public class SkinOption
    {
        // ############################# PRIVATE ############################# //
        // holds the current skin index (must be set in the constructor)
        int m_index;

        // holds all possible formats for this
        // GUI - array that holds all the possible frame-rate/texture size combinations
        List<TexSampleFormat> m_formats = new List<TexSampleFormat>();

        // cached strings for m_formats
        List<string> m_formatStrings = new List<string>();

        // ############################# PUBLIC ############################# //

        // currently selected vertex animation texture type to output
        public VtxAnimTexType TexType = VtxAnimTexType.PosAndNml;

        // GUI - currently selected index in the lists
        public int SelectedFmtIdx = 0;

        // access the format strings by array (read-only) (useful for combat box)
        public string[] FormatStringsArray { get { return m_formatStrings.ToArray(); } }

        // access the skin index (read-only)
        public int SkinIndex { get { return m_index; } }

        // constructor
        public SkinOption(int index) { m_index = index; }

        // append the specified format.
        public void AddFormat(TexSampleFormat format)
        {
            m_formats.Add(format);
            m_formatStrings.Add(format.MakeString(TexType));
        }

        public void SetFormats(List<TexSampleFormat> formats, ReselectMode mode)
        {
            if ((m_formats.Count > 0) && (mode != ReselectMode.None)) {
                // currently initialized and a mode is selected, so save old format and try to match it.
                int oldFrameRate = SelectedFormat.FrameRate;
                int oldWidth = SelectedFormat.Width;
                int oldHeight = SelectedFormat.Height;
                m_formats = formats;
                if (mode == ReselectMode.Dimension) {
                    SelectClosestFormat(oldWidth, oldHeight);
                } else if (mode == ReselectMode.Fps) {
                    SelectClosestFormat(oldFrameRate);
                }
            } else {
                m_formats = formats; // uninitialized
            }

            RefreshFormatStrings();
        }

        // access the currently selected format.
        public TexSampleFormat SelectedFormat
        {
            get { 
                return SelectedFmtIdx < m_formats.Count ? m_formats[SelectedFmtIdx] : default;
            }
        }

        // sets the current selected index to the closest format to the specified frame rate
        // if the specified frame rate is invalid, then the first available format is selected.
        // m_formats must be valid before calling this function.
        public void SelectClosestFormat(int frameRate)
        {
            SelectedFmtIdx = 0;
            if (frameRate <= 0) { return; }

            float smallestDif = float.MaxValue;
            int smallestDifIndex = 0;
            for (int i = 0; i < m_formats.Count; i++) {
                float dif = UnityEngine.Mathf.Abs(m_formats[i].FrameRate - frameRate);
                if (dif < smallestDif) {
                    smallestDif = dif;
                    smallestDifIndex = i;
                }
            }
            SelectedFmtIdx = smallestDifIndex;
        }

        // sets the current selected index to the closest format to the specified width and height
        // if the specified width and height are is invalid, then the first available format is selected.
        // m_formats must be valid before calling this function.
        public void SelectClosestFormat(int width, int height)
        {
            SelectedFmtIdx = 0;
            if ((width <= 0) || (height <= 0)) { return; }

            int selectedIndex = 0;
            int closestHeight = 0;
            for (int i = 0; i < m_formats.Count; i++) {
                if (m_formats[i].Width == width) {
                    if (m_formats[i].Height == height) {
                        selectedIndex = i;
                        break; // exact match
                    } else {
                        int dif = UnityEngine.Mathf.Abs(m_formats[i].Height - height);
                        if (dif < closestHeight) {
                            closestHeight = dif;
                            selectedIndex = i;
                        }
                    }
                }
            }

            SelectedFmtIdx = selectedIndex;
        }

        // ########################### PRIVATE ################################ //
    
        public void RefreshFormatStrings()
        {
            m_formatStrings.Clear();
            for (int i = 0; i < m_formats.Count; i++) {
                m_formatStrings.Add(m_formats[i].MakeString(TexType));
            }
        }
    }
} // namespace