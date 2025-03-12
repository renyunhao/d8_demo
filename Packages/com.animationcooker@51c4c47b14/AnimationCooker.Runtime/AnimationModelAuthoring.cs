// Attaching this class to a gameobject that is going to be converted to an entity
// will cause the resulting entity to have the necessary components to animate it.
// You can then use those components in your systems to change the animation or get animation info.
// Whenever the animation is baked, the values here will be filled out, however,
// you may choose to override the default clip name or default play speed.
// The same gameobject should also have mesh-renderer and a mesh/mesh-filter components on it,
// where the mesh-renderer uses the baked material that has the vtxanim shader.
//--------------------------------------------------------------------------------------------------//

using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace AnimCooker
{
	public class AnimationModelAuthoring : MonoBehaviour
	{
		[Tooltip("The name of the model. The baker will set this automatically and you shouldn't change it.")]
		public string AnimationModelName; // example: "Horse", "Metalon"

		[Tooltip("The default clip to play on repeat. The baker will choose the first clip found.")]
		public string DefaultClipName = "Idle"; // usually "Idle"

		[Tooltip("The default animation play speed multiplier. 0.5 is half speed, 2 is double speed. (default 1.0)")]
		public float DefaultPlaySpeed = 1.0f;

		[Tooltip("The default animation command. (default SetPlayForever)")]
		public AnimationCmd DefaultCommand = AnimationCmd.SetPlayForever;

		[Tooltip("Points to the animation database scriptable object. The baker sets this automatically and you shouldn't change it.")]
		public AnimDbSo AnimationDb;
	}

	public class AnimationClipBaker : Baker<AnimationModelAuthoring>
	{
		public override void Bake(AnimationModelAuthoring authoring)
		{
			if (authoring.AnimationDb == null) { UnityEngine.Debug.LogWarning($"Warning! AnimationDb is null for authoring component {authoring.AnimationModelName}."); return; } // nothing to bake
			short modelIndex = authoring.AnimationDb.FindModelIndex(authoring.AnimationModelName);
			if (modelIndex < 0) { UnityEngine.Debug.LogWarning($"Warning! Model: {authoring.AnimationModelName} was not found in the database."); return; } // nothing to bake
			ModelEntry model = authoring.AnimationDb.GetModel(modelIndex);

			// make an attempt to find the default clip and make that the default "forever" clip index
			short defaultClipIndex = model.FindClipIndexThatContains(authoring.DefaultClipName);
			if (defaultClipIndex < 0) { defaultClipIndex = 0; } // default to zero if clip not found

			Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
			// add some per-instance components that can be used to set where the play loop starts and ends.
			float beginFrame = model.Skins[0].SkinClips[defaultClipIndex].BeginFrame;
			float endFrame = model.Skins[0].SkinClips[defaultClipIndex].EndFrame;
			AddComponent(entity, new MatPropShift { Shift = new float4(0, beginFrame, endFrame, 0) });
			AddComponent(entity, new AnimationStateData { CurrentClipIndex = defaultClipIndex, ForeverClipIndex = defaultClipIndex, Mode = AnimationPlayMode.PlayForever, ModelIndex = modelIndex, LastPlayedClipIndex = 0 });
			AddComponent(entity, new AnimationCmdData { ClipIndex = defaultClipIndex, Cmd = authoring.DefaultCommand, Speed = authoring.DefaultPlaySpeed });
			AddComponent(entity, new AnimationSpeedData { PlaySpeed = authoring.DefaultPlaySpeed });
		}
	}
} // namespace