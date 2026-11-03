using System;
using HarmonyLib;
using PlayerNotebooks;
using SecretHistories.Abstract;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Infrastructure;
using SecretHistories.Services;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// FEATHER resolves the global class from the synopsis name with spaces removed.
public static class PlayerNotebook
{
    public static void Initialise()
    {
        if (NotebookWindow.Instance != null) return;
#if NOTEBOOK_TESTING
        NotebookTestEnvironment.Prepare();
#endif
        new Harmony("playernotebook.prototype").PatchAll(typeof(PlayerNotebook).Assembly);
        var host = new GameObject("PlayerNotebook");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.AddComponent<NotebookWindow>();
#if NOTEBOOK_TESTING
        host.AddComponent<NotebookTestControls>();
#endif
        Debug.Log("Player Notebook 0.1.0 prototype initialised");
    }

    internal static bool IsNotebook(ITokenPayload payload) => payload != null &&
        payload.EntityId != null && payload.EntityId.StartsWith("playernotebook.", StringComparison.Ordinal);
}

[HarmonyPatch(typeof(ElementStackCreationCommand), nameof(ElementStackCreationCommand.Execute))]
internal static class NotebookCreationPatch
{
    static void Postfix(ITokenPayload __result)
    {
        if (!PlayerNotebook.IsNotebook(__result) || !(__result is ElementStack card)) return;
        try
        {
            var saved = card.GetIllumination(NotebookCodec.IlluminationKey);
            var document = NotebookCodec.Read(saved, card.Id);
            if (string.IsNullOrEmpty(saved))
            {
                var parts = card.EntityId.Split('.');
                document.Color = parts[1];
                document.Design = parts[2];
            }
            card.SetIllumination(NotebookCodec.IlluminationKey, NotebookCodec.Write(document));
        }
        catch (Exception ex) { Debug.LogError("Player Notebook: original card data preserved: " + ex.Message); }
    }
}

[HarmonyPatch(typeof(Recipe), "get_Priority")]
internal static class NotebookRecipePriorityPatch
{
    static void Postfix(Recipe __instance, ref int __result)
    {
        // Reserve the explicit paper + binding skill + no optional inputs combination.
        // Removing paper or adding optional inputs retains vanilla recipe selection.
        if (__instance.Id == "playernotebook.craft.brown.plain" ||
            __instance.Id == "playernotebook.craft.brown.decorated") __result = 2;
        if (__instance.Id?.StartsWith("playernotebook.rebind.", StringComparison.Ordinal) == true)
            __result = __instance.Craftable ? 2 : -1;
    }
}

// Pause simulation without changing the player's speed selection or the game's modal
// pause state. Restrict this to the live editor; closing immediately restores ticking.
[HarmonyPatch(typeof(Heart), nameof(Heart.FixedUpdate))]
internal static class NotebookTimePatch
{
    static bool Prefix() => NotebookWindow.Instance == null || !NotebookWindow.Instance.IsOpen;
}

// Include the complete notebook modal in native text-edit detection, including when a
// button rather than a field has focus, so Abort/camera shortcuts remain isolated.
[HarmonyPatch(typeof(UIController), nameof(UIController.IsEditingText))]
internal static class NotebookTextInputPatch
{
    static void Postfix(ref bool __result)
    {
        if (NotebookWindow.Instance != null && NotebookWindow.Instance.IsOpen) __result = true;
    }
}

// Commit and release our modal before the native scene transition clears old cards and
// camera services. Waiting for a destroyed token in Update is too late for that ordering.
[HarmonyPatch(typeof(StageHand), "CleanupNonPersistentFucineObjects")]
internal static class NotebookSceneTransitionPatch
{
    static void Prefix() => NotebookWindow.Instance?.Close();
}

[HarmonyPatch(typeof(ElementStack), "get_Label")]
internal static class NotebookLabelPatch
{
    static void Postfix(ElementStack __instance, ref string __result)
    {
        if (!PlayerNotebook.IsNotebook(__instance)) return;
        try { __result = NotebookCodec.Read(__instance.GetIllumination(NotebookCodec.IlluminationKey), __instance.Id).DisplayName; }
        catch { __result = "Untitled Notebook"; } // Opening reports the error; do not rewrite unknown data.
    }
}

[HarmonyPatch(typeof(ElementStack), nameof(ElementStack.CanMergeWith))]
internal static class NotebookMergePatch
{
    static bool Prefix(ElementStack __instance, ITokenPayload otherPayload, ref bool __result)
    {
        if (!PlayerNotebook.IsNotebook(__instance) && !PlayerNotebook.IsNotebook(otherPayload)) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(Token), nameof(Token.OnPointerClick))]
internal static class NotebookClickPatch
{
    static bool Prefix(Token __instance, PointerEventData eventData)
    {
        if (!PlayerNotebook.IsNotebook(__instance.Payload) || eventData.dragging ||
            eventData.button != PointerEventData.InputButton.Left || eventData.clickCount < 2) return true;
        NotebookWindow.Instance.Open((ElementStack)__instance.Payload);
        return false;
    }
}
