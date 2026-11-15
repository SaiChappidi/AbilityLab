namespace Dex
{
    public sealed class DemoCase
    {
        public string id,title,report,player,opponent,expected;
        public DemoCase(string id,string title,string report,string player,string opponent,string expected){this.id=id;this.title=title;this.report=report;this.player=player;this.opponent=opponent;this.expected=expected;}
    }
    public static class DemoCases
    {
        public static readonly DemoCase[] All={
            new DemoCase("root-blink","Binding / Arcane Shift","Ezreal can blink while rooted by Morgana. Reproduce it and repair the shared mobility gate.","Ezreal","Morgana","Root blocks a new blink; expiry restores it."),
            new DemoCase("root-dash","Solar Snare / Spirit Rush","Ahri can dash while rooted by Mel's Solar Snare. Investigate and preserve normal dashes.","Ahri","Mel","Solar Snare blocks a new dash; expiry restores it."),
            new DemoCase("mel-nova","Rebuttal / hostile missile","Mel's Rebuttal nova lets Morgana's projectile through. Repair missile destruction without blocking missiles after the nova expires.","Mel","Morgana","Active nova destroys hostile missiles inside its radius; expired nova does not."),
            new DemoCase("shield-charm","Black Shield / Charm","Ahri's Charm controls Morgana through an active Black Shield. Repair control immunity and preserve the expired-shield case.","Morgana","Ahri","An active magic shield prevents Charm; an expired shield does not.")};
        public static DemoCase Find(string id){foreach(var item in All)if(item.id==id)return item;return All[0];}
        public static bool Contains(string id){foreach(var item in All)if(item.id==id)return true;return false;}
    }
}
