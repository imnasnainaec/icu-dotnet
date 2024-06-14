// Copyright (c) 2022 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System.IO;
using NUnit.Framework;

namespace Icu.Tests
{
#if NET
	[TestFixture]
	public class DllResolverTests
	{
		private string _tempPath;

		[TearDown]
		public void TearDown()
		{
			if (string.IsNullOrEmpty(_tempPath))
				return;

			try
			{
				Directory.Delete(_tempPath, true);
			}
			catch (System.Exception)
			{
				// just ignore
			}
			_tempPath = null;
		}

		[TestCase("linux", ExpectedResult = true, IncludePlatform = "Linux")]
		[TestCase("Linux", ExpectedResult = true, IncludePlatform = "Linux")]
		[TestCase("!windows,osx", ExpectedResult = true, IncludePlatform = "Linux")]
		[TestCase("!windows,linux", ExpectedResult = false, IncludePlatform = "Linux, Win")]
		[TestCase("windows,linux", ExpectedResult = true, IncludePlatform = "Linux")]
		[TestCase("windows", ExpectedResult = true, IncludePlatform = "Win")]
		[TestCase("Windows", ExpectedResult = true, IncludePlatform = "Win")]
		[TestCase("!linux,osx", ExpectedResult = true, IncludePlatform = "Win")]
		[TestCase("windows,linux", ExpectedResult = true, IncludePlatform = "Win")]
		[TestCase("osx", ExpectedResult = true, IncludePlatform = "MacOsX")]
		[TestCase("OSX", ExpectedResult = true, IncludePlatform = "MacOsX")]
		[TestCase("!linux,windows", ExpectedResult = true, IncludePlatform = "MacOsX")]
		[TestCase("!windows,osx", ExpectedResult = false, IncludePlatform = "MacOsX, Win")]
		[TestCase("windows,osx", ExpectedResult = true, IncludePlatform = "MacOsX")]
		public bool ConditionApplies(string condition)
		{
			return DllResolver.ConditionApplies(condition);
		}

		[TestCase(ExpectedResult = "libdl.so.2", IncludePlatform = "Linux")]
		[TestCase(ExpectedResult = "libdl.dll", IncludePlatform = "MacOsX, Win")]
		public string MapLibraryName()
		{
			_tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(_tempPath);
			var fakeDll = Path.Combine(_tempPath, "test.dll");
			var configFile = fakeDll + ".config";
			File.WriteAllText(configFile, @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <dllmap os=""!windows,osx"" dll=""libdl.dll"" target=""libdl.so.2"" />
</configuration>");

			return DllResolver.MapLibraryName(fakeDll, "libdl.dll");
		}
	}
#endif
}
