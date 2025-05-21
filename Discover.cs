using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static PTHunter.Program;

namespace PTHunter
{
    internal class Discover
    {
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


        public static async Task<int> StartDiscoverAsync(DiscoverOptions opts)
        {
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

            Console.WriteLine("Scanning...");
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

                if (links.Count > 0)
                {
                    Console.WriteLine("Found links:");
                    Console.WriteLine();
                    List<string> unique_links = new List<string>();
                    foreach (var link in links)
                    {
                        Utils.WriteLine(link, WType.Success);
                        string result = ReplaceFilePathValue(link, "X", false);

                        if (!unique_links.Contains(result) && !String.IsNullOrEmpty(result))
                            unique_links.Add(result);
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Total unique links count: {unique_links.Count}");
                    Console.WriteLine("Unique links:");
                    Console.WriteLine();

                    foreach (var link in unique_links)
                    {
                        Console.WriteLine(link);
                    }
                }

            }
            catch (Exception ex)
            {
                Utils.WriteLine($"Unhandled error occurred: {ex.Message}", WType.Error);
            }
            watch.Stop();

            return 0;
        }
    }
}
