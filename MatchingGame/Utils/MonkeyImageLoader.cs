using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using Svg;

namespace MatchingGame.Utils
{
    /// <summary>
    /// Renders each monkey SVG file into a Bitmap using SVG.NET.
    /// Each SVG contains clip-paths and transforms that isolate one monkey
    /// from a shared sprite sheet — SVG.NET applies these correctly.
    /// </summary>
    public static class MonkeyImageLoader
    {
        private static readonly Dictionary<int, Image> _cache = new Dictionary<int, Image>();
        private static string? _svgFolder;

        public static int Count => 9; // we have 9 monkey SVGs (1.svg … 9.svg)

        public static void Initialize(string svgFolder)
        {
            _svgFolder = svgFolder;
        }

        /// <summary>
        /// Returns a rendered Image for the given 1-based monkey index.
        /// The SVG is rendered at 512×512 pixels for crisp display on cards.
        /// </summary>
        public static Image? GetMonkeyImage(int index)
        {
            if (_cache.TryGetValue(index, out Image? cached))
                return cached;

            string folder = _svgFolder ?? FallbackFolder();
            string svgPath = Path.Combine(folder, $"{index}.svg");

            if (!File.Exists(svgPath))
                return null;

            try
            {
                // SVG.NET loads the document and honours all clip-paths, transforms,
                // and the viewBox — giving us exactly one monkey per image.
                var doc = SvgDocument.Open(svgPath);

                // Render at a fixed size; the card button will scale it further.
                const int size = 512;
                using var bmp = doc.Draw(size, size);

                // Clone so we own the bits independently of the SvgDocument
                var result = new Bitmap(bmp);
                _cache[index] = result;
                return result;
            }
            catch
            {
                return null;
            }
        }

        private static string FallbackFolder()
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(dir ?? ".", "MonkeyImages");
        }
    }
}
