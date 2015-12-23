using System;
using System.Configuration;
using System.Xml.Serialization;

namespace WindowPainless
{
    public class DivisionGrid
    {
        public DivisionGrid()
        {
        }

        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString()
            => $"DivisionGrid: {Width}x{Height}";
    }
}