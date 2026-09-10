#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Margins
{
    public sealed class ProductionAssetLedger
    {
        private readonly Dictionary<string, ProductionAssetLedgerRecord> records;

        private ProductionAssetLedger(
            Dictionary<string, ProductionAssetLedgerRecord> parsedRecords)
        {
            records = parsedRecords;
        }

        public int Count => records.Count;

        public bool TryGetRecord(string assetId, out ProductionAssetLedgerRecord record)
        {
            return records.TryGetValue(assetId ?? string.Empty, out record);
        }

        public static bool TryParse(
            string csv,
            out ProductionAssetLedger ledger,
            out IReadOnlyList<string> errors)
        {
            List<string> parseErrors = new();
            ledger = new ProductionAssetLedger(
                new Dictionary<string, ProductionAssetLedgerRecord>(StringComparer.Ordinal));

            if (!TryReadRows(csv, out List<List<string>> rows, out string csvError))
            {
                parseErrors.Add(csvError);
                errors = parseErrors;
                return false;
            }

            if (rows.Count == 0)
            {
                parseErrors.Add("The production asset ledger is empty.");
                errors = parseErrors;
                return false;
            }

            Dictionary<string, int> columns = new(StringComparer.Ordinal);
            for (int column = 0; column < rows[0].Count; column++)
            {
                string name = rows[0][column].Trim();
                if (string.IsNullOrEmpty(name) || !columns.TryAdd(name, column))
                {
                    parseErrors.Add($"Ledger header column {column + 1} is empty or duplicated.");
                }
            }

            if (!columns.ContainsKey("asset_id"))
            {
                parseErrors.Add("Ledger header is missing required column 'asset_id'.");
            }

            Dictionary<string, ProductionAssetLedgerRecord> parsed =
                new(StringComparer.Ordinal);
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0]))
                {
                    continue;
                }

                string assetId = ReadCell(row, columns, "asset_id").Trim();
                if (string.IsNullOrEmpty(assetId))
                {
                    parseErrors.Add($"Ledger row {rowIndex + 1} has no asset_id.");
                    continue;
                }

                if (!parsed.TryAdd(
                        assetId,
                        new ProductionAssetLedgerRecord(assetId, rowIndex + 1, row, columns)))
                {
                    parseErrors.Add($"Ledger asset_id '{assetId}' is duplicated.");
                }
            }

            ledger = new ProductionAssetLedger(parsed);
            errors = parseErrors;
            return parseErrors.Count == 0;
        }

        private static string ReadCell(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> columns,
            string name)
        {
            return columns.TryGetValue(name, out int index) && index < row.Count
                ? row[index]
                : string.Empty;
        }

        private static bool TryReadRows(
            string csv,
            out List<List<string>> rows,
            out string error)
        {
            rows = new List<List<string>>();
            error = null;
            if (csv == null)
            {
                error = "The production asset ledger text is missing.";
                return false;
            }

            List<string> row = new();
            StringBuilder cell = new();
            bool quoted = false;
            for (int index = 0; index < csv.Length; index++)
            {
                char character = csv[index];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (index + 1 < csv.Length && csv[index + 1] == '"')
                        {
                            cell.Append('"');
                            index++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        cell.Append(character);
                    }

                    continue;
                }

                switch (character)
                {
                    case '"':
                        if (cell.Length != 0)
                        {
                            error = "The production asset ledger contains a quote inside an unquoted cell.";
                            return false;
                        }

                        quoted = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        cell.Append(character);
                        break;
                }
            }

            if (quoted)
            {
                error = "The production asset ledger ends inside a quoted cell.";
                return false;
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return true;
        }
    }

    public sealed class ProductionAssetLedgerRecord
    {
        private readonly IReadOnlyList<string> row;
        private readonly IReadOnlyDictionary<string, int> columns;

        internal ProductionAssetLedgerRecord(
            string assetId,
            int rowNumber,
            IReadOnlyList<string> values,
            IReadOnlyDictionary<string, int> columnIndexes)
        {
            AssetId = assetId;
            RowNumber = rowNumber;
            row = values;
            columns = columnIndexes;
        }

        public string AssetId { get; }
        public int RowNumber { get; }

        public bool HasColumn(string name)
        {
            return columns.ContainsKey(name);
        }

        public string Get(string name)
        {
            return columns.TryGetValue(name, out int index) && index < row.Count
                ? row[index].Trim()
                : string.Empty;
        }

        public bool TryGetNonNegativeInt(string name, out int value)
        {
            return int.TryParse(
                       Get(name),
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   value >= 0;
        }
    }
}
#endif
