using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Editor-only helper for testing: finds every ScentMaker asset in the
/// project and spawns one cube per scent (up to 10), each already wired
/// with a ScentSource + ScentTrigger (collision) + XRSimpleInteractable +
/// ScentInteractable (reach-and-select, works for controller ray, poke,
/// or hand-tracking pinch) pointing at that scent. Also spawns one
/// TestGun with a ScentGun aimed at the row. Enter Play mode, walk the
/// Player into a cube, reach out and select one, or fire the gun at one —
/// no manual Inspector wiring needed for any of the three. Must live
/// under an "Editor" folder (or an asmdef with
/// includePlatforms: ["Editor"]) so it's excluded from player builds.
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
            // isTrigger so ScentTrigger's OnTriggerEnter fires on walk-in.
            // XRSimpleInteractable works fine with a trigger collider for
            // Poke/Direct interactors; a Ray interactor needs its own
            // "Raycast Trigger Interaction" set to Collide (not the default
            // Ignore) to register hits on it — see the README note.
            col.isTrigger = true;

            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            ScentSource source = cube.AddComponent<ScentSource>();
            source.scent = scent;

            cube.AddComponent<ScentTrigger>();
            cube.AddComponent<XRSimpleInteractable>();
            cube.AddComponent<ScentInteractable>();
        }

        GameObject gun = new GameObject("TestGun");
        Undo.RegisterCreatedObjectUndo(gun, "Create Scent Test Objects");
        gun.transform.SetParent(parent.transform);
        gun.transform.position = new Vector3(0f, 0.75f, -1f);

        GameObject muzzle = new GameObject("Muzzle");
        Undo.RegisterCreatedObjectUndo(muzzle, "Create Scent Test Objects");
        muzzle.transform.SetParent(gun.transform);
        muzzle.transform.localPosition = Vector3.zero;
        muzzle.transform.localRotation = Quaternion.identity;

        ScentGun scentGun = gun.AddComponent<ScentGun>();
        SerializedObject gunSerialized = new SerializedObject(scentGun);
        gunSerialized.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        gunSerialized.ApplyModifiedProperties();

        Selection.activeGameObject = parent;
        Debug.Log($"[Olorama] Created {count} scent test cube(s) plus \"TestGun\" under \"ScentTestObjects\", 1.5m apart ~2m in front of the origin, gun at ~1m behind that facing +Z. Enter Play mode: walk the Player (tagged \"Player\", non-kinematic Rigidbody) into a cube for ScentTrigger, reach out and select one for ScentInteractable (controller ray/poke/hand pinch), or left-click (new Input System) / Fire1 (old Input Manager) with TestGun facing a cube for ScentGun. All three read the same ScentSource -> ScentMaker -> OloramaUDPSender -> UDP.");
    }
}
