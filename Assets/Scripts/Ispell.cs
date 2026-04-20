
public interface ISpell
{
    string SpellName { get; }   // название
    float ManaCost { get; }   // стоимость маны
    float Cooldown { get; }   // кулдаун в секундах

    void Cast(UnityEngine.Transform caster);
}