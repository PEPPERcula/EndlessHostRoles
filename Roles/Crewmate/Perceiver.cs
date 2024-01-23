﻿using System.Linq;

namespace TOHE.Roles.Crewmate
{
    internal class Perceiver
    {
        private static int Id => 643360;
        private static OptionItem Radius;
        public static OptionItem CD;
        public static OptionItem Limit;
        public static OptionItem PerceiverAbilityUseGainWithEachTaskCompleted;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Perceiver);
            Radius = FloatOptionItem.Create(Id + 2, "PerceiverRadius", new(0.05f, 5f, 0.05f), 0.5f, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Perceiver])
                .SetValueFormat(OptionFormat.Multiplier);
            CD = Options.CreateCDSetting(Id + 3, TabGroup.CrewmateRoles, CustomRoles.Perceiver);
            Limit = IntegerOptionItem.Create(Id + 4, "AbilityUseLimit", new(0, 20, 1), 0, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Perceiver])
                .SetValueFormat(OptionFormat.Times);
            PerceiverAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 5, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 0.4f, TabGroup.CrewmateRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Perceiver])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void UseAbility(PlayerControl pc)
        {
            if (pc == null) return;
            var killers = Main.AllAlivePlayerControls.Where(x => !x.Is(Team.Crewmate) && x.HasKillButton() && UnityEngine.Vector2.Distance(x.Pos(), pc.Pos()) <= Radius.GetFloat()).ToArray();
            pc.Notify(string.Format(Translator.GetString("PerceiverNotify"), killers.Length));
        }
    }
}
