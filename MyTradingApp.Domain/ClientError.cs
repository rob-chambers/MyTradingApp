using System;

namespace MyTradingApp.Domain
{
    public class ClientError
    {
        public ClientError(int id, int errorCode, string errorMessage, Exception exception)
        {
            Id = id;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public int Id { get; }
        public int ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }
    }
}