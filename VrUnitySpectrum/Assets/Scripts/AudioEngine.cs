using System.Collections;
using System.Collections.Generic;
using NAudio;
using UnityEngine;

public class AudioEngine : MonoBehaviour
{
    public string filePath;
    public double[][] fftData;
    private double[] _importDataAsSamples;
    public int importSampleRate;
    public int importBitDepth;
    public double[] fftFrequencies;

    void Start()
    {
        this.LoadAudioData();
        this.DoFft();
    }
    public void LoadAudioData()
    {
        //TODO this is still hardcoded for testing
        filePath = @"D:\Temp\test.wav";

        // Read in wav file and convert into an array of samples
        NAudio.Wave.WaveFileReader waveFileReader = new NAudio.Wave.WaveFileReader(filePath);
        int numOfBytes = (int)waveFileReader.Length;
        this.importSampleRate = waveFileReader.WaveFormat.SampleRate;
        this.importBitDepth = waveFileReader.WaveFormat.BitsPerSample;

        byte[] importDataAsBytes = new byte[numOfBytes];
        waveFileReader.Read(importDataAsBytes, 0, numOfBytes);
        waveFileReader.Close();


        // Convert into double array
        // 8, 16, 24, 32 and 64 bits per sample are supported for now
        int numOfSamples = numOfBytes / (this.importBitDepth / 8);
        this._importDataAsSamples = new double[numOfSamples];

        switch (this.importBitDepth)
        {
            case 8:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this._importDataAsSamples[i] = importDataAsBytes[i];
                }
                break;
            case 16:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this._importDataAsSamples[i] = (double)System.BitConverter.ToInt16(importDataAsBytes, i);
                }
                break;
            case 24:
                for (int i = 0; i < numOfSamples - 2; i++)
                {
                    this._importDataAsSamples[i] = (double)(importDataAsBytes[i] + (importDataAsBytes[i + 1] << 8) + (importDataAsBytes[i + 2]) << 16);
                }
                break;
            case 32:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this._importDataAsSamples[i] = (double)System.BitConverter.ToInt32(importDataAsBytes, i);
                }
                break;
            case 64:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this._importDataAsSamples[i] = (double)System.BitConverter.ToInt64(importDataAsBytes, i);
                }
                break;
            default:
                //TODO do some error handling here
                break;
        }
    }

    public void DoFft()
    {
        if (this._importDataAsSamples != null && this._importDataAsSamples.Length > 0)
        {
            // Calculate size of chunk that will be sent to FFT routine
            int numOfSamples = this._importDataAsSamples.Length;
            double durationInSecs = numOfSamples / this.importSampleRate;
            double chunkFactor = durationInSecs / 0.05; // 50ms per chunk
            int chunkSize = (int)(numOfSamples / chunkFactor);
            int numOfChunks = (int)System.Math.Ceiling(numOfSamples / chunkFactor);

            int fftSize = (int)(this.importSampleRate * 0.0232199546485261); // magic number taken from 44.100 Hz / 1024

            // Create map of frequencies for the bins
            this.fftFrequencies = new double[fftSize / 2];
            for (int i = 0; i < fftSize / 2; i++)
                this.fftFrequencies[i] = (double)i / (fftSize / 2) * this.importSampleRate / 2;

            // Do FFT per chunk
            double[][] input = new double[numOfChunks][];
            double[][] result = new double[numOfChunks][];

            Fft fft = new Fft(fftSize, Fft.WindowType.hamming, this.importSampleRate);

            for (int i = 0; i < numOfChunks; i++)
            {
                input[i] = new double[chunkSize];
                result[i] = new double[fftSize];

                for (int j = 0; j < chunkSize; j++)
                {
                    // last chunk might be smaller than chunkSize
                    if (this._importDataAsSamples.Length > (i * chunkSize) + j)
                    {
                        input[i][j] = this._importDataAsSamples[(i * chunkSize) + j];
                    } else
                    {
                        break;
                    }
                }
                fft.Run(input[i], result[i]);
            }
            this.fftData = result;
            fft.Dispose();
        }
    }

    void Update()
    {
        
    }
}
