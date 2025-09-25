using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.Grpc.Protocol.Columnar;
using InvalidCredentialException = Couchbase.AnalyticsClient.Exceptions.InvalidCredentialException;
using QueryException = Couchbase.AnalyticsClient.Exceptions.QueryException;


namespace Couchbase.Analytics.Performer.Internal.Utils;

internal static class ExceptionExtensions
{
    public static Error ToProtoError(this Exception exception)
    {
        Serilog.Log.Information("An error happened: {Message}", exception.ToString());

        var errorResponse = new Error();

        if (exception.IsColumnarError())
        {
            var columnarError = new ColumnarError
            {
                AsString = exception.ToString()
            };

            if (exception is QueryException queryException)
            {
                columnarError.SubException = new SubColumnarError
                {
                    QueryException = new Grpc.Protocol.Columnar.QueryException
                    {
                        ErrorCode = queryException.Code,
                        ServerMessage = queryException.Message
                    }
                };
            }

            if (exception is InvalidCredentialException)
            {
                columnarError.SubException = new SubColumnarError
                {
                    InvalidCredentialException =
                        new Grpc.Protocol.Columnar.InvalidCredentialException()
                };
            }

            if (exception is AnalyticsTimeoutException timeoutEx)
            {
                columnarError.SubException = new SubColumnarError
                {
                    TimeoutException = new Grpc.Protocol.Columnar.TimeoutException()
                };
                if (timeoutEx.InnerException is not null)
                {
                    columnarError.Cause = timeoutEx.InnerException.ToProtoError();
                }
            }

            if (exception.InnerException != null)
            {
                columnarError.Cause = ToProtoError(exception.InnerException);
            }

            errorResponse.Columnar ??= columnarError;
        }
        else
        {
            errorResponse.Platform = UnwrapPlatformError(exception);
        }
        return errorResponse;
    }

    private static PlatformError UnwrapPlatformError(this Exception exception)
    {
        return new PlatformError()
        {
            Type = exception is ArgumentException
                ? PlatformErrorType.PlatformErrorInvalidArgument
                : PlatformErrorType.PlatformErrorUnspecified,
            AsString = exception.Message
        };
    }

    private static bool IsColumnarError(this Exception exception)
    {
        switch (exception)
        {
            case null:
                return false;
            case InvalidCredentialException invalidCredentialException:
            case QueryException queryException:
            case AnalyticsTimeoutException timeoutException:
            case AnalyticsException analyticsException:
                return true;
            default:
                return false;
        }
    }
}