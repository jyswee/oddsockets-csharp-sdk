using System;
using System.Text;
using System.Text.Json;
using OddSockets.Exceptions;

namespace OddSockets;

/// <summary>
/// Message size validation utilities
/// 
/// Validates message sizes against industry standard limits (32KB)
/// to ensure reliable real-time messaging performance.
/// </summary>
public static class MessageSizeValidator
{
    /// <summary>
    /// Message size limits (industry standard - matches PubNub)
    /// </summary>
    public static class MessageSizeLimits
    {
        /// <summary>
        /// Maximum message size in bytes (32KB)
        /// </summary>
        public const int MaxMessageSize = 32768;

        /// <summary>
        /// Maximum message size in KB
        /// </summary>
        public const int MaxMessageSizeKB = 32;
    }

    /// <summary>
    /// Validates message size against the 32KB limit
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <returns>The message size in bytes</returns>
    /// <exception cref="OddSocketsValidationException">Thrown when message exceeds size limit</exception>
    public static int ValidateMessageSize(object message)
    {
        if (message == null)
            return 0;

        string messageStr;
        if (message is string str)
        {
            messageStr = str;
        }
        else
        {
            try
            {
                messageStr = JsonSerializer.Serialize(message);
            }
            catch (Exception ex)
            {
                throw new OddSocketsValidationException("Failed to serialize message for size validation", ErrorCodes.SerializationFailed, ex);
            }
        }

        var messageSize = Encoding.UTF8.GetByteCount(messageStr);

        if (messageSize > MessageSizeLimits.MaxMessageSize)
        {
            var messageSizeKB = Math.Round(messageSize / 1024.0, 1);
            throw new OddSocketsValidationException(
                $"Message size ({messageSizeKB}KB) exceeds maximum allowed size of {MessageSizeLimits.MaxMessageSizeKB}KB. " +
                $"This limit matches industry standards (PubNub, Socket.IO) for reliable real-time messaging.",
                ErrorCodes.MessageTooLarge);
        }

        return messageSize;
    }

    /// <summary>
    /// Checks if a message size is valid without throwing an exception
    /// </summary>
    /// <param name="message">Message to check</param>
    /// <returns>True if the message size is valid, false otherwise</returns>
    public static bool IsMessageSizeValid(object message)
    {
        try
        {
            ValidateMessageSize(message);
            return true;
        }
        catch (OddSocketsValidationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a message in bytes
    /// </summary>
    /// <param name="message">Message to measure</param>
    /// <returns>The message size in bytes</returns>
    public static int GetMessageSize(object message)
    {
        if (message == null)
            return 0;

        string messageStr;
        if (message is string str)
        {
            messageStr = str;
        }
        else
        {
            try
            {
                messageStr = JsonSerializer.Serialize(message);
            }
            catch
            {
                return 0; // Return 0 if serialization fails
            }
        }

        return Encoding.UTF8.GetByteCount(messageStr);
    }
}
