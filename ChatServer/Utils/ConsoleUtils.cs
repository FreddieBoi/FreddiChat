using System;
using System.Collections.Generic;
using FreddieChatServer.Managers;

namespace FreddieChatServer.Utils {

    public static class ConsoleUtils {

        /// <summary>
        /// Trace recieved service call.
        /// </summary>
        public static void TraceCall(string format, params object[] arg) {
            Console.WriteLine(string.Format("[{0}] Recieved {1}", DateTime.Now, format), arg);
        }

        /// <summary>
        /// Trace successful service call.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceCallSuccess(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.Green, user, format, arg);
        }

        /// <summary>
        /// Trace successful service call notification.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceNotificationSuccess(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.DarkGreen, user, format, arg);
        }

        /// <summary>
        /// Trace alert service call.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceCallWarning(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.Yellow, user, format, arg);
        }

        /// <summary>
        /// Trace alert service call notification.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceNotificationWarning(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.DarkYellow, user, format, arg);
        }

        /// <summary>
        /// Trace successful broadcast service call.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceCallBroadcast(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.Cyan, user, format, arg);
        }

        /// <summary>
        /// Trace successful broadcast service call notification.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceNotificationBroadcast(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.DarkCyan, user, format, arg);
        }

        /// <summary>
        /// Trace successful whipser service call.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceCallWhisper(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.Magenta, user, format, arg);
        }

        /// <summary>
        /// Trace successful whipser service call notification.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceNotificationWhipser(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.DarkMagenta, user, format, arg);
        }

        /// <summary>
        /// Trace failed service call.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceCallFailure(User user, string format, params object[] arg) {
            TraceSend(ConsoleColor.Red, user, format, arg);
        }


        /// <summary>
        /// Trace service system work.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceSystemWork(string format, params object[] arg) {
            TraceSystem(ConsoleColor.DarkGray, format, arg);
        }

        /// <summary>
        /// Trace service system work.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceSystemInfo(string format, params object[] arg) {
            TraceSystem(ConsoleColor.Green, format, arg);
        }

        /// <summary>
        /// Trace service system warning.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceSystemWarning(string format, params object[] arg) {
            TraceSystem(ConsoleColor.Yellow, format, arg);
        }

        /// <summary>
        /// Trace service system error.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public static void TraceSystemError(string format, params object[] arg) {
            TraceSystem(ConsoleColor.Red, format, arg);
        }

        /// <summary>
        /// Trace sending service call.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="user"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        private static void TraceSend(ConsoleColor color, User user, string format, params object[] arg) {
            Console.ForegroundColor = color;
            Console.WriteLine(string.Format("[{0}] Sending  {1} to {2}", DateTime.Now, format, user.Name ?? "Unknown"), arg);
            Console.ResetColor();
        }

        /// <summary>
        /// Trace service system work.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        private static void TraceSystem(ConsoleColor color, string format, params object[] arg) {
            Console.ForegroundColor = color;
            Console.WriteLine("[{0}] System   {1}", DateTime.Now, string.Format(format, arg));
            Console.ResetColor();
        }

        public static string ReadAny(string info) {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0}:", info);
            Console.Write("> ");
            string command = Console.ReadLine();

            Console.ResetColor();
            return command.ToLower();
        }

        public static string ReadNonEmpty(string info) {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0}:", info);
            Console.Write("> ");

            var command = string.Empty;
            while (string.IsNullOrWhiteSpace(command)) {
                command = Console.ReadLine();
            }

            Console.ResetColor();
            return command.ToLower();
        }

        public static string ReadCommand(string info, params string[] commands) {
            var commandList = new List<string>(commands);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0} ({1}):", info, string.Join(", ", commandList));
            Console.Write("> ");

            var command = string.Empty;
            while (!commandList.Contains(command)) {
                command = Console.ReadLine().ToLower();
            }

            Console.ResetColor();
            return command;
        }

    }

}
