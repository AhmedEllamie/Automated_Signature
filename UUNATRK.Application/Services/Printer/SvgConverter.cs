using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UUNATRK.Application.Models;

namespace UUNATRK.Application.Services.Printer
{
    public static class SvgConverter
    {
        private const double FlattenTolerance = 0.5;
        private const int ArcSegments = 72;

        private const double SvgUnitToMM = 25.4 / 96.0;

        public static List<string> ConvertToGCode(Stream svgStream, PrintRequest req)
        {
            var doc = XDocument.Load(svgStream);
            var ns = doc.Root!.Name.Namespace;

            // Parse viewBox and SVG dimensions
            double svgWidth, svgHeight, minX, minY;
            ParseViewBox(doc.Root, ns, out minX, out minY, out svgWidth, out svgHeight);

            double unitToMM = DetermineUnitToMM(doc.Root, svgWidth, svgHeight);

            // Parse paper offset from request
            double offsetX = ParseMM(req.XPosition);
            double offsetY = ParseMM(req.YPosition);

            // Final scale = SVG-to-mm conversion * user scale multiplier
            // Scale=1 → real-world size (what you see in Inkscape/browser)
            // Scale=2 → double size, Scale=0.5 → half size
            double scale = unitToMM * req.Scale;

            // Extract all polylines (list of point lists) from SVG elements
            var polylines = new List<List<PointD>>();
            ExtractPaths(doc.Root, ns, polylines);

            if (polylines.Count == 0)
                return [];

            // Transform all points: viewBox offset → scale → invert → rotate → paper offset
            var gcode = new List<string>();
            bool penIsDown = false;

            foreach (var polyline in polylines)
            {
                if (polyline.Count == 0) continue;

                for (int i = 0; i < polyline.Count; i++)
                {
                    var pt = polyline[i];

                    // Step 1: Remove viewBox min offset
                    double x = pt.X - minX;
                    double y = pt.Y - minY;

                    // Step 2: Apply scale (SVG units → mm)
                    x *= scale;
                    y *= scale;

                    // Step 3: Apply inversions (after scaling, within the scaled drawing bounds)
                    double scaledW = svgWidth * scale;
                    double scaledH = svgHeight * scale;

                    if (req.InvertX)
                        x = scaledW - x;
                    if (req.InvertY)
                        y = scaledH - y;

                    // Step 4: Apply rotation around center of scaled drawing
                    double cx = scaledW / 2.0;
                    double cy = scaledH / 2.0;
                    (x, y) = RotatePoint(x, y, cx, cy, req.Rotation);

                    // Step 5: Add user offset (positions drawing on paper)
                    double finalX = offsetX + x;
                    double finalY = offsetY + y;

                    if (i == 0)
                    {
                        // First point of a new stroke — pen up, rapid move, pen down
                        if (penIsDown)
                        {
                            gcode.Add("G1 E4.0 F4000");  // intermediate lift
                            penIsDown = false;
                        }
                        gcode.Add($"G0 X{finalX:F3} Y{finalY:F3} F6000.0");  // rapid move
                        gcode.Add("G1 E8.0 F4000");  // pen down
                        penIsDown = true;
                    }
                    else
                    {
                        // Continuing stroke — draw line
                        gcode.Add($"G1 X{finalX:F3} Y{finalY:F3} F5000.0");
                    }
                }
            }

            // Final pen up — full lift (E0.0) only at the very end
            if (penIsDown)
                gcode.Add("G1 E0.0 F4000");

            return gcode;
        }

        #region SVG Parsing

        private static void ParseViewBox(XElement root, XNamespace ns,
            out double minX, out double minY, out double width, out double height)
        {
            minX = 0; minY = 0; width = 0; height = 0;

            var viewBox = root.Attribute("viewBox")?.Value;
            if (!string.IsNullOrEmpty(viewBox))
            {
                var parts = viewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4)
                {
                    minX = ParseDouble(parts[0]);
                    minY = ParseDouble(parts[1]);
                    width = ParseDouble(parts[2]);
                    height = ParseDouble(parts[3]);
                    return;
                }
            }

            // No viewBox — try width/height attributes
            var wAttr = root.Attribute("width")?.Value;
            var hAttr = root.Attribute("height")?.Value;

            if (!string.IsNullOrEmpty(wAttr) && !string.IsNullOrEmpty(hAttr))
            {
                width = ParseSvgLength(wAttr);
                height = ParseSvgLength(hAttr);
                return;
            }

            // Fallback: we must scan all elements to find bounds
            width = 200;
            height = 200;
        }

        /// <summary>
        /// Determine the conversion factor from SVG user units to mm.
        /// 
        /// Strategy:
        ///   1. If SVG has width/height with explicit units (e.g., "210mm", "8in"),
        ///      derive the factor from those + the viewBox.
        ///   2. Otherwise, assume standard 96 DPI: 1 user unit = 1px = 25.4/96 mm.
        /// 
        /// Examples:
        ///   - viewBox="0 0 794 1123" width="210mm" → 210/794 = 0.2645 mm/unit
        ///   - viewBox="0 0 417 127" width="300" (no unit) → 96 DPI = 0.2646 mm/unit
        ///   - viewBox="0 0 100 100" width="100mm" → 100/100 = 1.0 mm/unit
        /// </summary>
        private static double DetermineUnitToMM(XElement root, double viewBoxW, double viewBoxH)
        {
            if (viewBoxW <= 0 || viewBoxH <= 0)
                return SvgUnitToMM;

            var wAttr = root.Attribute("width")?.Value?.Trim();
            var hAttr = root.Attribute("height")?.Value?.Trim();

            // Try width first, then height
            double? fromWidth = TryParseExplicitMM(wAttr, viewBoxW);
            if (fromWidth.HasValue) return fromWidth.Value;

            double? fromHeight = TryParseExplicitMM(hAttr, viewBoxH);
            if (fromHeight.HasValue) return fromHeight.Value;

            // No explicit units — assume 96 DPI standard
            return SvgUnitToMM;
        }

        /// <summary>
        /// If the attribute has explicit physical units (mm, cm, in, pt),
        /// return the mm-per-SVG-unit factor. 
        /// If unitless (pixels), account for viewport-to-viewBox ratio at 96 DPI.
        /// Returns null only for percentages or missing values.
        /// </summary>
        private static double? TryParseExplicitMM(string? attr, double viewBoxDim)
        {
            if (string.IsNullOrEmpty(attr) || viewBoxDim <= 0) return null;
            if (attr.Contains('%')) return null;

            double mmValue;

            if (attr.EndsWith("mm"))
                mmValue = ParseDouble(attr[..^2]);
            else if (attr.EndsWith("cm"))
                mmValue = ParseDouble(attr[..^2]) * 10.0;
            else if (attr.EndsWith("in"))
                mmValue = ParseDouble(attr[..^2]) * 25.4;
            else if (attr.EndsWith("pt"))
                mmValue = ParseDouble(attr[..^2]) * 25.4 / 72.0;
            else if (attr.EndsWith("px"))
            {
                // Explicit px: viewport pixels → mm, then divide by viewBox to get mm/unit
                double px = ParseDouble(attr[..^2]);
                if (px <= 0) return null;
                mmValue = px * SvgUnitToMM;
            }
            else
            {
                // Unitless = pixels in SVG spec: viewport pixels → mm
                double px = ParseDouble(attr);
                if (px <= 0) return null;
                mmValue = px * SvgUnitToMM;
            }

            if (mmValue <= 0) return null;
            return mmValue / viewBoxDim;
        }

        /// <summary>
        /// Recursively extract all drawable elements from the SVG into polylines.
        /// Each polyline is a list of points representing a continuous stroke.
        /// </summary>
        private static void ExtractPaths(XElement element, XNamespace ns, List<List<PointD>> polylines)
        {
            foreach (var child in element.Elements())
            {
                var localName = child.Name.LocalName;

                switch (localName)
                {
                    case "path":
                        ExtractPathElement(child, polylines);
                        break;
                    case "line":
                        ExtractLineElement(child, polylines);
                        break;
                    case "rect":
                        ExtractRectElement(child, polylines);
                        break;
                    case "circle":
                        ExtractCircleElement(child, polylines);
                        break;
                    case "ellipse":
                        ExtractEllipseElement(child, polylines);
                        break;
                    case "polyline":
                        ExtractPolylineElement(child, polylines, close: false);
                        break;
                    case "polygon":
                        ExtractPolylineElement(child, polylines, close: true);
                        break;
                    case "g":
                    case "svg":
                    case "defs": // skip defs content but recurse into g/svg
                        if (localName != "defs")
                            ExtractPaths(child, ns, polylines);
                        break;
                    default:
                        // Recurse into unknown containers
                        ExtractPaths(child, ns, polylines);
                        break;
                }
            }
        }

        private static void ExtractPathElement(XElement path, List<List<PointD>> polylines)
        {
            var d = path.Attribute("d")?.Value;
            if (string.IsNullOrWhiteSpace(d)) return;

            var strokes = ParsePathData(d);
            foreach (var stroke in strokes)
            {
                if (stroke.Count >= 2)
                    polylines.Add(stroke);
                else if (stroke.Count == 1)
                    polylines.Add(stroke); // single point move — still valid
            }
        }

        private static void ExtractLineElement(XElement el, List<List<PointD>> polylines)
        {
            double x1 = GetAttrDouble(el, "x1");
            double y1 = GetAttrDouble(el, "y1");
            double x2 = GetAttrDouble(el, "x2");
            double y2 = GetAttrDouble(el, "y2");

            polylines.Add([new PointD(x1, y1), new PointD(x2, y2)]);
        }

        private static void ExtractRectElement(XElement el, List<List<PointD>> polylines)
        {
            // Skip rects with percentage dimensions (e.g., background fills)
            var wStr = el.Attribute("width")?.Value ?? "";
            var hStr = el.Attribute("height")?.Value ?? "";
            if (wStr.Contains('%') || hStr.Contains('%')) return;

            double x = GetAttrDouble(el, "x");
            double y = GetAttrDouble(el, "y");
            double w = GetAttrDouble(el, "width");
            double h = GetAttrDouble(el, "height");

            if (w <= 0 || h <= 0) return;

            // rx/ry for rounded corners — skip rounding, draw sharp rect
            polylines.Add([
                new PointD(x, y),
                new PointD(x + w, y),
                new PointD(x + w, y + h),
                new PointD(x, y + h),
                new PointD(x, y) // close
            ]);
        }

        private static void ExtractCircleElement(XElement el, List<List<PointD>> polylines)
        {
            double cx = GetAttrDouble(el, "cx");
            double cy = GetAttrDouble(el, "cy");
            double r = GetAttrDouble(el, "r");

            if (r <= 0) return;
            polylines.Add(ApproximateEllipse(cx, cy, r, r));
        }

        private static void ExtractEllipseElement(XElement el, List<List<PointD>> polylines)
        {
            double cx = GetAttrDouble(el, "cx");
            double cy = GetAttrDouble(el, "cy");
            double rx = GetAttrDouble(el, "rx");
            double ry = GetAttrDouble(el, "ry");

            if (rx <= 0 || ry <= 0) return;
            polylines.Add(ApproximateEllipse(cx, cy, rx, ry));
        }

        private static void ExtractPolylineElement(XElement el, List<List<PointD>> polylines, bool close)
        {
            var points = el.Attribute("points")?.Value;
            if (string.IsNullOrWhiteSpace(points)) return;

            var nums = Regex.Matches(points, @"-?[\d.]+(?:[eE][+-]?\d+)?");
            var pts = new List<PointD>();

            for (int i = 0; i + 1 < nums.Count; i += 2)
            {
                pts.Add(new PointD(ParseDouble(nums[i].Value), ParseDouble(nums[i + 1].Value)));
            }

            if (close && pts.Count >= 2)
                pts.Add(pts[0]); // close polygon

            if (pts.Count >= 2)
                polylines.Add(pts);
        }

        private static List<PointD> ApproximateEllipse(double cx, double cy, double rx, double ry)
        {
            var pts = new List<PointD>();
            int segments = ArcSegments;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2.0 * Math.PI * i / segments;
                pts.Add(new PointD(cx + rx * Math.Cos(angle), cy + ry * Math.Sin(angle)));
            }

            return pts;
        }

        #endregion

        #region SVG Path Data Parser

        /// <summary>
        /// Full SVG path data parser supporting:
        /// M/m, L/l, H/h, V/v, C/c, S/s, Q/q, T/t, A/a, Z/z
        /// Returns a list of strokes (each stroke = list of points from one continuous pen-down).
        /// </summary>
        private static List<List<PointD>> ParsePathData(string d)
        {
            var strokes = new List<List<PointD>>();
            var currentStroke = new List<PointD>();
            var tokens = TokenizePath(d);

            double curX = 0, curY = 0;       // current point
            double startX = 0, startY = 0;   // start of current subpath (for Z)
            double lastCX = 0, lastCY = 0;   // last control point (for S/T)
            char lastCmd = ' ';

            int i = 0;

            while (i < tokens.Count)
            {
                char cmd;

                // If token is a command letter, consume it
                if (tokens[i].Length == 1 && char.IsLetter(tokens[i][0]))
                {
                    cmd = tokens[i][0];
                    i++;
                }
                else
                {
                    // Implicit repeat of last command
                    // After M, implicit repeats become L; after m, they become l
                    cmd = lastCmd switch
                    {
                        'M' => 'L',
                        'm' => 'l',
                        _ => lastCmd
                    };
                }

                bool isRelative = char.IsLower(cmd);
                char cmdUpper = char.ToUpper(cmd);

                switch (cmdUpper)
                {
                    case 'M': // MoveTo
                    {
                        if (!TryConsume(tokens, ref i, 2, out var args)) break;
                        double x = args[0], y = args[1];
                        if (isRelative) { x += curX; y += curY; }

                        // Start new stroke
                        if (currentStroke.Count > 0)
                            strokes.Add(currentStroke);
                        currentStroke = [new PointD(x, y)];

                        curX = x; curY = y;
                        startX = x; startY = y;
                        lastCX = curX; lastCY = curY;
                        break;
                    }

                    case 'L': // LineTo
                    {
                        if (!TryConsume(tokens, ref i, 2, out var args)) break;
                        double x = args[0], y = args[1];
                        if (isRelative) { x += curX; y += curY; }

                        currentStroke.Add(new PointD(x, y));
                        curX = x; curY = y;
                        lastCX = curX; lastCY = curY;
                        break;
                    }

                    case 'H': // Horizontal LineTo
                    {
                        if (!TryConsume(tokens, ref i, 1, out var args)) break;
                        double x = args[0];
                        if (isRelative) x += curX;

                        currentStroke.Add(new PointD(x, curY));
                        curX = x;
                        lastCX = curX; lastCY = curY;
                        break;
                    }

                    case 'V': // Vertical LineTo
                    {
                        if (!TryConsume(tokens, ref i, 1, out var args)) break;
                        double y = args[0];
                        if (isRelative) y += curY;

                        currentStroke.Add(new PointD(curX, y));
                        curY = y;
                        lastCX = curX; lastCY = curY;
                        break;
                    }

                    case 'C': // Cubic Bezier
                    {
                        if (!TryConsume(tokens, ref i, 6, out var args)) break;
                        double x1 = args[0], y1 = args[1];
                        double x2 = args[2], y2 = args[3];
                        double x = args[4], y = args[5];

                        if (isRelative)
                        {
                            x1 += curX; y1 += curY;
                            x2 += curX; y2 += curY;
                            x += curX; y += curY;
                        }

                        FlattenCubicBezier(currentStroke, curX, curY, x1, y1, x2, y2, x, y);
                        lastCX = x2; lastCY = y2;
                        curX = x; curY = y;
                        break;
                    }

                    case 'S': // Smooth Cubic Bezier
                    {
                        if (!TryConsume(tokens, ref i, 4, out var args)) break;
                        double x2 = args[0], y2 = args[1];
                        double x = args[2], y = args[3];

                        if (isRelative)
                        {
                            x2 += curX; y2 += curY;
                            x += curX; y += curY;
                        }

                        // Reflect last control point
                        double x1 = 2 * curX - lastCX;
                        double y1 = 2 * curY - lastCY;

                        FlattenCubicBezier(currentStroke, curX, curY, x1, y1, x2, y2, x, y);
                        lastCX = x2; lastCY = y2;
                        curX = x; curY = y;
                        break;
                    }

                    case 'Q': // Quadratic Bezier
                    {
                        if (!TryConsume(tokens, ref i, 4, out var args)) break;
                        double x1 = args[0], y1 = args[1];
                        double x = args[2], y = args[3];

                        if (isRelative)
                        {
                            x1 += curX; y1 += curY;
                            x += curX; y += curY;
                        }

                        FlattenQuadraticBezier(currentStroke, curX, curY, x1, y1, x, y);
                        lastCX = x1; lastCY = y1;
                        curX = x; curY = y;
                        break;
                    }

                    case 'T': // Smooth Quadratic Bezier
                    {
                        if (!TryConsume(tokens, ref i, 2, out var args)) break;
                        double x = args[0], y = args[1];
                        if (isRelative) { x += curX; y += curY; }

                        // Reflect last control point
                        double x1 = 2 * curX - lastCX;
                        double y1 = 2 * curY - lastCY;

                        FlattenQuadraticBezier(currentStroke, curX, curY, x1, y1, x, y);
                        lastCX = x1; lastCY = y1;
                        curX = x; curY = y;
                        break;
                    }

                    case 'A': // Arc
                    {
                        if (!TryConsume(tokens, ref i, 7, out var args)) break;
                        double rx = args[0], ry = args[1];
                        double xRotation = args[2];
                        int largeArc = (int)args[3];
                        int sweep = (int)args[4];
                        double x = args[5], y = args[6];

                        if (isRelative) { x += curX; y += curY; }

                        FlattenArc(currentStroke, curX, curY, rx, ry, xRotation, largeArc != 0, sweep != 0, x, y);
                        curX = x; curY = y;
                        lastCX = curX; lastCY = curY;
                        break;
                    }

                    case 'Z': // ClosePath
                    {
                        if (Math.Abs(curX - startX) > 0.001 || Math.Abs(curY - startY) > 0.001)
                            currentStroke.Add(new PointD(startX, startY));

                        curX = startX; curY = startY;
                        lastCX = curX; lastCY = curY;

                        // End this stroke and prepare for next subpath
                        if (currentStroke.Count > 0)
                            strokes.Add(currentStroke);
                        currentStroke = [];
                        break;
                    }

                    default:
                        i++; // skip unknown
                        break;
                }

                lastCmd = cmd;
            }

            // Add final stroke
            if (currentStroke.Count > 0)
                strokes.Add(currentStroke);

            return strokes;
        }

        /// <summary>
        /// Tokenize SVG path data into command letters and numbers.
        /// Handles: negative numbers, decimals without leading zero (.5), comma/space separators,
        /// and numbers directly following command letters (e.g., "M10,20" or "L-5.3-2.1").
        /// </summary>
        private static List<string> TokenizePath(string d)
        {
            var tokens = new List<string>();
            // Match: command letters OR numbers (with optional sign, decimal, scientific notation)
            var matches = Regex.Matches(d, @"[MmLlHhVvCcSsQqTtAaZz]|-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?");

            foreach (Match m in matches)
                tokens.Add(m.Value);

            return tokens;
        }

        private static bool TryConsume(List<string> tokens, ref int i, int count, out double[] args)
        {
            args = new double[count];
            if (i + count > tokens.Count) return false;

            for (int j = 0; j < count; j++)
            {
                if (i >= tokens.Count || (tokens[i].Length == 1 && char.IsLetter(tokens[i][0])))
                    return false;

                args[j] = ParseDouble(tokens[i]);
                i++;
            }
            return true;
        }

        #endregion

        #region Curve Flattening

        private static void FlattenCubicBezier(List<PointD> pts,
            double x0, double y0, double x1, double y1,
            double x2, double y2, double x3, double y3)
        {
            // Adaptive subdivision — split until flat enough
            FlattenCubicRecursive(pts, x0, y0, x1, y1, x2, y2, x3, y3, 0);
            pts.Add(new PointD(x3, y3));
        }

        private static void FlattenCubicRecursive(List<PointD> pts,
            double x0, double y0, double x1, double y1,
            double x2, double y2, double x3, double y3, int depth)
        {
            if (depth > 12) return; // max recursion

            // Check flatness: distance of control points from the line (x0,y0)→(x3,y3)
            double dx = x3 - x0, dy = y3 - y0;
            double d = Math.Sqrt(dx * dx + dy * dy);

            if (d < 0.001)
            {
                // Degenerate — control points are essentially the same
                return;
            }

            double d1 = Math.Abs((x1 - x3) * dy - (y1 - y3) * dx) / d;
            double d2 = Math.Abs((x2 - x3) * dy - (y2 - y3) * dx) / d;

            if (d1 + d2 <= FlattenTolerance)
                return; // flat enough

            // De Casteljau subdivision at t=0.5
            double x01 = (x0 + x1) / 2, y01 = (y0 + y1) / 2;
            double x12 = (x1 + x2) / 2, y12 = (y1 + y2) / 2;
            double x23 = (x2 + x3) / 2, y23 = (y2 + y3) / 2;
            double x012 = (x01 + x12) / 2, y012 = (y01 + y12) / 2;
            double x123 = (x12 + x23) / 2, y123 = (y12 + y23) / 2;
            double x0123 = (x012 + x123) / 2, y0123 = (y012 + y123) / 2;

            FlattenCubicRecursive(pts, x0, y0, x01, y01, x012, y012, x0123, y0123, depth + 1);
            pts.Add(new PointD(x0123, y0123));
            FlattenCubicRecursive(pts, x0123, y0123, x123, y123, x23, y23, x3, y3, depth + 1);
        }

        private static void FlattenQuadraticBezier(List<PointD> pts,
            double x0, double y0, double x1, double y1, double x2, double y2)
        {
            // Convert quadratic to cubic: CP1 = P0 + 2/3*(P1-P0), CP2 = P2 + 2/3*(P1-P2)
            double cx1 = x0 + 2.0 / 3.0 * (x1 - x0);
            double cy1 = y0 + 2.0 / 3.0 * (y1 - y0);
            double cx2 = x2 + 2.0 / 3.0 * (x1 - x2);
            double cy2 = y2 + 2.0 / 3.0 * (y1 - y2);

            FlattenCubicBezier(pts, x0, y0, cx1, cy1, cx2, cy2, x2, y2);
        }

        /// <summary>
        /// Flatten an SVG arc into line segments using the endpoint-to-center parameterization.
        /// </summary>
        private static void FlattenArc(List<PointD> pts,
            double x1, double y1, double rx, double ry,
            double xRotationDeg, bool largeArc, bool sweep,
            double x2, double y2)
        {
            // Handle degenerate cases
            if (Math.Abs(x1 - x2) < 0.001 && Math.Abs(y1 - y2) < 0.001) return;

            rx = Math.Abs(rx);
            ry = Math.Abs(ry);

            if (rx < 0.001 || ry < 0.001)
            {
                // Degenerate arc — treat as line
                pts.Add(new PointD(x2, y2));
                return;
            }

            double phi = xRotationDeg * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);

            // Step 1: Compute (x1', y1') — transformed midpoint
            double dx2 = (x1 - x2) / 2.0, dy2 = (y1 - y2) / 2.0;
            double x1p = cosPhi * dx2 + sinPhi * dy2;
            double y1p = -sinPhi * dx2 + cosPhi * dy2;

            // Step 2: Correct radii if too small
            double x1p2 = x1p * x1p, y1p2 = y1p * y1p;
            double rx2 = rx * rx, ry2 = ry * ry;
            double lambda = x1p2 / rx2 + y1p2 / ry2;
            if (lambda > 1)
            {
                double sqrtLambda = Math.Sqrt(lambda);
                rx *= sqrtLambda;
                ry *= sqrtLambda;
                rx2 = rx * rx;
                ry2 = ry * ry;
            }

            // Step 3: Compute center point (cx', cy')
            double num = rx2 * ry2 - rx2 * y1p2 - ry2 * x1p2;
            double den = rx2 * y1p2 + ry2 * x1p2;
            double sq = Math.Max(0, num / den);
            double sqRoot = Math.Sqrt(sq) * (largeArc == sweep ? -1 : 1);

            double cxp = sqRoot * rx * y1p / ry;
            double cyp = sqRoot * -ry * x1p / rx;

            // Step 4: Compute center (cx, cy) in original coords
            double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2.0;

            // Step 5: Compute start angle and sweep angle
            double theta1 = AngleOf(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
            double dTheta = AngleOf((x1p - cxp) / rx, (y1p - cyp) / ry,
                                    (-x1p - cxp) / rx, (-y1p - cyp) / ry);

            if (!sweep && dTheta > 0) dTheta -= 2 * Math.PI;
            if (sweep && dTheta < 0) dTheta += 2 * Math.PI;

            // Step 6: Generate points along the arc
            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(dTheta) / (2 * Math.PI) * ArcSegments));

            for (int i = 1; i <= segments; i++)
            {
                double t = theta1 + dTheta * i / segments;
                double xArc = rx * Math.Cos(t);
                double yArc = ry * Math.Sin(t);

                double px = cosPhi * xArc - sinPhi * yArc + cx;
                double py = sinPhi * xArc + cosPhi * yArc + cy;

                pts.Add(new PointD(px, py));
            }
        }

        private static double AngleOf(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            double angle = Math.Acos(Math.Clamp(dot / len, -1, 1));
            if (ux * vy - uy * vx < 0) angle = -angle;
            return angle;
        }

        #endregion

        #region Helpers

        private static (double x, double y) RotatePoint(double x, double y, double cx, double cy, int degrees)
        {
            if (degrees == 0) return (x, y);

            double rad = degrees * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double dx = x - cx, dy = y - cy;

            return (cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
        }

        private static double ParseMM(string value) =>
            ParseDouble(value.Replace("mm", "").Trim());

        private static double ParseSvgLength(string value)
        {
            // Strip known units and parse
            value = value.Trim();
            if (value.EndsWith("px")) return ParseDouble(value[..^2]);
            if (value.EndsWith("pt")) return ParseDouble(value[..^2]) * 1.333; // pt → px
            if (value.EndsWith("mm")) return ParseDouble(value[..^2]) * 3.7795; // mm → px
            if (value.EndsWith("cm")) return ParseDouble(value[..^2]) * 37.795; // cm → px
            if (value.EndsWith("in")) return ParseDouble(value[..^2]) * 96.0; // in → px
            return ParseDouble(value); // assume unitless = user units
        }

        private static double GetAttrDouble(XElement el, string name) =>
            ParseDouble(el.Attribute(name)?.Value ?? "0");

        private static double ParseDouble(string s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

        #endregion
    }

    internal record struct PointD(double X, double Y);
}
