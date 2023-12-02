using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Vulture
{
    private static readonly int Id = 11600;
    private static List<byte> playerIdList = [];

    public static List<byte> UnreportablePlayers = [];
    public static Dictionary<byte, int> BodyReportCount = [];
    public static Dictionary<byte, int> AbilityLeftInRound = [];
    public static Dictionary<byte, long> LastReport = [];

    public static OptionItem ArrowsPointingToDeadBody;
    public static OptionItem NumberOfReportsToWin;
    public static OptionItem CanVent;
    public static OptionItem VultureReportCD;
    public static OptionItem MaxEaten;
    public static OptionItem HasImpVision;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vulture);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "VultureArrowsPointingToDeadBody", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        NumberOfReportsToWin = IntegerOptionItem.Create(Id + 11, "VultureNumberOfReportsToWin", new(1, 10, 1), 4, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, true).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        VultureReportCD = FloatOptionItem.Create(Id + 13, "VultureReportCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture])
                .SetValueFormat(OptionFormat.Seconds);
        MaxEaten = IntegerOptionItem.Create(Id + 14, "VultureMaxEatenInOneRound", new(1, 10, 1), 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
        HasImpVision = BooleanOptionItem.Create(Id + 15, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vulture]);
    }
    public static void Init()
    {
        playerIdList = [];
        UnreportablePlayers = [];
        BodyReportCount = [];
        AbilityLeftInRound = [];
        LastReport = [];

    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BodyReportCount[playerId] = 0;
        AbilityLeftInRound[playerId] = MaxEaten.GetInt();
        LastReport[playerId] = Utils.GetTimeStamp();
        _ = new LateTask(() =>
        {
            if (GameStates.IsInTask)
            {
                //Utils.GetPlayerById(playerId).RpcGuardAndKill(Utils.GetPlayerById(playerId));
                Utils.GetPlayerById(playerId).Notify(GetString("VultureCooldownUp"));
            }
            return;
        }, VultureReportCD.GetFloat() + 8f, "Vulture CD");  //for some reason that idk vulture cd completes 8s faster when the game starts, so I added 8f for now 
    }
    public static bool IsEnable => playerIdList.Count > 0;

    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpVision.GetBool());

    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        if (!IsEnable || !Utils.DoRPC) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVultureArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
    }

    public static void Clear()
    {
        foreach (byte apc in playerIdList.ToArray())
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }
    }
    public static void AfterMeetingTasks()
    {
        foreach (byte apc in playerIdList.ToArray())
        {
            var player = Utils.GetPlayerById(apc);
            if (player.IsAlive())
            {
                AbilityLeftInRound[apc] = MaxEaten.GetInt();
                LastReport[apc] = Utils.GetTimeStamp();
                _ = new LateTask(() =>
                {
                    if (GameStates.IsInTask)
                    {
                        //Utils.GetPlayerById(apc).RpcGuardAndKill(Utils.GetPlayerById(apc));
                        Utils.GetPlayerById(apc).Notify(GetString("VultureCooldownUp"));
                    }
                    return;
                }, VultureReportCD.GetFloat(), "Vulture CD");
                SendRPC(apc, false);
            }
        }
    }

    public static void OnPlayerDead(PlayerControl target)
    {
        if (!ArrowsPointingToDeadBody.GetBool()) return;

        var pos = target.Pos();
        float minDis = float.MaxValue;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Vector2.Distance(pc.Pos(), pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
            }
        }

        foreach (byte pc in playerIdList.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
            SendRPC(pc, true, target.transform.position);
        }
    }

    public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        BodyReportCount[pc.PlayerId]++;
        AbilityLeftInRound[pc.PlayerId]--;
        Logger.Msg($"target.object {target.Object}, is null? {target.Object == null}", "VultureNull");
        if (target.Object != null)
        {
            foreach (byte apc in playerIdList.ToArray())
            {
                LocateArrow.Remove(apc, target.Object.transform.position);
                SendRPC(apc, false);
            }
        }

        pc.Notify(GetString("VultureBodyReported"));
        UnreportablePlayers.Remove(target.PlayerId);
        UnreportablePlayers.Add(target.PlayerId);
        //playerIdList.Remove(target.PlayerId);
    }

    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!seer.Is(CustomRoles.Vulture)) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (GameStates.IsMeeting) return string.Empty;
        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
}