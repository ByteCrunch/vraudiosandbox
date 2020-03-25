using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFTWSharp;

public class Ifft
{

    private System.IntPtr ptr, plan;

    public Ifft()
    {
        // TODO optimization: plan and ptr unmanaged memory could be re-used if data length is the same (should be the case except for the last block)
    }

    public double[] DoIfft(double[] data)
    {
        // input and output aire of the same length, so we can use just one memory block
        this.ptr = fftw.malloc(data.Length * sizeof(double));
        Marshal.Copy(data, 0, ptr, data.Length);

        this.plan = fftw.dft_1d(data.Length / 2, ptr, ptr, fftw_direction.Backward, fftw_flags.Estimate);
        fftw.execute(plan);

        double[] output = new double[data.Length];
        Marshal.Copy(ptr, output, 0, data.Length);

        // Clean up
        fftw.destroy_plan(this.plan);
        fftw.free(ptr);
        fftw.cleanup();

        return output;
    }
}
