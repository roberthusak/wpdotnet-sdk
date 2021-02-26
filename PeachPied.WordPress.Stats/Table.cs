using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace PeachPied.WordPress.Stats
{
    internal class Table
    {
        private readonly ImmutableArray<Column> _columns;

        public Table(string[] headers, string[][] data)
        {
            _columns =
                headers
                    .Select(h => new Column(h, data.Length))
                    .ToImmutableArray();

            for (int i = 0; i < data.Length; i++)
            {
                var line = data[i];
                for (int j = 0; j < line.Length; j++)
                {
                    _columns[j].Data[i] = line[j];
                }
            }
        }

        private Table(ImmutableArray<Column> columns)
        {
            _columns = columns;
        }

        public void Print(TextWriter writer)
        {
            var columnWidths =
                _columns
                    .Select(
                        col => col.GetWidth())
                    .ToImmutableArray();

            // Headers
            for (int j = 0; j < _columns.Length; j++)
            {
                writer.Write("| ");
                writer.Write(_columns[j].Header.PadLeft(columnWidths[j]));
                writer.Write(' ');
            }
            writer.WriteLine("|");

            // Separating line
            for (int j = 0; j < columnWidths.Length; j++)
            {
                writer.Write("|");
                for (int i = 0; i < columnWidths[j] + 2; i++)
                {
                    writer.Write('-');
                }
            }
            writer.WriteLine("|");

            // Data
            for (int i = 0; i < _columns[0].Data.Length; i++)
            {
                for (int j = 0; j < _columns.Length; j++)
                {
                    writer.Write("| ");
                    writer.Write(_columns[j].Data[i].PadLeft(columnWidths[j]));
                    writer.Write(' ');
                }
                writer.WriteLine("|");
            }
        }

        public (Table, Table) Split(int commonColumns, int dataColumnsLeft)
        {
            int firstColumnCount = commonColumns + dataColumnsLeft;
            var firstColumns = _columns.RemoveRange(firstColumnCount, _columns.Length - firstColumnCount);

            var secondColumns = _columns.RemoveRange(commonColumns, dataColumnsLeft);

            return (new Table(firstColumns), new Table(secondColumns));
        }

        private class Column
        {
            public string Header { get; }
            public string[] Data { get; }

            public Column(string header, int dataCount)
            {
                Header = header;
                Data = new string[dataCount];
            }

            public int GetWidth() =>
                Math.Max(
                    Header.Length,
                    Data.Max(cell => cell.Length));
        }
    }
}
