using CommandLine;
using CommandLine.Text;

namespace PTHunter
{
    internal class Program
    {
        [Verb("attack", HelpText = "Start testing the host(s)")]
        public class AttackOptions
        {
            [Option("no-banner", HelpText = "Disable welcome banner")]
            public bool NoBanner { get; set; }

            [Option('h', "host", Required = true, Separator = ';', HelpText = "Specifies the target host(s). Multiple hosts must be separated with ';'")]
            public IEnumerable<string> Host { get; set; }

            [Option('p', "payload", HelpText = "Specifies the payload file")]
            public string Payload { get; set; }

            [Option('v', "verbose", HelpText = "Enables detailed console output")]
            public bool Verbose { get; set; }

            [Option('P', "payload-string", HelpText = "Specifies the payload strings. Multiple payloads must be separated with '|'")]
            public IEnumerable<string> PayloadString { get; set; }

            [Option('t', "tag", HelpText = "The tag will be replaced by a payload")]
            public string Tag { get; set; }

            [Option('c', "cookie", HelpText = "Specifies cookies. Multiple cookies must be separated with ';'")]
            public string Cookie { get; set; }

            [Option('a', "auth", HelpText = "Specifies auth tokens and sessions")]
            public string Auth { get; set; }

            [Option('f', "file", HelpText = "Specifies the file to be inserted instead of the file tag")]
            public string File { get; set; }

            [Option('T', "file-tag", HelpText = "Specifies the file tag (NOT ALL PAYLOADS SUPPORT FILE TAG. Only with _f tag)")]
            public string FileTag { get; set; }

            [Option('d', "delay", HelpText = "Specifies the delay between requests")]
            public int Delay { get; set; }

            [Option("user-agent", HelpText = "Specifies the user agent. RANDOM for random user agent")]
            public string UserAgent { get; set; }

            [Option('b', "break-on-find", HelpText = "Specifies whether the host scan should terminate after the first detection")]
            public bool Break { get; set; }

            [Option('B', "break-on-error", HelpText = "Specifies whether the host scan should terminate after the first http error")]
            public bool BreakOnError { get; set; }

            [Option('H', "header", HelpText = "Specifies request headers in JSON format (Replace \" with \')")]
            public string Header { get; set; }

            [Option("basic", HelpText = "Specifies the credentials for basic authentication (username:password)", Separator = ':')]
            public IEnumerable<string> BasicAuthCreds { get; set; }

            [Usage(ApplicationAlias = "pthunter")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    return new List<Example>() {
                        new Example("Basic scan with default payload", new AttackOptions { Host = new List<string> { "http://example.com?filename={X}" }, Tag = "{X}"}),
                        new Example("Advanced scan with delay and user agent", new AttackOptions { Host = new List<string> { "http://example.com?filename={X}" }, Tag = "{X}", Payload = "payload_advanced", Delay = 20, UserAgent = "random"}),
                        new Example("Scan with file tag", new AttackOptions { Host = new List<string> { "http://example.com?filename={X}" }, Tag = "{X}", Payload = "payload_deep_f.txt", File = "etc/passwd", FileTag = "{FILE}"})
                    };
                }
            }
        }
        [Verb("discover", HelpText = "Scan the page for potential vulnerabilities")]
        public class DiscoverOptions
        {
            [Option("no-banner", HelpText = "Disable welcome banner")]
            public bool NoBanner { get; set; }

            [Option('v', "verbose", HelpText = "Enables detailed console output")]
            public bool Verbose { get; set; }

            [Option('c', "cookie", HelpText = "Specifies cookies. Multiple cookies must be separated with ';'")]
            public string Cookie { get; set; }

            [Option('a', "auth", HelpText = "Specifies auth tokens and sessions")]
            public string Auth { get; set; }

            [Option("user-agent", HelpText = "Specifies the user agent. RANDOM for random user agent")]
            public string UserAgent { get; set; }

            [Option('b', "break-on-find", HelpText = "Specifies whether the host scan should terminate after the first detection")]
            public bool Break { get; set; }

            [Option('B', "break-on-error", HelpText = "Specifies whether the host scan should terminate after the first http error")]
            public bool BreakOnError { get; set; }

            [Option('H', "header", HelpText = "Specifies request headers in JSON format (Replace \" with \')")]
            public string Header { get; set; }

            [Option("basic", HelpText = "Specifies the credentials for basic authentication (username:password)", Separator = ':')]
            public IEnumerable<string> BasicAuthCreds { get; set; }

            [Option('t', "target", HelpText = "Specifies the scan target", Required = true)]
            public string Target { get; set; }
        }

        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<AttackOptions, DiscoverOptions>(args)
              .MapResult(
                (AttackOptions opts) => Attack.StartAttackAsync(opts).Result,
                (DiscoverOptions opts) => Discover.StartDiscoverAsync(opts).Result,
                errs => 1);
        }
    }
}
