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
    /// Interface for service modes.
    /// </summary>
    public interface IChatServiceMode {

        string Protocol {
            get;
        }

        Binding ServiceEndpointBinding {
            get;
        }

        Binding ServiceMetadataBinding {
            get;
        }

        bool IsPortRequired {
            get;
        }

        IServiceBehavior ServiceBehavior {
            get;
        }

    }

}
