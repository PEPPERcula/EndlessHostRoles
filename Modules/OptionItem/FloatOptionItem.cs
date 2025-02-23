using System.Globalization;

namespace EHR;

public class FloatOptionItem(int id, string name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue = false) : OptionItem(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue)
{
    public readonly FloatValueRule Rule = rule;

    // Getter
    public override int GetInt()
    {
        return (int)Rule.GetValueByIndex(CurrentValue);
    }

    public override float GetFloat()
    {
        return Rule.GetValueByIndex(CurrentValue);
    }

    public override string GetString()
    {
        return ApplyFormat(((float)((int)(Rule.GetValueByIndex(CurrentValue) * 100) * 1.0) / 100).ToString(CultureInfo.CurrentCulture));
    }

    public override int GetValue()
    {
        return Rule.RepeatIndex(base.GetValue());
    }

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(value), doSync);
    }
}