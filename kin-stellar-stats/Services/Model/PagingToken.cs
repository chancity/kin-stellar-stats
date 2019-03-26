using System;
using System.Collections.Generic;
using System.Text;

namespace Kin.Horizon.Api.Poller.Services.Model
{
    public class PagingToken
    {
        public long Value { get; set; }
        public string Cursor { get; set; }
    }
}
