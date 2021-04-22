using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PeachPied.WordPress.Stats
{
    class TraceChecker
    {
        private List<BlockChecker> _checkers = new List<BlockChecker>();

        public TraceChecker(string patternFile)
        {
            var lines = File.ReadAllLines(patternFile);
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith('*'))
                {
                    string pattern = lines[i].Substring(1);
                    _checkers.Add(new PatternLineChecker(pattern));
                }
                else
                {
                    string[] exactLines =
                        lines
                            .Skip(i)
                            .TakeWhile(line => !line.StartsWith('*'))
                            .ToArray();
                    _checkers.Add(new ExactBlockChecker(exactLines));

                    i += exactLines.Length - 1;       // -1 compensates i++
                }
            }
        }

        public int? CheckTrace(string traceFile)
        {
            using var reader = new StreamReader(traceFile);

            int line = 1;
            foreach (var checker in _checkers)
            {
                var errorOffset = checker.CheckBlock(reader);
                if (errorOffset == null)
                {
                    line += checker.LineCount;
                }
                else
                {
                    return line + errorOffset.Value;
                }
            }

            return null;
        }

        private abstract class BlockChecker
        {
            public abstract int LineCount { get; }

            public abstract int? CheckBlock(StreamReader reader);
        }

        private sealed class ExactBlockChecker : BlockChecker
        {
            private readonly string[] _expectedLines;

            public ExactBlockChecker(string[] expectedLines)
            {
                _expectedLines = expectedLines;
            }

            public override int LineCount => _expectedLines.Length;

            public override int? CheckBlock(StreamReader reader)
            {
                for (int i = 0; i < _expectedLines.Length; i++)
                {
                    string checkedLine = reader.ReadLine();
                    if (checkedLine == null || checkedLine != _expectedLines[i])
                    {
                        return i;
                    }
                }

                return null;
            }
        }

        private sealed class PatternLineChecker : BlockChecker
        {
            private readonly Regex _pattern;

            public override int LineCount => 1;

            public PatternLineChecker(string pattern)
            {
                _pattern = new Regex(pattern);
            }

            public override int? CheckBlock(StreamReader reader)
            {
                string checkedLine = reader.ReadLine();
                return (checkedLine != null && _pattern.IsMatch(checkedLine)) ? (int?)null : 0;
            }
        }
    }
}