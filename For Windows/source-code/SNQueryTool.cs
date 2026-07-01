using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SNQueryTool
{
    class Program
    {
        // ANSI color codes
        const string RED = "\x1b[0;31m";
        const string GREEN = "\x1b[0;32m";
        const string YELLOW = "\x1b[1;33m";
        const string BLUE = "\x1b[0;34m";
        const string CYAN = "\x1b[0;36m";
        const string NC = "\x1b[0m";

        static void Main(string[] args)
        {
            bool showHelp = false;
            string targetIP = null;

            foreach (string arg in args)
            {
                if (arg == "-h" || arg == "--help")
                    showHelp = true;
                else if (!arg.StartsWith("-") && targetIP == null)
                    targetIP = arg;
            }

            if (showHelp)
            {
                ShowHelp();
                return;
            }

            Console.WriteLine($"{BLUE}========================================{NC}");
            Console.WriteLine($"{BLUE}   SN Query Tool (Cross-Platform){NC}");
            Console.WriteLine($"{BLUE}========================================{NC}");
            Console.WriteLine();

            // Check binary exists
            string binaryName = File.Exists("upgrade.exe") ? "upgrade.exe" : "upgrade-linux-amd64";
            if (!File.Exists(binaryName))
            {
                Console.WriteLine($"{RED}ERROR: Neither upgrade.exe nor upgrade-linux-amd64 found!{NC}");
                Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                Environment.Exit(1);
            }

            // Check dlv
            if (!CheckDlvAvailable())
            {
                Console.WriteLine($"{RED}ERROR: dlv (Delve debugger) not found!{NC}");
                Console.WriteLine("Install with:");
                Console.WriteLine("  go install github.com/go-delve/delve/cmd/dlv@latest");
                Console.WriteLine("  sudo apt install delve  (Ubuntu/Debian)");
                Environment.Exit(1);
            }

            // Single IP query
            if (targetIP != null)
            {
                QueryAndDisplaySingleIP(targetIP, binaryName);
                return;
            }

            // Interactive mode
            InteractiveMode(binaryName);
        }

        static void ShowHelp()
        {
            Console.WriteLine("SN Query Tool - Help");
            Console.WriteLine("====================");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  SNQueryTool [IP]");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  -h, --help      Show this help");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  SNQueryTool               Interactive mode");
            Console.WriteLine("  SNQueryTool 192.168.1.140 Query single IP");
            Console.WriteLine();
        }

        static bool CheckDlvAvailable()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dlv",
                    Arguments = "version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        static void QueryAndDisplaySingleIP(string ip, string binaryName)
        {
            if (!ValidateIP(ip))
            {
                Console.WriteLine($"{RED}Invalid IP format: {ip}{NC}");
                Console.WriteLine("Use format: 192.168.1.140");
                return;
            }

            Console.WriteLine($"{CYAN}Querying IP: {ip}{NC}");

            string sn = QuerySN(ip, binaryName);

            if (!string.IsNullOrEmpty(sn) && sn != "NOT_FOUND" && !sn.StartsWith("ERROR"))
            {
                Console.WriteLine($"{GREEN}✅ SN: {sn}{NC}");
            }
            else if (sn == "NOT_FOUND")
            {
                Console.WriteLine($"{RED}❌ IP not found in mapping{NC}");
            }
            else
            {
                Console.WriteLine($"{RED}❌ Error: {sn}{NC}");
            }
        }

        static void InteractiveMode(string binaryName)
        {
            Console.WriteLine($"{YELLOW}Interactive Mode{NC}");
            Console.WriteLine("Type 'quit' or 'exit' to quit");
            Console.WriteLine();

            while (true)
            {
                Console.Write($"{YELLOW}Enter IP: {NC}");
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "quit" || input.ToLower() == "exit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (!ValidateIP(input))
                {
                    Console.WriteLine($"{RED}Invalid IP format. Use: 192.168.1.140{NC}");
                    continue;
                }

                string sn = QuerySN(input, binaryName);

                if (!string.IsNullOrEmpty(sn) && sn != "NOT_FOUND" && !sn.StartsWith("ERROR"))
                {
                    Console.WriteLine($"{GREEN}✅ SN: {sn}{NC}");
                }
                else if (sn == "NOT_FOUND")
                {
                    Console.WriteLine($"{RED}❌ IP not found in mapping{NC}");
                }
                else
                {
                    Console.WriteLine($"{RED}❌ Error: {sn}{NC}");
                }

                Console.WriteLine();
            }
        }

        static bool ValidateIP(string ip)
        {
            if (!Regex.IsMatch(ip, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$"))
                return false;

            string[] octets = ip.Split('.');
            foreach (string octet in octets)
            {
                if (!int.TryParse(octet, out int value))
                    return false;
                if (value < 0 || value > 255)
                    return false;
            }

            return true;
        }

        static string QuerySN(string ip, string binaryName)
        {
            try
            {
                // Create dummy file
                string tmpDir = "./tmp";
                if (!Directory.Exists(tmpDir))
                    Directory.CreateDirectory(tmpDir);

                string dummyFile = "./tmp/fake.tar.gz";
                if (!File.Exists(dummyFile))
                    File.Create(dummyFile).Close();

                // Build dlv commands
                string commands = $"break main.main\ncontinue\nprint main.ipToSN[\"{ip}\"]\nquit";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dlv",
                    Arguments = $"--allow-non-terminal-interactive=true exec ./{binaryName} -- -remote \"{dummyFile}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardInputEncoding = System.Text.Encoding.UTF8
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    process.StandardInput.WriteLine(commands);
                    process.StandardInput.Close();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Extract SN
                    Match match = Regex.Match(output, "\"([^\"]+)\"");
                    if (match.Success)
                    {
                        string sn = match.Groups[1].Value;
                        if (Regex.IsMatch(sn, @"^[A-Z0-9]"))
                            return sn;
                    }

                    if (output.Contains("key not found"))
                        return "NOT_FOUND";

                    if (output.Contains("Command failed"))
                        return "ERROR: Command failed";

                    return "ERROR: Unknown error";
                }
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
