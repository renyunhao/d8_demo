// This class creates a window in the unity editor used for baking vertex animation textures.
//--------------------------------------------------------------------------------------------------//

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnimCooker
{
    public class BakeAndSaveResult
    {
        public BakeResult BakeRes;
        public SaveResult SaveRes;
    }

    public class AnimationKitchenWindow : EditorWindow
    {
        AnimDbSo m_animDbSo;
        string m_animDbSoPath;

        // GUI - set by user - the prefab with animation clips, at least one skin mesh renderer, and possibly lods.
        GameObject m_prefab;

        //// GUI - set by the user - subscene where baked prefabs are placed.
        //Unity.Scenes.SubScene m_subScene;
        //// holds the subscene name (since it can't always be accessed).
        //string m_subSceneName;

        // GUI - the folder where the baked stuff should be placed.
        string m_artOutputFolder = "Samples/ExampleScene/Baked/{name}";

        // GUI - the folder where the baked stuff should be placed.
        string m_prefabOutputFolder = "Samples/ExampleScene/Baked";

        // GUI - the folder where the output script will be generated.
        string m_generatedScriptOutFolder = "Samples/ExampleScene/Baked";

        // GUI - displays the text with info about the predicted output .
        string m_predictionText = "";

        // GUI - the folder where post-bake summary is placed (most recent bake operation).
        string m_lastBakeText = "";

        // GUI - holds the current warning text (if any).
        string m_warningText = "";

        // the animation database file name.
        //string m_animationFileName = "AnimDb.cs";

        string m_enumFileName = "AnimEnums.cs";

        // GUI - the playback shader that the operator wants to be attached to the output material and prefab. 
        [SerializeReference]
        Shader m_playbackShader; 

        // GUI when set to false, the bake button will be disabled
        bool m_bakeButtonEnabled = false;

        // GUI when set to true, the frame-rate combo will be disabled.
        bool m_formatComboEnabled = false;

        // Holds a list of the 3 loaded computation shaders
        // (each slot corresponds with VtxAnimTexType)
        List<ComputeShader> m_computeShaders = new List<ComputeShader>();

        // Holds a list of skin informations (and the skin references)
        // If there are LODs, each skin corresponds with an LOD.
        // Note - this may not match the previously saved settings.
        List<SkinInfo> m_skinInfos = new List<SkinInfo>();

        // these variables are used in detecting when the editor finishes recompiling
        // (we force a recompile because baking auto-generates AnimDb.cs)
        static bool m_justRecompiled = false;

        // This flag is used when polling to see when a domain reload has finished.
        bool m_isWaitingForRecompile = false;

        // This holds all the current bake options (passed to the Bake function)
        BakeOptions m_opts = new BakeOptions();

        // This flag is used when the scene is first enabled to avoid doing unnecessary stuff.
        bool m_isInitialized = false;
    
        // true during baking
        bool m_isBaking = false;

        GameObject m_bakedPrefab = null;

        // static constructor
        static AnimationKitchenWindow()
        {
            m_justRecompiled = true;
        }

        // This function is responosible for putting the window in the menu and showing it.
        [MenuItem("Tools/Animation Kitchen")]
        static void Initialize()
        {
            // Get existing open window or if none, make a new one:
            AnimationKitchenWindow window = (AnimationKitchenWindow)EditorWindow.GetWindow(typeof(AnimationKitchenWindow), false, "Animation Kitchen");
            window.Show();
        }

        // Called on domain reload, when the window first displays, and a few other scenarios.
        void OnEnable()
        {
            if (RefreshPrefab()) { 
                LoadSpecificSettings(m_prefab.name);
                RefreshPredictions();
            }

            // Whenever the window is first displayed, we'll restore previous settings.
            if (m_isInitialized) { return; }

            // load the computer shaders.
            m_computeShaders.Add((ComputeShader)Resources.Load("VtxAnimTextureGenPos"));
            m_computeShaders.Add((ComputeShader)Resources.Load("VtxAnimTextureGenPosNml"));
            m_computeShaders.Add((ComputeShader)Resources.Load("VtxAnimTextureGenPosNmlTan"));
            if (m_computeShaders[0] == null) { UnityEngine.Debug.Log($"Couldn't find compute shader: VtxAnimTextureGenPos"); }
            if (m_computeShaders[1] == null) { UnityEngine.Debug.Log($"Couldn't find compute shader: VtxAnimTextureGenPosNml"); }
            if (m_computeShaders[2] == null) { UnityEngine.Debug.Log($"Couldn't find compute shader: VtxAnimTextureGenPosNmlTan"); }

            LoadWindowSettings(); // should only ever get called once
        
            m_isInitialized = true;
        }

        private void OnDestroy()
        {
            SaveWindowSettings();
        }

        // This gets called whenver the GUI needs to be refreshed.
        // It is where we draw all of the controls.
        void OnGUI()
        {
            // scriptable object database
            EditorGUI.BeginChangeCheck();
            m_animDbSo = EditorGUILayout.ObjectField(new GUIContent("Database SO", "The scriptable object database - drag-and-drop it here."), m_animDbSo, typeof(ScriptableObject), true) as AnimDbSo;
            if (EditorGUI.EndChangeCheck()) { OnPrefabChanged(); }

            // playback shader
            EditorGUI.BeginChangeCheck();
            m_playbackShader = EditorGUILayout.ObjectField(new GUIContent("Playback Shader", "The shader that will be used for the material that gets generated. (default AnimationCooker/VtxAnimUnlit)"), m_playbackShader, typeof(Shader), true) as Shader;
            if (EditorGUI.EndChangeCheck()) { OnPrefabChanged(); }

            // art output folder
            EditorGUI.BeginChangeCheck();
            m_artOutputFolder = EditorGUILayout.TextField(new GUIContent("Art Output Folder", "The directory where all the generated assets will be placed (default ExampleScene/Baked)"), m_artOutputFolder);
            if (EditorGUI.EndChangeCheck()) { SaveWindowSettings(); }

            // prefab output folder
            EditorGUI.BeginChangeCheck();
            m_prefabOutputFolder = EditorGUILayout.TextField(new GUIContent("Prefab Output Folder", "The directory where the final baked output prefab will be placed (default ExampleScene/Baked)"), m_prefabOutputFolder);
            if (EditorGUI.EndChangeCheck()) { SaveWindowSettings(); }

            // generated script output folder
            EditorGUI.BeginChangeCheck();
            m_generatedScriptOutFolder = EditorGUILayout.TextField(new GUIContent("Enum Output Folder", "The directory where generated enums will be placed (default ExampleScene/Baked)"), m_generatedScriptOutFolder);
            if (EditorGUI.EndChangeCheck()) { SaveWindowSettings(); }

            //// subscene
            //EditorGUI.BeginChangeCheck();
            //m_subScene = EditorGUILayout.ObjectField(new GUIContent("Subscene Object", "Optional - If the 'Subscene Object' field is not null, the output prefab will be placed under the subscene. (default null)"), m_subScene, typeof(Unity.Scenes.SubScene), true) as Unity.Scenes.SubScene;
            //if (EditorGUI.EndChangeCheck()) { OnSubsceneChanged(); }

            // source prefab field
            EditorGUI.BeginChangeCheck();
            m_prefab = EditorGUILayout.ObjectField(new GUIContent("Source Prefab", "The prefab that contains the animations - you can drag-and-drop it here."), m_prefab, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck()) { OnPrefabChanged(); }

            GUILayout.Space(8);

            // first window settings checkbox row
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.EnableEnumDeclaration = EditorGUILayout.Toggle(new GUIContent("Generate Clip Enums", "If this is enabled, the output database will attempt to create enums for clip types. (default true)"), m_opts.EnableEnumDeclaration);
            m_opts.EnableLogFile = EditorGUILayout.Toggle(new GUIContent("Enable Log File", "Outputs a log file to the output folder. (default true)"), m_opts.EnableLogFile);
            if (EditorGUI.EndChangeCheck()) { SaveWindowSettings(); }
            EditorGUILayout.EndHorizontal();

            // second window settings checkbox row
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.EnablePostDomainReload = EditorGUILayout.Toggle(new GUIContent("Post Domain Reload", "If false, when baking is finished the domain won't reload - faster, but won't reload the anim database. (default true)"), m_opts.EnablePostDomainReload);
            m_opts.EnableCopyTexturesToOutput = EditorGUILayout.Toggle(new GUIContent("Copy Tex to Output", "If enabled, copies texture source files to the output folder. (default true)"), m_opts.EnableCopyTexturesToOutput);
            if (EditorGUI.EndChangeCheck()) { SaveWindowSettings(); }
            EditorGUILayout.EndHorizontal();

            // list of all animation clips
            GUILayout.Space(8);

            // show a list of all the clips and checkboxes next to each one.
            if ((m_prefab != null) && (m_opts.ClipOpts != null)) {
                // Select all/none animation clips 
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All")) { OnBtnToggleAllClips(true); }
                if (GUILayout.Button("Deselect All")) { OnBtnToggleAllClips(false); }
                if (GUILayout.Button("Reset Names")) { OnBtnResetNames(); }
                if (GUILayout.Button("Refresh Prefab")) { RefreshModifiedPrefab(); }
                EditorGUILayout.EndHorizontal();
                int idx = 0;

                foreach (ClipOption clipOpt in m_opts.ClipOpts) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    m_opts.ClipOpts[idx].SetName(EditorGUILayout.TextField(new GUIContent("", ""), m_opts.ClipOpts[idx].Name, GUILayout.MinWidth(100)));
                    if (clipOpt.Clip != null) {
                        if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }
                        EditorGUILayout.LabelField(string.Format(" [{0:0.##}s, {1}f]", clipOpt.Clip.length, (int)(clipOpt.Clip.length * clipOpt.Clip.frameRate)), GUILayout.MinWidth(50));
                        EditorGUI.BeginChangeCheck();
                    }
                    m_opts.ClipOpts[idx].SetEnable(EditorGUILayout.Toggle(new GUIContent("", "Check to include in the baked output."), m_opts.ClipOpts[idx].IsEnabled));
                    if (EditorGUI.EndChangeCheck()) { OnClipStatusChanged(idx); }
                    EditorGUILayout.EndHorizontal();
                    idx++;
                }
            }
            GUILayout.Space(8);

            // checkbox row 1
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.EnableResetPositionBeforeBake = EditorGUILayout.Toggle(new GUIContent("Reset Position B4 Bake", "Some models require their positions to be reset to vertices to match their meshes. (default false)"), m_opts.EnableResetPositionBeforeBake);
            m_opts.EnableResetRotationBeforeBake = EditorGUILayout.Toggle(new GUIContent("Reset Rotation B4 Bake", "Some models require their rotations to be reset to get their vertices to match their meshes. (default false)"), m_opts.EnableResetRotationBeforeBake);
            if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }
            EditorGUILayout.EndHorizontal();

            // checkbox row 2
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.EnableResetScaleBeforeBake = EditorGUILayout.Toggle(new GUIContent("Reset Scale B4 Bake", "Some models require their scales to be reset to get their vertices to match their meshes. (default false)"), m_opts.EnableResetScaleBeforeBake);
            m_opts.EnableBoneAdjust = EditorGUILayout.Toggle(new GUIContent("Enable Bone Adjust", "If true, bone offset/scale based on bounds will be performed. (default false)"), m_opts.EnableBoneAdjust);
            if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }
            EditorGUILayout.EndHorizontal();

            // checkbox row 3
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.IgnoreBoneMeshes = EditorGUILayout.Toggle(new GUIContent("Ignore Bone Meshes", "If true, child bone meshes will be ignored. (default false)"), m_opts.IgnoreBoneMeshes);
            if (EditorGUI.EndChangeCheck()) { RefreshPredictionAndSaveSpecificSettings(); }
            EditorGUI.BeginChangeCheck();
            m_opts.EnableR11G10B11 = EditorGUILayout.Toggle(new GUIContent("Use R11G10B11 Compression.", "If true, each pixel is encoded and decoded using RGBA 11-10-11 bit compression. (default true)"), m_opts.EnableR11G10B11);
            if (EditorGUI.EndChangeCheck()) { SaveWindowSettings(); }
            EditorGUILayout.EndHorizontal();

            // make a row for every skin (lod)...
            int skinIndex = 0;
            string textureLabel = "Texture: ";
            float textureLabelWidth = EditorStyles.label.CalcSize(new GUIContent(textureLabel)).x;
            string formatLabel = "Format: ";
            float formatLabelWidth = EditorStyles.label.CalcSize(new GUIContent(formatLabel)).x;
            foreach (SkinOption skinOpt in m_opts.SkinOpts) {
                GUILayout.BeginVertical("Box");
                EditorGUILayout.BeginHorizontal();
                string skinLabel = $"Skin {skinIndex}> ";
                EditorGUILayout.LabelField(skinLabel, GUILayout.Width(EditorStyles.label.CalcSize(new GUIContent(skinLabel)).x));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField(textureLabel, GUILayout.Width(textureLabelWidth));
                skinOpt.TexType = (VtxAnimTexType)EditorGUILayout.EnumPopup(skinOpt.TexType, GUILayout.MinWidth(100));
                if (EditorGUI.EndChangeCheck()) { HandleTextureSelection(skinIndex); }

                // format combo-box
                GUI.enabled = m_formatComboEnabled; // disable if there are any problems
                EditorGUI.BeginChangeCheck();
                GUILayout.Label(formatLabel, GUILayout.Width(formatLabelWidth));
                skinOpt.SelectedFmtIdx = EditorGUILayout.Popup(skinOpt.SelectedFmtIdx, skinOpt.FormatStringsArray, GUILayout.MinWidth(130));
                if (EditorGUI.EndChangeCheck()) { RefreshPredictionAndSaveSpecificSettings(); }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                skinIndex++;
                GUILayout.EndVertical();
            }
        
            // a row of buttons
            EditorGUILayout.BeginHorizontal();
            // bake button
            GUI.enabled = m_bakeButtonEnabled;
            if (GUILayout.Button(new GUIContent("Bake","Press this button when you are ready to bake an animation."))) { OnBtnBake(); }
            if (GUILayout.Button(new GUIContent("Open Art Folder","Press this button to open the recent bake folder."))) { OnBtnOpenArtFolder(); }
            if (GUILayout.Button(new GUIContent("Select Output Prefab", "Press this button to select the baked output prefab in the project view."))) { OnBtnSelectBakedPrefabInProject(); }
            //if (GUILayout.Button(new GUIContent("Instantiate/Select", "Press this button to select instantiate the last baked object to the subscene (or select if it already exists)."))) { OnBtnInstantiateToSubscene(); }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // prediction text area
            EditorGUILayout.PrefixLabel("Prediction:");
            EditorGUILayout.TextArea(m_predictionText, EditorStyles.textArea);

            // result text area
            EditorGUILayout.PrefixLabel("Last Bake Result:");
            EditorGUILayout.TextArea(m_lastBakeText, EditorStyles.textArea);

            // warning text area
            EditorGUILayout.PrefixLabel("Warnings:");
            EditorGUILayout.TextArea(m_warningText, EditorStyles.textArea);
        }

        // Called when the user presses the "Reset Names" button.
        void OnBtnResetNames()
        {
            if (m_opts.ClipOpts == null) { return; }
            for (int i = 0; i < m_opts.ClipOpts.Count; i++) {
                m_opts.ClipOpts[i].Name = m_opts.ClipOpts[i].Clip.name;
            }
        }

        // Called when the user clicks the toggle-all-clips box.
        void OnBtnToggleAllClips(bool enable)
        {
            // toggle all clips
            for (int i = 0; i < m_opts.ClipOpts.Count; i++) { 
                m_opts.ClipOpts[i].SetEnable(enable);
            }
            // ensure that at least one item is selected if this is a disable-all command
            if (enable == false) { m_opts.ClipOpts[0].SetEnable(true); }
            RefreshPredictionAndFormatsAndSaveSpecificSettings();
        }

        // Called whenever someone checks one of the boxes next to a clip.
        void OnClipStatusChanged(int index)
        {
            // Important! Force at least one clip to be selected.
            // If there is no selection, then we can't make any predictions,
            // which will prevent us from refreshing the combo box.
            EnsureAtLeastOneClip();
            RefreshPredictionAndFormatsAndSaveSpecificSettings();
        }

        // Called whenever the user drops in a new prefab (or selects it).
        // Whenever the user changes the prefab, we'll recalculate all the possible frame-rates and texture sizes
        // and use that info to repopulate the frame rate combo-box
        void OnPrefabChanged()
        {
            RefreshModifiedPrefab();
        }

        //void OnSubsceneChanged()
        //{
        //    if (m_subScene == null) { return; }
        //    m_subSceneName = m_subScene.name;
        //    SaveWindowSettings();
        //}

        // I had to do some voodoo here to catch when recompile finished. 
        void Update()
        {
            if (m_justRecompiled && m_isWaitingForRecompile) {
                m_isWaitingForRecompile = false;
                OnRecompileFinished();
            }
            m_justRecompiled = false;
        }

        // This gets called whenever recompiling finishes
        void OnRecompileFinished()
        {
            m_lastBakeText += "\nDomain reload finished.";
        }

        // Called whenever the user presses the "Bake" button in the inspector
        // Here BakeAndSave is called for every skinned mesh
        void OnBtnBake()
        {
            if (!EnumUtils.IsEnumCompatible(m_prefab.name)) {
                string msg = $"Warning! The name of {m_prefab.name} is not enum-compatible. You need to rename it.";
                EditorUtility.DisplayDialog("Are you sure?", msg, "OK");
                return;
            }

            try {
                m_isBaking = true;

                // this string will accumulate extra lines for the log file.
                string extraText = $"Started: {System.DateTime.Now.ToString(new System.Globalization.CultureInfo("en-US"))}";

                AnimationCookerUtils.DisplayProgress("Cooking up some textures. Ommmm nom nom nom.", "0 of 0 clips finished.", 0);

                m_lastBakeText = "";
                m_warningText = "";
                m_isWaitingForRecompile = true;
                var watch = System.Diagnostics.Stopwatch.StartNew();

                //List<BakeAndSaveResult> results = new List<BakeAndSaveResult>();
                //BakeAndSaveResult result = new BakeAndSaveResult();

                string artSubFolderPath = m_artOutputFolder.Replace("{name}", m_prefab.name);
                string prefabOutPath = m_prefabOutputFolder.Replace("{name}", m_prefab.name);
                string outScriptPath = m_generatedScriptOutFolder.Replace("{name}", m_prefab.name);


                // Setup the subfolder - this is a path like Assets/Prefabs/Baked/MyModel where all files for this bake will be saved.
                // If it exists, it will be deleted and overwritten. If it doesn't exist, it will be created.
                artSubFolderPath = Path.Combine("Assets/", artSubFolderPath);
                if (Directory.Exists(artSubFolderPath)) {
                    // delete all files inside the directory.
                    System.IO.DirectoryInfo di = new DirectoryInfo(artSubFolderPath);
                    foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
                    // refresh the assets
                    AssetDatabase.Refresh();
                }
                Directory.CreateDirectory(artSubFolderPath);
                UnityEngine.Debug.Log($"artSubFolderPath: {artSubFolderPath}");

                // Make the generated script folder path and ensure that it exists.
                prefabOutPath = Path.Combine("Assets/", prefabOutPath);
                Directory.CreateDirectory(prefabOutPath);
                UnityEngine.Debug.Log($"prefabOutPath: {prefabOutPath}");

                // Make the generated script folder path and ensure that it exists.
                outScriptPath = Path.Combine("Assets/", outScriptPath);
                Directory.CreateDirectory(outScriptPath);
                UnityEngine.Debug.Log($"outScriptPath: {outScriptPath}");

                if (m_opts == null) { UnityEngine.Debug.Log($"m_opts is null"); }
                if (m_opts.SkinOpts == null) { UnityEngine.Debug.Log($"m_opts.SkinOpts is null"); }
                if (m_computeShaders == null) { UnityEngine.Debug.Log($"m_computeShaders is null"); }

                // bake entire model - all skins and all selected clips
                BakeAndSaveResult result = new BakeAndSaveResult();
                result.BakeRes = AnimationCookerUtils.BakeModel(m_prefab, m_opts, m_skinInfos, m_computeShaders);
                result.SaveRes = AnimationCookerUtils.SaveBakedFiles(result.BakeRes, artSubFolderPath, m_playbackShader, m_opts.EnableCopyTexturesToOutput);

                // Save all files.
                if (m_playbackShader == null) { UnityEngine.Debug.Log("\n!!! You didn't set the play shader. Output files cannot be saved!!!"); }

                // Create a new model entry and initialize it using the bake result.
                ModelEntry model = new ModelEntry();
                model.ModelName = m_prefab.name;
                foreach (var sr in result.BakeRes.SkinResults) {
                    SkinEntry si = new SkinEntry((ushort)sr.PositionRenderTex.width, (ushort)sr.PositionRenderTex.height, sr.Interval, sr.Pow2, sr.FrameRate, (ushort)sr.FixedMesh.vertexCount);
                    si.SkinClips = sr.SkinClips;
                    model.Skins.Add(si);
                }
                foreach (var clip in m_opts.ClipOpts) {
                    if (clip.IsEnabled) {
                        model.Clips.Add(new ClipEntry { ClipName = clip.Name, Length = clip.Clip.length });
                    }
                }

                // Add or replace the model in the scriptable-object database. This should save it to disk automatically.
                // The function is not very fast, but that's because unity can't do dictionaries in SOs.
                // A linear search shouldn't matter too much because it only happens when pressing the bake button,
                // and there aren't likely more than a few dozen things in the database anyway.
                m_animDbSo.SetModel(model);
                EditorUtility.SetDirty(m_animDbSo);
                AssetDatabase.SaveAssetIfDirty(m_animDbSo);

                // overwrite the current enums file with the new values (if enabled)
                if (m_opts.EnableEnumDeclaration) { m_animDbSo.WriteEnums(outScriptPath, m_enumFileName); }

                // 1x prefab.
                m_bakedPrefab = SavePrefabToDisk(result, prefabOutPath, m_animDbSo);

                EditorUtility.ClearProgressBar();

                if (m_opts.EnablePostDomainReload) {
                    // Warning - calling ImportAsset() on a script is asynchronous and triggers a domain reload.
                    // The class that called this function will have to do some voodoo in Update()
                    // to determine when the import finished if it wants to do something with
                    // the database file that was just created.
                    AssetDatabase.ImportAsset(Path.Combine(outScriptPath, m_enumFileName));
                }

                // Add a footer to the log.
                extraText += "\nArt files were saved to: " + artSubFolderPath;
                extraText += "\nAnimation enums saved to: " + outScriptPath + "/" + m_enumFileName;
                extraText += "\nBaked Prefab saved to: " + prefabOutPath + "/" + m_bakedPrefab.name + ".prefab";
                extraText += "\nCompleted in " + string.Format("{0:0.####}", watch.Elapsed.TotalSeconds) + " seconds.";

                if (m_opts.EnableLogFile) {
                    SaveLogToDisk(result, artSubFolderPath, extraText, m_opts);
                }

                if ((m_warningText.Length <= 0) && (m_opts.SkinOpts.Count >= 2)) {
                    if ((m_opts.SkinOpts[1].TexType != VtxAnimTexType.Pos) || (m_opts.SkinOpts[2].TexType != VtxAnimTexType.Pos)) {
                        m_warningText = "LOD levels 1 and 2 are PosAndNml or PosNmlAndTan. You can save GPU cycles and memory by switching them to Position only.";
                    }
                }

                m_lastBakeText = extraText;
                m_isBaking = false;
            } catch (System.Exception) {
                EditorUtility.ClearProgressBar();
            }
        }

        // When the user clicks this button the most recent bake output folder path will be opened in explorer.
        void OnBtnOpenArtFolder()
        {
            string artPathReplaced = m_artOutputFolder.Replace("{name}", m_prefab.name);
            EditorUtility.RevealInFinder(Path.Combine("Assets/", artPathReplaced));
        }

        void OnBtnSelectBakedPrefabInProject()
        {
            string prefabPathReplaced = m_prefabOutputFolder.Replace("{name}", m_prefab.name);
            string prefabPath = Path.Combine("Assets/", prefabPathReplaced, m_prefab.name + ".prefab");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        //// When this button is pressed, attempt to find the game-object in the subscene.
        //// If it can't be found, then it'll attempt to load it.
        //// If it can be and the subscene is open, then it'll attempt to instantiate it in the subscene.
        //void OnBtnInstantiateToSubscene()
        //{
        //    GameObject go = GameObject.Find(m_prefab.name);
        //    if (go == null) {
        //        // if the baked prefab is null, attempt to load it.
        //        if (m_bakedPrefab == null) {
        //            string prefabPath = Path.Combine("Assets/", m_outputFolder, m_prefab.name, m_prefab.name + ".prefab");
        //            m_bakedPrefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath) as GameObject;
        //        }
        //        if ((m_bakedPrefab != null) && (m_subScene != null)) {
        //            // If a subscene is specified, instantiate the new prefab and add it to the subscene.
        //            // This took me 8 hours to figure out how to do!
        //            bool wasUnloaded = false;
        //            // if the scene is closed, then open it.
        //            if (!m_subScene.IsLoaded) {
        //                wasUnloaded = true;
        //                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(m_subScene.EditableScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        //            }
        //            // I can't use m_subScene.transform.Find(m_prefab.name) because.... subscenes smoke crack. bug maybe?
        //            // Only add the prefab to the subscene if it doesn't already exist.
        //            if (GameObject.Find(m_prefab.name) == null) {
        //                GameObject instantiatedGameObj = PrefabUtility.InstantiatePrefab(m_bakedPrefab) as GameObject;
        //                UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(instantiatedGameObj, m_subScene.EditingScene);
        //                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(m_subScene.EditingScene);
        //                Selection.objects = new Object[] { instantiatedGameObj };
        //            }
        //            // close the subscene again if it was opened.
        //            if (wasUnloaded) { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(m_subScene.EditingScene, true); }
        //        }
        //    }
        //    Selection.objects = new Object[] { go };
        //}

        // Read in Prefab specific settings (tied together by the key, which is the prefab name)
        void LoadSpecificSettings(string key)
        {
            m_opts.EnableResetPositionBeforeBake = EditorPrefs.GetBool(key + "EnableResetPositionBeforeBake", false);
            m_opts.EnableResetRotationBeforeBake = EditorPrefs.GetBool(key + "EnableResetRotationBeforeBake", false);
            m_opts.EnableResetScaleBeforeBake = EditorPrefs.GetBool(key + "EnableResetScaleBeforeBake", false);
            m_opts.IgnoreBoneMeshes = EditorPrefs.GetBool(key + "IgnoreBoneMeshes", false);
            m_opts.EnableBoneAdjust = EditorPrefs.GetBool(key + "EnableBoneAdjust", false);
            m_opts.EnableR11G10B11 = EditorPrefs.GetBool(key + "EnableR11G10B11", true);

            LoadClipOptSettings(key);

            // Read the number of lod options that were last set for this key.
            // Note that the the LODs of the current prefab may not match!
            // The number of LODs could have changed.
            // Likewise, the available list of formats could have changed too.
            // Before this code runs it is IMPERATIVE that m_opts.SkinOpts
            // and m_opts.SkinOpts[].Format has already been set from loading the prefab.
            int skinCount = EditorPrefs.GetInt(key + "SkinOptionCount", 0);
            if (skinCount > 0) {
                for (int i = 0; i < m_opts.SkinOpts.Count; i++) {
                    TexSampleFormat fmt = new TexSampleFormat();
                    fmt.Width = EditorPrefs.GetInt(key + "FormatWidth" + i, 0);
                    fmt.Height = EditorPrefs.GetInt(key + "FormatHeight" + i, 0);
                    fmt.FrameRate = (byte)EditorPrefs.GetInt(key + "FormatFps" + i, 0);
                    m_opts.SkinOpts[i].TexType = (VtxAnimTexType)EditorPrefs.GetInt(key + "TexType" + i, (int)VtxAnimTexType.PosAndNml);
                    m_opts.SkinOpts[i].SelectClosestFormat(fmt.FrameRate);
                }
            }
        }

        void LoadClipOptSettings(string key)
        {
            // Read the clip options that were last set for this key.
            // Note that the number of clips and clip orders may have changed,
            // so the best we can do is match them up and the user might have to
            // redo some of them.
            // Before this code runs it is IMPERATIVE that m_opts.ClipOpts
            // has already been set from loading the prefab.
            int clipCount = EditorPrefs.GetInt(key + "ClipOptionCount", 0);
            if ((m_opts.ClipOpts != null) && (clipCount > 0)) {
                for (int i = 0; i < m_opts.ClipOpts.Count; i++) {
                    m_opts.ClipOpts[i].IsEnabled = EditorPrefs.GetBool(key + "EnableClip" + i, true);
                    m_opts.ClipOpts[i].Name = EditorPrefs.GetString(key + "ClipName" + i, "");
                }
            }
        }

        // Loads all general window settings (not the ones specific to a prefab)
        void LoadWindowSettings()
        {
            string projectName = GetProjectName();

            // common settings
            string playbackShaderName = EditorPrefs.GetString(projectName + "PlaybackShaderName", "AnimationCooker/VtxAnimSimpleLit");
            m_playbackShader = Shader.Find(playbackShaderName);
            m_artOutputFolder = EditorPrefs.GetString(projectName + "ArtOutputFolder", "ExampleScene/Baked");
            m_prefabOutputFolder = EditorPrefs.GetString(projectName + "PrefabOutputFolder", "ExampleScene/Baked");
            m_generatedScriptOutFolder = EditorPrefs.GetString(projectName + "GeneratedScriptOutFolder", "ExampleScene/Scripts/Generated");
            m_enumFileName = EditorPrefs.GetString(projectName + "AnimationEnumsFileName", "AnimEnums.cs");
            m_opts.EnableLogFile = EditorPrefs.GetBool(projectName + "EnableLogFile", true);
            m_opts.EnableEnumDeclaration = EditorPrefs.GetBool(projectName + "EnableEnumDeclaration", true);
            m_opts.EnablePostDomainReload = EditorPrefs.GetBool(projectName + "EnablePostDomainReload", true);
            m_opts.EnableCopyTexturesToOutput = EditorPrefs.GetBool(projectName + "EnableCopyTexturesToOutput", true);
            m_animDbSoPath = EditorPrefs.GetString(projectName + "AnimDbSoPath", "ExampleScene/Baked/AnimDbData.asset");

            // animdb
            m_animDbSo = AssetDatabase.LoadAssetAtPath<AnimDbSo>(m_animDbSoPath);

            //// subscene
            //m_subSceneName = EditorPrefs.GetString(projectName + "SubsceneName", "");
            //RefreshSubscene();

            // prefab settings (all other setting will depend on whether or not a prefab can be loaded)
            bool isPrefabValid = EditorPrefs.GetBool(projectName + "IsPrefabValid", false);
            if (isPrefabValid) {
                if (EditorPrefs.GetBool(projectName + "IsPrefabInScene", false)) {
                    string gameObjectName = EditorPrefs.GetString(projectName + "GameObjectName", "");
                    if (gameObjectName.Length > 0) { m_prefab = GameObject.Find(gameObjectName); }
                } else {
                    string prefabAssetPath = EditorPrefs.GetString(projectName + "PrefabAssetPath", "");
                    if (prefabAssetPath.Length > 0) { m_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath); }
                }
            }

            if (RefreshPrefab()) { 
                LoadSpecificSettings(m_prefab.name);
                RefreshPredictions();
            }
        }

        //void RefreshSubscene()
        //{
        //    if (m_subSceneName.Length <= 0) { return; }
        //    var subScenes = GameObject.FindObjectsOfType<Unity.Scenes.SubScene>();
        //    for (int i = 0; i < subScenes.Length; i++) {
        //        if (subScenes[i].name == m_subSceneName) {
        //            m_subScene = subScenes[i];
        //            return;
        //        }
        //    }
        //}

        // Call this function to refresh the text in the predictions text box.
        void RefreshPredictions()
        {
            m_predictionText = "";
            float estimatedBakeTime = 0f;
            for (int i = 0; i < m_opts.SkinOpts.Count; i++) {
                if (i != 0) { m_predictionText += "\n"; }
                estimatedBakeTime += RefreshPrediction(i);
            }
            ClipStats stats = m_opts.CalculateSelectedClipStats();

            // This estimate is based on a 6 core i7 9750H laptop with a mobile 2060 rtx gpu... it won't be very accurate if the machine differs a lot in specs.
            m_predictionText += $"\nEstimated bake time: {string.Format("{0:0.#}", estimatedBakeTime)}s ({m_opts.SkinOpts.Count} skins, {stats.TotalClipCount} clips, {string.Format("{0:0.####}", stats.TotalClipLength)}s of animation.)";
        }

        // This function will save all settings associated with a prefab name.
        void SaveSpecificSettings()
        {
            string key = m_prefab.name;
            EditorPrefs.SetBool(key + "EnableResetPositionBeforeBake", m_opts.EnableResetPositionBeforeBake);
            EditorPrefs.SetBool(key + "EnableResetRotationBeforeBake", m_opts.EnableResetRotationBeforeBake);
            EditorPrefs.SetBool(key + "EnableResetScaleBeforeBake", m_opts.EnableResetScaleBeforeBake);
            EditorPrefs.SetBool(key + "IgnoreBoneMeshes", m_opts.IgnoreBoneMeshes);
            EditorPrefs.SetBool(key + "EnableBoneAdjust", m_opts.EnableBoneAdjust);
            EditorPrefs.SetBool(key + "EnableR11G10B11", m_opts.EnableR11G10B11);

            EditorPrefs.SetInt(key + "ClipOptionCount", m_opts.ClipOpts.Count);
            for (int i = 0; i < m_opts.ClipOpts.Count; i++) {
                ClipOption clipOpt = m_opts.ClipOpts[i];
                EditorPrefs.SetBool(key + "EnableClip" + i, clipOpt.IsEnabled);
                EditorPrefs.SetString(key + "ClipName" + i, clipOpt.Name);
            }

            EditorPrefs.SetInt(key + "SkinOptionCount", m_opts.SkinOpts.Count);
            for (int i = 0; i < m_opts.SkinOpts.Count; i++) {
                TexSampleFormat fmt = m_opts.SkinOpts[i].SelectedFormat;
                VtxAnimTexType texType = m_opts.SkinOpts[i].TexType;
                EditorPrefs.SetInt(key + "FormatWidth" + i, fmt.Width);
                EditorPrefs.SetInt(key + "FormatHeight" + i, fmt.Height);
                EditorPrefs.SetInt(key + "FormatFps" + i, fmt.FrameRate);
                EditorPrefs.SetInt(key + "TexType" + i, (int)texType);
            }
        }

        // Save all general window settings
        void SaveWindowSettings()
        {
            string projectName = GetProjectName();

            // common settings
            EditorPrefs.SetString(projectName + "ArtOutputFolder", m_artOutputFolder);
            EditorPrefs.SetString(projectName + "PrefabOutputFolder", m_prefabOutputFolder);
            EditorPrefs.SetString(projectName + "GeneratedScriptOutFolder", m_generatedScriptOutFolder);
            EditorPrefs.SetString(projectName + "PlaybackShaderName", m_playbackShader.name);
            //EditorPrefs.SetString(projectName + "SubsceneName", m_subSceneName);
            EditorPrefs.SetString(projectName + "AnimationEnumsFileName", m_enumFileName);
            EditorPrefs.SetBool(projectName + "EnablePostDomainReload", m_opts.EnablePostDomainReload);
            EditorPrefs.SetBool(projectName + "EnableEnumDeclaration", m_opts.EnableEnumDeclaration);
            EditorPrefs.SetBool(projectName + "EnableLogFile", m_opts.EnableLogFile);
            EditorPrefs.SetBool(projectName + "EnableCopyTexturesToOutput", m_opts.EnableCopyTexturesToOutput);
            EditorPrefs.SetString(projectName + "AnimDbSoPath", AssetDatabase.GetAssetPath(m_animDbSo));

            if (m_prefab != null) {
                GameObject go = GameObject.Find(m_prefab.name);
                if (go == m_prefab) {
                    EditorPrefs.SetBool(projectName + "IsPrefabValid", true);
                    EditorPrefs.SetBool(projectName + "IsPrefabInScene", true);
                    EditorPrefs.SetString(projectName + "GameObjectName", m_prefab.name);
                    SaveSpecificSettings();
                } else {
                    EditorPrefs.SetBool(projectName + "IsPrefabValid", true);
                    EditorPrefs.SetBool(projectName + "IsPrefabInScene", false);
                    EditorPrefs.SetString(projectName + "PrefabAssetPath", AssetDatabase.GetAssetPath(m_prefab));
                    SaveSpecificSettings();
                }
            } else {
                EditorPrefs.SetBool(projectName + "IsPrefabValid", false);
            }
        }

        // Call this to update the window to reflect contents of the specified prefab.
        bool RefreshPrefab()
        {
            m_formatComboEnabled = m_bakeButtonEnabled = false;
            m_warningText = "";

            // validate a whole bunch of things.

            // prefab cannot be null
            if (m_prefab == null) {
                m_warningText = "Prefab is null.";
                return false; // buttons will be disabled
            }

            // verify that there is at least one skinned mesh renderer on this prefab
            m_skinInfos = AnimationCookerUtils.FindSkinnedMeshRenderers(m_prefab);
            if (m_skinInfos.Count <= 0) {
                m_warningText = $"A skinned mesh renderer was not found on prefab: {m_prefab.name}";
                UnityEngine.Debug.Log($"{m_warningText}");
                return false;
            }

            // There must be an animator or animation component on it that has some clips.
            // This is the one and only place that the clips get fetched (RefreshPrefab function).
            // This must be done BEFORE loading the skin options.
            List<AnimationClip> allClips = AnimationCookerUtils.FindClips(m_prefab, ref m_warningText);
            if (allClips.Count <= 0) {
                m_warningText = $"No animation clips were found on found on prefab: {m_prefab.name}";
                return false;
            }
            // refresh ClipOpts to match the new clips - all clips will be enabled by default.
            m_opts.ClipOpts = new List<ClipOption>(allClips.Count);
            for (int i = 0; i < allClips.Count; i++) {
                m_opts.ClipOpts.Add(new ClipOption(allClips[i].name, true, allClips[i]));
            }
            // Since all clips are enabled, attempt to match up the old settings with the new clips.
            LoadClipOptSettings(m_prefab.name);

            // refresh SkinOpts to match the new skins.
            // MUST be done AFTER clips so that DiscoverPossibleFormats() can be called.
            m_opts.SkinOpts = new List<SkinOption>(m_skinInfos.Count);
            ClipStats clipStats = m_opts.CalculateSelectedClipStats();
            for (int i = 0; i < m_skinInfos.Count; i++) {
                SkinOption skinOpt = new SkinOption(i); // uses defaults for some options.
                MeshStats meshStats = AnimationCookerUtils.CalculateMeshStats(m_skinInfos[i], m_opts.IgnoreBoneMeshes);
                List<TexSampleFormat> Formats = AnimationCookerUtils.DiscoverPossibleFormats(meshStats.VertexCount, clipStats.TotalClipLength, clipStats.SmallestFps);
                skinOpt.SetFormats(Formats, ReselectMode.Fps);
                m_opts.SkinOpts.Add(skinOpt);
            }

            // re-enable buttons
            m_formatComboEnabled = m_bakeButtonEnabled = true;
            return true;
        }

        // There must always be at least one clip selected, so this function
        // ensures that - if the user tries to disable all of them,
        // then it won't let that happen.
        void EnsureAtLeastOneClip()
        {
            bool hasAtLeastOneClip = false;
            for (int i = 0; i < m_opts.ClipOpts.Count; i++) {
                if (m_opts.ClipOpts[i].IsEnabled) { hasAtLeastOneClip = true; break; }
            }
            if (!hasAtLeastOneClip) { m_opts.ClipOpts[0].SetEnable(true); }
        }

        // This function will create a prefab from the last bake results and save it to disk.
        GameObject SavePrefabToDisk(BakeAndSaveResult result, string subFolderPath, AnimDbSo db)
        {
            GameObject oldBakedPrefab = GameObject.Find(m_prefab.name + "_Baked");
            if (oldBakedPrefab != null) { GameObject.DestroyImmediate(oldBakedPrefab); }
            GameObject tempGameObj = new GameObject(m_prefab.name + "_Baked");

            // mesh renderer (lod 0)
            MeshRenderer renderer = tempGameObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(result.SaveRes.Skins[0].MatPath);

            // mesh filter (lod 0)
            MeshFilter filter = tempGameObj.AddComponent<MeshFilter>();
            filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(result.SaveRes.Skins[0].MeshPath);

            // animation clip authoring
            AnimationModelAuthoring animAuth = tempGameObj.AddComponent<AnimationModelAuthoring>();
            // Model Name...
            animAuth.AnimationModelName = m_prefab.name;
            // Default Clip Name...
            short modelIndex = db.FindModelIndex(m_prefab.name);
            if (modelIndex >= 0) {
                ModelEntry model = db.GetModel(modelIndex);
                short idleClipIndex = model.FindClipIndexThatContains("Idle");
                if (idleClipIndex < 0) { idleClipIndex = 0; } // default to first clip if an idle one wasn't found
                ClipEntry idleClip = model.GetClip(idleClipIndex);
                animAuth.DefaultClipName = idleClip.ClipName.ToString();
            } else {
                UnityEngine.Debug.LogWarning($"Could not find model {m_prefab.name} in the database.");
            }
            // Default Play Speed...
            animAuth.DefaultPlaySpeed = 1f;
            // Animation Database...
            animAuth.AnimationDb = db;

            // lod authoring
            LODGroup srcGroup = m_prefab.GetComponent<LODGroup>();
            if (srcGroup != null) {
                LODGroup destGroup = tempGameObj.AddComponent<LODGroup>();
                LOD[] lods = srcGroup.GetLODs(); // makes a deep copy
                for (int i = 0; i < lods.Length; i++) { lods[i].renderers = null; }
                destGroup.SetLODs(lods);
                destGroup.size = srcGroup.size;

                // add a LOD group authoring component, which will use the LODGroup to set distances.
                SimpleLodGroupAuthoring lodAuth = tempGameObj.AddComponent<SimpleLodGroupAuthoring>();

                lodAuth.Mat0 = renderer.sharedMaterial;
                lodAuth.Mat1 = AssetDatabase.LoadAssetAtPath<Material>(result.SaveRes.Skins[1].MatPath);
                lodAuth.Mat2 = AssetDatabase.LoadAssetAtPath<Material>(result.SaveRes.Skins[2].MatPath);
                lodAuth.Mesh0 = filter.sharedMesh;
                lodAuth.Mesh1 = AssetDatabase.LoadAssetAtPath<Mesh>(result.SaveRes.Skins[1].MeshPath);
                lodAuth.Mesh2 = AssetDatabase.LoadAssetAtPath<Mesh>(result.SaveRes.Skins[2].MeshPath);
            }

            // save that temporary gameobject to a new prefab on disk
            GameObject go = PrefabUtility.SaveAsPrefabAsset(tempGameObj, Path.Combine(subFolderPath, m_prefab.name + ".prefab"));

            // delete the temporary gameobject since it's no longer needed.
            GameObject.DestroyImmediate(tempGameObj);

            AssetDatabase.SaveAssets();
            return go;
        }

        // This will save a text log file to the specified path
        void SaveLogToDisk(BakeAndSaveResult result, string subFolderPath, string extraText, BakeOptions opts)
        {
            string txt = $"\nModel: {result.BakeRes.Originalprefab.name}";
            //txt += opts.MsgPrefix;
            for (short s = 0; s < result.BakeRes.SkinResults.Count; s++) {
                txt += $"\nSkin: {s}, Root Bone: {result.BakeRes.SkinResults[s].SkinRenderer.rootBone.name}";
                txt += $"\n  {opts.SkinOpts[s].SelectedFormat.MakeString(opts.SkinOpts[s].TexType)}";
            }
            txt += $"\nPlay Shader: {result.SaveRes.PlayShaderName}";
            txt += $"\n{opts.MakeReport()}\n{result.BakeRes.Message}\n{result.SaveRes.Message}\n{extraText}";
            File.WriteAllText(Path.Combine(subFolderPath, result.BakeRes.Originalprefab.name + ".log.log"), txt);
        }

        // Refreshes the prediction text and saves the current prefab-specific settings to disk.
        // Many GUI items use this because when their value changes, we need that change saved to disk
        // and also the change will affect the prediction text.
        void RefreshPredictionAndSaveSpecificSettings()
        {
            RefreshPredictions();
            SaveSpecificSettings();
        }

        void RefreshPredictionAndFormatsAndSaveSpecificSettings()
        {
            RefreshFormats();
            RefreshPredictions();
            SaveSpecificSettings();
        }

        void HandleTextureSelection(int skinIndex)
        {
            RefreshPredictions();
            RefreshCombo(skinIndex);
            SaveSpecificSettings();
        }

        // call this when the prefab has been modified.
        public void RefreshModifiedPrefab()
        {
            if (!RefreshPrefab()) { return; }
            LoadSpecificSettings(m_prefab.name);
            RefreshPredictions();
            SaveWindowSettings();
        }

        public string GetCurrentPrefabPath() 
        { 
            return AssetDatabase.GetAssetPath(m_prefab);
        }

        public bool PrefabHasValidLod(string path)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            LODGroup group = go.GetComponent<LODGroup>();
            if (group == null) { return false; }
            LOD[] lods = group.GetLODs();
            for (int i = 0; i < lods.Length; i++) {
                if (lods[i].renderers.Length == 0) { return false; }
                if ((lods[i].renderers[0] as SkinnedMeshRenderer).sharedMesh == null) { return false; }
                if ((lods[i].renderers[0] as SkinnedMeshRenderer).sharedMaterial == null) { return false; }
            }
            return true;
        }

        public bool IsBaking() { return m_isBaking; }

        // Whenever something changes that could affect the prediction, this function should be called.
        // It will recalculate stats with the new settings and update the prediction string.
        // returns the exstimated bake time in seconds.
        float RefreshPrediction(int skinIndex)
        {
            // first calculate some stats that we'll need later
            ClipStats clipStats = m_opts.CalculateSelectedClipStats();
            MeshStats meshStats = AnimationCookerUtils.CalculateMeshStats(m_skinInfos[skinIndex], m_opts.IgnoreBoneMeshes);
        
            SkinOption skinOpt = m_opts.SkinOpts[skinIndex];

            // using the NEW format, calculate the new frame stats
            FrameStats frameStats = AnimationCookerUtils.CalculateFrameStats(m_opts.ClipOpts, skinOpt.SelectedFormat, meshStats.VertexCount);

            // now summarize all the new stats we just calculated into the prediction string
            m_predictionText += AnimationCookerUtils.MakePredictionString(m_opts, meshStats, clipStats, frameStats, skinIndex);
            if (skinOpt.SelectedFormat.Width > 2048) { m_predictionText += $"\nSkin {skinIndex}, Texture width is > 2048, mobile devices will be sad."; }
            if (skinOpt.SelectedFormat.Height > 2048) { m_predictionText += $"\nSkin {skinIndex}, Texture height is > 2048, mobile devices will be sad."; }

            // This estimate is based on a 6 core i7 9750H laptop with a mobile 2060 rtx gpu... it won't be very accurate if the machine differs a lot in specs.
            return 0.0000366f * frameStats.PointCount;
        }

        void RefreshCombo(int skinIndex)
        {
            m_opts.SkinOpts[skinIndex].RefreshFormatStrings();
        }

        void RefreshFormats()
        {
            ClipStats clipStats = m_opts.CalculateSelectedClipStats();
            for (int i = 0; i < m_opts.SkinOpts.Count; i++) {
                SkinOption skinOpt = m_opts.SkinOpts[i];
                MeshStats meshStats = AnimationCookerUtils.CalculateMeshStats(m_skinInfos[i], m_opts.IgnoreBoneMeshes);
                List<TexSampleFormat> formats = AnimationCookerUtils.DiscoverPossibleFormats(meshStats.VertexCount, clipStats.TotalClipLength, clipStats.SmallestFps);
                skinOpt.SetFormats(formats, ReselectMode.Dimension);
                skinOpt.RefreshFormatStrings();
            }
        }


        // This function fetches the project name, which is used during save/load.
        string GetProjectName()
        {
             string[] s = Application.dataPath.Split('/');
             string projectName = s[s.Length - 2];
             return projectName;
        }
    }


    //// The purpose of this is to intercept when the prefab was changed on disk because the designer modified it.
    //// Unfortunately, I have to intercept all events and filter out everything except for the prefab,
    //// And even then, it will refresh whenever AssetDatabase.SaveAssets() is called, which happpens in backing,
    //// So I filter it while baking is happening as well.
    //public class PrefabModificationProcessor : UnityEditor.AssetModificationProcessor
    //{
    //    public static string[] OnWillSaveAssets(string[] paths)
    //    {
    //        // do nothing if the animation window is not already open
    //        if (!EditorWindow.HasOpenInstances<AnimationKitchenWindow>()) { return paths; }

    //        // fetch the animation kitchen window (note, this will show it if it's not already showing)
    //        AnimationKitchenWindow window = (AnimationKitchenWindow)EditorWindow.GetWindow<AnimationKitchenWindow>(false, "Animation Kitchen");
    //        if (window == null) { return paths; } // bail out if no window found

    //        foreach (string path in paths) {
    //            if (path.EndsWith(".prefab")) {
    //                if (!window.IsBaking() && (window.GetCurrentPrefabPath() == path)) {
    //                    if (window.PrefabHasValidLod(path)) {
    //                        window.RefreshModifiedPrefab();
    //                        UnityEngine.Debug.Log($"Externally refreshing {path}");
    //                    } else {
    //                        // found prefab without LODs
    //                        UnityEngine.Debug.Log($"prefab had invalid LOD. Avoiding prefab refresh.");
    //                    }
    //                }
    //            }
    //        }
    //        return paths;
    //    }
    //}
} // namespace

#endif // UNITY_EDITOR