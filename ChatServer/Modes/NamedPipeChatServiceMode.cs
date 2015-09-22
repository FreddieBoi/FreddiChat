using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace FreddieChatServer.Modes {

    /// <summary>
    /// Service mode used for Named Pipe (NetNamedPipeBinding).
    /// </summary>
    public class NamedPipeChatServiceMode : IChatServiceMode {

        public string Protocol {
            get {
                return "net.pipe";
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
        public NamedPipeChatServiceMode() {
            var binding = new NetNamedPipeBinding();
            binding.Security.Mode = NetNamedPipeSecurityMode.None;
            ServiceEndpointBinding = binding;
            ServiceMetadataBinding = MetadataExchangeBindings.CreateMexNamedPipeBinding();
            ServiceBehavior = new ServiceMetadataBehavior {
                MetadataExporter = {
                    PolicyVersion = PolicyVersion.Policy15
                }
            };
        }

    }

}
