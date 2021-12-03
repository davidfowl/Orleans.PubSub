using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.PubSub;
using Orleans.Placement;
using Orleans.Runtime;

using var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ILocalMessageBus, SiloAwareMessageBus>();
        services.AddSingleton<IPubSubProvider, PubSubProvider>();
    })
    .UseOrleans(builder => builder.UseLocalhostClustering()
                                  .AddPlacementDirector<FixedPlacement, FixedPlacementDirector>())
    .Build();

await host.StartAsync();

var siloDetails = host.Services.GetRequiredService<ILocalSiloDetails>();
var provider = host.Services.GetRequiredService<IPubSubProvider>();

var pubsub = provider.Create("Chat");

var sub = await pubsub.SubscribeAsync(message =>
{
    Console.WriteLine(Encoding.ASCII.GetString(message));
    return Task.CompletedTask;
});

while (true)
{
    Console.Write($"{siloDetails.SiloAddress}> ");
    var line = Console.ReadLine();
    if (line is null) break;

    var msg = Encoding.ASCII.GetBytes(line);

    await pubsub.PublishAsync(msg);
}

await sub.DisposeAsync();

await host.StopAsync();
