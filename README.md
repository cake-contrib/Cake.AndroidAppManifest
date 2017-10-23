## Cake.AndroidAppManifest [![Build status](https://ci.appveyor.com/api/projects/status/85o5pruse4h0kotv/branch/master?svg=true)](https://ci.appveyor.com/project/cakecontrib/cake-androidappmanifest/branch/master)

Cake Build addin for [de]serializing and updating an Android AppManifest.</description>

## Installation

Add the following reference to your cake build script:

```
#addin "Cake.AndroidAppManifest"
```

## Usage

```csharp
// load
var manifest = DeserializeAppManifest(new FilePath("AndroidManifest.xml"));

// adjust as needed
manifest.MinSdkVersion = 24;
manifest.PackageName = "com.example.mycoolapp";
manifest.VersionName = "1.0";
manifest.VersionCode = 10;
manifest.ApplicationIcon = "@mipmap/ic_launcher";
manifest.ApplicationLabel = "Android Application";
manifest.Debuggable = false;

// save
SerializeAppManifest(new FilePath("AndroidManifest.xml"), manifest);
```

## With thanks to
* Xamarin for open sourcing their build tools, the internals of this addin were pulled directly from Xamarin.Android.Build.Utilities.
