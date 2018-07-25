// Automatically generated by xdrgen
// DO NOT EDIT or your changes may be overwritten

namespace stellar_dotnet_sdk.xdr
{
// === xdr source ============================================================

//  struct TransactionSet
//  {
//      Hash previousLedgerHash;
//      TransactionEnvelope txs<>;
//  };

//  ===========================================================================
    public class TransactionSet
    {
        public Hash PreviousLedgerHash { get; set; }
        public TransactionEnvelope[] Txs { get; set; }

        public static void Encode(XdrDataOutputStream stream, TransactionSet encodedTransactionSet)
        {
            Hash.Encode(stream, encodedTransactionSet.PreviousLedgerHash);
            var txssize = encodedTransactionSet.Txs.Length;
            stream.WriteInt(txssize);
            for (var i = 0; i < txssize; i++) TransactionEnvelope.Encode(stream, encodedTransactionSet.Txs[i]);
        }

        public static TransactionSet Decode(XdrDataInputStream stream)
        {
            var decodedTransactionSet = new TransactionSet();
            decodedTransactionSet.PreviousLedgerHash = Hash.Decode(stream);
            var txssize = stream.ReadInt();
            decodedTransactionSet.Txs = new TransactionEnvelope[txssize];
            for (var i = 0; i < txssize; i++) decodedTransactionSet.Txs[i] = TransactionEnvelope.Decode(stream);
            return decodedTransactionSet;
        }
    }
}