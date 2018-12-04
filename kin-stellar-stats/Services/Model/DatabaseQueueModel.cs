using System.Collections.Generic;
using Kin.Horizon.Api.Poller.Database.StellarObjectWrappers;

namespace Kin.Horizon.Api.Poller.Services.Model
{
    public class DatabaseQueueModel
    {
        public FlattenedOperation Operation { get; }
        public HashSet<KinAccount> KinAccounts { get; }

        public DatabaseQueueModel(FlattenedOperation operation, params KinAccount[] kinAccounts)
        {
            Operation = operation;

            KinAccounts = new HashSet<KinAccount>();

            foreach (KinAccount kinAccount in kinAccounts)
            {
                KinAccounts.Add(kinAccount);
            }
            
        }

        public override string ToString()
        {
            return $"{nameof(Operation)}: {Operation.Id}, {nameof(KinAccounts)}: {KinAccounts.Count}";
        }
    }
}
