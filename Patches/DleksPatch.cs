﻿using HarmonyLib;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient._CoStartGameHost_d__30), nameof(AmongUsClient._CoStartGameHost_d__30.MoveNext))]
public static class DleksPatch
{
    public static bool Prefix(AmongUsClient._CoStartGameHost_d__30 __instance, ref bool __result)
    {
        if (__instance.__1__state != 0)
        {
            return true;
        }

        __instance.__1__state = -1;
        if (LobbyBehaviour.Instance)
        {
            LobbyBehaviour.Instance.Despawn();
        }

        if (ShipStatus.Instance)
        {
            __instance.__2__current = null;
            __instance.__1__state = 2;
            __result = true;
            return false;
        }

        // removed dleks check as it's always false
        var num2 = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.MapId, 0, Constants.MapNames.Length - 1);
        __instance.__2__current = __instance.__4__this.ShipLoadingAsyncHandle = __instance.__4__this.ShipPrefabs[num2].InstantiateAsync();
        __instance.__1__state = 1;

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.SetButtons))]
public static class VentSetButtonsPatch
{
    public static bool ShowButtons;

    // Fix arrows buttons in vent on Dleks map and "Index was outside the bounds of the array" errors
    private static bool Prefix( /*Vent __instance,*/ [HarmonyArgument(0)] ref bool enabled)
    {
        if (Main.CurrentMap == MapNames.Dleks && Main.introDestroyed)
        {
            enabled = false;
            if (GameStates.IsMeeting)
                ShowButtons = false;
        }

        return true;
    }

    public static void Postfix(Vent __instance, [HarmonyArgument(0)] bool enabled)
    {
        if (Main.CurrentMap != MapNames.Dleks) return;
        if (enabled || !Main.introDestroyed) return;

        var setActive = ShowButtons || !PlayerControl.LocalPlayer.inVent && !GameStates.IsMeeting;
        switch (__instance.Id)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 5:
            case 6:
                __instance.Buttons[0].gameObject.SetActive(setActive);
                __instance.Buttons[1].gameObject.SetActive(setActive);
                break;
            case 7:
            case 12:
            case 13:
                __instance.Buttons[0].gameObject.SetActive(setActive);
                break;
            case 4:
            case 8:
            case 9:
            case 10:
            case 11:
                __instance.Buttons[1].gameObject.SetActive(setActive);
                break;
        }
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.TryMoveToVent))]
class VentTryMoveToVentPatch
{
    // Update arrows buttons when player move to vents
    private static void Postfix(Vent __instance, [HarmonyArgument(0)] Vent otherVent)
    {
        if (__instance == null || otherVent == null || Main.CurrentMap != MapNames.Dleks) return;

        VentSetButtonsPatch.ShowButtons = true;
        VentSetButtonsPatch.Postfix(otherVent, false);
        VentSetButtonsPatch.ShowButtons = false;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.UpdateArrows))]
class VentUpdateArrowsPatch
{
    // Fixes "Index was outside the bounds of the array" errors when arrows updates in vent on Dleks map
    private static bool Prefix()
    {
        return Main.CurrentMap != MapNames.Dleks;
    }
}

[HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.OnEnable))]
public static class AutoselectDleksPatch
{
    public static void Postfix(KeyValueOption __instance)
    {
        if (__instance.Title == StringNames.GameMapName)
        {
            // vanilla clamps this to not autoselect dleks
            __instance.Selected = GameOptionsManager.Instance.CurrentGameOptions.MapId;
        }
    }
}