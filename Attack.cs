using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using static PTHunter.Program;

namespace PTHunter
{
    internal class Attack
    {
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

        public static async Task<int> StartAttackAsync(AttackOptions opts)
        {
            HttpClient client = new HttpClient();

            int pt_counter = 0;
            int payloads_scanned = 0;
            int total_payloads_count = 0;
            bool use_file_tags = false;
            if (!opts.NoBanner)
                Console.WriteLine(Vars.banner);

            if (!String.IsNullOrEmpty(opts.UserAgent))
            {
                if (opts.UserAgent.ToLower() == "random")
                {
                    try
                    {
                        string filePath = Path.Combine(AppContext.BaseDirectory, $"{Vars.default_user_agents}");

                        if (!File.Exists(filePath))
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                filePath = Path.Combine("/usr/local/share/pthunter", Vars.default_user_agents);

                                if (!File.Exists(filePath))
                                {
                                    Utils.WriteLine($"User agents files '{Vars.default_user_agents}' doesn`t exist", WType.Error);
                                    return 1;
                                }
                            }
                        }

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

            if (String.IsNullOrEmpty(opts.Tag))
            {
                Utils.WriteLine($"Tag is not specified", WType.Error);
                return 1;
            }

            if (!String.IsNullOrEmpty(opts.Auth))
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(opts.Auth);


            Console.WriteLine($"Payload selected : {opts.Payload}");

            if (!String.IsNullOrEmpty(opts.Payload))
                total_payloads_count += File.ReadLines(opts.Payload).Count();

            total_payloads_count += opts.PayloadString.Count();

            Console.WriteLine($"Total payloads count : {total_payloads_count}");

            int delay = opts.Delay;

            if (!String.IsNullOrEmpty(opts.File) || !String.IsNullOrEmpty(opts.FileTag)) 
            {
                if (String.IsNullOrEmpty(opts.File) || String.IsNullOrEmpty(opts.FileTag))
                {
                    Utils.WriteLine($"File path or file tag has been specified but will not be used", WType.Warning);
                }
                else
                {
                    use_file_tags = true;
                }
                Console.WriteLine($"File : {opts.File}");
                Console.WriteLine($"File tag : {opts.FileTag}");
            }

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

            if (use_file_tags && opts.PayloadString.Count() == 0 && !Path.GetFileNameWithoutExtension(opts.Payload).EndsWith("_f"))
            {
                Utils.WriteLine($"Payload may not support file tags", WType.Warning);
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
            Console.WriteLine("---------------------------------------------------");
            int lastRowCount = 0;
            int lastTopRow = Console.CursorTop;

            foreach (string host in opts.Host)
            {
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
                                    if (use_file_tags)
                                        line = line.Replace(opts.FileTag, opts.File);

                                    double g = (double)payloads_scanned / (double)total_payloads_count;
                                    UpdateConsoleLine(TrimIfTooLong($"[{(int)(g * 100)}%][{payloads_scanned}] > {line}", Console.WindowWidth - 3), ref lastRowCount, ref lastTopRow);
                                    using var result = await client.GetAsync(host.Replace(opts.Tag, line));

                                    if (opts.Verbose)
                                    {
                                        UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                        Console.WriteLine("");
                                        Console.WriteLine("------------------------");
                                        Console.WriteLine($"Target : {TrimIfTooLong(host.Replace(opts.Tag, line), Console.WindowWidth - 3)}");
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
                                        Console.Write($"] {host.Replace(opts.Tag, line)}{Environment.NewLine}");

                                        pt_counter++;

                                        if (opts.Break)
                                            break;
                                    }
                                    payloads_scanned++;
                                }
                                catch (Exception ex)
                                {
                                    UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                    Utils.WriteLine($"An error occurred when connecting to '{host.Replace(opts.Tag, line)}'", WType.Error);

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
                            if (use_file_tags)
                                line = line.Replace(opts.FileTag, opts.File);

                            double g = (double)payloads_scanned / (double)total_payloads_count;
                            UpdateConsoleLine(TrimIfTooLong($"[{(int)(g * 100)}%][{payloads_scanned}] > {line}", Console.WindowWidth - 3), ref lastRowCount, ref lastTopRow);
                            using var result = await client.GetAsync(host.Replace(opts.Tag, line));

                            if (opts.Verbose)
                            {
                                UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                                Console.WriteLine("");
                                Console.WriteLine("------------------------");
                                Console.WriteLine($"Target : {TrimIfTooLong(host.Replace(opts.Tag, line), Console.WindowWidth - 3)}");
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
                                Console.Write($"] {host.Replace(opts.Tag, line)}{Environment.NewLine}");

                                pt_counter++;

                                if (opts.Break)
                                    break;
                            }
                            payloads_scanned++;
                        }
                        catch (Exception ex)
                        {
                            UpdateConsoleLine("", ref lastRowCount, ref lastTopRow);
                            Utils.WriteLine($"An error occurred when connecting to '{host.Replace(opts.Tag, line)}'", WType.Error);

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
            Console.WriteLine($"Hosts scanned : {opts.Host.Count()}");
            Console.WriteLine($"Total payloads count : {total_payloads_count}");
            Console.WriteLine($"Payloads scanned : {payloads_scanned}");
            Console.WriteLine($"PTs found : {pt_counter}");
            Console.WriteLine($"Time elapsed (ms) : {watch.ElapsedMilliseconds}");
            return 1;
        }
    }
}
