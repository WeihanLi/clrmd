﻿
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Diagnostics.Runtime.Utilities;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    [TestClass]
    public class SymbolLocatorTests
    {
        static readonly string WellKnownFile = "mscordacwks_X86_X86_4.6.96.00.dll";
        static readonly int WellKnownTimeStamp = 0x55b96946;
        static readonly int WellKnownImageSize = 0x006a8000;

        static readonly string WellKnownPdb = "clr.pdb";
        static readonly Guid WellKnownGuid = new Guid("0350aa66-2d49-4425-ab28-9b43a749638d");
        static readonly int WellKnownAge = 2;

        private SymbolLocator GetLocator()
        {
            string tmpPath = Helpers.TestWorkingDirectory;

            SymbolLocator locator = new DefaultSymbolLocator();
            locator.SymbolCache = tmpPath;

            var sourceBase = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent;
            string msdiaPath = Path.Combine(sourceBase.FullName, "Microsoft.Diagnostics.Runtime", "Refs", IntPtr.Size == 4 ? "x86" : "amd64", "msdia120.dll");
            IntPtr loadLibraryResult = Native.LoadLibraryEx(msdiaPath, 0, Native.LoadLibraryFlags.NoFlags);
            Assert.AreNotEqual(IntPtr.Zero, loadLibraryResult);

            return locator;
        }

        [TestMethod]
        public void TestFindBinaryNegative()
        {
            SymbolLocator _locator = GetLocator();
            string dac = _locator.FindBinary(WellKnownFile, WellKnownTimeStamp + 1, WellKnownImageSize + 1, false);
            Assert.IsNull(dac);
        }

        [TestMethod]
        public void TestFindPdbNegative()
        {
            SymbolLocator _locator = GetLocator();
            string pdb = _locator.FindPdb(WellKnownPdb, WellKnownGuid, WellKnownAge + 1);
            Assert.IsNull(pdb);
        }
        [TestMethod]
        public async Task TestFindBinaryAsyncNegative()
        {
            SymbolLocator _locator = GetLocator();

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindBinaryAsync(WellKnownFile, WellKnownTimeStamp + 1, WellKnownImageSize + 1, false));
            
            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string dac = await task;
                Assert.IsNull(dac);
            }
        }

        [TestMethod]
        public async Task TestFindPdbAsyncNegative()
        {
            SymbolLocator _locator = GetLocator();

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindPdbAsync(WellKnownPdb, WellKnownGuid, WellKnownAge + 1));
            
            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string pdb = await task;
                Assert.IsNull(pdb);
            }
        }

        [TestMethod]
        public void TestFindBinary()
        {
            SymbolLocator _locator = GetLocator();
            string dac = _locator.FindBinary(WellKnownFile, WellKnownTimeStamp, WellKnownImageSize, false);
            Assert.IsNotNull(dac);
            Assert.IsTrue(File.Exists(dac));
        }

        [TestMethod]
        public void TestFindPdb()
        {
            SymbolLocator _locator = GetLocator();
            string pdb = _locator.FindPdb(WellKnownPdb, WellKnownGuid, WellKnownAge);
            Assert.IsNotNull(pdb);
            Assert.IsTrue(File.Exists(pdb));

            Assert.IsTrue(SymbolLocator.PdbMatches(pdb, WellKnownGuid, WellKnownAge));
        }

        [TestMethod]
        public async Task TestFindBinaryAsync()
        {
            SymbolLocator _locator = GetLocator();
            Task<string> first = _locator.FindBinaryAsync(WellKnownFile, WellKnownTimeStamp, WellKnownImageSize, false);

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindBinaryAsync(WellKnownFile, WellKnownTimeStamp, WellKnownImageSize, false));

            string dac = await first;

            Assert.IsNotNull(dac);
            Assert.IsTrue(File.Exists(dac));
            new PEFile(dac).Dispose();  // This will throw if the image is invalid.

            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string taskDac = await task;
                Assert.AreEqual(dac, taskDac);
            }
        }


        [TestMethod]
        public async Task TestFindPdbAsync()
        {
            SymbolLocator _locator = GetLocator();
            Task<string> first = _locator.FindPdbAsync(WellKnownPdb, WellKnownGuid, WellKnownAge);

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindPdbAsync(WellKnownPdb, WellKnownGuid, WellKnownAge));

            string pdb = await first;
            
            Assert.IsNotNull(pdb);
            Assert.IsTrue(File.Exists(pdb));
            Assert.IsTrue(SymbolLocator.PdbMatches(pdb, WellKnownGuid, WellKnownAge));

            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string taskPdb = await task;
                Assert.AreEqual(taskPdb, pdb);
            }
        }
    }
}