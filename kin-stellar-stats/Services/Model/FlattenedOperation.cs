using System;
using Kin.Stellar.Sdk.responses;
using Kin.Stellar.Sdk.responses.operations;

namespace Kin.Horizon.Api.Poller.Services.Model
{

    public class OperationRequestFactory
    {
        public static OperationRequest GibeFlattenedOperation(OperationResponse operationResponse, TransactionResponse transactionResponse)
        {
            if (operationResponse is PaymentOperationResponse paymentOperation)
            {
                var po = new FlattenPaymentOperation(paymentOperation, transactionResponse);

                return new OperationRequest()
                {
                    Amount = (long)po.Amount,
                    AppId = GetAppId(po),
                    Cursor = "operation",
                    EpochTime = po.CreatedAt.ToUnixTimeSeconds(),
                    OperationType = OperationType.Payment,
                    PagingToken = po.Id,
                    Recipient = po.To,
                    Sender = po.From
                };
            }

            if (operationResponse is CreateAccountOperationResponse createAccountOperation)
            {
                var ca = new FlattenCreateAccountOperation(createAccountOperation, transactionResponse);

                return new OperationRequest()
                {
                    Amount = (long)ca.StartingBalance,
                    AppId = GetAppId(ca),
                    Cursor = "operation",
                    EpochTime = ca.CreatedAt.ToUnixTimeSeconds(),
                    OperationType = OperationType.CreateAccount,
                    PagingToken = ca.Id,
                    Recipient = ca.Account,
                    Sender = ca.Funder
                };
            }


            var op = new FlattenedOperation(operationResponse, transactionResponse);

            return new OperationRequest()
            {
                Amount = 0,
                AppId = GetAppId(op),
                Cursor = "operation",
                EpochTime = op.CreatedAt.ToUnixTimeSeconds(),
                OperationType = OperationType.Operation,
                PagingToken = op.Id,
                Recipient = "",
                Sender = op.SourceAccount
            };
        }

        public static string GetAppId(FlattenedOperation operation)
        {
            string appId = "not_set";

            try
            {
                if (!string.IsNullOrEmpty(operation.Memo))
                {
                    string[] appIdSplit = operation.Memo.Split('-');

                    if (appIdSplit.Length >= 2)
                    {
                        appId = appIdSplit[1];
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            return appId;
        }
    }
    public class FlattenedOperation
    {
        public long Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
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
            StartingBalance = startingBalance < 0 ? 0 : startingBalance;
        }
    }
}
