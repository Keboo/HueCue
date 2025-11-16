# OBS Studio Integration Setup Guide

This guide explains how to set up OBS Studio to work with HueCue for real-time histogram monitoring of your OBS output.

## Prerequisites

- OBS Studio (version 28.0 or later recommended)
- HueCue application
- Both applications running on the same computer (or accessible via network)

## Step 1: Install OBS WebSocket Plugin

OBS Studio version 28.0 and later includes the OBS WebSocket plugin built-in. If you're using an older version of OBS, you'll need to upgrade to version 28.0 or later.

### Verify OBS WebSocket is Available

1. Open OBS Studio
2. Go to **Tools** → **WebSocket Server Settings**
3. If you see the WebSocket Server Settings dialog, the plugin is installed correctly

## Step 2: Configure OBS WebSocket

1. In OBS Studio, go to **Tools** → **WebSocket Server Settings**
2. Check **Enable WebSocket server**
3. Configure the following settings:
   - **Server Port**: Default is `4455` (you can change this if needed)
   - **Server Password**: Optionally set a password for security
     - For local testing, you can leave this empty
     - For remote connections, setting a password is highly recommended
4. Click **OK** to save the settings

### Important Notes

- The WebSocket server must be enabled for HueCue to connect
- If you change the port from the default `4455`, you'll need to modify the HueCue source code to match
- If you set a password, you'll need to modify the HueCue source code to include it

## Step 3: Configure HueCue for OBS

The default HueCue OBS integration is configured with these settings:
- **Host**: `localhost` (for local connections)
- **Port**: `4455` (OBS WebSocket default port)
- **Password**: `` (empty - no password)

### Customizing Connection Settings

If you need to change these settings (e.g., different port or password), you'll need to modify the `ObsStreamSource.cs` file:

```csharp
// In Sources/ObsStreamSource.cs, update the constructor:
public ObsStreamSource(string host = "localhost", string port = "4455", string password = "your-password-here")
```

## Step 4: Connect HueCue to OBS

1. Start OBS Studio
2. Ensure the WebSocket server is enabled (see Step 2)
3. Create or switch to a scene in OBS
4. Launch HueCue
5. In HueCue, go to **File** → **Load from OBS Studio**
6. HueCue will connect to OBS and begin displaying the histogram of your current program output

## Troubleshooting

### Connection Failed

If HueCue cannot connect to OBS:

1. **Verify OBS WebSocket is enabled**
   - Go to **Tools** → **WebSocket Server Settings** in OBS
   - Ensure **Enable WebSocket server** is checked

2. **Check the port number**
   - The default is `4455`
   - If you changed it, update the HueCue code accordingly

3. **Verify password settings**
   - If you set a password in OBS, you must configure it in HueCue
   - Passwords are case-sensitive

4. **Check firewall settings**
   - For remote connections, ensure your firewall allows traffic on port `4455` (or your custom port)

5. **Review OBS Studio logs**
   - Go to **Help** → **Log Files** → **View Current Log** in OBS
   - Look for WebSocket-related messages

### No Video Display

If HueCue connects but doesn't show video:

1. **Ensure a scene is active in OBS**
   - You must have an active scene with content in OBS

2. **Check OBS output**
   - Make sure OBS is not paused
   - Verify that your scenes have visible sources

3. **Verify scene has content**
   - The current program scene in OBS must have at least one visible source

## How It Works

HueCue connects to OBS Studio via the WebSocket API and:

1. Establishes a connection to the OBS WebSocket server
2. Queries the current program scene
3. Requests screenshots of the program output at 30 FPS
4. Processes each screenshot to generate a real-time RGB histogram
5. Displays the histogram alongside or below the video feed

## Performance Considerations

- The screenshot capture runs at 30 FPS
- Screenshots are compressed as JPEG with 85% quality for optimal performance
- Resolution is set to 1920x1080 by default
- Network latency may affect performance for remote connections

## Advanced Configuration

### Changing Frame Rate

To modify the frame rate, edit `ObsStreamSource.cs`:

```csharp
public double Fps { get; } = 30;  // Change this value (e.g., 60 for higher frame rate)
```

### Changing Screenshot Resolution

To modify the screenshot resolution, edit `ObsStreamSource.cs` in the `GetFrameAsync` method:

```csharp
imageWidth: 1920,   // Change to desired width
imageHeight: 1080,  // Change to desired height
```

### Changing Image Quality

To adjust the JPEG compression quality (1-100), edit `ObsStreamSource.cs`:

```csharp
imageCompressionQuality: 85  // Higher values = better quality, larger size
```

## Security Best Practices

1. **Use passwords** for WebSocket connections when:
   - Connecting over a network
   - OBS is accessible from other computers
   - You want to restrict access

2. **Firewall rules**:
   - Only open port `4455` to trusted networks
   - Use localhost connections when possible

3. **Update regularly**:
   - Keep OBS Studio updated to the latest version
   - Stay informed about security advisories

## Support and Resources

- OBS Studio Website: https://obsproject.com/
- OBS WebSocket Documentation: https://github.com/obsproject/obs-websocket/
- OBS Forums: https://obsproject.com/forum/
- HueCue Issues: Report problems via the GitHub repository

## Version Information

- Compatible with OBS Studio 28.0 and later
- Uses OBS WebSocket Protocol v5.0
- Tested with OBS Studio 30.0+
