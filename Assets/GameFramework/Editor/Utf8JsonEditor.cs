using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    public class Utf8JsonEditor
    {
        public static void GenerateJsonFormatter()
        {
            string exePath = Path.GetFullPath(".");
            exePath = Path.Combine(exePath, "../../tool/Utf8Json.UniversalCodeGenerator/win-x64/Utf8Json.UniversalCodeGenerator.exe");
            exePath = Path.GetFullPath(exePath);
            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, GameEditorConfig.Utf8JsonCastTypePath.CastFormaterOutPath));
            string param0 = "-d ";
            var pathList = GameEditorConfig.Utf8JsonCastTypePath.CastTypePath;
            for (int i = 0; i < pathList.Count; i++)
            {
                string tempPath = Path.GetFullPath(Path.Combine(Application.dataPath, pathList[i]));
                if (i == 0)
                {
                    param0 += tempPath;
                }
                else
                {
                    param0 += ("," + tempPath);
                }
            }
            string param1 = $"-o {outputPath}";
            string argument = $"{param0} {param1}";
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.FileName = exePath;
                    process.StartInfo.Arguments = argument;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    process.WaitForExit();
                    GameFramework.Debug.Log(process.StandardOutput.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                GameFramework.Debug.LogError($"生成反序列化脚本出现异常：{e}");
            }
            AssetDatabase.Refresh();
        }
    }
}
