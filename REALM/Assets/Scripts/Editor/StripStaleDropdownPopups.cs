using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Realm.Editor
{
    // TMP_Dropdown builds a "Dropdown List" child when it opens and destroys it
    // on close. Opening one in the editor and saving while it is open bakes that
    // popup into the scene, where it renders at sortingOrder 30000 with its own
    // GraphicRaycaster - it shows stale options and swallows every click in its
    // area. That cost us a "card review button does nothing" bug once already,
    // so the popups are stripped automatically on every scene save.
    [InitializeOnLoad]
    internal static class StripStaleDropdownPopups
    {
        static StripStaleDropdownPopups()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (!scene.IsValid()) return;

            var doomed = new System.Collections.Generic.List<GameObject>();
            foreach (var root in scene.GetRootGameObjects()) Collect(root.transform, doomed);
            if (doomed.Count == 0) return;

            foreach (var go in doomed)
            {
                Debug.LogWarning($"[Realm] Removed a baked-in TMP dropdown popup under '{go.transform.parent?.name}' before saving. " +
                                 "It would have blocked clicks at runtime.", go.transform.parent);
                Object.DestroyImmediate(go);
            }
        }

        private static void Collect(Transform t, System.Collections.Generic.List<GameObject> into)
        {
            if (t.name == "Dropdown List")
            {
                into.Add(t.gameObject);
                return; // no need to walk inside one we are deleting
            }
            for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), into);
        }
    }
}
