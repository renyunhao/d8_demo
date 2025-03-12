# AnimationCooker
AnimationCooker can be used to animate tens to hundreds of thousands of ECS entities by storing animation vertices in a texture which allows the skinned mesh to be treated as a static mesh where the animation is handled in the GPU - greatly reducing GPU usage.

There are two main types of texture based GPU animations that are being used today:
  1. *GPU bone skinning* involves baking bone data into a texture to animate bones via a shader or gpu compute buffer. This method taxes the GPU more because each vertex must be run through the bone matrix. Because it is only storing animation curves, texture sizes can be very small and vertex count is not an issue. An LOD system can be used to decrease the number of bones further away and alleviate some of the GPU taxation. It is difficult, but not impossible to develop animation blending with this technique.
  2. *Texture vertex baking* involves baking mesh vertex positions into textures. Because the texture size depends on the number of vertices in a mesh, this approach tends to have large textures, so vertex counts must be kept low to avoid too much memory use.  It has extremely low GPU and CPU overhead. With some compression techniques, texture sizes can be improved. This is the strategy that AnimationCooker uses.
  
See <a href="https://forum.unity.com/threads/dots-animation-options-wiki.1339196/" target="_blank">The DOTS Animation Options Wiki</a> for more information about animation options in DOTS.
  
The main advantage to these two GPU animation techniques is that they are blazing fast - you can animate tens to hundreds of thousands of meshes simultaneously. However, there are sacrifices that must be made in order to achieve this speed. One problem is that it's very difficult to do animation blending or events.  Another disadvantage is that it is difficult to attach and remove things like weapons and armor. Lastly, with baked vertex animation you cannot use meshes with a high number of vertices (it's recommended to keep them around 2k or less for mobile support).  If you want your characters and animations to look really beautiful up close and you don't need a boatload of instances, then this technique is probably not right for you.  However, if you are instantiating 10k+ instances of zombies at a time and you don't expect them to look amazing up close, then these limitations are likely a decent compromise.  When combined with LODs and culling, counts above 100k are possible and in fact the actual animation on CPU and GPU is going to be miniscule compared the high triangle count bottleneck that result from so many meshes.

Animation cooker currently requires ECS. It would be possible to create a small game-object based script to change animations and to add a switch on the gui to disable ECS component generation and attachment.

### **Features:**
- Works with Unity 6, URP 17, and DOTS 1.0. (Not tested with SRP or HDRP).
- Vertex animations are stored in a static mesh so that GPU instancing can be used, reducing CPU overhead.
- All animations are combined into the same texture.
- The textures are stored in ARGB32 (32 bit) buffers which are half the size of floating point textures. The vertexes and normals are stored as R11G10B11 (aka X11Y10Z11) format with auto-range-discovery to maximize precision.  Unpacking calculations are done without division to maximize speed.
- Linear interpolation between frames allows baking at lower frame-rates to keep textures small (saving memory).
- AnimationCooker is separated into Editor, Runtime, and ExampleScene so that you can easily add only the necessary components to your projects.
- The GUI has an auto frame-rate/texture-size optimization feature to make it easy to choose the right texture.
- There are options to reset root bone/scale and mesh rotation/scale on models before baking them.
- Bone-meshes are supported (though not thoroughly tested).
- Baking generates a C# database that can be used to access all clip information as well as enums for each clip.
- Automatically generates textures, and adjusted mesh, a material, and a prefab with ECS animation components on it.
- Performs wrapping in order to reduce texture widths and allow more vertices in situations like mobile phones that have a limitation at 1024 (old phones) or 2048. Wrapping calculation does not use division so it's super fast.
- Lit and Unlit shader. The Lit shader is derived from URP SimpleLit.
- Settings get saved per-prefab to make it easier to go back and change previous bakes.
- Authoring tool to store the animation database in a singleton blob-asset so it can be accessed via ISystem with Burst and Jobs
- Custom super-fast Level of Detail (LOD) System that avoids child-objects and allows different animation settings per LOD level. It works with most LOD generator packages. This feature is optional.
- Allows use of built-in Unity compressions schemes and formats **new as of 01/05/2024**

### **Installation:**

<b>Package Installation:</b>
	To install simply open your package manager, click the "+" button on the top-left, choose "Install Package from Git URL", paste in this URL: https://gitlab.com/lclemens/animationcooker.git , and then press the Install button.
	
![screenshot](Images~/package_manager.png){width=30%}
	
<b>Example Installation:</b>
	To install the example (optional), open your package manager, select Animation Cooker in the package list, select the <b>Samples Tab</b>, then Press the <b>Import</b> button.
	You will need to install the package for entites.graphics, render-pipelines.universal, and import TextMesh Pro.
	You will also need to install Unity Mesh Simplifier https://github.com/Whinarn/UnityMeshSimplifier.git via the package manager.
	![screenshot](Images~/samples_install.png){width=70%}

The repository's example relies on a free LOD generator called Unity Mesh Simplifier (MIT license), which is a dependency of that project.  This is only used during build - during runtime no LOD generators are required. LODs are totally optional - see below for more info.

The latest push in the master branch is for DOTS 1.0+ and URP 17. If you want to use entities 0.51 with URP 12, there is a tag for v0.51Final that should work, but it will be missing some newer features like LODs (note that it is not a package). If you want to use URP 14 you can also install the URP shaders from the Samples tab, which imports a zip file named "vtxanim-shader-urp14.zip" so you can replace the URP 17 shader/hlsl files.
If you want to make a shader for a different URP version (or perhaps make a Lit or HDRP version), you can copy the equivalent shader and HLSL files that make up the current AnimVtxSimpleLit shader and with a merge tool you could make some of the same modifications that I made. I can't guarantee it will work, but there's a decent chance of it. If you are successful, send me a copy and I can put them into repo for other people to share. If anyone knows a better way of setting up the shaders please let me know - It is super annoying having the shaders break every time I upgrade to a later URP version.

### **Configuration:**

![menu](Images~/menu.png){width=30%}

The Animation Kitchen is where you'll do your cooking and baking. To get started, use the Unity Menu Tools/Animation Kitchen. The other menu items are mainly for debugging and you probably won't use them.

![cooker](Images~/cooker.png)

- **Database SO:** This should point to a scriptable object that contains information about each model. There is usually only one scriptable object for all models. At runtime, this scriptable object will be converted to a blob asset that can be accessed in jobs/systems. Normally this would reside in the same directory as your baked output folder.
- **Playback Shader:** Baking will create a new material that will use this shader. The default is CookedAnimation/VtxAnimSimpleLit. AnimationCooker will attempt to give the new material similar shader settings to the original material.
- **Art Output Folder:** Set this to the folder where you want the resulting textures, materials, and meshes to be placed. It is relative to the Asset folder. The substring "{name}" will be replaced by the prefab name.
- **Prefab Output Folder:** Set this to the folder where you want the prefab to be placed. It is relative to the Asset folder. The substring "{name}" will be replaced by the prefab name.
- **Enum Output Folder:** Set this to the folder where you want the resulting animation enum file to be placed. There is only one enum file for all models. At runtime you can use these enums to easily access clips and to switch between them. The substring "{name}" will be replaced by the prefab name.
- **Source Prefab:** The models that you bake can be either in your assets folder or in the scene. Drag the object you want to bake into this field. It must have an Animation or an Animator attached to it. It must also have a skinned mesh renderer. WARNING - prefab names must be enum compatible.
- **Generate Enum Clips:** If this box is enabled, all your clip names will be declared as enums during the generation phase. This will allow you to access clips via enum values. It is not necessary however, because you can also access clips via a string in the database. See note below about enum naming.
- **Enable Log File:** Check this to output a log file of the bake. This is useful when you need to remember the parameters you originally used to bake. (default true)
- **Post Domain Reload:** This will disable reloading the domain after a bake. It greatly decreases bake time, however, if you changed some of the skin parameters the newest database C# file won't be reloaded, so animations may not work properly when pressing play until you reload the domain another way.
- **Copy Tex to Output:** Setting this to true will copy all source textures to the output directory (such as the alebedo color, normal maps, emmission maps, etc). This might be useful if you have only one texture file for each model. If your models are sharing a texture atlas, then you'll likely want this disabled. (default true)
- **Select All button:** This selects all animations.
- **Deselect All button:** This deselectes all but one animation (1 is required).
- **Reset Names button:** This resets the names of all animation clips to their default (from the animation componenent on the source prefab).
- **Refresh Prefab button:** In theory changes to the source prefab should automatically be reflected in the window. However, if for some reason the data doesn't look right you can press this button to have the source prefab rescanned.
- **Animation clips**: Deselect the animations that you want to ignore. The more animations you have, the more texture space will be required, so don't include any extra animations that you won't use.  Keeping animations short also helps.  You can also rename animations here if you wish. If you rename them, the generated database keys and enums will reflect those new names. The names you choose aren't saved permanently in the asset.  See the notes section below for info about choosing animation clip names.
- **Reset Position B4 Bake:** This toggles the reset position flag - see section entitled "Mesh/Vertex Alignment". (default false)
- **Reset Rotation B4 Bake:** This toggles the reset rotation flag - see section entitled "Mesh/Vertex Alignment". (default false)
- **Reset Scale B4 Bake:** This toggles the reset scale flag - see section entitled "Mesh/Vertex Alignment". (default false)
- **Enable Bone Adjust:** In some circumstances this might be necessary, but most of the time you can leave it disabled. (default false)
- **Ignore Bone Meshes:** When this is disabled, if there are meshes on each individual bone, they will be included in the baking process. If you enable this checkbox, it will cause all bone meshes to be ignored.
- **Use R11G10B11 Compression** Enabling this will give you the best possible compression for a 32 bit image. See the compression section below for more info. (default enabled)
- **Texture:** This will determine which textures are output during the bake process. For unlit shaders, you should only ever use Position. For lit shaders, it is common to output the normals as well. There is an option for tangent, but as far as I can tell, it's not used by anything. I recommend using Pos only for any LODs greater than zero.
- **Format:** This setting determines the frame rate and texture size of the resulting baked animation. The optimized values are auto-discovered so you don't have to waste time trying to figure out the best frame rate for each given texture size. Each rate listed corresponds to the rate that best fills each texture size so that you get the smoothest possible animation for the given texture size.  I find that for most animations, 6 to 12 FPS is the sweet-spot for tradeoff between visual quality and memory use. If the resulting texture's width is wider than 2048, a message will display stating that the texture won't play well on most mobile devices.  If you're targetting mobile devices, you should use modeling software like Blender to reduce the vertex count via a decimation algorithm or merging triangles manually.
- **Bake button:** Press this button to generate the baked output and update the animation database.  In most cases, the baking takes only a few seconds. However, during baking, the animation clip database is generated (a C# file), and Unity will want to recompile its assets. Refreshing assets takes several seconds. After the asset refresh, you will see "Asset recompilation finished." at the end of the Result text field.
- **Open Folder button:** Pressing this button will open the Bake Output Folder in the file explorer of whatever OS you are using.
- **Select in Project button:** This selects the baked prefab that was generated by the last bake operation.
- **Prediction:** This field displays information about the expected output.
- **Last Bake Result:** After you press the cook button, this field will display information about the resulting baked output.
- **Warnings:** If anything went wrong during the bake this field might contain some extra info.

### **How animations are played:**
The animations are played by the CPU in AnimationSystem (This method is currently ECS only).  Unlike GPU playback modes, this method allows you to know when clips end (which is important for stopping animations when they finish).  There is a very basic scheme to control animations via AnimationCmdData.  See the Code section below to learn how to use it. It currently does not have a state-machine type system or blending like Mechanim... it's very basic, but works well in many zombie-type games.

### **Mesh/Vertex Alignment:**
Unfortunately, I couldn't figure out a way to auto-determine if a particular mesh needs its positions, rotations, or scaling reset to zero before baking. There are three checkboxes: "Reset Position B4 Bake", "Reset Rotation B4 Bake", and "Reset Scale B4 Bake" that will toggle the flags that reset to origin/defaults before baking.  Some models+animations will require a specific combination of the two boxes to be checked, while others will work regardless of what you check or uncheck. When you select your baked prefab in the heirarchy, there will be a red outline of the mesh.

Additionally, it is important to make sure that the **root bone** is selected on your skinned mesh renderer.  Sometimes it is difficult to tell which bone is meant to be root bone for a particular model. I have found that a good way to determine which bone to use is by dragging the model/fbx into the scene - the root bone usually automatically gets set.  

![rootbone](Images~/rootbone.png)

I find that the best way to work with models is by turning the model/fbx into a prefab - if it is a legacy animation it will automatically add the Animation component and fill it out for you. If it's an animation with a controller it will automatically add an Animator component and then you can set the controller for it.

Here is an example of an incorrectly aligned model due to rotation. Notice how the red outline doesn't match what the shader is drawing:

![mismatch](Images~/mismatch.png)

And here is the same model after toggling the "Reset Rotation B4 Bake"

![aligned](Images~/aligned.png)

Note that the alignment won't always be 100% exact - sometimes the current pose in editor won't match the outline, but it should look fine in-game.

If neither of the approaches above work, you may have to manually adjust the skinned mesh renderer's transform.  Find the skinned mesh renderer in your heirarchy and tweak its position/rotation/scale parameters such that the outline matches.  Often the numbers can be adjusted to be similar to one of the bones in the armature heirachy.

![aligned](Images~/skin_mesh_transform.png){width=50%}

So to sum it up... Bake your animations. If the red mesh outline doesn't match try:
<ul>
   <li>Ensure that the root bone is set.</li>
   <li>Try toggling the reset before bake options.</li>
   <li>Try manually adjusting the transform values on the child gameobject that holds the skinned mesh renderer.</li>
</ul>

### **Multiple Skins:**
If your prefab has multiple skinned mesh renderers on it, the baking may still work but the result will be multiple textures and skins. Normally it is more efficient and convenient to have only a single skin. AnimationCooker does not have the algorithms for skinned mesh combining. There are several tools in the Unity asset store that can do it. You could probably find some open source ones as well.

### **Notes:**
- **Unique names - IMPORTANT:** The prefabs/models that you bake should have unique names. They are used as keys in a dictionary, so if you were to bake two models that both had the exact same name, the second one's settings would overwrite the first.
- **Declare Clip Enums** If you enabled the setting to declare animation enum clips, then be aware that the enums generated may not match your string exactly. An attempt will be made to rename the enum such that it doesn't violate any C# rules.  There are no guarantees, however, so use at your own risk. I suggest using simple names like "Walk", "Idle", or "Run". You should be careful because if your enums need to be renamed, then you should rebake to make sure the strings match the database.
- **Bilinear Filtering:** It is very important that your textures (position and normal) do not have a bilinear filter enabled (which is the default).  The baking script disables the bilinear filter after it builds the textures, but if for some reason you reimport you might lose that setting. You can change the "Filter Mode" field in the inspector tab by selecting the texture in the Project tab.  Set it to "Point".  Whenever the bilinear filter is on, you'll noticed that the first and last animated frames will be garbage.
**Mutiple Materials** - If your Source Prefab has multiple materials, the result will only use one of the materials. To fix this you will need to merge the materials into one. There are several assets in the Unity Asset store that can do this and a few opensource tools.

![texprops](Images~/tex_props.png)

### **LODs:**
If you don't already know, LOD means Level of Detail and is a way of swapping out high quality models for lower quality ones at far distances. It is not necessary for you to use LODs, however, it's highly recommended in zombie-hoard type games because even with say 2000k polygons, if you have 100k entities, that's 200 million vertices which will bring even the best of graphics cards to its needs. Even if you're only doing 10k animations, it's still useful to free the GPU so it can render other things.  When you have that many animated entities on screen, it's impossible for the player to notice that waaaay off in the distance some of the models have less triangles and are playing animations at a lower quality.

If the source prefab has an LODGroup on it, AnimationCooker will read the LOD info and make use of it.  Thus, you have many options. Most of the LOD generator assets on the asset store will work as long as they aren't the runtime-only style and support skinned mesh simplification.  Included in the example project is an unmodified version of [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier/). NanoLOD is another free one that works well, however it has less control over quality and it generates warnings in the editor.  There are at least 10 or more packages in the asset store and I'm sure there are a lot more open-source ones.

To use UnityMeshSimplifier, add a LODGeneratorHelper component to the Source Prefab, tweak the settings, and then press "Generate LODs".

Animation Cooker does NOT use the standard Unity LOD runtime system - rather it uses a much faster one called SimpleLod. The Unity ECS LOD system currently is quite slow, and on top of that, it forces you to use child entities for each LOD, which causes the transform system to use a lot of CPU.  In a test with 100k entities, SimpleLod is 5x faster, and if you count the extra overhead from transforms, it is 15x faster.  It does not support transitions, however, currently Unity LOD does not support them either.

Once you bake an animation, the resulting baked prefab will have an LODGroup component on it with the same settings as the Source Prefab's LODGroup. You can tweak settings easily on the baked prefab and test, however, be aware that baking will reset those settings, so you may want to write them down or mirror them to the source prefab in case you need to rebake.

![texprops](Images~/lods.png)

This is an example of a source prefab that uses Unity Mesh Simplifier. Again, you are not limited to just this tool - you can use any LOD generator you want.

NOTE - At the moment only 3 levels of LOD are supported. I may add support for more later, but typically 3 is enough.

It is recommended that you don't use Normal textures on any skins other than zero. They are typically far enough away that players won't notice.

RUNTIME - At runtime, your scene needs a singleton in it to configure LOD parameters. Simply add a SimpleLodOptionsAuthoring component to a game-object in the scene. It can be the same singleton that you add AnimationDbAuthoring to.

### **Material Settings:**

![lit shadersettings](Images~/litshader_settings.png){width=50%}

When baking, AnimationCooker will attempt to setup the resulting material so that the shader options match the original material - so for example if you setup a bump map in the original material, it will also be set in the generated material.  You can change these at runtime, if for example, you want to modify the animation speed.

- **\_PosMap**: Texture that holds vertex position and clip information. This will be generated in your output folder.
- **\_NmlMap**: Texture that holds normals. This will be generated in your output folder.
- **\_TanMap**: Texture that holds tangents. This will be generated in your output folder. (not normally used).
- **\_Shift**: This contains the current time and the begin/end frame offset. These values are set by the AnimationSystem at runtime.
- **\_Stat1, \_Stat2, \_Stat3** These 3 properties contain various info about each texture. This info is filled out by the baking process so you should not need to modify it.
- **\_UseNormalMap:** Selecting this will put in a compiler directive to use the normal map. (default - enabled)
- **\_UseTangentMap:** Selecting this will put in a compiler directive to use tangent maps. (default - disabled)
- **Enable GPU Instancing**: I'm not really sure if this checkbox makes a difference because whenever I have it checked and display a bunch of entities, "Saved by Batching" is still zero, and it doesn't seem to make a difference in performance.  I believe that instancing is always enabled whether or not this is checked. ¯\\_(ツ)_/¯

Output folder:

![bakedfiles](Images~/baked_files.png){width=50%}

- **AnimDbData.asset** is a scriptable object that is auto-generated and updated whenever you press the "Bake" button. It contains information about each model and clip.  There currently isn't a way to remove items, but it doesn't really hurt to have unused items in there (for example, if you baked a model but no longer use it). If for some reason the file grows really huge with unwanted data you could manually delete entries.  Whenever the subscene is baked, the AnimationDbAuthoring script will convert this database into a blob asset that you can access in your bursted systems/jobs (typically via SystemAPI.GetSingleton()).
- **AnimEnums.cs** is an auto-generated C# file that contains definitions for the models and animation clips.
- The **.mat.asset** file contains the material that points to the texture files and vertex animation playback shader.
- The **.normTex.asset** and **.posTex.asset** files are the normal and position textures, respectively.
- The **.prefab** file holds a prefab that is already setup for ECS use.
- The **.mesh.asset** mesh is nearly identical to the original mesh, but it won't have bone information and its coordinates and rotations will be reset to defaults.

### **Runtime Code:**
The most important thing is that each animatable entity has an **AnimationModelAuthoring** component added to its gameobject (this is automatically added to the baked prefab created when you press the "Bake" button). If you're creating your objects in Pure ECS instead, look at the code inside of **AnimationModelAuthoring**.  The authoring class adds (and populates) an **AnimationStateData** component that holds current animation state data.  It also adds some other useful components.

If your bake includes LODs, then a **SimpleLodGroupAuthoring** script will be added to the baked prefab. This will allow the SimpleLod systems to act on it and perform LOD functions.  Typically it does not need to be configured. You can use the standard unity LODGroup even though it is disabled to tweak the settings.  A LODGroup component will also be on the baked prefab, and you can adjust its values as you want. Whenever the subscene is baked, these values will be used.

Animation Clip Converter (Authoring Component):

![converter](Images~/converter.png){width=50%}

To get details about clips and models at runtime, you have two choices. If you want to access the info from bursted systems and jobs, you can use the blob asset with something like job.AnimDb = SystemAPI.GetSingleton\<AnimDbRefData\>(); . This singleton is created via AnimationDbAuthoring which should be placed on an object in the subscene. If you want to access the info from a monobehavior, you can either use the blob asset or simply read the scriptable object directly (note - you should never modify the SO). You can see the example scene for uses of this.

``` csharp
[BurstCompile]
public partial struct MyJob : IJobEntity
{
    [ReadOnly] public AnimDbRefData Db;
	
    // matches all animation entities in the scene
    [BurstCompile]
    public void Execute(ref AnimationCmdData cmd, in AiActionData action, in AnimationStateData state)
    {
        // fetch the currently playing clip
        AnimDbEntry clip = Db.GetClip(state.ModelIndex, state.CurrentClipIndex);
        UnityEngine.Debug.Log($"The current model {clip.ModelName} is playing the {clip.ClipName} clip which is {clip.GetLength()} seconds long.");
    }
}

[BurstCompile]
public partial struct MySystem : ISystem
{
    void OnCreate(ref SystemState state)
    {
        // ensures that animation database singleton exists before calling update
        state.RequireForUpdate<AnimDbRefData>();
    }
	
    [BurstCompile]
    void OnUpdate(ref SystemState state)
    {
        MyJob job = new MyJob();
        job.Db = SystemAPI.GetSingleton<AnimDbRefData>(); // fetch the animation database singleton
        job.ScheduleParallel();
    }
}
```

You change the current clip by setting a command.  
- PlayOnce will play the clip once and then go back to playing the forever-clip (this is useful for actions like Attack, Stagger, or Jump).
- SetPlayForever will set the forever-clip (this is useful for actions like Idle, Run, or Walk).
- PlayOnceAndStop will play the specified clip and then stop the play loop (this is useful for death animations).
Here's an example:

``` csharp
EntityManager.SetComponentData<AnimationCmdData>(entity, new AnimationCmdData { Cmd = AnimationCmd.SetPlayForever, ClipIndex = (byte)AnimDb.Horse.Idle });
```

Here's how you could double the animation playback speed:

``` csharp
EntityManager.SetComponentData<AnimationSpeedData>(entity, new AnimationSpeedData { PlaySpeed = 2f });
```

### **Linear Interpolation:**
Here is the original animation at 30fps:

![30fps](Images~/30fps.gif){width=30%}

Here it is at 11fps without interpolation:

![10fps](Images~/10fps.gif){width=30%}

Here it is at 11fps with interpolation:

![10fps](Images~/10fps_interpolated.gif){width=30%}

The 11fps version is 3,076KB vs 12,292KB for the 30fps one, so it's about 1/4 the size.

### **Example Scene:**

![fullwindow](Images~/full_window.png)

The example scene contains a small UI and a spawner script so you can spawn a large number of entities in a grid.

The Spawner script will query for any entities in the scene that have an AnimationStateData attached to it (which is automatically added to your baked prefab by the baking/cooking script.)  It will spawn all found entities in equal numbers and cycle through the animations for each one.

### **Compression:**
By default, the R11G10B11 compression scheme is used and it is built into the shader. In my opinion it has the best quality/size tradeoff such that X and Z use 11 bits (2048 possible values) and Y uses 10 bits (1024 values). However, you are free to experiment with other compression schemes. To do so, disable the Use R11G10B11 Compression toggle and rebake the animation. Then in the output directory, find the .posTex file and click on it and in the inspector at the bottom you'll see a format section.  You can mess around with these values. The inspector will tell you the resulting size, however, be aware that it may not give the true GPU size -- for example most GPUs cannot handle RGB24 and they internally convert them to RGBA32, thus the size you will see in the inspector is NOT the actual size that the data will take in most GPUs.  Currently there is no interface in the baker to tweak these compression schemes so you'll need to do it after baking. Maybe sometime in the future I'll add those options, but as I said, so far I haven't found any great reason to use something other than R11G10B11. The compression schemes that look equally as good are larger, and the ones that are smaller look absolutely horrible.

![fullwindow](Images~/compression.png)

If you want to learn more about my experimentation results with different compressions, see this post: https://forum.unity.com/threads/graphics-drawmeshinstanced.537927/page-3#post-9544825 

### **For reference:**
In case you want to understand the position texture format, here is the pixel diagram of a simple example with 3 animation clips.
![example texture format](Images~/example_tex_fmt.png)

### **TODO:**
1. A GameObject based animation script might be useful for people who aren't using ECS. It should be pretty easy to do by using the animation database which is already generated and easy to use. This is low on my priority list since I won't really need it.
2. Add basic animation blending support?
3. It would be nice to save the LOD settings (Transition % Screen Size, Object Size) in the log file.
4. Precalculate distances in the CameraDataSystem to avoid the divide and tangent calculation at runtime. (low priority)
5. Add options to disable shadows and maybe a few other material settings on low quality LODs.
6. Make the LODs match the game object LODs at the same distance.

### **Credits and resources:**
- The guys in this thread: https://forum.unity.com/threads/graphics-drawmeshinstanced.537927/ (zulfajuniadi, Arathorn_J, elJoel, DreamingImLatios, jdtec, and Shane_Michael)
- TheLordBaski's video tutorial: https://www.youtube.com/watch?v=KUuuXowdYyM&ab_channel=TheLordBaski
- Jiadong's coding blog: https://www.cnblogs.com/murongxiaopifu/p/7250772.html
- The programmersought article: https://www.programmersought.com/article/5083122695/
- Andre Sato's article: https://medium.com/tech-at-wildlife-studios/texture-animation-techniques-1daecb316657
- zulfajuniadi's Animation Texture Baker (dev branch) (forked from sugi-cho): https://github.com/zulfajuniadi/Animation-Texture-Baker
- sugi-cho's Animation Texture Baker: https://github.com/sugi-cho/Animation-Texture-Baker
- bgolus: https://forum.unity.com/members/bgolus.163285/
- gussk (Gustav): https://forum.unity.com/members/gussk.114410/
- DreamingImLatios on LODs: https://forum.unity.com/threads/how-to-implement-megacity-lods-and-culling-system.1334756/#post-9160781
- WildMan: https://forum.unity.com/members/wildman.185896/

### <a href="license.md" target="_blank">**Software License**</a>
