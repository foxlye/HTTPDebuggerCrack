using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HTTPDebuggerCrack
{
    internal class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool GetVolumeInformation(string lpRootPathName, IntPtr lpVolumeNameBuffer, uint nVolumeNameSize, out uint lpVolumeSerialNumber, out uint lpMaximumComponentLength, out uint lpFileSystemFlags, IntPtr lpFileSystemNameBuffer, uint nFileSSystemNameSize);

        [DllImport("kernel32.dll")]
        static extern uint GetVersion();

        
        static string get_regex_value(string text, string regex)
        {
            try
            {
                Match result = Regex.Match(text, regex, RegexOptions.IgnoreCase);
                string ret_value = null;

                if (result.Success)
                {
                    ret_value = remove_string_statement(result.Value, ".", "");
                    return ret_value;
                }
                else
                    return null;
            }
            catch(Exception e)
            {
                logger(e.Message, true, true, ConsoleColor.Red);
                return null;
            }
        }

        static string remove_string_statement(string text, string _old, string _new)
        {
            return text.Replace(_old, _new);
        }

        static string get_http_debugger_version()
        {
            try
            {
                RegistryKey folder_key = Registry.CurrentUser;
                RegistryKey http_debugger_folder = folder_key.OpenSubKey("SOFTWARE\\MadeForNet\\HTTPDebuggerPro");
                string reg_version = http_debugger_folder.GetValue("AppVer").ToString();
                reg_version = get_regex_value(reg_version, "(\\d+.*)");

                return reg_version;
            }
            catch(Exception e)
            {
                logger(e.Message, true, true, ConsoleColor.Red); 
                return null;
            }
        }

        static string get_http_debugger_serial_number(string version)
        {
            try
            {
                uint lpVolumeSerialNumber, lpMaximumComponentLength, lpFileSystemFlags;
                uint nVolumeNameSize = 256, nFileSystemNameSize = 256;
                IntPtr lpVolumeNameBuffer = Marshal.AllocHGlobal((int)nVolumeNameSize);
                IntPtr lpFileSystemNameBuffer = Marshal.AllocHGlobal((int)nFileSystemNameSize);

                bool success = GetVolumeInformation("C:\\", lpVolumeNameBuffer, nVolumeNameSize, out lpVolumeSerialNumber, out lpMaximumComponentLength, out lpFileSystemFlags, lpFileSystemNameBuffer, nFileSystemNameSize);

                string drive_serial = success ? lpVolumeSerialNumber.ToString() : GetVersion().ToString();

                uint uint_version = Convert.ToUInt32(version);
                uint uint_drive_serial = Convert.ToUInt32(drive_serial);

                uint http_debugger_serial = (uint)(uint_version ^ ((uint_drive_serial >> 1) + 0x2E0) ^ 0x590D4);

                Marshal.FreeHGlobal(lpVolumeNameBuffer);
                Marshal.FreeHGlobal(lpFileSystemNameBuffer);

                return http_debugger_serial.ToString();
            }
            catch (Exception e) 
            {
                logger(e.Message, true, true, ConsoleColor.Red); 
                return null;
            }
        }

        static string generate_http_debugger_activation_key()
        {
            try
            {
                string key = "";
                Random rnd = new Random();

                while(key.Length != 16)
                {
                    int v1 = rnd.Next(0, 256);
                    int v2 = rnd.Next(0, 256);
                    int v3 = rnd.Next(0, 256);

                    StringBuilder sb = new StringBuilder();

                    sb.Append(v1.ToString("X2"));
                    sb.Append((v2 ^ 0x7C).ToString("X2"));
                    sb.Append((0xFF ^ v1).ToString("X2"));
                    sb.Append("7C");
                    sb.Append(v2.ToString("X2"));
                    sb.Append((v3 % 255).ToString("X2"));
                    sb.Append(((v3 % 255) ^ 7).ToString("X2"));
                    sb.Append((v1 ^ (0xFF ^ (v3 % 255))).ToString("X2"));

                    key = sb.ToString();
                }

                return key;
            }
            catch(Exception e)
            {
                logger(e.Message, true, true, ConsoleColor.Red);
                return null;
            }
        }

        static bool install_http_debugger_key_to_registry(string key_name, string key_value)
        {
            try
            {
                RegistryKey folder_key = Registry.CurrentUser;
                RegistryKey http_debugger_folder = folder_key.OpenSubKey("SOFTWARE\\MadeForNet\\HTTPDebuggerPro", true);

                http_debugger_folder.SetValue(key_name, key_value);
                http_debugger_folder.Close();

                return true;
            }
            catch (Exception e)
            {
                logger(e.Message, true, true, ConsoleColor.Red);
                return false;
            }
        }

        static bool KillIsRunningHTTPDebugger()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("HTTPDebuggerUI");

                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        logger($"Killed {process.ProcessName}.exe [PID: {process.Id}]", true, true, ConsoleColor.Green);
                        process.Kill();
                    }
                }

                return true;
            }
            catch(Exception e)
            {
                logger(e.Message, true, true, ConsoleColor.Red);
                return false;
            }
        }

        static void Main(string[] args)
        {
            if (KillIsRunningHTTPDebugger())
            {
                logger("Getting version..", true, false, ConsoleColor.Gray);

                string ver = get_http_debugger_version();

                if (ver != null)
                {
                    logger($"HTTP Debugger Version: {ver}", true, true, ConsoleColor.Green);

                    logger("Getting Install Serial..", true, false, ConsoleColor.Gray);

                    string http_debugger_serial = get_http_debugger_serial_number(ver);

                    if (http_debugger_serial != null)
                    {
                        logger($"HTTP Debugger Install Serial: SN{http_debugger_serial}", true, true, ConsoleColor.Green);

                        logger("Generating activation key..", true, false, ConsoleColor.Gray);

                        string key = generate_http_debugger_activation_key();

                        if (key != null)
                        {
                            logger($"HTTP Debugger Activation Key: {key}", true, true, ConsoleColor.Green);

                            logger("Installing key to registry..", true, false, ConsoleColor.Gray);

                            bool success_crack = install_http_debugger_key_to_registry($"SN{http_debugger_serial}", key);

                            if (success_crack)
                                logger("Successfully activated.", true, true, ConsoleColor.Yellow);
                            else
                                logger("Failed to Install Key.", true, true, ConsoleColor.Red);
                        }
                        else
                            logger("Failed to generate key.", true, true, ConsoleColor.Red);
                    }
                    else
                        logger("Failed to get install serial.", true, true, ConsoleColor.Red);
                }
                else
                    logger("Failed to get version.", true, true, ConsoleColor.Red);
            }
            else
                logger("Failed to kill HTTP Debugger process.", true, true, ConsoleColor.Red);
            

            logger("Press any key to exit.", true, false, ConsoleColor.Gray);
            Console.ReadKey();
        }

        static void logger(string msg, bool cmd, bool file, ConsoleColor col)
        {
            if (cmd)
            {
                Console.ForegroundColor = col;
                Console.WriteLine($"[github.com/foxlye] {msg}");
            }
                

            if (file)
            {
                using (StreamWriter sw = new StreamWriter("log.txt", true))
                {
                    sw.WriteLine($"[github.com/foxlye] {msg}");
                }
            }
        }
    }
}
