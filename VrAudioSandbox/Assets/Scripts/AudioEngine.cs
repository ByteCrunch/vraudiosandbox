﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NAudio;
using SimpleFileBrowser;
using UnityEngine;

public class AudioEngine : MonoBehaviour
{
    // just for testing in Unity play mode
    public enum testFiles
    {
        sinesweep1HzTo48000Hz,
        sine4000Hz,
        sine100Hz,
        test,
        drumloop
    }
    public testFiles selectAudioTestFile;

    public string filePath;

    [HideInInspector]
    public double[][] fftData;
    public double[][] fftDataMagnitudes;
    public double[][] fftDataPhases;

    public double[][] ifftData;

    private NAudio.Wave.WaveFileReader waveReader;
    private NAudio.Wave.WaveOutEvent waveOut;
    private NAudio.Wave.WaveStream waveProvider;
    private LoopStream loopStream;

    private float[] audioData;
    private byte[] playbackBuffer;
    private System.IO.Stream memoryStream;

    public int importSampleRate;
    public int importBitDepth;
    public double importDurationInMs;
    public int importChannels;
    public int audioNumOfChunks;
    public int fftSize;
    public int fftNumOfChunks;
    public float fftOverlapPercent;
    public int fftOverlapOffset;
    public double[] fftFrequencies;
    public int fftBinCount;

    public bool isPlaying;

    private SpectrumMeshGenerator spectrum;
    private Fft fft;

    /// <summary>
    /// Makes sure audio engine is loaded before Start() routines of other GameObjects
    /// </summary>
    void Awake()
    {

        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();

        if (!Application.isEditor)
        {
            // Show file dialog when in standalone mode
            FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio", ".wav", ".aiff", ".mp3", ".m4a", ".ogg"));
            FileBrowser.AddQuickLink("Examples", Application.dataPath + "/Resources/Audio/", null);
            StartCoroutine(ShowLoadDialogCoroutine());
        } else {

            // Load audio data based on selection in AudioEngine component in Unity editor mode
            switch (this.selectAudioTestFile)
            {
                case testFiles.sinesweep1HzTo48000Hz:
                    this.filePath = Application.dataPath + "/Resources/Audio/" + "sinesweep_1Hz_48000Hz_-3dBFS_30s.wav";
                    break;

                case testFiles.sine4000Hz:
                    this.filePath = Application.dataPath + "/Resources/Audio/" + "sinus4000hz-10db.wav";
                    break;

                case testFiles.sine100Hz:
                    this.filePath = Application.dataPath + "/Resources/Audio/" + "sinus100hz-10db.wav";
                    break;

                case testFiles.test:
                    this.filePath = Application.dataPath + "/Resources/Audio/" + "test.wav";
                    break;

                case testFiles.drumloop:
                    this.filePath = Application.dataPath + "/Resources/Audio/" + "drumloop.wav";
                    break;
            }
            this.LoadAudioData();
        }
    }

    /// <summary>
    /// Shows file browser dialog in standalone mode
    /// </summary>
    IEnumerator ShowLoadDialogCoroutine()
    {
        // Show a load file dialog and wait for a response from user
        yield return FileBrowser.WaitForLoadDialog(false, null, "Load audio file", "Load");

        Debug.Log(FileBrowser.Success + " " + FileBrowser.Result);

        if (FileBrowser.Success)
        {
            this.filePath = FileBrowser.Result;
            this.LoadAudioData();
        }
    }

    /// <summary>
    /// Loads audio data from external file
    /// </summary>
    public void LoadAudioData()
    {
        // Read in wav file and convert into a float[] of samples
        this.waveReader = new NAudio.Wave.WaveFileReader(this.filePath);

        this.importSampleRate = this.waveReader.WaveFormat.SampleRate;
        this.importBitDepth = this.waveReader.WaveFormat.BitsPerSample;
        this.importChannels = this.waveReader.WaveFormat.Channels; //TODO multi channel support
        this.importDurationInMs = this.waveReader.TotalTime.TotalMilliseconds;

        long numOfSamples = this.waveReader.SampleCount * this.importChannels;
        this.audioData = new float[numOfSamples];

        float[] sample;
        int pos = 0;

        while ((sample = this.waveReader.ReadNextSampleFrame()) != null)
        {
            if (sample.Length > 1) {
                //TODO multi channel support - only take first channel for now
                Debug.Log("<AudioEngine> multi-channel audio files are not supported right now - so only first channel is used, number of channels in the input file: " + this.importChannels.ToString());
                this.importChannels = 1; //override, because only mono supported for now
            }
            this.audioData[pos] = sample[0];
            pos++;
        }

        this.DoFft();
        this.spectrum.Init();
        this.DoIfft();
    }

    /// <summary>
    /// Start playback of IFFT audio data
    /// </summary>
    public void Play()
    {
        Debug.Log("<AudioEngine> Play");
        if (this.waveOut == null)
        {
            this.waveProvider = new NAudio.Wave.RawSourceWaveStream(
                this.memoryStream,
                NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(this.importSampleRate, this.importChannels)
                );
            this.waveOut = new NAudio.Wave.WaveOutEvent();
            this.waveOut.PlaybackStopped += OnPlaybackStopped;
            this.loopStream = new LoopStream(this.waveProvider);
            this.waveOut.Init(this.loopStream);
            this.waveOut.Play();
        } else {
            this.waveOut.Play();
        }
        this.isPlaying = true;
    }

    /// <summary>
    /// Stop playback of IFFT audio data
    /// </summary>
    public void Stop() 
    {
        this.waveOut.Stop();
        this.isPlaying = false;
        this.spectrum.ResetMeshColors();
        
        Debug.Log("<AudioEngine> Stop");
    }

    private void OnPlaybackStopped(object sender, System.EventArgs e)
    {
        Debug.Log("<AudioEngine> Playback stopped");
    }

    /// <summary>
    /// Provides current playback position
    /// </summary>
    /// <returns>Playback position in milliseconds</returns>
    public double GetPositionInMs()
    {
        long bytePos = this.loopStream.Position;
        //long bytePos = this.memoryStream.Position;
        double ms = bytePos / this.importSampleRate / 4 * 1000.0;

        // Loop
        if (bytePos >= this.memoryStream.Length)
            this.Rewind();

        return ms;
    }

    /// <summary>
    /// Set looping of audio playback
    /// </summary>
    /// <param name="loop">true: enable looping, false: disable looping</param>
    public void SetAudioLooping(bool loop)
    {
        this.loopStream.EnableLooping = loop;
    }

    /// <summary>
    /// Rewind playback of IFFT audio data
    /// </summary>
    public void Rewind()
    {
        Debug.Log("<AudioEngine> Rewind");
        if (this.waveOut != null)
        {
            this.loopStream.Position = 0;
            //this.memoryStream.Position = 0;
            this.spectrum.ResetMeshColors();
        }
    }

    /// <summary>
    /// Free audio resources
    /// </summary>
    public void StopAudioEngine()
    {
        if (this.waveOut != null)
        {
            this.waveOut.Dispose();
            this.waveOut = null;
        }

        if (this.waveReader != null)
        {
            this.waveReader.Close();
            this.waveReader.Dispose();
            this.waveReader = null;
        }

        this.isPlaying = false;
    }

    /// <summary>
    /// Create overlapping & windowing and perform FFT
    /// </summary>
    public void DoFft()
    {
        if (this.audioData != null && this.audioData.Length > 0)
        {
            this.fftSize = (int)(this.importSampleRate * 0.0232199546485261); // magic number taken from 44.100 Hz / 1024
            // Make even if odd
            if (this.fftSize % 2 != 0)
                this.fftSize--;

            this.fftBinCount = fftSize / 2;

            this.fft = new Fft();

            // Calculate number of chunks for the given FFT size...
            this.audioNumOfChunks = (this.audioData.Length + this.fftSize) / this.fftSize; // integer round up
            // ... with overlapping:
            this.fftNumOfChunks = (int)(this.audioNumOfChunks / this.fftOverlapPercent) - 1;

            Debug.Log("<AudioEngine> numOfSamples: " + this.audioData.Length.ToString() +
                " fftSize: " + this.fftSize.ToString() +
                " numberOfChunks: " + this.audioNumOfChunks.ToString() +
                " numberOfChunks with " + (100 * this.fftOverlapPercent).ToString() + "% overlap: " + this.fftNumOfChunks.ToString() +
                " binResolution: " + (this.importSampleRate / 2 / this.fftSize).ToString() + "Hz"
                );

            
            // Create map of frequencies for the bins
            this.fftFrequencies = new double[this.fftBinCount];
            for (int i = 0; i < this.fftBinCount; i++)
                this.fftFrequencies[i] = (double)i / this.fftBinCount * this.importSampleRate / 2;

            // Get window function factors
            double[] window = Fft.MakeWindow(this.fftSize, Fft.WindowType.hann); // Don't change window function, only Von-Hann supported right now

            // Chunk data into overlapping parts, apply window function and run FFT
            double[][] input = new double[this.fftNumOfChunks][];
            double[][] result = new double[this.fftNumOfChunks][];
            double[][] magnitudes = new double[this.fftNumOfChunks][];
            double[][] phases = new double[this.fftNumOfChunks][];

            int idx = 0;
            this.fftOverlapOffset = (int)(this.fftSize * this.fftOverlapPercent);
            for (int i = 0; i < this.fftNumOfChunks; i++)
            {
                input[i] = new double[this.fftSize];
                result[i] = new double[this.fftSize * 2];
                magnitudes[i] = new double[this.fftSize];
                phases[i] = new double[this.fftSize];

                for (int j = 0; j < this.fftSize; j++)
                {
                    // last chunk might be smaller than fftSize...
                    if (this.audioData.Length > idx)
                    {
                        input[i][j] = this.audioData[idx] * window[j];

                    } else {
                        // ... fill up with zeros then
                        input[i][j] = 0;
                    }
                    idx++;
                }

                // Reset index for overlap
                idx -= this.fftOverlapOffset;

                result[i] = this.fft.RunFft(input[i], true);
                magnitudes[i] = Fft.GetMagnitudes(result[i]);
                phases[i] = Fft.GetPhaseInformation(result[i], magnitudes[i]);
                //phases[i] = Fft.GetPhaseInformation(result[i]);

                this.fftData = result;
                this.fftDataMagnitudes = magnitudes;
                this.fftDataPhases = phases;
            }
        }
    }

    /// <summary>
    /// Performs IFFT and removes overlapping & windowing
    /// </summary>
    public void DoIfft()
    {
        if (this.fftData != null && this.fftData.Length > 0)
        {
            // Do IFFT per chunk
            double[][] result = new double[this.fftData.Length][];

            for (int i = 0; i < this.fftData.Length; i++)
            {
                // result of ifft is in interleaved complex format - take only even indexes (the real part)
                /*result[i] = fft.RunIfft(this.fftData[i])
                    .Where((value, index) => index % 2 == 0).ToArray();*/
                result[i] = fft.RunIfft(Fft.GetFftDataFromMagnitudeAndPhase(this.fftDataMagnitudes[i], this.fftDataPhases[i])) // Testing with fftData from magnitudes and phase
                    .Where((value, index) => index % 2 == 0).ToArray();
            }

            // Get window function factors
            double[] window = Fft.MakeWindow(this.fftSize, Fft.WindowType.hann);

            // By using Von-Hann-Window with 50% overlap we can simply sum the values in the overlap region together to revert the windowing 
            // and to get the correct sample count again
            this.ifftData = new double[this.audioNumOfChunks][];

            for (int i = 0; i < this.audioNumOfChunks; i++)
            {
                this.ifftData[i] = new double[this.fftSize];

                for (int j = 0; j < this.fftSize; j++)
                {
                    // Copy over and revert window function for the values of the non-overlap region from the first chunk AND...
                    if (i == 0 && j < this.fftSize - this.fftOverlapOffset)
                    {
                        this.ifftData[i][j] = result[i][j] / window[j];
                        continue;
                    }

                    // ...of the non-overlap region at the end
                    if (i >= this.audioNumOfChunks - 1 && j >= this.fftSize - this.fftOverlapOffset)
                    {
                        this.ifftData[i][j] = result[i * 2][j] / window[j];
                        continue;
                    }
                    
                    // Sum overlapping values (this code only works for 50% overlap)
                    if (j < this.fftSize / 2)
                    {
                        this.ifftData[i][j] = result[i * 2 - 1][this.fftOverlapOffset + j] + result[i * 2][j];
                    } else {
                        this.ifftData[i][j] = result[i * 2][j] + result[i * 2 + 1][j - this.fftOverlapOffset];
                    }
                }
            }

            this.FillPlaybackBuffer();
        }
    }

    /// <summary>
    /// Converts IFFT data to bytes and fills playback buffer
    /// </summary>
    private void FillPlaybackBuffer()
    {
        this.playbackBuffer = new byte[this.ifftData.Length * this.fftSize * 8];
        int pos = 0;
        for (int i = 0; i < this.ifftData.Length; i++)
        {
            // double to float conversion
            float[] values;
            values = System.Array.ConvertAll<double, float>(this.ifftData[i], y => (float)y);

            // float to bytes conversion
            byte[] frames;
            frames = values.SelectMany(value => System.BitConverter.GetBytes(value)).ToArray();
            System.Array.Copy(frames, 0, this.playbackBuffer, pos, frames.Length);
            pos += frames.Length;
        }
        this.memoryStream = new System.IO.MemoryStream(this.playbackBuffer);
        this.memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
    }


    /// <summary>
    /// Test function for FFT/IFFT routines
    /// </summary>
    private static void FftTest() {
        float[] testInF = { 0.00000002345f, -0.00000045464f, 1.0040346634f, -0.86747333346f };
        double[] testInD = new double[testInF.Length];

        for (int i = 0; i<testInD.Length; i++)
            testInD[i] = testInF[i];

        Fft testFft = new Fft();
        double[] fftResultD = testFft.RunFft(testInD, true);
        double[] ifftResultD = testFft.RunIfft(fftResultD);
        float[] ifftResultF;
        ifftResultF = System.Array.ConvertAll<double, float>(ifftResultD, y => (float)y);

        for (int i = 0; i< testInF.Length; i++)
        {
            Debug.Log("INPUT: " + testInF[i] + " OUPUT: " + ifftResultF[i * 2]);
        }
    } 
    
    /// <summary>
    /// Input handling
    /// </summary>
    private void Update()
    {
        if (Input.GetButtonDown("PlayStop"))
        {
            if (!this.isPlaying)
            {
                this.Play();
            } else {
                this.Stop();
            }
        }

        if (Input.GetButtonDown("Rewind"))
        {
            this.Rewind();
        }
    }

    /// <summary>
    /// Exit handler
    /// </summary>
    void OnApplicationQuit()
    {
        Debug.Log("Exit");
        this.StopAudioEngine();
    }
}
