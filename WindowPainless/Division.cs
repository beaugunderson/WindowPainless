using System;
using System.Configuration;
using System.Windows;
using System.Xml.Serialization;

using static WindowPainless.NativeMethods;

namespace WindowPainless
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class Division
    {
        public Division()
        {
        }

        [XmlAttribute("Width")]
        public int Width { get; set; }

        [XmlAttribute("Height")]
        public int Height { get; set; }

        [XmlAttribute("X")]
        public int X { get; set; }

        [XmlAttribute("Y")]
        public int Y { get; set; }

        public InteropRectangle Bounds(Rect workArea)
        {
            var divisionWidth = workArea.Width / Width;
            var divisionHeight = workArea.Height / Height;

            return new InteropRectangle()
            {
                Left = (int)(workArea.Left + (divisionWidth * (X - 1))),
                Top = (int)(workArea.Top + (divisionHeight * (Y - 1))),
                Right = (int)(workArea.Left + (divisionWidth * X)),
                Bottom = (int)(workArea.Top + (divisionHeight * Y)),
            };
        }

        public override string ToString()
            => $"Division: {Width}x{Height}, X: {X}, Y: {Y}";

        // TODO: still needed?
        public override bool Equals(object o)
        {
            var division = o as Division;

            if (division == null)
            {
                return false;
            }

            return X == division.X &&
                   Y == division.Y &&
                   Width == division.Width &&
                   Height == division.Height;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;

                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;

                return hashCode;
            }
        }
    }
}