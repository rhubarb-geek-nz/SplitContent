// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace RhubarbGeekNz.SplitContent
{
    [Cmdlet(VerbsCommon.Split, "Content")]
    [OutputType(typeof(byte[]))]
    sealed public class SplitContent : PSCmdlet
    {
        [Parameter(ParameterSetName = "literal-byte", Mandatory = true, ValueFromPipeline = true, HelpMessage = "Source literal filename")]
        [Parameter(ParameterSetName = "literal-char", Mandatory = true, ValueFromPipeline = true, HelpMessage = "Source literal filename")]
        public string[] LiteralPath;

        [Parameter(ParameterSetName = "wild-byte", Mandatory = true, ValueFromPipeline = true, HelpMessage = "Source filename patterns", Position = 0)]
        [Parameter(ParameterSetName = "wild-char", Mandatory = true, ValueFromPipeline = true, HelpMessage = "Source filename patterns", Position = 0)]
        public string[] Path;

        [Parameter(ParameterSetName = "literal-byte", Mandatory = false, HelpMessage = "Buffer Length")]
        [Parameter(ParameterSetName = "wild-byte", Mandatory = false, HelpMessage = "Buffer Length")]
        public int ReadCount = 4096;

        [Parameter(ParameterSetName = "literal-byte", Mandatory = true, HelpMessage = "Treat file as binary")]
        [Parameter(ParameterSetName = "wild-byte", Mandatory = true, HelpMessage = "Treat file as binary")]
        public SwitchParameter AsByteStream
        {
            get
            {
                return asByteStream;
            }

            set
            {
                asByteStream = value;
            }
        }
        private bool asByteStream;

        [Parameter(ParameterSetName = "literal-char", Mandatory = false, HelpMessage = "Text encoding")]
        [Parameter(ParameterSetName = "wild-char", Mandatory = false, HelpMessage = "Text encoding")]
        public Encoding Encoding
        {
            get
            {
                return encoding;
            }

            set
            {
                encoding = value;
            }
        }
        private Encoding encoding = Encoding.Default;

        protected override void ProcessRecord()
        {
            if (Path != null)
            {
                foreach (string path in Path)
                {
                    try
                    {
                        var paths = GetResolvedProviderPathFromPSPath(path, out var providerPath);

                        if ("FileSystem".Equals(providerPath.Name))
                        {
                            foreach (string item in paths)
                            {
                                SplitContentPath(item);
                            }
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new Exception($"Provider {providerPath.Name} not handled"), "ProviderError", ErrorCategory.NotImplemented, providerPath));
                        }
                    }
                    catch (ItemNotFoundException ex)
                    {
                        WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.ResourceUnavailable, path));
                    }
                }
            }

            if (LiteralPath != null)
            {
                foreach (string literalPath in LiteralPath)
                {
                    try
                    {
                        SplitContentPath(GetUnresolvedProviderPathFromPSPath(literalPath));
                    }
                    catch (ItemNotFoundException ex)
                    {
                        WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.ResourceUnavailable, literalPath));
                    }
                }
            }
        }

        private void SplitContentPath(string path)
        {
            try
            {
                if (asByteStream)
                {
                    SplitByteContent(path);
                }
                else
                {
                    SplitTextContent(path);
                }
            }
            catch (FileNotFoundException ex)
            {
                WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.ResourceUnavailable, path));
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.PermissionDenied, path));
            }
        }

        private void SplitTextContent(string path)
        {
            using (StreamReader streamReader = new StreamReader(path, encoding))
            {
                while (true)
                {
                    string line = streamReader.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    WriteObject(line);
                }
            }
        }

        private void SplitByteContent(string path)
        {
            using (Stream file = File.OpenRead(path))
            {
                byte[] record = null;

                while (true)
                {
                    if (record == null)
                    {
                        record = new byte[ReadCount];
                    }

                    int i = file.Read(record, 0, record.Length);

                    if (i > 0)
                    {
                        if (i == record.Length)
                        {
                            WriteObject(record);

                            record = null;
                        }
                        else
                        {
                            byte[] bytes = new byte[i];

                            Buffer.BlockCopy(record, 0, bytes, 0, i);

                            WriteObject(bytes);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
