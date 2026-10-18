namespace EpicLoot.Compendium;

public class MagicEffectTextInfo(string topic) : MagicTextInfo(topic)
{
    public override void Build(MagicPages instance)
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        VitalsTextSection.Build(instance);
        OffenseTextSection.Build(instance);
        DefenseTextSection.Build(instance);
        ArmorTextSection.Build(instance);
        SkillsTextSection.Build(instance);
        EffectGroupsSection.Build(instance);
    }
}
