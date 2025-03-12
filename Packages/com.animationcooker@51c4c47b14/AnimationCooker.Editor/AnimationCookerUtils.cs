// This is a static class that contains a bunch of functions that can be used to bake vertex animations.
//--------------------------------------------------------------------------------------------------//

#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor; // used for AssetDatabase
using UnityEngine;
using Unity.Mathematics;

namespace AnimCooker
{
    public static class AnimationCookerUtils
    {
        const byte CookerFormatVersionNumber = 4;
        static readonly int[] Powers = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 16384 };

        static BoneInfo CalculateBoneInfo(Mesh mesh, Transform skinTransform, List<MeshFilter> boneMeshes, bool ignoreBoneMeshes, Mesh newMesh)
        {
            // find the boundaries for the mesh and any submeshes
            Bounds bounds = new Bounds();
            for (int j = 0; j < mesh.vertexCount; j++) {
                var point = skinTransform.TransformPoint(mesh.vertices[j]);
                if (j == 0) { bounds.center = point; }
                bounds.Encapsulate(point);
            }
            if (!ignoreBoneMeshes) {
                foreach (var filter in boneMeshes) {
                    var boneMesh = filter.sharedMesh;
                    for (int j = 0; j < boneMesh.vertexCount; j++) {
                        var point = filter.transform.TransformPoint(boneMesh.vertices[j]);
                        bounds.Encapsulate(point);
                    }
                }
            }
            BoneInfo boneInf;
            boneInf.Scale = newMesh.bounds.size.y / bounds.size.y;
            boneInf.Offset.y = 0 - bounds.min.y;
            boneInf.Offset.x = 0;
            boneInf.Offset.z = 0;
            return boneInf;
        }

        public static BakeResult BakeModel(GameObject prefab, in BakeOptions opts, List<SkinInfo> skins, List<ComputeShader> shaders)
        {
            BakeResult result = new BakeResult();
            result.Originalprefab = prefab;
            result.EnableR11G10B11 = opts.EnableR11G10B11;

            for (short s = 0; s < opts.SkinOpts.Count; s++) {
                result.SkinResults.Add(BakeSkin(prefab, s, opts, skins, shaders));
            }

            return result;
        }

        // This function create animation textures for the specified skin using the given prefab
        // and specified options and compute shader. results are stored in the return value.
        public static SkinResult BakeSkin(GameObject prefab, short skinIndex, in BakeOptions opts, List<SkinInfo> skinInfos, List<ComputeShader> shaders)
        {
            SkinOption skinOpt = opts.SkinOpts[skinIndex];
            SkinInfo skinInfo = skinInfos[skinIndex];
            float interval = 1f / skinOpt.SelectedFormat.FrameRate; // interval for this skin

            SkinResult result = new SkinResult();
            int enabledClipCount = opts.CalculateSelectedClipStats().TotalClipCount; // just used for logging/display

            // Save the old position and rotation because we don't want to change the original.
            // Note that because Quaternion and Vector3 are structs, we can use "=" notation to copy.
            // Making a copy of a game object and using that tranform does not work.
            // It has to be Skin.transform. Dunno why.
            Quaternion oldSkinRotation = skinInfo.Skin.transform.rotation;
            Vector3 oldSkinPosition = skinInfo.Skin.transform.position;
            Vector3 oldSkinScale = skinInfo.Skin.transform.localScale;
            Vector3 oldGoScale = prefab.transform.localScale;

            // Make an inverse scale value
            Vector3 invScale = new Vector3(invScale.x = 1f / prefab.transform.localScale.x, invScale.y = 1f / prefab.transform.localScale.y, invScale.z = 1f / prefab.transform.localScale.z);

            // set the transform pos, rot, and scale of the skin transform to the origin
            Quaternion rotation = opts.EnableResetRotationBeforeBake ? prefab.transform.rotation : oldSkinRotation;
            Vector3 position = opts.EnableResetPositionBeforeBake ? prefab.transform.position : oldSkinPosition;
            Vector3 scale = opts.EnableResetScaleBeforeBake ? invScale : oldSkinScale;
            skinInfo.Skin.transform.SetPositionAndRotation(position, rotation);
            skinInfo.Skin.transform.localScale = scale;

            // this will create a brand new mesh, but with the points transformed according to the specified transform
            Mesh adjustedMesh = CopyAndAdjustMesh(skinInfo.Skin.sharedMesh, skinInfo.BoneMeshes, skinInfo.Skin.transform);

            // now reset the transform for sure (no matter what is checked) before filling the pixel array.
            skinInfo.Skin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            skinInfo.Skin.transform.localScale = invScale;

            // preallocate the pixel array (it's faster this way)
            // we must make a prediction in order to know the necessary sample count.
            MeshStats meshStats = CalculateMeshStats(skinInfo, opts.IgnoreBoneMeshes);
            FrameStats frameStats = CalculateFrameStats(opts.ClipOpts, skinOpt.SelectedFormat, meshStats.VertexCount);

            // now that the predicted frame count is known, the pixels array can be allocated
            PixelInfo[] pixels = new PixelInfo[frameStats.PointCount];

            // keep track of the current position in vertexInfos.
            //int pixelIndex = skinOpt.SelectedFormat.Width; // skip over the first row - it will be filled at the end.
            int pixelIndex = 0;
            short clipIndex = 0; // the clip index in result.SkinClips - this is NOT the global index out of all available clips
            float totalLength = 0f; // accumulates total animation time for this skin (for all clips)
            int totalSampleCount = 0; // accumulates a total frame count for this skin (for all clips)
            PointRange vertStats = PointRange.Default;
            Mesh bakedMesh = new Mesh(); // holds the result of the baked mesh

            bool useNormal = (skinOpt.TexType == VtxAnimTexType.PosAndNml) || (skinOpt.TexType == VtxAnimTexType.PosNmlAndTan);
            bool useTangent = skinOpt.TexType == VtxAnimTexType.PosNmlAndTan;

            BoneInfo boneInfo = new BoneInfo { Offset = Vector3.zero, Scale = 0 };
            bool needsBoneAdjustCalc = false;

            foreach (ClipOption clipOpt in opts.ClipOpts) {
                if (!clipOpt.IsEnabled) { continue; }
                DisplayProgress("Cooking up some textures.", $"Skin {skinIndex}, {clipIndex} of {enabledClipCount} clips finished.", ((float)clipIndex) / ((float)enabledClipCount));

                // clip frame count is length * frameRate * texStats.sampleMultiplier
                short clipFrameCount = (short)Mathf.FloorToInt(clipOpt.Clip.length * skinOpt.SelectedFormat.FrameRate);

                // for every sampled frame in this clip
                for (int f = 0; f < clipFrameCount; f++) {
                    clipOpt.Clip.SampleAnimation(prefab.gameObject, interval * f);
                    skinInfo.Skin.BakeMesh(bakedMesh); // result is store in bakedMesh

                    // only calculate bone info if it hasn't been done
                    if (opts.EnableBoneAdjust && needsBoneAdjustCalc) {
                        boneInfo = CalculateBoneInfo(bakedMesh, skinInfo.Skin.transform, skinInfo.BoneMeshes, opts.IgnoreBoneMeshes, adjustedMesh);
                    }

                    for (int i = 0; i < bakedMesh.vertexCount; i++) {
                        AddPixel(pixels, ref vertStats, bakedMesh, skinInfo.Skin.transform, pixelIndex, i, useNormal, useTangent, boneInfo, opts.EnableBoneAdjust);
                        pixelIndex++;
                    }

                    if (!opts.IgnoreBoneMeshes) {
                        foreach (var filter in skinInfo.BoneMeshes) {
                            for (int i = filter.sharedMesh.vertexCount; i < filter.sharedMesh.vertexCount; i++) {
                                AddPixel(pixels, ref vertStats, filter.sharedMesh, filter.transform, pixelIndex, i, useNormal, useTangent, boneInfo, opts.EnableBoneAdjust);
                                pixelIndex++;
                            }
                        }
                    }
                }

                SkinClipEntry skinClip = new SkinClipEntry();
                skinClip.BeginFrame = (short)totalSampleCount;
                skinClip.EndFrame = (short)(skinClip.BeginFrame + clipFrameCount - 1);
                result.SkinClips.Add(skinClip);
                totalSampleCount += clipFrameCount;
                float clipLength = clipFrameCount * interval;
                totalLength += clipLength;
                clipIndex++;
            } // for each clip

            // now that we're done with setting pixels and building the mesh, we no longer need modified transforms.
            // we must set these values back to their original values because they belong to the prefab that was passed in.
            skinInfo.Skin.transform.SetPositionAndRotation(oldSkinPosition, oldSkinRotation);
            skinInfo.Skin.transform.localScale = oldSkinScale;

            // The above loop fills the pixel values, but the values are unencoded.
            // Because the values are being stored in an RGBA texture, we need to encode each vertex position to maximize precision.
            // This can't be performed in the loop above because we need to know the min and max values, (which were being calculated above)
            // This could be moved to the compute shader to make it run faster, but for now it's easiest to just do it right here.
            if (opts.EnableR11G10B11) {
                for (int i = 0; i < pixels.Length; i++) {
                    pixels[i].Position = PackingUtils.PackToR11G10B11(pixels[i].Position.xyz, vertStats.MinPos, vertStats.MaxPos);
                    pixels[i].Normal = PackingUtils.PackToR11G10B11(pixels[i].Normal.xyz, vertStats.MinNml, vertStats.MaxNml);
                    pixels[i].Tangent = PackingUtils.PackTangentToArgb(pixels[i].Tangent, vertStats.MinTan, vertStats.MaxTan);
                }
            } else {
                for (int i = 0; i < pixels.Length; i++) {
                    pixels[i].Position = PackingUtils.ScaleThreeFloatsToFloat4(pixels[i].Position.xyz, vertStats.MinPos, vertStats.MaxPos);
                    pixels[i].Normal = PackingUtils.ScaleThreeFloatsToFloat4(pixels[i].Normal.xyz, vertStats.MinNml, vertStats.MaxNml);
                    pixels[i].Tangent = PackingUtils.ScaleFourFloatsToFloat4(pixels[i].Tangent, vertStats.MinTan, vertStats.MaxTan);
                }
            }

            RenderTextureFormat fmt = RenderTextureFormat.ARGBHalf;
            if (opts.EnableR11G10B11) { fmt = RenderTextureFormat.ARGB32; }

            // It is VERY important to set the color space to linear! I wasted 16+ hours trying to figure 
            // out why the values I was encoding were not decoding properly (thanks bgolus!)
            RenderTexture positionRenderTex = new RenderTexture(skinOpt.SelectedFormat.Width, skinOpt.SelectedFormat.Height, 0, fmt, RenderTextureReadWrite.Linear);
            SetupRenderTexture(positionRenderTex);

            RenderTexture normalRenderTex = null, tangentRenderTex = null;
            if (useNormal) {
                normalRenderTex = new RenderTexture(skinOpt.SelectedFormat.Width, skinOpt.SelectedFormat.Height, 0, fmt, RenderTextureReadWrite.Linear);
                SetupRenderTexture(normalRenderTex);
            }
            if (useTangent) {
                tangentRenderTex = new RenderTexture(skinOpt.SelectedFormat.Width, skinOpt.SelectedFormat.Height, 0, fmt, RenderTextureReadWrite.Linear);
                SetupRenderTexture(tangentRenderTex);
            }

            // setup the compute buffer and run it (its task is to write all the vertexes to the texture)
            ComputeBuffer buffer = new ComputeBuffer(pixels.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PixelInfo)));
            buffer.SetData(pixels);
            ComputeShader shader = shaders[(byte)skinOpt.TexType];
            int kernel = shader.FindKernel("CSMain");
            shader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
            shader.SetInt("TexWidth", (int)skinOpt.SelectedFormat.Width);
            shader.SetBuffer(kernel, "Info", buffer);
            shader.SetTexture(kernel, "OutPosition", positionRenderTex);
            if (normalRenderTex != null) { shader.SetTexture(kernel, "OutNormal", normalRenderTex); }
            if (tangentRenderTex != null) { shader.SetTexture(kernel, "OutTangent", tangentRenderTex); }
            // when dispatching, we don't need to cover the whole texture - just the pixels that will be written to.
            // the vertexes will get blasted to pixels in the textures from left to right, bottom to top
            int height = Mathf.CeilToInt(pixels.Length / skinOpt.SelectedFormat.Width);
            shader.Dispatch(kernel, skinOpt.SelectedFormat.Width / (int)x + 1, height + 1 / (int)y + 1, 1);
            buffer.Release();

            // because unity randomly resets this for no particular reason...
            prefab.transform.localScale = oldGoScale;

            // store the rest of the result
            result.SkinRenderer = skinInfo.Skin;
            result.PositionRenderTex = positionRenderTex;
            result.NormalRenderTex = normalRenderTex;
            result.TangentRenderTex = tangentRenderTex;
            result.FixedMesh = adjustedMesh;
            result.Interval = interval;
            result.FrameRate = skinOpt.SelectedFormat.FrameRate;
            result.Pow2 = (byte)Mathf.Log(positionRenderTex.width, 2f);
            result.VertStats = vertStats; // copies because it's a struct
            result.VertexCount = meshStats.VertexCount;

            return result;
        }

        public static string SetupOutputDir(string outBakeFolder, string prefabName)
        {
            string subFolderPath = Path.Combine("Assets/", outBakeFolder, prefabName);
            // create the child folder and any parent folders that don't exist already.
            if (Directory.Exists(subFolderPath)) { Directory.Delete(subFolderPath, true); }
            Directory.CreateDirectory(subFolderPath);
            return subFolderPath;
        }

        public static void SetDefaultTextureParams(Texture2D tex)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 0;
        }

        public static void SetDefaultImporterParams(string path, int width, bool useR11B10G11)
        {
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);

            TextureImporterPlatformSettings tips = importer.GetDefaultPlatformTextureSettings();
            tips.format = useR11B10G11 ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGBAHalf;
            tips.maxTextureSize = width;
            importer.SetPlatformTextureSettings(tips);
            importer.mipmapEnabled = false;
            importer.compressionQuality = 100;
            importer.convertToNormalmap = false;
            importer.maxTextureSize = width;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // test with CompressedHQ ?
            importer.sRGBTexture = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = true; // important for release-builds
            // platform overrides are required for release-builds which often try to use compression.
            SetPlatformOverride(importer, "Windows", width, tips.format);
            SetPlatformOverride(importer, "Mac", width, tips.format);
            SetPlatformOverride(importer, "Linux", width, tips.format);
            SetPlatformOverride(importer, "Android", width, tips.format);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        static void SetPlatformOverride(TextureImporter textureImporter, string platformName, int maxTextureSize, TextureImporterFormat textureFormat)
        {
            TextureImporterPlatformSettings platformSettings = textureImporter.GetPlatformTextureSettings(platformName);

            platformSettings.overridden = true;  // Enable override for the platform
            platformSettings.maxTextureSize = maxTextureSize;  // Set max texture size for the platform
            platformSettings.format = textureFormat;  // Set texture format for the platform

            textureImporter.SetPlatformTextureSettings(platformSettings);
        }

        // This function can only be called from the Unity Editor
        // It does the following:
        //   - Creates a .posTex.asset file for the the position texture
        //   - Optionally creates .nmlTex and .tanTex files for the normal and position textures.
        //   - Creates a .material.asset file using the specified shader (uses the textures)
        //   - Creates a .mesh.asset file with the new static mesh (no bones, no multiple meshes)
        // bakeResult --> the result of a baking operation (for one skin)
        // subFolderPath --> the path where files should be saved to. (ex: Assets\Prefabs\BakedAnimations\Horse)
        // playShader --> the shader use for playback (this MUST be valid)
        // enableSaveTextures --> true to save a copy of the source textures (basemap, bumpmap, etc) to the output folder.
        public static SaveResult SaveBakedFiles(BakeResult bakeResult, string subFolderPath, Shader playShader, bool enableCopyTextures)
        {
            SaveResult result = new SaveResult();
            result.PlayShaderName = playShader.name;
            string prefabName = $"{bakeResult.Originalprefab.name}";

            // Multiple materials might use the same texture files (_BaseMap, _BumpMap, etc).
            // This dictionary ensures that textures are only copied once if EnableCopyTexturesToOutput is enabled.
            Dictionary<string, Texture> texDict = new Dictionary<string, Texture>();

            for (short s = 0; s < bakeResult.SkinResults.Count; s++) {
                SkinResult bakeSkinRes = bakeResult.SkinResults[s];
                SaveSkinResult saveSkinRes = new SaveSkinResult();
                //################# CREATE TEXTURES #################//
                // Make sure to use linear instead of sRGB!!! (the second parameter in Convert())
                // Scheisse! I was having so many issues with the first and last frames.
                // I wasted over 12 hours trying to figure out what was wrong and it turned
                // out that all I needed was to change filter mode from bilinear to point in the texture properties.
                Texture2D posTex = RenderTextureToTexture2D.Convert(bakeSkinRes.PositionRenderTex, true);
                SetDefaultTextureParams(posTex);
                Graphics.CopyTexture(bakeSkinRes.PositionRenderTex, posTex);

                //// METHOD 1: .asset file (works, but doesn't allow adjusting compression)
                //saveSkinRes.PosTexPath = Path.Combine(subFolderPath, $"{prefabName}{s}.posTex.asset");
                //AssetDatabase.CreateAsset(posTex, saveSkinRes.PosTexPath);

                //// METHOD 2: .png file (has problems with certain formats)
                //saveSkinRes.PosTexPath = Path.Combine(subFolderPath, $"{prefabName}{s}.posTex.png");
                //System.IO.File.WriteAllBytes(saveSkinRes.PosTexPath, posTex.EncodeToPNG());

                // METHOD 3: .exr file
                saveSkinRes.PosTexPath = Path.Combine(subFolderPath, $"{prefabName}{s}.posTex.exr");
                System.IO.File.WriteAllBytes(saveSkinRes.PosTexPath, posTex.EncodeToEXR());

                Texture2D nmlTex = null;
                Texture2D tanTex = null;

                if (bakeSkinRes.NormalRenderTex != null) {
                    nmlTex = RenderTextureToTexture2D.Convert(bakeSkinRes.NormalRenderTex, true);
                    Graphics.CopyTexture(bakeSkinRes.NormalRenderTex, nmlTex);

                    // METHOD 2: exr file
                    saveSkinRes.NmlTexPath = Path.Combine(subFolderPath, $"{prefabName}{s}.nmlTex.exr");
                    System.IO.File.WriteAllBytes(saveSkinRes.NmlTexPath, nmlTex.EncodeToEXR());
                }
                if (bakeSkinRes.TangentRenderTex != null) {
                    tanTex = RenderTextureToTexture2D.Convert(bakeSkinRes.TangentRenderTex, true);
                    Graphics.CopyTexture(bakeSkinRes.TangentRenderTex, tanTex);

                    // METHOD 2: .exr file
                    saveSkinRes.TanTexPath = Path.Combine(subFolderPath, $"{prefabName}{s}.tanTex.exr");
                    System.IO.File.WriteAllBytes(saveSkinRes.TanTexPath, tanTex.EncodeToEXR());
                }

                // since the file was written outside of the asset system, a refresh needs to be done here.
                AssetDatabase.Refresh();

                // Now that the database has been refreshed, the new textures should be available.
                // However, they are going to have messed up default import settings (such as automatic compression and mipmaps).
                // Make some changes to the importer and then reimport and reload the textures.
                int width = bakeResult.SkinResults[s].PositionRenderTex.width;
                SetDefaultImporterParams(saveSkinRes.PosTexPath, width, bakeResult.EnableR11G10B11);
                posTex = AssetDatabase.LoadAssetAtPath<Texture2D>(saveSkinRes.PosTexPath);
                if (bakeSkinRes.NormalRenderTex != null) {
                    SetDefaultImporterParams(saveSkinRes.NmlTexPath, width, bakeResult.EnableR11G10B11);
                    nmlTex = AssetDatabase.LoadAssetAtPath<Texture2D>(saveSkinRes.NmlTexPath);
                }
                if (bakeSkinRes.TangentRenderTex != null) {
                    SetDefaultImporterParams(saveSkinRes.TanTexPath, width, bakeResult.EnableR11G10B11);
                    tanTex = AssetDatabase.LoadAssetAtPath<Texture2D>(saveSkinRes.TanTexPath);
                }

                //################# CREATE FIXED MESH #################//

                // Save the fixed mesh
                saveSkinRes.MeshPath = Path.Combine(subFolderPath, $"{prefabName}{s}.mesh.asset");
                AssetDatabase.CreateAsset(bakeSkinRes.FixedMesh, saveSkinRes.MeshPath);

                //################# SAVE #################//

                // We must save the mesh before creating the material or else the material won't be able to point at them.
                AssetDatabase.SaveAssets();

                //################# CREATE MATERIAL #################//

                // Create the material using the specified shader.
                Material mat = new Material(playShader);
                mat.name = $"{prefabName}{s}";
                saveSkinRes.MatPath = Path.Combine(subFolderPath, $"{mat.name}.mat.mat");
                AssetDatabase.CreateAsset(mat, saveSkinRes.MatPath);

                // This doesn't work the way you might expect. For some reason, if you call this copy function
                // and then set the material parameters (_PosMap, etc) immediately after,
                // and then save the material to disk, they will be ignored. I don't know why.
                // The way to make it work was to call copy, save the material to disk, and then set the
                // custom parameters (_PosMap, etc), and then save again.
                mat.CopyPropertiesFromMaterial(bakeSkinRes.SkinRenderer.sharedMaterial);
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssetIfDirty(mat);
      
                // Finish setting up the material and save it

                if (enableCopyTextures) {                
                    SetTexCopy(mat, texDict, "_BaseMap", subFolderPath);
                    SetTexCopy(mat, texDict, "_BumpMap", subFolderPath);
                    SetTexCopy(mat, texDict, "_SpecGlossMap", subFolderPath);
                    SetTexCopy(mat, texDict, "_EmissionMap", subFolderPath);
                }

                if (bakeResult.EnableR11G10B11) {
                    mat.EnableKeyword("ENABLE_VA_RGBA10BIT");
                    mat.SetFloat("_EnableRgba10Bit", 1f);
                } else {
                    mat.DisableKeyword("ENABLE_VA_RGBA10BIT");
                    mat.SetFloat("_EnableRgba10Bit", 0f);
                }

                Vector4 shift;
                shift.x = 0; // time // time 0 (dynamic)
                shift.y = 0; // begin frame - first skin, first clip (dynamic)
                shift.z = bakeResult.SkinResults[s].SkinClips[0].EndFrame; // end frame - first skin, first clip (dynamic)
                shift.w = 0; // unused;
                mat.SetVector("_Shift", shift); // dynamic - animation system will set this every frame

                Vector4 stat1;
                stat1.x = bakeResult.SkinResults[s].Pow2; // power-of-two for width aka log2(width), (fixed)
                stat1.y = bakeResult.SkinResults[s].VertStats.MinPos; // min position value (fixed)
                stat1.z = bakeResult.SkinResults[s].VertStats.MaxPos; // max position value (fixed)
                stat1.w = bakeResult.SkinResults[s].FrameRate; // frame rate for the skin (fixed)
                mat.SetVector("_Stat1", stat1); // fixed/static

                Vector4 stat2;
                stat2.x = bakeResult.SkinResults[s].VertStats.MinNml; // min normal value (fixed)
                stat2.y = bakeResult.SkinResults[s].VertStats.MaxNml; // max normal value (fixed)
                stat2.z = bakeResult.SkinResults[s].VertStats.MinTan; // min tangent value (fixed)
                stat2.w = bakeResult.SkinResults[s].VertStats.MaxTan; // max tangent value (fixed)
                mat.SetVector("_Stat2", stat2); // fixed/static

                Vector4 stat3;
                stat3.x = bakeResult.SkinResults[s].VertexCount; // vertex count (fixed)
                stat3.y = s; // skin index (fixed, always the same for the material)
                stat3.z = 0; // unused
                stat3.w = 0; // unused
                mat.SetVector("_Stat3", stat3); // fixed

                mat.SetTexture("_PosMap", posTex);
                if (nmlTex != null) {
                    mat.SetTexture("_NmlMap", nmlTex);
                    mat.EnableKeyword("USE_VA_NORMAL_MAP");
                    mat.SetFloat("_UseNormalMap", 1f);
                } else {
                    mat.SetFloat("_UseNormalMap", 0f);
                    mat.DisableKeyword("USE_VA_NORMAL_MAP");
                }
                if (tanTex != null) {
                    mat.SetTexture("_TanMap", tanTex);
                    mat.EnableKeyword("USE_VA_TANGENT_MAP");
                    mat.SetFloat("_UseTangentMap", 1f);
                } else {
                    mat.SetFloat("_UseTangentMap", 0f);
                    mat.DisableKeyword("USE_VA_TANGENT_MAP");
                }

                // This is the second save - meant to save the above params to the material.
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssetIfDirty(mat);
                result.Skins.Add(saveSkinRes);
            }

            return result;
        }

        // This function will check if the specified texture is in the dictionary.
        // If not, it will make a copy of the material into the subfolder path, and then reload the texture.
        // It will also set the newly reloaded texture to the material using the specified variable name.
        // mat --> the source material that contains the texture
        // texDict --> a dictionary of textures that prevents a texture from getting copied multiple times
        // varName --> the name of the variable in the shader, such as _BaseMap, _BumpMap, etc.
        // subFolderPath --> the output folder where the texture will be copied to 
        static void SetTexCopy(Material mat, Dictionary<string, Texture> texDict, string varName, string subFolderPath)
        {
            Texture srcTex = mat.GetTexture(varName);
            if (srcTex) {
                string srcPath = AssetDatabase.GetAssetPath(srcTex);
                Texture destTex;
                if (texDict.ContainsKey(srcPath)) {
                    destTex = texDict[srcPath];
                } else {
                    string fileNameWithExt = Path.GetFileName(srcPath);
                    string newPath = Path.Combine(subFolderPath, fileNameWithExt);
                    if (!AssetDatabase.CopyAsset(srcPath, newPath)) { UnityEngine.Debug.Log($"copy failed {srcPath} to {newPath}"); }
                    AssetDatabase.Refresh();
                    destTex = AssetDatabase.LoadAssetAtPath<Texture>(newPath);
                    texDict.Add(srcPath, destTex);
                }
                if (destTex != null) { mat.SetTexture(varName, destTex); }
            }
        }

        public static List<SkinInfo> FindSkinnedMeshRenderers(GameObject go)
        {
            List<SkinInfo> ret = new List<SkinInfo>();

            LODGroup lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup == null) { lodGroup = go.GetComponentInChildren<LODGroup>(); }

            if (lodGroup != null) {
                LOD[] lods = lodGroup.GetLODs();
                for (int i = 0; i < lods.Length; i++) {
                    if (lods[i].renderers.Length > 0) {
                        SkinnedMeshRenderer skin = lods[i].renderers[0] as SkinnedMeshRenderer;
                        if ((skin != null) && (skin.sharedMesh != null)) { ret.Add(new SkinInfo { Skin = skin, BoneMeshes = FindMeshesInBones(skin.transform) }); }
                    }
                }
            } else {
                SkinnedMeshRenderer[] skins = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < skins.Length; i++) {
                    ret.Add(new SkinInfo { Skin = skins[i], BoneMeshes = FindMeshesInBones(skins[i].transform) });
                }
            }
            return ret;
        }

        public static List<MeshFilter> FindMeshesInBones(Transform parent)
        {
            List<MeshFilter> filters = new List<MeshFilter>();
            RecursivelyFindMeshesInBones(filters, parent.transform);
            return filters;
        }

        public static MeshStats CalculateAllMeshStats(List<SkinInfo> skinInfos, bool ignoreBoneMeshes, bool useLods)
        {
            MeshStats total = default;
            foreach (SkinInfo skinInfo in skinInfos) {
                MeshStats stats = CalculateMeshStats(skinInfo, ignoreBoneMeshes);
                total.BoneMeshCount += stats.BoneMeshCount;
                total.VertexCount += stats.VertexCount;
            }
            return total;
        }

        public static MeshStats CalculateMeshStats(SkinInfo skinInfo, bool ignoreBoneMeshes)
        {
            if (skinInfo == null) { return default; }
            if (skinInfo.Skin == null) { return default; }
            if (skinInfo.Skin.sharedMesh == null) { return default; }
            MeshStats stats;
            stats.VertexCount = 0;
            stats.BoneMeshCount = 0;
            stats.VertexCount = (skinInfo.Skin != null) ? skinInfo.Skin.sharedMesh.vertexCount : 0;
            if (!ignoreBoneMeshes) {
                for (int i = 0; i < skinInfo.BoneMeshes.Count; i++) {
                    stats.VertexCount += skinInfo.BoneMeshes[i].sharedMesh.vertexCount;
                }
                stats.BoneMeshCount = skinInfo.BoneMeshes.Count;
            }
            return stats;
        }

        // using the given format and clips, calculate the frame stats
        // (this can be a prediction of how many frames or points will be needed)
        public static FrameStats CalculateFrameStats(List<ClipOption> clipOpts, TexSampleFormat fmt, int vertexCount)
        {
            FrameStats stats;
            stats.FrameCount = 0;
            for (int i = 0; i < clipOpts.Count; i++) {
                ClipOption clipOpt = clipOpts[i];
                if (clipOpt.IsEnabled) {
                    stats.FrameCount += (short)Mathf.FloorToInt(clipOpt.Clip.length * fmt.FrameRate);
                }
            }
            stats.PointCount = (vertexCount * stats.FrameCount);
            return stats;
        }

        public static string MakePredictionString(BakeOptions opts, MeshStats meshStats, ClipStats clipStats, FrameStats frameStats, int lodIndex)
        {
            if (opts.SkinOpts[lodIndex].SelectedFormat.Width <= 0) { return ""; }
            string ret = $"Skin {lodIndex} > {meshStats.VertexCount} verts, {(int)((float)opts.SkinOpts[lodIndex].SelectedFormat.CalculateBytes(opts.SkinOpts[lodIndex].TexType) * 0.001)} KB";
            ret += ", " + frameStats.FrameCount + "f at " + opts.SkinOpts[lodIndex].SelectedFormat.FrameRate + "fps";
            ret += ", " + opts.SkinOpts[lodIndex].SelectedFormat.Width + "x" + opts.SkinOpts[lodIndex].SelectedFormat.Height;
            //ret += "\n   Bone Meshes: " + meshStats.BoneMeshCount;

            float expectedHeight = Mathf.Ceil(meshStats.VertexCount * frameStats.FrameCount / opts.SkinOpts[lodIndex].SelectedFormat.Width) + 1;
            float expectedFillPercent = (expectedHeight / opts.SkinOpts[lodIndex].SelectedFormat.Height) * 100f;
            ret += $", {string.Format("{0:0.##}", expectedFillPercent)}% full.";
            return ret;
        }

        // function to fetch all the clips from m_prefab
        // it will attempt to fetch them from either an Animator or an Animation
        public static List<AnimationClip> FindClips(GameObject prefab, ref string msg)
        {
            // first attempt to fetch an animator
            Animator animator = FindAnimator(prefab);
            string animatorStr = "";
            if (animator == null) {
                animatorStr = "An animator was not found.";
            } else if (animator.runtimeAnimatorController == null){
                animatorStr = "A runtime animator controller was not found";
            } else if (animator.runtimeAnimatorController.animationClips.Length <= 0) {
                animatorStr = "A runtime animator controller was found but it has no animation clips.";
            } else {
                return animator.runtimeAnimatorController.animationClips.ToList();
            }

            // if there is no animator, try to fetch an animation
            Animation animation = FindAnimation(prefab);
            if (animation == null) {
                msg += "There is no Animation on the prefab or its children. " + animatorStr;
                return null;
            }
            if (animation.GetClipCount() <= 0) {
                msg += "Animation was found, but it contains no clips. " + animatorStr;
                return null;
            }
            int clipCount = animation.GetClipCount();
            List<AnimationClip> clips = new List<AnimationClip>();
            int i = 0;
            foreach (AnimationState state in animation) {
                if (state.clip != null) {
                    clips.Add(state.clip);
                    i++;
                }
            }
            if (i != clipCount) {
                msg += $"Animation found, but {clipCount - i} out of {clipCount} clips are invalid. " + animatorStr;
                return null;
            }
            return clips;
        }

        // smallestFps --> the smallest out of any of the clips (can be obtained via the prediction)
        public static List<TexSampleFormat> DiscoverPossibleFormats(long vertexCount, float totalClipLength, float smallestFps)
        {
            List<TexSampleFormat> formats = new List<TexSampleFormat>(); // the return value

            // note that there might actually be less points than what is calculated below.
            // when we actually do the sampling, we will need to take an integer number of samples,
            // and the number of seconds for each clip is a decimal value,
            // so we could have clip lengths that look something like:
            // 5.0 + 2.5 + 3.2 + 1.9 + 3.4 + 2.3 + 4.8 --> 23.1 seconds.
            // If the desired frame rate is 7fps, then that makes 23.1s * 7fps --> 161.7
            // 7fps has an interval of 1/7.
            // If we calculate the number of times we need to loop and take a sample for each frame,
            // we get a table like this, where the last two numbers use floor() and ceiling():
            // 5.0s * 7fps --> 35.0 --> 35 | 35
            // 2.5s * 7fps --> 17.5 --> 17 | 18
            // 3.2s * 7fps --> 22.4 --> 22 | 23
            // 1.9s * 7fps --> 13.3 --> 13 | 14
            // 3.4s * 7fps --> 23.8 --> 23 | 24
            // 2.3s * 7fps --> 16.1 --> 16 | 17
            // 4.8s * 7fps --> 33.6 --> 33 | 34
            // floor() --> 159 samples | ceiling() --> 165 samples
            // calculated --> 161.7 (from 23.1s * 7)
            // So its apparent that we can't know the exact number of samples that will result without knowing the desired fps
            // One way to handle this would be to loop for all fps (1..60) and calculate the sample counts, but that would get messy.
            // The current way it's being handled is by using the floor of the calculated number (161) as a worst case guess,
            // and using floor() during the actual sampling. The estimate will likely be over by a small amount, but it should never be under.

            float worstCasePointCount = Mathf.FloorToInt(totalClipLength * vertexCount);

            // test each power for a width
            for (int i = 0; i < Powers.Length; i++) {
                int width = Powers[i];
                for (int j = 0; j < Powers.Length; j++) {
                    int height = Powers[j];

                    // only look at situations where height is smaller than or the same size as width
                    // (we don't care about textures like 128x8192)
                    if (height <= width) {

                        // example with slots needed at 36532.5 (7.5 seconds * 4871 vertexes)
                        // ((128 * 128) - 128) / 36532.5 --> 0.4449 [0] (ignore)
                        // ((256 * 128) - 256) / 36532.5 --> 0.8899 [0] (ignore)
                        // ((256 * 256) - 256) / 36532.5 --> 1.7869 [1]
                        // ((512 * 128) - 512) / 36532.5 --> 1.7798 [1]
                        // ((512 * 256) - 512) / 36532.5 --> 3.5738 [3] ** add me
                        // ((512 * 512) - 512) / 36532.5 --> 7.1616 [7] ** add me
                        // ((1024 * 128) - 1024) / 36532.5 --> 3.5597 [3]
                        // ((1024 * 256) - 1024) / 36532.5 --> 7.1476 [7]
                        // ((1024 * 512) - 1024) / 36532.5 --> 14.3232 [14] ** add me
                        // ((1024 * 1024) - 1024) / 36532.5 --> 28.6745 [28] ** add me
                        // ((2048 * 128) - 2048) / 36532.5 --> 7.1195 [7]
                        // ((2048 * 256) - 2048) / 36532.5 --> 14.2952 [14]
                        // ((2048 * 512) - 2048) / 36532.5 --> 28.6464 [28]
                        // ((2048 * 1024) - 2048) / 36532.5 --> 57.3490 [57] ** add me
                        // ((2048 * 2048) - 2048) / 36532.5 --> 114.7541 [114] (clamp to 60) ** add me
                        // exit --> no need to go past our max frame rate (everything after will just be repeat)

                        // the max number of data points we can possibly hold in this texture
                        int availablePointCount = (width * height);

                        // calculate the highest frame rate that could possibly fit into the texture
                        byte maxRate = (byte)Mathf.FloorToInt(availablePointCount / worstCasePointCount);

                        // ignore frame rates less than zero
                        if (maxRate > 0) {
                            // clamp the max frame rate to the highest value that the clips will handle
                            bool maxReached = false;
                            if (maxRate >= smallestFps) {
                                maxRate = (byte)smallestFps; // clamp it
                                maxReached = true;
                            }

                            // only add the rate if it doesn't already exist
                            // in theory a hash set would speed this up, but there are so few values usually that it's probably not worth the hassle.
                            if (!RateExists(maxRate, formats)) {
                                formats.Add(new TexSampleFormat(maxRate, width, height));
                            }

                            // if the max frame rate has been reached, then we're done - nothing after this interests us
                            if (maxReached) { return formats;}
                        }
                    }
                }
            }
            return formats;
        }


        //#############################################################################################################################
        //############################################ PRIVATE FUNCTIONS BELOW ########################################################
        //#############################################################################################################################

        static void AddPixel(PixelInfo[] pixels, ref PointRange vertStats, Mesh mesh, Transform xform, int pixIdx, int vertIdx, bool useNormal, bool useTangent, BoneInfo boneInfo, bool enableBoneAdjust)
        {
            Vector3 pos;
            if (enableBoneAdjust) {
                pos = (xform.TransformPoint(mesh.vertices[vertIdx]) + boneInfo.Offset);
            } else {
                pos = xform.TransformPoint(mesh.vertices[vertIdx]);
            }

            // note - the position can't be encoded here because here we are calculating vert stats
            pixels[pixIdx].Position = new float4(pos.x, pos.y, pos.z, 0f);
            vertStats.UpdatePos(pos);

            // note - the normal can't be encoded here because here we are calculating vert stats
            if (useNormal) {
                Vector3 nml = mesh.normals[vertIdx];
                pixels[pixIdx].Normal = new float4(nml.x, nml.y, nml.z, 0f);
                vertStats.UpdateNml(nml);
            }
            if (useTangent) {
                Vector4 tan = mesh.tangents[vertIdx];
                pixels[pixIdx].Tangent = tan;
                vertStats.UpdateTan(tan);
            }
        }

        // recursive function to find all MeshFilters
        static void RecursivelyFindMeshesInBones(List<MeshFilter> filters, Transform bone)
        {
            foreach (Transform child in bone) { RecursivelyFindMeshesInBones(filters, child); }
            var filter = bone.GetComponent<MeshFilter>();
            if (filter != null) { filters.Add(filter); }
        }

        static bool RateExists(int rate, List<TexSampleFormat> formats)
        {
            for (int r = 0; r < formats.Count; r++) {
                if (formats[r].FrameRate == rate) { return true; }
            }
            return false;
        }

        static Mesh CopyAndAdjustMesh(Mesh srcMesh, List<MeshFilter> boneMeshes, Transform xForm)
        {
            List<Vector3> vertices = new List<Vector3>(srcMesh.vertexCount);
            // copy the vertexes, transforming each one from local to world space during the process
            foreach (var vertex in srcMesh.vertices) {
                vertices.Add(xForm.TransformPoint(vertex));
            }

            // copy values into a brand new mesh.
            // note that using ToArray() forces a deep copy
            Mesh newMesh = new Mesh();
            newMesh.subMeshCount = srcMesh.subMeshCount;
            newMesh.SetVertices(vertices);
            for (int i = 0; i < srcMesh.subMeshCount; i++) { newMesh.SetTriangles(srcMesh.GetTriangles(i).ToArray(), i); }
            int offset = vertices.Count;
            newMesh.uv = srcMesh.uv.ToArray();
            newMesh.normals = srcMesh.normals.ToArray();
            newMesh.tangents = srcMesh.tangents.ToArray();
            newMesh.colors = srcMesh.colors.ToArray();

            foreach (var filter in boneMeshes) {
                Mesh boneMesh = filter.sharedMesh;
                List<Vector3> newVerts = newMesh.vertices.ToList();
                List<Vector2> newUv = newMesh.uv.ToList();
                List<Vector3> newNormals = newMesh.normals.ToList();
                List<Vector4> newTangents = newMesh.tangents.ToList();
                List<Color> newColors = newMesh.colors.ToList();
                List<int> newTris = newMesh.triangles.ToList();

                for (int i = 0; i < boneMesh.vertexCount; i++) {
                    newVerts.Add(filter.transform.TransformPoint(boneMesh.vertices[i]));
                }
                newMesh.vertices = newVerts.ToArray();

                var boneTris = boneMesh.triangles.ToList();
                for (int i = 0; i < boneTris.Count; i++) { boneTris[i] = boneTris[i] + offset; }
                newTris.AddRange(boneTris);
                newMesh.SetTriangles(newTris, 0);

                newUv.AddRange(boneMesh.uv);
                newNormals.AddRange(boneMesh.normals);
                newTangents.AddRange(boneMesh.tangents);
                newColors.AddRange(boneMesh.colors);

                newMesh.uv = newUv.ToArray();
                newMesh.normals = newNormals.ToArray();
                newMesh.tangents = newTangents.ToArray();
                if (srcMesh.colors.Length > 0) { newMesh.colors = newColors.ToArray(); }

                offset += boneMesh.vertexCount;
            }
            newMesh.RecalculateBounds();
            newMesh.MarkDynamic();
            return newMesh;
        }

        static void SetupRenderTexture(RenderTexture rendTex)
        {
            rendTex.enableRandomWrite = true;
            rendTex.Create();
            RenderTexture.active = rendTex;
            GL.Clear(true, true, Color.clear);
        }

        public static void DisplayProgress(string title, string info, float progress)
        {
            EditorUtility.DisplayProgressBar(title, info, progress);
        }

        // I tried to make this a template function, but unity would complain about accessing a component that didn't exist
        static Animator FindAnimator(GameObject go)
        {
            Animator ret = go.GetComponent<Animator>();
            if (ret != null) { return ret; }
            return go.GetComponentInChildren<Animator>(true);
        }

        static Animation FindAnimation(GameObject go)
        {
            Animation ret = go.GetComponent<Animation>();
            if (ret != null) { return ret; }
            return go.GetComponentInChildren<Animation>(true);
        }
    }
} // namespace

#endif