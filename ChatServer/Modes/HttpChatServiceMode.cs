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
    /// Service mode used for HTTP (WSDualHttpBinding).
    /// </summary>
    public class HttpChatServiceMode : IChatServiceMode {

        public string Protocol {
            get {
                return "http";
            }
        }

        public bool IsPortRequired {
            get {
                return true;
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
        public HttpChatServiceMode() {
            ServiceEndpointBinding = new WSDualHttpBinding();
            ServiceMetadataBinding = MetadataExchangeBindings.CreateMexHttpBinding();
            ServiceBehavior = new ServiceMetadataBehavior {
                MetadataExporter = {
                    PolicyVersion = PolicyVersion.Policy15
                },
                HttpGetEnabled = true
            };
        }

    }

}
