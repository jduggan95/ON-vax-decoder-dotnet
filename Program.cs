using System;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.Text.Json;

namespace VaccinePassportDecode
{
    class Program
    {
        public static string Base64Pad(string encodedPayload)
        {
            int paddingNeeded = encodedPayload.Length % 4;
            for (int i = 0; i < (4 - paddingNeeded) ; i++)
            {
                encodedPayload = encodedPayload + '=';
            }

            return encodedPayload;
        }

        static string FormatJsonText(string jsonString)
        {
            using var doc = JsonDocument.Parse(
                jsonString,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true
                }
            );
            MemoryStream memoryStream = new MemoryStream();
            using (
                var utf8JsonWriter = new Utf8JsonWriter(
                    memoryStream,
                    new JsonWriterOptions
                    {
                        Indented = true
                    }
                )
            )
            {
                doc.WriteTo(utf8JsonWriter);
            }
            return new System.Text.UTF8Encoding()
            .GetString(memoryStream.ToArray());
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            { 
                string executableName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                Console.WriteLine(string.Format("Usage {0} \"shc:\\\\6762909524320603460...\"", executableName));
            }
            string rawValue = args[0];
            rawValue = rawValue.Replace("shc:/", "");

            StringBuilder sb = new StringBuilder();
            for (int i = 0 ; i < rawValue.Length; i = i + 2)
            {
                string character = rawValue.Substring(i, 2);
                int value = int.Parse(character);
                value = value + 45;
                char asciiChar = (char)value;
                sb.Append(asciiChar);
            }

            string encodedJWT = sb.ToString();
            string [] splits = encodedJWT.Split('.');
            string encodedHeader = splits[0];
            string encodedPayload = splits[1];
            string encodedSignature = splits[2];

            string paddedPayload = Base64Pad(encodedPayload);
            byte[] data = Base64UrlEncoder.DecodeBytes(paddedPayload);

            Inflater inflater = new Inflater(true);
            MemoryStream inMemoryStream = new MemoryStream(data);
            InflaterInputStream zipStream = new InflaterInputStream(inMemoryStream, inflater);
            StreamReader sr = new StreamReader(zipStream);
            string result = sr.ReadToEnd();
            var parsedJson = FormatJsonText(result);
            Console.WriteLine(parsedJson);
        }
    }
}
