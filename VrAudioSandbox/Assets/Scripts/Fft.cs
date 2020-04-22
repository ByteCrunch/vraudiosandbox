using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFTWSharp;

// FFT uses FFTW - see http://www.fftw.org/index.html
// C# Wrapper based on the documentation of FFTWSharp - see https://github.com/tszalay/FFTWSharp

public class Fft
{
    System.IntPtr ptr;

    // pointers to FFTW plan objects
    System.IntPtr fplanForward, fplanBackward;

    private int n;

    private double[] window;
    public enum WindowType
    {
        flat,
        hann,
        hamming,
        blackman
    };
    private WindowType windowFunction;

    /// <summary>
    /// Creates array of window function factors for the current FFT size
    /// Windowing taken from https://github.com/101010b/AudioTest/blob/master/fft.cs
    /// </summary>
    public static double[] MakeWindow(int n, Fft.WindowType windowFunction)
    {
        double alpha, a0, a1, a2;

        double[] window = new double[n];

        switch (windowFunction)
        {
            case WindowType.hann:
                for (int i = 0; i < n; i++)
                    window[i] = 0.5 - 0.5 * System.Math.Cos((double)i * 2 * System.Math.PI / (n - 1));
                //amp_cf = 6;
                //pwr_cf = 4.3;
                break;

            case WindowType.hamming:
                for (int i = 0; i < n; i++)
                    window[i] = 0.54 - 0.46 * System.Math.Cos((double)i * 2 * System.Math.PI / (n - 1));
                //amp_cf = 5.35;
                //pwr_cf = 4.0;
                break;

            case WindowType.blackman:
                alpha = 0.16;
                a0 = (1.0 - alpha) / 2.0;
                a1 = 0.5;
                a2 = alpha / 2;
                for (int i = 0; i < n; i++)
                    window[i] =
                            a0
                        - a1 * System.Math.Cos((double)i * 2 * System.Math.PI / (n - 1))
                        + a2 * System.Math.Cos((double)i * 4 * System.Math.PI / (n - 1));
                //amp_cf = 7.54;
                //pwr_cf = 5.2;
                break;

            case WindowType.flat:
            default:
                for (int i = 0; i < n; i++)
                    window[i] = 1.0;
                //amp_cf = 0;
                //pwr_cf = 0;
                break;

        }

        return window;

    }


    /// <summary>
    /// Runs the FFT with double precision
    /// </summary>
    /// <param name="input">input array</param>
    /// <param name="output">array to store unnormalized transform ouput of FFT</param>
    /// <param name="real">set to true for converting double[] of real numbers to double[] of complex numbers with real and imagenary parts interleaved</param>
    /// <param name="wt">enum window_type for window function to use</param>
    public double[] RunFft(double[] input, bool real)
    {
        if (real)
            input = Fft.RealToComplex(input);

        this.n = input.Length;

        this.ptr = fftw.malloc(this.n * sizeof(double));

        //this.fplanForward = fftw.r2r_1d(this.n, this.pin, this.pout, fftw_kind.R2HC, fftw_flags.Measure);
        // (n / 2 because complex numbers are stored as pairs of doubles)
        this.fplanForward = fftw.dft_1d(this.n / 2, this.ptr, this.ptr, fftw_direction.Forward, fftw_flags.Measure);

        Marshal.Copy(input, 0, this.ptr, this.n);
        fftw.execute(this.fplanForward);
        double[] output = new double[this.n];
        Marshal.Copy(this.ptr, output, 0, this.n);

        fftw.free(this.ptr);
        fftw.destroy_plan(this.fplanForward);

        return output;
    }

    /// <summary>
    /// Runs the IFFT with double precision - RunFft() must be run before for initalization of fft size and window function
    /// </summary>
    /// <param name="input">unnormalized transform ouput of FFT</param>
    /// <param name="output">array to store output of IFFT</param>
    public double[] RunIfft(double[] input)
    {
        this.ptr = fftw.malloc(this.n * sizeof(double));

        // (n / 2 because complex numbers are stored as pairs of doubles)
        this.fplanBackward = fftw.dft_1d(this.n / 2, this.ptr, this.ptr, fftw_direction.Backward, fftw_flags.Measure);

        Marshal.Copy(input, 0, this.ptr, this.n);
        fftw.execute(this.fplanBackward);
        double[] output = new double[this.n];
        Marshal.Copy(this.ptr, output, 0, this.n);

        // FFTW computes an unnormalized transform, in that there is no coefficient in front of the summation in the DFT.
        // In other words, applying the forward and then the backward transform will multiply the input by n. 

        // Divide by n/2
        for (int i = 0; i < this.n; i++)
        {
            output[i] /= this.n / 2;
        }

        fftw.free(this.ptr);
        fftw.destroy_plan(this.fplanBackward);

        return output;
    }

    /// <summary>
    /// Converts a real number double[] to complex number double[] with zeros as imagenary part
    /// </summary>
    /// <param name="real">double[] input array of real numbers</param>
    /// <returns>double[] double the size of input with interleaved zeros</returns>
    private static double[] RealToComplex(double[] real)
    {
        int n = real.Length;

        double[] comp = new double[n * 2];
        for (int i = 0; i < n; i++)
        {
            comp[2 * i] = real[i];
        }
        return comp;
    }

    /// <summary>
    /// Calculates the absolute values for a double[] of complex numbers and normalize the transform.
    /// </summary>
    /// <param name="x">input double[] with real and imagenary parts interleaved</param>
    /// <returns>double[] with absolute values and normalized transform</returns>
    public static double[] GetMagnitudes(double[] x)
    {
        //double cf = 2.0 / 32767.0; // what is the meaning of this constant factor?
        double cf = 1.0;

        int n = x.Length / 2;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = cf * System.Math.Sqrt((x[2 * i] * x[2 * i] + x[2 * i + 1] * x[2 * i + 1]) / (n * n));
        }
        return y;
    }

    /// <summary>
    /// Calculates the phase information values for a double[] of complex fft data
    /// </summary>
    /// <param name="x">input double[] with real and imagenary parts interleaved</param>
    /// <returns>double[] with phase information</returns>
    public static double[] GetPhaseInformation(double[] x)
    {
        int n = x.Length / 2;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = System.Math.Atan2(x[2 * i + 1], x[2 * i]) * 180 / System.Math.PI;
        }
        return y;
    }

    /// <summary>
    /// Calculates power in dB
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double[] GetPowerInDb(double[] x)
    {
        int n = x.Length / 2;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = 10 * System.Math.Log(x[2 * i] * x[2 * i] + x[2 * i + 1] * x[2 * i + 1]) / System.Math.Log(10);
        }
        return y;
    }
}
