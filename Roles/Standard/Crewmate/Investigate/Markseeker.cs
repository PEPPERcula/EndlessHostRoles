using System.Collections.Generic;

namespace EHR.Roles;

internal class Markseeker : RoleBase
{
    private const int Id = 643550;
    public static List<byte> PlayerIdList = [];
    public static bool On;
    public static OptionItem CancelVote;

    public byte MarkedId;
    public bool TargetRevealed;
    public override bool IsEnable => On;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Markseeker);
        CancelVote = Options.CreateVoteCancellingUseSetting(Id + 2, CustomRoles.Markseeker, TabGroup.CrewmateRoles);
    }

    public override void Add(byte playerId)
    {
        On = true;
        MarkedId = byte.MaxValue;
        TargetRevealed = false;
        PlayerIdList.Add(playerId);
    }

    public override void Remove(byte playerId)
    {
        PlayerIdList.Remove(playerId);
    }

    public override void Init()
    {
        On = false;
        PlayerIdList = [];
    }

    public override bool OnVote(PlayerControl player, PlayerControl target)
    {
        if (Starspawn.IsDayBreak) return false;
        if (!player || !target || player.PlayerId == target.PlayerId || MarkedId != byte.MaxValue || Main.DontCancelVoteList.Contains(player.PlayerId)) return false;

        MarkedId = target.PlayerId;

        player.RPCPlayCustomSound("Clothe");
        Main.DontCancelVoteList.Add(player.PlayerId);
        return true;
    }

    public override void OnMeetingShapeshift(PlayerControl shapeshifter, PlayerControl target)
    {
        OnVote(shapeshifter, target);
    }

    public static void OnDeath(PlayerControl player)
    {
        if (Main.PlayerStates[player.PlayerId].Role is not Markseeker { IsEnable: true } markseeker || markseeker.MarkedId == byte.MaxValue) return;

        markseeker.TargetRevealed = true;

        Utils.SendRPC(CustomRPC.SyncRoleData, player.PlayerId, markseeker.MarkedId);
    }

    public void ReceiveRPC(MessageReader reader)
    {
        MarkedId = reader.ReadByte();
        TargetRevealed = true;
    }
}
