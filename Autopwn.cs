using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static PTHunter.Program;

namespace PTHunter
{
    internal class Autopwn
    {
        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void UpdateConsoleLine(string text, ref int lastRowCount, ref int lastTopRow)
        {
            int width = Console.WindowWidth;
            int maxRows = Console.BufferHeight;

            lastTopRow = Math.Min(lastTopRow, maxRows - 1);

            for (int i = 0; i < lastRowCount; i++)
            {
                int row = lastTopRow + i;
                if (row >= maxRows) break;

                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', width));
            }

            int newRowCount = (text.Length + width - 1) / width;

            Console.SetCursorPosition(0, lastTopRow);
            Console.Write(text);

            lastRowCount = newRowCount;
        }
        static string TrimIfTooLong(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }

        static string GetBaseUrl(string fullUrl)
        {
            try
            {
                Uri uri = new Uri(fullUrl);
                string port = uri.IsDefaultPort ? "" : $":{uri.Port}";
                return $"{uri.Scheme}://{uri.Host}{port}";
            }
            catch
            {
                return "";
            }
        }

        static List<string> ExtractLinks(string html, string baseUrl)
        {
            var links = new List<string>();

            string pattern = @"(?:href|src)\s*=\s*(?:""([^""]+)""|'([^']+)'|([^\s>]+))";

            MatchCollection matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                string url = match.Groups[1].Value;
                if (string.IsNullOrEmpty(url)) url = match.Groups[2].Value;
                if (string.IsNullOrEmpty(url)) url = match.Groups[3].Value;

                string absoluteUrl = ResolveUrl(url, baseUrl);
                if (!string.IsNullOrEmpty(absoluteUrl) && absoluteUrl.Contains("?"))
                {
                    links.Add(absoluteUrl);
                }
            }

            return links;
        }

        static string ResolveUrl(string url, string baseUrl)
        {
            try
            {
                Uri baseUri = new Uri(baseUrl);
                Uri fullUri = new Uri(baseUri, url);
                return fullUri.ToString();
            }
            catch
            {
                return null;
            }
        }


        static string ReplaceFilePathValue(string url, string replace_string, bool save_start_path)
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);

            bool replaced = false;

            foreach (string key in query.AllKeys)
            {
                if (key == null) continue;

                string value = query[key];
                if (!string.IsNullOrEmpty(value) && !String.IsNullOrEmpty(Path.GetExtension(value)))
                {
                    if (save_start_path)
                    {
                        query[key] = value.Replace(Path.GetFileName(value), replace_string);
                    }
                    else
                    {
                        query[key] = replace_string;
                    }

                    replaced = true;
                    break;
                }
            }

            if (!replaced)
                return null;

            var queryParts = new List<string>();
            foreach (string key in query.AllKeys)
            {
                if (key == null) continue;
                string value = query[key];
                queryParts.Add($"{key}={value}");
            }

            var newQuery = string.Join("&", queryParts);

            var uriBuilder = new UriBuilder(uri)
            {
                Query = newQuery
            };

            return uriBuilder.ToString();
        }

        public static async Task<int> StartAttackAsync(AutopwnOptions opts)
        {
            int pt_counter = 0;
            int payloads_scanned = 0;
            int total_payloads_count = 0;

            HttpClient client = new HttpClient();

            if (!opts.NoBanner)
                Console.WriteLine(Vars.banner);

            if (!String.IsNullOrEmpty(opts.UserAgent))
            {
                if (opts.UserAgent.ToLower() == "random")
                {
                    try
                    {
                        string filePath = Path.Combine(AppContext.BaseDirectory, $"{Vars.default_user_agents}");

                        string selectedLine = null;
                        int count = 0;
                        Random rand = new Random();

                        foreach (string line in File.ReadLines(filePath))
                        {
                            count++;
                            if (rand.Next(count) == 0)
                            {
                                selectedLine = line;
                            }
                        }

                        if (selectedLine == null)
                            Utils.WriteLine($"User agents file is empty : {Vars.default_user_agents}", WType.Warning);


                        opts.UserAgent = selectedLine;
                    }
                    catch
                    {
                        Utils.WriteLine($"Unable to access {Vars.default_user_agents}", WType.Error);
                        return 1;
                    }


                }

                client.DefaultRequestHeaders.Add("User-Agent", opts.UserAgent);

                Console.WriteLine("User-Agent selected : " + opts.UserAgent);
            }

            if (!String.IsNullOrEmpty(opts.Auth))
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(opts.Auth);

            if (!String.IsNullOrEmpty(opts.Cookie))
            {
                try
                {
                    client.DefaultRequestHeaders.Add("Cookie", opts.Cookie);
                }
                catch (Exception ex)
                {
                    Utils.WriteLine(ex.Message, WType.Error);
                    return 1;
                }
            }

            if (!String.IsNullOrEmpty(opts.Header))
            {
                try
                {
                    var hdr = opts.Header.Replace('\'', '\"');
                    var headers = JsonSerializer.Deserialize(hdr, HeaderJsonContext.Default.DictionaryStringString);

                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                catch (System.FormatException ex)
                {
                    Utils.WriteLine(ex.Message, WType.Error);
                    return 1;
                }
                catch (System.InvalidOperationException ex)
                {
                    Utils.WriteLine(ex.Message, WType.Error);
                    return 1;
                }
                catch (System.Text.Json.JsonException)
                {
                    Utils.WriteLine("Invalid JSON format", WType.Error);
                    return 1;
                }
            }

            if (String.IsNullOrEmpty(opts.Payload) && opts.PayloadString.Count() == 0)
            {
                Utils.WriteLine($"Payload not specified. Default payload will be used", WType.Info);
                opts.Payload = Vars.default_payload;
            }


            if (!File.Exists(opts.Payload) && opts.PayloadString.Count() == 0)
            {
                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "wordlists", $"{opts.Payload}")))
                {
                    opts.Payload = Path.Combine(AppContext.BaseDirectory, "wordlists", $"{opts.Payload}");
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        opts.Payload = Path.Combine("/usr/local/share/pthunter/wordlists", opts.Payload);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        opts.Payload = Path.Combine("/usr/local/share/pthunter/wordlists", opts.Payload);
                    }

                    if (!File.Exists(opts.Payload) && opts.PayloadString.Count() == 0)
                    {
                        Utils.WriteLine($"Payload file '{opts.Payload}' not found", WType.Error);
                        return 1;
                    }
                }
            }

            Console.WriteLine($"Payload selected : {opts.Payload}");

            if (!String.IsNullOrEmpty(opts.Payload))
                total_payloads_count += File.ReadLines(opts.Payload).Count();

            total_payloads_count += opts.PayloadString.Count();

            Console.WriteLine($"Total payloads count : {total_payloads_count}");

            int delay = opts.Delay;

            if (opts.BasicAuthCreds.Count() > 0)
            {
                try
                {
                    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{opts.BasicAuthCreds.ToList()[0]}:{opts.BasicAuthCreds.ToList()[1]}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }
                catch (Exception ex)
                {
                    Utils.WriteLine(ex.ToString(), WType.Error);
                }
            }

            if (opts.Verbose)
            {
                Console.WriteLine("Client headers:");
                foreach (var header in client.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {String.Join(';', header.Value)}");
                }
                Console.WriteLine("");
            }

            Console.WriteLine($"Scanning links...{Environment.NewLine}");
            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                string base_url = GetBaseUrl(opts.Target);

                Console.WriteLine($"Base url: {base_url}");
                var response = await client.GetStringAsync(opts.Target);

                List<string> links = ExtractLinks(response, base_url);

                Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds}");
                Console.WriteLine($"Content length: {response.Length}");
                Console.WriteLine($"Links count: {links.Count}");

                string rand_string = $"REPLACE{RandomString(7)}";

                if (links.Count > 0)
                {
                    Console.WriteLine("Found links:");
                    Console.WriteLine();
                    List<string> unique_links = new List<string>();
                    foreach (var link in links)
                    {
                        Utils.WriteLine(link, WType.Success);
                        string result = ReplaceFilePathValue(link, rand_string, opts.SaveStartingPath);

                        if (!unique_links.Contains(result) && !String.IsNullOrEmpty(result))
                            unique_links.Add(result);
                    }

                   

                    Console.WriteLine();
                    Console.WriteLine($"Total unique links count: {unique_links.Count}");
                    Console.WriteLine("Unique links:");
                    Console.WriteLine();

                    foreach (var link in unique_links)
                    {
                        Console.WriteLine(link.Replace(rand_string, "PAYLOAD"));
                    }

                    Console.WriteLine();

                    Console.WriteLine("---------------------------------------------------");

                    Console.WriteLine();

                    Console.WriteLine("Scanning links for vulnerabilities...");

                    Console.WriteLine();

                    int lastRowCount = 0;
                    int lastTopRow = Console.CursorTop;

                    foreach (var link in unique_links)
                    {
                        Console.WriteLine($"Current link: {link}");
                        Console.WriteLine();
                        if (!String.IsNullOrEmpty(opts.Payload))
                        {
                            try
                            {
                                using (var reader = new StreamReader(opts.Payload))
                                {
                                    if (opts.Verbose)
                                        Console.WriteLine($"Using '{opts.Payload}':");

                                    string? line;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        try
                                        {

                                            double g = (double)payloads_scanned / (double)total_payloads_count;
                                            UpdateConsoleLine(TrimIfTooLong($"[{(int)(g * 100)}%][{payloads_scanned}] > {line}", Console.WindowWidth - 3), ref lastRowCount, ref lastTopRow);
                                            using var result = await client.GetAsync(link.Replace(rand_string, line));

                                            if (opts.Verbose)
                                            {
                                                UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                                Console.WriteLine("");
                                                Console.WriteLine("------------------------");
                                                Console.WriteLine($"Target : {TrimIfTooLong(link.Replace(rand_string, line), Console.WindowWidth - 3)}");
                                                Console.WriteLine($"Time elapsed : {watch.ElapsedMilliseconds}ms");
                                                Console.WriteLine($"Status code : {result.StatusCode}");
                                                Console.WriteLine($"Content : {TrimIfTooLong(await result.Content.ReadAsStringAsync(), Console.WindowWidth - 3)}");
                                                Console.WriteLine("=== Response headers ===");
                                                foreach (var header in result.Headers)
                                                {
                                                    Console.WriteLine($"{header.Key}: {String.Join(';', header.Value)}");
                                                }
                                                Console.WriteLine("=== Response headers END ===");
                                            }

                                            if (result.IsSuccessStatusCode)
                                            {
                                                UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                                Console.WriteLine();
                                                Utils.Write($"[", WType.Success);
                                                Console.ForegroundColor = ConsoleColor.Green;
                                                Console.Write(result.StatusCode);
                                                Console.ResetColor();
                                                Console.Write($"] {link.Replace(rand_string, line)}{Environment.NewLine}");

                                                pt_counter++;

                                                if (opts.Break)
                                                    break;
                                            }
                                            payloads_scanned++;
                                        }
                                        catch (Exception ex)
                                        {
                                            UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                            Utils.WriteLine($"An error occurred when connecting to '{link.Replace(rand_string, line)}'", WType.Error);

                                            if (opts.BreakOnError)
                                                break;
                                        }
                                        if (opts.Verbose)
                                        {
                                            UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                            Console.WriteLine("------------------------");
                                        }

                                        Thread.Sleep(delay);
                                    }
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                Utils.WriteLine($"An error occurred when reading payload '{opts.Payload}'", WType.Error);
                            }
                        }

                        if (opts.PayloadString.Count() > 0)
                        {
                            foreach (var payload in opts.PayloadString)
                            {
                                string line = payload;
                                try
                                {
                                    double g = (double)payloads_scanned / (double)total_payloads_count;
                                    UpdateConsoleLine(TrimIfTooLong($"[{(int)(g * 100)}%][{payloads_scanned}] > {line}", Console.WindowWidth - 3), ref lastRowCount, ref lastTopRow);
                                    using var result = await client.GetAsync(link.Replace(rand_string, line));

                                    if (opts.Verbose)
                                    {
                                        UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                        Console.WriteLine("");
                                        Console.WriteLine("------------------------");
                                        Console.WriteLine($"Target : {TrimIfTooLong(link.Replace(rand_string, line), Console.WindowWidth - 3)}");
                                        Console.WriteLine($"Time elapsed : {watch.ElapsedMilliseconds}ms");
                                        Console.WriteLine($"Status code : {result.StatusCode}");
                                        Console.WriteLine($"Content : {TrimIfTooLong(await result.Content.ReadAsStringAsync(), Console.WindowWidth - 3)}");
                                        Console.WriteLine("=== Response headers ===");
                                        foreach (var header in result.Headers)
                                        {
                                            Console.WriteLine($"{header.Key}: {String.Join(';', header.Value)}");
                                        }
                                        Console.WriteLine("=== Response headers END ===");
                                    }

                                    if (result.IsSuccessStatusCode)
                                    {
                                        UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                        Console.WriteLine();
                                        Utils.Write($"[", WType.Success);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.Write(result.StatusCode);
                                        Console.ResetColor();
                                        Console.Write($"] {link.Replace(rand_string, line)}{Environment.NewLine}");

                                        pt_counter++;

                                        if (opts.Break)
                                            break;
                                    }
                                    payloads_scanned++;
                                }
                                catch (Exception ex)
                                {
                                    UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                    Utils.WriteLine($"An error occurred when connecting to '{link.Replace(rand_string, line)}'", WType.Error);

                                    if (opts.BreakOnError)
                                        break;
                                }
                                if (opts.Verbose)
                                {
                                    UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                    Console.WriteLine("------------------------");
                                }
                                Thread.Sleep(delay);
                            }
                        }
                    }
                    UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                    Console.WriteLine();
                    Console.WriteLine("---------------------------------------------------");
                    watch.Stop();
                    Console.WriteLine("Scanning complete");
                    Console.WriteLine();
                    Console.WriteLine($"Links scanned : {unique_links.Count()}");
                    Console.WriteLine($"Total payloads count : {total_payloads_count}");
                    Console.WriteLine($"Payloads scanned : {payloads_scanned}");
                    Console.WriteLine($"PTs found : {pt_counter}");
                    Console.WriteLine($"Time elapsed (ms) : {watch.ElapsedMilliseconds}");
                }

            }
            catch (Exception ex)
            {
                Utils.WriteLine($"Unhandled error occurred: {ex.Message}", WType.Error);
            }

            return 0;
        }
    }
}
