using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.responses;
using stellar_dotnet_sdk.responses.effects;
using stellar_dotnet_sdk.responses.operations;

namespace kin_stellar_stats.Database.StellarObjectWrappers
{

    public class FlattenOperationFactory
    {
        public static FlattenedOperation GibeFlattenedOperation(OperationResponse operationResponse, TransactionResponse transactionResponse, EffectResponse effectResponse)
        {
            if (operationResponse is PaymentOperationResponse paymentOperation)
            {
                return new FlattenPaymentOperation(paymentOperation, transactionResponse, effectResponse);
            }
            else if (operationResponse is CreateAccountOperationResponse createAccountOperation)
            {
                return new FlattenCreateAccountOperation(createAccountOperation, transactionResponse, effectResponse);
            }
            else
            {
                return new FlattenedOperation(operationResponse, transactionResponse, effectResponse);
            }
        }
    }
    public class FlattenedOperation
    {
        public long Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string PagingToken { get; set; }
        public string SourceAccount { get; set; }
        public string TransactionHash { get; set; }
        public string Type { get; set; }
        public string EffectType { get; set; }
        public string Memo { get; set; }

        public FlattenedOperation() { }

        public FlattenedOperation(OperationResponse operationResponse, TransactionResponse transactionResponse, EffectResponse effectResponse)
        {
            Id = operationResponse.Id;
            CreatedAt = DateTimeOffset.Parse(operationResponse.CreatedAt);
            PagingToken = operationResponse.PagingToken;
            SourceAccount = operationResponse.SourceAccount.AccountId;
            TransactionHash = operationResponse.TransactionHash;
            Type = operationResponse.Type;
            Memo = transactionResponse.MemoStr;
            EffectType = effectResponse.Type;
        }
    }

    public class FlattenPaymentOperation : FlattenedOperation
    {
        public string Amount { get; set; }
        public string AssetCode { get; set; }
        public string AssetIssuer { get; set; }
        public string AssetType { get; set; }
        public string From { get; set; }
        public string To { get; set; }

        public FlattenPaymentOperation() { }

        public FlattenPaymentOperation(PaymentOperationResponse operationResponse, TransactionResponse transactionResponse, EffectResponse effectResponse) : base(operationResponse, transactionResponse, effectResponse)
        {
            Amount = operationResponse.Amount;
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
        public string StartingBalance { get; set; }

        public FlattenCreateAccountOperation() { }

        public FlattenCreateAccountOperation(CreateAccountOperationResponse operationResponse, TransactionResponse transactionResponse, EffectResponse effectResponse) : base(operationResponse, transactionResponse, effectResponse)
        {
            Account = operationResponse.Account.AccountId;
            Funder = operationResponse.Funder.AccountId;
            StartingBalance = operationResponse.StartingBalance;
        }
    }
}
