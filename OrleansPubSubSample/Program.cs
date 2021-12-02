using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.PubSub;
using Orleans.Placement;

using var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ILocalMessageBus, SiloAwareMessageBus>();
        services.AddSingleton<IPubSub, PubSub>();
    })
    .UseOrleans(builder => builder.UseLocalhostClustering()
                                  .AddPlacementDirector<FixedPlacement, FixedPlacementDirector>())
    .Build();

await host.StartAsync();

var pubsub = host.Services.GetRequiredService<IPubSub>();

var topic = "Chat";

var sub = await pubsub.SubscribeAsync(topic, message =>
{
    Console.WriteLine(Encoding.ASCII.GetString(message));
    return Task.CompletedTask;
});

var msg = Encoding.ASCII.GetBytes("Hello World");

await pubsub.PublishAsync(topic, msg);

await sub.DisposeAsync();

host.WaitForShutdown();
