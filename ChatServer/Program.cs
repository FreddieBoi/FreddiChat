using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace FreddieChatServer {

    public class Program {

        private const string url = "net.pipe://localhost/FreddiChat";

        private static readonly List<string> exitCommands = new List<string> { "q", "exit", "quit" };

        public static void Main(string[] args) {
            ConsoleUtils.TraceSystemWork("Starting server...");
            var service = new ServiceHost(typeof(ChatService), new Uri(url));
            try {
                // Check to see if the service host already has a ServiceMetadataBehavior. If not, add one...
                var behavior = new ServiceMetadataBehavior {
                    MetadataExporter = {
                        PolicyVersion = PolicyVersion.Policy15
                    }
                };
                service.Description.Behaviors.Add(behavior);
                // Add MEX endpoint
                service.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexNamedPipeBinding(), "mex");
                // Add application endpoint
                service.AddServiceEndpoint(typeof(IChatService), new NetNamedPipeBinding(), "");
                // Open the service host to accept incoming calls
                service.Open();

                // The service can now be accessed.
                ConsoleUtils.TraceSystemWork("Server started: {0}", url);

                var command = string.Empty;
                while (!exitCommands.Contains(command)) {
                    command = Console.ReadLine();
                }

            } catch (CommunicationException communicationException) {
                ConsoleUtils.TraceSystemError("Unexpected communication error: {0}", communicationException.Message);
            } catch (Exception exception) {
                ConsoleUtils.TraceSystemError("Unexpected error: {0}", exception.Message);
            } finally {
                // Close the ServiceHostBase to shutdown the service.
                service.Close();
            }
        }

    }

}
