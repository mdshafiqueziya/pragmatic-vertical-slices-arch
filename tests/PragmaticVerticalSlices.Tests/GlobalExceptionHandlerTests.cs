using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PragmaticVerticalSlices.Api.Infrastructure.Errors;

namespace PragmaticVerticalSlices.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ReturnsGenericProblemDetailsAndLogsStructuredTraceIdentifier()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123"
        };
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("sensitive exception details"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);

        responseBody.Position = 0;
        using var response = await JsonDocument.ParseAsync(responseBody);
        Assert.Equal("An unexpected error occurred.", response.RootElement.GetProperty("title").GetString());
        Assert.Equal("The request could not be completed.", response.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("sensitive exception details", responseBody.ToString());
        Assert.Equal("trace-123", response.RootElement.GetProperty("traceId").GetString());

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, logEntry.Level);
        Assert.Contains(
            logEntry.State,
            item => item.Key == "TraceIdentifier" && Equals(item.Value, "trace-123"));
        Assert.Equal("sensitive exception details", logEntry.Exception?.Message);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var structuredState = state is IEnumerable<KeyValuePair<string, object?>> properties
                ? properties.ToArray()
                : [];
            Entries.Add(new LogEntry(logLevel, structuredState, exception));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        KeyValuePair<string, object?>[] State,
        Exception? Exception);
}
