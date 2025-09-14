# HueCue AJA Helo Live Stream Feature Implementation

## Summary
Added live streaming capability from AJA Helo devices to the HueCue video analysis application.

## Changes Made:

### 1. New Menu Item
- Added "Load from AJA Helo" menu item under File menu
- Accessible via File > Load from AJA Helo

### 2. New Classes Added:
- `AjaHeloStreamSource.cs`: Handles live stream connection and frame retrieval

### 3. Updated MainWindowViewModel:
- Added `IsLiveStreaming` property
- Added `LoadFromAjaHeloCommand` 
- Added live stream timer (4 FPS - 250ms interval)
- Enhanced PlayPause functionality to work with live streams
- Added proper cleanup for live stream resources

### 4. Technical Implementation:
- Uses HTTP GET requests to `http://192.168.10.248/wall/videofeed.jpg?<GUID>`
- Random GUID cache busting as requested
- 4 FPS refresh rate (loads frames 4 times per second)
- Full integration with existing face detection and histogram analysis
- Proper resource disposal and error handling

### 5. Test Coverage:
- Added unit tests for AjaHeloStreamSource
- Updated MainWindowViewModel tests for new functionality

## User Interface Changes:
The File menu now includes:
- Open Video...
- **Load from AJA Helo** (NEW)
- Exit

When live streaming is active:
- Status shows "AJA Helo Live Stream"
- Play/Pause controls the live stream display
- All existing features (face detection, histogram) work with live stream
- Automatic frame refresh at 4 FPS