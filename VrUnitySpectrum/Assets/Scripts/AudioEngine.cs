using System.Collections;
using System.Collections.Generic;
using NAudio;
using UnityEngine;

public class AudioEngine : MonoBehaviour
{
    public string filePath;

    [HideInInspector]
    public double[][] fftData;

    private double[] importDataAsSamples;
    public int importSampleRate;
    public int importBitDepth;
    public double[] fftFrequencies;
    public int fftBinCount;

    // Make sure audio engine is loaded before Start() routines of other GameObjects
    void Awake()
    {
        this.LoadAudioData();
        this.DoFft();
    }
    public void LoadAudioData()
    {
        //TODO this is still hardcoded for testing
        //filePath = @"E:\Temp\test.wav";
        //filePath = @"E:\Temp\sinus4000hz-10db.wav";
        filePath = @"E:\Temp\audiocheck.net_hdsweep_1Hz_48000Hz_-3dBFS_30s.wav";

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
        this.importDataAsSamples = new double[numOfSamples];

        switch (this.importBitDepth)
        {
            case 8:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this.importDataAsSamples[i] = importDataAsBytes[i];
                }
                break;
            case 16:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this.importDataAsSamples[i] = (double)System.BitConverter.ToInt16(importDataAsBytes, i);
                }
                break;
            case 24:
                for (int i = 0; i < numOfSamples - 2; i++)
                {
                    this.importDataAsSamples[i] = (double)(importDataAsBytes[i] + (importDataAsBytes[i + 1] << 8) + (importDataAsBytes[i + 2]) << 16);
                }
                break;
            case 32:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this.importDataAsSamples[i] = (double)System.BitConverter.ToInt32(importDataAsBytes, i);
                }
                break;
            case 64:
                for (int i = 0; i < numOfSamples; i++)
                {
                    this.importDataAsSamples[i] = (double)System.BitConverter.ToInt64(importDataAsBytes, i);
                }
                break;
            default:
                //TODO do some error handling here
                break;
        }
    }

    public void DoFft()
    {
        if (this.importDataAsSamples != null && this.importDataAsSamples.Length > 0)
        {
            int fftSize = (int)(this.importSampleRate * 0.0232199546485261); // magic number taken from 44.100 Hz / 1024
            //int fftSize = 2048;
            this.fftBinCount = fftSize / 2;

            // Calculate size of chunk that will be sent to FFT routine
            //int chunkSize = this.importSampleRate / 2 / 100; // 50ms
            //int chunkSize = this.importSampleRate / 2; // 500ms
            int chunkSize = fftSize;
            int numOfChunks = (this.importDataAsSamples.Length + chunkSize - 1) / chunkSize; // integer round up

            Debug.Log("numOfSamples: " + this.importDataAsSamples.Length.ToString() + " chunkSize: " + chunkSize.ToString() + " numOfChunks: " + numOfChunks.ToString() + " binResolution: " + (this.importSampleRate / fftSize).ToString() + "Hz");

            // Create map of frequencies for the bins
            this.fftFrequencies = new double[this.fftBinCount];
            for (int i = 0; i < this.fftBinCount; i++)
                this.fftFrequencies[i] = (double)i / this.fftBinCount * this.importSampleRate / 2;

            // Do FFT per chunk
            double[][] input = new double[numOfChunks][];
            double[][] result = new double[numOfChunks][];

            Fft fft = new Fft(fftSize, Fft.WindowType.hamming, this.importSampleRate);

            for (int i = 0; i < numOfChunks; i++)
            {
                input[i] = new double[chunkSize];

                // Due to aliasing the second part would be redundant, so the ouput array is fftSize / 2 + 1
                // as there is no need to store mirrored information
                result[i] = new double[fftSize / 2 + 1];

                for (int j = 0; j < chunkSize; j++)
                {
                    // last chunk might be smaller than chunkSize or chunkSize smaller than fft size
                    if (this.importDataAsSamples.Length > i * chunkSize + j)
                    {
                        input[i][j] = this.importDataAsSamples[i * chunkSize + j];
                    } else {
                        // fill with zeros
                        input[i][j] = 0.0;
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
