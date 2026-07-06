using UnityEditor;
using UnityEngine;

namespace BadFaith.EditorTools
{
    /// <summary>
    /// Répare les matériaux restés roses après le Render Pipeline Converter :
    /// les shaders custom/built-in non mappés sont forcés vers URP/Lit en
    /// préservant texture principale et couleur (les matériaux Synty sont
    /// de simples atlas + teinte, ça suffit).
    /// </summary>
    public static class SyntyMaterialFixer
    {
        [MenuItem("MAUVAISE FOI/Réparer les matériaux roses (Synty)")]
        public static void FixPinkMaterials()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[SyntyMaterialFixer] Shader URP/Lit introuvable — le projet est-il bien en URP ?");
                return;
            }

            int fixedCount = 0, checkedCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SyntyStudios" });

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null || mat.shader == null)
                        continue;
                    checkedCount++;

                    string shaderName = mat.shader.name;
                    bool broken = shaderName == "Hidden/InternalErrorShader"
                                  || shaderName.StartsWith("Standard")
                                  || shaderName.StartsWith("Legacy")
                                  || shaderName.StartsWith("Particles/")
                                  || shaderName.StartsWith("Mobile/")
                                  || shaderName.StartsWith("SyntyStudios/");
                    if (!broken)
                        continue;

                    // Sauve texture + couleur avant de changer de shader.
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    if (mainTex == null && mat.HasProperty("_BaseMap"))
                        mainTex = mat.GetTexture("_BaseMap");
                    Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color")
                        : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;

                    mat.shader = urpLit;
                    if (mainTex != null)
                        mat.SetTexture("_BaseMap", mainTex);
                    mat.SetColor("_BaseColor", color);
                    mat.SetFloat("_Smoothness", 0.1f); // le low-poly n'aime pas briller

                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[SyntyMaterialFixer] {fixedCount} matériaux réparés sur {checkedCount} vérifiés. S'il reste du rose : dis-le-moi avec le nom de l'objet.");
        }
    }
}
