using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SL.Systems;

namespace SL.EditorTools
{
    /// <summary>Makes the owner's OBJ base meshes readable at runtime (needed to paint species colours) and sets sane import options.</summary>
    public class MeshImportSettings : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Meshes/")) return;
            ModelImporter mi = (ModelImporter)assetImporter;
            mi.isReadable = true;
            mi.importNormals = ModelImporterNormals.Import;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.importBlendShapes = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.globalScale = 1f;
        }
    }

    public static class SLProjectTools
    {
        [MenuItem("Soulbound/Use Direct3D11 on Windows")]
        public static void UseDirect3D11()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 });
            Debug.Log("[Soulbound] Windows graphics API set to Direct3D11. Restart the editor if Unity asks.");
        }

        [MenuItem("Soulbound/Validate Systems")]
        public static void ValidateSystems()
        {
            int problems = 0;
            System.Collections.Generic.List<string> errors = CombatActionLibrary.ValidateAll();
            for (int i = 0; i < errors.Count; i++) { Debug.LogError("[Soulbound] Combat: " + errors[i]); problems++; }
            for (int i = 0; i < BeginnerSpecies.All.Count; i++)
            {
                SpeciesRecord s = BeginnerSpecies.All[i];
                if (!BaseMeshLibrary.Exists(s.BaseMesh)) { Debug.LogError("[Soulbound] Missing mesh Assets/Resources/Meshes/" + s.BaseMesh + ".obj for " + s.Name); problems++; }
                for (int a = 0; a < s.Actions.Length; a++)
                    if (CombatActionLibrary.Get(s.Actions[a]) == null) { Debug.LogError("[Soulbound] " + s.Name + " uses unknown action " + s.Actions[a]); problems++; }
            }
            Debug.Log("[Soulbound] Validate Systems finished: " + CombatActionLibrary.All.Count + " combat actions, " + BeginnerSpecies.All.Count + " beginner species, " + problems + " problem(s).");
        }

        [MenuItem("Soulbound/Export Project Zip (source for Cloud Build)")]
        public static void ExportZip()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SoulboundLegends_Project_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".zip");
            string[] dirs = new string[] { "Assets", "Packages", "ProjectSettings" };
            using (FileStream fs = new FileStream(dest, FileMode.Create))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                for (int d = 0; d < dirs.Length; d++)
                {
                    string full = Path.Combine(root, dirs[d]);
                    if (!Directory.Exists(full)) continue;
                    string[] files = Directory.GetFiles(full, "*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string rel = files[i].Substring(root.Length + 1).Replace('\\', '/');
                        zip.CreateEntryFromFile(files[i], rel, System.IO.Compression.CompressionLevel.Optimal);
                    }
                }
            }
            Debug.Log("[Soulbound] Project zip written to " + dest);
            EditorUtility.RevealInFinder(dest);
        }
    }
}
