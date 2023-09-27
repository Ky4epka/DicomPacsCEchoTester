using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Dicom.Network.Client;

namespace DicomPacsConTest
{
    class Program
    {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INVALID_DATA = 0xD;
        public const int ERROR_CONNECTION_ABORTED = 0x4D3;
        public const int ERROR_SERVICE_TIMEOUT = 0x41D;

        public const char ARGUMENT_VALUE_DELIMITER = '=';

        public static int ResultCode { get; private set; } = 0x00;

        public static int WriteSystemError(string description, int code)
        {
            Console.WriteLine($"System code error: {code}. {description}");
            return code;
        }

        class CommandArgument
        {
            public string Argument;
            public bool Required;
            public bool HasValue;
            public bool NotEmptyValue;
            public string Description;

            public string Value { get; set; }
            public bool ValueChanged { get => iValueChanged; }

            private bool iValueChanged;

            public CommandArgument(string argName, bool hasValue, bool notEmptyValue, string description, bool required)
            {
                Argument = argName;
                HasValue = hasValue;
                NotEmptyValue = notEmptyValue;
                Description = description;
                Value = string.Empty;
                Required = required;
                iValueChanged = false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="rawArgument"></param>
            /// <returns>True is parsed successfully</returns>
            public bool Parse(string rawArgument)
            {
                string[] data = rawArgument.Split(new char[1] { ARGUMENT_VALUE_DELIMITER }, 2);

                if (data[0].Trim().ToLower() != Argument.ToLower())
                    return false;

                iValueChanged = false;
                if ((data.Length > 1) && (HasValue))
                {
                    Value = data[1].Trim();
                    iValueChanged = true;
                }
                else
                    Value = string.Empty;

                if (NotEmptyValue && (Value == string.Empty))
                {
                    throw new InvalidOperationException($"Argument {Value} must be not empty value!");
                }
                    
                return true;
            }
        }
        public async static Task<int> Main(string[] args)
        {
            string hostName = "";
            int port = 0;
            string clientAE = "";
            string hostAE = "";

            Dictionary<string, CommandArgument> argumentsMeta = new Dictionary<string, CommandArgument>();

            argumentsMeta.Add("-help", new CommandArgument("-help", true, false, "Help", false));
            argumentsMeta.Add("-host", new CommandArgument("-host", true, true, "Host address", true));
            argumentsMeta.Add("-port", new CommandArgument("-port", true, true, "Host address port", true));
            argumentsMeta.Add("-clientae", new CommandArgument("-clientae", true, true, "Client AE title", true));
            argumentsMeta.Add("-hostae", new CommandArgument("-hostae", true, true, "Host AE title", true));

            if (args.Length == 0)
            {
                return WriteSystemError("Empty command line", ERROR_INVALID_DATA);
            }

            foreach (var arg in args)
            {

                foreach (var argMeta in argumentsMeta)
                {
                    try
                    {
                        if (argMeta.Value.Parse(arg) && argMeta.Value.Argument == "-help")
                        {
                            Console.WriteLine("Help about commands:");

                            foreach (var argMetaDisplay in argumentsMeta)
                            {
                                Console.WriteLine($"Argument: {argMetaDisplay.Value.Argument} - {argMetaDisplay.Value.Description}");
                            }

                            return ERROR_INVALID_DATA;
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine(e.Message);
                        return ERROR_INVALID_DATA;
                    }
                }                    
            }

            bool requiredCheckPassed = true;

            foreach (var argMeta in argumentsMeta)
            {
                if (argMeta.Value.Required && !argMeta.Value.ValueChanged)
                {
                    Console.WriteLine($"Value of required argument '{argMeta.Value.Argument}' not defined!");
                    requiredCheckPassed = false;
                }
            }

            if (!requiredCheckPassed)
                return WriteSystemError("One or more required arguments not found", ERROR_INVALID_DATA);

            hostName = argumentsMeta["-host"].Value;

            if (!int.TryParse(argumentsMeta["-port"].Value, out port))
            {
                return WriteSystemError("port argument must be integer value", ERROR_INVALID_DATA);
            }

            clientAE = argumentsMeta["-clientae"].Value;
            hostAE = argumentsMeta["-hostae"].Value;

            try
            {
                var client = new DicomClient(hostName, port, false, clientAE, hostAE);
                client.AssociationRequestTimeoutInMs = 5000;

                var dicomCEchoRequest = new Dicom.Network.DicomCEchoRequest();

                dicomCEchoRequest.OnResponseReceived += (s, e) => {
                    Console.WriteLine($"C-ECHO status code: {e.Status.Code}");
                    ResultCode = ERROR_SUCCESS;
                };

                dicomCEchoRequest.OnTimeout += (s, e) => {
                    ResultCode = ERROR_SERVICE_TIMEOUT;
                };

                await client.AddRequestAsync(dicomCEchoRequest);
                await client.SendAsync();
            }
            catch (Exception e)
            {
                return WriteSystemError("Sending operation aborted by exception: " + e.Message, ERROR_CONNECTION_ABORTED);
            }

            return ResultCode;
        }

    }

}