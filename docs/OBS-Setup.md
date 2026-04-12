# OBS Studio Integration Setup Guide

This guide explains how to set up OBS Studio to work with HueCue for real-time histogram monitoring of your OBS output.

HueCue supports two integration modes:

| Mode | Description | Best for |
|---|---|---|
| **Browser Source** *(primary)* | Overlay that runs natively inside OBS | Live production use |
| **WPF desktop app** *(secondary)* | Separate window that pulls frames from OBS | Director/monitoring display on a second screen |

---

## Prerequisites

- OBS Studio **28.0 or later** (includes the built-in OBS WebSocket server)
- Both the Browser Source and the WPF app use the OBS WebSocket server

---

## Step 1: Enable the OBS WebSocket Server

1. Open OBS Studio
2. Go to **Tools** → **WebSocket Server Settings**
3. Check **Enable WebSocket server**
4. Configure the following settings:
   - **Server Port**: Default is `4455`
   - **Server Password**: Optionally set a password for security
     - For local use you can leave this empty
     - For remote/LAN connections a password is strongly recommended
5. Click **OK** to save the settings

---

## Integration A – OBS Browser Source (primary, recommended)

The Browser Source runs as a transparent HTML overlay directly inside OBS.  No separate application needs to be running.

### Quick-start

1. In OBS, add a **Browser Source** to your scene.
2. Check **Local file** and browse to `HueCue.BrowserSource/index.html`
   (or point it at a hosted URL if you are serving the files).
3. Set the width/height to match your canvas resolution (e.g., 1920 × 1080).
4. Click **OK** – HueCue connects to the OBS WebSocket automatically.

> See **[HueCue.BrowserSource/README.md](../HueCue.BrowserSource/README.md)** for full configuration options, URL parameters, face detection setup, and troubleshooting.

### Configuration via URL parameters

All settings are encoded as query-string parameters:

```
file:///path/to/HueCue.BrowserSource/index.html?obsHost=localhost&obsPort=4455&obsPassword=&histogram=right&guide=none&faceDetection=true&fps=30
```

| Parameter | Default | Values |
|---|---|---|
| `obsHost` | `localhost` | hostname or IP |
| `obsPort` | `4455` | port number |
| `obsPassword` | *(empty)* | string |
| `histogram` | `right` | `right` \| `below` \| `none` |
| `guide` | `none` | `none` \| `thirds` \| `heatmap` |
| `faceDetection` | `true` | `true` \| `false` |
| `fps` | `30` | 1–60 |

### Monitoring scene tip

Because the Browser Source is part of the scene, screenshots taken via the WebSocket will include the overlay from the previous frame.  To avoid this, add the Browser Source to a **dedicated monitoring scene** (or Studio Mode preview) rather than your live program scene.

---

## Integration B – WPF Desktop App (secondary)

Use the WPF app when you need a separate monitoring window on a second display, such as a director's monitor, while the Browser Source handles the in-OBS overlay.

### Step 1: Configure the connection

The default connection settings are:
- **Host**: `localhost`
- **Port**: `4455`
- **Password**: *(empty)*

These match the OBS WebSocket defaults.  If you changed the port or added a password, update the constructor in `HueCue/Sources/ObsStreamSource.cs`:

```csharp
// In Sources/ObsStreamSource.cs:
public ObsStreamSource(string host = "localhost", string port = "4455", string password = "your-password-here")
```

### Step 2: Connect

1. Start OBS Studio and ensure the WebSocket server is enabled (Step 1 above)
2. Make sure there is an active scene with at least one visible source
3. Launch HueCue
4. Go to **File** → **Load from OBS Studio**
5. HueCue connects and begins displaying the histogram of your current program output

---

## Troubleshooting

### Connection failed (both modes)

1. **Verify OBS WebSocket is enabled**
   - Go to **Tools** → **WebSocket Server Settings** in OBS
   - Ensure **Enable WebSocket server** is checked

2. **Check the port number**
   - Default is `4455`; update the Browser Source URL param or the WPF source code if you changed it

3. **Verify password settings**
   - If a password is set in OBS, supply it via the `obsPassword` URL param (Browser Source) or in `ObsStreamSource.cs` (WPF app)
   - Passwords are case-sensitive

4. **Check firewall settings**
   - For remote/LAN connections, ensure your firewall allows traffic on port `4455`

5. **Review OBS Studio logs**
   - Go to **Help** → **Log Files** → **View Current Log** in OBS
   - Look for WebSocket-related messages

### No histogram / no video

1. **Ensure a scene is active in OBS** with at least one visible source
2. **Check OBS output** – make sure OBS is not paused

---

## How It Works

Both integration modes share the same underlying mechanism:

1. Connect to the OBS WebSocket server
2. Query the current program scene (`GetCurrentProgramScene`)
3. Request JPEG screenshots of the program output at the configured FPS (`GetSourceScreenshot`)
4. Process each screenshot to generate a real-time RGB histogram and run face detection
5. Display the results (histogram + overlays) alongside the video feed

---

## Security Best Practices

1. **Use passwords** for WebSocket connections when connecting over a network or when OBS is accessible from other computers.
2. **Firewall rules**: only open port `4455` to trusted networks; use localhost connections when possible.
3. **Update regularly**: keep OBS Studio updated to the latest version.

---

## Support and Resources

- OBS Studio: https://obsproject.com/
- OBS WebSocket Documentation: https://github.com/obsproject/obs-websocket/
- OBS Forums: https://obsproject.com/forum/
- HueCue Issues: report problems via the GitHub repository

## Version Information

- Compatible with OBS Studio 28.0 and later
- Uses OBS WebSocket Protocol v5.0
- Tested with OBS Studio 30.0+
