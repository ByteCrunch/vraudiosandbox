using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio;
using NAudio.Wave;
using SimpleFileBrowser;
using UnityEngine;
using Valve.VR.Extras;

public class AudioEngine : MonoBehaviour
{
    // just for testing in Unity play mode
    public enum testFiles
    {
        sinesweep1HzTo48000Hz,
        sine4000Hz,
        sine100Hz,
        test,
        drumloop,
        silence
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
    //private LoopStream loopStream;

    public bool fftDataEdited = false;
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

    private GameObject fileBrowserVr;
    private SpectrumMeshGenerator spectrum;
    private SteamVR_LaserPointer laser;
    private Fft fft;

    /// <summary>
    /// Makes sure audio engine is loaded before Start() routines of other GameObjects
    /// </summary>
    void Awake()
    {
        this.fileBrowserVr = GameObject.Find("SimpleFileBrowserCanvas");
        this.fileBrowserVr.SetActive(false);

        this.spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();
        this.laser = GameObject.Find("RightHand").GetComponent<SteamVR_LaserPointer>();

        if (!Application.isEditor)
        {
            if (!UnityEngine.XR.XRDevice.isPresent)
            {
                // Show 2D file dialog when in standalone mode
                Debug.Log("This application is meant to be used in VR using a HMD. 2D mode fallback only supports basic display of audio information and simple navigation.");
                
            } else {
                // Show file dialog in Vr world space
                this.fileBrowserVr.SetActive(true);
                FileBrowser.SingleClickMode = true;
                FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio", ".wav", ".aiff", ".mp3", ".m4a", ".ogg"));
                FileBrowser.AddQuickLink("Examples", Application.dataPath + "/Resources/Audio/", null);
                StartCoroutine(WaitForLoadDialog(true));
            }
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

                case testFiles.silence:
                    this.filePath = Application.dataPath + "/Resources/Audio/" + "silence.wav";
                    break;
            }
            this.LoadAudioData();
        }
    }


    public void OpenDialog()
    {
        this.fileBrowserVr.SetActive(true);
        StartCoroutine(WaitForLoadDialog(false));
    }

    /// <summary>
    /// Shows file browser dialog in VR standalone mode
    /// </summary>
    IEnumerator WaitForLoadDialog(bool ignoreCancel)
    {
        while (!FileBrowser.Success)
        {
            if (ignoreCancel && !this.fileBrowserVr.activeSelf)
                this.fileBrowserVr.SetActive(true);

            yield return null;
        }

        this.filePath = FileBrowser.Result;
        this.LoadAudioData();
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
        this.DoIfft();
        this.fftDataEdited = false;
        this.spectrum.GenerateMeshFromAudioData();
        
    }

    /// <summary>
    /// Start playback of IFFT audio data
    /// </summary>
    public void Play()
    {
        this.laser.active = false;

        // Do IFFT if there are changes in the spectrum
        if (this.fftDataEdited)
            this.DoIfft();

        Debug.Log("<AudioEngine> Play");

        this.waveProvider = new NAudio.Wave.RawSourceWaveStream(
                this.memoryStream,
                NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(this.importSampleRate, this.importChannels)
                );

        if (this.waveOut == null)
        {
            this.waveOut = new NAudio.Wave.WaveOutEvent();
            this.waveOut.PlaybackStopped += OnPlaybackStopped;
        }
        //this.loopStream = new LoopStream(this.waveProvider);
        this.waveOut.Init(this.waveProvider);

        this.waveOut.Play();
        this.isPlaying = true;
    }

    /// <summary>
    /// Stop playback of IFFT audio data
    /// </summary>
    public void Stop() 
    {
        this.laser.active = true;

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
        long bytePos = this.waveProvider.Position;
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
       //this.loopStream.EnableLooping = loop;
    }

    /// <summary>
    /// Rewind playback of IFFT audio data
    /// </summary>
    public void Rewind()
    {
        Debug.Log("<AudioEngine> Rewind");
        if (this.waveOut != null)
        {
            this.waveProvider.Position = 0;
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
                this.fftFrequencies[i] = (double)(i+1) / this.fftBinCount * this.importSampleRate / 2;

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
        if (this.fftDataMagnitudes != null && this.fftDataMagnitudes.Length > 0)
        {
            // Do IFFT per chunk
            double[][] result = new double[this.fftDataMagnitudes.Length][];

            for (int i = 0; i < this.fftDataMagnitudes.Length; i++)
            {
                // result of ifft is in interleaved complex format - take only even indexes (the real part)
                result[i] = fft.RunIfft(Fft.GetFftDataFromMagnitudeAndPhase(this.fftDataMagnitudes[i], this.fftDataPhases[i]))
                    .Where((value, index) => index % 2 == 0).ToArray();
            }
            
            //this.PrintFftData(fft.RunIfft(this.fftData[1337]), fft.RunIfft(Fft.GetFftDataFromMagnitudeAndPhase(this.fftDataMagnitudes[1337], this.fftDataPhases[1337])), "fftdata.txt");

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
        Debug.Log("<AudioEngine> playback buffer updated");
    }

    public void WriteAudioToDisk()
    {
        // Do IFFT if there are changes in the spectrum
        if (this.fftDataEdited)
            this.DoIfft();

        this.waveProvider = new NAudio.Wave.RawSourceWaveStream(
                this.memoryStream,
                NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(this.importSampleRate, this.importChannels)
                );

        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory).ToString() + "/" + "VrAudioSandboxExport-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".wav"); //TODO insert timestamp
        WaveFileWriter.CreateWaveFile(path, this.waveProvider);
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

    private void PrintFftData(double[] fftData1, double[] fftData2, string fileName)
    {
        List<string> lines = new List<string>();
        lines.Add("fftData-Length: "+fftData.Length.ToString());
        lines.Add("Re1\t\t\t\t\tImg1\t\t\t\t\tRe2\t\t\t\t\tImg2\t\t\t\t\tRe1-Re2\t\t\t\t\tImg1-Img2");

        for (int i = 0; i < fftData.Length / 2; i++)
        {
            lines.Add(
                fftData1[2 * i].ToString() + "\t\t\t" + fftData1[2 * i + 1].ToString() + "\t\t\t" +
                fftData2[2 * i].ToString() + "\t\t\t" + fftData2[2 * i + 1].ToString() + "\t\t\t" +
                (fftData1[2 * i] - fftData2[2 * i]).ToString() + "\t\t\t" + (fftData1[2 * i + 1] - fftData2[2 * i + 1]).ToString());
        }      
        
        System.IO.File.WriteAllLines(@"C:\Users\bytecrunch\Desktop\" + fileName, lines);

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

    public void ExitProgram()
    {
        Application.Quit();
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
