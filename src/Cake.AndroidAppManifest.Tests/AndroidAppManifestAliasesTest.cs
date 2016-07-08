using System;
using System.IO;
using Cake.Core;
using Cake.Core.IO;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Cake.AndroidAppManifest.Tests
{
    public class AndroidManifestAliasesTest
    {
        [Fact]
        public void SerializeTest()
        {
            var cake = Substitute.For<ICakeContext>();
            var manifest = cake.DeserializeAppManifest(new FilePath("AndroidManifest.xml"));
            manifest.MinSdkVersion.Should().Be(15);
            manifest.PackageName.Should().Be("com.example.android");
            manifest.VersionName.Should().Be("1.0");
            manifest.VersionCode.Should().Be(1);
            manifest.ApplicationIcon.Should().Be("@drawable/icon");
            manifest.ApplicationLabel.Should().Be("Android Application");
        }


        const string SaveTestPath = "Test.xml";
        [Fact]
        public void SaveTest()
        {
            if (File.Exists(SaveTestPath))
            {
                File.Delete(SaveTestPath);
            }

            var originalManifest = AndroidAppManifest.Create("com.test.test", "Test Label");
            originalManifest.MinSdkVersion = 14;
            originalManifest.VersionName = "3.3";
            originalManifest.VersionCode = 2;
            originalManifest.ApplicationIcon = "@drawable/icon";

            var cake = Substitute.For<ICakeContext> ();
            cake.SerializeAppManifest(SaveTestPath, originalManifest);

            var modifiedManifest = cake.DeserializeAppManifest(new FilePath(SaveTestPath));
            modifiedManifest.PackageName.Should().Be(originalManifest.PackageName);
            modifiedManifest.ApplicationLabel.Should().Be(originalManifest.ApplicationLabel);
            modifiedManifest.ApplicationIcon.Should().Be(originalManifest.ApplicationIcon);
            modifiedManifest.MinSdkVersion.Should().Be(originalManifest.MinSdkVersion);
            modifiedManifest.VersionName.Should().Be("3.3");
            modifiedManifest.VersionCode.Should().Be(2);
        }
    }
}
