﻿using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral
{
    internal class Hookshot : RoleBase
    {
        private static int Id => 643230;

        private PlayerControl Hookshot_ => GetPlayerById(HookshotId);
        private byte HookshotId = byte.MaxValue;

        private static OptionItem KillCooldown;
        private static OptionItem HasImpostorVision;
        public static OptionItem CanVent;

        private bool ToTargetTP;
        public byte MarkedPlayerId = byte.MaxValue;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Hookshot, 1, zeroOne: false);
            KillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 180f, 2.5f), 22.5f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Hookshot])
                .SetValueFormat(OptionFormat.Seconds);
            HasImpostorVision = BooleanOptionItem.Create(Id + 3, "ImpostorVision", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Hookshot]);
            CanVent = BooleanOptionItem.Create(Id + 4, "CanVent", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Hookshot]);
        }

        public override void Init()
        {
            HookshotId = byte.MaxValue;
            MarkedPlayerId = byte.MaxValue;
        }

        public override void Add(byte playerId)
        {
            HookshotId = playerId;

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }

        public override bool IsEnable => HookshotId != byte.MaxValue;
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
        public override bool CanUseSabotage(PlayerControl pc) => true;
        public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

        void SendRPC()
        {
            if (!IsEnable || !DoRPC) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncHookshot, SendOption.Reliable);
            writer.Write(HookshotId);
            writer.Write(ToTargetTP);
            writer.Write(MarkedPlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            var playerId = reader.ReadByte();
            if (Main.PlayerStates[playerId].Role is not Hookshot hs) return;
            hs.ToTargetTP = reader.ReadBoolean();
            hs.MarkedPlayerId = reader.ReadByte();
        }

        public override void OnPet(PlayerControl pc)
        {
            ExecuteAction();
        }

        public override void OnSabotage(PlayerControl pc)
        {
            ExecuteAction();
        }

        void ExecuteAction()
        {
            if (MarkedPlayerId == byte.MaxValue) return;

            var markedPlayer = GetPlayerById(MarkedPlayerId);
            if (markedPlayer == null)
            {
                MarkedPlayerId = byte.MaxValue;
                SendRPC();
                return;
            }

            bool isTPsuccess = ToTargetTP ? Hookshot_.TP(markedPlayer) : markedPlayer.TP(Hookshot_);

            if (isTPsuccess)
            {
                MarkedPlayerId = byte.MaxValue;
                SendRPC();
            }
        }

        public override void OnEnterVent(PlayerControl pc, Vent vent)
        {
            ToTargetTP = !ToTargetTP;
            SendRPC();
            NotifyRoles(SpecifySeer: Hookshot_, SpecifyTarget: Hookshot_);
        }

        public override bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (target == null) return false;

            return Hookshot_.CheckDoubleTrigger(target, () =>
            {
                MarkedPlayerId = target.PlayerId;
                SendRPC();
                Hookshot_.SetKillCooldown(5f);
            });
        }

        public override void OnReportDeadBody()
        {
            MarkedPlayerId = byte.MaxValue;
            SendRPC();
        }

        public static string SuffixText(byte id) => Main.PlayerStates[id].Role is Hookshot hs ? $"<#00ffa5>{Translator.GetString("Mode")}:</color> <#ffffff>{(hs.ToTargetTP ? Translator.GetString("HookshotTpToTarget") : Translator.GetString("HookshotPullTarget"))}</color>" : string.Empty;
    }
}
