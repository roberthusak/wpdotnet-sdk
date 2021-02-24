using Pchp.Core;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.Stats
{
    class Program
    {
        static void Main(string[] args)
        {
            var table = new Table(new []{"Header 1", "Header 2"}, new []{new []{"V1", "Value 1"}});

            table.Print(Console.Out);
        }
    }
}
