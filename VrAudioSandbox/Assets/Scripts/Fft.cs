using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFTWSharp;

// FFT uses FFTW - see http://www.fftw.org/index.html
// C# Wrapper based on the documentation of FFTWSharp - see https://github.com/tszalay/FFTWSharp

public class Fft
{
    System.IntPtr pin, pout;

    // pointers to FFTW plan objects
    System.IntPtr fplanForward, fplanBackward;

    private int n;

    public void RunFft(double[] input, double[] output, bool real)
    {
        /*if (real)
            input = Fft.ToComplex(input);*/

        this.n = input.Length;

        this.pin = fftw.malloc(this.n * 2 * sizeof(double));
        this.pout = fftw.malloc(this.n * 2 * sizeof(double));

        this.fplanForward = fftw.r2r_1d(this.n, this.pin, this.pout, fftw_kind.R2HC, fftw_flags.Estimate);

        Marshal.Copy(input, 0, this.pin, this.n);
        fftwf.execute(this.fplanForward);
        Marshal.Copy(this.pout, output, 0, this.n);
    }

    public void RunIfft(double[] input, double[] output)
    {
        this.fplanBackward = fftw.r2r_1d(this.n, this.pin, this.pout, fftw_kind.HC2R, fftw_flags.Estimate);

        Marshal.Copy(input, 0, this.pin, this.n);
        fftwf.execute(this.fplanBackward);
        Marshal.Copy(this.pout, output, 0, this.n);

        // FFTW computes an unnormalized transform, in that there is no coefficient in front of the summation in the DFT.
        // In other words, applying the forward and then the backward transform will multiply the input by n. 
        // Divide by n:
        for (int i = 0; i < output.Length; i++)
        {
            output[i] /= this.n;
        }
    }

    /*private static double[] ToComplex(double[] real)
    {
        int n = real.Length;

        double[] comp = new double[n * 2];
        for (int i = 0; i < n; i++)
        {
            comp[2 * i] = real[i];
        }
        return comp;
    }

    public static double[] ComplexMagnitudes(double[] x)
    {
        int n = x.Length / 2;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = System.Math.Sqrt(x[2 * i] * x[2 * i] + x[2 * i + 1] * x[2 * i + 1]);
        }
        return y;
    }*/

    public static double[] Magnitudes(double[] x)
    {
        int n = x.Length;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = System.Math.Abs(x[i]);
        }
        return y;
    }

    public static double[] dB(double[] x)
    {
        int n = x.Length;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = (double)(20 * System.Math.Log(x[i]));
        }
        return y;
    }

    public void Dispose()
    {
        fftwf.free(this.pin);
        fftwf.free(this.pout);
        fftwf.destroy_plan(this.fplanForward);
        fftwf.destroy_plan(this.fplanBackward);
    }
}
