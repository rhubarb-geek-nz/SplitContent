// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

#if NETCOREAPP
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RhubarbGeekNz.SplitContent
{
    [TestClass]
    public class UnitTests
    {
        readonly InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        public UnitTests()
        {
            foreach (Type t in new Type[] {
                typeof(SplitContent)
            })
            {
                CmdletAttribute ca = t.GetCustomAttribute<CmdletAttribute>();

                if (ca == null) throw new NullReferenceException();

                initialSessionState.Commands.Add(new SessionStateCmdletEntry($"{ca.VerbName}-{ca.NounName}", t, ca.HelpUri));
            }

            initialSessionState.Variables.Add(new SessionStateVariableEntry("ErrorActionPreference", ActionPreference.Stop, "Stop action"));
        }

        [TestMethod]
        public void TestSplitText()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
#if NETCOREAPP
                string solution = "../../../..";
#else
                string solution = "../../..";
#endif
                var env = Environment.GetEnvironmentVariables();

                powerShell.AddScript(
                    "Set-Location " + solution + Environment.NewLine +
                    "Split-Content -LiteralPath 'SplitContent.sln'");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(37, outputPipeline.Count);
            }
        }

        [TestMethod]
        public void TestSplitBinary()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                FileInfo file = new FileInfo("RhubarbGeekNz.SplitContent.dll");

                powerShell.AddScript($"Split-Content -LiteralPath '{file.Name}' -AsByteStream");

                var outputPipeline = powerShell.Invoke();

                int total = 0;

                foreach (var item in outputPipeline)
                {
                    byte[] record = (byte[])item.BaseObject;

                    total += record.Length;
                }

                Assert.AreEqual(file.Length, total);
            }
        }

        [TestMethod]
        public void TestWildcardExists()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript($"Split-Content -Path '*.dll' -AsByteStream");

                var outputPipeline = powerShell.Invoke();

                Assert.IsTrue(outputPipeline.Count > 5);
            }
        }

        [TestMethod]
        public void TestWildcardDoesNotExist()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript($"Split-Content -Path '*.foo' -AsByteStream");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(0, outputPipeline.Count);
            }
        }

        [TestMethod]
        public void TestTildeInExistingFile()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript($"Split-Content -LiteralPath '~/.ssh/known_hosts'");

                powerShell.Invoke();
            }
        }


        [TestMethod]
        public void TestFileNotFound()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                bool caught = false;
                string exName = null;

                powerShell.AddScript("Split-Content -LiteralPath 'NoSuchFile' -AsByteStream");

                try
                {
                    powerShell.Invoke();
                }
                catch (ActionPreferenceStopException ex)
                {
                    exName = ex.ErrorRecord.Exception.GetType().Name;
                    caught = ex.ErrorRecord.Exception is FileNotFoundException;
                }

                Assert.IsTrue(caught, exName);
            }
        }

        [TestMethod]
        public void TestAccess()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                bool caught = false;
                string exName = null;
                string file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\ProgramData\\ssh\\ssh_host_ecdsa_key" : "/etc/ssh/ssh_host_ecdsa_key";

                FileInfo info = new FileInfo(file);

                if (info.Exists)
                {
                    powerShell.AddScript($"Split-Content -LiteralPath '{file}' -AsByteStream");

                    try
                    {
                        powerShell.Invoke();
                    }
                    catch (ActionPreferenceStopException ex)
                    {
                        exName = ex.ErrorRecord.Exception.GetType().Name;
                        caught = ex.ErrorRecord.Exception is UnauthorizedAccessException;
                    }

                    Assert.IsTrue(caught, exName);
                }
            }
        }

        [TestMethod]
        public void TestPipelineValue()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript($"'README.md' | Split-Content");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(8, outputPipeline.Count);
            }
        }
    }
}
