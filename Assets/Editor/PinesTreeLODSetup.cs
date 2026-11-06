using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Configura un LOD Group funcional en el prefab PinesTree_01_Snow.
// El pack solo trae una malla por arbol, asi que:
//   LOD0 = malla original con sombras
//   LOD1 = copia de la malla sin sombras (mas barata)
//   Culled = por debajo del 2% de altura en pantalla
public static class PinesTreeLODSetup
{
    const string PrefabPath = "Assets/Invierno/Prefabs/PinesTree_01_Snow.prefab";

    [MenuItem("Tools/LOD/Configurar LOD Group PinesTree_01_Snow")]
    public static void Setup()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (var old in root.GetComponentsInChildren<LODGroup>(true))
                Object.DestroyImmediate(old);

            var lod1Existing = root.transform.Find("PinesTree_01_LOD1");
            if (lod1Existing != null)
                Object.DestroyImmediate(lod1Existing.gameObject);

            var lod0 = root.GetComponentInChildren<MeshRenderer>(true);
            if (lod0 == null)
            {
                Debug.LogError("No se encontro MeshRenderer en " + PrefabPath);
                return;
            }
            lod0.shadowCastingMode = ShadowCastingMode.On;

            var lod1Go = new GameObject("PinesTree_01_LOD1");
            lod1Go.transform.SetParent(lod0.transform.parent, false);
            lod1Go.transform.localPosition = lod0.transform.localPosition;
            lod1Go.transform.localRotation = lod0.transform.localRotation;
            lod1Go.transform.localScale = lod0.transform.localScale;
            lod1Go.isStatic = lod0.gameObject.isStatic;
            lod1Go.AddComponent<MeshFilter>().sharedMesh = lod0.GetComponent<MeshFilter>().sharedMesh;
            var lod1 = lod1Go.AddComponent<MeshRenderer>();
            lod1.sharedMaterials = lod0.sharedMaterials;
            lod1.shadowCastingMode = ShadowCastingMode.Off;
            lod1.receiveShadows = false;

            var group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.SetLODs(new[]
            {
                new LOD(0.25f, new Renderer[] { lod0 }),
                new LOD(0.02f, new Renderer[] { lod1 }),
            });
            group.RecalculateBounds();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("LOD Group configurado en " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        RemoveBrokenSceneOverrides();
    }

    // Quita los LOD Group agregados a mano en las instancias de la escena,
    // que tenian niveles vacios y hacian desaparecer el arbol.
    static void RemoveBrokenSceneOverrides()
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        foreach (var group in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!PrefabUtility.IsAddedComponentOverride(group)) continue;
            var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(group);
            if (instanceRoot == null || PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot) != prefabAsset) continue;
            Undo.DestroyObjectImmediate(group);
            Debug.Log("Eliminado LOD Group roto de la instancia " + instanceRoot.name);
        }
    }
}
