using System;
using System.Collections.Generic;
using System.Text;
using Kin.Stellar.Sdk.xdr;

namespace Kin.Horizon.Api.Poller.Services.Model
{
    public enum OperationType
    {
        Operation,
        CreateAccount,
        Payment
    }
    public class OperationRequest
    {

            public long PagingToken { get; set; }
            public string Cursor { get; set; }
            public long EpochTime { get; set; }
            public OperationType OperationType { get; set; }
            public string AppId { get; set; }
            public string Sender { get; set; }
            public string Recipient { get; set; }
            public long Amount { get; set; }
        
    }
}
