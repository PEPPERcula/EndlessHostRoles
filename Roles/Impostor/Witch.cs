using Hazel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Witch
{
    public enum SwitchTrigger
    {
        Kill,
        Vent,
        DoubleTrigger,
    };
    public static readonly string[] SwitchTriggerText =
    [
        "TriggerKill", "TriggerVent","TriggerDouble"
    ];

    private static readonly int Id = 2000;
    public static List<byte> playerIdList = [];

    public static Dictionary<byte, bool> SpellMode = [];
    public static Dictionary<byte, List<byte>> SpelledPlayer = [];

    public static OptionItem ModeSwitchAction;
    public static SwitchTrigger NowSwitchTrigger;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Witch);
        ModeSwitchAction = StringOptionItem.Create(Id + 10, "WitchModeSwitchAction", SwitchTriggerText, 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Witch]);
    }
    public static void Init()
    {
        playerIdList = [];
        SpellMode = [];
        SpelledPlayer = [];
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SpellMode.Add(playerId, false);
        SpelledPlayer.Add(playerId, []);
        NowSwitchTrigger = (SwitchTrigger)ModeSwitchAction.GetValue();
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

    }
    public static bool IsEnable => playerIdList.Any();
    private static void SendRPC(bool doSpell, byte witchId, byte target = 255)
    {
        if (doSpell)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
            writer.Write(witchId);
            writer.Write(target);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
            writer.Write(witchId);
            writer.Write(SpellMode[witchId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

        }
    }

    public static void ReceiveRPC(MessageReader reader, bool doSpell)
    {
        if (doSpell)
        {
            var witch = reader.ReadByte();
            var spelledId = reader.ReadByte();
            if (spelledId != 255)
            {
                SpelledPlayer[witch].Add(spelledId);
            }
            else
            {
                SpelledPlayer[witch].Clear();
            }
        }
        else
        {
            byte playerId = reader.ReadByte();
            SpellMode[playerId] = reader.ReadBoolean();
        }
    }
    public static bool IsSpellMode(byte playerId)
    {
        return SpellMode.ContainsKey(playerId) && SpellMode[playerId];
    }
    public static void SwitchSpellMode(byte playerId, bool kill)
    {
        bool needSwitch = false;
        switch (NowSwitchTrigger)
        {
            case SwitchTrigger.Kill:
                needSwitch = kill;
                break;
            case SwitchTrigger.Vent:
                needSwitch = !kill;
                break;
        }
        if (needSwitch)
        {
            SpellMode[playerId] = !SpellMode[playerId];
            SendRPC(false, playerId);
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
        }
    }
    public static bool HaveSpelledPlayer()
    {
        foreach (byte witch in playerIdList.ToArray())
        {
            if (SpelledPlayer[witch].Any())
            {
                return true;
            }
        }
        return false;

    }
    public static bool IsSpelled(byte target)
    {
        foreach (byte witch in playerIdList.ToArray())
        {
            if (SpelledPlayer[witch].Contains(target))
            {
                return true;
            }
        }
        return false;
    }
    public static void SetSpelled(PlayerControl killer, PlayerControl target)
    {
        if (!IsSpelled(target.PlayerId))
        {
            SpelledPlayer[killer.PlayerId].Add(target.PlayerId);
            SendRPC(true, killer.PlayerId, target.PlayerId);
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Curse");
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        }
    }
    public static void RemoveSpelledPlayer()
    {
        foreach (byte witch in playerIdList.ToArray())
        {
            SpelledPlayer[witch].Clear();
            SendRPC(true, witch);
        }
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        if (NowSwitchTrigger == SwitchTrigger.DoubleTrigger)
        {
            return killer.CheckDoubleTrigger(target, () => { SetSpelled(killer, target); });
        }
        if (!IsSpellMode(killer.PlayerId))
        {
            SwitchSpellMode(killer.PlayerId, true);
            //キルモードなら通常処理に戻る
            return true;
        }
        SetSpelled(killer, target);

        //スペルに失敗してもスイッチ判定
        SwitchSpellMode(killer.PlayerId, true);
        //キル処理終了させる
        return false;
    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!IsEnable || deathReason != PlayerState.DeathReason.Vote) return;
        foreach (byte id in exileIds)
        {
            if (SpelledPlayer.ContainsKey(id))
                SpelledPlayer[id].Clear();
        }
        var spelledIdList = new List<byte>();
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            var dic = SpelledPlayer.Where(x => x.Value.Contains(pc.PlayerId));
            if (!dic.Any()) continue;
            var whichId = dic.FirstOrDefault().Key;
            var witch = Utils.GetPlayerById(whichId);
            if (witch != null && witch.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(witch);
                    spelledIdList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Spell, [.. spelledIdList]);
        RemoveSpelledPlayer();
    }
    public static string GetSpelledMark(byte target, bool isMeeting)
    {
        if (isMeeting && IsEnable && IsSpelled(target))
        {
            return Utils.ColorString(Palette.ImpostorRed, "†");
        }
        return string.Empty;
    }
    public static string GetSpellModeText(PlayerControl witch, bool hud, bool isMeeting = false)
    {
        if (witch == null || isMeeting) return string.Empty;

        var str = new StringBuilder();
        if (hud)
        {
            str.Append($"<color=#00ffa5>{GetString("WitchCurrentMode")}:</color> <b>");
        }
        else
        {
            str.Append($"{GetString("Mode")}: ");
        }
        if (NowSwitchTrigger == SwitchTrigger.DoubleTrigger)
        {
            str.Append(GetString("WitchModeDouble"));
        }
        else
        {
            str.Append(IsSpellMode(witch.PlayerId) ? GetString("WitchModeSpell") : GetString("WitchModeKill"));
        }
        return str.ToString();
    }
    public static void GetAbilityButtonText(HudManager hud)
    {
        if (IsSpellMode(PlayerControl.LocalPlayer.PlayerId) && NowSwitchTrigger != SwitchTrigger.DoubleTrigger)
        {
            hud.KillButton.OverrideText(GetString("WitchSpellButtonText"));
        }
        else
        {
            hud.KillButton.OverrideText(GetString("KillButtonText"));
        }
    }

    public static void OnEnterVent(PlayerControl pc)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (playerIdList.Contains(pc.PlayerId))
        {
            if (NowSwitchTrigger is SwitchTrigger.Vent)
            {
                SwitchSpellMode(pc.PlayerId, false);
            }
        }
    }
}