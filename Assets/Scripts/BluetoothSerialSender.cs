using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
using UnityCoreBluetooth;

public class BluetoothSerialSender : MonoBehaviour
{
    public Text text;

    private CoreBluetoothManager manager;

    // ★Write先（ESP32が受け取る側）
    private CoreBluetoothCharacteristic rxCharacteristic;

    // ★Notify受信用（ESP32が送ってくる側）
    private CoreBluetoothCharacteristic txCharacteristic;

    // ===== ここをESP32側に合わせる =====
    // スキャンで探すAdvertise名（ESP32側の local_name と一致させる）
    [SerializeField] private string targetPeripheralName = "bluetoothESP32";

    // NUS(UUID) ※ESP32側がNordic UART Serviceで動いてる場合
    [SerializeField] private string serviceUuid = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
    [SerializeField] private string rxUuid = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E"; // Unity -> ESP32 (Write)
    [SerializeField] private string txUuid = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E"; // ESP32 -> Unity (Notify)
    // ========================================

    private bool flag = false;
    private byte[] value = Array.Empty<byte>(); // ★初期値は空に（受信で上書き）

    private float vy = 0.0f;
    private int counter = 0;

    // notify二重有効化防止（同じCharacteristicが複数回通知されるケース対策）
    private bool notifyEnabled = false;

    // Use this for initialization
    void Start()
    {
        manager = CoreBluetoothManager.Shared;

        manager.OnUpdateState((string state) =>
        {
            Debug.Log("state: " + state);
            if (state != "poweredOn") return;

            Debug.Log("StartScan");
            manager.StartScan();
        });

        manager.OnDiscoverPeripheral((CoreBluetoothPeripheral peripheral) =>
        {
            // peripheral.name は空のことがあるので安全に扱う
            if (peripheral == null) return;

            // UnityCoreBluetoothのサンプルでも name が "" のことがある
            if (string.IsNullOrEmpty(peripheral.name)) return;

            // "(null-name)" 系も弾く（環境によって出ることがある）
            if (peripheral.name == " (null-name)" || peripheral.name == "(null-name)") return;


            // ★ここが「ESP32 を探す」条件（大文字小文字を揃えたいならIgnoreCaseでも可）
            if (peripheral.name != targetPeripheralName)
            {
                Debug.Log("discover peripheral name: " + peripheral.name);
                return;

            }

            Debug.Log("Target found. StopScan & Connect: " + peripheral.name);
            manager.StopScan();
            manager.ConnectToPeripheral(peripheral);
        });

        manager.OnConnectPeripheral((CoreBluetoothPeripheral peripheral) =>
        {
            Debug.Log("connected peripheral name: " + peripheral.name);

            // 接続したらサービス探索
            peripheral.discoverServices();
        });

        manager.OnDiscoverService((CoreBluetoothService service) =>
        {
            Debug.Log("discover service uuid: " + service.uuid);

            // UnityCoreBluetooth側がハイフン無し/有り、16bit表記など混在することがあるので正規化して比較
            if (NormalizeUuid(service.uuid) != NormalizeUuid(serviceUuid)) return;

            Debug.Log("Target service matched. Discover characteristics...");
            service.discoverCharacteristics();
        });

        manager.OnDiscoverCharacteristic((CoreBluetoothCharacteristic characteristic) =>
        {
            // ★UnityCoreBluetoothのAPI名は uuid / propertis / setNotifyValue が基本
            // （READMEサンプルに合わせる）:contentReference[oaicite:1]{index=1}
            string uuid = characteristic.Uuid;
            string[] usage = characteristic.Propertis;

            Debug.Log("discover characteristic uuid: " + uuid);

            if (usage != null)
            {
                for (int i = 0; i < usage.Length; i++)
                {
                    Debug.Log("  usage: " + usage[i]);
                }
            }

            // ★RX / TX を UUID で判別して保持（これが重要）
            if (NormalizeUuid(uuid) == NormalizeUuid(rxUuid))
            {
                rxCharacteristic = characteristic;
                Debug.Log("RX characteristic ready (Write target).");
            }
            else if (NormalizeUuid(uuid) == NormalizeUuid(txUuid))
            {
                txCharacteristic = characteristic;
                Debug.Log("TX characteristic found (Notify source).");

                // notify を有効化（1回だけ）
                if (!notifyEnabled && usage != null)
                {
                    for (int i = 0; i < usage.Length; i++)
                    {
                        if (usage[i] == "notify")
                        {
                            // ★UnityCoreBluetoothは setNotifyValue(true)（小文字）:contentReference[oaicite:2]{index=2}
                            txCharacteristic.SetNotifyValue(true);
                            notifyEnabled = true;
                            Debug.Log("Notify enabled on TX.");
                            break;
                        }
                    }
                }
            }
        });

        manager.OnUpdateValue((CoreBluetoothCharacteristic characteristic, byte[] data) =>
        {
            // TXからの通知だけ処理（安全のため）
            if (NormalizeUuid(characteristic.Uuid) != NormalizeUuid(txUuid)) return;

            // ★参照渡しだと内部バッファ再利用の可能性がゼロと言い切れないのでコピー
            if (data == null)
            {
                value = Array.Empty<byte>();
            }
            else
            {
                value = new byte[data.Length];
                Buffer.BlockCopy(data, 0, value, 0, data.Length);
            }

            flag = true;
        });

        manager.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) Write();


    }


    void OnDestroy()
    {
        if (manager != null)
        {
            manager.Stop();
        }
    }

    public void Write()
    {
        if (rxCharacteristic == null)
        {
            Debug.LogWarning("RX characteristic is not ready yet. (Write target not found)");
            return;
        }

        // Unity -> ESP32 送信（文字列）
        rxCharacteristic.Write(System.Text.Encoding.UTF8.GetBytes($"{counter}"));
        counter++;
        print($"sent:{counter}");
    }

    private static string NormalizeUuid(string uuid)
    {
        if (string.IsNullOrEmpty(uuid)) return "";
        return uuid.Replace("-", "").ToUpperInvariant();
    }
}
#endif
