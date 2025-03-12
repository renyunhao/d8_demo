// This SO definition contains an "animation database", which can be used
// to store and lookup information about animation models and clips.
//
// AnimationCooker overwrites the animation database file every time the designer presses the "Bake"
// button in the Animation Kitchen window. Thus, the actual scriptable object data file instance
// is auto-generated and should not be manually edited in the Unity Inspector.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnimCooker
{
	[System.Serializable]
    public class SkinClipEntry
	{
		public short BeginFrame;
		public short EndFrame;

		public short GetFrameCount() { return (short)(EndFrame - BeginFrame + 1); }
		public float GetLength(float interval) { return interval * GetFrameCount(); }
		public bool IsValid() { return BeginFrame < EndFrame; }
	}

	[System.Serializable]
	public class SkinEntry
	{
		public List<SkinClipEntry> SkinClips = new List<SkinClipEntry>(); // one slot for each selected clip
		public ushort Width;
		public ushort Height;
		public float Interval; // "sampled interval" (doesn't account for speed)
		public byte Pow2;
		public byte FrameRate; // "sampled frame rate" (doesn't account for speed)
		public ushort VertCount;
		public SkinEntry(ushort w, ushort h, float intrvl, byte pow2, byte fr, ushort vertCount)
		{
			Width = w;
			Height = h;
			Interval = intrvl;
			Pow2 = pow2;
			FrameRate = fr;
			VertCount = vertCount;
		}
	}

	[System.Serializable]
	public class ModelEntry
	{
		public string ModelName;
		public List<SkinEntry> Skins = new List<SkinEntry>();
		public List<ClipEntry> Clips = new List<ClipEntry>(); // one slot for each selected clip

		// returns the index of a clip that matches the specified text
		// returns -1 if not found.
		public short FindClipIndex(string text)
		{
			for (short c = 0; c < Clips.Count; c++) {
				// case insensitive search on the key (won't work for all languages)
				if (string.Compare(Clips[c].ClipName, text, true) == 0) { return c; }
			}
			return -1;
		}

		// returns the index of a clip that contains the specified text
		// returns -1 if not found.
		public short FindClipIndexThatContains(string text)
		{
			for (short c = 0; c < Clips.Count; c++) {
				// case insensitive search on the key (won't work for all languages)
				if (Clips[c].ClipName.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0) { return c; }
			}
			return -1;
		}

		// returns the clip at the specified index
		public ClipEntry GetClip(short clipIndex) { return Clips[clipIndex]; }
	}

	[System.Serializable]
	public class ClipEntry
	{
		public string ClipName;
		public float Length; // todo - is this needed? is it filled out?
	}

	[CreateAssetMenu(fileName = "New Animation Database", menuName = "Game/Animation Database")]
    public class AnimDbSo : ScriptableObject
    {
		// The list of all model entries.
		// It's a list because Unity doesn't have serialization for dictionaries. :-(
		public List<ModelEntry> Models;

		public List<short> FindClips(string clipName)
        {
            var lst = new List<short>(Models.Count);
            for (int m = 0; m < Models.Count; m++) {
				short idx = Models[m].FindClipIndex(clipName);
				if (idx >= 0) { lst.Add(idx); }
            }
            return lst;
        }

        // Same as FindClips(), however, this version matches clips that partially or fully contains the specified text.
        // For example, if you search for "Run" in Dog{ Bark, Idle, RunFast, RunSlow }, Cat{ Meow, Attack, Run}, Bird { Fly, Tweet, Idle}
        // The function will returns the clips corresponding with: { 2, 2, -1 }, where each slot corresponds with a model index.
        // clipName --> a clip text to search for in all clip names for all models.
        public List<short> FindClipsThatContain(string clipText)
        {
            var lst = new List<short>(Models.Count);
            for (int m = 0; m < Models.Count; m++) {
				short idx = Models[m].FindClipIndexThatContains(clipText);
                if (idx >= 0) { lst.Add(idx); }
            }
            return lst;
        }

		public void SetModel(ModelEntry model)
		{
			short foundIndex = -1;
            for (short m = 0; m < Models.Count; m++) {
                if (Models[m].ModelName == model.ModelName) { foundIndex = m; break; }
            }
            if (foundIndex >= 0) {
                Models[foundIndex] = model;
            } else {
                Models.Add(model);
            }
		}

		public ModelEntry GetModel(short modelIndex) { return Models[modelIndex]; }

		// returns the index of the specified model name, or -1 if it's not found.
		public short FindModelIndex(string modelName)
		{
			if (Models == null) { return -1; }
			for (short m = 0; m < Models.Count; m++) {
				if (Models[m].ModelName == modelName) { return m; }
			}
			return -1;
		}

		public bool WriteEnums(string folderPath, string fileName)
		{
			Directory.CreateDirectory(folderPath);
			string pathAndName = Path.Combine(folderPath, fileName);
			string text = $"{MakeModelEnumText()}\n{MakeClipEnumsText()}";
			File.WriteAllText(pathAndName, text);
			return true;
		}

		string MakeModelEnumText()
		{
			// example --> public enum Model { Horse, Farmer, Chicken, Goat }
			List<string> values = new List<string>();
			foreach (var model in Models) { values.Add(model.ModelName); }
			return EnumUtils.MakeEnumDeclaration("Model", values, "short", true);
		}

		string MakeClipEnumsText()
		{
			List<string> values = new List<string>();
			string text = "";
			foreach (var model in Models) {
				values.Clear();
				foreach (var clip in model.Clips) { values.Add(clip.ClipName); }
				text += EnumUtils.MakeEnumDeclaration(model.ModelName, values, "byte", true);
			}
			return text;
		}
	}

} // namespace