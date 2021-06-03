using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace Wyam.Tables
{
    internal static class CsvFile
    {
        public static IEnumerable<IEnumerable<string>> GetAllRecords(Stream stream, string delimiter = null)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                return GetAllRecords(reader, delimiter);
            }
        }

        public static IEnumerable<IEnumerable<string>> GetAllRecords(TextReader reader, string delimiter = null)
        {
            List<IEnumerable<string>> records = new List<IEnumerable<string>>();
            CsvConfiguration configuration = delimiter == null
                ? new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }
                : new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false, Delimiter = delimiter };

            using (CsvReader csv = new CsvReader(reader, configuration))
            {
                while (csv.Read())
                {
                    string[] currentRecord = csv.Parser.Record;
                    records.Add(currentRecord);
                }
            }

            return records;
        }

        public static void WriteAllRecords(IEnumerable<IEnumerable<string>> records, Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);
            WriteAllRecords(records, writer);
            writer.Flush();
        }

        public static void WriteAllRecords(IEnumerable<IEnumerable<string>> records, TextWriter writer)
        {
            if (records == null)
            {
                return;
            }

            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture) { ShouldQuote = (args) => true };
            CsvWriter csv = new CsvWriter(writer, configuration);
            {
                foreach (IEnumerable<string> row in records)
                {
                    foreach (string cell in row)
                    {
                        csv.WriteField(cell ?? string.Empty);
                    }

                    csv.NextRecord();
                }
            }
        }
    }
}