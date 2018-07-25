using System;
using System.Collections.Generic;
using stellar_dotnet_sdk.responses;

namespace kin_stellar_stats.Database.StellarObjectWrappers
{
    public class KinAccount
    {
        public string Id { get; set; }
        public string Memo { get; set; }
        public virtual ICollection<FlattenedBalance> FlattenedBalance { get; set; }
        public int AccountCreditedCount { get; set; } = 0;
        public int AccountDebitedCount { get; set; } = 0;
        public int AccountCreditedVolume { get; set; } = 0;
        public int AccountDebitedVolume { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastActive { get; set; }


        public KinAccount()
        {
            FlattenedBalance = new List<FlattenedBalance>();
        }

        public KinAccount(AccountResponse accountResponse)
        {
            Id = accountResponse.KeyPair.AccountId;
            FlattenedBalance = new List<FlattenedBalance>();

            foreach (Balance accountResponseBalance in accountResponse.Balances)
            {
                FlattenedBalance.Add(new FlattenedBalance(accountResponseBalance));
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
