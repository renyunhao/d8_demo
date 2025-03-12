// Place this on an empty object somewhere in a SUBscene to bake it into the subscene
// which will allow you to access the database from ISystem and jobs without touching managed code.
// If there is only one in the scene (recommended), you can access it as a singleton.
//
// There are several convenience functions within the singleton to help you lookup clips/entities easier.
//
// Example of use:
//    [BurstCompile]
//    public partial struct MySystem : ISystem
//    {
//       void OnCreate(ref SystemState state)
//       {
//          state.RequireForUpdate<AnimDbRefData>();
//       }
//       [BurstCompile]
//       void OnUpdate(ref SystemState state)
//       {
//          AnimDbRefData db = SystemAPI.GetSingleton<AnimDbRefData>();
//          UnityEngine.Debug.Log($"There are {db.GetModelCount()} models in the database.");
//       }
//    }

using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AnimCooker
{
    public struct ModelData
    {
        public FixedString128Bytes ModelName;
        public BlobArray<SkinData> Skins;
        public short MinPos;
        public short MaxPos;
        public short MinNml;
        public short MaxNml;
        public short MinTan;
        public short MaxTan;

        public short Index;

        // holds information about each animation clip
        public BlobArray<ClipData> Clips;

        // Searches for the first clip name that contains the specified word (Case Sensitive)
        // For example, if your clips are { "Dog_Run", "Dog_Idle", "Dog_Attack", "Dog_RunFast" } and you search for "Run",
        // the function will return 0 (correpsonding with "Dog_Run").
        // text --> the search text
        // return --> the clip index corresponding with the first instance found, or -1 if not found.
        public short FindClipThatContains(FixedString128Bytes text)
        {
            for (short i = 0; i < Clips.Length; i++) {
                //ClipData clip = Clips[i];
                if (Clips[i].Name.Contains(text)) { return i; }
            }
            return -1;
        }

        // Searches for the first clip with the specified name.
        // text --> the clip name (must match exactly)
        // return --> the clip index corresponding with the first instance found, or -1 if not found.
        public short FindClip(FixedString128Bytes name)
        {
            for (short i = 0; i < Clips.Length; i++) {
                if (Clips[i].Name == name) { return i; }
            }
            return -1;
        }
    }

    public struct SkinClipData
    {
        public short BeginFrame;
        public short EndFrame;
        public short GetFrameCount() { return (short)(EndFrame - BeginFrame + 1); }
        public float GetLength(float interval) { return interval * GetFrameCount(); }
        public bool IsValid() { return BeginFrame < EndFrame; }
    }

    public struct ClipData
    {
        public FixedString128Bytes Name;
        public short Index;
        public float Length; // in seconds
    }

    public struct SkinData
    {
        public BlobArray<SkinClipData> SkinClips;
        public ushort VertCount;
        public ushort Width;
        public ushort Height;
        public float Interval; // "sampled interval" (doesn't account for speed)
        public byte Pow2;
        public byte FrameRate; // "sampled frame rate" (doesn't account for speed)
        public float GetClipLength(short clipIndex) { return Interval * SkinClips[clipIndex].GetFrameCount(); }
    }

    public struct AnimModelData : IComponentData
    {
        // this holds information for each model
        public BlobArray<ModelData> Models;

        public NativeList<int> FindClips(FixedString128Bytes clipText, Allocator allocator = Allocator.Persistent)
        {
            NativeList<int> arr = new NativeList<int>(allocator);
            for (int m = 0; m < Models.Length; m++) {
                for (int c = 0; c < Models[m].Clips.Length; c++) {
                    if (Models[m].Clips[c].Name == clipText) { arr.Add(c); }
                }
            }
            return arr;
        }

        public NativeList<int> FindClipsThatContain(FixedString128Bytes clipText, Allocator allocator = Allocator.Persistent)
        {
            NativeList<int> arr = new NativeList<int>(allocator);
            for (int m = 0; m < Models.Length; m++) {
                for (int c = 0; c < Models[m].Clips.Length; c++) {
                    if (Models[m].Clips[c].Name.Contains(clipText)) { arr.Add(c); }
                }
            }
            return arr;
        }
    }

    public struct AnimDbRefData : IComponentData
    {
        public BlobAssetReference<AnimModelData> Ref;

        public AnimDbRefData(AnimDbSo db)
        {
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            if ((db != null) && (db.Models != null) && (db.Models.Count > 0)) {
                ref AnimModelData blobAsset = ref builder.ConstructRoot<AnimModelData>();

                // allocate models array
                BlobBuilderArray<ModelData> models = builder.Allocate(ref blobAsset.Models, db.Models.Count);

                for (short m = 0; m < db.Models.Count; m++) {
                    ModelEntry srcModel = db.GetModel(m);
                    models[m].ModelName = srcModel.ModelName;
                    models[m].Index = m;
                    // allocate clips array
                    BlobBuilderArray<ClipData> clips = builder.Allocate(ref models[m].Clips, srcModel.Clips.Count);
                    for (short c = 0; c < srcModel.Clips.Count; c++) {
                        // copy clips data
                        clips[c].Name = srcModel.Clips[c].ClipName;
                        clips[c].Length = srcModel.Clips[c].Length;
                    }
                    // allocate skins array
                    BlobBuilderArray<SkinData> skins = builder.Allocate(ref models[m].Skins, srcModel.Skins.Count);
                    for (short s = 0; s < srcModel.Skins.Count; s++) {
                        SkinEntry srcSkin = srcModel.Skins[s];
                        // copy skin vars
                        skins[s].FrameRate = srcSkin.FrameRate;
                        skins[s].Height = srcSkin.Height;
                        skins[s].Interval = srcSkin.Interval;
                        skins[s].Pow2 = srcSkin.Pow2;
                        skins[s].Width = srcSkin.Width;
                        skins[s].VertCount = srcSkin.VertCount;
                        // allocate skin clips array
                        BlobBuilderArray<SkinClipData> skinClips = builder.Allocate(ref skins[s].SkinClips, srcSkin.SkinClips.Count);
                        for (short sc = 0; sc < srcSkin.SkinClips.Count; sc++) {
                            // copy skin clip values
                            skinClips[sc].BeginFrame = srcSkin.SkinClips[sc].BeginFrame;
                            skinClips[sc].EndFrame = srcSkin.SkinClips[sc].EndFrame;
                        }
                    }
                }
            } else {
                if (db != null) { 
                    UnityEngine.Debug.Log($"db is null");
                } else if (db.Models == null) {
                    UnityEngine.Debug.Log($"db.Models is null");
                } else if (db.Models.Count <= 0) {
                    UnityEngine.Debug.Log($"db.Models is empty");
                }
            }
            Ref = builder.CreateBlobAssetReference<AnimModelData>(Allocator.Persistent);
            builder.Dispose();
        }

        public ref ModelData GetModel(int modelIndex) { return ref Ref.Value.Models[modelIndex]; }

        public short GetModelCount() { return (short)Ref.Value.Models.Length; }

        // Returns the model index if found, and -1 if not found.
        // This function is slow, but it gives the model index which can be used with GetModel() for instant access.
        public short FindModelIndex(FixedString32Bytes modelName)
        {
            for (short m = 0; m < Ref.Value.Models.Length; m++) {
                if (Ref.Value.Models[m].ModelName == modelName) { return m; }
            }
            return -1;
        }

        // This function will return a list of all clips with names that match the specified text.
        // For example, if you search for "Run" in Dog{ Bark, Idle, RunFast, RunSlow }, Cat{ Meow, Attack, Run }, Bird { Fly, Tweet, Idle}
        // The function will returns the clips corresponding with: { -1, 2, -1 }, where each slot corresponds with a model index.
        // Note that entries are -1 if they don't have an entry corresponding exactly with "Run".
        // If model has two entries with the same clip name, only the first one will be used.
        // You must dispose the resulting list when you are done with it if you make it persistent.
        // This function is not very fast, so it's recommended that you run it once and then cache the result,
        // (which is fine because this data never changes at runtime).
        // To get information about the clip you can do:
        //    short modelIndex = db.FindModelIndex("Cat");
        //    short clipIndex = runClips[modelIndex];
        //    ClipData clip = db.GetModel(modelIndex).Clips[clipIndex];
        //    UnityEngine.Debug.Log($"Clip {clip.Name} is {clip.Length} seconds long.");
        // clipName --> a clip name to search for in all clip names for all models.
        // allocator --> the allocator to use for the returned list.
        // return --> an array containing the clip indexes.
        public NativeArray<short> FindClips(FixedString128Bytes clipName, Allocator allocator = Allocator.Persistent)
        {
            int modelCount = Ref.Value.Models.Length;
            NativeArray<short> arr = new NativeArray<short>(modelCount, allocator, NativeArrayOptions.ClearMemory);
            for (int m = 0; m < modelCount; m++) {
                arr[m] = Ref.Value.Models[m].FindClip(clipName);
            }
            return arr;
        }

        // Same as FindClips(), however, this version matches clips that partially or fully contains the specified text.
        // For example, if you search for "Run" in Dog{ Bark, Idle, RunFast, RunSlow }, Cat{ Meow, Attack, Run}, Bird { Fly, Tweet, Idle}
        // The function will returns the clips corresponding with: { 2, 2, -1 }, where each slot corresponds with a model index.
        // clipName --> a clip text to search for in all clip names for all models.
        public NativeArray<short> FindClipsThatContain(FixedString128Bytes clipText, Allocator allocator = Allocator.Persistent)
        {
            int modelCount = Ref.Value.Models.Length;
            NativeArray<short> arr = new NativeArray<short>(modelCount, allocator, NativeArrayOptions.ClearMemory);
            for (int m = 0; m < modelCount; m++) {
                arr[m] = Ref.Value.Models[m].FindClipThatContains(clipText);
            }
            return arr;
        }
    }

    public class AnimationDbAuthoring : MonoBehaviour
    {
        [Tooltip("Point this at an animation database scriptable object file.")]
        public AnimDbSo AnimDbScriptable;
    }

    public class AnimationDbBaker : Baker<AnimationDbAuthoring>
    {
        public override void Bake(AnimationDbAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new AnimDbRefData(authoring.AnimDbScriptable));
        }
    }
} // namespace