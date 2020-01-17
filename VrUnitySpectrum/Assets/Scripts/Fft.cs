using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFTWSharp;

// FFT uses FFTW - see http://www.fftw.org/index.html
// C# Wrapper based on the documentation of FFTWSharp - see https://github.com/tszalay/FFTWSharp

public class Fft
{
    public enum WindowType
    {
        flat,
        hann,
        hamming,
        blackman
    };
    private WindowType winfunc;
    private int fftLength;
    private double[] fftWindow, fout, fin;
    private System.IntPtr pin, pout, fplan;
    public double ampCf, pwrCf;

    public Fft(int n, WindowType wt, int sampleRate)
    {
        this.fftLength = n;
        this.pin = fftw.malloc(n * 2 * sizeof(double));
        this.pout = fftw.malloc(n * 2 * sizeof(double));

        this.fin = new double[n];
        this.fout = new double[n];
        this.fftWindow = new double[n];
        
        this.winfunc = wt;
        this.MakeFftWindow();

        for (int i = 0; i < n; i++)
            this.fin[i] = 0.0;

        // http://www.fftw.org/fftw3_doc/The-1d-Real_002ddata-DFT.html#The-1d-Real_002ddata-DFT
        this.fplan = fftw.dft_r2c_1d(n, this.pin, this.pout, fftw_flags.Estimate);
    }

    private void MakeFftWindow()
    {
        double alpha, a0, a1, a2;

        switch (this.winfunc)
        {
            case WindowType.hann:
                for (int i = 0; i < this.fftLength; i++)
                    fftWindow[i] = 0.5 - 0.5 * System.Math.Cos((double)i * 2 * System.Math.PI / (this.fftLength - 1));
                this.ampCf = 6;
                this.pwrCf = 4.3;
                break;

            case WindowType.hamming:
                for (int i = 0; i < this.fftLength; i++)
                    fftWindow[i] = 0.54 - 0.46 * System.Math.Cos((double)i * 2 * System.Math.PI / (this.fftLength - 1));
                this.ampCf = 5.35;
                this.pwrCf = 4.0;
                break;

            case WindowType.blackman:
                alpha = 0.16;
                a0 = (1.0 - alpha) / 2.0;
                a1 = 0.5;
                a2 = alpha / 2;
                for (int i = 0; i < this.fftLength; i++)
                    fftWindow[i] =
                        a0
                        - a1 * System.Math.Cos((double)i * 2 * System.Math.PI / (this.fftLength - 1))
                        + a2 * System.Math.Cos((double)i * 4 * System.Math.PI / (this.fftLength - 1));
                this.ampCf = 7.54;
                this.pwrCf = 5.2;
                break;

            case WindowType.flat:
            default:
                for (int i = 0; i < this.fftLength; i++)
                    fftWindow[i] = 1.0;
                this.ampCf = 0;
                this.pwrCf = 0;
                break;

        }

    }

    private void DoFft(double[] outp)
    {
        double c = this.fftLength * this.fftLength;
        double cf = 2.0 / 32767.0;

        Marshal.Copy(this.fin, 0, this.pin, this.fftLength);
        fftwf.execute(this.fplan);

        Marshal.Copy(this.pout, this.fout, 0, this.fftLength);
        for (int i = 0; i < this.fftLength / 2; i++)
            outp[i] = cf * System.Math.Sqrt((this.fout[2 * i] * this.fout[2 * i] + this.fout[2 * i + 1] * this.fout[2 * i + 1]) / c);


    }

    public void Run(double[] inp, double[] outp)
    {
        for (int i = 0; i < this.fftLength; i++)
            this.fin[i] = inp[i] * this.fftWindow[i];
        this.DoFft(outp);
    }
    public void Dispose()
    {
        fftwf.free(this.pin);
        fftwf.free(this.pout);
        fftwf.destroy_plan(this.fplan);
    }
}
