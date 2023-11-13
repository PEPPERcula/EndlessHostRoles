﻿using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.AddOns.Impostor
{
    public static class Damocles
    {
        private static readonly int Id = 14670;

        private static OptionItem DamoclesExtraTimeAfterKill;
        private static OptionItem DamoclesExtraTimeAfterMeeting;
        private static OptionItem DamoclesStartingTime;

        private static int TimeAfterKill;
        private static int TimeAfterMeeting;
        private static int StartingTime;

        public static int Timer;

        public static long lastUpdate;
        public static List<int> PreviouslyEnteredVents;

        public static bool countRepairSabotage;

        public static void SetupCustomOption()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Damocles, canSetNum: false);
            DamoclesExtraTimeAfterKill = IntegerOptionItem.Create(Id + 10, "DamoclesExtraTimeAfterKill", new(0, 60, 1), 30, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Damocles])
                .SetValueFormat(OptionFormat.Seconds);
            DamoclesExtraTimeAfterMeeting = IntegerOptionItem.Create(Id + 11, "DamoclesExtraTimeAfterMeeting", new(0, 60, 1), 30, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Damocles])
                .SetValueFormat(OptionFormat.Seconds);
            DamoclesStartingTime = IntegerOptionItem.Create(Id + 12, "DamoclesStartingTime", new(0, 60, 1), 30, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Damocles])
                .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Initialize()
        {
            TimeAfterKill = DamoclesExtraTimeAfterKill.GetInt();
            TimeAfterMeeting = DamoclesExtraTimeAfterMeeting.GetInt();
            StartingTime = DamoclesStartingTime.GetInt();

            Timer = StartingTime;
            lastUpdate = GetTimeStamp() + 10;
            PreviouslyEnteredVents = new();
            countRepairSabotage = true;
        }

        public static void Update(PlayerControl pc)
        {
            if (lastUpdate >= GetTimeStamp() || !GameStates.IsInTask || !pc.IsAlive()) return;
            lastUpdate = GetTimeStamp();
            Logger.Warn($"Timer for {pc.GetNameWithRole()}: {Timer}", "Damocles");

            Timer--;

            if (Timer < 0)
            {
                Timer = 0;
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                pc.SetRealKiller(pc);
                pc.Kill(pc);
                Main.PlayerStates[pc.PlayerId].SetDead();
            }

            if (pc.IsModClient() && pc.PlayerId != 0) SendRPC();
            NotifyRoles(SpecifySeer: pc);
        }

        public static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncDamoclesTimer, SendOption.Reliable, -1);
            writer.Write(Timer);
            writer.Write(lastUpdate);
            writer.Write(PreviouslyEnteredVents.Count);
            if (PreviouslyEnteredVents.Any()) foreach (var vent in PreviouslyEnteredVents.ToArray()) writer.Write(vent);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            Timer = reader.ReadInt32();
            lastUpdate = long.Parse(reader.ReadString());
            var elements = reader.ReadInt32();
            if (elements > 0) for (int i = 0; i < elements; i++) PreviouslyEnteredVents.Add(reader.ReadInt32());
        }

        public static void OnMurder()
        {
            Timer += TimeAfterKill;
            Logger.Warn("murder", "debug");
        }

        public static void OnOtherImpostorMurder()
        {
            Timer += 10;
            Logger.Warn("other impostor killed", "debug");
        }

        public static void OnEnterVent(int ventId)
        {
            if (PreviouslyEnteredVents.Contains(ventId)) return;

            PreviouslyEnteredVents.Add(ventId);
            Timer += 10;
            Logger.Warn("enter vent", "debug");
        }

        public static void AfterMeetingTasks()
        {
            PreviouslyEnteredVents.Clear();

            Timer += TimeAfterMeeting;
            Timer += 7;
            countRepairSabotage = true;
            Logger.Warn("after meeting", "debug");
        }

        public static void OnCrewmateEjected()
        {
            Timer = (int)Math.Round(Timer * 1.3);
            Logger.Warn("crewmate ejected", "debug");
        }

        public static void OnRepairSabotage()
        {
            Timer -= 15;
            Logger.Warn("repair sabo", "debug");
        }

        public static void OnImpostorDeath()
        {
            Timer -= 20;
            Logger.Warn("an impostor died", "debug");
        }

        public static void OnReport()
        {
            Timer = (int)Math.Round(Timer * 0.9);
            Logger.Warn("called meeting", "debug");
        }

        public static void OnImpostorEjected()
        {
            Timer = (int)Math.Round(Timer * 0.8);
            Logger.Warn("Impostor ejected", "debug");
        }

        public static string GetProgressText() => string.Format(GetString("DamoclesTimeLeft"), Timer);
    }
}