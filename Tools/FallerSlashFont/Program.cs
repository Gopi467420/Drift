using System.Text;

// Faller Slash: an original dry-brush display font, generated from code.
// Each glyph is a list of brush strokes. A simulated bristle brush paints them into
// a mask, the mask is traced into outlines, and the outlines become a TrueType file.
// Capitals and lowercase are the same letters painted with a different random brush,
// so mixing cases ("RoGeNz") stops repeated letters looking stamped.
//
// Usage: dotnet run -- <output.ttf>


string outputPath = args.Length > 0 ? args[0] : "FallerSlash.ttf";

var designs = Designs.All();
var glyphs = new List<Glyph>
{
    Glyph.Notdef(),
    new Glyph { Name = "space", Advance = 260 }
};

var charToGlyph = new SortedDictionary<int, int> { [' '] = 1, [0xA0] = 1 };

foreach (var (character, strokes) in designs)
{
    int upper = Add(character, strokes, 1000 + character);
    charToGlyph[character] = upper;

    if (char.IsLetter(character))
        charToGlyph[char.ToLowerInvariant(character)] = Add(char.ToLowerInvariant(character), strokes, 5000 + character);
}

int Add(char name, Stroke[] strokes, int seed)
{
    glyphs.Add(BrushPainter.Paint(name.ToString(), strokes, seed));
    return glyphs.Count - 1;
}

byte[] font = TrueType.Build(glyphs, charToGlyph, "Faller Slash");
File.WriteAllBytes(outputPath, font);

int points = glyphs.Sum(g => g.Contours.Sum(c => c.Count));
Console.WriteLine($"{glyphs.Count} glyphs, {points} points, {font.Length / 1024} KB -> {outputPath}");

// ---------------------------------------------------------------- Data

public class Stroke
{
    public (float x, float y)[] Points;
    public float Width = 1f;
    public Stroke(float width, params (float, float)[] points) { Points = points; Width = width; }
    public Stroke(params (float, float)[] points) { Points = points; }
}

public class Glyph
{
    public string Name;
    public int Advance;
    public int Lsb;
    public List<List<(int x, int y)>> Contours = new();

    public static Glyph Notdef()
    {
        var box = new List<(int, int)> { (50, 0), (50, 700), (350, 700), (350, 0) };
        return new Glyph { Name = ".notdef", Advance = 400, Lsb = 50, Contours = { box } };
    }
}

// ---------------------------------------------------------------- Letter shapes

public static class Designs
{
    private static Stroke S(params (float, float)[] p) => new Stroke(p);
    private static Stroke Thin(params (float, float)[] p) => new Stroke(0.62f, p);
    private static Stroke Dab(params (float, float)[] p) => new Stroke(1.05f, p);

    public static List<(char, Stroke[])> All()
    {
        (float, float)[] ring = { (250, 720), (100, 630), (45, 350), (105, 85), (265, -10), (410, 100), (450, 360), (385, 615), (235, 722), (130, 670) };

        return new List<(char, Stroke[])>
        {
            ('A', new[] { S((30, -25), (135, 350), (245, 735)), S((235, 735), (340, 340), (440, -35)), Thin((35, 215), (250, 250), (500, 300)) }),
            ('B', new[] { S((80, 735), (72, 350), (62, -25)), S((10, 705), (240, 715), (365, 610), (300, 450), (95, 375)), S((85, 385), (300, 345), (415, 200), (320, 45), (15, 0)) }),
            ('C', new[] { S((430, 610), (310, 712), (140, 640), (55, 360), (115, 95), (280, -8), (455, 85)) }),
            ('D', new[] { S((85, 735), (75, 350), (65, -25)), S((-5, 705), (260, 690), (440, 490), (410, 210), (240, 30), (5, -5)) }),
            ('E', new[] { S((90, 720), (78, 350), (68, -10)), S((10, 695), (230, 705), (465, 730)), Thin((55, 365), (210, 375), (390, 395)), S((5, 5), (230, 15), (480, 40)) }),
            ('F', new[] { S((95, 725), (82, 350), (55, -40)), S((10, 695), (230, 705), (470, 730)), Thin((60, 350), (210, 360), (400, 380)) }),
            ('G', new[] { S((430, 615), (310, 712), (140, 640), (55, 360), (115, 95), (270, -8), (400, 70), (425, 290)), Thin((240, 300), (360, 310), (510, 320)) }),
            ('H', new[] { S((85, 735), (75, 350), (60, -30)), S((410, 740), (400, 350), (385, -30)), Thin((5, 325), (230, 350), (490, 395)) }),
            ('I', new[] { S((105, 740), (95, 350), (85, -35)) }),
            ('J', new[] { S((350, 735), (355, 330), (300, 70), (160, -15), (20, 105)) }),
            ('K', new[] { S((85, 735), (75, 350), (60, -30)), S((430, 735), (240, 480), (65, 320)), S((130, 375), (290, 190), (465, -35)) }),
            ('L', new[] { S((90, 735), (80, 350), (65, 0)), S((0, 15), (210, 15), (450, 35)) }),
            ('M', new[] { S((35, -30), (85, 350), (130, 735)), S((120, 735), (220, 450), (315, 170)), S((305, 170), (410, 450), (500, 740)), S((495, 740), (540, 350), (585, -35)) }),
            ('N', new[] { S((60, -30), (75, 350), (95, 735)), S((85, 735), (250, 350), (420, -30)), S((410, -30), (425, 350), (450, 745)) }),
            ('O', new[] { S(ring) }),
            ('P', new[] { S((90, 735), (78, 350), (55, -40)), S((5, 700), (270, 710), (405, 570), (300, 395), (85, 335)) }),
            ('Q', new[] { S(ring), Thin((255, 185), (360, 75), (495, -60)) }),
            ('R', new[] { S((90, 735), (78, 350), (60, -30)), S((5, 700), (270, 710), (395, 570), (290, 405), (90, 345)), S((165, 355), (310, 170), (470, -40)) }),
            ('S', new[] { S((425, 615), (300, 712), (130, 645), (115, 505), (265, 375), (405, 225), (355, 70), (200, -8), (25, 90)) }),
            ('T', new[] { S((-5, 680), (230, 700), (490, 730)), S((245, 720), (235, 350), (220, -40)) }),
            ('U', new[] { S((75, 735), (62, 220), (135, 40), (265, -12), (390, 65), (425, 250), (445, 745)) }),
            ('V', new[] { S((25, 740), (130, 350), (235, -35)), S((225, -35), (340, 350), (465, 750)) }),
            ('W', new[] { S((15, 740), (85, 350), (150, -35)), S((140, -35), (215, 240), (285, 520)), S((275, 520), (345, 240), (415, -35)), S((405, -35), (480, 350), (570, 750)) }),
            ('X', new[] { S((30, 740), (235, 350), (450, -40)), S((445, 740), (235, 350), (10, -40)) }),
            ('Y', new[] { S((25, 740), (135, 520), (255, 310)), S((470, 750), (300, 400), (125, -50)) }),
            ('Z', new[] { S((15, 685), (230, 700), (470, 725)), S((460, 715), (240, 360), (20, 10)), S((5, 5), (240, 15), (490, 40)) }),

            ('0', new[] { S((235, 720), (100, 630), (55, 350), (105, 85), (245, -10), (375, 100), (405, 360), (355, 615), (225, 722), (130, 670)) }),
            ('1', new[] { Thin((35, 530), (120, 620), (215, 735)), S((215, 735), (205, 350), (190, -35)) }),
            ('2', new[] { S((45, 570), (140, 705), (315, 690), (360, 520), (195, 250), (20, 10)), S((5, 5), (210, 15), (440, 40)) }),
            ('3', new[] { S((45, 640), (200, 720), (355, 625), (235, 410)), S((205, 415), (375, 285), (350, 95), (185, -10), (25, 85)) }),
            ('4', new[] { S((305, 735), (170, 480), (30, 225)), Thin((10, 225), (220, 240), (470, 265)), S((325, 500), (315, 230), (300, -35)) }),
            ('5', new[] { S((395, 705), (240, 695), (105, 690)), S((115, 700), (100, 540), (85, 390)), S((80, 405), (270, 435), (395, 285), (340, 80), (175, -10), (25, 75)) }),
            ('6', new[] { S((355, 715), (180, 530), (65, 270), (130, 60), (270, -10), (395, 130), (305, 295), (160, 290), (85, 185)) }),
            ('7', new[] { S((15, 685), (220, 700), (455, 725)), S((445, 715), (290, 350), (130, -45)) }),
            ('8', new[] { S((360, 620), (225, 722), (90, 625), (130, 480), (335, 285), (380, 120), (225, -10), (70, 120), (140, 300), (335, 475), (365, 630)) }),
            ('9', new[] { S((345, 470), (205, 395), (85, 525), (190, 712), (345, 640), (345, 390), (285, 170), (185, -45)) }),

            ('.', new[] { Dab((40, 50), (130, 15)) }),
            (',', new[] { Dab((115, 70), (30, -105)) }),
            ('!', new[] { S((115, 740), (100, 420), (85, 200)), Dab((50, 45), (130, 10)) }),
            ('?', new[] { S((40, 590), (170, 715), (340, 640), (300, 460), (190, 340), (170, 210)), Dab((120, 45), (200, 10)) }),
            (':', new[] { Dab((65, 450), (145, 415)), Dab((40, 75), (120, 40)) }),
            ('-', new[] { S((20, 290), (150, 305), (300, 325)) }),
            ('+', new[] { S((20, 310), (190, 325), (370, 345)), S((205, 505), (195, 320), (180, 135)) }),
            ('/', new[] { S((330, 760), (180, 350), (20, -60)) }),
            ('\'', new[] { Thin((75, 740), (60, 540)) }),
            ('%', new[] { Thin((110, 700), (45, 560), (110, 450), (190, 560), (120, 705)), S((470, 745), (260, 350), (50, -40)), Thin((400, 260), (335, 130), (400, 15), (480, 130), (410, 270)) })
        };
    }
}

// ---------------------------------------------------------------- Brush

public static class BrushPainter
{
    private const int GridWidth = 1100, GridHeight = 1250;
    private const int OriginX = 170, OriginY = 260;
    private const float Slant = 0.17f;       // forward lean
    private const float BrushWidth = 134f;   // full width of a stroke

    public static Glyph Paint(string name, Stroke[] strokes, int seed)
    {
        var random = new Random(seed);
        var mask = new bool[GridWidth, GridHeight];

        foreach (Stroke stroke in strokes)
            PaintStroke(mask, stroke, random);

        mask = Smooth(mask);

        var contours = Tracer.Trace(mask);
        int minX = int.MaxValue, maxX = int.MinValue;

        foreach (var contour in contours)
            foreach (var (x, _) in contour)
            {
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
            }

        // Every glyph starts at a small side bearing. The tight right edge lets letters nest.
        const int bearing = 18;
        var glyph = new Glyph { Name = name, Lsb = bearing, Advance = maxX - minX + bearing - 14 };

        foreach (var contour in contours)
            glyph.Contours.Add(contour.Select(p => (p.x - minX + bearing, p.y - OriginY)).ToList());

        return glyph;
    }

    private static void PaintStroke(bool[,] mask, Stroke stroke, Random random)
    {
        var path = Resample(stroke, random);
        int count = path.Count;
        float width = BrushWidth * stroke.Width;

        int bristles = (int)(width / 3.4f);
        float dryness = (0.72f + (float)random.NextDouble() * 0.33f) * (count < 150 ? 0.15f : 1f);

        for (int b = 0; b < bristles; b++)
        {
            float across = (b + 0.5f) / bristles * 2f - 1f + ((float)random.NextDouble() - 0.5f) * 0.05f;
            float edge = Math.Abs(across);

            // Each bristle lands and lifts at its own moment: rough start, frayed tail.
            float startAt = (float)random.NextDouble() * 0.035f * (0.3f + edge);
            float endAt = 1f - (float)Math.Pow(random.NextDouble(), 1.6) * 0.17f * dryness;

            // Lines of the brush run dry together, which draws the long streaks.
            float streak = Noise(b * 0.21f + seedOffset(random), 3.7f);
            float noiseSeed = (float)random.NextDouble() * 500f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                if (t < startAt || t > endAt) continue;

                // Ink thins along the stroke and toward the edges of the brush.
                float dry = Smooth01((t - 0.22f) / 0.78f) * dryness;
                float threshold = 0.1f + dry * 0.62f + edge * edge * 0.14f + (streak - 0.5f) * 0.8f * dry;
                float ink = Noise(i * 0.016f + noiseSeed, b * 0.3f) * 0.55f + Noise(i * 0.06f + noiseSeed, 7.1f) * 0.45f;

                if (ink < threshold * 0.8f) continue;

                float taper = Math.Min(1f, 0.72f + t * 6f) * (1f - Smooth01((t - 0.8f) / 0.2f) * 0.2f);
                var (x, y, nx, ny) = path[i];

                Stamp(mask, x + nx * across * width * 0.5f * taper, y + ny * across * width * 0.5f * taper, 2.5f);
            }
        }

        // A few flicks of ink thrown off the end of the stroke.
        int flecks = random.Next(0, 4);

        for (int f = 0; f < flecks; f++)
        {
            var (x, y, nx, ny) = path[count - 1 - random.Next(0, count / 6)];
            var (px, py, _, _) = path[Math.Max(0, count - 12)];
            var (qx, qy, _, _) = path[count - 1];

            float dirX = qx - px, dirY = qy - py;
            float length = MathF.Max(MathF.Sqrt(dirX * dirX + dirY * dirY), 0.01f);
            float reach = 18f + (float)random.NextDouble() * 62f;
            float side = ((float)random.NextDouble() - 0.5f) * width * 1.3f;

            Stamp(mask,
                x + dirX / length * reach + nx * side,
                y + dirY / length * reach + ny * side,
                3f + (float)random.NextDouble() * 5f);
        }
    }

    private static float seedOffset(Random random) => (float)random.NextDouble() * 90f;

    private static float Smooth01(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    // Condenses the letter, makes it taller, and carries each stroke past its end,
    // the way a fast brush overshoots. Short dabs keep their length.
    private static (float x, float y)[] Overshoot((float x, float y)[] source)
    {
        var points = source.Select(p => (x: p.x * 0.9f, y: p.y * 1.14f)).ToArray();

        var first = points[0];
        var second = points[1];
        var last = points[^1];
        var beforeLast = points[^2];

        float startX = first.x - second.x, startY = first.y - second.y;
        float endX = last.x - beforeLast.x, endY = last.y - beforeLast.y;
        float startLength = MathF.Sqrt(startX * startX + startY * startY);
        float endLength = MathF.Sqrt(endX * endX + endY * endY);

        if (startLength + endLength < 260f) return points;

        points[0] = (first.x + startX / startLength * 12f, first.y + startY / startLength * 12f);
        points[^1] = (last.x + endX / endLength * 52f, last.y + endY / endLength * 52f);
        return points;
    }

    // A smooth curve through the stroke's points, sampled evenly, with a small hand wobble.
    private static List<(float x, float y, float nx, float ny)> Resample(Stroke stroke, Random random)
    {
        var points = Overshoot(stroke.Points);
        float jitterX = ((float)random.NextDouble() - 0.5f) * 14f;
        float jitterY = ((float)random.NextDouble() - 0.5f) * 14f;
        float wobbleSeed = (float)random.NextDouble() * 300f;

        var raw = new List<(float x, float y)>();

        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = points[Math.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(i + 2, points.Length - 1)];

            for (int s = 0; s < 40; s++)
            {
                float t = s / 40f, t2 = t * t, t3 = t2 * t;

                raw.Add((
                    0.5f * (2 * p1.x + (p2.x - p0.x) * t + (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * t2 + (3 * p1.x - p0.x - 3 * p2.x + p3.x) * t3),
                    0.5f * (2 * p1.y + (p2.y - p0.y) * t + (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * t2 + (3 * p1.y - p0.y - 3 * p2.y + p3.y) * t3)));
            }
        }

        raw.Add(points[^1]);

        var even = new List<(float x, float y)> { raw[0] };
        float carried = 0f;

        for (int i = 1; i < raw.Count; i++)
        {
            float dx = raw[i].x - raw[i - 1].x, dy = raw[i].y - raw[i - 1].y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < 0.0001f) continue;

            float at = 1.5f - carried;

            while (at <= length)
            {
                even.Add((raw[i - 1].x + dx * at / length, raw[i - 1].y + dy * at / length));
                at += 1.5f;
            }

            carried = length - (at - 1.5f);
        }

        var result = new List<(float, float, float, float)>(even.Count);

        for (int i = 0; i < even.Count; i++)
        {
            var before = even[Math.Max(i - 4, 0)];
            var after = even[Math.Min(i + 4, even.Count - 1)];
            float dx = after.x - before.x, dy = after.y - before.y;
            float length = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 0.001f);
            float wobble = (Noise(i * 0.012f + wobbleSeed, 1.3f) - 0.5f) * 13f;

            float nx = -dy / length, ny = dx / length;
            float x = even[i].x + jitterX + nx * wobble;
            float y = even[i].y + jitterY + ny * wobble;

            // Lean the whole letter forward.
            result.Add((x + y * Slant + OriginX, y + OriginY, nx, ny));
        }

        return result;
    }

    private static void Stamp(bool[,] mask, float cx, float cy, float radius)
    {
        int minX = Math.Max((int)(cx - radius), 0), maxX = Math.Min((int)(cx + radius) + 1, GridWidth - 1);
        int minY = Math.Max((int)(cy - radius), 0), maxY = Math.Min((int)(cy + radius) + 1, GridHeight - 1);
        float r2 = radius * radius;

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            if (dx * dx + dy * dy <= r2) mask[x, y] = true;
        }
    }

    // Removes pixel-sized crumbs and rounds the stair steps before tracing.
    private static bool[,] Smooth(bool[,] mask)
    {
        var result = new bool[GridWidth, GridHeight];

        for (int y = 2; y < GridHeight - 2; y++)
        for (int x = 2; x < GridWidth - 2; x++)
        {
            int sum = 0;

            for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                if (mask[x + dx, y + dy]) sum++;

            result[x, y] = sum >= 13;
        }

        return result;
    }

    // Small value noise, 0-1.
    private static float Noise(float x, float y)
    {
        int ix = (int)MathF.Floor(x), iy = (int)MathF.Floor(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);

        float a = Hash(ix, iy), b = Hash(ix + 1, iy), c = Hash(ix, iy + 1), d = Hash(ix + 1, iy + 1);

        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    private static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }
}

// ---------------------------------------------------------------- Mask to outlines

public static class Tracer
{
    private const float Tolerance = 1.4f;
    private const float MinArea = 45f;

    // Walks pixel edges with the ink on the right, so outer shapes run clockwise and
    // holes counter-clockwise, which is the winding TrueType expects.
    public static List<List<(int x, int y)>> Trace(bool[,] mask)
    {
        int width = mask.GetLength(0), height = mask.GetLength(1);
        var edges = new Dictionary<(int, int), List<(int, int)>>();

        void Edge((int, int) from, (int, int) to)
        {
            if (!edges.TryGetValue(from, out var list)) edges[from] = list = new List<(int, int)>(1);
            list.Add(to);
        }

        bool Filled(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && mask[x, y];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (!mask[x, y]) continue;

            if (!Filled(x, y + 1)) Edge((x, y + 1), (x + 1, y + 1));
            if (!Filled(x + 1, y)) Edge((x + 1, y + 1), (x + 1, y));
            if (!Filled(x, y - 1)) Edge((x + 1, y), (x, y));
            if (!Filled(x - 1, y)) Edge((x, y), (x, y + 1));
        }

        var contours = new List<List<(int x, int y)>>();

        while (edges.Count > 0)
        {
            var start = edges.Keys.First();
            var loop = new List<(int x, int y)>();
            var current = start;

            do
            {
                loop.Add(current);
                var outgoing = edges[current];
                var next = outgoing[^1];
                outgoing.RemoveAt(outgoing.Count - 1);
                if (outgoing.Count == 0) edges.Remove(current);
                current = next;
            }
            while (current != start && edges.ContainsKey(current));

            if (Math.Abs(Area(loop)) < MinArea) continue;

            var simple = SimplifyClosed(loop);
            if (simple.Count >= 3) contours.Add(simple);
        }

        return contours;
    }

    private static float Area(List<(int x, int y)> loop)
    {
        long sum = 0;

        for (int i = 0; i < loop.Count; i++)
        {
            var a = loop[i];
            var b = loop[(i + 1) % loop.Count];
            sum += a.x * b.y - b.x * a.y;
        }

        return sum * 0.5f;
    }

    private static List<(int x, int y)> SimplifyClosed(List<(int x, int y)> loop)
    {
        int far = 0;
        long best = -1;

        for (int i = 0; i < loop.Count; i++)
        {
            long dx = loop[i].x - loop[0].x, dy = loop[i].y - loop[0].y;
            if (dx * dx + dy * dy > best) { best = dx * dx + dy * dy; far = i; }
        }

        var keep = new bool[loop.Count + 1];
        keep[0] = keep[far] = keep[loop.Count] = true;

        var closed = new List<(int x, int y)>(loop) { loop[0] };
        Reduce(closed, 0, far, keep);
        Reduce(closed, far, loop.Count, keep);

        var result = new List<(int, int)>();
        for (int i = 0; i < loop.Count; i++)
            if (keep[i]) result.Add(loop[i]);

        return result;
    }

    private static void Reduce(List<(int x, int y)> points, int from, int to, bool[] keep)
    {
        var stack = new Stack<(int, int)>();
        stack.Push((from, to));

        while (stack.Count > 0)
        {
            var (a, b) = stack.Pop();
            if (b - a < 2) continue;

            float ax = points[a].x, ay = points[a].y;
            float dx = points[b].x - ax, dy = points[b].y - ay;
            float length = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 0.001f);

            float furthest = 0f;
            int index = -1;

            for (int i = a + 1; i < b; i++)
            {
                float distance = MathF.Abs((points[i].x - ax) * dy - (points[i].y - ay) * dx) / length;
                if (distance > furthest) { furthest = distance; index = i; }
            }

            if (furthest > Tolerance)
            {
                keep[index] = true;
                stack.Push((a, index));
                stack.Push((index, b));
            }
        }
    }
}

// ---------------------------------------------------------------- TrueType writer

public class Writer
{
    public readonly MemoryStream Stream = new();
    public void U8(int v) => Stream.WriteByte((byte)v);
    public void U16(int v) { U8(v >> 8); U8(v); }
    public void I16(int v) => U16((ushort)(short)v);
    public void U32(uint v) { U16((int)(v >> 16)); U16((int)(v & 0xFFFF)); }
    public void I64(long v) { U32((uint)(v >> 32)); U32((uint)v); }
    public void Bytes(byte[] b) => Stream.Write(b, 0, b.Length);
    public void Tag(string t) => Bytes(Encoding.ASCII.GetBytes(t));
    public byte[] ToArray() => Stream.ToArray();
    public void Align(int n) { while (Stream.Length % n != 0) U8(0); }
}

public static class TrueType
{
    public static byte[] Build(List<Glyph> glyphs, SortedDictionary<int, int> cmap, string family)
    {
        var glyf = new Writer();
        var offsets = new List<uint>();

        int xMin = 0, yMin = 0, xMax = 0, yMax = 0, maxPoints = 0, maxContours = 0;

        foreach (Glyph glyph in glyphs)
        {
            offsets.Add((uint)glyf.Stream.Length);
            if (glyph.Contours.Count == 0) continue;

            int gx0 = glyph.Contours.Min(c => c.Min(p => p.x));
            int gx1 = glyph.Contours.Max(c => c.Max(p => p.x));
            int gy0 = glyph.Contours.Min(c => c.Min(p => p.y));
            int gy1 = glyph.Contours.Max(c => c.Max(p => p.y));

            xMin = Math.Min(xMin, gx0); xMax = Math.Max(xMax, gx1);
            yMin = Math.Min(yMin, gy0); yMax = Math.Max(yMax, gy1);

            maxPoints = Math.Max(maxPoints, glyph.Contours.Sum(c => c.Count));
            maxContours = Math.Max(maxContours, glyph.Contours.Count);

            WriteGlyph(glyf, glyph, gx0, gy0, gx1, gy1);
            glyf.Align(4);
        }

        offsets.Add((uint)glyf.Stream.Length);

        var loca = new Writer();
        foreach (uint offset in offsets) loca.U32(offset);

        var head = new Writer();
        head.U32(0x00010000); head.U32(0x00010000);
        head.U32(0);                    // checkSumAdjustment, patched at the end
        head.U32(0x5F0F3CF5);
        head.U16(0x000B);
        head.U16(1000);
        long seconds = (long)(DateTime.UtcNow - new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        head.I64(seconds); head.I64(seconds);
        head.I16(xMin); head.I16(yMin); head.I16(xMax); head.I16(yMax);
        head.U16(0); head.U16(8); head.I16(2);
        head.I16(1);                    // long loca
        head.I16(0);

        const int ascent = 860, descent = -240;

        var hhea = new Writer();
        hhea.U32(0x00010000);
        hhea.I16(ascent); hhea.I16(descent); hhea.I16(0);
        hhea.U16(glyphs.Max(g => g.Advance));
        hhea.I16(0); hhea.I16(0); hhea.I16(xMax);
        hhea.I16(1); hhea.I16(0); hhea.I16(0);
        for (int i = 0; i < 5; i++) hhea.I16(0);
        hhea.U16(glyphs.Count);

        var hmtx = new Writer();
        foreach (Glyph glyph in glyphs) { hmtx.U16(glyph.Advance); hmtx.I16(glyph.Lsb); }

        var maxp = new Writer();
        maxp.U32(0x00010000);
        maxp.U16(glyphs.Count);
        maxp.U16(maxPoints); maxp.U16(maxContours);
        maxp.U16(0); maxp.U16(0);
        maxp.U16(1); maxp.U16(0); maxp.U16(0); maxp.U16(0); maxp.U16(0); maxp.U16(0); maxp.U16(0); maxp.U16(0);
        maxp.U16(0); maxp.U16(0);

        var os2 = new Writer();
        os2.U16(3);
        os2.I16((int)glyphs.Average(g => g.Advance));
        os2.U16(400); os2.U16(5); os2.U16(0);
        os2.I16(650); os2.I16(700); os2.I16(0); os2.I16(140);
        os2.I16(650); os2.I16(700); os2.I16(0); os2.I16(480);
        os2.I16(50); os2.I16(250); os2.I16(0);
        for (int i = 0; i < 10; i++) os2.U8(0);
        os2.U32(1); os2.U32(0); os2.U32(0); os2.U32(0);
        os2.Tag("NONE");
        os2.U16(0x0040);                // regular
        os2.U16(cmap.Keys.Min()); os2.U16(Math.Min(cmap.Keys.Max(), 0xFFFF));
        os2.I16(ascent); os2.I16(descent); os2.I16(120);
        os2.U16(Math.Max(ascent, yMax)); os2.U16(Math.Max(-descent, -yMin));
        os2.U32(1); os2.U32(0);
        os2.I16(350); os2.I16(700);
        os2.U16(0); os2.U16(32); os2.U16(0);

        var post = new Writer();
        post.U32(0x00030000);
        post.U32(0);
        post.I16(-100); post.I16(50);
        post.U32(0); post.U32(0); post.U32(0); post.U32(0); post.U32(0);

        var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["OS/2"] = os2.ToArray(),
            ["cmap"] = BuildCmap(cmap),
            ["glyf"] = glyf.ToArray(),
            ["head"] = head.ToArray(),
            ["hhea"] = hhea.ToArray(),
            ["hmtx"] = hmtx.ToArray(),
            ["loca"] = loca.ToArray(),
            ["maxp"] = maxp.ToArray(),
            ["name"] = BuildName(family),
            ["post"] = post.ToArray()
        };

        return Assemble(tables);
    }

    private static void WriteGlyph(Writer w, Glyph glyph, int x0, int y0, int x1, int y1)
    {
        w.I16(glyph.Contours.Count);
        w.I16(x0); w.I16(y0); w.I16(x1); w.I16(y1);

        int end = -1;
        foreach (var contour in glyph.Contours)
        {
            end += contour.Count;
            w.U16(end);
        }

        w.U16(0);   // no instructions

        var points = glyph.Contours.SelectMany(c => c).ToList();
        var flags = new Writer();
        var xs = new Writer();
        var ys = new Writer();

        int lastX = 0, lastY = 0;

        foreach (var (x, y) in points)
        {
            int dx = x - lastX, dy = y - lastY;
            int flag = 0x01;

            if (dx == 0) flag |= 0x10;
            else if (Math.Abs(dx) < 256) { flag |= 0x02; if (dx > 0) flag |= 0x10; xs.U8(Math.Abs(dx)); }
            else xs.I16(dx);

            if (dy == 0) flag |= 0x20;
            else if (Math.Abs(dy) < 256) { flag |= 0x04; if (dy > 0) flag |= 0x20; ys.U8(Math.Abs(dy)); }
            else ys.I16(dy);

            flags.U8(flag);
            lastX = x; lastY = y;
        }

        w.Bytes(flags.ToArray());
        w.Bytes(xs.ToArray());
        w.Bytes(ys.ToArray());
    }

    // Format 4: runs of consecutive characters mapped to consecutive glyphs.
    private static byte[] BuildCmap(SortedDictionary<int, int> map)
    {
        var segments = new List<(int start, int end, int glyph)>();

        foreach (var (code, glyph) in map)
        {
            if (segments.Count > 0)
            {
                var last = segments[^1];
                if (code == last.end + 1 && glyph == last.glyph + (code - last.start))
                {
                    segments[^1] = (last.start, code, last.glyph);
                    continue;
                }
            }

            segments.Add((code, code, glyph));
        }

        segments.Add((0xFFFF, 0xFFFF, 0));

        int segCount = segments.Count;
        int searchRange = 2 * (1 << (int)Math.Floor(Math.Log2(segCount)));

        var sub = new Writer();
        sub.U16(4);
        sub.U16(16 + segCount * 8);
        sub.U16(0);
        sub.U16(segCount * 2);
        sub.U16(searchRange);
        sub.U16((int)Math.Floor(Math.Log2(segCount)));
        sub.U16(segCount * 2 - searchRange);
        foreach (var s in segments) sub.U16(s.end);
        sub.U16(0);
        foreach (var s in segments) sub.U16(s.start);
        foreach (var s in segments) sub.U16(s.start == 0xFFFF ? 1 : (s.glyph - s.start) & 0xFFFF);
        foreach (var _ in segments) sub.U16(0);

        var w = new Writer();
        w.U16(0); w.U16(2);
        w.U16(0); w.U16(3); w.U32(20);
        w.U16(3); w.U16(1); w.U32(20);
        w.Bytes(sub.ToArray());
        return w.ToArray();
    }

    private static byte[] BuildName(string family)
    {
        var entries = new (int id, string text)[]
        {
            (0, "Original font generated for Endless Faller."),
            (1, family),
            (2, "Regular"),
            (3, family + " 1.0"),
            (4, family),
            (5, "Version 1.0"),
            (6, family.Replace(" ", "") + "-Regular")
        };

        var strings = new Writer();
        var w = new Writer();
        w.U16(0); w.U16(entries.Length * 2); w.U16(6 + 12 * entries.Length * 2);

        // Records must be sorted: Mac platform first, then Windows.
        foreach (var (id, text) in entries)
        {
            byte[] mac = Encoding.ASCII.GetBytes(text);
            w.U16(1); w.U16(0); w.U16(0); w.U16(id); w.U16(mac.Length); w.U16((int)strings.Stream.Length);
            strings.Bytes(mac);
        }

        foreach (var (id, text) in entries)
        {
            byte[] wide = Encoding.BigEndianUnicode.GetBytes(text);
            w.U16(3); w.U16(1); w.U16(0x409); w.U16(id); w.U16(wide.Length); w.U16((int)strings.Stream.Length);
            strings.Bytes(wide);
        }

        w.Bytes(strings.ToArray());
        return w.ToArray();
    }

    private static uint Checksum(byte[] data)
    {
        uint sum = 0;
        int padded = (data.Length + 3) & ~3;

        for (int i = 0; i < padded; i += 4)
        {
            uint word = 0;
            for (int j = 0; j < 4; j++)
                word = (word << 8) | (i + j < data.Length ? data[i + j] : (uint)0);
            sum += word;
        }

        return sum;
    }

    private static byte[] Assemble(SortedDictionary<string, byte[]> tables)
    {
        int count = tables.Count;
        int entrySelector = (int)Math.Floor(Math.Log2(count));
        int searchRange = (1 << entrySelector) * 16;

        var w = new Writer();
        w.U32(0x00010000);
        w.U16(count); w.U16(searchRange); w.U16(entrySelector); w.U16(count * 16 - searchRange);

        uint offset = (uint)(12 + 16 * count);
        int headOffset = 0;

        foreach (var (tag, data) in tables)
        {
            w.Tag(tag);
            w.U32(Checksum(data));
            w.U32(offset);
            w.U32((uint)data.Length);
            if (tag == "head") headOffset = (int)offset;
            offset += (uint)((data.Length + 3) & ~3);
        }

        foreach (var (_, data) in tables)
        {
            w.Bytes(data);
            w.Align(4);
        }

        byte[] file = w.ToArray();
        uint adjustment = 0xB1B0AFBA - Checksum(file);
        file[headOffset + 8] = (byte)(adjustment >> 24);
        file[headOffset + 9] = (byte)(adjustment >> 16);
        file[headOffset + 10] = (byte)(adjustment >> 8);
        file[headOffset + 11] = (byte)adjustment;
        return file;
    }
}
