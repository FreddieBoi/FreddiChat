using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace FreddieChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var svcHost = new ServiceHost(typeof(ChatService), new Uri("net.pipe://localhost/FreddiChat"));
            try
            {
                // Check to see if the service host already has a ServiceMetadataBehavior. If not, add one...
                var smb = svcHost.Description.Behaviors.Find<ServiceMetadataBehavior>() ?? new ServiceMetadataBehavior();
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                svcHost.Description.Behaviors.Add(smb);
                // Add MEX endpoint
                svcHost.AddServiceEndpoint(
                  ServiceMetadataBehavior.MexContractName,
                  MetadataExchangeBindings.CreateMexNamedPipeBinding(),
                  "mex"
                );
                // Add application endpoint
                svcHost.AddServiceEndpoint(typeof(IChatService), new NetNamedPipeBinding(), "");
                // Open the service host to accept incoming calls
                svcHost.Open();

                // The service can now be accessed.
                Console.WriteLine("The service is ready.");
                Console.WriteLine("Press <ENTER> to terminate service.");
                Console.WriteLine();
                Console.ReadLine();

                // Close the ServiceHostBase to shutdown the service.
                svcHost.Close();
            }
            catch (CommunicationException commProblem)
            {
                Console.WriteLine("There was a communication problem. " + commProblem.Message);
                Console.Read();
            }

        }
    }
}
