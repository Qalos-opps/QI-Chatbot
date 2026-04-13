using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QalosIntelligence
{
    // ==================== ENTRY POINT ====================
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new QIBot();
            bot.Run();
        }
    }

    // ==================== MAIN CONTROLLER ====================
    public class QIBot
    {
        // Variables to store helper objects
        private UserInterface _ui;
        private ResponseEngine _responses;
        private InputValidator _validator;
        private string _userName = "Friend";

        // Constructor - runs when bot is created
        public QIBot()
        {
            _ui = new UserInterface();
            _responses = new ResponseEngine();
            _validator = new InputValidator();
        }

        // Main execution flow
        public void Run()
        {
            try
            {
                PlayVoiceGreeting();      // Step 1: Play audio
                DisplayAsciiArt();       // Step 2: Show logo
                GetUserName();           // Step 3: Get user's name
                ShowWelcomeMessage();    // Step 4: Welcome them
                RunChatLoop();           // Step 5: Chat forever
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Critical error: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Play voice greeting or simulate it
        private void PlayVoiceGreeting()
        {
            _ui.ShowSectionHeader("VOICE GREETING");

            try
            {
                // Look for audio file in multiple locations (accept common variants)
                string[] possiblePaths = new[]
                {
                  "greeting.wav",
                  "greeting .wav",
                  "greetings.wav",
                  Path.Combine("Audio", "greeting.wav"),
                  Path.Combine("Audio", "greeting .wav"),
                  Path.Combine("Audio", "greetings.wav"),
                  Path.Combine("Resources", "greeting.wav"),
                  Path.Combine("Resources", "greeting .wav"),
                  Path.Combine("Resources", "greetings.wav"),
                  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greeting.wav"),
                  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greeting .wav"),
                  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greetings.wav")
                };

                string audioPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        audioPath = path;
                        break;
                    }
                }

                if (audioPath != null)
                {
                    bool played = AudioHelper.PlayWavSync(audioPath);
                    if (played)
                    {
                        _ui.ShowSuccess("🔊 Voice greeting played successfully!");
                    }
                    else
                    {
                        _ui.ShowWarning("⚠️  Audio file found but playback failed");
                        _ui.ShowInfo("💡 Tip: Record a WAV file saying 'Hello! Welcome to QI. I'm here to help you stay safe online.'");

                        _ui.TypeText("🎵 [Simulated Voice]: ", ConsoleColor.Cyan, 20);
                        _ui.TypeText("\"Hello! Welcome to QI. I'm here to help you stay safe online.\"", ConsoleColor.White, 30);
                        Console.WriteLine();
                    }
                }
                else
                {
                    _ui.ShowWarning("⚠️  Audio file 'greeting.wav' not found");
                    _ui.ShowInfo("💡 Tip: Record a WAV file saying 'Hello! Welcome to QI. I'm here to help you stay safe online.'");

                    _ui.TypeText("🎵 [Simulated Voice]: ", ConsoleColor.Cyan, 20);
                    _ui.TypeText("\"Hello! Welcome to QI. I'm here to help you stay safe online.\"", ConsoleColor.White, 30);
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _ui.ShowError($"Audio playback failed: {ex.Message}");
            }

            Thread.Sleep(500);
        }

        // Display QI logo
        private void DisplayAsciiArt()
        {
            _ui.ClearScreen();
            _ui.ShowSectionHeader("QALOS INTELLIGENCE (QI)");

            string asciiArt = @"
    ╔══════════════════════════════════════════════════════════════════╗
    ║                                                                  ║
    ║     ██████╗  █████╗ ██╗      ██████╗ ███████╗                    ║
    ║    ██╔═══██╗██╔══██╗██║     ██╔═══██╗██╔════╝                    ║
    ║    ██║   ██║███████║██║     ██║   ██║███████╗                    ║
    ║    ██║▄▄ ██║██╔══██║██║     ██║   ██║╚════██║                    ║
    ║    ╚██████╔╝██║  ██║███████╗╚██████╔╝███████║                    ║
    ║     ╚══▀▀═╝ ╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚══════╝                    ║
    ║                                                                  ║
    ║              ██╗███╗   ██╗████████╗███████╗██╗                   ║
    ║              ██║████╗  ██║╚══██╔══╝██╔════╝██║                   ║
    ║              ██║██╔██╗ ██║   ██║   █████╗  ██║                   ║
    ║              ██║██║╚██╗██║   ██║   ██╔══╝  ██║                   ║
    ║              ██║██║ ╚████║   ██║   ███████╗███████╗              ║
    ║              ╚═╝╚═╝  ╚═══╝   ╚═╝   ╚══════╝╚══════╝              ║
    ║                                                                  ║
    ║              QALOS INTELLIGENCE SECURITY SYSTEM                  ║
    ║                                                                  ║ 
    ║                                                                  ║
    ║     🔐 Password Safety   🎣 Phishing Detection                   ║
    ║                                                                  ║
    ║                                                                  ║
    ║                                                                  ║
    ║     🌐 Safe Browsing     🛡️ Privacy Protection                   ║
    ║                                                                  ║
    ║                                                                  ║
    ╚══════════════════════════════════════════════════════════════════╝
";

            _ui.ShowAsciiArt(asciiArt);
            Thread.Sleep(300);
        }

        // Ask for and validate user's name
        private void GetUserName()
        {
            _ui.ShowDivider();
            _ui.TypeText("Before we begin, I'd love to know your name!", ConsoleColor.Green, 30);
            Console.WriteLine();

            bool validName = false;
            while (!validName)
            {
                _ui.PromptInput("Please enter your name");
                string input = Console.ReadLine();

                var validation = _validator.ValidateName(input);
                if (validation.IsValid)
                {
                    _userName = validation.CleanedValue;
                    validName = true;
                    _ui.ShowSuccess($"Welcome, {_userName}! 👋");
                }
                else
                {
                    _ui.ShowError(validation.ErrorMessage);
                }
            }
        }

        // Show personalized welcome
        private void ShowWelcomeMessage()
        {
            _ui.ClearScreen();
            _ui.ShowSectionHeader($"WELCOME, {_userName.ToUpper()}!");

            string welcomeText = $@"
╔════════════════════════════════════════════════════════════════════╗
║                                                                    ║
║   Hello, {_userName}! Welcome to QI - Qalos Intelligence.          ║
║                                                                    ║
║   I'm here to help you stay safe online by providing guidance      ║
║   on critical cybersecurity topics:                                ║
║                                                                    ║
║   • 🔐 Password Safety & Strong Authentication                     ║
║   • 🎣 Phishing Detection & Social Engineering Defense             ║
║   • 🌐 Safe Browsing & Web Security                                ║
║   • 🛡️ Privacy Protection & Data Security                          ║
║                                                                    ║
║   Type 'help' to see available commands, or 'exit' to quit.        ║
║                                                                    ║
╚════════════════════════════════════════════════════════════════════╝
";

            _ui.ShowBoxedText(welcomeText);
            Thread.Sleep(500);
        }

        // Main chat loop - runs until user types exit
        private void RunChatLoop()
        {
            bool running = true;

            while (running)
            {
                _ui.ShowDivider();
                _ui.PromptInput($"{_userName}, enter your question (or 'help'/'exit')");

                string input = Console.ReadLine();

                // Validate input
                var validation = _validator.ValidateInput(input);
                if (!validation.IsValid)
                {
                    _ui.ShowError(validation.ErrorMessage);
                    continue;
                }

                string command = validation.CleanedValue.ToLower();

                // Process command
                switch (command)
                {
                    case "exit":
                    case "quit":
                    case "bye":
                        ShowFarewell();
                        running = false;
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    case "clear":
                        _ui.ClearScreen();
                        break;

                    default:
                        ProcessQuery(command, validation.CleanedValue);
                        break;
                }
            }
        }

        // Get and display response
        private void ProcessQuery(string command, string originalInput)
        {
            _ui.ShowThinkingAnimation();
            string response = _responses.GetResponse(command, originalInput, _userName);
            _ui.ShowBotResponse(response);
        }

        // Show help menu
        private void ShowHelp()
        {
            string helpText = @"
AVAILABLE COMMANDS AND TOPICS:
══════════════════════════════════════════════════════════════════════

GENERAL INTERACTION:
  • how are you           - Check QI's status
  • what's your purpose    - Learn about QI's mission
  • what can i ask you   - See all available topics

CYBERSECURITY TOPICS:
  • password safety      - Master password creation and management
  • phishing             - Recognize and avoid digital scams
  • safe browsing        - Navigate the web securely
  • privacy              - Protect your personal information
  • malware              - Understand and prevent malicious software

SYSTEM COMMANDS:
  • help                 - Display this menu
  • clear                - Clear the screen
  • exit / quit / bye    - Shutdown QI

══════════════════════════════════════════════════════════════════════
";

            _ui.ShowInfo(helpText);
        }

        // Show goodbye message
        private void ShowFarewell()
        {
            _ui.ClearScreen();
            string farewell = $@"
╔════════════════════════════════════════════════════════════════════╗
║                                                                    ║
║             👋 GOODBYE, {_userName.ToUpper()}!                     ║
║                                                                    ║
║     Remember: In the quantum realm of cyberspace, awareness        ║
║     is your greatest defense. Stay curious, stay secure!           ║
║                                                                    ║
║          🔒 Security is not a product, but a process. 🔒           ║
║                                                                    ║
╚════════════════════════════════════════════════════════════════════╝
";

            _ui.ShowBoxedText(farewell);

            // Try to play goodbye sound using AudioHelper
            try
            {
                if (File.Exists("goodbye.wav"))
                {
                    AudioHelper.PlayWavSync("goodbye.wav");
                }
            }
            catch { }

            Thread.Sleep(1000);
        }
    }

    // ==================== AUDIO HELPER (replaces System.Media.SoundPlayer usage) ====================
    public static class AudioHelper
    {
        // Use winmm.dll PlaySound on Windows for simple synchronous playback of .wav files.
        // On other platforms try to start the file with the default associated application.
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        private const uint SND_SYNC = 0x0000;
        private const uint SND_FILENAME = 0x00020000;
        private const uint SND_NODEFAULT = 0x00000002;

        public static bool PlayWavSync(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Play synchronously and return success flag. Avoid falling back to default sound.
                    bool ok = PlaySound(path, IntPtr.Zero, SND_FILENAME | SND_SYNC | SND_NODEFAULT);
                    if (!ok)
                    {
                        // GetLastWin32Error can help diagnose failures when needed.
                        int err = Marshal.GetLastWin32Error();
                        return false;
                    }
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS: use afplay for simple WAV playback
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "afplay",
                            Arguments = $"\"{path}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (var proc = Process.Start(psi))
                        {
                            proc?.WaitForExit();
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    // Linux/other Unix: try a sequence of common players, then fall back to shell open
                    string[] linuxPlayers = new[] { "aplay", "paplay", "ffplay", "xdg-open" };

                    foreach (var player in linuxPlayers)
                    {
                        try
                        {
                            string fileName = player;
                            string arguments = $"\"{path}\"";

                            if (player == "ffplay")
                            {
                                fileName = "ffplay";
                                arguments = $"-nodisp -autoexit \"{path}\"";
                            }

                            // UseShellExecute = false so we get a Process to wait on when possible
                            var psi = new ProcessStartInfo
                            {
                                FileName = fileName,
                                Arguments = arguments,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (var proc = Process.Start(psi))
                            {
                                if (proc == null)
                                    continue;
                                proc.WaitForExit();
                            }

                            return true;
                        }
                        catch
                        {
                            // try next player
                        }
                    }

                    // Last resort: try to open with the default handler (may not give a Process to wait on)
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        };

                        using (var proc = Process.Start(psi))
                        {
                            proc?.WaitForExit();
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }

    // ==================== VISUAL INTERFACE ====================
    public class UserInterface
    {
        private ConsoleColor _primaryColor = ConsoleColor.Green;
        private ConsoleColor _secondaryColor = ConsoleColor.Cyan;
        private ConsoleColor _accentColor = ConsoleColor.Yellow;

        public void ClearScreen()
        {
            Console.Clear();
            Console.ResetColor();
        }

        public void ShowSectionHeader(string title)
        {
            Console.WriteLine();
            Console.ForegroundColor = _primaryColor;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
            Console.Write("║  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(title.PadRight(64));
            Console.ForegroundColor = _primaryColor;
            Console.WriteLine("  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        public void ShowAsciiArt(string art)
        {
            Console.ForegroundColor = _secondaryColor;
            Console.WriteLine(art);
            Console.ResetColor();
        }

        public void ShowBoxedText(string text)
        {
            Console.ForegroundColor = _primaryColor;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public void ShowDivider()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("─────────────────────────────────────────────────────────────────────");
            Console.ResetColor();
        }

        public void PromptInput(string prompt)
        {
            Console.WriteLine();
            Console.ForegroundColor = _accentColor;
            Console.Write($"➤ {prompt}: ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void TypeText(string text, ConsoleColor color, int delayMs = 30)
        {
            Console.ForegroundColor = color;
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(delayMs);
            }
            Console.ResetColor();
        }

        public void ShowBotResponse(string response)
        {
            Console.WriteLine();
            Console.ForegroundColor = _secondaryColor;
            Console.WriteLine("┌─ QI RESPONSE ─────────────────────────────────────────────────────┐");
            Console.ForegroundColor = ConsoleColor.White;

            string[] lines = response.Split('\n');
            foreach (string line in lines)
            {
                Console.Write("│ ");
                TypeText(line, ConsoleColor.White, 10);
                Console.WriteLine();
            }

            Console.ForegroundColor = _secondaryColor;
            Console.WriteLine("└────────────────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void ShowThinkingAnimation()
        {
            Console.WriteLine();
            string[] frames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

            for (int i = 0; i < 10; i++)
            {
                Console.ForegroundColor = _accentColor;
                Console.Write($"\r{frames[i % frames.Length]} QI is analyzing... ");
                Thread.Sleep(100);
            }

            Console.WriteLine("\r                              ");
            Console.ResetColor();
        }

        public void ShowSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {message}");
            Console.ResetColor();
        }

        public void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {message}");
            Console.ResetColor();
        }

        public void ShowWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ {message}");
            Console.ResetColor();
        }

        public void ShowInfo(string message)
        {
            Console.ForegroundColor = _secondaryColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    // ==================== KNOWLEDGE BASE ====================
    public class ResponseEngine
    {
        private Dictionary<string, string> _responses;

        public ResponseEngine()
        {
            _responses = InitializeResponses();
        }

        private Dictionary<string, string> InitializeResponses()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["how are you"] = "QI is operating at quantum efficiency! All security protocols are active and ready to help you stay safe online. How are you feeling about your digital security today?",

                ["how are you?"] = "QI is operating at quantum efficiency! All security protocols are active and ready to help you stay safe online. How are you feeling about your digital security today?",

                ["what's your purpose"] = "I am QI - Qalos Intelligence. My purpose is to educate and assist you in maintaining robust cybersecurity practices. I provide guidance on password safety, phishing prevention, safe browsing, and privacy protection. Think of me as your personal quantum security consultant!",

                ["what is your purpose"] = "I am QI - Qalos Intelligence. My purpose is to educate and assist you in maintaining robust cybersecurity practices. I provide guidance on password safety, phishing prevention, safe browsing, and privacy protection. Think of me as your personal quantum security consultant!",

                ["what can i ask you"] = @"You can ask QI about several critical cybersecurity topics:

1. PASSWORD SAFETY - Creating unbreakable passwords and managing them securely
2. PHISHING - Identifying and neutralizing social engineering attacks  
3. SAFE BROWSING - Navigating the quantum web without leaving traces
4. PRIVACY - Protecting your personal data from prying eyes
5. MALWARE - Understanding and preventing digital infections

Simply type any topic name to access QI's knowledge base!",

                ["what can i ask you about"] = @"You can ask QI about several critical cybersecurity topics:

1. PASSWORD SAFETY - Creating unbreakable passwords and managing them securely
2. PHISHING - Identifying and neutralizing social engineering attacks  
3. SAFE BROWSING - Navigating the quantum web without leaving traces
4. PRIVACY - Protecting your personal data from prying eyes
5. MALWARE - Understanding and preventing digital infections

Simply type any topic name to access QI's knowledge base!",

                ["password"] = @"🔐 QI PASSWORD SAFETY PROTOCOLS:

QUANTUM STRENGTH REQUIREMENTS:
• Minimum 16 characters (12 absolute minimum)
• Mix uppercase, lowercase, numbers, and symbols: P@ssw0rd!sW3ak
• Never reuse passwords across quantum systems
• Enable 2FA (Two-Factor Authentication) - your quantum shield
• Use password managers: Bitwarden, 1Password, or KeePass
• Avoid dictionary words, names, dates, or patterns

QI REMINDER: Your password is the key to your digital universe. Protect it like you would protect your physical home keys!",

                ["passwords"] = @"🔐 QI PASSWORD SAFETY PROTOCOLS:

QUANTUM STRENGTH REQUIREMENTS:
• Minimum 16 characters (12 absolute minimum)
• Mix uppercase, lowercase, numbers, and symbols: P@ssw0rd!sW3ak
• Never reuse passwords across quantum systems
• Enable 2FA (Two-Factor Authentication) - your quantum shield
• Use password managers: Bitwarden, 1Password, or KeePass
• Avoid dictionary words, names, dates, or patterns

QI REMINDER: Your password is the key to your digital universe. Protect it like you would protect your physical home keys!",

                ["password safety"] = @"🔐 QI PASSWORD SAFETY PROTOCOLS:

QUANTUM STRENGTH REQUIREMENTS:
• Minimum 16 characters (12 absolute minimum)
• Mix uppercase, lowercase, numbers, and symbols: P@ssw0rd!sW3ak
• Never reuse passwords across quantum systems
• Enable 2FA (Two-Factor Authentication) - your quantum shield
• Use password managers: Bitwarden, 1Password, or KeePass
• Avoid dictionary words, names, dates, or patterns

QI REMINDER: Your password is the key to your digital universe. Protect it like you would protect your physical home keys!",

                ["phishing"] = @"🎣 QI PHISHING DETECTION MATRIX:

WARNING INDICATORS:
• Urgent quantum threats: 'Account suspended in 24 hours!'
• Suspicious sender addresses: support@amaz0n-security.net
• Generic greetings: 'Dear Customer' instead of your name
• Credential harvesting: Requests for passwords via email
• Malicious attachments: Unexpected .exe, .zip, or .docm files
• Too-perfect offers: 'You won $1,000,000! Click here!'

QI DEFENSE PROTOCOLS:
• Hover before clicking - verify the actual URL destination
• Contact organizations directly through official channels
• Never submit credentials through email links
• Install anti-phishing browser extensions
• Report suspicious quantum signals to IT security

QI ADVICE: When in doubt, verify through a separate trusted channel!",

                ["safe browsing"] = @"🌐 QI SAFE BROWSING NAVIGATION:

QUANTUM WEB PROTECTION:
• Keep browsers updated - patches close security wormholes
• Verify HTTPS (padlock icon) before entering sensitive data
• Use private/incognito mode on shared quantum terminals
• Disable auto-fill for financial data on public devices
• Clear quantum traces: cookies, cache, and history regularly
• Deploy ad-blockers (uBlock Origin) to stop malvertising
• Download only from official quantum repositories
• Audit browser extensions - remove unused, verify trusted sources

QI WISDOM: Your browser is your portal to the digital multiverse - fortify it!",

                ["browsing"] = @"🌐 QI SAFE BROWSING NAVIGATION:

QUANTUM WEB PROTECTION:
• Keep browsers updated - patches close security wormholes
• Verify HTTPS (padlock icon) before entering sensitive data
• Use private/incognito mode on shared quantum terminals
• Disable auto-fill for financial data on public devices
• Clear quantum traces: cookies, cache, and history regularly
• Deploy ad-blockers (uBlock Origin) to stop malvertising
• Download only from official quantum repositories
• Audit browser extensions - remove unused, verify trusted sources

QI WISDOM: Your browser is your portal to the digital multiverse - fortify it!",

                ["privacy"] = @"🛡️ QI PRIVACY PROTECTION FIELD:

DATA SHIELDING TECHNIQUES:
• Review social media privacy settings quarterly
• Minimize personal data exposure (birthdate, location, phone)
• Deploy VPNs on public Wi-Fi to encrypt quantum transmissions
• Enable 'Do Not Track' in browser privacy settings
• Read privacy policies before granting app permissions
• Use encrypted messaging: Signal, WhatsApp, or Matrix
• Conduct regular app permission audits on all devices
• Switch to privacy-focused search: DuckDuckGo or Startpage

QI PRINCIPLE: Your personal data is more valuable than gold in the quantum age - guard it!",

                ["malware"] = @"🦠 QI MALWARE THREAT ANALYSIS:

DIGITAL PATHOGEN TYPES:
• Viruses: Self-replicating code that corrupts systems
• Ransomware: Encrypts your files, demands quantum ransom
• Spyware: Stealth surveillance of your digital activities
• Trojans: Malicious code disguised as legitimate software
• Adware: Unwanted advertisements and browser hijackers

QI IMMUNIZATION PROTOCOLS:
• Install reputable antivirus: Bitdefender, Kaspersky, or Windows Defender
• Enable automatic OS and application updates
• Never open email attachments from unknown quantum signatures
• Scan all external storage before file access
• Maintain offline backups of critical data (3-2-1 rule)
• Activate firewalls to block unauthorized quantum access

QI MAXIM: Prevention is infinitely more efficient than dealing with infection aftermath!"
            };
        }

        public string GetResponse(string command, string originalInput, string userName)
        {
            // Check exact match
            if (_responses.ContainsKey(command))
            {
                return _responses[command];
            }

            // Check partial match
            foreach (var kvp in _responses)
            {
                if (command.Contains(kvp.Key) || kvp.Key.Contains(command))
                {
                    return kvp.Value;
                }
            }

            // Return random default response
            return GetDefaultResponse(userName);
        }

        private string GetDefaultResponse(string userName)
        {
            string[] defaults = new[]
            {
        $"QI's quantum sensors are not detecting a match for that query, {userName}. Could you rephrase? Try 'help' to see available commands.",
        $"That query is outside QI's current knowledge matrix, {userName}. I specialize in cybersecurity topics. Type 'help' for available options.",
        $"QI apologizes, but I don't understand that specific query. As your security assistant, I focus on password safety, phishing, and privacy. What would you like to know?",
        $"Unknown quantum signature detected, {userName}. I'm programmed to help with digital security topics. Type 'help' to see my capabilities!"
      };

            Random rand = new Random();
            return defaults[rand.Next(defaults.Length)];
        }
    }

    // ==================== INPUT VALIDATION ====================
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string CleanedValue { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class InputValidator
    {
        private string[] _forbiddenChars = new[] { "<", ">", "\"", "'", ";", "--", "/*", "*/" };

        public ValidationResult ValidateName(string input)
        {
            var result = new ValidationResult();

            // Check empty
            if (string.IsNullOrWhiteSpace(input))
            {
                result.IsValid = false;
                result.ErrorMessage = "Name cannot be empty. Please enter a valid name.";
                return result;
            }

            // Check length
            if (input.Length < 2)
            {
                result.IsValid = false;
                result.ErrorMessage = "Name must be at least 2 characters long.";
                return result;
            }

            if (input.Length > 50)
            {
                result.IsValid = false;
                result.ErrorMessage = "Name is too long (maximum 50 characters).";
                return result;
            }

            // Check forbidden characters
            foreach (var ch in _forbiddenChars)
            {
                if (input.Contains(ch))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Name contains invalid characters. Please use only letters and spaces.";
                    return result;
                }
            }

            // Check letters only
            if (!input.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                result.IsValid = false;
                result.ErrorMessage = "Please enter a valid name using only letters.";
                return result;
            }

            result.IsValid = true;
            result.CleanedValue = input.Trim();
            return result;
        }

        public ValidationResult ValidateInput(string input)
        {
            var result = new ValidationResult();

            // Check empty
            if (string.IsNullOrWhiteSpace(input))
            {
                result.IsValid = false;
                result.ErrorMessage = "Input cannot be empty. Please enter a question or command.";
                return result;
            }

            // Check length
            if (input.Length > 500)
            {
                result.IsValid = false;
                result.ErrorMessage = "Input is too long (maximum 500 characters). Please be more concise.";
                return result;
            }

            // Check for dangerous commands (security)
            string lowerInput = input.ToLower();
            if (lowerInput.Contains("drop table") ||
              lowerInput.Contains("delete from") ||
              lowerInput.Contains("<script>") ||
              lowerInput.Contains("javascript:") ||
              lowerInput.Contains("onclick=") ||
              lowerInput.Contains("onerror="))
            {
                result.IsValid = false;
                result.ErrorMessage = "Potentially harmful input detected. QI's security protocols block suspicious queries. Please enter a valid cybersecurity question.";
                return result;
            }

            result.IsValid = true;
            result.CleanedValue = input.Trim();
            return result;
        }
    }
}       