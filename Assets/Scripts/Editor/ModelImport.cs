// Импорт 3D-моделей из Tools/Blender (Assets/Resources/Models, Assets/Models): без анимаций, камер и света,
// материалы собираются по имени. Имя материала из генератора: LB_<цвет>_r<шероховатость>_m<металл>_e<свечение×10>_c<лак×10>,
// например LB_e8483f_r85_m00_e00_c00. Работает и со встроенным рендером (Standard), и с URP (Lit).
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

public class ModelImport : AssetPostprocessor
{
    static readonly Regex MatName = new Regex(@"^LB_([0-9a-fA-F]{6})_r(\d+)_m(\d+)_e(\d+)_c(\d+)");

    static bool Ours(string path) => path.Replace('\\', '/').Contains("/Models/");

    void OnPreprocessModel()
    {
        if (!Ours(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.importAnimation = false;
        mi.animationType = ModelImporterAnimationType.None;
        mi.importCameras = false;
        mi.importLights = false;
        mi.importBlendShapes = false;
        mi.isReadable = false;
        mi.useFileScale = true;
        mi.importNormals = ModelImporterNormals.Import;
    }

    void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
    {
        if (!Ours(assetPath)) return;
        var m = MatName.Match(description.materialName ?? "");
        if (!m.Success) return;
        ColorUtility.TryParseHtmlString("#" + m.Groups[1].Value, out var color);
        float rough = Num(m, 2) / 100f, metal = Num(m, 3) / 100f, emit = Num(m, 4) / 10f, coat = Num(m, 5) / 10f;
        // Лак (coat) у Blender — глянцевый слой поверх: в Unity его нет, делаем поверхность глаже
        float smooth = Mathf.Clamp01(1f - rough * (1f - coat * 0.6f));

        bool urp = GraphicsSettings.defaultRenderPipeline != null;
        var shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (shader != null) material.shader = shader;
        material.name = description.materialName;
        material.SetColor("_Color", color);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metal);
        material.SetFloat("_Glossiness", smooth);
        material.SetFloat("_Smoothness", smooth);
        if (emit > 0)
        {
            // Неон и экраны: свечение тем же цветом (без bloom — просто не темнеют в тени)
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", color * Mathf.Min(1.4f, 0.35f + emit * 0.12f));
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
    }

    static float Num(Match m, int g) => float.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
}
