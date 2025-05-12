using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTHunter
{
    internal class Vars
    {

        public static string banner = $@" ___ _____ _  _          _                  (__)
| _ \_   _| || |_  _ _ _| |_ ___ _ _`\------(oo)
|  _/ | | | __ | || | ' \  _/ -_) '_| ||     (__)
|_|   |_| |_||_|\_,_|_||_\__\___|_|   ||w--||   
┌───────────────────────────────────────────┐   
│ version : v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}                        │   
│                                           │   
│ Advanced path traversal analysis tool     │   
└───────────────────────────────────────────┘   
                               coded by k4tz    ";

        public const string default_payload = "payload.txt";
        public const string default_user_agents = "user-agents.txt";

    }
}
