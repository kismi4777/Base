using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainMenuUI))]
public sealed class MainMenuUiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MainMenuUI menu = (MainMenuUI)target;
        if (menu == null)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("UI в сцене", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Соберите UI в сцену, чтобы видеть и настраивать элементы в Hierarchy/Scene без Play Mode. " +
            "DailyTasksPanel не пересоздаётся — настройте его вручную под Home, код только подключит текст и скролл. " +
            "Кнопка «Собрать UI» пересоздаст MainMenuHUD и удалит ручную настройку DailyTasksPanel.",
            MessageType.Info);

        if (GUILayout.Button("Собрать UI в сцене", GUILayout.Height(28f)))
        {
            Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Build Main Menu UI");
            menu.BuildUiInScene();
        }

        if (GUILayout.Button("Очистить сгенерированный UI"))
        {
            if (EditorUtility.DisplayDialog("Очистить UI", "Удалить MainMenuHUD и все оверлеи с Canvas?", "Да", "Отмена"))
            {
                Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Clear Main Menu UI");
                menu.ClearSceneUi();
            }
        }
    }
}
