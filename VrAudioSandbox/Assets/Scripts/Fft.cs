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
    public double amp_cf;
    public double pwr_cf;


    /// <summary>
    /// Creates array of window function factors for the current FFT size
    /// </summary>
    // windowing code taken from: https://github.com/101010b/AudioTest/blob/master/fft.cs
    private void MakeWindow()
    {
        double alpha, a0, a1, a2;

        this.window = new double[this.n];

        switch (this.windowFunction)
        {
            case WindowType.hann:
                for (int i = 0; i < this.n; i++)
                    this.window[i] = 0.5 - 0.5 * System.Math.Cos((double)i * 2 * System.Math.PI / (this.n - 1));
                amp_cf = 6;
                pwr_cf = 4.3;
                break;

            case WindowType.hamming:
                for (int i = 0; i < this.n; i++)
                    this.window[i] = 0.54 - 0.46 * System.Math.Cos((double)i * 2 * System.Math.PI / (this.n - 1));
                amp_cf = 5.35;
                pwr_cf = 4.0;
                break;

            case WindowType.blackman:
                alpha = 0.16;
                a0 = (1.0 - alpha) / 2.0;
                a1 = 0.5;
                a2 = alpha / 2;
                for (int i = 0; i < this.n; i++)
                    this.window[i] =
                            a0
                        - a1 * System.Math.Cos((double)i * 2 * System.Math.PI / (this.n - 1))
                        + a2 * System.Math.Cos((double)i * 4 * System.Math.PI / (this.n - 1));
                amp_cf = 7.54;
                pwr_cf = 5.2;
                break;

            case WindowType.flat:
            default:
                for (int i = 0; i < this.n; i++)
                    this.window[i] = 1.0;
                amp_cf = 0;
                pwr_cf = 0;
                break;

        }

    }


    /// <summary>
    /// Runs the FFT with double precision
    /// </summary>
    /// <param name="input">input array</param>
    /// <param name="output">array to store unnormalized transform ouput of FFT</param>
    /// <param name="real">set to true for converting double[] of real numbers to double[] of complex numbers with real and imagenary parts interleaved</param>
    /// <param name="wt">enum window_type for window function to use</param>
    public double[] RunFft(double[] input, bool real, WindowType wt)
    {
        if (real)
            input = Fft.RealToComplex(input);

        this.n = input.Length;

        // Apply window function
        this.windowFunction = wt;
        this.MakeWindow();
        for (int i = 0; i < this.n; i++)
        {
            input[i] *= this.window[i];
        }

        this.ptr = fftw.malloc(this.n * sizeof(double));

        //this.fplanForward = fftw.r2r_1d(this.n, this.pin, this.pout, fftw_kind.R2HC, fftw_flags.Measure);
        // (n / 2 because complex numbers are stored as pairs of doubles)
        this.fplanForward = fftw.dft_1d(this.n / 2, this.ptr, this.ptr, fftw_direction.Forward, fftw_flags.Measure);

        Marshal.Copy(input, 0, this.ptr, this.n);
        fftw.execute(this.fplanForward);
        double[] output = new double[this.n];
        Marshal.Copy(this.ptr, output, 0, this.n);

        return output;
    }

    /// <summary>
    /// Runs the IFFT with double precision - RunFft() must be run before for initalization of fft size and window function
    /// </summary>
    /// <param name="input">unnormalized transform ouput of FFT</param>
    /// <param name="output">array to store output of IFFT</param>
    public double[] RunIfft(double[] input)
    {

        //this.fplanBackward = fftw.r2r_1d(this.n, this.pin, this.pout, fftw_kind.HC2R, fftw_flags.Measure);
        // (n / 2 because complex numbers are stored as pairs of doubles)
        this.fplanBackward = fftw.dft_1d(this.n / 2, this.ptr, this.ptr, fftw_direction.Backward, fftw_flags.Measure);

        Marshal.Copy(input, 0, this.ptr, this.n);
        fftw.execute(this.fplanBackward);
        double[] output = new double[this.n]; 
        Marshal.Copy(this.ptr, output, 0, this.n);

        // FFTW computes an unnormalized transform, in that there is no coefficient in front of the summation in the DFT.
        // In other words, applying the forward and then the backward transform will multiply the input by n. 
        
        // Revert windowing and divide by n/2
        for (int i = 0; i > this.n; i++)
        {
            output[i] = output[i] / this.window[i] / (this.n / 2);
        }

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
    public double[] MagnitudesComplex(double[] x)
    {
        //double cf = 2.0 / 32767.0; // what is the meaning of this constant factor?
        double cf = 1.0;

        int n = x.Length / 2;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = cf * System.Math.Sqrt((x[2 * i] * x[2 * i] + x[2 * i + 1] * x[2 * i + 1]) / (this.n * this.n));
        }
        return y;
    }

    /// <summary>
    /// Calculates the absolute values for a double[] of real numbers and normalize the transform.
    /// </summary>
    /// <param name="x">input double[] with real numbers</param>
    /// <returns>double[] with absolute values and normalized transform</returns>
    public double[] MagnitudesReal(double[] x)
    {

        //double cf = 2.0 / 32767.0; // what is the meaning of this constant factor?
        double cf = 1.0;

        int n = x.Length;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = cf * System.Math.Abs(x[i]) / this.n;
        }
        return y;
    }

    /// <summary>
    /// Calculates dB for given magnitude values
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public double[] dB(double[] x)
    {
        int n = x.Length;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = (double)(20 * System.Math.Log(x[i]));
        }
        return y;
    }

    /// <summary>
    /// Deallocates resources
    /// </summary>
    public void Dispose()
    {
        fftwf.free(this.ptr);
        fftwf.destroy_plan(this.fplanForward);
        fftwf.destroy_plan(this.fplanBackward);
    }
}
