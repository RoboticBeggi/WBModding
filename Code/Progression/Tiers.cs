using UnityEngine;

namespace TeyvatChronicle.Progression
{
    /// <summary>Names, colours and short descriptions for the seven power tiers.</summary>
    public static class Tiers
    {
        public static string Name(int pTier) => pTier switch
        {
            1 => "Vision Bearer",
            2 => "Exalted",
            3 => "Archon",
            4 => "Dragon Sovereign",
            5 => "Shade",
            6 => "Lunar Power",
            7 => "Heavenly Principles",
            _ => "Mortal"
        };

        public static Color Color(int pTier) => pTier switch
        {
            1 => new Color32(221, 230, 240, 255),
            2 => new Color32(111, 183, 255, 255),
            3 => new Color32(255, 216, 107, 255),
            4 => new Color32(255, 138, 58, 255),
            5 => new Color32(224, 74, 90, 255),
            6 => new Color32(201, 184, 255, 255),
            7 => new Color32(255, 229, 138, 255),
            _ => new Color32(150, 150, 160, 255)
        };

        public static string Description(int pTier) => pTier switch
        {
            1 => "Mortals with a Vision, a Delusion, or (as an Allogene) no need for either. They rise by training and battle.",
            2 => "Adepti, Fatui Harbingers, Hexenzirkel witches, Abyss Heralds and Demon Gods. Hallowed Vision holders with two marks may be Exalted.",
            3 => "The Seven, each holding a Gnosis. When an Archon dies, the Gnosis passes on; a Paragon Exalted with four marks may claim an empty throne.",
            4 => "The Dragon Sovereigns of the Old World: colossal dragons, each holding an elemental Authority.",
            5 => "The Four Shades of the Primordial One: Life, Death, Time and Space.",
            6 => "The Moon Goddesses, the Trilunar Authority and Nibelung, the Dragon King.",
            7 => "The Heavenly Principles themselves: the Primordial One, ruler of Celestia.",
            _ => "Ordinary people. Training and danger build potential; at 100 potential, a Vision may answer their wish."
        };

        public static string HowToGet(int pTier) => pTier switch
        {
            1 => "Awaken naturally, use Grant Vision / Grant Delusion, or add the trait",
            2 => "Exaltation, the Chronicle, or add the trait",
            3 => "Succession, Godhood, Crown Archon, or the Chronicle",
            4 => "The Chronicle's Old World, or add a Sovereign trait",
            5 => "The Chronicle (Doves era), or add a Shade trait",
            6 => "The Chronicle (Moons, Nod-Krai), or add the trait",
            7 => "The Chronicle (Doves era), or add the trait",
            _ => "—"
        };
    }
}
