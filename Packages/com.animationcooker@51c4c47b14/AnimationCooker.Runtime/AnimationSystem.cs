// If this system is enabled, it will animate the vertexes of any entity that has AnimationStateData, AnimationCmdData, and the time and speed properties.
//
// To change an animation at runtime, set the values in AnimationCmdData to get your desired animation.
//
// Note - This system requires an AnimDbRefData singleton in the scene.
//        Place an AnimationDbAuthoring component onto a gameobject in the subscene to create this singleton.
//
// I attempted to make this system entirely independent of LODs by storing the current skin index as a material property.
// Unfortunately, there are ways to WRITE to a property in a bursted job (Material Property Override), but there is no way to READ them!
// So the best solution I found was to have the LOD system update a SimpleLodSkinData component whenever it swaps material/mesh using MaterialMeshInfo.
// This SimpleLodSkinData component may or may not be on the entity depending on whether the particular model has LODs.
// Because it's optional, I could either use a component-lookup which is about 2x slower, or I could use two systems.
// So I chose to use two systems, which is verbose and lame, but screw cutting speed in half.
// It's not as good as I would like, but it's the best way I could do it without using non-burst sharedMaterial.GetFloat().
//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

using Unity.Entities; // for SystemBase
using Unity.Burst;
using Unity.Jobs;

namespace AnimCooker
{
    [BurstCompile]
    [WithNone(typeof(SimpleLodSkinData))]
    public partial struct AnimationJobWithoutLod : IJobEntity
    {
        public float DeltaTime;
        public AnimDbRefData AnimDb;
        public void Execute(ref MatPropShift shiftProp, ref AnimationStateData state, ref AnimationCmdData cmd, ref AnimationSpeedData speed)
        {
            AnimSysUtils.HandleAnimation(0, DeltaTime, ref AnimDb.GetModel(state.ModelIndex), ref shiftProp, ref state, ref cmd, ref speed);
        }
    }

    [BurstCompile]
    public partial struct AnimationJobWithLod : IJobEntity
    {
        public float DeltaTime;
        public AnimDbRefData AnimDb;
        public void Execute(ref MatPropShift shiftProp, ref AnimationStateData state, ref AnimationCmdData cmd, ref AnimationSpeedData speed, in SimpleLodSkinData skin)
        {
            AnimSysUtils.HandleAnimation(skin.SkinIndex, DeltaTime, ref AnimDb.GetModel(state.ModelIndex), ref shiftProp, ref state, ref cmd, ref speed);
        }
    }

    [RequireMatchingQueriesForUpdate]
    public partial struct AnimationSystem : ISystem
    {
        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimDbRefData>();
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            AnimationJobWithoutLod jobWithout = new AnimationJobWithoutLod();
            AnimationJobWithLod jobWith = new AnimationJobWithLod();
            jobWith.AnimDb = jobWithout.AnimDb = SystemAPI.GetSingleton<AnimDbRefData>();
            jobWith.DeltaTime = jobWithout.DeltaTime = SystemAPI.Time.DeltaTime;
            JobHandle jhWith = jobWith.ScheduleParallel(state.Dependency);
            JobHandle jhWithout = jobWithout.ScheduleParallel(jhWith);
            state.Dependency = JobHandle.CombineDependencies(jhWith, jhWithout);
        }
    }

    // common static functions
    static class AnimSysUtils
    {
        public static void HandleAnimation(short skinIdx, float DeltaTime, ref ModelData model, ref MatPropShift shiftProp, ref AnimationStateData state, ref AnimationCmdData cmd, ref AnimationSpeedData speed)
        {
            if (cmd.Cmd == AnimationCmd.PlayOnce) {
                // we received a play once command, so change the clip and set the state mode.
                state.Mode = AnimationPlayMode.PlayOnce;
                shiftProp.Shift.x = 0f; // start playing the new clip at the beginning
                state.CurrentClipIndex = cmd.ClipIndex;
                state.LastPlayedClipIndex = cmd.ClipIndex;
                if (cmd.Speed > 0f) { speed.PlaySpeed = cmd.Speed; }
                cmd.Cmd = AnimationCmd.None; // indicates command has been processed
            } else if (cmd.Cmd == AnimationCmd.SetPlayForever) {
                // we received a command to change the play-forever clip, so change state to reflect it.
                state.ForeverClipIndex = cmd.ClipIndex;
                // start playing the new forever clip IF state is currently playing the forever clip
                if (state.Mode == AnimationPlayMode.PlayForever) {
                    state.CurrentClipIndex = cmd.ClipIndex;
                    shiftProp.Shift.x = 0f;
                }
                cmd.Cmd = AnimationCmd.None; // indicates command has been processed
            } else if (cmd.Cmd == AnimationCmd.PlayOnceAndStop) {
                // we recieved a play-once-and-stop command, so start a play-once-and-stop operation
                shiftProp.Shift.x = 0f;
                state.CurrentClipIndex = cmd.ClipIndex;
                state.LastPlayedClipIndex = cmd.ClipIndex;
                state.Mode = AnimationPlayMode.PlayOnceAndStop;
                if (cmd.Speed > 0f) { speed.PlaySpeed = cmd.Speed; }
                cmd.Cmd = AnimationCmd.None; // reset (cmd processed)
            } else if (cmd.Cmd == AnimationCmd.Stop) {
                // we received a stop command, so reset the command
                state.Mode = AnimationPlayMode.Stopped;
                cmd.Cmd = AnimationCmd.None; // indicates command has been processed
            } else if (state.Mode != AnimationPlayMode.Stopped) {
                // no command was sent, so this is where increment the time
                float endTime = model.Skins[skinIdx].GetClipLength(state.CurrentClipIndex);// / state.PlaySpeed;
                float shiftAmt = DeltaTime * speed.PlaySpeed;
                if ((shiftProp.Shift.x + shiftAmt) >= endTime) { // if clip finished playing
                    if (state.Mode == AnimationPlayMode.PlayForever) {
                        shiftProp.Shift.x = 0f; // reset to beginning
                    } else if (state.Mode == AnimationPlayMode.PlayOnce) {
                        // transition back to forever mode
                        shiftProp.Shift.x = 0f; // show first frame of the forever clip
                        state.Mode = AnimationPlayMode.PlayForever;
                        state.CurrentClipIndex = state.ForeverClipIndex;
                    } else if (state.Mode == AnimationPlayMode.PlayOnceAndStop) {
                        state.Mode = AnimationPlayMode.Stopped;
                        shiftProp.Shift.x = endTime;
                    }
                }

                // if not stopped, increment the timer and set the _Shift material property
                if (state.Mode != AnimationPlayMode.Stopped) {
                    shiftProp.Shift.x += shiftAmt;
                    shiftProp.Shift.y = model.Skins[skinIdx].SkinClips[state.CurrentClipIndex].BeginFrame;
                    shiftProp.Shift.z = model.Skins[skinIdx].SkinClips[state.CurrentClipIndex].EndFrame;
                }
            } // else { } - no command and animation is stopped.
        }
    }
} // namespace