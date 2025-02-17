﻿using System.Collections;
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
                break;

            case WindowType.hamming:
                for (int i = 0; i < n; i++)
                    window[i] = 0.54 - 0.46 * System.Math.Cos((double)i * 2 * System.Math.PI / (n - 1));
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
                break;

            case WindowType.flat:
            default:
                for (int i = 0; i < n; i++)
                    window[i] = 1.0;
                break;

        }
        return window;
    }


    /// <summary>
    /// Runs the FFT with double precision
    /// </summary>
    /// <param name="input">input array</param>
    /// <param name="real">set to true for converting double[] of real numbers to double[] of complex numbers with real and imagenary parts interleaved</param>
    public double[] RunFft(double[] input, bool real)
    {
        if (real)
            input = Fft.RealToComplex(input);

        this.n = input.Length;

        this.ptr = fftw.malloc(this.n * sizeof(double));

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

        /*
         * FFTW computes an unnormalized transform, in that there is no coefficient in front of the summation in the DFT.
         * In other words, applying the forward and then the backward transform will multiply the input by n. 
         * http://www.fftw.org/fftw3_doc/The-1d-Discrete-Fourier-Transform-_0028DFT_0029.html
         */

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
    /// Calculates the absolute values for a double[] of complex numbers.
    /// </summary>
    /// <param name="x">input double[] with real and imagenary parts interleaved</param>
    /// <returns>double[] with absolute values</returns>
    public static double[] GetMagnitudes(double[] x)
    {
        int n = x.Length / 2;
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            y[i] = System.Math.Sqrt(x[2 * i] * x[2 * i] + x[2 * i + 1] * x[2 * i + 1]);
        }
        return y;
    }

    /// <summary>
    /// Calculates the phase information values for a double[] of complex fft data WITH threshold limit
    /// </summary>
    /// <param name="fftData">input double[] of FFT data with real and imagenary parts interleaved</param>
    /// <param name="magnitudes">corresponding double[] magnitudes (size half of fftData array)</param>
    /// <returns>double[] with phase information</returns>
    public static double[] GetPhaseInformation(double[] fftData, double[] magnitudes)
    {
        int n = fftData.Length / 2;
        double[] y = new double[n];
        double threshold = 0;

        // Use threshold limit if magnitudes were provided
        /*
            * "Even a small floating rounding off error will amplify the result and manifest incorrectly
            * as useful phase information. [...] The solution is to define a tolerance threshold and
            * ignore all the computed phase values that are below the threshold."
            * https://www.gaussianwaves.com/2015/11/interpreting-fft-results-obtaining-magnitude-and-phase-information/
            */

        // Find abs(maximum) of provided magnitudes data...
        double max = 0;
        for (int i = 0; i < n; i++)
        {
            double m = magnitudes[i];
            if (m > max)
                max = m;
        }
        // ...and use 1/10000th of it as threshold
        threshold = max / 10000;

        // Calculate phase information with above threshold
        for (int i = 0; i < n; i++)
        {
            if (magnitudes[i] > threshold)
            {
                // Calculate phase information
                y[i] = System.Math.Atan2(fftData[2 * i + 1], fftData[2 * i]);
            } else {
                // Ignore value
                y[i] = 0;
            }
        }       
        return y;
    }

    /// <summary>
    /// Returns FFT complex data for use in IFFT from magnitudes and phase information
    /// </summary>
    /// <param name="magnitudes">double[] with magnitudes</param>
    /// <param name="phases">double[] with phase information</param>
    /// <returns>FFT data as interleaved complex format double[]</returns>
    public static double[] GetFftDataFromMagnitudeAndPhase(double[] magnitudes, double[] phases)
    {
        int n = magnitudes.Length;
        double[] fftData = new double[n * 2];

        for (int i = 0; i < n; i++)
        {
            fftData[i * 2] = magnitudes[i] * System.Math.Cos(phases[i]); // real part
            fftData[i * 2 + 1] = magnitudes[i] * System.Math.Sin(phases[i]); // imagenary part
        }

        return fftData;
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
