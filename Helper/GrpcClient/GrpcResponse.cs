using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.GrpcClient
{
    public class GrpcResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StatusCode { get; set; } // Map từ RpcException.StatusCode

        public static GrpcResponse<T> Success(T data) => new() { IsSuccess = true, Data = data };

        public static GrpcResponse<T> Failure(string message, string code = "Unknown")
            => new() { IsSuccess = false, ErrorMessage = message, StatusCode = code };
    }
}
