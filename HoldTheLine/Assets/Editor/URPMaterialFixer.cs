// URPMaterialFixer.cs - Converts Standard/Built-in materials to URP shaders
// Location: Assets/Editor/
// Menu: Tools > URP Material Fixer > ...

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace HoldTheLine.Editor
{
    /// <summary>
    /// Editor tool to find and convert non-URP materials to URP shaders.
    /// Fixes pink rendering caused by Standard/Built-in shaders in URP projects.
    /// </summary>
    public class URPMaterialFixer : EditorWindow
    {
        // Shader references
        private static Shader urpUnlitShader;
        private static Shader urpLitShader;

        // Results tracking
        private static List<string> convertedMaterials = new List<string>();
        private static List<string> skippedMaterials = new List<string>();
        private static List<string> errorMaterials = new List<string>();

        // Non-URP shader names to detect
        private static readonly string[] nonURPShaderNames = new string[]
        {
            "Standard",
            "Standard (Specular setup)",
            "Legacy Shaders/",
            "Mobile/",
            "Nature/",
            "Particles/",
            "Skybox/",
            "Sprites/",
            "UI/",
            "Unlit/Color",
            "Unlit/Texture",
            "Unlit/Transparent",
            "Unlit/Transparent Cutout"
        };

        // URP shader names (these are already correct)
        private static readonly string[] urpShaderPrefixes = new string[]
        {
            "Universal Render Pipeline/",
            "Shader Graphs/",
            "Hidden/",
            "URP/",
            "Sprites/Default" // URP handles this one
        };

        [MenuItem("Tools/URP Material Fixer/Scan Materials (Dry Run)")]
        public static void ScanMaterialsDryRun()
        {
            ScanAndConvertMaterials(false, false);
        }

        [MenuItem("Tools/URP Material Fixer/Convert to URP Unlit")]
        public static void ConvertToURPUnlit()
        {
            if (EditorUtility.DisplayDialog(
                "Convert Materials to URP Unlit",
                "This will convert all non-URP materials to Universal Render Pipeline/Unlit shader.\n\n" +
                "Base colors will be preserved where possible.\n\n" +
                "This operation modifies asset files on disk. Continue?",
                "Convert", "Cancel"))
            {
                ScanAndConvertMaterials(true, false);
            }
        }

        [MenuItem("Tools/URP Material Fixer/Convert to URP Lit")]
        public static void ConvertToURPLit()
        {
            if (EditorUtility.DisplayDialog(
                "Convert Materials to URP Lit",
                "This will convert all non-URP materials to Universal Render Pipeline/Lit shader.\n\n" +
                "Base colors will be preserved where possible.\n\n" +
                "This operation modifies asset files on disk. Continue?",
                "Convert", "Cancel"))
            {
                ScanAndConvertMaterials(true, true);
            }
        }

        [MenuItem("Tools/URP Material Fixer/Show Conversion Report")]
        public static void ShowReport()
        {
            if (convertedMaterials.Count == 0 && skippedMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "No conversion has been run yet. Run a scan or conversion first.", "OK");
                return;
            }

            string report = GenerateReport();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Conversion Report",
                $"Converted: {convertedMaterials.Count}\n" +
                $"Skipped (already URP): {skippedMaterials.Count}\n" +
                $"Errors: {errorMaterials.Count}\n\n" +
                "Full details logged to Console.", "OK");
        }

        private static void ScanAndConvertMaterials(bool applyChanges, bool useLitShader)
        {
            // Clear previous results
            convertedMaterials.Clear();
            skippedMaterials.Clear();
            errorMaterials.Clear();

            // Find URP shaders
            urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpUnlitShader == null)
            {
                Debug.LogError("[URPMaterialFixer] Could not find 'Universal Render Pipeline/Unlit' shader. Is URP installed?");
                return;
            }

            if (useLitShader && urpLitShader == null)
            {
                Debug.LogError("[URPMaterialFixer] Could not find 'Universal Render Pipeline/Lit' shader. Is URP installed?");
                return;
            }

            Shader targetShader = useLitShader ? urpLitShader : urpUnlitShader;
            string targetShaderName = useLitShader ? "URP Lit" : "URP Unlit";

            Debug.Log($"[URPMaterialFixer] Starting {(applyChanges ? "conversion" : "scan")} - Target: {targetShaderName}");

            // Find all material assets
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            int totalMaterials = materialGuids.Length;
            int processed = 0;

            try
            {
                foreach (string guid in materialGuids)
                {
                    processed++;
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip materials outside Assets folder
                    if (!path.StartsWith("Assets/"))
                    {
                        continue;
                    }

                    // Skip materials in Packages
                    if (path.StartsWith("Packages/"))
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Scanning Materials",
                        $"Processing: {path}",
                        (float)processed / totalMaterials);

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        errorMaterials.Add($"{path} (could not load)");
                        continue;
                    }

                    if (mat.shader == null)
                    {
                        errorMaterials.Add($"{path} (null shader)");
                        continue;
                    }

                    string shaderName = mat.shader.name;

                    // Check if already using URP shader
                    if (IsURPShader(shaderName))
                    {
                        skippedMaterials.Add($"{path} (shader: {shaderName})");
                        continue;
                    }

                    // Check if it's a non-URP shader that needs conversion
                    if (IsNonURPShader(shaderName))
                    {
                        if (applyChanges)
                        {
                            ConvertMaterial(mat, path, targetShader, useLitShader);
                        }
                        else
                        {
                            convertedMaterials.Add($"{path} (shader: {shaderName}) -> WOULD CONVERT");
                        }
                    }
                    else
                    {
                        // Unknown shader - flag it
                        skippedMaterials.Add($"{path} (shader: {shaderName}) [UNKNOWN - not converted]");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Save all changes
            if (applyChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Log report
            string report = GenerateReport();
            Debug.Log(report);

            // Show summary dialog
            string modeText = applyChanges ? "Conversion" : "Scan";
            EditorUtility.DisplayDialog(
                $"{modeText} Complete",
                $"Materials {(applyChanges ? "converted" : "found for conversion")}: {convertedMaterials.Count}\n" +
                $"Already URP / Skipped: {skippedMaterials.Count}\n" +
                $"Errors: {errorMaterials.Count}\n\n" +
                "See Console for full details.",
                "OK");
        }

        private static bool IsURPShader(string shaderName)
        {
            foreach (string prefix in urpShaderPrefixes)
            {
                if (shaderName.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsNonURPShader(string shaderName)
        {
            foreach (string name in nonURPShaderNames)
            {
                if (shaderName == name || shaderName.StartsWith(name))
                {
                    return true;
                }
            }
            return false;
        }

        private static void ConvertMaterial(Material mat, string path, Shader targetShader, bool useLitShader)
        {
            try
            {
                string originalShader = mat.shader.name;

                // Preserve color properties before changing shader
                Color baseColor = Color.white;
                Color emissionColor = Color.black;
                bool hasEmission = false;

                // Try to get color from various property names
                if (mat.HasProperty("_Color"))
                {
                    baseColor = mat.GetColor("_Color");
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    baseColor = mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_TintColor"))
                {
                    baseColor = mat.GetColor("_TintColor");
                }

                // Try to get emission
                if (mat.HasProperty("_EmissionColor"))
                {
                    emissionColor = mat.GetColor("_EmissionColor");
                    hasEmission = emissionColor != Color.black;
                }

                // Get main texture if any
                Texture mainTexture = null;
                if (mat.HasProperty("_MainTex"))
                {
                    mainTexture = mat.GetTexture("_MainTex");
                }

                // Change shader
                mat.shader = targetShader;

                // Apply preserved properties to new shader
                if (useLitShader)
                {
                    // URP Lit shader properties
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", baseColor);
                    }
                    if (mat.HasProperty("_BaseMap") && mainTexture != null)
                    {
                        mat.SetTexture("_BaseMap", mainTexture);
                    }
                    if (hasEmission && mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", emissionColor);
                    }
                }
                else
                {
                    // URP Unlit shader properties
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", baseColor);
                    }
                    if (mat.HasProperty("_BaseMap") && mainTexture != null)
                    {
                        mat.SetTexture("_BaseMap", mainTexture);
                    }
                }

                // Mark as dirty so Unity saves it
                EditorUtility.SetDirty(mat);

                convertedMaterials.Add($"{path} ({originalShader} -> {targetShader.name})");
                Debug.Log($"[URPMaterialFixer] Converted: {path} | Color: {baseColor}");
            }
            catch (System.Exception e)
            {
                errorMaterials.Add($"{path} (error: {e.Message})");
                Debug.LogError($"[URPMaterialFixer] Error converting {path}: {e.Message}");
            }
        }

        private static string GenerateReport()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("========================================");
            sb.AppendLine("   URP MATERIAL FIXER - REPORT");
            sb.AppendLine("========================================");
            sb.AppendLine();

            sb.AppendLine($"CONVERTED ({convertedMaterials.Count}):");
            sb.AppendLine("----------------------------------------");
            foreach (string item in convertedMaterials)
            {
                sb.AppendLine($"  ✓ {item}");
            }
            if (convertedMaterials.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            sb.AppendLine();

            sb.AppendLine($"SKIPPED / ALREADY URP ({skippedMaterials.Count}):");
            sb.AppendLine("----------------------------------------");
            foreach (string item in skippedMaterials)
            {
                sb.AppendLine($"  - {item}");
            }
            if (skippedMaterials.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            sb.AppendLine();

            sb.AppendLine($"ERRORS ({errorMaterials.Count}):");
            sb.AppendLine("----------------------------------------");
            foreach (string item in errorMaterials)
            {
                sb.AppendLine($"  ✗ {item}");
            }
            if (errorMaterials.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            sb.AppendLine();

            sb.AppendLine("========================================");
            sb.AppendLine($"SUMMARY: {convertedMaterials.Count} converted, {skippedMaterials.Count} skipped, {errorMaterials.Count} errors");
            sb.AppendLine("========================================");

            return sb.ToString();
        }

        /// <summary>
        /// Creates a new URP-compatible material at runtime.
        /// Call this from game scripts instead of using Shader.Find("Standard").
        /// </summary>
        public static Material CreateURPMaterial(Color color, bool useEmission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // Fallback - try Lit
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                Debug.LogError("[URPMaterialFixer] Could not find URP shader!");
                return new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", color);

            return mat;
        }
    }
}
#endif
