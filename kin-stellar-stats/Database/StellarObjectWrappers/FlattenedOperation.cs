using System;
using System.ComponentModel.DataAnnotations.Schema;
using Kin.Stellar.Sdk.responses;
using Kin.Stellar.Sdk.responses.effects;
using Kin.Stellar.Sdk.responses.operations;

namespace Kin.Horizon.Api.Poller.Database.StellarObjectWrappers
{

    public class FlattenOperationFactory
    {
        public static FlattenedOperation GibeFlattenedOperation(OperationResponse operationResponse, TransactionResponse transactionResponse)
        {
            if (operationResponse is PaymentOperationResponse paymentOperation)
            {
                return new FlattenPaymentOperation(paymentOperation, transactionResponse);
            }

            if (operationResponse is CreateAccountOperationResponse createAccountOperation)
            {
                return new FlattenCreateAccountOperation(createAccountOperation, transactionResponse);
            }


            return new FlattenedOperation(operationResponse, transactionResponse);
        }
    }
    public class FlattenedOperation
    {
        public long Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        [NotMapped]
        public string PagingToken { get; set; }
        public string SourceAccount { get; set; }
        public string Type { get; set; }
        public string EffectType { get; set; }
        public string Memo { get; set; }

        public FlattenedOperation() { }

        public FlattenedOperation(OperationResponse operationResponse, TransactionResponse transactionResponse)
        {
            Id = operationResponse.Id;
            CreatedAt = DateTimeOffset.Parse(operationResponse.CreatedAt);
            PagingToken = operationResponse.PagingToken;
            SourceAccount = operationResponse.SourceAccount.AccountId;
            Type = operationResponse.Type;
            Memo = transactionResponse.MemoStr;
        }
    }

    public class FlattenPaymentOperation : FlattenedOperation
    {
        public double Amount { get; set; }
        public string AssetCode { get; set; }
        public string AssetIssuer { get; set; }
        public string AssetType { get; set; }
        public string From { get; set; }
        public string To { get; set; }

        public FlattenPaymentOperation() { }

        public FlattenPaymentOperation(PaymentOperationResponse operationResponse, TransactionResponse transactionResponse) : base(operationResponse, transactionResponse)
        {
            double.TryParse(operationResponse.Amount, out var amountN);
            Amount = amountN;
            AssetCode = operationResponse.AssetCode;
            AssetIssuer = operationResponse.AssetIssuer;
            AssetType = operationResponse.AssetType;
            From = operationResponse.From.AccountId;
            To = operationResponse.To.AccountId;
        }
    }

    public class FlattenCreateAccountOperation : FlattenedOperation
    {
        public string Account { get; set; }
        public string Funder { get; set; }
        public double StartingBalance { get; set; }

        public FlattenCreateAccountOperation() { }

        public FlattenCreateAccountOperation(CreateAccountOperationResponse operationResponse, TransactionResponse transactionResponse) : base(operationResponse, transactionResponse)
        {
            Account = operationResponse.Account.AccountId;
            Funder = operationResponse.Funder.AccountId;
            double.TryParse(operationResponse.StartingBalance, out var startingBalance);
            StartingBalance = startingBalance;
        }
    }
}
