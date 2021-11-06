using System;

namespace Item
{
  [Serializable]
  public class Effect
  {
    public EffectType EffectType;
    public float StatModifierValue = float.NaN;
    public SpecialEffect? SpecialEffect;
  }

  public enum EffectType
  {
    StatIncrease,
    StatDecrease,
    SpecialEffect,
  }

  public enum SpecialEffect
  {
    Poison,
    Explosion,
    Fire,
  }
}