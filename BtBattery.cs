using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace DynamicIsland;

internal sealed record BtDev(string Name, int? Percent);


internal static class BtBattery
{
   
    private const string BatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    private const string IsConnectedKey = "System.Devices.Aep.IsConnected";
    private const string ClassicAqs = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";
    private const string LeAqs = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";

    public static async Task<List<BtDev>> GetConnectedAsync()
    {
        var list = new List<BtDev>();
        var props = new[] { BatteryKey, IsConnectedKey };

       
        try
        {
            var found = await DeviceInformation.FindAllAsync(ClassicAqs, props, DeviceInformationKind.AssociationEndpoint);
            foreach (var d in found)
            {
                if (!Connected(d) || string.IsNullOrWhiteSpace(d.Name)) continue;
                list.Add(new BtDev(d.Name, Percent(d)));
            }
        }
        catch { }

        
        try
        {
            var found = await DeviceInformation.FindAllAsync(LeAqs, props, DeviceInformationKind.AssociationEndpoint);
            foreach (var d in found)
            {
                if (!Connected(d) || string.IsNullOrWhiteSpace(d.Name) || list.Any(x => x.Name == d.Name)) continue;
                int? pct = Percent(d) ?? await ReadGattBatteryAsync(d.Id);
                list.Add(new BtDev(d.Name, pct));
            }
        }
        catch { }

        return list;
    }

    private static bool Connected(DeviceInformation d)
        => d.Properties.TryGetValue(IsConnectedKey, out var v) && v is bool b && b;

    private static int? Percent(DeviceInformation d)
    {
        if (d.Properties.TryGetValue(BatteryKey, out var v) && v != null)
        {
            try
            {
                int p = Convert.ToInt32(v);
                if (p is >= 0 and <= 100) return p;
            }
            catch { }
        }
        return null;
    }

    private static async Task<int?> ReadGattBatteryAsync(string id)
    {
        try
        {
            var work = ReadGattCore(id);
            
            return await Task.WhenAny(work, Task.Delay(3000)) == work ? await work : null;
        }
        catch { return null; }
    }

    private static async Task<int?> ReadGattCore(string id)
    {
        using var le = await BluetoothLEDevice.FromIdAsync(id);
        if (le == null) return null;

        var svc = await le.GetGattServicesForUuidAsync(GattServiceUuids.Battery);
        if (svc.Status != GattCommunicationStatus.Success || svc.Services.Count == 0) return null;

        var chars = await svc.Services[0].GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);
        if (chars.Status != GattCommunicationStatus.Success || chars.Characteristics.Count == 0) return null;

        var res = await chars.Characteristics[0].ReadValueAsync();
        if (res.Status != GattCommunicationStatus.Success) return null;
        return DataReader.FromBuffer(res.Value).ReadByte();
    }
}
