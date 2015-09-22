using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using FreddieChatServer.Communications;
using FreddieChatServer.Modes;
using FreddieChatServer.Utils;

namespace FreddieChatServer {

    public class Program {

        private const string applicationName = "FreddiChat (server)";
        private static readonly Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string applicationVersionVerboseName = string.Format("v{0}.{1} Patch {2} Build {3}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);

        private static readonly List<string> exitCommands = new List<string> { "q", "exit", "quit" };

        public static void Main(string[] args) {
            ConsoleUtils.TraceSystemInfo("{0} {1}", applicationName, applicationVersionVerboseName);

            ConsoleUtils.TraceSystemWork("Configuring server...");

            // Select server protocol
            var protocol = ConsoleUtils.ReadCommand("Select server protocol", "http", "net.pipe", "net.tcp");
            IChatServiceMode mode;
            switch (protocol) {
                case "net.pipe":
                    mode = new NamedPipeChatServiceMode();
                    break;
                case "net.tcp":
                    mode = new TcpChatServiceMode();
                    break;
                case "http":
                default:
                    mode = new HttpChatServiceMode();
                    break;
            }

            // Select server hostname (the name clients will use)
            var hostname = ConsoleUtils.ReadNonEmpty("Select server hostname");

            string url = string.Format("{0}://{1}", mode.Protocol, hostname);

            // Select server port (if required)
            if (mode.IsPortRequired) {
                var port = ConsoleUtils.ReadNonEmpty("Select server port");
                url = string.Format("{0}:{1}", url, port);
            }

            url = string.Format("{0}/FreddiChat", url);

            ConsoleUtils.TraceSystemWork("Server configured: {0}", url);

            ConsoleUtils.TraceSystemWork("Starting server...");
            var service = new ServiceHost(typeof(ChatService), new Uri(url));
            try {
                // Add service behavior
                service.Description.Behaviors.Add(mode.ServiceBehavior);

                // Add MEX endpoint (allowing clients to fetch metadata)
                service.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, mode.ServiceMetadataBinding, "mex");
                // Add application endpoint
                service.AddServiceEndpoint(typeof(IChatService), mode.ServiceEndpointBinding, "");

                // Open the service host to accept incoming calls
                service.Open();

                // The service can now be accessed.
                ConsoleUtils.TraceSystemWork("Server started.");

                var command = string.Empty;
                while (!exitCommands.Contains(command)) {
                    command = Console.ReadLine();
                }

                service.Close();
            } catch (CommunicationException communicationException) {
                ConsoleUtils.TraceSystemError("Unexpected communication error: {0}", communicationException.Message);
            } catch (Exception exception) {
                ConsoleUtils.TraceSystemError("Unexpected error: {0}", exception.Message);
            } finally {
                // Close the ServiceHostBase to shutdown the service.
                service.Abort();
            }
        }

    }

}
