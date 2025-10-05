using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HueCue;

public class AtemConnection : IDisposable
{
    private UdpClient? _client;
    private IPEndPoint? _remoteEndPoint;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    private bool _isConnected;
    private bool _disposed;
    private ushort _sessionId;
    private ushort _remotePacketId;
    private ushort _localPacketId;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<AtemInputChangedEventArgs>? PreviewInputChanged;
    public event EventHandler<AtemInputChangedEventArgs>? ProgramInputChanged;

    public bool IsConnected => _isConnected;

    public string? PreviewInput { get; private set; }
    public string? ProgramInput { get; private set; }

    public async Task<bool> ConnectAsync(string ipAddress, int port = 9910)
    {
        if (_isConnected)
        {
            return false;
        }

        try
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            _client = new UdpClient();
            _client.Client.ReceiveBufferSize = 1500 * 50;
            
            _sessionId = (ushort)new Random().Next(32767);
            _remotePacketId = 0;
            _localPacketId = 0;

            _cancellationTokenSource = new CancellationTokenSource();
            
            // Send handshake
            await SendHandshakeAsync();

            // Start receiving task
            _receiveTask = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));

            // Wait a bit for connection
            await Task.Delay(500);

            _isConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);
            
            System.Diagnostics.Debug.WriteLine($"Connected to ATEM at {ipAddress}:{port}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error connecting to ATEM: {ex.Message}");
            Cleanup();
            return false;
        }
    }

    private async Task SendHandshakeAsync()
    {
        if (_client == null || _remoteEndPoint == null)
            return;

        // ATEM handshake packet: 0x10 (handshake flag) + 0x14 (20 bytes total)
        byte[] handshake = new byte[20];
        handshake[0] = 0x10; // Flags: handshake
        handshake[1] = 0x14; // Length: 20 bytes
        handshake[2] = (byte)(_sessionId >> 8);
        handshake[3] = (byte)(_sessionId & 0xFF);
        handshake[12] = 0x01; // Protocol version

        await _client.SendAsync(handshake, handshake.Length, _remoteEndPoint);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _client != null)
        {
            try
            {
                var result = await _client.ReceiveAsync();
                ProcessPacket(result.Buffer);
            }
            catch (SocketException)
            {
                // Connection lost
                break;
            }
            catch (ObjectDisposedException)
            {
                // Client disposed
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error receiving ATEM packet: {ex.Message}");
            }
        }

        if (_isConnected)
        {
            _isConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ProcessPacket(byte[] data)
    {
        if (data.Length < 12)
            return;

        byte flags = data[0];
        ushort length = (ushort)((data[0] & 0x07) << 8 | data[1]);
        
        // Check if this is an acknowledgement
        if ((flags & 0x80) == 0x80)
        {
            // ACK packet
            return;
        }

        // Check if this contains commands
        if (length > 12 && data.Length >= length)
        {
            // Send ACK
            SendAck(data);

            // Parse commands starting at byte 12
            int offset = 12;
            while (offset + 8 <= length)
            {
                ushort cmdLength = (ushort)(data[offset] << 8 | data[offset + 1]);
                if (cmdLength < 8 || offset + cmdLength > length)
                    break;

                // Command name is at offset + 4 (4 bytes)
                string cmdName = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);
                
                ProcessCommand(cmdName, data, offset + 8, cmdLength - 8);
                
                offset += cmdLength;
            }
        }
    }

    private void ProcessCommand(string cmdName, byte[] data, int offset, int length)
    {
        switch (cmdName)
        {
            case "PrvI": // Preview Input
                if (length >= 4)
                {
                    ushort source = (ushort)(data[offset + 2] << 8 | data[offset + 3]);
                    var inputName = GetInputName(source);
                    if (PreviewInput != inputName)
                    {
                        PreviewInput = inputName;
                        PreviewInputChanged?.Invoke(this, new AtemInputChangedEventArgs(inputName));
                        System.Diagnostics.Debug.WriteLine($"Preview changed to: {inputName} (source: {source})");
                    }
                }
                break;

            case "PrgI": // Program Input
                if (length >= 4)
                {
                    ushort source = (ushort)(data[offset + 2] << 8 | data[offset + 3]);
                    var inputName = GetInputName(source);
                    if (ProgramInput != inputName)
                    {
                        ProgramInput = inputName;
                        ProgramInputChanged?.Invoke(this, new AtemInputChangedEventArgs(inputName));
                        System.Diagnostics.Debug.WriteLine($"Program changed to: {inputName} (source: {source})");
                    }
                }
                break;
        }
    }

    private string GetInputName(ushort source)
    {
        // Common ATEM input sources
        return source switch
        {
            0 => "Black",
            1000 => "Camera 1",
            1001 => "Camera 2",
            1002 => "Camera 3",
            1003 => "Camera 4",
            1004 => "Camera 5",
            1005 => "Camera 6",
            1006 => "Camera 7",
            1007 => "Camera 8",
            2001 => "Input 1",
            2002 => "Input 2",
            2003 => "Input 3",
            2004 => "Input 4",
            3010 => "Color Bars",
            3020 => "Color 1",
            3021 => "Color 2",
            4010 => "Media Player 1",
            4020 => "Media Player 2",
            5010 => "Key 1 Mask",
            6000 => "Super Source",
            7001 => "Clean Feed 1",
            7002 => "Clean Feed 2",
            8001 => "Auxiliary 1",
            8002 => "Auxiliary 2",
            10010 => "ME 1 Prog",
            10011 => "ME 1 Prev",
            10020 => "ME 2 Prog",
            10021 => "ME 2 Prev",
            _ => $"Input {source}"
        };
    }

    private async void SendAck(byte[] originalData)
    {
        if (_client == null || _remoteEndPoint == null || originalData.Length < 12)
            return;

        try
        {
            // Create ACK packet
            byte[] ack = new byte[12];
            ack[0] = 0x80; // Flags: ACK
            ack[1] = 0x0C; // Length: 12 bytes
            ack[2] = originalData[2]; // Session ID high
            ack[3] = originalData[3]; // Session ID low
            ack[4] = originalData[10]; // Remote packet ID high
            ack[5] = originalData[11]; // Remote packet ID low

            await _client.SendAsync(ack, ack.Length, _remoteEndPoint);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending ACK: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        if (!_isConnected)
            return;

        _isConnected = false;
        Cleanup();
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    private void Cleanup()
    {
        _cancellationTokenSource?.Cancel();
        _receiveTask?.Wait(TimeSpan.FromSeconds(2));
        _client?.Close();
        _client?.Dispose();
        _client = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Disconnect();
        _disposed = true;
    }
}

public class AtemInputChangedEventArgs : EventArgs
{
    public string InputName { get; }

    public AtemInputChangedEventArgs(string inputName)
    {
        InputName = inputName;
    }
}
