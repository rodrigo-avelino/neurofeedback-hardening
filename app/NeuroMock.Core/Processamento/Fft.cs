using System;

namespace NeuroMock.Core.Processamento;

/// <summary>FFT radix-2 iterativa (Cooley-Tukey), in place, sobre partes real e imaginária.</summary>
public static class Fft
{
    public static void Transformar(double[] re, double[] im)
    {
        var n = re.Length;
        if (n == 0 || n != im.Length || (n & (n - 1)) != 0)
            throw new ArgumentException("os vetores precisam ter o mesmo tamanho, potência de 2", nameof(re));

        // Reordena pelos índices com bits invertidos.
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        for (var tamanho = 2; tamanho <= n; tamanho <<= 1)
        {
            var angulo = -2 * Math.PI / tamanho;
            double wRe = Math.Cos(angulo), wIm = Math.Sin(angulo);
            var metade = tamanho / 2;
            for (var inicio = 0; inicio < n; inicio += tamanho)
            {
                double uRe = 1, uIm = 0;
                for (var k = 0; k < metade; k++)
                {
                    int a = inicio + k, b = a + metade;
                    var tRe = re[b] * uRe - im[b] * uIm;
                    var tIm = re[b] * uIm + im[b] * uRe;
                    re[b] = re[a] - tRe;
                    im[b] = im[a] - tIm;
                    re[a] += tRe;
                    im[a] += tIm;
                    var proximoRe = uRe * wRe - uIm * wIm;
                    uIm = uRe * wIm + uIm * wRe;
                    uRe = proximoRe;
                }
            }
        }
    }
}
