using System;
using System.Linq;
using HarmonyLib;
using PlayerNotebooks;
using SecretHistories.Abstract;
using SecretHistories.Entities;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;

internal static class NotebookRebinding
{
    internal static bool IsPaint(string id) => id == "paint.blue" || id == "paint.green" || id == "paint.red";

    internal static bool CanRebind(ITokenPayload payload)
    {
        if (!(payload is ElementStack card) || card.Defunct || card.Quantity != 1 ||
            !NotebookBinding.TryParseEntity(card.EntityId, out _, out _)) return false;
        try { NotebookCodec.Read(card.GetIllumination(NotebookCodec.IlluminationKey), card.Id); return true; }
        catch { return false; }
    }
}

// Broaden only our notebooks' desk inputs. Native state, capacity and movement checks
// still run; ordinary paper/ink/tool inputs and every other workstation are untouched.
[HarmonyPatch(typeof(Sphere), nameof(Sphere.GetMatchForTokenPayload))]
internal static class NotebookDeskSlotPatch
{
    static void Postfix(Sphere __instance, ITokenPayload payload, ref ContainerMatchForStack __result)
    {
        if (!(__instance.GetContainer() is Situation desk) ||
            !desk.VerbId.StartsWith("library.desk.", StringComparison.Ordinal)) return;
        if (__instance.Id == "i1" && NotebookRebinding.CanRebind(payload))
            __result = ContainerMatchForStack.MatchOK();
        if (__instance.Id == "i2" && payload != null && NotebookRebinding.IsPaint(payload.EntityId) &&
            desk.GetInteriorSpheres().Where(s => s.Id == "i1").SelectMany(s => s.GetTokens())
                .Any(t => NotebookRebinding.CanRebind(t.Payload)))
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
