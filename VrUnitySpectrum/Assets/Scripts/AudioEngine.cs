using System.Collections;
using System.Collections.Generic;
using NAudio;
using UnityEngine;

public class AudioEngine : MonoBehaviour
{
    public string filePath;
    public static double[][] fftData;
    private byte[] _importData;
    public int importSampleRate;
    public int importBitDepth;
    public double[] fftFrequencies;

    void Start()
    {
        filePath = @"D:\Temp\test.wav";

        // Read in wav file
        NAudio.Wave.WaveFileReader waveFileReader = new NAudio.Wave.WaveFileReader(filePath);
        this._importData = new byte[waveFileReader.Length];
        this.importSampleRate = waveFileReader.WaveFormat.SampleRate;
        this.importBitDepth = waveFileReader.WaveFormat.BitsPerSample;

        waveFileReader.Read(_importData, 0, (int)waveFileReader.Length);
        waveFileReader.Close();

        int numOfBytes = (int)waveFileReader.Length;
        int numOfSamples = numOfBytes / (this.importBitDepth / 8);
        double durationInSecs = numOfSamples / this.importSampleRate;
        double chunkFactor = durationInSecs / 0.05; // 50ms
        int chunkSize = (int)(numOfSamples / chunkFactor);
        int numOfChunks = (int)System.Math.Ceiling(numOfSamples / chunkFactor);
        int fftSize = (int)(this.importSampleRate * 0.0232199546485261); // magic number taken from 1024 for 44.100 Hz

        Debug.Log("sampleRate: " + this.importSampleRate.ToString());
        Debug.Log("numOfBytes: " + numOfBytes.ToString());
        Debug.Log("numOfSamples: " + numOfSamples.ToString());
        Debug.Log("durationInSecs: " + durationInSecs.ToString());
        Debug.Log("chunkSize: " + chunkSize.ToString());
        Debug.Log("numOfChunks: " + numOfChunks.ToString());
        Debug.Log("fftSize: " + fftSize.ToString());

        // Create map of frequencies for the all the bins
        this.fftFrequencies = new double[fftSize / 2];
        for (int i = 0; i < fftSize / 2; i++)
            this.fftFrequencies[i] = (double)i / (fftSize / 2) * this.importSampleRate / 2;

        double[][] input = new double[numOfChunks][];
        double[][] result = new double[numOfChunks][];
        fftData = new double[numOfChunks][];

        Fft fft = new Fft(fftSize, Fft.WindowType.hamming, this.importSampleRate);

        for (int i=0; i < numOfChunks; i++)
        {
            input[i] = new double[chunkSize * 2];
            result[i] = new double[fftSize];
            fftData[i] = new double[fftSize];

            // Assume for now everything is 16 bits per sample
            for (int j=0; j < chunkSize * 2; j += 2)
            {
                // abort if last chunk is smaller
                if (i * j > numOfSamples)
                    return;
                input[i][j] = (double)System.BitConverter.ToInt16(this._importData, (i*chunkSize)+j);
            }
            fft.Run(input[i], result[i]);
            fftData[i] = result[i];
        }
        fft.Dispose();
    }

    void Update()
    {
        
    }
}
