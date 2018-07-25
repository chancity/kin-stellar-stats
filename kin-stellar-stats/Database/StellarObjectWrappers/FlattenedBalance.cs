using System;
using System.Collections.Generic;
using System.Text;
using stellar_dotnet_sdk.responses;

namespace kin_stellar_stats.Database.StellarObjectWrappers
{
    public class FlattenedBalance
    {
        public virtual string KinAccountId { get; set; }
        public string AssetCode { get; set; }
        public string AssetType { get; set; }
        public string BalanceString { get; set; }
        public string Limit { get; set; }

        public FlattenedBalance(Balance balance)
        {
            AssetCode = balance.AssetCode;
            AssetType = balance.AssetType;
            BalanceString = balance.BalanceString;
            Limit = balance.Limit;
        }

        public FlattenedBalance() { }
    }
}
