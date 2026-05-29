using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace QalosIntelligence
{
    public partial class MainWindow : Window
    {
        // ── Fields ───────────────────────────────────────────────────────────
        private readonly ResponseEngine  _responses  = new ResponseEngine();
        private readonly InputValidator  _validator  = new InputValidator();
        private string _userName = "Friend";
        private bool   _chatStarted = false;

        // ── Startup ──────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Show welcome prompt in the name bar
            NameInput.Text = "";

            AddBotBubble(
                "Welcome to QI — Qalos Intelligence!\n\n" +
                "I am here to help you stay safe online. " +
                "Before we begin, please enter your name above and click Start Chat.",
                isIntro: true);

            // Play greeting audio in background so UI stays responsive
            await Task.Run(() => TryPlayAudio("greeting.wav"));
        }

        // ── Name Entry ───────────────────────────────────────────────────────
        private void NameInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) StartChat_Click(sender, e);
        }

        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            ValidationResult result = _validator.ValidateName(NameInput.Text);

            if (!result.IsValid)
            {
                ShakeElement(NameInput);
                AddSystemMessage(result.ErrorMessage);
                return;
            }

            _userName    = result.CleanedValue;
            _chatStarted = true;

            // Hide name bar, show chat input and chips
            NameBar.Visibility   = Visibility.Collapsed;
            InputBar.Visibility  = Visibility.Visible;
            ChipsBar.Visibility  = Visibility.Visible;

            AddBotBubble(
                $"Great to meet you, {_userName}!\n\n" +
                "You can ask me about cybersecurity topics like password safety, phishing, " +
                "safe browsing, privacy, and malware.\n\n" +
                "Use the quick buttons below or type your own question.");

            MessageInput.Focus();
        }

        // ── Sending Messages ─────────────────────────────────────────────────
        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Send_Click(sender, e);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!_chatStarted) return;

            string raw = MessageInput.Text;
            ValidationResult result = _validator.ValidateInput(raw);

            if (!result.IsValid)
            {
                ShakeElement(MessageInput);
                AddSystemMessage(result.ErrorMessage);
                return;
            }

            string clean = result.CleanedValue;
            MessageInput.Clear();

            // Show user bubble
            AddUserBubble(clean);

            // Handle built-in commands
            string lower = clean.ToLower().Trim();

            if (lower == "exit" || lower == "quit" || lower == "bye")
            {
                AddBotBubble(
                    $"Goodbye, {_userName}!\n\n" +
                    "Remember: in the digital world, awareness is your greatest defence.\n" +
                    "Stay curious, stay secure!");
                await Task.Run(() => TryPlayAudio("goodbye.wav"));
                return;
            }

            if (lower == "clear")
            {
                ChatPanel.Children.Clear();
                return;
            }

            // Show typing indicator then respond
            var typingId = AddTypingIndicator();
            await Task.Delay(700);
            RemoveTypingIndicator(typingId);

            string response = _responses.GetResponse(lower, _userName);
            AddBotBubble(response);
        }

        // Quick topic chip clicked
        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (!_chatStarted) return;
            if (sender is Button btn)
            {
                MessageInput.Text = btn.Content.ToString();
                Send_Click(sender, e);
            }
        }

        // ── Bubble Builders ──────────────────────────────────────────────────

        private void AddUserBubble(string text)
        {
            var bubble = new Border
            {
                Background       = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                CornerRadius     = new CornerRadius(16, 16, 4, 16),
                Padding          = new Thickness(14, 10, 14, 10),
                MaxWidth         = 560,
                Margin           = new Thickness(0, 4, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            bubble.Child = new TextBlock
            {
                Text           = text,
                Foreground     = Brushes.White,
                FontFamily     = new FontFamily("Segoe UI"),
                FontSize       = 14,
                TextWrapping   = TextWrapping.Wrap,
                LineHeight     = 22
            };

            var wrapper = new Grid { Margin = new Thickness(80, 0, 0, 0) };
            wrapper.Children.Add(bubble);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
        }

        private void AddBotBubble(string text, bool isIntro = false)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 4, 0, 4)
            };

            // QI avatar dot
            panel.Children.Add(new Border
            {
                Background          = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                CornerRadius        = new CornerRadius(10),
                Width               = 28,
                Height              = 28,
                Margin              = new Thickness(0, 0, 10, 0),
                VerticalAlignment   = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text                = "QI",
                    Foreground          = Brushes.White,
                    FontFamily          = new FontFamily("Consolas"),
                    FontSize            = 10,
                    FontWeight          = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                }
            });

            var bubble = new Border
            {
                Background   = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding      = new Thickness(14, 10, 14, 10),
                MaxWidth     = 580,
                BorderBrush  = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                BorderThickness = new Thickness(1)
            };

            bubble.Child = new TextBlock
            {
                Text         = text,
                Foreground   = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                FontFamily   = new FontFamily("Consolas"),
                FontSize     = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight   = 22
            };

            panel.Children.Add(bubble);

            var wrapper = new Grid { Margin = new Thickness(0, 0, 80, 0) };
            wrapper.Children.Add(panel);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
        }

        private void AddSystemMessage(string text)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text                = text,
                Foreground          = new SolidColorBrush(Color.FromRgb(248, 81, 73)),
                FontFamily          = new FontFamily("Segoe UI"),
                FontSize            = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 4, 0, 4),
                TextWrapping        = TextWrapping.Wrap
            });
            ScrollToBottom();
        }

        // ── Typing Indicator ─────────────────────────────────────────────────
        private string? _typingId = null;

        private string AddTypingIndicator()
        {
            string id = Guid.NewGuid().ToString();

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 4, 0, 4),
                Tag         = id
            };

            panel.Children.Add(new Border
            {
                Background        = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                CornerRadius      = new CornerRadius(10),
                Width             = 28,
                Height            = 28,
                Margin            = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text                = "QI",
                    Foreground          = Brushes.White,
                    FontFamily          = new FontFamily("Consolas"),
                    FontSize            = 10,
                    FontWeight          = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                }
            });

            var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width   = 7, Height = 7,
                    Fill    = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    Margin  = new Thickness(3, 0, 3, 0)
                };

                // Animate each dot with a slight delay
                var anim = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(500))
                {
                    AutoReverse    = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime      = TimeSpan.FromMilliseconds(i * 160)
                };
                dot.BeginAnimation(UIElement.OpacityProperty, anim);
                dots.Children.Add(dot);
            }

            var bubble = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                CornerRadius    = new CornerRadius(4, 16, 16, 16),
                Padding         = new Thickness(14, 12, 14, 12),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                BorderThickness = new Thickness(1),
                Child           = dots
            };

            panel.Children.Add(bubble);

            var wrapper = new Grid { Margin = new Thickness(0, 0, 80, 0), Tag = id };
            wrapper.Children.Add(panel);
            ChatPanel.Children.Add(wrapper);
            _typingId = id;
            ScrollToBottom();
            return id;
        }

        private void RemoveTypingIndicator(string id)
        {
            var toRemove = ChatPanel.Children
                .OfType<Grid>()
                .FirstOrDefault(g => g.Tag?.ToString() == id);
            if (toRemove != null)
                ChatPanel.Children.Remove(toRemove);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void ScrollToBottom()
        {
            ChatScroller.UpdateLayout();
            ChatScroller.ScrollToBottom();
        }

        private void ShakeElement(UIElement element)
        {
            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(-8,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(8,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(-6,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(6,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));

            var transform = new TranslateTransform();
            element.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        private void TryPlayAudio(string fileName)
        {
            try
            {
                string[] paths =
                {
                    fileName,
                    System.IO.Path.Combine("Audio",     fileName),
                    System.IO.Path.Combine("Resources", fileName),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)
                };

                string? found = paths.FirstOrDefault(File.Exists);
                if (found == null) return;

                using (var player = new SoundPlayer(found))
                    player.PlaySync();
            }
            catch { /* Audio failure is non-critical */ }
        }
    }


    // ==================== RESPONSE ENGINE ====================
    public class ResponseEngine
    {
        private readonly Dictionary<string, string>            _exactResponses;
        private readonly List<(string Keyword, string Response)> _keywordRules;
        private readonly List<string>                          _defaultResponses;
        private readonly Random _random = new Random();

        public ResponseEngine()
        {
            _exactResponses   = BuildExactResponses();
            _keywordRules     = BuildKeywordRules();
            _defaultResponses = BuildDefaultResponses();
        }

        public string GetResponse(string input, string userName)
        {
            if (string.IsNullOrWhiteSpace(input))
                return GetDefaultResponse(userName);

            if (_exactResponses.TryGetValue(input, out string? exact))
                return exact;

            foreach (var (keyword, response) in _keywordRules)
                if (input.Contains(keyword))
                    return response;

            return GetDefaultResponse(userName);
        }

        private Dictionary<string, string> BuildExactResponses()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["how are you"] =
                    "QI is operating at full efficiency!\n" +
                    "All systems are active and ready to help you stay safe online.\n" +
                    "How are you feeling about your digital security today?",

                ["how are you?"] =
                    "QI is operating at full efficiency!\n" +
                    "All systems are active and ready to help you stay safe online.\n" +
                    "How are you feeling about your digital security today?",

                ["what is your purpose"] =
                    "I am QI - Qalos Intelligence.\n\n" +
                    "My purpose is to educate and assist you in maintaining strong cybersecurity practices.\n" +
                    "I cover password safety, phishing prevention, safe browsing, privacy, and malware.\n\n" +
                    "Think of me as your personal digital security consultant!",

                ["what's your purpose"] =
                    "I am QI - Qalos Intelligence.\n\n" +
                    "My purpose is to educate and assist you in maintaining strong cybersecurity practices.\n" +
                    "I cover password safety, phishing prevention, safe browsing, privacy, and malware.\n\n" +
                    "Think of me as your personal digital security consultant!",

                ["what can i ask you"] =
                    "You can ask QI about these cybersecurity topics:\n\n" +
                    "  1. password safety   - Creating and managing strong passwords\n" +
                    "  2. phishing          - Identifying and avoiding social engineering\n" +
                    "  3. safe browsing     - Navigating the web without risk\n" +
                    "  4. privacy           - Protecting your personal data\n" +
                    "  5. malware           - Understanding and preventing malicious software\n\n" +
                    "Type any topic name or use the quick buttons below!",

                ["help"] =
                    "AVAILABLE TOPICS:\n\n" +
                    "  password safety   - Password creation and management\n" +
                    "  phishing          - Recognise and avoid digital scams\n" +
                    "  safe browsing     - Navigate the web securely\n" +
                    "  privacy           - Protect your personal information\n" +
                    "  malware           - Understand and prevent malicious software\n\n" +
                    "COMMANDS:\n\n" +
                    "  help              - Show this menu\n" +
                    "  clear             - Clear the chat\n" +
                    "  exit              - End the session"
            };
        }

        private List<(string, string)> BuildKeywordRules()
        {
            return new List<(string, string)>
            {
                ("password safety", ResponseLibrary.PasswordSafety),
                ("password",        ResponseLibrary.PasswordSafety),
                ("passwords",       ResponseLibrary.PasswordSafety),

                ("phishing",        ResponseLibrary.Phishing),
                ("scam email",      ResponseLibrary.Phishing),
                ("fake email",      ResponseLibrary.Phishing),

                ("safe browsing",   ResponseLibrary.SafeBrowsing),
                ("browsing",        ResponseLibrary.SafeBrowsing),
                ("browser",         ResponseLibrary.SafeBrowsing),
                ("https",           ResponseLibrary.SafeBrowsing),

                ("privacy",         ResponseLibrary.Privacy),
                ("personal data",   ResponseLibrary.Privacy),
                ("popia",           ResponseLibrary.Privacy),

                ("malware",         ResponseLibrary.Malware),
                ("ransomware",      ResponseLibrary.Malware),
                ("virus",           ResponseLibrary.Malware),
                ("spyware",         ResponseLibrary.Malware),
                ("trojan",          ResponseLibrary.Malware),

                ("how are you",     "QI is running at full efficiency! How can I help you today?"),
                ("your purpose",    "I am QI - Qalos Intelligence, your personal cybersecurity guide!"),
                ("what do you do",  "I help you stay safe online by answering cybersecurity questions."),
                ("thank",           "You're welcome! Staying informed is the first step to staying secure."),
                ("hello",           "Hello! Great to hear from you. Ask me anything about cybersecurity!"),
                ("hi ",             "Hi there! What cybersecurity topic can I help you with today?"),
            };
        }

        private List<string> BuildDefaultResponses()
        {
            return new List<string>
            {
                "I'm not sure I understand. Can you try rephrasing?\nUse the quick buttons below or type 'help' to see available topics.",
                "I didn't quite catch that. Could you rephrase?\nTry typing a topic like 'phishing' or 'password safety'.",
                "That's outside my current knowledge base. I specialise in cybersecurity.\nType 'help' for a full list of what I can assist with.",
                "Hmm, I'm not sure how to answer that.\nTry asking about passwords, phishing, safe browsing, privacy, or malware.",
                "I didn't recognise that input. Type 'help' to see all available topics."
            };
        }

        private string GetDefaultResponse(string userName)
        {
            string response = _defaultResponses[_random.Next(_defaultResponses.Count)];
            return $"{response}\n\n(Reminder: you can always type 'help', {userName}.)";
        }
    }


    // ==================== RESPONSE LIBRARY ====================
    public static class ResponseLibrary
    {
        public const string PasswordSafety =
            "PASSWORD SAFETY GUIDE\n\n" +
            "CREATING STRONG PASSWORDS:\n" +
            "  - Use at least 12 characters (16+ recommended)\n" +
            "  - Mix uppercase, lowercase, numbers, and symbols\n" +
            "  - Avoid names, birthdays, or common dictionary words\n" +
            "  - Never reuse the same password on different accounts\n\n" +
            "MANAGING PASSWORDS SECURELY:\n" +
            "  - Use a password manager: Bitwarden, 1Password, or KeePass\n" +
            "  - Enable Two-Factor Authentication (2FA) on every account\n" +
            "  - Change passwords immediately if a breach is suspected\n" +
            "  - Never share passwords via email, chat, or phone\n\n" +
            "REMINDER: Your password is the key to your digital life. Protect it!";

        public const string Phishing =
            "PHISHING DETECTION GUIDE\n\n" +
            "WARNING SIGNS TO LOOK FOR:\n" +
            "  - Urgency tactics: 'Your account will be closed in 24 hours!'\n" +
            "  - Suspicious sender address: support@amaz0n-security.net\n" +
            "  - Generic greetings: 'Dear Customer' instead of your real name\n" +
            "  - Requests for passwords or banking info via email\n" +
            "  - Unexpected attachments (.exe, .zip, .docm)\n\n" +
            "HOW TO STAY SAFE:\n" +
            "  - Hover over links to preview the real URL before clicking\n" +
            "  - Go directly to the organisation's official website instead\n" +
            "  - Never enter credentials on a page reached through an email link\n" +
            "  - Report suspicious emails to your IT or security team\n\n" +
            "ADVICE: When in doubt, verify through a separate trusted channel!";

        public const string SafeBrowsing =
            "SAFE BROWSING GUIDE\n\n" +
            "BEFORE YOU BROWSE:\n" +
            "  - Keep your browser and OS updated at all times\n" +
            "  - Install a reputable ad-blocker (e.g., uBlock Origin)\n" +
            "  - Remove browser extensions you no longer use\n\n" +
            "WHILE BROWSING:\n" +
            "  - Always check for HTTPS (padlock icon) before entering personal data\n" +
            "  - Use private/incognito mode on shared or public computers\n" +
            "  - Never save passwords or financial data on public devices\n" +
            "  - Download software only from official, verified sources\n\n" +
            "AFTER BROWSING:\n" +
            "  - Clear cookies, cache, and history on shared devices\n\n" +
            "REMINDER: Your browser is your gateway to the internet — secure it!";

        public const string Privacy =
            "PRIVACY PROTECTION GUIDE\n\n" +
            "MANAGING YOUR DIGITAL FOOTPRINT:\n" +
            "  - Review social media privacy settings every few months\n" +
            "  - Limit what you share online: location, phone number, date of birth\n" +
            "  - Read app privacy policies before granting permissions\n" +
            "  - Audit app permissions on your phone regularly\n\n" +
            "TOOLS TO PROTECT YOUR PRIVACY:\n" +
            "  - Use a VPN on public Wi-Fi to encrypt your connection\n" +
            "  - Enable 'Do Not Track' in your browser settings\n" +
            "  - Use encrypted messaging apps: Signal or WhatsApp\n" +
            "  - Switch to a privacy-first search engine: DuckDuckGo\n\n" +
            "South African users: POPIA governs how your personal data must be handled.\n\n" +
            "PRINCIPLE: Your personal data is valuable. Guard it carefully!";

        public const string Malware =
            "MALWARE THREAT GUIDE\n\n" +
            "COMMON TYPES OF MALWARE:\n" +
            "  - Viruses      : Self-replicating code that corrupts files and systems\n" +
            "  - Ransomware   : Locks your files and demands payment to restore them\n" +
            "  - Spyware      : Silently monitors your activity and steals information\n" +
            "  - Trojans      : Malicious software disguised as something legitimate\n" +
            "  - Adware       : Floods your device with unwanted ads and pop-ups\n\n" +
            "HOW TO PROTECT YOURSELF:\n" +
            "  - Install reputable antivirus software (Bitdefender, Windows Defender)\n" +
            "  - Keep your operating system and all applications updated\n" +
            "  - Never open email attachments from unknown or untrusted senders\n" +
            "  - Scan all USB drives and external storage before opening files\n" +
            "  - Follow the 3-2-1 backup rule:\n" +
            "      3 copies, on 2 different media types, with 1 stored offsite\n\n" +
            "MAXIM: Prevention is far better than dealing with an infection!";
    }


    // ==================== INPUT VALIDATOR ====================
    public class InputValidator
    {
        private const int MaxInputLength = 500;
        private const int MaxNameLength  = 50;
        private const int MinNameLength  = 2;

        private static readonly string[] BlockedPatterns =
        {
            "drop table", "delete from", "<script>",
            "javascript:", "onclick=", "onerror="
        };

        private static readonly string[] ForbiddenNameChars =
        {
            "<", ">", "\"", ";", "--", "/*", "*/"
        };

        public ValidationResult ValidateName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Fail("Name cannot be empty. Please enter your name.");

            string trimmed = input.Trim();

            if (trimmed.Length < MinNameLength)
                return Fail($"Name must be at least {MinNameLength} characters.");

            if (trimmed.Length > MaxNameLength)
                return Fail($"Name is too long (maximum {MaxNameLength} characters).");

            if (ForbiddenNameChars.Any(ch => trimmed.Contains(ch)))
                return Fail("Name contains invalid characters. Please use letters only.");

            if (!trimmed.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                return Fail("Please enter a valid name using letters only.");

            return Pass(trimmed);
        }

        public ValidationResult ValidateInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Fail("Please type a message before sending.");

            if (input.Length > MaxInputLength)
                return Fail($"Message is too long (maximum {MaxInputLength} characters).");

            string lower = input.ToLower();
            if (BlockedPatterns.Any(p => lower.Contains(p)))
                return Fail("Potentially harmful input detected. Please enter a valid question.");

            return Pass(input.Trim());
        }

        private static ValidationResult Pass(string value) =>
            new ValidationResult { IsValid = true, CleanedValue = value };

        private static ValidationResult Fail(string message) =>
            new ValidationResult { IsValid = false, ErrorMessage = message };
    }


    // ==================== VALIDATION RESULT ====================
    public class ValidationResult
    {
        public bool   IsValid      { get; set; }
        public string CleanedValue { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
