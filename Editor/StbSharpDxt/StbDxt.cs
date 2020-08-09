/// This code had been borrowed from here: https://github.com/rds1983/StbSharp

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.StbSharpDxt
{
    internal unsafe class StbDxt
    {
        public static byte[] stb__Expand5 = new byte[32];
        public static byte[] stb__Expand6 = new byte[64];
        public static byte[] stb__OMatch5 = new byte[512];
        public static byte[] stb__OMatch6 = new byte[512];
        public static byte[] stb__QuantRBTab = new byte[256 + 16];
        public static byte[] stb__QuantGTab = new byte[256 + 16];
        public static int nIterPower = 4;

        public static float[] midpoints5 =
        {
            0.015686f, 0.047059f, 0.078431f, 0.111765f, 0.145098f, 0.176471f, 0.207843f, 0.241176f, 0.274510f,
            0.305882f, 0.337255f, 0.370588f, 0.403922f, 0.435294f, 0.466667f, 0.5f, 0.533333f, 0.564706f, 0.596078f,
            0.629412f, 0.662745f, 0.694118f, 0.725490f, 0.758824f, 0.792157f, 0.823529f, 0.854902f, 0.888235f,
            0.921569f, 0.952941f, 0.984314f, 1.0f
        };

        public static float[] midpoints6 =
        {
            0.007843f, 0.023529f, 0.039216f, 0.054902f, 0.070588f, 0.086275f, 0.101961f, 0.117647f, 0.133333f,
            0.149020f, 0.164706f, 0.180392f, 0.196078f, 0.211765f, 0.227451f, 0.245098f, 0.262745f, 0.278431f,
            0.294118f, 0.309804f, 0.325490f, 0.341176f, 0.356863f, 0.372549f, 0.388235f, 0.403922f, 0.419608f,
            0.435294f, 0.450980f, 0.466667f, 0.482353f, 0.500000f, 0.517647f, 0.533333f, 0.549020f, 0.564706f,
            0.580392f, 0.596078f, 0.611765f, 0.627451f, 0.643137f, 0.658824f, 0.674510f, 0.690196f, 0.705882f,
            0.721569f, 0.737255f, 0.754902f, 0.772549f, 0.788235f, 0.803922f, 0.819608f, 0.835294f, 0.850980f,
            0.866667f, 0.882353f, 0.898039f, 0.913725f, 0.929412f, 0.945098f, 0.960784f, 0.976471f, 0.992157f, 1.0f
        };

        public static int[] w1Tab = { 3, 0, 2, 1 };
        public static int[] prods = { 0x090000, 0x000900, 0x040102, 0x010402 };
        public static int init = 1;

        public static int stb__Mul8Bit(int a, int b)
        {
            var t = a * b + 128;
            return (t + (t >> 8)) >> 8;
        }

        public static void stb__From16Bit(byte* _out_, ushort v)
        {
            var rv = (v & 0xf800) >> 11;
            var gv = (v & 0x07e0) >> 5;
            var bv = (v & 0x001f) >> 0;
            _out_[0] = stb__Expand5[rv];
            _out_[1] = stb__Expand6[gv];
            _out_[2] = stb__Expand5[bv];
            _out_[3] = 0;
        }

        public static ushort stb__As16Bit(int r, int g, int b)
        {
            return (ushort)((stb__Mul8Bit(r, 31) << 11) + (stb__Mul8Bit(g, 63) << 5) + stb__Mul8Bit(b, 31));
        }

        public static int stb__Lerp13(int a, int b)
        {
            return (2 * a + b) / 3;
        }

        public static void stb__Lerp13RGB(byte* _out_, byte* p1, byte* p2)
        {
            _out_[0] = (byte)stb__Lerp13(p1[0], p2[0]);
            _out_[1] = (byte)stb__Lerp13(p1[1], p2[1]);
            _out_[2] = (byte)stb__Lerp13(p1[2], p2[2]);
        }

        public static void stb__PrepareOptTable(byte[] Table, byte[] expand, int size)
        {
            var i = 0;
            var mn = 0;
            var mx = 0;
            for (i = 0; i < 256; i++)
            {
                var bestErr = 256;
                for (mn = 0; mn < size; mn++)
                    for (mx = 0; mx < size; mx++)
                    {
                        var mine = (int)expand[mn];
                        var maxe = (int)expand[mx];
                        var err = Math.Abs(stb__Lerp13(maxe, mine) - i);
                        err += Math.Abs(maxe - mine) * 3 / 100;
                        if (err < bestErr)
                        {
                            Table[i * 2 + 0] = (byte)mx;
                            Table[i * 2 + 1] = (byte)mn;
                            bestErr = err;
                        }
                    }
            }
        }

        public static void stb__EvalColors(byte* color, ushort c0, ushort c1)
        {
            stb__From16Bit(color + 0, c0);
            stb__From16Bit(color + 4, c1);
            stb__Lerp13RGB(color + 8, color + 0, color + 4);
            stb__Lerp13RGB(color + 12, color + 4, color + 0);
        }

        public static uint stb__MatchColorsBlock(byte* block, byte* color, int dither)
        {
            var mask = (uint)0;
            var dirr = color[0 * 4 + 0] - color[1 * 4 + 0];
            var dirg = color[0 * 4 + 1] - color[1 * 4 + 1];
            var dirb = color[0 * 4 + 2] - color[1 * 4 + 2];
            var dots = stackalloc int[16];
            var stops = stackalloc int[4];
            var i = 0;
            var c0Point = 0;
            var halfPoint = 0;
            var c3Point = 0;
            for (i = 0; i < 16; i++)
                dots[i] = block[i * 4 + 0] * dirr + block[i * 4 + 1] * dirg + block[i * 4 + 2] * dirb;
            for (i = 0; i < 4; i++)
                stops[i] = color[i * 4 + 0] * dirr + color[i * 4 + 1] * dirg + color[i * 4 + 2] * dirb;
            c0Point = stops[1] + stops[3];
            halfPoint = stops[3] + stops[2];
            c3Point = stops[2] + stops[0];
            if (dither == 0)
            {
                for (i = 15; i >= 0; i--)
                {
                    var dot = dots[i] * 2;
                    mask <<= 2;
                    if (dot < halfPoint) mask |= (uint)(dot < c0Point ? 1 : 3);
                    else mask |= (uint)(dot < c3Point ? 2 : 0);
                }
            }
            else
            {
                var err = stackalloc int[8];
                var ep1 = err;
                var ep2 = err + 4;
                var dp = dots;
                var y = 0;
                c0Point <<= 3;
                halfPoint <<= 3;
                c3Point <<= 3;
                for (i = 0; i < 8; i++) err[i] = 0;
                for (y = 0; y < 4; y++)
                {
                    var dot = 0;
                    var lmask = 0;
                    var step = 0;
                    dot = (dp[0] << 4) + 3 * ep2[1] + 5 * ep2[0];
                    if (dot < halfPoint) step = dot < c0Point ? 1 : 3;
                    else step = dot < c3Point ? 2 : 0;
                    ep1[0] = dp[0] - stops[step];
                    lmask = step;
                    dot = (dp[1] << 4) + 7 * ep1[0] + 3 * ep2[2] + 5 * ep2[1] + ep2[0];
                    if (dot < halfPoint) step = dot < c0Point ? 1 : 3;
                    else step = dot < c3Point ? 2 : 0;
                    ep1[1] = dp[1] - stops[step];
                    lmask |= step << 2;
                    dot = (dp[2] << 4) + 7 * ep1[1] + 3 * ep2[3] + 5 * ep2[2] + ep2[1];
                    if (dot < halfPoint) step = dot < c0Point ? 1 : 3;
                    else step = dot < c3Point ? 2 : 0;
                    ep1[2] = dp[2] - stops[step];
                    lmask |= step << 4;
                    dot = (dp[3] << 4) + 7 * ep1[2] + 5 * ep2[3] + ep2[2];
                    if (dot < halfPoint) step = dot < c0Point ? 1 : 3;
                    else step = dot < c3Point ? 2 : 0;
                    ep1[3] = dp[3] - stops[step];
                    lmask |= step << 6;
                    dp += 4;
                    mask |= (uint)(lmask << (y * 8));
                    {
                        var et = ep1;
                        ep1 = ep2;
                        ep2 = et;
                    }
                }
            }

            return mask;
        }

        public static void stb__OptimizeColorsBlock(byte* block, ushort* pmax16, ushort* pmin16)
        {
            var mind = 0x7fffffff;
            var maxd = -0x7fffffff;
            byte* minp = null;
            byte* maxp = null;
            double magn = 0;
            var v_r = 0;
            var v_g = 0;
            var v_b = 0;
            var covf = stackalloc float[6];
            float vfr = 0;
            float vfg = 0;
            float vfb = 0;
            var cov = stackalloc int[6];
            var mu = stackalloc int[3];
            var min = stackalloc int[3];
            var max = stackalloc int[3];
            var ch = 0;
            var i = 0;
            var iter = 0;
            for (ch = 0; ch < 3; ch++)
            {
                var bp = block + ch;
                var muv = 0;
                var minv = 0;
                var maxv = 0;
                muv = minv = maxv = bp[0];
                for (i = 4; i < 64; i += 4)
                {
                    muv += bp[i];
                    if (bp[i] < minv) minv = bp[i];
                    else if (bp[i] > maxv) maxv = bp[i];
                }

                mu[ch] = (muv + 8) >> 4;
                min[ch] = minv;
                max[ch] = maxv;
            }

            for (i = 0; i < 6; i++) cov[i] = 0;
            for (i = 0; i < 16; i++)
            {
                var r = block[i * 4 + 0] - mu[0];
                var g = block[i * 4 + 1] - mu[1];
                var b = block[i * 4 + 2] - mu[2];
                cov[0] += r * r;
                cov[1] += r * g;
                cov[2] += r * b;
                cov[3] += g * g;
                cov[4] += g * b;
                cov[5] += b * b;
            }

            for (i = 0; i < 6; i++) covf[i] = cov[i] / 255.0f;
            vfr = max[0] - min[0];
            vfg = max[1] - min[1];
            vfb = max[2] - min[2];
            for (iter = 0; iter < nIterPower; iter++)
            {
                var r = vfr * covf[0] + vfg * covf[1] + vfb * covf[2];
                var g = vfr * covf[1] + vfg * covf[3] + vfb * covf[4];
                var b = vfr * covf[2] + vfg * covf[4] + vfb * covf[5];
                vfr = r;
                vfg = g;
                vfb = b;
            }

            magn = (float)Math.Abs((double) vfr);
            if ((float)Math.Abs((double) vfg) > magn) magn = (float)Math.Abs((double) vfg);
            if ((float)Math.Abs((double) vfb) > magn) magn = (float)Math.Abs((double) vfb);
            if (magn < 4.0f)
            {
                v_r = 299;
                v_g = 587;
                v_b = 114;
            }
            else
            {
                magn = 512.0 / magn;
                v_r = (int)(vfr * magn);
                v_g = (int)(vfg * magn);
                v_b = (int)(vfb * magn);
            }

            for (i = 0; i < 16; i++)
            {
                var dot = block[i * 4 + 0] * v_r + block[i * 4 + 1] * v_g + block[i * 4 + 2] * v_b;
                if (dot < mind)
                {
                    mind = dot;
                    minp = block + i * 4;
                }

                if (dot > maxd)
                {
                    maxd = dot;
                    maxp = block + i * 4;
                }
            }

            *pmax16 = stb__As16Bit(maxp[0], maxp[1], maxp[2]);
            *pmin16 = stb__As16Bit(minp[0], minp[1], minp[2]);
        }

        public static ushort stb__Quantize5(float x)
        {
            ushort q = 0;
            x = x < 0 ? 0 : x > 1 ? 1 : x;
            q = (ushort)(x * 31);
            q += (ushort)(x > midpoints5[q] ? 1 : 0);
            return q;
        }

        public static ushort stb__Quantize6(float x)
        {
            ushort q = 0;
            x = x < 0 ? 0 : x > 1 ? 1 : x;
            q = (ushort)(x * 63);
            q += (ushort)(x > midpoints6[q] ? 1 : 0);
            return q;
        }

        public static int stb__RefineBlock(byte* block, ushort* pmax16, ushort* pmin16, uint mask)
        {
            float f = 0;
            ushort oldMin = 0;
            ushort oldMax = 0;
            ushort min16 = 0;
            ushort max16 = 0;
            var i = 0;
            var akku = 0;
            var xx = 0;
            var xy = 0;
            var yy = 0;
            var At1_r = 0;
            var At1_g = 0;
            var At1_b = 0;
            var At2_r = 0;
            var At2_g = 0;
            var At2_b = 0;
            var cm = mask;
            oldMin = *pmin16;
            oldMax = *pmax16;
            if ((mask ^ (mask << 2)) < 4)
            {
                var r = 8;
                var g = 8;
                var b = 8;
                for (i = 0; i < 16; ++i)
                {
                    r += block[i * 4 + 0];
                    g += block[i * 4 + 1];
                    b += block[i * 4 + 2];
                }

                r >>= 4;
                g >>= 4;
                b >>= 4;
                max16 = (ushort)((stb__OMatch5[r * 2 + 0] << 11) | (stb__OMatch6[g * 2 + 0] << 5) |
                                  stb__OMatch5[b * 2 + 0]);
                min16 = (ushort)((stb__OMatch5[r * 2 + 1] << 11) | (stb__OMatch6[g * 2 + 1] << 5) |
                                  stb__OMatch5[b * 2 + 1]);
            }
            else
            {
                At1_r = At1_g = At1_b = 0;
                At2_r = At2_g = At2_b = 0;
                for (i = 0; i < 16; ++i, cm >>= 2)
                {
                    var step = (int)(cm & 3);
                    var w1 = w1Tab[step];
                    var r = (int)block[i * 4 + 0];
                    var g = (int)block[i * 4 + 1];
                    var b = (int)block[i * 4 + 2];
                    akku += prods[step];
                    At1_r += w1 * r;
                    At1_g += w1 * g;
                    At1_b += w1 * b;
                    At2_r += r;
                    At2_g += g;
                    At2_b += b;
                }

                At2_r = 3 * At2_r - At1_r;
                At2_g = 3 * At2_g - At1_g;
                At2_b = 3 * At2_b - At1_b;
                xx = akku >> 16;
                yy = (akku >> 8) & 0xff;
                xy = (akku >> 0) & 0xff;
                f = 3.0f / 255.0f / (xx * yy - xy * xy);
                max16 = (ushort)(stb__Quantize5((At1_r * yy - At2_r * xy) * f) << 11);
                max16 |= (ushort)(stb__Quantize6((At1_g * yy - At2_g * xy) * f) << 5);
                max16 |= (ushort)(stb__Quantize5((At1_b * yy - At2_b * xy) * f) << 0);
                min16 = (ushort)(stb__Quantize5((At2_r * xx - At1_r * xy) * f) << 11);
                min16 |= (ushort)(stb__Quantize6((At2_g * xx - At1_g * xy) * f) << 5);
                min16 |= (ushort)(stb__Quantize5((At2_b * xx - At1_b * xy) * f) << 0);
            }

            *pmin16 = min16;
            *pmax16 = max16;
            return oldMin != min16 || oldMax != max16 ? 1 : 0;
        }

        public static void stb__CompressColorBlock(byte* dest, byte* block, int mode)
        {
            uint mask = 0;
            var i = 0;
            var dither = 0;
            var refinecount = 0;
            ushort max16 = 0;
            ushort min16 = 0;
            var dblock = stackalloc byte[16 * 4];
            var color = stackalloc byte[4 * 4];
            dither = mode & 1;
            refinecount = (mode & 2) != 0 ? 2 : 1;
            for (i = 1; i < 16; i++)
                if (((uint*)block)[i] != ((uint*)block)[0])
                    break;
            if (i == 16)
            {
                var r = (int)block[0];
                var g = (int)block[1];
                var b = (int)block[2];
                mask = 0xaaaaaaaa;
                max16 = (ushort)((stb__OMatch5[r * 2 + 0] << 11) | (stb__OMatch6[g * 2 + 0] << 5) |
                                  stb__OMatch5[b * 2 + 0]);
                min16 = (ushort)((stb__OMatch5[r * 2 + 1] << 11) | (stb__OMatch6[g * 2 + 1] << 5) |
                                  stb__OMatch5[b * 2 + 1]);
            }
            else
            {
                if (dither != 0) stb__DitherBlock(dblock, block);
                stb__OptimizeColorsBlock(dither != 0 ? dblock : block, &max16, &min16);
                if (max16 != min16)
                {
                    stb__EvalColors(color, max16, min16);
                    mask = stb__MatchColorsBlock(block, color, dither);
                }
                else
                {
                    mask = 0;
                }

                for (i = 0; i < refinecount; i++)
                {
                    var lastmask = mask;
                    if (stb__RefineBlock(dither != 0 ? dblock : block, &max16, &min16, mask) != 0)
                    {
                        if (max16 != min16)
                        {
                            stb__EvalColors(color, max16, min16);
                            mask = stb__MatchColorsBlock(block, color, dither);
                        }
                        else
                        {
                            mask = 0;
                            break;
                        }
                    }

                    if (mask == lastmask) break;
                }
            }

            if (max16 < min16)
            {
                var t = min16;
                min16 = max16;
                max16 = t;
                mask ^= 0x55555555;
            }

            dest[0] = (byte)max16;
            dest[1] = (byte)(max16 >> 8);
            dest[2] = (byte)min16;
            dest[3] = (byte)(min16 >> 8);
            dest[4] = (byte)mask;
            dest[5] = (byte)(mask >> 8);
            dest[6] = (byte)(mask >> 16);
            dest[7] = (byte)(mask >> 24);
        }

        public static void stb__CompressAlphaBlock(byte* dest, Color32* src)
        {
            var i = 0;
            var dist = 0;
            var bias = 0;
            var dist4 = 0;
            var dist2 = 0;
            var bits = 0;
            var mask = 0;
            var mn = 0;
            var mx = 0;
            mn = mx = src[0].a;
            for (i = 1; i < 16; i++)
                if (src[i].a < mn) mn = src[i].a;
                else if (src[i].a > mx) mx = src[i].a;
            dest[0] = (byte)mx;
            dest[1] = (byte)mn;
            dest += 2;
            dist = mx - mn;
            dist4 = dist * 4;
            dist2 = dist * 2;
            bias = dist < 8 ? dist - 1 : dist / 2 + 2;
            bias -= mn * 7;
            bits = 0;
            mask = 0;
            for (i = 0; i < 16; i++)
            {
                var a = src[i].a * 7 + bias;
                var ind = 0;
                var t = 0;
                t = a >= dist4 ? -1 : 0;
                ind = t & 4;
                a -= dist4 & t;
                t = a >= dist2 ? -1 : 0;
                ind += t & 2;
                a -= dist2 & t;
                ind += a >= dist ? 1 : 0;
                ind = -ind & 7;
                ind ^= 2 > ind ? 1 : 0;
                mask |= ind << bits;
                if ((bits += 3) >= 8)
                {
                    *dest++ = (byte)mask;
                    mask >>= 8;
                    bits -= 8;
                }
            }
        }

        public static void stb__InitDXT()
        {
            var i = 0;
            for (i = 0; i < 32; i++) stb__Expand5[i] = (byte)((i << 3) | (i >> 2));
            for (i = 0; i < 64; i++) stb__Expand6[i] = (byte)((i << 2) | (i >> 4));
            for (i = 0; i < 256 + 16; i++)
            {
                var v = i - 8 < 0 ? 0 : i - 8 > 255 ? 255 : i - 8;
                stb__QuantRBTab[i] = stb__Expand5[stb__Mul8Bit(v, 31)];
                stb__QuantGTab[i] = stb__Expand6[stb__Mul8Bit(v, 63)];
            }

            stb__PrepareOptTable(stb__OMatch5, stb__Expand5, 32);
            stb__PrepareOptTable(stb__OMatch6, stb__Expand6, 64);
        }

        public static void stb_compress_dxt_block(byte* dest, Color32* src, int alpha, int mode)
        {
            var data = stackalloc Color32[16];
            if (init != 0)
            {
                stb__InitDXT();
                init = 0;
            }

            if (alpha != 0)
            {
                var i = 0;
                stb__CompressAlphaBlock(dest, src);
                dest += 8;
                for (i = 0; i < 16; ++i)
                {
                    data[i] = src[i];
                    data[i].a = 255;
                }
                src = data;
            }

            stb__CompressColorBlock(dest, (byte*)src, mode);
        }

        public static void stb__DitherBlock(byte* dest, byte* block)
        {
            int* err = stackalloc int[8];
            var ep1 = err;
            var ep2 = err + 4;
            int ch;
            for (ch = 0; ch < 3; ++ch)
            {
                var bp = block + ch;
                var dp = dest + ch;
                var quantArray = ch == (1) ? stb__QuantGTab : stb__QuantRBTab;
                fixed (byte* quant = quantArray)
                {
                    for (long i = 0; i < 8; ++i)
                    {
                        err[i] = 0;
                    }

                    int y;
                    for (y = 0; (y) < (4); ++y)
                    {
                        dp[0] = quant[bp[0] + ((3 * ep2[1] + 5 * ep2[0]) >> 4)];
                        ep1[0] = bp[0] - dp[0];
                        dp[4] = quant[bp[4] + ((7 * ep1[0] + 3 * ep2[2] + 5 * ep2[1] + ep2[0]) >> 4)];
                        ep1[1] = bp[4] - dp[4];
                        dp[8] = quant[bp[8] + ((7 * ep1[1] + 3 * ep2[3] + 5 * ep2[2] + ep2[1]) >> 4)];
                        ep1[2] = bp[8] - dp[8];
                        dp[12] = quant[bp[12] + ((7 * ep1[2] + 5 * ep2[3] + ep2[2]) >> 4)];
                        ep1[3] = bp[12] - dp[12];
                        bp += 16;
                        dp += 16;
                        var et = ep1;
                        ep1 = ep2;
                        ep2 = et;
                    }
                }
            }
        }

        public static byte[] CompressDxt(int width, int height, Color32[] data, bool hasAlpha, CompressionMode mode)
        {
            if (data.Length != width * height)
            {
                throw new Exception("Unexpected data length");
            }

            var blockSize = hasAlpha ? 16 : 8;
            var numBlocks = ((width + 3) / 4) * ((height + 3) / 4);
            var result = new byte[numBlocks * blockSize];
            Color32* block = stackalloc Color32[16];

            fixed (Color32* colors = data)
            {
                fixed (byte* resultPtr = result)
                {
                    var p = resultPtr;

                    for (var row = 0; row < height; row += 4)
                    {
                        for (var col = 0; col < width; col += 4)
                        {
                            var y = 0;
                            Color32* blockRow = block;
                            var numRows = Math.Min(4, height - row);
                            for (; y < numRows; ++y)
                            {
                                CopyRow(blockRow, colors + ((height - row - y - 1) * width + col), width - col);
                                blockRow += 4;
                            }

                            for (; y < 4; ++y)
                            {
                                CopyRow(blockRow, blockRow - 4, 4);
                                blockRow += 4;
                            }

                            stb_compress_dxt_block(p, block, hasAlpha ? 1 : 0, (int) mode);
                            p += hasAlpha ? 16 : 8;
                        }
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyFullRow(Color32* dst, Color32* src)
        {
            dst[0] = src[0];
            dst[1] = src[1];
            dst[2] = src[2];
            dst[3] = src[3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyRow(Color32* dst, Color32* src, int maxWidth)
        {
            switch (maxWidth)
            {
                case 1:
                    dst[0] = dst[1] = dst[2] = dst[3] = src[0];
                    break;
                case 2:
                    dst[0] = src[0];
                    dst[1] = dst[2] = dst[3] = src[1];
                    break;
                case 3:
                    dst[0] = src[0];
                    dst[1] = src[1];
                    dst[2] = dst[3] = src[2];
                    break;
                default:
                    CopyFullRow(dst, src);
                    break;
            }
        }
    }
}
