using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
﻿using System.Diagnostics;

namespace FreddiChatClient {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {

        #region Private fields

        private const string applicationName = "FreddiChat";
        private static readonly Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string applicationVersionName = string.Format("v{0}.{1}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);
        private static readonly string applicationVersionVerboseName = string.Format("v{0}.{1} Patch {2} Build {3}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);

        private readonly Dispatcher dispatcher;

        private readonly Chats chats;

        private readonly List<string> messageHistory = new List<string> { string.Empty };

        private int messageHistoryIndex;

        private string respondToUser;

        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.UnhandledException += DispatcherUnhandledException;

            // Listen to chat events
            chats = new Chats(dispatcher);
            chats.ConnectionFailed += ConnectionFailedHandler;
            chats.Connected += ConnectedHandler;
            chats.UserConnected += UserConnectedHandler;
            chats.Disconnected += DisconnectedHandler;
            chats.UserDisconnected += UserDisconnectedHandler;
            chats.Broadcasted += BroadcastedHandler;
            chats.UserBroadcasted += UserBroadcastedHandler;
            chats.Whispered += WhisperedHandler;
            chats.UserWhispered += UserWhisperedHandler;

            UpdateTitle();
        }

        #region Connection event handlers

        private void ConnectionFailedHandler(object sender, DateTime dateTime, string message) {
            dispatcher.Invoke(() => AppendText(dateTime, message, Colors.Red));
            dispatcher.Invoke(() => UpdateTitle());
        }

        private void ConnectedHandler(object sender, bool result, DateTime dateTime, string message, string[] users) {
            dispatcher.Invoke(() => AppendText(dateTime, message, result ? Colors.OliveDrab : Colors.Red));
            if (!result) {
                return;
            }
            dispatcher.Invoke(() => UpdateTitle());
            dispatcher.Invoke(() => AddUsers(users));
            dispatcher.Invoke(() => AppendText(string.Format("There are currently {0} user(s) connected.", users.Length), Colors.OliveDrab));
        }

        private void DisconnectedHandler(object sender, bool result, DateTime dateTime, string message) {
            if (dispatcher.HasShutdownStarted) {
                return;
            }
            dispatcher.Invoke(() => AppendText(dateTime, message, result ? Colors.Orange : Colors.Red));
            dispatcher.Invoke(() => UpdateTitle());
            dispatcher.Invoke(() => RemoveUsers());
        }

        private void UserConnectedHandler(object sender, DateTime dateTime, string user, string message) {
            dispatcher.Invoke(() => AddUsers(user));
            dispatcher.Invoke(() => AppendText(dateTime, message, Colors.OliveDrab));
        }

        private void UserDisconnectedHandler(object sender, DateTime dateTime, string user, string message) {
            dispatcher.Invoke(() => RemoveUser(user));
            dispatcher.Invoke(() => AppendText(dateTime, message, Colors.Orange));
        }

        private void BroadcastedHandler(object sender, DateTime dateTime, string message) {
            // Just output what was sent
            dispatcher.Invoke(() => AppendText(dateTime, "You", "say", message));
        }

        private void UserBroadcastedHandler(object sender, DateTime dateTime, string user, string message) {
            dispatcher.Invoke(() => AppendText(dateTime, user, "says", message));
        }

        private void WhisperedHandler(object sender, DateTime dateTime, string user, string message) {
            // Just output what was sent
            dispatcher.Invoke(() => AppendText(dateTime, "You", string.Format("whisper to {0}", user), message, Colors.BlueViolet));
        }

        private void UserWhisperedHandler(object sender, DateTime dateTime, string user, string message) {
            // Update the user name to quick respond to through "/r "
            respondToUser = user;
            dispatcher.Invoke(() => AppendText(dateTime, user, "whispers", message, Colors.BlueViolet));
        }

        #endregion

        #region GUI event handlers

        private void DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            // Do nothing.
        }

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            Help();

            messageTextBox.Focus();
            messageTextBox.SelectAll();
        }

        private void WindowClosing(object sender, CancelEventArgs e) {
            if (chats.IsConnected) {
                chats.Disconnect();
            }

            // Abort pending work in the queue
            dispatcher.InvokeShutdown();
        }

        private void UserListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var listBoxItem = GetElementFromPoint(userListBox, e.GetPosition(userListBox)) as ListBoxItem;
            if (listBoxItem == null) {
                return;
            }

            if (listBoxItem.Content.Equals(chats.User)) {
                AppendText("There is no need to whisper to yourself.", Colors.Red);
                return;
            }

            messageTextBox.Text = string.Empty;
            messageTextBox.Text += string.Format("/w {0} ", listBoxItem.Content);
            messageTextBox.CaretIndex = messageTextBox.Text.Length;
            messageTextBox.Focus();
        }

        private void MessageTextBoxPreviewKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Up:
                    UpdateMessageTextFromHistory(1);
                    break;
                case Key.Down:
                    UpdateMessageTextFromHistory(-1);
                    break;
                case Key.Enter:
                    HandleMessageTextBoxInput();
                    break;
            }
        }

        private void ExitMenuItemClick(object sender, RoutedEventArgs e) {
            Close();
        }

        private void AboutMenuItemClick(object sender, RoutedEventArgs e) {
            var title = string.Format("About {0} {1}", applicationName, applicationVersionName);
            var message = string.Format("{0} {1}{2}{2}A simple chat client and server solution written in C# using WCF (client, server) and WPF (client) by FreddieBoi.",
                applicationName,
                applicationVersionVerboseName,
                Environment.NewLine);
            MessageBox.Show(message, title, MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MessageTextBoxTextChanged(object sender, TextChangedEventArgs e) {
            var text = messageTextBox.Text.ToLower();
            // Do we have a user to quick respond to? And should we?
            if (respondToUser != null && (text.Contains("/r ") || text.Contains("/respond "))) {
                var respondToString = string.Format("/w {0} ", respondToUser);
                messageTextBox.Text = messageTextBox.Text.Replace("/r ", respondToString).Replace("/R ", respondToString).Replace("/respond ", respondToString);
                messageTextBox.CaretIndex = messageTextBox.Text.Length;
            }
        }

        private void LinkRequestNavigate(object sender, RequestNavigateEventArgs e) {
            if (e.Uri != null) {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()));
            }
        }

        #endregion

        #region Private helpers

        #region Commands

        private void HandleCommandOrMessage(string message) {
            try {
                // Is it a command?
                if (message.StartsWith("/")) {
                    var command = message.Remove(0, 1).ToLower();

                    // Special handling of commands with arguments
                    if (command.StartsWith("w") || command.StartsWith("whipser")) {
                        Whisper(message);
                        return;
                    }
                    if (command.StartsWith("connect")) {
                        Connect(message);
                        return;
                    }

                    // Handle all commands without arguments
                    switch (command) {
                        case "clear":
                            dispatcher.Invoke(ClearText);
                            return;
                        case "disconnect":
                            Disconnect();
                            return;
                        case "?":
                        case "h":
                        case "help":
                            dispatcher.Invoke(Help);
                            return;
                        case "q":
                        case "quit":
                        case "exit":
                            dispatcher.Invoke(Close);
                            return;
                        default:
                            dispatcher.Invoke(() => AppendText("Invalid command.", Colors.Red));
                            goto case "help";
                    }
                }
            } catch {
                dispatcher.Invoke(() => AppendText("Invalid command syntax.", Colors.Red));
                dispatcher.Invoke(Help);
            }

            // Just regular broadcast chat...
            Broadcast(message);
        }

        private void Help() {
            AppendText(string.Format("{0} {1}", applicationName, applicationVersionVerboseName), Colors.CadetBlue);
            AppendText("General:\t/h[elp], /q[uit]", Colors.CadetBlue);
            AppendText("Connection:\t/connect user [protocol://]host[:port], /disconnect", Colors.CadetBlue);
            AppendText("Chat:\t/clear, /r[eply], /w[hisper] user message", Colors.CadetBlue);
        }

        private void Connect(string message) {
            try {
                // Split into 3 substrings on ' ' (space).
                // eg. "/connect User http://localhost:8080/FreddiChat" splits to "/connect", "User" and "http://localhost:8080/FreddiChat".
                var words = message.Split(new[] { ' ' }, 3);
                var user = words[1];
                var host = words[2];

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(host)) {
                    // Bad format
                    dispatcher.Invoke(() => AppendText("Invalid connect command, please use: /connect user [protocol://]host[:port]", Colors.Red));
                    return;
                }

                if (chats.IsConnected) {
                    dispatcher.Invoke(() => AppendText("Please disconnect before trying to connect again.", Colors.Red));
                    return;
                }

                // Send the whisper message.
                chats.Connect(user, host);

                dispatcher.Invoke(() => AppendText(string.Format("Connecting to {0} as {1}...", chats.Host, chats.User), Colors.CadetBlue));
            } catch {
                dispatcher.Invoke(() => AppendText("Invalid connect command, please use: /connect user [protocol://]host[:port]", Colors.Red));
            }
        }

        private void Disconnect() {
            if (!chats.IsConnected) {
                dispatcher.Invoke(() => AppendText("Please connect before trying to disconnect.", Colors.Red));
                return;
            }
            chats.Disconnect();
        }

        private void Broadcast(string message) {
            if (!chats.IsConnected) {
                dispatcher.Invoke(() => AppendText("Please connect before trying to broadcast.", Colors.Red));
                return;
            }
            try {
                // Broadcast instead
                chats.Broadcast(message);
            } catch (Exception e) {
                dispatcher.Invoke(() => AppendText(string.Format("Couldn't establish connection to server. {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""), Colors.Red));
            }
        }

        private void Whisper(string message) {
            try {
                // Split into 3 substrings on ' ' (space).
                // eg. "/w User This is a message" splits to "/w", "User" and "This is a message".
                var words = message.Split(new[] { ' ' }, 3);
                var toUser = words[1];
                var whisperMessage = words[2];

                if (string.IsNullOrEmpty(toUser) || string.IsNullOrEmpty(whisperMessage)) {
                    // Bad format
                    dispatcher.Invoke(() => AppendText("Invalid whisper command, please use: /w user message", Colors.Red));
                    return;
                }
                if (toUser.Equals(chats.User)) {
                    // The user is whispering to herself
                    dispatcher.Invoke(() => AppendText("There is no need to whisper to yourself.", Colors.Red));
                    return;
                }

                if (!chats.IsConnected) {
                    dispatcher.Invoke(() => AppendText("Please connect before trying to whisper.", Colors.Red));
                    return;
                }

                // Send the whisper message.
                chats.Whisper(toUser, whisperMessage);
            } catch {
                dispatcher.Invoke(() => AppendText("Invalid whisper command, please use: /w user message", Colors.Red));
            }
        }

        #endregion

        private void UpdateTitle() {
            Title = string.Format("{0} ({1})", applicationName, applicationVersionName);
            if (chats.IsConnected) {
                Title += string.Format(" - {0}@{1}", chats.User, chats.Host);
            }
        }

        private void HandleMessageTextBoxInput() {
            var message = messageTextBox.Text;
            messageHistory.Insert(1, message);
            messageHistoryIndex = 0;
            messageTextBox.Text = string.Empty;
            message = message.Trim();
            if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)) {
                AppendText("Please enter a valid command or message.", Colors.Red);
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate {
                HandleCommandOrMessage(message);
            });
        }

        /// <summary>
        /// Remove a users from the userListBox.
        /// </summary>
        private void RemoveUser(string user) {
            var itemToRemove = userListBox.Items.Cast<ListBoxItem>().FirstOrDefault(item => item.Content.Equals(user));
            if (itemToRemove == null) {
                return;
            }
            userListBox.Items.Remove(itemToRemove);
        }

        /// <summary>
        /// Remove all users from the userListBox.
        /// </summary>
        private void RemoveUsers() {
            var itemsToRemove = userListBox.Items.Cast<ListBoxItem>().ToList();
            foreach (var listBoxItem in itemsToRemove) {
                userListBox.Items.Remove(listBoxItem);
            }
        }

        /// <summary>
        /// Add users to the userListBox.
        /// </summary>
        private void AddUsers(params string[] users) {
            foreach (var user in users.Where(user => !userListBox.Items.Contains(user))) {
                userListBox.Items.Add(new ListBoxItem {
                    Content = user
                });
            }
        }

        private void ClearText() {
            chatTextBox.Document.Blocks.Clear();
        }

        private void AppendText(string text, Color color) {
            AppendText(DateTime.Now, text, color);
        }

        private void AppendText(DateTime dateTime, string text, Color color) {
            AppendText(dateTime, "System", null, text, color);
        }

        private void AppendText(DateTime dateTime, string sender, string senderInfo, string text) {
            AppendText(dateTime, sender, senderInfo, text, Colors.Black);
        }

        private void AppendText(DateTime dateTime, string sender, string senderInfo, string text, Color color) {
            // Create a textrange at the very end of the chat text box, extend the range with the new text
            var timestampTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = string.Format("[{0}] ", dateTime)
            };
            // Colorize the timestamp
            timestampTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            timestampTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            var senderTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = string.Format("{0}", sender)
            };
            // Colorize the sender and make it bold
            senderTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            senderTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            var senderInfoTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = string.IsNullOrWhiteSpace(senderInfo) ? ": " : string.Format(" {0}: ", senderInfo)
            };
            // Colorize the sender info
            senderInfoTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            senderInfoTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            // Try to extract URLs and make them clickable
            foreach (var partialText in UrlExtractor.Extract(text)) {
                if (partialText.Value != null) {
                    var linkTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                        Text = partialText.Key
                    };
                    var link = new Hyperlink(linkTextRange.Start, linkTextRange.End);
                    link.NavigateUri = partialText.Value;
                    link.RequestNavigate += LinkRequestNavigate;
                } else {
                    var regularTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                        Text = partialText.Key
                    };
                    // Colorize the text
                    regularTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
                    regularTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }
            }

            // Add a new line and reset styles
            var resetTextRange = new TextRange(chatTextBox.Document.ContentEnd, chatTextBox.Document.ContentEnd) {
                Text = Environment.NewLine
            };
            resetTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            // Scroll to end of chat text if ScrollLock isn't on
            var scrollLock = (((ushort)GetKeyState(0x91)) & 0xffff) != 0;
            if (!scrollLock) {
                chatTextBox.ScrollToEnd();
            }
        }

        private static object GetElementFromPoint(ItemsControl box, Point point) {
            var element = (UIElement)box.InputHitTest(point);
            while (true) {
                if (element == box) {
                    return null;
                }
                var item = box.ItemContainerGenerator.ItemFromContainer(element);
                var itemFound = !(item.Equals(DependencyProperty.UnsetValue));
                if (itemFound) {
                    return item;
                }
                element = (UIElement)VisualTreeHelper.GetParent(element);
            }
        }

        private void UpdateMessageTextFromHistory(int direction) {
            messageHistoryIndex += direction;

            // Wrap around to index 0, the string.Empty entry.
            if (Math.Abs(messageHistoryIndex) > messageHistory.Count - 1) {
                messageHistoryIndex = 0;
            }

            // Set the message to the history message (the message at |index|)
            // Using absolute will make both Up and Down keys work.
            messageTextBox.Text = messageHistory[Math.Abs(messageHistoryIndex)];
            messageTextBox.CaretIndex = messageTextBox.Text.Length;
        }

        #endregion

    }

}
