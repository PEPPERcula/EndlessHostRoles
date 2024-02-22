﻿using System;

namespace TOHE.Roles.Impostor
{
    internal class Inhibitor : RoleBase
    {
        private byte InhibitorId;
        public static bool On;
        public override bool IsEnable => On;

        public override void Add(byte playerId)
        {
            On = true;
            InhibitorId = playerId;
        }

        public override void Init()
        {
            On = false;
            InhibitorId = byte.MaxValue;
        }

        public override bool CanUseKillButton(PlayerControl pc)
        {
            return base.CanUseKillButton(pc) && !Utils.IsActive(SystemTypes.Electrical) && !Utils.IsActive(SystemTypes.Comms) && !Utils.IsActive(SystemTypes.MushroomMixupSabotage) && !Utils.IsActive(SystemTypes.Laboratory) && !Utils.IsActive(SystemTypes.LifeSupp) && !Utils.IsActive(SystemTypes.Reactor) && !Utils.IsActive(SystemTypes.HeliSabotage);
        }

        public override void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = Options.InhibitorCDAfterMeetings.GetFloat();
        }

        public override bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (Math.Abs(Main.AllPlayerKillCooldown[killer.PlayerId] - Options.InhibitorCD.GetFloat()) > 0.5f)
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.InhibitorCD.GetFloat();
                killer.SyncSettings();
            }

            return base.OnCheckMurder(killer, target);
        }

        public override void OnReportDeadBody()
        {
            Main.AllPlayerKillCooldown[InhibitorId] = Options.InhibitorCDAfterMeetings.GetFloat();
        }
    }
}