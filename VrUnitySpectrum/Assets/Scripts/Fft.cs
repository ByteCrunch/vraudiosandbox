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
    private WindowType _winfunc;
    private int _fftLength;
    private double[] _fftWindow, _fout, _fin;
    private System.IntPtr _pin, _pout, _fplan;
    public double ampCf, pwrCf;

    public Fft(int n, WindowType wt, int sampleRate)
    {
        this._fftLength = n;
        this._pin = fftw.malloc(n * 2 * sizeof(double));
        this._pout = fftw.malloc(n * 2 * sizeof(double));

        this._fin = new double[n];
        this._fout = new double[n];
        this._fftWindow = new double[n];
        
        this._winfunc = wt;
        this.MakeFftWindow();

        for (int i = 0; i < n; i++)
            this._fin[i] = 0.0;

        this._fplan = fftw.dft_r2c_1d(n, this._pin, this._pout, fftw_flags.Estimate);
    }

    private void MakeFftWindow()
    {
        double alpha, a0, a1, a2;

        switch (this._winfunc)
        {
            case WindowType.hann:
                for (int i = 0; i < this._fftLength; i++)
                    _fftWindow[i] = 0.5 - 0.5 * System.Math.Cos((double)i * 2 * System.Math.PI / (this._fftLength - 1));
                this.ampCf = 6;
                this.pwrCf = 4.3;
                break;

            case WindowType.hamming:
                for (int i = 0; i < this._fftLength; i++)
                    _fftWindow[i] = 0.54 - 0.46 * System.Math.Cos((double)i * 2 * System.Math.PI / (this._fftLength - 1));
                this.ampCf = 5.35;
                this.pwrCf = 4.0;
                break;

            case WindowType.blackman:
                alpha = 0.16;
                a0 = (1.0 - alpha) / 2.0;
                a1 = 0.5;
                a2 = alpha / 2;
                for (int i = 0; i < this._fftLength; i++)
                    _fftWindow[i] =
                        a0
                        - a1 * System.Math.Cos((double)i * 2 * System.Math.PI / (this._fftLength - 1))
                        + a2 * System.Math.Cos((double)i * 4 * System.Math.PI / (this._fftLength - 1));
                this.ampCf = 7.54;
                this.pwrCf = 5.2;
                break;

            case WindowType.flat:
            default:
                for (int i = 0; i < this._fftLength; i++)
                    _fftWindow[i] = 1.0;
                this.ampCf = 0;
                this.pwrCf = 0;
                break;

        }

    }

    private void DoFft(double[] outp)
    {
        double c = this._fftLength * this._fftLength;
        double cf = 2.0 / 32767.0;

        Marshal.Copy(this._fin, 0, this._pin, this._fftLength);
        fftwf.execute(this._fplan);

        Marshal.Copy(this._pout, this._fout, 0, this._fftLength);
        for (int i = 0; i < this._fftLength / 2; i++)
            outp[i] = cf * System.Math.Sqrt((this._fout[2 * i] * this._fout[2 * i] + this._fout[2 * i + 1] * this._fout[2 * i + 1]) / c);


    }

    public void Run(double[] inp, double[] outp)
    {
        for (int i = 0; i < this._fftLength; i++)
            this._fin[i] = inp[i] * this._fftWindow[i];
        this.DoFft(outp);
    }
    public void Dispose()
    {
        fftwf.free(this._pin);
        fftwf.free(this._pout);
        fftwf.destroy_plan(this._fplan);
    }
}
