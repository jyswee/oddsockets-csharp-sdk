using System.Runtime.Serialization;

namespace OddSockets.Exceptions;

/// <summary>
/// Base exception for all OddSockets-related errors.
/// </summary>
[Serializable]
public class OddSocketsException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets additional details about the error.
    /// </summary>
    public Dictionary<string, object>? Details { get; }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class.
    /// </summary>
    public OddSocketsException() { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OddSocketsException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    public OddSocketsException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class with a specified error message, error code, and details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="details">Additional error details.</param>
    public OddSocketsException(string message, string errorCode, Dictionary<string, object> details) : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class with a specified error message, error code, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected OddSocketsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ErrorCode = info.GetString(nameof(ErrorCode));
        Details = (Dictionary<string, object>?)info.GetValue(nameof(Details), typeof(Dictionary<string, object>));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(Details), Details);
    }
}

/// <summary>
/// Exception thrown when connection-related errors occur.
/// </summary>
[Serializable]
public class OddSocketsConnectionException : OddSocketsException
{
    /// <summary>
    /// Initializes a new instance of the OddSocketsConnectionException class.
    /// </summary>
    public OddSocketsConnectionException() { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsConnectionException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OddSocketsConnectionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsConnectionException class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    public OddSocketsConnectionException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsConnectionException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsConnectionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsConnectionException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected OddSocketsConnectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when authentication-related errors occur.
/// </summary>
[Serializable]
public class OddSocketsAuthenticationException : OddSocketsException
{
    /// <summary>
    /// Initializes a new instance of the OddSocketsAuthenticationException class.
    /// </summary>
    public OddSocketsAuthenticationException() { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsAuthenticationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OddSocketsAuthenticationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsAuthenticationException class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    public OddSocketsAuthenticationException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsAuthenticationException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsAuthenticationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsAuthenticationException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected OddSocketsAuthenticationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when channel-related errors occur.
/// </summary>
[Serializable]
public class OddSocketsChannelException : OddSocketsException
{
    /// <summary>
    /// Gets the channel name associated with this exception.
    /// </summary>
    public string? ChannelName { get; }

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannelException class.
    /// </summary>
    public OddSocketsChannelException() { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannelException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OddSocketsChannelException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannelException class with a specified error message and channel name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="channelName">The channel name.</param>
    public OddSocketsChannelException(string message, string channelName) : base(message)
    {
        ChannelName = channelName;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannelException class with a specified error message, error code, and channel name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="channelName">The channel name.</param>
    public OddSocketsChannelException(string message, string errorCode, string channelName) : base(message, errorCode)
    {
        ChannelName = channelName;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannelException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsChannelException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannelException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected OddSocketsChannelException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ChannelName = info.GetString(nameof(ChannelName));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ChannelName), ChannelName);
    }
}

/// <summary>
/// Exception thrown when message-related errors occur.
/// </summary>
[Serializable]
public class OddSocketsMessageException : OddSocketsException
{
    /// <summary>
    /// Gets the message ID associated with this exception.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// Initializes a new instance of the OddSocketsMessageException class.
    /// </summary>
    public OddSocketsMessageException() { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsMessageException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OddSocketsMessageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsMessageException class with a specified error message and message ID.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="messageId">The message ID.</param>
    public OddSocketsMessageException(string message, string messageId) : base(message)
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsMessageException class with a specified error message, error code, and message ID.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="messageId">The message ID.</param>
    public OddSocketsMessageException(string message, string errorCode, string messageId) : base(message, errorCode)
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsMessageException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsMessageException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsMessageException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected OddSocketsMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        MessageId = info.GetString(nameof(MessageId));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(MessageId), MessageId);
    }
}

/// <summary>
/// Common error codes used throughout the SDK.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Invalid API key format or value.
    /// </summary>
    public const string InvalidApiKey = "INVALID_API_KEY";

    /// <summary>
    /// Connection to OddSockets failed.
    /// </summary>
    public const string ConnectionFailed = "CONNECTION_FAILED";

    /// <summary>
    /// Authentication failed.
    /// </summary>
    public const string AuthenticationFailed = "AUTHENTICATION_FAILED";

    /// <summary>
    /// Channel access denied.
    /// </summary>
    public const string ChannelAccessDenied = "CHANNEL_ACCESS_DENIED";

    /// <summary>
    /// Message delivery failed.
    /// </summary>
    public const string MessageDeliveryFailed = "MESSAGE_DELIVERY_FAILED";

    /// <summary>
    /// Invalid configuration.
    /// </summary>
    public const string InvalidConfiguration = "INVALID_CONFIGURATION";

    /// <summary>
    /// Worker assignment failed.
    /// </summary>
    public const string WorkerAssignmentFailed = "WORKER_ASSIGNMENT_FAILED";

    /// <summary>
    /// Maximum reconnection attempts reached.
    /// </summary>
    public const string MaxReconnectAttemptsReached = "MAX_RECONNECT_ATTEMPTS_REACHED";

    /// <summary>
    /// Operation timeout.
    /// </summary>
    public const string OperationTimeout = "OPERATION_TIMEOUT";

    /// <summary>
    /// Invalid channel name.
    /// </summary>
    public const string InvalidChannelName = "INVALID_CHANNEL_NAME";

    /// <summary>
    /// Message too large.
    /// </summary>
    public const string MessageTooLarge = "MESSAGE_TOO_LARGE";

    /// <summary>
    /// Serialization failed.
    /// </summary>
    public const string SerializationFailed = "SERIALIZATION_FAILED";
}

/// <summary>
/// Exception thrown when validation errors occur.
/// </summary>
[Serializable]
public class OddSocketsValidationException : OddSocketsException
{
    /// <summary>
    /// Initializes a new instance of the OddSocketsValidationException class.
    /// </summary>
    public OddSocketsValidationException() { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsValidationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public OddSocketsValidationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsValidationException class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    public OddSocketsValidationException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsValidationException class with a specified error message, error code, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsValidationException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the OddSocketsValidationException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OddSocketsValidationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the OddSocketsValidationException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected OddSocketsValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
