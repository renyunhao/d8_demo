using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    public class MeshUtilEditor
    {
        [MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
        public static void SaveMeshInPlace(MenuCommand menuCommand)
        {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;
            SaveMesh(m, m.name, false, true);
        }

        [MenuItem("CONTEXT/MeshFilter/Save Mesh As New Instance...")]
        public static void SaveMeshNewInstanceItem(MenuCommand menuCommand)
        {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;
            SaveMesh(m, m.name, true, true);
        }

        public static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
        {
            string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);

            Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);

            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("CONTEXT/MeshFilter/Save Mesh As Obj...")]
        public static void SaveMeshAsObj(MenuCommand menuCommand)
        {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;

            string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", m.name, "obj");
            if (string.IsNullOrEmpty(path)) return;
            using (StreamWriter sw = new StreamWriter(path))
            {
                Material[] mats = mf.GetComponent<MeshRenderer>().sharedMaterials;

                StringBuilder sb = new StringBuilder();

                sb.Append("g ").Append(mf.name).Append("\n");
                foreach (Vector3 v in m.vertices)
                {
                    sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
                }
                sb.Append("\n");
                foreach (Vector3 v in m.normals)
                {
                    sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
                }
                sb.Append("\n");
                foreach (Vector3 v in m.uv)
                {
                    sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
                }
                for (int material = 0; material < m.subMeshCount; material++)
                {
                    sb.Append("\n");
                    sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                    sb.Append("usemap ").Append(mats[material].name).Append("\n");

                    int[] triangles = m.GetTriangles(material);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                            triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
                    }
                }
                sw.Write(sb.ToString());
            }
        }
    }

}