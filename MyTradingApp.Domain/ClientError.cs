using System;

namespace MyTradingApp.Domain
{
    public class ClientError
    {
        public ClientError(int id, int errorCode, string errorMessage)
        {
            Id = id;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public int Id { get; }
        public int ErrorCode { get; }
        public string ErrorMessage { get; }
    }
}