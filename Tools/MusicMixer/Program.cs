// Takes a generated music track and turns it into a game loop: finds its tempo, beat and
// chords, plays synthesised drums and guitar along with it, adds glitch edits and a
// rollercoaster rise and drop, and joins the end to the start.
//
// Usage: dotnet run -- <input.mp3> <output.wav> <bpm> [menu-loop.wav]
using NLayer;

static partial class Program
{
    const int Hop = 512;
    const int Seed = 174;

    static int rate;
    static readonly Random rng = new Random(Seed);

    static void Main(string[] args)
    {
        float[] srcL, srcR;
        Decode(args[0], out srcL, out srcR);

        var mono = new float[srcL.Length];
        for (int i = 0; i < mono.Length; i++) mono[i] = 0.5f * (srcL[i] + srcR[i]);

        float seconds = mono.Length / (float)rate;
        Console.WriteLine($"decoded {seconds:0.00}s at {rate} Hz");

        // ---------------------------------------------------------- Listen to the track
        float[] onset = Flux(mono);
        float[] lowOnset = Flux(LowPass(mono, 150f));
        float[] highOnset = Flux(HighPass(mono, 2500f));

        double bpm = args.Length > 2 ? double.Parse(args[2]) : FindTempo(onset);
        double beat = 60.0 / bpm;
        double bar = beat * 4.0;
        double phase = FindPhase(onset, beat);

        // Whichever beat has the kick, followed by snares on the next and third after it.
        int downbeat = 0;
        double bestScore = double.MinValue;

        for (int k = 0; k < 4; k++)
        {
            double score = 0.0;

            for (double t = phase + k * beat; t < seconds; t += bar)
                score += At(lowOnset, t) + At(highOnset, t + beat) + At(highOnset, t + beat * 3.0);

            if (score > bestScore) { bestScore = score; downbeat = k; }
        }

        double firstBar = phase + downbeat * beat;
        int bars = (int)((seconds - firstBar) / bar);

        Console.WriteLine($"tempo {bpm:0.00} BPM, first bar at {firstBar:0.000}s, {bars} whole bars");

        // The track may drift a little, so each bar line is nudged onto the real hits.
        var offsets = new double[bars + 1];

        for (int b = 0; b <= bars; b++)
        {
            double best = double.MinValue;

            for (double d = -0.035; d <= 0.0351; d += 0.005)
            {
                double score = 0.0;
                for (int i = 0; i < 4; i++) score += At(onset, firstBar + (b + i / 4.0) * bar + d);

                if (score > best) { best = score; offsets[b] = d; }
            }
        }

        offsets = Median3(offsets);
        Console.WriteLine($"drift between {offsets.Min() * 1000:0} and {offsets.Max() * 1000:0} ms");

        int loopBars = (bars - 1) / 4 * 4;
        var barSample = new int[loopBars + 1];

        for (int b = 0; b <= loopBars; b++)
            barSample[b] = (int)Math.Round((firstBar + b * bar + offsets[b]) * rate);

        int start = barSample[0];
        int length = barSample[loopBars] - start;

        for (int b = 0; b <= loopBars; b++) barSample[b] -= start;

        Console.WriteLine($"loop is {loopBars} bars, {length / (float)rate:0.00}s");

        int key;
        bool isMinor;
        int[] roots = FindChords(mono, start, barSample, out key, out isMinor);

        Console.WriteLine($"key {Note(key)} {(isMinor ? "minor" : "major")}");
        Console.WriteLine("chords " + string.Join(" ", roots.Select(Note)));

        int beatSamples = (int)(beat * rate);

        // ---------------------------------------------------------- The original, as a loop
        var bedL = new float[length];
        var bedR = new float[length];
        int fade = beatSamples;

        for (int i = 0; i < length; i++)
        {
            bedL[i] = srcL[start + i];
            bedR[i] = srcR[start + i];
        }

        // What plays after the loop's end fades under its start, so the join has no seam.
        for (int i = 0; i < fade; i++)
        {
            double t = i / (double)fade * Math.PI * 0.5;
            float inside = (float)Math.Sin(t);
            float outside = (float)Math.Cos(t);

            bedL[i] = bedL[i] * inside + srcL[start + length + i] * outside;
            bedR[i] = bedR[i] * inside + srcR[start + length + i] * outside;
        }

        float bedLevel = Rms(bedL, bedR);

        // The bed thins out over the climb, so the drop lands harder.
        for (int b = 18; b + 2 <= loopBars; b += 20)
        {
            ClimbFilter(bedL, barSample[b], barSample[b + 2]);
            ClimbFilter(bedR, barSample[b], barSample[b + 2]);
        }

        // ---------------------------------------------------------- Drums and guitar
        var drumsL = new float[length];
        var drumsR = new float[length];
        var duck = new float[length];
        PlayDrums(drumsL, drumsR, duck, barSample, loopBars);

        var guitarL = new float[length];
        var guitarR = new float[length];
        PlayGuitar(guitarL, guitarR, barSample, loopBars, roots);

        Scale(drumsL, drumsR, bedLevel * 0.7f / Rms(drumsL, drumsR));
        Scale(guitarL, guitarR, bedLevel * 0.45f / Rms(guitarL, guitarR));

        var mixL = new float[length];
        var mixR = new float[length];

        for (int i = 0; i < length; i++)
        {
            float bed = 0.8f * (1f - 0.3f * duck[i]);

            mixL[i] = bedL[i] * bed + drumsL[i] + guitarL[i];
            mixR[i] = bedR[i] * bed + drumsR[i] + guitarR[i];
        }

        // ---------------------------------------------------------- Glitches
        Glitch(mixL, mixR, barSample, loopBars);

        // ---------------------------------------------------------- Melody
        // Added after the glitches, so it floats on untouched while the track breaks up.
        var melodyL = new float[length];
        var melodyR = new float[length];
        PlayMelody(melodyL, melodyR, barSample, loopBars, roots, key, isMinor, 0.12f);
        AddSpace(melodyL, melodyR, beatSamples);
        Scale(melodyL, melodyR, bedLevel * 0.85f / Rms(melodyL, melodyR));

        for (int i = 0; i < length; i++)
        {
            mixL[i] += melodyL[i];
            mixR[i] += melodyR[i];
        }

        if (args.Length > 3) RenderMenuLoop(args[3], barSample, roots, key, isMinor, beatSamples);

        // ---------------------------------------------------------- Master
        float peak = 0f;
        for (int i = 0; i < length; i++) peak = Math.Max(peak, Math.Max(Math.Abs(mixL[i]), Math.Abs(mixR[i])));

        float drive = 1.25f / peak * 1.4f;
        float top = (float)Math.Tanh(peak * drive);

        for (int i = 0; i < length; i++)
        {
            mixL[i] = 0.95f * (float)Math.Tanh(mixL[i] * drive) / top;
            mixR[i] = 0.95f * (float)Math.Tanh(mixR[i] * drive) / top;
        }

        // A millisecond of fade either side of the join, so it can never click.
        int guard = rate / 1000;

        for (int i = 0; i < guard; i++)
        {
            float gain = i / (float)guard;

            mixL[i] *= gain;
            mixR[i] *= gain;
            mixL[length - 1 - i] *= gain;
            mixR[length - 1 - i] *= gain;
        }

        WriteWav(args[1], mixL, mixR);

        // ---------------------------------------------------------- Checks
        Console.WriteLine($"levels: bed {bedLevel:0.000} drums {Rms(drumsL, drumsR):0.000} guitar {Rms(guitarL, guitarR):0.000} melody {Rms(melodyL, melodyR):0.000} master {Rms(mixL, mixR):0.000}");

        int edge = rate / 10;
        Console.WriteLine($"join: last sample {mixL[length - 1]:0.000}, first {mixL[0]:0.000}, last 0.1s level {RmsOf(mixL, length - edge, edge):0.000}, first 0.1s level {RmsOf(mixL, 0, edge):0.000}");

        var shape = new List<string>();

        for (int b = 0; b + 4 <= loopBars; b += 4)
            shape.Add(RmsOf(mixL, barSample[b], barSample[b + 4] - barSample[b]).ToString("0.00"));

        Console.WriteLine("level every 4 bars: " + string.Join(" ", shape));
    }

    // ------------------------------------------------------------ Drums

    static void PlayDrums(float[] left, float[] right, float[] duck, int[] barSample, int bars)
    {
        float[] kick = MakeKick();
        float[] snare = MakeSnare();
        float[] hat = MakeHat(0.03f);
        float[] openHat = MakeHat(0.14f);
        float[] crash = MakeCrash();
        float[] boom = MakeBoom();
        float[] floorTom = MakeFloorTom();
        float[] ride = MakeRide();

        int Pos(int b, double beat) => barSample[b] + (int)(beat / 4.0 * (barSample[b + 1] - barSample[b]));

        void Kick(int b, double beat, float gain)
        {
            int at = Pos(b, beat);
            Add(left, right, kick, at, gain, 0f);

            int fall = rate / 8;
            for (int i = 0; i < fall; i++)
            {
                int w = (at + i) % duck.Length;
                duck[w] = Math.Max(duck[w], 1f - i / (float)fall);
            }
        }

        for (int b = 0; b < bars; b++)
        {
            int inWave = b % 20;
            bool isClimb = inWave >= 18;

            if (inWave == 0)
            {
                Add(left, right, crash, Pos(b, 0.0), 0.9f, 0f);
                Add(left, right, boom, Pos(b, 0.0), 1.1f, 0f);
            }
            else if (b % 4 == 0)
            {
                Add(left, right, crash, Pos(b, 0.0), 0.55f, -0.3f);
            }

            if (!isClimb)
            {
                // Two-step breakbeat, with a busier second half to every other bar.
                Kick(b, 0.0, 1f);
                Kick(b, 2.5, 0.9f);
                if (b % 2 == 1) Kick(b, 3.5, 0.75f);

                Add(left, right, snare, Pos(b, 1.0), 0.9f, 0f);
                Add(left, right, snare, Pos(b, 3.0), 1f, 0f);
                Add(left, right, snare, Pos(b, 1.75), 0.22f, 0.1f);

                for (int i = 0; i < 8; i++)
                {
                    float[] sound = i % 4 == 2 ? openHat : hat;
                    float gain = i % 2 == 0 ? 0.2f : 0.12f;

                    Add(left, right, sound, Pos(b, i / 2.0), gain, 0.25f);
                }

                for (int i = 0; i < 4; i++)
                    Add(left, right, ride, Pos(b, i), i % 2 == 0 ? 0.5f : 0.35f, -0.25f);

                // Floor tom answers the snare, with a pair of hits on the even bars.
                Add(left, right, floorTom, Pos(b, 1.5), 0.85f, 0.2f);
                if (b % 2 == 0) Add(left, right, floorTom, Pos(b, 3.5), 0.95f, 0.2f);

                continue;
            }

            // The climb: the snare speeds up and gets louder, then all stops on the last beat.
            if (inWave == 18)
            {
                Kick(b, 0.0, 1f);

                for (int i = 0; i < 8; i++)
                {
                    Add(left, right, snare, Pos(b, i / 2.0), 0.4f + i * 0.03f, 0f);
                    Add(left, right, floorTom, Pos(b, i / 2.0 + 0.25), 0.55f + i * 0.04f, 0.2f);
                }
            }
            else
            {
                Kick(b, 0.0, 1f);

                for (int i = 0; i < 12; i++)
                    Add(left, right, snare, Pos(b, i / 4.0), 0.6f + i * 0.035f, i % 2 == 0 ? -0.15f : 0.15f);
            }
        }
    }

    static float[] MakeKick()
    {
        int n = (int)(0.22f * rate);
        var s = new float[n];
        double phase = 0.0;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            double freq = 48.0 + 130.0 * Math.Exp(-t * 34.0);

            phase += 2.0 * Math.PI * freq / rate;

            double body = Math.Sin(phase) * Math.Exp(-t * 11.0);
            double click = t < 0.004 ? (rng.NextDouble() * 2.0 - 1.0) * 0.6 : 0.0;

            s[i] = (float)Math.Tanh((body + click) * 2.2);
        }

        return s;
    }

    static float[] MakeSnare()
    {
        int n = (int)(0.2f * rate);
        var s = new float[n];
        float last = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float bright = noise - last;
            last = noise;

            double tone = (Math.Sin(2.0 * Math.PI * 190.0 * t) + 0.6 * Math.Sin(2.0 * Math.PI * 330.0 * t)) * Math.Exp(-t * 28.0);

            s[i] = (float)Math.Tanh((tone * 0.7 + bright * 0.8 * Math.Exp(-t * 19.0)) * 1.6);
        }

        return s;
    }

    static float[] MakeHat(float decay)
    {
        int n = (int)(decay * 3f * rate);
        var s = new float[n];
        float last = 0f;

        for (int i = 0; i < n; i++)
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            s[i] = (noise - last) * 0.5f * (float)Math.Exp(-i / (float)rate / decay);
            last = noise;
        }

        return s;
    }

    // A big drum tuned low, with a short thump of stick noise on the skin.
    static float[] MakeFloorTom()
    {
        int n = (int)(0.42f * rate);
        var s = new float[n];
        double phase = 0.0;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;

            phase += 2.0 * Math.PI * (88.0 + 62.0 * Math.Exp(-t * 26.0)) / rate;

            double body = Math.Sin(phase) * Math.Exp(-t * 7.5);
            double skin = (rng.NextDouble() * 2.0 - 1.0) * 0.25 * Math.Exp(-t * 60.0);

            s[i] = (float)Math.Tanh((body + skin) * 1.8);
        }

        return s;
    }

    // Metal: a stack of unrelated high tones over a little noise, ringing on.
    static float[] MakeRide()
    {
        int n = (int)(0.7f * rate);
        var s = new float[n];
        double[] tones = { 2960.0, 3714.0, 4811.0, 5417.0, 6683.0, 8127.0 };
        float last = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            double metal = 0.0;

            foreach (double tone in tones) metal += Math.Sin(2.0 * Math.PI * tone * t);

            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float hiss = noise - last;
            last = noise;

            s[i] = (float)((metal * 0.09 + hiss * 0.12) * Math.Exp(-t * 5.5) + hiss * 0.25 * Math.Exp(-t * 70.0));
        }

        return s;
    }

    static float[] MakeCrash()
    {
        int n = (int)(1.6f * rate);
        var s = new float[n];
        float last = 0f;

        for (int i = 0; i < n; i++)
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            s[i] = (noise - last) * 0.5f * (float)Math.Exp(-i / (float)rate * 2.6f);
            last = noise;
        }

        return s;
    }

    // The low sweep under each drop.
    static float[] MakeBoom()
    {
        int n = (int)(1.1f * rate);
        var s = new float[n];
        double phase = 0.0;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            phase += 2.0 * Math.PI * (34.0 + 70.0 * Math.Exp(-t * 7.0)) / rate;
            s[i] = (float)(Math.Sin(phase) * Math.Exp(-t * 3.2));
        }

        return s;
    }

    // ------------------------------------------------------------ Guitar

    static void PlayGuitar(float[] left, float[] right, int[] barSample, int bars, int[] roots)
    {
        var cache = new Dictionary<(int, bool, int), float[]>();

        float[] Get(int root, bool muted, int side)
        {
            var key = (root, muted, side);

            if (!cache.TryGetValue(key, out float[] sound))
            {
                sound = MakeChord(root, muted, side == 0 ? 4f : -4f);
                cache[key] = sound;
            }

            return sound;
        }

        void Hit(int b, double beat, bool muted, float gain)
        {
            int at = barSample[b] + (int)(beat / 4.0 * (barSample[b + 1] - barSample[b]));
            int root = roots[b];
            int late = rate / 120;

            AddOne(left, Get(root, muted, 0), at, gain);
            AddOne(right, Get(root, muted, 1), at + late, gain);
        }

        // Palm-muted gallop: the pushes on the off-beats are what drive it forward.
        double[] riff = { 0.0, 0.5, 1.0, 1.5, 1.75, 2.5, 3.0, 3.5 };

        for (int b = 0; b < bars; b++)
        {
            int inWave = b % 20;

            if (inWave == 19)
            {
                // Held open chord under the top of the climb, stopping for the drop.
                Hit(b, 0.0, false, 1f);
                Hit(b, 1.5, false, 0.9f);
                continue;
            }

            if (inWave == 18)
            {
                for (int i = 0; i < 16; i++) Hit(b, i / 4.0, true, 0.55f + i * 0.03f);
                continue;
            }

            if (inWave == 0 || inWave == 10)
            {
                Hit(b, 0.0, false, 1f);
                for (int i = 3; i < riff.Length; i++) Hit(b, riff[i], true, 0.8f);
                continue;
            }

            foreach (double at in riff) Hit(b, at, true, at == 0.0 ? 1f : 0.8f);
        }
    }

    // A distorted power chord: root, fifth and octave, plucked a moment apart.
    static float[] MakeChord(int root, bool muted, float cents)
    {
        int midi = 40 + ((root - 4 + 12) % 12);
        float length = muted ? 0.16f : 0.8f;

        int n = (int)(length * rate);
        var chord = new float[n];

        int[] notes = { midi, midi + 7, midi + 12 };

        for (int v = 0; v < notes.Length; v++)
        {
            double freq = 440.0 * Math.Pow(2.0, (notes[v] - 69 + cents / 100.0) / 12.0);
            float[] note = Pluck(freq, n, muted ? 0.975f : 0.997f);
            int late = v * rate / 500;

            for (int i = 0; i + late < n; i++) chord[i + late] += note[i];
        }

        for (int i = 0; i < n; i++) chord[i] = (float)Math.Tanh(chord[i] * 7f);

        chord = HighPass(chord, 110f);
        chord = LowPass(LowPass(chord, 3600f), 3600f);

        int end = rate / 100;
        for (int i = 0; i < end; i++) chord[n - 1 - i] *= i / (float)end;

        return chord;
    }

    // A string: a noise burst circulating in a delay line, losing a little each pass.
    static float[] Pluck(double freq, int n, float keep)
    {
        int period = Math.Max(2, (int)Math.Round(rate / freq - 0.5));
        var y = new float[n];

        for (int i = 0; i <= period && i < n; i++) y[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        for (int i = period + 1; i < n; i++) y[i] = keep * 0.5f * (y[i - period] + y[i - period - 1]);

        return y;
    }

    // ------------------------------------------------------------ Glitches

    static void Glitch(float[] left, float[] right, int[] barSample, int bars)
    {
        var dryL = (float[])left.Clone();
        var dryR = (float[])right.Clone();

        int Pos(int b, double beat) => barSample[b] + (int)(beat / 4.0 * (barSample[b + 1] - barSample[b]));

        int Beat(int b) => (barSample[b + 1] - barSample[b]) / 4;

        void Stutter(int at, int length, int slice)
        {
            Slices(left, dryL, at, length, slice);
            Slices(right, dryR, at, length, slice);
        }

        for (int b = 0; b < bars; b++)
        {
            int beat = Beat(b);
            int inWave = b % 20;

            if (inWave == 19)
            {
                // Top of the climb: the whole track winds down, then a breath of silence.
                TapeStop(left, dryL, Pos(b, 3.0), beat * 3 / 4);
                TapeStop(right, dryR, Pos(b, 3.0), beat * 3 / 4);
                Mute(left, Pos(b, 3.75), Pos(b, 4.0) - Pos(b, 3.75));
                Mute(right, Pos(b, 3.75), Pos(b, 4.0) - Pos(b, 3.75));
                continue;
            }

            if (inWave == 18) continue;

            if (b % 8 == 7)
            {
                // Rolls that tighten: eighths, sixteenths, then thirty-seconds.
                Stutter(Pos(b, 2.0), beat, beat / 2);
                Stutter(Pos(b, 3.0), beat / 2, beat / 4);
                Stutter(Pos(b, 3.5), beat / 2, beat / 8);
            }
            else if (b % 4 == 3)
            {
                Stutter(Pos(b, 3.0), beat, beat / 4);
            }

            if (b % 4 == 1)
            {
                Crush(left, Pos(b, 2.0), beat);
                Crush(right, Pos(b, 2.0), beat);
            }

            if (b % 2 == 1)
            {
                // Hard digital cut-outs, on off-beats so the groove survives them.
                double[] spots = { 0.75, 1.25, 1.75, 2.25, 2.75 };
                double spot = spots[rng.Next(spots.Length)];
                int length = beat / 4;

                Mute(left, Pos(b, spot), length);
                Mute(right, Pos(b, spot), length);
            }
        }
    }

    static void Slices(float[] target, float[] dry, int at, int length, int slice)
    {
        int edge = rate / 500;

        for (int i = 0; i < length; i++)
        {
            int inside = i % slice;
            float gain = 1f;

            if (inside < edge) gain = inside / (float)edge;
            else if (slice - inside < edge) gain = (slice - inside) / (float)edge;

            target[(at + i) % target.Length] = dry[(at + inside) % dry.Length] * gain;
        }
    }

    static void Mute(float[] target, int at, int length)
    {
        int edge = rate / 500;

        for (int i = 0; i < length; i++)
        {
            float gain = 0f;

            if (i < edge) gain = 1f - i / (float)edge;
            else if (length - i < edge) gain = 1f - (length - i) / (float)edge;

            target[(at + i) % target.Length] *= gain;
        }
    }

    // Reads the audio ever more slowly, like a tape machine losing power.
    static void TapeStop(float[] target, float[] dry, int at, int length)
    {
        double read = 0.0;

        for (int i = 0; i < length; i++)
        {
            double left = 1.0 - i / (double)length;
            int whole = (int)read;
            float part = (float)(read - whole);

            float a = dry[(at + whole) % dry.Length];
            float b = dry[(at + whole + 1) % dry.Length];

            target[(at + i) % target.Length] = (a + (b - a) * part) * (float)Math.Min(1.0, left * 8.0);
            read += left * left;
        }
    }

    // Lower sample rate and fewer bits: the digital crunch.
    static void Crush(float[] target, int at, int length)
    {
        float held = 0f;

        for (int i = 0; i < length; i++)
        {
            int w = (at + i) % target.Length;

            if (i % 10 == 0) held = MathF.Round(target[w] * 12f) / 12f;

            target[w] = held;
        }
    }

    // A high-pass that rises across the climb.
    static void ClimbFilter(float[] x, int from, int to)
    {
        float low = 0f;

        for (int i = from; i < to && i < x.Length; i++)
        {
            float t = (i - from) / (float)(to - from);
            float cutoff = 120f * (float)Math.Pow(30.0, t);
            float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoff / rate);

            low += a * (x[i] - low);
            x[i] -= low * Math.Min(1f, (i - from) / (rate * 0.2f));
        }
    }

    // ------------------------------------------------------------ Listening

    static float[] Flux(float[] x)
    {
        int frames = x.Length / Hop;
        var level = new float[frames];
        var flux = new float[frames];

        for (int f = 0; f < frames; f++)
        {
            double sum = 0.0;
            for (int i = 0; i < Hop; i++) sum += x[f * Hop + i] * x[f * Hop + i];

            level[f] = (float)Math.Log(1.0 + 1000.0 * Math.Sqrt(sum / Hop));

            if (f > 0) flux[f] = Math.Max(0f, level[f] - level[f - 1]);
        }

        return flux;
    }

    static double FindTempo(float[] onset)
    {
        double seconds = onset.Length * Hop / (double)rate;
        var scores = new List<(double bpm, double score)>();

        for (double bpm = 120.0; bpm <= 190.0; bpm += 0.05)
        {
            double beat = 60.0 / bpm;
            double best = 0.0;

            for (double p = 0.0; p < beat; p += Hop / (double)rate)
            {
                double sum = 0.0;
                int count = 0;

                for (double t = p; t < seconds; t += beat, count++) sum += At(onset, t);

                best = Math.Max(best, sum / count);
            }

            scores.Add((bpm, best));
        }

        var top = scores.OrderByDescending(s => s.score).First();
        Console.WriteLine($"tempo search: best {top.bpm:0.00} BPM, score {top.score:0.000}, average {scores.Average(s => s.score):0.000}");
        Console.WriteLine("  nearest other peaks: " + string.Join(", ", scores.OrderByDescending(s => s.score)
            .Where(s => Math.Abs(s.bpm - top.bpm) > 3.0).Take(40).Select(s => Math.Round(s.bpm)).Distinct().Take(5)));

        return top.bpm;
    }

    static double FindPhase(float[] onset, double beat)
    {
        double seconds = onset.Length * Hop / (double)rate;
        double best = -1.0, phase = 0.0;

        for (double p = 0.0; p < beat; p += 0.002)
        {
            double sum = 0.0;
            for (double t = p; t < seconds; t += beat) sum += At(onset, t);

            if (sum > best) { best = sum; phase = p; }
        }

        return phase;
    }

    static float At(float[] env, double seconds)
    {
        int frame = (int)Math.Round(seconds * rate / Hop);
        if (frame < 1 || frame >= env.Length - 1) return 0f;

        return Math.Max(env[frame], Math.Max(env[frame - 1], env[frame + 1]));
    }

    static double[] Median3(double[] x)
    {
        var y = new double[x.Length];

        for (int i = 0; i < x.Length; i++)
        {
            double a = x[Math.Max(0, i - 1)], b = x[i], c = x[Math.Min(x.Length - 1, i + 1)];
            y[i] = Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        return y;
    }

    // Measures how strong each note is per bar, then picks the key and each bar's chord.
    static int[] FindChords(float[] mono, int start, int[] barSample, out int key, out bool isMinor)
    {
        int bars = barSample.Length - 1;
        var chroma = new double[bars, 12];
        var total = new double[12];

        for (int b = 0; b < bars; b++)
        {
            int from = start + barSample[b];
            int count = barSample[b + 1] - barSample[b];

            for (int midi = 36; midi <= 84; midi++)
            {
                double freq = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
                double power = Goertzel(mono, from, count, freq, 3);

                chroma[b, midi % 12] += power;
                total[midi % 12] += power;
            }
        }

        double[] majorShape = { 6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88 };
        double[] minorShape = { 6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17 };

        double best = double.MinValue;
        key = 0;
        isMinor = true;

        for (int k = 0; k < 12; k++)
        {
            double major = 0.0, minor = 0.0;

            for (int i = 0; i < 12; i++)
            {
                major += majorShape[i] * total[(k + i) % 12];
                minor += minorShape[i] * total[(k + i) % 12];
            }

            if (major > best) { best = major; key = k; isMinor = false; }
            if (minor > best) { best = minor; key = k; isMinor = true; }
        }

        int[] scale = isMinor ? new[] { 0, 2, 3, 5, 7, 8, 10 } : new[] { 0, 2, 4, 5, 7, 9, 11 };
        var roots = new int[bars];

        for (int b = 0; b < bars; b++)
        {
            double top = double.MinValue;

            foreach (int degree in scale)
            {
                int r = (key + degree) % 12;
                double third = Math.Max(chroma[b, (r + 3) % 12], chroma[b, (r + 4) % 12]);
                double score = chroma[b, r] + 0.8 * chroma[b, (r + 7) % 12] + 0.4 * third;

                if (degree == 0) score *= 1.1;

                if (score > top) { top = score; roots[b] = r; }
            }
        }

        return roots;
    }

    static double Goertzel(float[] x, int from, int count, double freq, int step)
    {
        double w = 2.0 * Math.PI * freq * step / rate;
        double c = 2.0 * Math.Cos(w);
        double a = 0.0, b = 0.0;

        for (int i = 0; i < count; i += step)
        {
            double y = x[from + i] + c * a - b;
            b = a;
            a = y;
        }

        return a * a + b * b - c * a * b;
    }

    static string Note(int pitchClass)
    {
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        return names[pitchClass];
    }

    // ------------------------------------------------------------ Basics

    static void Add(float[] left, float[] right, float[] sound, int at, float gain, float pan)
    {
        float l = gain * (1f - Math.Max(0f, pan));
        float r = gain * (1f + Math.Min(0f, pan));

        for (int i = 0; i < sound.Length; i++)
        {
            int w = (at + i) % left.Length;

            left[w] += sound[i] * l;
            right[w] += sound[i] * r;
        }
    }

    static void AddOne(float[] target, float[] sound, int at, float gain)
    {
        for (int i = 0; i < sound.Length; i++) target[(at + i) % target.Length] += sound[i] * gain;
    }

    static float[] LowPass(float[] x, float cutoff)
    {
        var y = new float[x.Length];
        float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoff / rate);
        float low = 0f;

        for (int i = 0; i < x.Length; i++)
        {
            low += a * (x[i] - low);
            y[i] = low;
        }

        return y;
    }

    static float[] HighPass(float[] x, float cutoff)
    {
        float[] low = LowPass(x, cutoff);
        var y = new float[x.Length];

        for (int i = 0; i < x.Length; i++) y[i] = x[i] - low[i];

        return y;
    }

    static float Rms(float[] left, float[] right)
    {
        double sum = 0.0;
        for (int i = 0; i < left.Length; i++) sum += left[i] * left[i] + right[i] * right[i];

        return (float)Math.Sqrt(sum / (left.Length * 2));
    }

    static float RmsOf(float[] x, int from, int count)
    {
        double sum = 0.0;
        for (int i = 0; i < count; i++) sum += x[from + i] * x[from + i];

        return (float)Math.Sqrt(sum / count);
    }

    static void Scale(float[] left, float[] right, float gain)
    {
        for (int i = 0; i < left.Length; i++)
        {
            left[i] *= gain;
            right[i] *= gain;
        }
    }

    static void Decode(string path, out float[] left, out float[] right)
    {
        var file = new MpegFile(path);
        rate = file.SampleRate;

        var all = new List<float>();
        var chunk = new float[16384];
        int read;

        while ((read = file.ReadSamples(chunk, 0, chunk.Length)) > 0)
            for (int i = 0; i < read; i++) all.Add(chunk[i]);

        int frames = all.Count / 2;
        left = new float[frames];
        right = new float[frames];

        for (int i = 0; i < frames; i++)
        {
            left[i] = all[i * 2];
            right[i] = all[i * 2 + 1];
        }
    }

    static void WriteWav(string path, float[] left, float[] right)
    {
        using var writer = new BinaryWriter(File.Create(path));

        int bytes = left.Length * 4;

        writer.Write("RIFF"u8);
        writer.Write(36 + bytes);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)2);
        writer.Write(rate);
        writer.Write(rate * 4);
        writer.Write((short)4);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(bytes);

        for (int i = 0; i < left.Length; i++)
        {
            writer.Write((short)(Math.Clamp(left[i], -1f, 1f) * 32767f));
            writer.Write((short)(Math.Clamp(right[i], -1f, 1f) * 32767f));
        }
    }
}
