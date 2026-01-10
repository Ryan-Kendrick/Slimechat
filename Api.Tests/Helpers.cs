using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace Api.Tests;

public static class ClientProxyExtensions
{
    // Can't use SignalR extension methods directly. This wrapper adds type safety and makes the tests more readable. 
    public static Task ReceivedSendAsync<T>(this IClientProxy clientProxy, string clientMethodInvocation, Predicate<T> predicate)
    {
        return clientProxy.Received(1).SendCoreAsync(
            clientMethodInvocation,
            Arg.Is<object[]>(args => args.Length == 1 && args[0] is T && predicate((T)args[0])),
            Arg.Any<CancellationToken>()
        );
    }
}