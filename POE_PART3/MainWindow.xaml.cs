using QI.Data;
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

namespace QI
{
    public partial class MainWindow : Window
    {
        private const bool V = true;

        // ── Services ─────────────────────────────────────────────────────────
        private readonly ResponseEngine _responses = new ResponseEngine();
        private readonly InputValidator _validator = new InputValidator();
        private readonly NlpService _nlp = new NlpService();
        private readonly TaskService _taskSvc = new TaskService();
        private readonly QuizEngine _quiz = new QuizEngine();
        private readonly ActivityLogService _actLog = new ActivityLogService();

        // ── State ─────────────────────────────────────────────────────────────
        private string _userName = "Friend";
        private bool _chatStarted = false;

        // Quiz state
        private bool _quizRunning = false;
        private bool _awaitingNext = false;

        // Task state — remembers the last added task id for quick reminder follow-up
        private int? _pendingTaskId = null;

        // ── Startup ───────────────────────────────────────────────────────────
        public MainWindow() => InitializeComponent();

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialise database
            try { DatabaseHelper.EnsureDatabaseReady(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Placeholder text for inputs
            TaskTitleInput.Text = "Task title (e.g. Enable 2FA)";
            TaskTitleInput.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158));
            TaskTitleInput.GotFocus += PlaceholderClear;

            TaskDescInput.Text = "Description (optional)";
            TaskDescInput.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158));
            TaskDescInput.GotFocus += PlaceholderClear;

            ReminderDaysInput.Text = "e.g. 7";
            ReminderDaysInput.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158));
            ReminderDaysInput.GotFocus += PlaceholderClear;

            NameInput.Text = "Enter your name to begin...";
            NameInput.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158));
            NameInput.GotFocus += PlaceholderClear;

            AddBotBubble(
                "Welcome to QI — Qalos Intelligence!\n\n" +
                "I am here to help you stay safe online.\n" +
                "Please enter your name above and click Start Chat.");

            await Task.Run(() => TryPlayAudio("greeting.wav"));
            RefreshTaskPanel();
            RefreshLogPanel();
        }

        private void PlaceholderClear(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb &&
                tb.Foreground is SolidColorBrush b &&
                b.Color == Color.FromRgb(139, 148, 158))
            {
                tb.Text = "";
                tb.Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243));
            }
        }

        // ── Name / Session Start ──────────────────────────────────────────────
        private void NameInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) StartChat_Click(sender, e);
        }

        private void StartChat_Click(object sender, RoutedEventArgs e)
        {
            string raw = NameInput.Text.Trim();

            // Ignore if placeholder is still showing
            if (raw == "Enter your name to begin...") raw = "";

            var result = _validator.ValidateName(raw);
            if (!result.IsValid)
            {
                ShakeElement(NameInput);
                AddSystemMessage(result.ErrorMessage);
                return;
            }

            _userName = result.CleanedValue;
            _chatStarted = V;

            NameBar.Visibility = Visibility.Collapsed;
            InputBar.Visibility = Visibility.Visible;
            ChipsBar.Visibility = Visibility.Visible;

            _actLog.Record($"Session started by {_userName}");

            AddBotBubble(
                $"Great to meet you, {_userName}!\n\n" +
                "You can:\n" +
                "  • Ask cybersecurity questions\n" +
                "  • Add tasks: 'Add task — Enable 2FA'\n" +
                "  • Set reminders: 'Remind me in 3 days'\n" +
                "  • Start the quiz: 'start quiz'\n" +
                "  • Check your log: 'activity log'\n\n" +
                "Use the panel on the right or type anything!");

            RefreshLogPanel();
            MessageInput.Focus();
        }

        private void AddSystemMessage(object errorMessage)
        {
            throw new NotImplementedException();
        }

        // ── Send Message ──────────────────────────────────────────────────────
        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Send_Click(sender, e);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!_chatStarted) return;

            var result = _validator.ValidateInput(MessageInput.Text);
            if (!result.IsValid)
            {
                ShakeElement(MessageInput);
                AddSystemMessage(result.ErrorMessage);
                return;
            }

            string clean = result.CleanedValue;
            MessageInput.Clear();
            AddUserBubble(clean);

            // Special commands
            string lower = clean.ToLower().Trim();
            if (lower == "clear") { ChatPanel.Children.Clear(); return; }
            if (lower == "exit" || lower == "quit" || lower == "bye")
            {
                AddBotBubble($"Goodbye, {_userName}! Stay safe online!");
                _actLog.Record("Session ended");
                RefreshLogPanel();
                await Task.Run(() => TryPlayAudio("goodbye.wav"));
                return;
            }

            var typingId = AddTypingIndicator();
            await Task.Delay(600);
            RemoveTypingIndicator(typingId);

            // NLP routing
            var intent = _nlp.DetectIntent(clean);
            HandleIntent(intent, clean);
            RefreshLogPanel();
        }

        // ── NLP Intent Handler ────────────────────────────────────────────────
        private void HandleIntent(NlpService.Intent intent, string raw)
        {
            switch (intent)
            {
                case NlpService.Intent.AddTask:
                    HandleAddTaskFromChat(raw);
                    break;

                case NlpService.Intent.SetReminder:
                    HandleSetReminderFromChat(raw);
                    break;

                case NlpService.Intent.ViewTasks:
                    ShowTaskSummaryInChat();
                    break;

                case NlpService.Intent.CompleteTask:
                    AddBotBubble(
                        "To mark a task as complete, use the Tasks panel on the right\n" +
                        "and click the green Complete button next to the task.");
                    break;

                case NlpService.Intent.DeleteTask:
                    AddBotBubble(
                        "To delete a task, use the Tasks panel on the right\n" +
                        "and click the red Delete button next to the task.");
                    break;

                case NlpService.Intent.StartQuiz:
                    AddBotBubble("Starting the quiz! Check the Quiz tab on the right panel.");
                    _actLog.Record("Quiz accessed from chat");
                    break;

                case NlpService.Intent.ShowLog:
                    ShowLogSummaryInChat();
                    break;

                case NlpService.Intent.Greeting:
                    AddBotBubble($"Hello, {_userName}! What cybersecurity topic can I help you with?");
                    break;

                case NlpService.Intent.HowAreYou:
                    AddBotBubble("QI is operating at full efficiency! All systems active.\nHow are you feeling about your digital security today?");
                    break;

                case NlpService.Intent.Purpose:
                    AddBotBubble(
                        "I am QI — Qalos Intelligence.\n\n" +
                        "I provide cybersecurity guidance, help you manage tasks, quiz your knowledge, " +
                        "and log all actions. Type 'help' to see everything I can do!");
                    break;

                case NlpService.Intent.ThankYou:
                    AddBotBubble($"You're welcome, {_userName}! Staying informed is the first step to staying secure.");
                    break;

                case NlpService.Intent.CyberTopic:
                    AddBotBubble(_responses.GetResponse(raw.ToLower(), _userName));
                    _actLog.Record($"Cybersecurity topic queried: {raw}");
                    break;

                default:
                    AddBotBubble(_responses.GetResponse(raw.ToLower(), _userName));
                    break;
            }
        }

        // ── Task Handling from Chat ───────────────────────────────────────────
        private void HandleAddTaskFromChat(string raw)
        {
            string title = _nlp.ExtractTaskTitle(raw);
            int? days = _nlp.ExtractReminderDays(raw);

            if (string.IsNullOrWhiteSpace(title) || title.Length < 2)
            {
                AddBotBubble(
                    "I'd like to add a task for you! Could you give me more detail?\n" +
                    "For example: 'Add task — Enable two-factor authentication'");
                return;
            }

            try
            {
                int id = _taskSvc.AddTask(title, "", days);
                _pendingTaskId = id;
                _actLog.Record($"Task added: '{title}'" + (days.HasValue ? $" (reminder in {days} days)" : ""));
                RefreshTaskPanel();

                if (days.HasValue)
                {
                    AddBotBubble(
                        $"Task added: '{title}'\n" +
                        $"Reminder set for {DateTime.Now.AddDays(days.Value):dd MMM yyyy}.");
                }
                else
                {
                    AddBotBubble(
                        $"Task added: '{title}'\n\n" +
                        "Would you like a reminder? If so, say something like:\n" +
                        "'Remind me in 3 days'");
                }
            }
            catch (Exception ex)
            {
                AddBotBubble($"Could not save the task: {ex.Message}");
            }
        }

        private void HandleSetReminderFromChat(string raw)
        {
            int? days = _nlp.ExtractReminderDays(raw);

            if (!days.HasValue)
            {
                AddBotBubble("How many days would you like me to remind you? e.g. 'Remind me in 5 days'");
                return;
            }

            if (_pendingTaskId.HasValue)
            {
                try
                {
                    _taskSvc.SetReminder(_pendingTaskId.Value, days.Value);
                    _actLog.Record($"Reminder set for task #{_pendingTaskId} in {days} days");
                    _pendingTaskId = null;
                    RefreshTaskPanel();
                    AddBotBubble(
                        $"Reminder set for {DateTime.Now.AddDays(days.Value):dd MMM yyyy}.\n" +
                        "Check the Tasks panel to view your tasks.");
                }
                catch (Exception ex)
                {
                    AddBotBubble($"Could not set the reminder: {ex.Message}");
                }
            }
            else
            {
                AddBotBubble(
                    "I don't have a pending task to attach a reminder to.\n" +
                    "Add a task first, then tell me when to remind you.");
            }
        }

        private void ShowTaskSummaryInChat()
        {
            try
            {
                var tasks = _taskSvc.GetAllTasks();
                if (tasks.Count == 0)
                {
                    AddBotBubble("You have no tasks yet. Add one using the Tasks panel on the right, or say 'Add task — [task name]'.");
                    return;
                }

                var pending = tasks.Where(t => !t.IsCompleted).ToList();
                var completed = tasks.Where(t => t.IsCompleted).ToList();

                string msg = $"You have {pending.Count} pending task(s) and {completed.Count} completed task(s).\n\n";

                if (pending.Any())
                {
                    msg += "PENDING:\n";
                    foreach (var t in pending.Take(5))
                        msg += $"  • {t.Title} — {t.ReminderDisplay}\n";
                }

                msg += "\nView and manage all tasks in the Tasks panel on the right.";
                AddBotBubble(msg);
                _actLog.Record("Tasks summary viewed in chat");
            }
            catch (Exception ex)
            {
                AddBotBubble($"Could not load tasks: {ex.Message}");
            }
        }

        private void ShowLogSummaryInChat()
        {
            var entries = _actLog.GetRecent(8);
            if (!entries.Any())
            {
                AddBotBubble("No activity has been recorded yet.");
                return;
            }

            string msg = "Here is a summary of recent actions:\n\n";
            for (int i = 0; i < entries.Count; i++)
                msg += $"  {i + 1}. {entries[i].Display}\n";

            AddBotBubble(msg);
            _actLog.Record("Activity log viewed in chat");
        }

        // ── Tasks Panel ───────────────────────────────────────────────────────
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleInput.Text.Trim();
            string desc = TaskDescInput.Text.Trim();
            string daysStr = ReminderDaysInput.Text.Trim();

            // Ignore placeholder text
            if (title == "Task title (e.g. Enable 2FA)") title = "";
            if (desc == "Description (optional)") desc = "";
            if (daysStr == "e.g. 7") daysStr = "";

            if (string.IsNullOrWhiteSpace(title))
            {
                ShakeElement(TaskTitleInput);
                AddSystemMessage("Please enter a task title.");
                return;
            }

            int? days = null;
            if (!string.IsNullOrWhiteSpace(daysStr) && int.TryParse(daysStr, out int d) && d > 0)
                days = d;

            try
            {
                int id = _taskSvc.AddTask(title, desc, days);
                _pendingTaskId = id;
                _actLog.Record($"Task added via panel: '{title}'" + (days.HasValue ? $" (reminder in {days} days)" : ""));

                // Clear inputs
                TaskTitleInput.Text = "";
                TaskDescInput.Text = "";
                ReminderDaysInput.Text = "";

                RefreshTaskPanel();

                if (_chatStarted)
                    AddBotBubble(
                        $"Task added: '{title}'\n" +
                        (days.HasValue
                            ? $"Reminder set for {DateTime.Now.AddDays(days.Value):dd MMM yyyy}."
                            : "No reminder set. Say 'Remind me in X days' to add one."));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not add task: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshTaskPanel()
        {
            TaskListPanel.Children.Clear();

            List<TaskItem> tasks;
            try { tasks = _taskSvc.GetAllTasks(); }
            catch { return; }

            if (!tasks.Any())
            {
                TaskListPanel.Children.Add(new TextBlock
                {
                    Text = "No tasks yet. Add one above!",
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return;
            }

            foreach (var task in tasks)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                    BorderBrush = new SolidColorBrush(
                        task.IsCompleted
                            ? Color.FromRgb(26, 92, 46)
                            : Color.FromRgb(33, 38, 45)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var inner = new StackPanel();

                // Title row
                var titleRow = new Grid();
                titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleTb = new TextBlock
                {
                    Text = task.Title,
                    Foreground = new SolidColorBrush(
                        task.IsCompleted
                            ? Color.FromRgb(63, 185, 80)
                            : Color.FromRgb(230, 237, 243)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
                };
                Grid.SetColumn(titleTb, 0);
                titleRow.Children.Add(titleTb);

                var statusBadge = new Border
                {
                    Background = new SolidColorBrush(
                        task.IsCompleted ? Color.FromRgb(26, 92, 46) : Color.FromRgb(30, 50, 80)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6, 2, 6, 2),
                    Child = new TextBlock
                    {
                        Text = task.StatusDisplay,
                        Foreground = new SolidColorBrush(
                            task.IsCompleted ? Color.FromRgb(63, 185, 80) : Color.FromRgb(88, 166, 255)),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 10
                    }
                };
                Grid.SetColumn(statusBadge, 1);
                titleRow.Children.Add(statusBadge);
                inner.Children.Add(titleRow);

                // Description
                if (!string.IsNullOrWhiteSpace(task.Description))
                    inner.Children.Add(new TextBlock
                    {
                        Text = task.Description,
                        Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 0)
                    });

                // Reminder
                inner.Children.Add(new TextBlock
                {
                    Text = $"Reminder: {task.ReminderDisplay}",
                    Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Margin = new Thickness(0, 4, 0, 6)
                });

                // Buttons
                if (!task.IsCompleted)
                {
                    var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
                    int tid = task.Id;

                    var completeBtn = new Button
                    {
                        Content = "Complete",
                        Style = FindResource("BtnSuccess") as Style,
                        Padding = new Thickness(10, 4, 10, 4),
                        FontSize = 11,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    completeBtn.Click += (s, ev) => OnCompleteTask(tid);

                    var deleteBtn = new Button
                    {
                        Content = "Delete",
                        Style = FindResource("BtnDanger") as Style,
                        Padding = new Thickness(10, 4, 10, 4),
                        FontSize = 11
                    };
                    deleteBtn.Click += (s, ev) => OnDeleteTask(tid, task.Title);

                    btnRow.Children.Add(completeBtn);
                    btnRow.Children.Add(deleteBtn);
                    inner.Children.Add(btnRow);
                }

                card.Child = inner;
                TaskListPanel.Children.Add(card);
            }
        }

        private void OnCompleteTask(int id)
        {
            try
            {
                var tasks = _taskSvc.GetAllTasks();
                var t = tasks.FirstOrDefault(x => x.Id == id);
                _taskSvc.CompleteTask(id);
                _actLog.Record($"Task completed: '{t?.Title ?? id.ToString()}'");
                RefreshTaskPanel();
                RefreshLogPanel();
                if (_chatStarted) AddBotBubble($"Task marked as complete.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not complete task: " + ex.Message);
            }
        }

        private void OnDeleteTask(int id, string title)
        {
            if (MessageBox.Show($"Delete task '{title}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _taskSvc.DeleteTask(id);
                    _actLog.Record($"Task deleted: '{title}'");
                    RefreshTaskPanel();
                    RefreshLogPanel();
                    if (_chatStarted) AddBotBubble($"Task '{title}' has been deleted.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not delete task: " + ex.Message);
                }
            }
        }

        // ── Quiz Panel ────────────────────────────────────────────────────────
        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quiz.Reset();
            _quizRunning = true;
            _awaitingNext = false;
            _actLog.Record("Quiz started");
            StartQuizBtn.Content = "Restart";
            QuizFeedbackBorder.Visibility = Visibility.Collapsed;
            NextQuestionBtn.Visibility = Visibility.Collapsed;
            ShowCurrentQuestion();

            if (_chatStarted)
                AddBotBubble("Quiz started! Answer the questions in the Quiz tab.");
        }

        private void ShowCurrentQuestion()
        {
            QuizQuestionPanel.Children.Clear();
            QuizFeedbackBorder.Visibility = Visibility.Collapsed;
            NextQuestionBtn.Visibility = Visibility.Collapsed;
            _awaitingNext = false;

            var q = _quiz.GetCurrentQuestion();
            if (q == null)
            {
                // Finished
                QuizProgressText.Text = $"Score: {_quiz.Score}/{_quiz.TotalQuestions}";
                QuizQuestionPanel.Children.Add(new TextBlock
                {
                    Text = _quiz.GetFinalMessage(),
                    Foreground = new SolidColorBrush(Color.FromRgb(63, 185, 80)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                _actLog.Record($"Quiz completed — Score: {_quiz.Score}/{_quiz.TotalQuestions}");
                RefreshLogPanel();
                if (_chatStarted)
                    AddBotBubble($"Quiz finished! {_quiz.GetFinalMessage()}");
                return;
            }

            QuizProgressText.Text = $"Question {_quiz.CurrentQuestionNumber} of {_quiz.TotalQuestions}";

            // Question text
            QuizQuestionPanel.Children.Add(new TextBlock
            {
                Text = q.Question,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            });

            // Options
            for (int i = 0; i < q.Options.Count; i++)
            {
                int idx = i;
                string prefix = q.Type == QuestionType.TrueFalse ? "" : $"{(char)('A' + i)}) ";

                var optBtn = new Button
                {
                    Content = prefix + q.Options[i],
                    Style = FindResource("BtnPrimary") as Style,
                    Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    FontSize = 12
                };
                optBtn.Click += (s, ev) => OnAnswerSelected(idx, s as Button);
                QuizQuestionPanel.Children.Add(optBtn);
            }
        }

        private void OnAnswerSelected(int chosenIndex, Button clickedBtn)
        {
            if (_awaitingNext) return;
            _awaitingNext = true;

            var q = _quiz.GetCurrentQuestion();
            bool correct = _quiz.SubmitAnswer(chosenIndex);

            // Colour the buttons
            int idx = 0;
            foreach (var child in QuizQuestionPanel.Children)
            {
                if (child is Button b && b != QuizQuestionPanel.Children[0])
                {
                    if (idx == q.CorrectIndex)
                        b.Background = new SolidColorBrush(Color.FromRgb(26, 92, 46));
                    else if (b == clickedBtn && !correct)
                        b.Background = new SolidColorBrush(Color.FromRgb(110, 26, 26));
                    b.IsEnabled = false;
                    idx++;
                }
            }

            // Feedback
            QuizFeedbackText.Text = correct
                ? $"Correct!\n{q.Explanation}"
                : $"Incorrect. The correct answer was: {q.Options[q.CorrectIndex]}\n{q.Explanation}";

            QuizFeedbackText.Foreground = new SolidColorBrush(
                correct ? Color.FromRgb(63, 185, 80) : Color.FromRgb(248, 81, 73));

            QuizFeedbackBorder.Visibility = Visibility.Visible;

            NextQuestionBtn.Content = _quiz.IsFinished ? "See Results" : "Next";
            NextQuestionBtn.Visibility = Visibility.Visible;
        }

        private void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            ShowCurrentQuestion();
        }

        // ── Activity Log Panel ────────────────────────────────────────────────
        private void RefreshLog_Click(object sender, RoutedEventArgs e) => RefreshLogPanel();

        private void RefreshLogPanel()
        {
            LogPanel.Children.Clear();
            var entries = _actLog.GetRecent(10);
            LogCountText.Text = $" ({_actLog.TotalCount} total)";

            if (!entries.Any())
            {
                LogPanel.Children.Add(new TextBlock
                {
                    Text = "No actions recorded yet.",
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Margin = new Thickness(0, 6, 0, 0)
                });
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 7, 10, 7),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = $"{i + 1}. {entry.Description}",
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
                sp.Children.Add(new TextBlock
                {
                    Text = entry.Timestamp.ToString("dd MMM yyyy HH:mm"),
                    Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Margin = new Thickness(0, 2, 0, 0)
                });

                row.Child = sp;
                LogPanel.Children.Add(row);
            }
        }

        // ── Chip Click ────────────────────────────────────────────────────────
        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (!_chatStarted) return;
            var btn = sender as Button;
            if (btn == null) return;
            MessageInput.Text = btn.Content.ToString();
            Send_Click(sender, e);
        }

        // ── Chat Bubble Helpers ───────────────────────────────────────────────
        private void AddUserBubble(string text)
        {
            var bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                CornerRadius = new CornerRadius(16, 16, 4, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 500,
                Margin = new Thickness(0, 4, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                }
            };
            var wrapper = new Grid { Margin = new Thickness(60, 0, 0, 0) };
            wrapper.Children.Add(bubble);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
        }

        private void AddBotBubble(string text)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 4)
            };

            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                CornerRadius = new CornerRadius(10),
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = "QI",
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 520,
                BorderBrush = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                }
            });

            var wrapper = new Grid { Margin = new Thickness(0, 0, 60, 0) };
            wrapper.Children.Add(panel);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
        }

        private void AddSystemMessage(string text)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(248, 81, 73)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4),
                TextWrapping = TextWrapping.Wrap
            });
            ScrollToBottom();
        }

        // ── Typing Indicator ──────────────────────────────────────────────────
        private string AddTypingIndicator()
        {
            string id = Guid.NewGuid().ToString();
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4), Tag = id };
            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                CornerRadius = new CornerRadius(10),
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = "QI",
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    Margin = new Thickness(3, 0, 3, 0)
                };
                var anim = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(500))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromMilliseconds(i * 160)
                };
                dot.BeginAnimation(UIElement.OpacityProperty, anim);
                dots.Children.Add(dot);
            }

            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(14, 12, 14, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                BorderThickness = new Thickness(1),
                Child = dots
            });

            var wrapper = new Grid { Margin = new Thickness(0, 0, 60, 0), Tag = id };
            wrapper.Children.Add(panel);
            ChatPanel.Children.Add(wrapper);
            ScrollToBottom();
            return id;
        }

        private void RemoveTypingIndicator(string id)
        {
            var el = ChatPanel.Children.OfType<Grid>()
                               .FirstOrDefault(g => g.Tag?.ToString() == id);
            if (el != null) ChatPanel.Children.Remove(el);
        }

        // ── Utilities ─────────────────────────────────────────────────────────
        private void ScrollToBottom()
        {
            ChatScroller.UpdateLayout();
            ChatScroller.ScrollToBottom();
        }

        private void ShakeElement(UIElement el)
        {
            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
            var tf = new TranslateTransform();
            el.RenderTransform = tf;
            tf.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        private void TryPlayAudio(string fileName)
        {
            try
            {
                string[] paths = { fileName,
                    System.IO.Path.Combine("Audio",     fileName),
                    System.IO.Path.Combine("Resources", fileName),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName) };
                string found = paths.FirstOrDefault(File.Exists);
                if (found == null) return;
                using (var player = new SoundPlayer(found)) player.PlaySync();
            }
            catch { }
        }
    }
}
