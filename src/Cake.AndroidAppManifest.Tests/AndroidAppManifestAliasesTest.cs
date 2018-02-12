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
        private const string SaveTestPath = "Test.xml";
        private ICakeContext Cake { get; }

        public AndroidManifestAliasesTest()
        {
            Cake = Substitute.For<ICakeContext>();
        }

        [Fact]
        public void DeserializeTest()
        {
            var manifest = Cake.DeserializeAppManifest(new FilePath("AndroidManifest.xml"));
            manifest.MinSdkVersion.Should().Be(15);
            manifest.PackageName.Should().Be("com.example.android");
            manifest.VersionName.Should().Be("1.0");
            manifest.VersionCode.Should().Be(1);
            manifest.ApplicationIcon.Should().Be("@drawable/icon");
            manifest.ApplicationLabel.Should().Be("Android Application");
        }      

        [Fact]
        public void SerializeTest()
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
            
            Cake.SerializeAppManifest(SaveTestPath, originalManifest);

            var modifiedManifest = Cake.DeserializeAppManifest(new FilePath(SaveTestPath));
            modifiedManifest.PackageName.Should().Be(originalManifest.PackageName);
            modifiedManifest.ApplicationLabel.Should().Be(originalManifest.ApplicationLabel);
            modifiedManifest.ApplicationIcon.Should().Be(originalManifest.ApplicationIcon);
            modifiedManifest.MinSdkVersion.Should().Be(originalManifest.MinSdkVersion);
            modifiedManifest.VersionName.Should().Be("3.3");
            modifiedManifest.VersionCode.Should().Be(2);
        }
    }
}
