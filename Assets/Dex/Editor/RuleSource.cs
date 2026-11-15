using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dex.Editor
{
    // The only source-writing capability available to the model-facing workbench.
    public static class RuleSource
    {
        public static string Patch(string source, string move, string shield,string projectile=null)
        {
            Validate(move, new[]{"stunned","rooted","charmed","grounded"});
            Validate(shield, new[]{"magicShield","physicalDamage"});
            source=Replace(Replace(source,"CanMove",move),"BlocksControl",shield);
            if(projectile!=null){Validate(projectile,new[]{"novaActive","enemyProjectile","insideNova"});source=Replace(source,"DestroysProjectile",projectile);}
            return source;
        }
        static string Replace(string source,string method,string expression)
        {
            var rx=new Regex(@"(public\s+static\s+bool\s+"+method+@"\s*\([^)]*\)\s*\{\s*return\s+)([^;{}]+)(;\s*\})");
            if(rx.Matches(source).Count!=1) throw new InvalidOperationException("Expected one expression-only "+method+" method. Source left unchanged; developer review required.");
            return rx.Replace(source,m=>m.Groups[1].Value+expression+m.Groups[3].Value,1);
        }
        public static void Validate(string expression,string[] variables)
        {
            if(string.IsNullOrWhiteSpace(expression)||expression.Length>240) throw new ArgumentException("Invalid rule length.");
            new Parser(expression,variables).Parse();
        }
        class Parser
        {
            readonly string s; readonly string[] vars; int pos;
            public Parser(string input,string[] allowed){s=input;vars=allowed;}
            bool Take(string token){while(pos<s.Length&&char.IsWhiteSpace(s[pos]))pos++;if(s.Substring(pos).StartsWith(token,StringComparison.Ordinal)){pos+=token.Length;return true;}return false;}
            void Atom(){if(Take("!")){Atom();return;}if(Take("(")){Or();if(!Take(")"))throw new ArgumentException("Missing parenthesis.");return;}foreach(var v in vars.Concat(new[]{"true","false"}))if(Take(v))return;throw new ArgumentException("Unsupported rule token.");}
            void And(){Atom();while(Take("&&"))Atom();}void Or(){And();while(Take("||"))And();}
            public void Parse(){Or();while(pos<s.Length&&char.IsWhiteSpace(s[pos]))pos++;if(pos!=s.Length)throw new ArgumentException("Unexpected rule token.");}
        }
    }
}
