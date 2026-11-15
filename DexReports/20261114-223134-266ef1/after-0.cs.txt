namespace Dex
{
    // Shared cast/immunity rules. Only these expressions are editable by the workbench.
    public static class CombatRules
    {
        public static bool CanMove(bool stunned, bool rooted, bool charmed, bool grounded)
        {
            return !stunned;
        }
        public static bool BlocksControl(bool magicShield, bool physicalDamage)
        {
            return magicShield;
        }
        public static bool DestroysProjectile(bool novaActive, bool enemyProjectile, bool insideNova)
        {
            return novaActive && enemyProjectile && insideNova;
        }
    }
}
