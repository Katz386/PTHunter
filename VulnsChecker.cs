using PTHunter.Exploits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTHunter
{
    public class VulnsChecker
    {
        public class Vulnerability
        { 
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public static List<Vulnerability> CheckWebserver(string webserver)
        {
            var list = new List<Vulnerability>();
            
            if (CVE_2021_42013.Check(webserver) == CheckResult.Vulnerable)
            {
                list.Add(new Vulnerability { Name = "CVE-2021-42013", Description = CVE_2021_42013.description });
            }

            return list;
        }
    }
}
