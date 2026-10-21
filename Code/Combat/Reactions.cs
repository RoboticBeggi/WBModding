using TeyvatChronicle.Cinematics;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// Genshin-style elemental reactions. A hit leaves an element aura on the target for a few seconds;
    /// a different element landing on that aura triggers a reaction and consumes it.
    /// </summary>
    public static class Reactions
    {
        private const float AuraSeconds = 4f;

        public static float Apply(Actor pCaster, Actor pVictim, string pElement, float pDamage)
        {
            if (!Settings.Reactions || !Elements.IsElement(pElement)) return pDamage;
            CombatData data = CombatCore.Data(pVictim);
            string aura = data.AuraUntil > CombatCore.Now ? data.Aura : null;

            if (aura == null || aura == pElement)
            {
                data.Aura = pElement;
                data.AuraUntil = CombatCore.Now + AuraSeconds;
                return pDamage;
            }

            data.Aura = null;
            Vector3 pos = pVictim.current_position;
            switch (Key(aura, pElement))
            {
                case "hydro+pyro":
                    return Label("Vaporize", pos, "#FFD27F", pDamage * 1.8f);
                case "cryo+pyro":
                    return Label("Melt", pos, "#FFB27F", pDamage * 1.8f);
                case "electro+pyro":
                    Fx.Play("burst", pos, 3f, Elements.Color("pyro"));
                    CombatCore.Sound("explode", pos);
                    foreach (Actor other in CombatCore.EnemiesNear(pCaster, pos, 1.6f))
                    {
                        if (other != pVictim) other.getHit(pDamage * 0.6f, true, AttackType.Other, pCaster, false);
                    }
                    return Label("Overloaded", pos, "#FF8A9A", pDamage * 1.3f);
                case "cryo+electro":
                    pVictim.addStatusEffect("slowness", 4f);
                    Fx.Play("nova", pos, 2.4f, Elements.Color("cryo"));
                    return Label("Superconduct", pos, "#C7B8FF", pDamage * 1.3f);
                case "electro+hydro":
                    Fx.Play("spark", pos, 2f, Elements.Color("electro"));
                    return Label("Electro-Charged", pos, "#D49CFF", pDamage * 1.4f);
                case "cryo+hydro":
                    pVictim.addStatusEffect("frozen", 2.5f);
                    return Label("Frozen", pos, "#BFEFFF", pDamage);
                case "dendro+pyro":
                    pVictim.addStatusEffect("burning", 4f);
                    return Label("Burning", pos, "#FF9A3A", pDamage * 1.2f);
                case "dendro+hydro":
                    ModClass.Instance.StartCoroutine(Bloom(pCaster, pos, pDamage));
                    return Label("Bloom", pos, "#9BFF8A", pDamage);
                case "dendro+electro":
                    return Label("Aggravate", pos, "#C8FF6A", pDamage * 1.45f);
            }

            if (pElement == "anemo" || aura == "anemo")
            {
                string other = pElement == "anemo" ? aura : pElement;
                if (other == "geo") return Label("Crystallize", pos, "#F2D27A", Crystallize(pCaster, pDamage));
                Fx.Play("tornado", pos, 2.4f, Elements.Color(other));
                foreach (Actor near in CombatCore.EnemiesNear(pCaster, pos, 2.2f))
                {
                    CombatData d = CombatCore.Data(near);
                    d.Aura = other;
                    d.AuraUntil = CombatCore.Now + AuraSeconds;
                    if (near != pVictim) near.getHit(pDamage * 0.4f, true, AttackType.Other, pCaster, false);
                }
                return Label("Swirl", pos, "#74E5C5", pDamage * 1.2f);
            }
            if (pElement == "geo" || aura == "geo")
            {
                return Label("Crystallize", pos, "#F2D27A", Crystallize(pCaster, pDamage));
            }
            return pDamage;
        }

        private static float Crystallize(Actor pCaster, float pDamage)
        {
            pCaster.addStatusEffect("shield", 5f);
            Fx.Play("shield", pCaster.current_position, 2.2f, Elements.Color("geo"));
            return pDamage * 1.1f;
        }

        private static System.Collections.IEnumerator Bloom(Actor pCaster, Vector3 pPos, float pDamage)
        {
            Fx.Play("bloom", pPos, 1.6f, Elements.Color("dendro"));
            yield return CombatCore.Wait(0.6f);
            Fx.Play("burst", pPos, 2.4f, Elements.Color("dendro"));
            CombatCore.Sound("nature", pPos);
            if (CombatCore.Alive(pCaster))
            {
                foreach (Actor other in CombatCore.EnemiesNear(pCaster, pPos, 1.6f))
                {
                    other.getHit(pDamage * 0.8f, true, AttackType.Other, pCaster, false);
                }
            }
        }

        private static float Label(string pName, Vector3 pPos, string pHex, float pDamage)
        {
            FloatingText.Show(pName, pPos, Toolbox.makeColor(pHex), 13);
            return pDamage;
        }

        private static string Key(string pA, string pB) =>
            string.CompareOrdinal(pA, pB) < 0 ? pA + "+" + pB : pB + "+" + pA;
    }
}
