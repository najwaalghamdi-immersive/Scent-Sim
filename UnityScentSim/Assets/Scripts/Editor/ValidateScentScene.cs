using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only sanity check for the scent setup, catching the class of bug
/// that's already shown up by hand once (two OloramaUDPSenders in the same
/// scene, found only by reading a Console log after the fact). Must live
/// under an "Editor" folder (or an asmdef with
/// includePlatforms: ["Editor"]) so it's excluded from player builds.
/// </summary>
public static class ValidateScentScene
{
    [MenuItem("Scent Sim/Validate Scene")]
    public static void Validate()
    {
        int issues = 0;

        issues += CheckSingleton<OloramaUDPSender>("OloramaUDPSender");
        issues += CheckSingleton<ScentSelection>("ScentSelection");
        issues += CheckScentMakerPortCollisions();
        issues += CheckTriggers();
        issues += CheckGuns();
        issues += CheckInteractables();
        issues += CheckMenus();

        if (issues == 0)
            Debug.Log("[Olorama] Validate Scene: no issues found.");
        else
            Debug.LogWarning($"[Olorama] Validate Scene: {issues} issue(s) found — see warnings/errors above.");
    }

    static T[] FindAll<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsSortMode.None);

    static int CheckSingleton<T>(string label) where T : Object
    {
        T[] found = FindAll<T>();
        if (found.Length == 0)
        {
            Debug.LogWarning($"[Olorama] Validate Scene: no {label} in the scene — nothing will fire until one is added.");
            return 1;
        }
        if (found.Length > 1)
        {
            string names = string.Join(", ", found.Select(f => f.name));
            Debug.LogWarning($"[Olorama] Validate Scene: {found.Length} {label} instances found ({names}) — only one should exist in the scene.");
            return 1;
        }
        return 0;
    }

    static int CheckScentMakerPortCollisions()
    {
        ScentMaker[] scents = AssetDatabase.FindAssets("t:ScentMaker")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ScentMaker>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(s => s != null)
            .ToArray();

        int issues = 0;
        foreach (IGrouping<int, ScentMaker> group in scents.GroupBy(s => s.port))
        {
            if (group.Count() > 1)
            {
                string names = string.Join(", ", group.Select(s => s.name));
                Debug.LogWarning($"[Olorama] Validate Scene: port {group.Key} is used by {group.Count()} ScentMaker assets ({names}) — only one cartridge is physically in that port.");
                issues++;
            }
        }
        return issues;
    }

    static ScentMaker GetSerializedScent(Object target, string fieldName)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        return prop?.objectReferenceValue as ScentMaker;
    }

    static bool HasFallbackSelection => FindAll<ScentSelection>().Length > 0;

    static int CheckTriggers()
    {
        int issues = 0;
        foreach (ScentTrigger trigger in FindAll<ScentTrigger>())
        {
            bool hasSource = trigger.GetComponent<ScentSource>() != null;
            bool hasFixedScent = GetSerializedScent(trigger, "scent") != null;

            if (!hasSource && !hasFixedScent && !HasFallbackSelection)
            {
                Debug.LogWarning($"[Olorama] Validate Scene: ScentTrigger on \"{trigger.name}\" has no ScentSource, no Scent, and there's no ScentSelection in the scene to fall back to — it will warn and do nothing when triggered.");
                issues++;
            }

            Collider col = trigger.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[Olorama] Validate Scene: ScentTrigger on \"{trigger.name}\" has no Collider — OnTriggerEnter will never fire.");
                issues++;
            }
            else if (!col.isTrigger)
            {
                Debug.LogWarning($"[Olorama] Validate Scene: ScentTrigger on \"{trigger.name}\"'s Collider doesn't have Is Trigger checked — OnTriggerEnter will never fire.");
                issues++;
            }
        }
        return issues;
    }

    static int CheckGuns()
    {
        int issues = 0;
        foreach (ScentGun gun in FindAll<ScentGun>())
        {
            bool hasFixedScent = GetSerializedScent(gun, "fixedScent") != null;
            if (!hasFixedScent && !HasFallbackSelection)
                Debug.Log($"[Olorama] Validate Scene: ScentGun on \"{gun.name}\" has no Fixed Scent and there's no ScentSelection in the scene — it only works on targets that carry their own ScentSource. That's fine if every target does.");
        }
        return issues;
    }

    static int CheckInteractables()
    {
        int issues = 0;
        foreach (ScentInteractable interactable in FindAll<ScentInteractable>())
        {
            bool hasSource = interactable.GetComponent<ScentSource>() != null;
            bool hasFixedScent = GetSerializedScent(interactable, "scent") != null;

            if (!hasSource && !hasFixedScent && !HasFallbackSelection)
            {
                Debug.LogWarning($"[Olorama] Validate Scene: ScentInteractable on \"{interactable.name}\" has no ScentSource, no Scent, and there's no ScentSelection to fall back to — selecting it will warn and do nothing.");
                issues++;
            }

            Collider col = interactable.GetComponent<Collider>();
            if (col != null && col.isTrigger)
                Debug.Log($"[Olorama] Validate Scene: \"{interactable.name}\"'s Collider is a trigger — a controller Ray Interactor needs its Raycast Trigger Interaction set to Collide to select it (Poke/Direct interactors are unaffected).");
        }
        return issues;
    }

    static int CheckMenus()
    {
        int issues = 0;
        foreach (ScentMenu menu in FindAll<ScentMenu>())
        {
            SerializedObject so = new SerializedObject(menu);
            SerializedProperty scentsProp = so.FindProperty("scents");
            if (scentsProp == null || scentsProp.arraySize == 0)
            {
                Debug.LogWarning($"[Olorama] Validate Scene: ScentMenu on \"{menu.name}\" has an empty Scents list.");
                issues++;
            }
        }
        return issues;
    }
}
