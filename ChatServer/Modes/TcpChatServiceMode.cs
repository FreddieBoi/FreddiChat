using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace FreddieChatServer.Modes {

    /// <summary>
    /// Service mode used for TCP (NetTcpBinding).
    /// </summary>
    public class TcpChatServiceMode : IChatServiceMode {

        public string Protocol {
            get {
                return "net.tcp";
            }
        }

        public bool IsPortRequired {
            get {
                return false;
            }
        }

        public Binding ServiceEndpointBinding {
            get;
            private set;
        }

        public Binding ServiceMetadataBinding {
            get;
            private set;
        }

        public IServiceBehavior ServiceBehavior {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TcpChatServiceMode() {
            var binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;
            ServiceEndpointBinding = binding;
            ServiceMetadataBinding = MetadataExchangeBindings.CreateMexTcpBinding();
            ServiceBehavior = new ServiceMetadataBehavior {
                MetadataExporter = {
                    PolicyVersion = PolicyVersion.Policy15
                }
            };
        }

    }

}
