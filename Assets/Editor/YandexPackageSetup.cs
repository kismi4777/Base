#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Подсказка и проверка локального пути пакета Yandex Games в manifest.json.
/// </summary>
static class YandexPackageSetup
{
    const string PackageFolder = "Packages/com.bananaparty.yandexgames";
    const string ManifestPath = "Packages/manifest.json";

    [MenuItem("Tools/Yandex/Проверить путь SDK в manifest")]
    static void CheckManifestPath()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string localPackage = Path.Combine(projectRoot, PackageFolder);

        if (Directory.Exists(localPackage))
        {
            Debug.Log($"[Yandex] Локальный пакет найден: {localPackage}. " +
                      $"Рекомендуемый manifest: \"com.bananaparty.yandexgames\": \"file:{PackageFolder}\"");
            return;
        }

        Debug.LogWarning(
            $"[Yandex] Папка {PackageFolder} не найдена. " +
            "Скопируйте SDK в Packages/com.bananaparty.yandexgames и обновите manifest.json, " +
            "чтобы путь не зависел от Downloads.");
    }
}
#endif
