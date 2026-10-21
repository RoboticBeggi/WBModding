namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Every fighting style, written as code. Each group lives in its own file; every technique has its
    /// own script, effects and sounds, and no two styles share a pattern.
    /// </summary>
    public static partial class Styles
    {
        public static void Register()
        {
            RegisterVisions();
            RegisterOutsiders();
            RegisterExalted();
            RegisterArchons();
            RegisterSovereigns();
            RegisterPrimordials();
        }

        static partial void RegisterVisions();
        static partial void RegisterOutsiders();
        static partial void RegisterExalted();
        static partial void RegisterArchons();
        static partial void RegisterSovereigns();
        static partial void RegisterPrimordials();
    }
}
