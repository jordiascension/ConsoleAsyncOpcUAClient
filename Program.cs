namespace ConsoleOpcUAClient
{
    using Opc.Ua;
    using Opc.UaFx;
    using Opc.UaFx.Client;

    using System;
    using System.Linq;
    using System.Timers;

    using System;
    using System.Linq;
    using System.Threading;
    // using Opc.UaFx;         // según tu librería
    // using Opc.UaFx.Client;  // según tu librería

    class Program
    {
        private static OpcClient? _client;
        private static OpcSubscription? _sub;
        private static readonly CancellationTokenSource _cts = new();

        // Ajustes
        private const string EndpointUrl = "opc.tcp://localhost:4840/";
        private const int PublishingIntervalMs = 1000;
        private const int SamplingIntervalMs = 1000;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
            };

            // Lista de nodos (ejemplo)
            var nodeIds = new[]
            {
                "ns=2;s=Demo/MyChangingValue",
                "ns=2;s=Demo/Variable1",
                "ns=2;s=Demo/Variable2",
            };

            // Si quieres “Tag001..Tag020” así podemos ver como recibes los valores de ellos en la primera subscripción.
            nodeIds = nodeIds
                .Concat(Enumerable.Range(1, 20).Select(i => $"ns=2;s=Demo/Tag{i:000}"))
                .ToArray();

            RunClientLoop(nodeIds, _cts.Token);

            Console.WriteLine("Saliendo...");
        }

        private static void RunClientLoop(string[] nodeIds, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    ConnectAndSubscribe(nodeIds);

                    Console.WriteLine("Client connected + subscribed.");
                    Console.WriteLine("Press ENTER to exit (o Ctrl+C).\n");

                    // Espera a ENTER o cancelación
                    while (!ct.IsCancellationRequested && !Console.KeyAvailable)
                    {
                        Thread.Sleep(100);
                    }

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                        break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
                    SafeCleanup();

                    // Backoff simple
                    if (!ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Reintentando en 2s...");
                        Thread.Sleep(2000);
                    }
                }
            }

            SafeCleanup();
        }

        private static void ConnectAndSubscribe(string[] nodeIds)
        {
            SafeCleanup();

            _client = new OpcClient(EndpointUrl);

            // Si tu librería tiene opciones de seguridad:
            // _client.Security.AutoAcceptUntrustedCertificates = true;

            _client.Connect();

            // Construye comandos de suscripción
            var commands = nodeIds
                .Distinct(StringComparer.Ordinal)
                .Select(id => new OpcSubscribeDataChange(id, HandleDataChanged))
                .ToArray();

            _sub = _client.SubscribeNodes(commands);

            // Config de la suscripción
            _sub.PublishingInterval = PublishingIntervalMs;

            // Config por MonitoredItem (si tu API expone MonitoredItems)
            foreach (var mi in _sub.MonitoredItems)
            {
                mi.SamplingInterval = SamplingIntervalMs;
                mi.QueueSize = 1;          // latest value only
                                           // mi.DiscardOldest = true; // si existe en tu lib
            }

            _sub.ApplyChanges();
        }

        private static void HandleDataChanged(object sender, OpcDataChangeReceivedEventArgs e)
        {
            var item = (OpcMonitoredItem)sender;

            // OJO: si llegan muchos cambios por segundo, esto “inunda” la consola.
            // Aquí lo dejamos simple. Si quieres throttle por nodo te lo añado.
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] {item.NodeId} = {e.Item.Value.Value} (Status: {e.Item.Value.Status})"
            );
        }

        private static void SafeCleanup()
        {
            try
            {
                if (_sub != null)
                {
                    // Si tu lib tiene StopPublishing/Dispose, úsalo.
                    _sub.StopPublishing();
                    //_sub.Dispose();
                    _sub = null;
                }
            }
            catch { /* swallow */ }

            try
            {
                if (_client != null)
                {
                    _client.Disconnect();
                    _client.Dispose();
                    _client = null;
                }
            }
            catch { /* swallow */ }
        }
    }

}
