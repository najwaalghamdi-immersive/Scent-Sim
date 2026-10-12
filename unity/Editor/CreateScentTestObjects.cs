using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only helper for testing: finds every ScentMaker asset in the
/// project and spawns one cube per scent (up to 10), each already wired
/// with a ScentSource + ScentTrigger pointing at that scent. Enter Play
/// mode and walk the Player into a cube to fire its scent — no manual
/// Inspector wiring needed. Must live under an "Editor" folder (or an
/// asmdef with includePlatforms: ["Editor"]) so it's excluded from
/// player builds.
/// </summary>
public static class CreateScentTestObjects
{
    [MenuItem("Scent Sim/Create Scent Test Objects")]
    public static void Create()
    {
        ScentMaker[] scents = AssetDatabase.FindAssets("t:ScentMaker")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ScentMaker>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(scent => scent != null)
            .OrderBy(scent => scent.port)
            .ToArray();

        if (scents.Length == 0)
        {
            Debug.LogError("[Olorama] No ScentMaker assets found in the project. Create some first: Assets > Create > Scriptable Objects > ScentMaker.");
            return;
        }

        int count = Mathf.Min(10, scents.Length);
        if (scents.Length < 10)
            Debug.LogWarning($"[Olorama] Only found {scents.Length} ScentMaker asset(s) — creating {count} test object(s) instead of 10.");
        else if (scents.Length > 10)
            Debug.LogWarning($"[Olorama] Found {scents.Length} ScentMaker assets — only the first 10 (by port) get a test object.");

        GameObject parent = new GameObject("ScentTestObjects");
        Undo.RegisterCreatedObjectUndo(parent, "Create Scent Test Objects");

        for (int i = 0; i < count; i++)
        {
            ScentMaker scent = scents[i];

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Create Scent Test Objects");
            cube.name = $"Scent_{scent.name}_Port{scent.port}";
            cube.transform.SetParent(parent.transform);
            cube.transform.localScale = Vector3.one * 0.5f;
            cube.transform.position = new Vector3((i - (count - 1) / 2f) * 1.5f, 0.75f, 2f);

            Renderer renderer = cube.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.HSVToRGB((float)i / count, 0.65f, 0.95f);
            renderer.sharedMaterial = material;

            Collider col = cube.GetComponent<Collider>();
            col.isTrigger = true;

            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            ScentSource source = cube.AddComponent<ScentSource>();
            source.scent = scent;

            cube.AddComponent<ScentTrigger>();
        }

        Selection.activeGameObject = parent;
        Debug.Log($"[Olorama] Created {count} scent test cube(s) under \"ScentTestObjects\", 1.5m apart starting ~2m in front of the origin. Enter Play mode and walk the Player (tagged \"Player\", with a non-kinematic Rigidbody) into each to fire ScentTrigger -> ScentSource -> UDP.");
    }
}
