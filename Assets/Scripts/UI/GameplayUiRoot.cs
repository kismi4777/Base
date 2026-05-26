using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Настраивает EventSystem для UGUI в геймплее.
/// </summary>
[DefaultExecutionOrder(-200)]
public class GameplayUiRoot : MonoBehaviour
{
    void Awake()
    {
        ConfigureEventSystem();
    }

    static void ConfigureEventSystem()
    {
        if (EventSystem.current == null)
            return;

        BaseInputModule[] modules = EventSystem.current.GetComponents<BaseInputModule>();
        for (int i = 0; i < modules.Length; i++)
        {
            BaseInputModule module = modules[i];
            if (module == null)
                continue;

            if (module.GetType().Name == "InputSystemUIInputModule")
                module.enabled = false;
            else if (module is StandaloneInputModule)
                module.enabled = true;
        }
    }
}
