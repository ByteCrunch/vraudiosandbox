using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFTWSharp;

// FFT uses FFTW - see http://www.fftw.org/index.html
// C# Wrapper based on the documentation of FFTWSharp - see https://github.com/tszalay/FFTWSharp

public class Fft
{
    System.IntPtr pin, pout;
    private float[] fin, fout;

    // pointers to FFTW plan objects
    System.IntPtr fplanForward, fplanBackward;

    private int fftSize;

    public Fft(int n)
    {
        this.fftSize = n;

        this.pin = fftwf.malloc(n * 2 * sizeof(float));
        this.pout = fftwf.malloc(n * 2 * sizeof(float));

        // TODO check if fftw_flags.Measure makes a huge difference
        this.fplanForward = fftwf.r2r_1d(n, this.pin, this.pout, fftw_kind.R2HC, fftw_flags.Estimate);
        this.fplanBackward = fftwf.r2r_1d(n, this.pout, this.pin, fftw_kind.HC2R, fftw_flags.Estimate);
    }

    public void RunFft(float[] input, float[] output)
    {
        this.fin = input;
        this.fout = output;

        Marshal.Copy(this.fin, 0, this.pin, this.fftSize);
        fftwf.execute(this.fplanForward);
        Marshal.Copy(this.pout, this.fout, 0, this.fftSize);

        
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = System.Math.Abs(output[i]);
        }
    }

    public void RunIfft(float[] input, float[] output)
    {
        // Transforms are unnormalized, so r2hc followed by hc2r will result in the original data multiplied by n.
        // Furthermore, like the c2r transform, an out-of-place hc2r transform will destroy its input array. 
        // (http://www.fftw.org/fftw3_doc/The-Halfcomplex_002dformat-DFT.html#The-Halfcomplex_002dformat-DFT)

        float[] inputCopy = new float[input.Length];
        System.Array.Copy(input, inputCopy, input.Length);
        this.fin = inputCopy;
        this.fout = output;

        Marshal.Copy(this.fin, 0, this.pin, this.fftSize);
        fftwf.execute(this.fplanBackward);
        Marshal.Copy(this.pout, this.fout, 0, this.fftSize);

        // divide by n
        for (int i = 0; i < output.Length; i++)
        {
            output[i] /= this.fftSize;
        }
    }

    public void Dispose()
    {
        fftwf.free(this.pin);
        fftwf.free(this.pout);
        fftwf.destroy_plan(this.fplanForward);
        fftwf.destroy_plan(this.fplanBackward);
    }
}
