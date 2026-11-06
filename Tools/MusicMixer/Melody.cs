// The slow flute melody that floats over the fast track: two notes a bar, sinking two octaves
// across each wave, then silent for the climb so it can start again from the top on the drop.
static partial class Program
{
    // Two notes per bar, as MIDI pitches. Each is pulled onto the track's real chords below.
    static readonly int[] fall =
    {
        86, 84, 81, 79,  77, 76, 74, 72,   84, 81, 79, 77,  76, 74, 72, 69,
        81, 79, 77, 76,  74, 72, 69, 67,   79, 77, 76, 74,  72, 69, 67, 62
    };

    static void PlayMelody(float[] left, float[] right, int[] barSample, int bars, int[] roots, int key, bool isMinor, float bellGain)
    {
        int[] scale = isMinor ? new[] { 0, 2, 3, 5, 7, 8, 10 } : new[] { 0, 2, 4, 5, 7, 9, 11 };
        var inScale = new HashSet<int>();
        foreach (int degree in scale) inScale.Add((key + degree) % 12);

        var cache = new Dictionary<(int, int), float[]>();

        void Note(int b, double beat, int midi, double beats)
        {
            int beatLength = (barSample[b + 1] - barSample[b]) / 4;
            int at = barSample[b] + (int)(beat * beatLength);
            int sustain = (int)(beats * beatLength);

            var id = (midi, sustain);

            if (!cache.TryGetValue(id, out float[] sound))
            {
                // The flute carries the tune, with the bell an octave up for shine.
                double freq = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
                float[] flute = MakeFlute(freq, sustain);
                float[] bell = MakeBell(freq * 2.0, sustain);

                sound = new float[Math.Max(flute.Length, bell.Length)];

                for (int i = 0; i < flute.Length; i++) sound[i] += flute[i];
                for (int i = 0; i < bell.Length; i++) sound[i] += bell[i] * bellGain;

                cache[id] = sound;
            }

            Add(left, right, sound, at, 1f, 0f);
        }

        for (int b = 0; b < bars; b++)
        {
            int inWave = b % 20;

            if (inWave < 16)
            {
                int strong = Nearest(fall[inWave * 2], pitch => IsChordTone(pitch, roots[b], inScale));
                int weak = Nearest(fall[inWave * 2 + 1], pitch => inScale.Contains(pitch % 12));

                Note(b, 0.0, strong, 2.4);
                Note(b, 2.0, weak, 2.4);
            }
            else if (inWave == 16)
            {
                // The bottom of the fall, left to ring out.
                Note(b, 0.0, Nearest(62, pitch => IsChordTone(pitch, roots[b], inScale)), 6.0);
            }
        }
    }

    // The start screen music: the flute and bell alone, one full fall, then back to the top.
    static void RenderMenuLoop(string path, int[] barSample, int[] roots, int key, bool isMinor, int beatSamples)
    {
        const int Bars = 18;

        int[] bars = barSample.Take(Bars + 1).ToArray();
        int length = bars[Bars];

        var left = new float[length];
        var right = new float[length];

        PlayMelody(left, right, bars, Bars, roots, key, isMinor, 0.5f);
        AddSpace(left, right, beatSamples);

        float loudest = Math.Max(left.Max(Math.Abs), right.Max(Math.Abs));
        Scale(left, right, 0.8f / loudest);

        WriteWav(path, left, right);
        Console.WriteLine($"menu loop {length / (float)rate:0.00}s, level {Rms(left, right):0.000}, last 0.5s level {RmsOf(left, length - rate / 2, rate / 2):0.000}, first 0.5s level {RmsOf(left, 0, rate / 2):0.000}");
    }

    static bool IsChordTone(int pitch, int root, HashSet<int> inScale)
    {
        int third = inScale.Contains((root + 3) % 12) ? 3 : 4;
        int step = (pitch - root + 120) % 12;

        return step == 0 || step == third || step == 7;
    }

    static int Nearest(int midi, Func<int, bool> allowed)
    {
        for (int distance = 0; distance < 12; distance++)
        {
            if (allowed(midi - distance)) return midi - distance;
            if (allowed(midi + distance)) return midi + distance;
        }

        return midi;
    }

    // A flute: a mostly pure tone with breath in it. Each note slides in from slightly
    // above, then a vibrato opens up once the note has settled.
    static float[] MakeFlute(double freq, int sustain)
    {
        int n = sustain + rate / 6;
        var s = new float[n];
        double phase = 0.0;
        float breath = 0f;

        for (int i = 0; i < n; i++)
        {
            double t = i / (double)rate;

            double slide = 45.0 * Math.Exp(-t * 18.0);
            double vibrato = 22.0 * Math.Min(1.0, Math.Max(0.0, (t - 0.18) / 0.3)) * Math.Sin(2.0 * Math.PI * 5.4 * t);
            double f = freq * Math.Pow(2.0, (slide + vibrato) / 1200.0);

            phase += 2.0 * Math.PI * f / rate;

            double tone = Math.Sin(phase) + 0.42 * Math.Sin(phase * 2.0) + 0.16 * Math.Sin(phase * 3.0) + 0.05 * Math.Sin(phase * 4.0);

            // Breath is filtered noise, strongest in the first moment of each note.
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            breath += 0.3f * (noise - breath);
            double air = breath * (0.1 + 0.3 * Math.Exp(-t * 22.0));

            double attack = Math.Min(1.0, t / 0.05);
            double release = i < sustain ? 1.0 : 1.0 - (i - sustain) / (double)(n - sustain);

            s[i] = (float)((tone + air) * attack * release * 0.75);
        }

        return s;
    }

    // A soft glassy bell that sags very slightly flat as it rings, like something sinking.
    static float[] MakeBell(double freq, int sustain)
    {
        int n = sustain + rate / 2;
        var s = new float[n];
        double phase = 0.0, modPhase = 0.0, under = 0.0;

        for (int i = 0; i < n; i++)
        {
            double t = i / (double)rate;
            double bend = Math.Pow(2.0, -0.15 / 12.0 * i / n);
            double f = freq * bend;

            phase += 2.0 * Math.PI * f / rate;
            modPhase += 2.0 * Math.PI * f * 2.0 / rate;
            under += 2.0 * Math.PI * f * 0.5 / rate;

            double brightness = 1.5 * Math.Exp(-t * 6.0) + 0.25;
            double body = Math.Sin(phase + brightness * Math.Sin(modPhase));
            double air = 0.3 * Math.Sin(under);

            double attack = Math.Min(1.0, t / 0.02);
            double decay = Math.Exp(-t * 1.5);
            double release = i < sustain ? 1.0 : 1.0 - (i - sustain) / (double)(n - sustain);

            s[i] = (float)((body + air) * attack * decay * release);
        }

        return s;
    }

    // Echoes and a wash of reverb, run round the loop twice so tails carry over the join.
    static void AddSpace(float[] left, float[] right, int beatSamples)
    {
        int length = left.Length;

        var echoL = new float[length];
        var echoR = new float[length];
        int gap = beatSamples * 3 / 2;

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < length; i++)
            {
                int from = (i - gap + length) % length;

                float nextL = right[i] * 0.2f + left[i] * 0.3f + echoR[from] * 0.45f;
                float nextR = left[i] * 0.3f + echoL[from] * 0.45f;

                echoL[i] = nextL;
                echoR[i] = nextR;
            }
        }

        float[] washL = Wash(left, right, 0);
        float[] washR = Wash(left, right, 37);

        for (int i = 0; i < length; i++)
        {
            left[i] += echoL[i] * 0.7f + washL[i] * 0.3f;
            right[i] += echoR[i] * 0.7f + washR[i] * 0.3f;
        }
    }

    static float[] Wash(float[] left, float[] right, int spread)
    {
        int length = left.Length;
        int[] delays = { 1687 + spread, 1781 + spread, 1913 + spread, 2039 + spread };
        var result = new float[length];

        foreach (int delay in delays)
        {
            var line = new float[delay];
            int at = 0;
            float damped = 0f;

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < length; i++)
                {
                    float out_ = line[at];

                    damped += 0.35f * (out_ - damped);
                    line[at] = (left[i] + right[i]) * 0.05f + damped * 0.88f;
                    at = (at + 1) % delay;

                    if (pass == 1) result[i] += out_;
                }
            }
        }

        return result;
    }
}
