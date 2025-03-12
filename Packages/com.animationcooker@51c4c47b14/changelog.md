<h1>AnimationCooker Changelog</h1>


DefaultCommand

Version 1.0.10
<ul>
	<li>Added DefaultCommand to AnimationModelAuthoring.</li>
</ul>
Version 1.0.9
<ul>
	<li>Added three clip text search functions to AnimDbSo.cs.</li>
	<li>Made some minor tweaks to burst-compile for jobs/systems.</li>
</ul>
Version 1.0.8
<ul>
	<li>ExampleScene - Added scene lighting files because the ambient lighting looked terrible.</li>
	<li>Modified the importer settings to enable read/write and override the compression format (fixed release build problems).</li>
</ul>
Version 1.0.7
<ul>
	<li>Fixed a bug in the DepthBuffer pass that caused a messy semi-transparent ghost around the objects.</li>
</ul>
Version 1.0.6
<ul>
	<li>Changed the default paths in the AnimationKitchenWindow.</li>
	<li>Added the ability to use {name} to specify the prefab name in paths.</li>
	<li>Fixed this weird giant transparent ghosting effect in the shader by adding VtxAnimSimpleLitDepthNormalsPass.hlsl.</li>
	<li>Added a try/catch in Bake() function to ensure that the progress bar goes away if there is an error.</li>
	<li>Added a check in the Bake() function to ensure that prefab names are enum compatible.</li>
</ul>
Version 1.0.5
<ul>
	<li>Fixed the camera controller in the example scene to support the new input system if it's in use.</li>
</ul>
Version 1.0.4
<ul>
	<li>URP 17.0.3 broke the shaders. I had to merge changes with the URP SimpleLit shader files.</li>
</ul>
Version 1.0.3
<ul>
	<li>Added the UMS_LODs folder to Samples and regenerated the prefabs.</li>
	<li>AnimationKitchenWindow.cs - Added a separate field to output the baked prefab to a separate folder if desired.</li>
	<li>Moved the prefab outputs up a directory.</li>
</ul>
Version 1.0.2
<ul>
	<li>Moved Resources folder from AnimationCooker.Resources to AnimationCooker.Runtime.</li>
</ul>
Version 1.0.1
<ul>
	<li>Various small package fixes.</li>
	<li>Updated readme to match the packaged version.</li>
</ul>

Version 1.0.0
<ul>
	<li>Converted the repository to a package.</li>
</ul>