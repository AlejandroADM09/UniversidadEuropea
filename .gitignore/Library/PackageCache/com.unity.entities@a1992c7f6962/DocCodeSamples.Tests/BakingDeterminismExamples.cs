using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEngine;

namespace Doc.CodeSamples.Tests
{
    #region determinism-check-selected
    static class CheckSubSceneDeterminism
    {
        [MenuItem("Tools/Check subscene determinism")]
        static void Check()
        {
            var subScene = Selection.activeGameObject.GetComponent<SubScene>();
            var scenePath = AssetDatabase.GetAssetPath(subScene.SceneAsset);

            var result = EntitySceneImporterDeterminismChecker.Check(scenePath);

            if (!result.Success)
                Debug.LogError($"The check didn't run: {result.Error}");
            else if (result.IsDeterministic)
                Debug.Log($"{scenePath} produced {result.FirstHash} both times.");
            else
                Debug.LogError($"{scenePath} produced {result.FirstHash}, " +
                    $"then {result.SecondHash}.\n{result.Diagnostics}");
        }

        // Enable the menu item only when the selected GameObject has a SubScene component.
        [MenuItem("Tools/Check subscene determinism", validate = true)]
        static bool CheckValidate()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<SubScene>() != null;
        }
    }
    #endregion

    #region determinism-record-hashes
    static class RecordSubSceneHashes
    {
        [MenuItem("Tools/Record subscene hashes")]
        static void Record()
        {
            foreach (var subScene
                    in Object.FindObjectsByType<SubScene>(FindObjectsInactive.Include))
            {
                var scenePath = AssetDatabase.GetAssetPath(subScene.SceneAsset);
                var result = EntitySceneImporterDeterminismChecker.Check(scenePath);

                if (result.Success)
                    Debug.Log($"{scenePath},{result.FirstHash},{result.IsDeterministic}");
                else
                    Debug.LogError($"{scenePath},{result.Error}");
            }
        }
    }
    #endregion
}
