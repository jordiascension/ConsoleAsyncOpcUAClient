namespace ConsoleOpcUAClient
{
    using Opc.Ua;
    using Opc.UaFx;
    using Opc.UaFx.Client;

    using System;
    using System.Linq;
    using System.Timers;

    class Program
    {
        private static OpcClient _client;
        private static OpcSubscription? _sub;
        
        static void Main(string[] args)
        {
            // Connect to server
            _client = new OpcClient("opc.tcp://localhost:4840/");
            _client.Connect();

            Console.WriteLine("Client connected! Reading value every second...");
            Console.WriteLine("Press ENTER to exit.\n");

            /*var  nodeIds = new OpcNodeId[]
            {
                "ns=2;s=Demo/MyChangingValue"
                // añade aquí los demás
            };

            var commands = nodeIds.Select(id => new OpcSubscribeDataChange(id.ToString(), (sender, e) =>
            {
                var item = (OpcMonitoredItem)sender;
                Console.WriteLine($"{item.NodeId} => {e.Item.Value.Value}");
            })).ToArray();*/



            var nodeIds = new OpcNodeId[]
            {
                "ns=2;s=Demo/MyChangingValue"
            };

            /*OpcSubscribeDataChange[] commands = nodeIds
                .Select(id => new OpcSubscribeDataChange(id.ToString(), HandleDataChanged))
                .ToArray();

            OpcSubscription sub = _client.SubscribeNodes(commands);

            sub.PublishingInterval = 1000;
            foreach (var mi in sub.MonitoredItems)
            {
                mi.SamplingInterval = 1000;
                mi.QueueSize = 1;
            }
            sub.ApplyChanges();*/



            var commands = nodeIds.Select(id => new OpcSubscribeDataChange(id.ToString(), (sender, e) =>
            {
                var item = (OpcMonitoredItem)sender;
                //Console.WriteLine($"{item.NodeId} => {e.Item.Value.Value}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {item.NodeId} = {e.Item.Value.Value} (Status: {e.Item.Value.Status})");
            })).ToArray();

            _sub = _client.SubscribeNodes(commands);
            _sub.PublishingInterval = 1000;
            _sub.ApplyChanges();

            Console.ReadLine();

            /* _sub = _client.SubscribeNodes(commands);
             _sub.PublishingInterval = 1000;
             _sub.ApplyChanges();
             Console.ReadLine();

             _sub.StopPublishing();
             _client.Disconnect();*/
        }

        /*private static void ReadValue(object sender, ElapsedEventArgs e)
        {
            // Leer el valor del nodo
            var value = _client.ReadNode("ns=2;s=Demo/MyChangingValue").As<int>();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Value = {value}");
        }*/

        /*static void HandleDataChanged(object sender, OpcDataChangeReceivedEventArgs e)
        {
            var item = (OpcMonitoredItem)sender;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {item.NodeId} = {e.Item.Value.Value} (Status: {e.Item.Value.Status})");
        }*/
    }

}
