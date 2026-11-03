using System;
using HarmonyLib;
using PlayerNotebooks;
using SecretHistories.Abstract;
using SecretHistories.Entities;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;

internal static class NotebookRebinding
{
    internal static bool CanRebind(ITokenPayload payload)
    {
        if (!(payload is ElementStack card) || card.Defunct || card.Quantity != 1 ||
            !NotebookBinding.TryParseEntity(card.EntityId, out _, out _)) return false;
        try { NotebookCodec.Read(card.GetIllumination(NotebookCodec.IlluminationKey), card.Id); return true; }
        catch { return false; }
    }
}

// Accept notebooks only at the binding desk and pigment workstation. Pigment admission
// is native; state, capacity and movement checks still run for both workstations.
[HarmonyPatch(typeof(Sphere), nameof(Sphere.GetMatchForTokenPayload))]
internal static class NotebookBindingSlotPatch
{
    static void Postfix(Sphere __instance, ITokenPayload payload, ref ContainerMatchForStack __result)
    {
        if (!(__instance.GetContainer() is Situation station)) return;
        var bindingSlot = station.VerbId?.StartsWith("library.desk.", StringComparison.Ordinal) == true && __instance.Id == "i1";
        var colorSlot = station.VerbId == "library.prospect.loggia" && __instance.Id == "i1";
        if ((bindingSlot || colorSlot) && NotebookRebinding.CanRebind(payload))
            __result = ContainerMatchForStack.MatchOK();
    }
}

// Native transformation keeps the stack ID and illuminations. Update binding metadata
// before its Fundamental change event refreshes the card and physical book artwork.
[HarmonyPatch(typeof(ElementStack), nameof(ElementStack.ChangeTo))]
internal static class NotebookBindingChangePatch
{
    static bool Prefix(ElementStack __instance, string newElementId, out string __state)
    {
        __state = null;
        if (!PlayerNotebook.IsNotebook(__instance) ||
            !NotebookBinding.TryParseEntity(newElementId, out var color, out var design)) return true;
        try
        {
            var raw = __instance.GetIllumination(NotebookCodec.IlluminationKey);
            var document = NotebookCodec.Read(raw, __instance.Id);
            document.Rebind(color, design);
            __state = raw;
            __instance.SetIllumination(NotebookCodec.IlluminationKey, NotebookCodec.Write(document));
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Player Notebook: rebinding refused; original data preserved: " + ex.Message);
            return false;
        }
    }

    static Exception Finalizer(ElementStack __instance, string __state, Exception __exception)
    {
        if (__exception != null && __state != null)
            __instance.SetIllumination(NotebookCodec.IlluminationKey, __state);
        return __exception;
    }
}
