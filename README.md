# HueCue - Video Histogram Viewer
A WPF application for viewing video files with real-time histogram overlays.

## Features

- **Video Playback**: Supports multiple video formats (MP4, AVI, MOV, MKV, WMV, FLV, WebM)
- **Real-time Histogram**: RGB histogram visualization overlaid on video
- **Performance Optimized**: Histogram updates every 1 second for optimal performance
- **User-friendly Interface**: Simple menu-driven interface

## Usage

1. **Open Video**: Use `File > Open Video...` to select a video file
2. **Playback Control**: Use `Playback > Play/Pause` to control video playback
3. **Histogram View**: The RGB histogram appears in the top-right corner of the video

## Technical Details

Built with:
- .NET 8.0 WPF
- OpenCvSharp4 for video processing
- CommunityToolkit.Mvvm for MVVM pattern
- Material Design themes

---

# Original WPF app template
This template creates a full WPF application, along with unit tests.

## Template
Create a new app in your current directory by running.

```cli
> dotnet new keboo.wpf
```

### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

## Key Features

### Generic Host Dependency Injection
[Docs](https://learn.microsoft.com/dotnet/core/extensions/generic-host?tabs=appbuilder&WT.mc_id=DT-MVP-5003472)

### Centralized Package Management
[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### Build Customization
[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### CommunityToolkit MVVM
[Docs](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/?WT.mc_id=DT-MVP-5003472)

### Material Design in XAML
[Repo](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)

### .editorconfig formatting
[Docs](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/code-style-rule-options?WT.mc_id=DT-MVP-5003472)

### Testing with Moq.AutoMocker
[Repo](https://github.com/moq/Moq.AutoMocker)

### NuGet package source mapping
[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### Dependabot auto updating of dependencies
[Docs](https://docs.github.com/code-security/dependabot/dependabot-version-updates)
Auto merging of these PRs done with [fastify/github-action-merge-dependabot](https://github.com/fastify/github-action-merge-dependabot).

### GitHub Actions workflow with code coverage reporting
[Docs](https://docs.github.com/actions).
Code coverage provided by [coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet).
Code coverage report provided by [danielpalme/ReportGenerator-GitHub-Action](https://github.com/danielpalme/ReportGenerator-GitHub-Action).
The coverage reports are posted as "stciky" PR comments provided by [marocchino/sticky-pull-request-comment](https://github.com/marocchino/sticky-pull-request-comment)