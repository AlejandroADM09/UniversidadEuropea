using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Scenes;
using Unity.Scenes.Editor.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Serializable]
public class SubSceneDomainReloadTests
{
    [SerializeField]
    string m_TempAssetDir;

    [SerializeField]
    GameObject m_SubSceneAGO;

    [SerializeField]
    GameObject m_SubSceneBGO;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var guid = AssetDatabase.CreateFolder("Assets", nameof(SubSceneDomainReloadTests));
        m_TempAssetDir = AssetDatabase.GUIDToAssetPath(guid);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        AssetDatabase.DeleteAsset(m_TempAssetDir);
    }

    [TearDown]
    public void TearDown()
    {
        // Triggers OnDisable on the SubScenes, removing them from the static AllSubScenes list.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    static int CountInAllSubScenes(SubScene subScene)
    {
        int count = 0;
        foreach (var s in SubScene.AllSubScenes)
        {
            if (s == subScene)
                count++;
        }
        return count;
    }

    [UnityTest, Description("Regression from PR #111448: a domain reload with two SubScenes referencing different scenes ran OnValidate before OnEnable for the second SubScene, so both re-registered it and AddSceneEntities was called twice. Each SubScene must be registered exactly once after a domain reload.")]
    public IEnumerator DomainReload_WithTwoSubScenes_RegistersEachExactlyOnce()
    {
        var mainScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(mainScene);
        var mainPath = Path.Combine(m_TempAssetDir, "DomainReloadMain.unity");
        EditorSceneManager.SaveScene(mainScene, mainPath);

        // Two SubScenes in the same main scene, referencing two distinct (empty) scenes, closed for editing.
        var subA = SubSceneTestsHelper.CreateSubSceneInSceneFromObjects("DR_SubA", false, mainScene);
        var subB = SubSceneTestsHelper.CreateSubSceneInSceneFromObjects("DR_SubB", false, mainScene);
        m_SubSceneAGO = subA.gameObject;
        m_SubSceneBGO = subB.gameObject;

        Assert.AreNotEqual(subA.SceneGUID, subB.SceneGUID, "Test requires the two SubScenes to reference different scenes");
        Assert.AreEqual(1, CountInAllSubScenes(subA), "Precondition: SubScene A registered exactly once before reload");
        Assert.AreEqual(1, CountInAllSubScenes(subB), "Precondition: SubScene B registered exactly once before reload");

        EditorSceneManager.SaveScene(mainScene);

        // Force a real domain reload (no script compilation needed): this reproduces the
        // OnValidate-before-OnEnable restoration ordering, combined with the default world
        // only being created during the first OnEnable.
        EditorUtility.RequestScriptReload();
        yield return new WaitForDomainReload();

        var reloadedA = m_SubSceneAGO.GetComponent<SubScene>();
        var reloadedB = m_SubSceneBGO.GetComponent<SubScene>();

        Assert.AreEqual(1, CountInAllSubScenes(reloadedA), "SubScene A should be in AllSubScenes exactly once after a domain reload");
        Assert.AreEqual(1, CountInAllSubScenes(reloadedB), "SubScene B should be in AllSubScenes exactly once after a domain reload");
    }
}
