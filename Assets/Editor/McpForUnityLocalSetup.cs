#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Указывает MCP for Unity локальный Server (beta), чтобы не тянуть mcpforunityserver с PyPI.
/// </summary>
[InitializeOnLoad]
static class McpForUnityLocalSetup
{
    const string GitUrlOverrideKey = "MCPForUnity.GitUrlOverride";
    const string ServerRelativePath = "Tools/unity-mcp/Server";

    static McpForUnityLocalSetup()
    {
        if (!string.IsNullOrEmpty(EditorPrefs.GetString(GitUrlOverrideKey, "")))
            return;

        string fullPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", ServerRelativePath));

        if (!System.IO.File.Exists(System.IO.Path.Combine(fullPath, "pyproject.toml")))
            return;

        EditorPrefs.SetString(GitUrlOverrideKey, fullPath);
        UnityEngine.Debug.Log($"[MCP for Unity] Git URL Override: {fullPath}");
    }
}
#endif
