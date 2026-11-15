namespace Dex
{
    // Kit identities follow official descriptions; all numbers are deliberate demo tuning.
    public static class ChampionKit
    {
        public static readonly string[] Roster={"Ahri","Morgana","Ezreal","Mel"};
        public static string[] Names(string c)
        {
            switch(c){case "Ahri":return new[]{"Orb of Deception","Fox-Fire","Charm","Spirit Rush"};case "Morgana":return new[]{"Dark Binding","Tormented Shadow","Black Shield","Soul Shackles"};case "Mel":return new[]{"Radiant Volley","Rebuttal","Solar Snare","Golden Eclipse"};default:return new[]{"Mystic Shot","Essence Flux","Arcane Shift","Trueshot Barrage"};}
        }
        public static float Mana(string c,int slot){return c=="Mel"?new[]{60f,60f,60f,100f}[slot]:new[]{55f,60f,70f,100f}[slot];}
        public static float Cooldown(string c,int slot){return c=="Mel"?new[]{8f,24f,10f,60f}[slot]:new[]{7f,9f,14f,80f}[slot];}
        public static string Description(string c,int slot)
        {
            if(c=="Mel")return new[]{"Six luminous missiles burst around your aim point. A champion hit refunds a little cooldown.","An expanding nova destroys hostile missiles and nearby enemy shields. Grants a barrier and speed, then retaliates at missile owners.","An aimed orb roots enemies at its centre and leaves a slowing, damaging solar field. Root duration grows with travel.","Detonate your active Overwhelm marks across the arena. Damage scales with their stacks and consumes them."}[slot];
            if(c=="Ahri")return new[]{"A piercing orb returns for true damage.","Three bolts seek nearby enemies.","A skillshot charms the first target hit.","Three dashes with nearby bolts. New dashes are blocked by crowd control."}[slot];
            if(c=="Morgana")return new[]{"A projectile roots the first enemy hit.","A persistent pool damages enemies inside it.","A magic shield prevents control while it remains active.","Nearby enemies are tethered; staying close causes a delayed stun."}[slot];
            return new[]{"Physical skillshot; a hit reduces ability cooldowns.","Mark a champion or turret; another hit detonates the mark.","Blink toward your aim and launch a seeking bolt. New casts respect roots.","A wide, piercing energy wave."}[slot];
        }
        public static string Passive(string c){return c=="Mel"?"Searing Brilliance: casts bank 3 bonus attack bolts (max 9). Damage applies Overwhelm; low-health marked targets can be executed.":c=="Ezreal"?"Hits build Rising Spell Force stacks (simplified).":c=="Ahri"?"Champion takedowns restore health (simplified).":"Magic damage restores a little health (simplified).";}
    }
}
