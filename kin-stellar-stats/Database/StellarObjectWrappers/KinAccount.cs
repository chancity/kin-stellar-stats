using System;
using Kin.Stellar.Sdk.responses;

namespace Kin.Horizon.Api.Poller.Database.StellarObjectWrappers
{
    public class KinAccount
    {
        public string Id { get; set; }
        public string Memo { get; set; }
        public double Balance { get; set; } = 0;
        public int AccountCreditedCount { get; set; } = 0;
        public int AccountDebitedCount { get; set; } = 0;
        public double AccountCreditedVolume { get; set; } = 0;
        public double AccountDebitedVolume { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastActive { get; set; }


        public KinAccount()
        {
            Balance = 0;
        }

        public KinAccount(AccountResponse accountResponse)
        {
            Id = accountResponse.KeyPair.AccountId;

            foreach (Balance accountResponseBalance in accountResponse.Balances)
            {
                if (accountResponseBalance.AssetCode == "KIN")
                {
                    double.TryParse(accountResponseBalance.BalanceString, out var balanceN);
                    Balance = balanceN;
                }
                
            }
        }

        protected bool Equals(KinAccount other)
        {
            return string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((KinAccount) obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }
}
