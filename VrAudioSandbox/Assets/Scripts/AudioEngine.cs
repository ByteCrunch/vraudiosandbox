using System.Collections;
using System.Collections.Generic;
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
        test
    }
    public testFiles selectAudioTestFile;

    public string filePath;

    [HideInInspector]
    public double[][] fftData;
    public double[][] fftDataMagnitudes;
    public double[][] fftDatadBs;
    public double[][] ifftData;

    private NAudio.Wave.WaveFileReader waveReader;
    private NAudio.Wave.WaveOut waveOut;
    private LoopStream loopStream;

    private float[] audioData;
    private byte[] playbackBuffer;

    private System.IO.MemoryStream memoryStream;

    public int importSampleRate;
    public int importBitDepth;
    public double importDurationInMs;
    public int importChannels;
    public int fftSize;
    public int fftNumOfChunks;
    public double[] fftFrequencies;
    public int fftBinCount;

    public bool isPlaying;

    private SpectrumMeshGenerator spectrum;
    private Fft fft;

    // Make sure audio engine is loaded before Start() routines of other GameObjects
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
            }
            this.LoadAudioData();
        }
    }

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

    public void LoadAudioData()
    {
        // Read in wav file and convert into a float[] of samples
        this.waveReader = new NAudio.Wave.WaveFileReader(this.filePath);
        int numOfBytes = (int)this.waveReader.Length;

        this.importSampleRate = this.waveReader.WaveFormat.SampleRate;
        this.importBitDepth = this.waveReader.WaveFormat.BitsPerSample;
        this.importChannels = this.waveReader.WaveFormat.Channels;
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
            }
            this.audioData[pos] = sample[0];
            pos++;
        }

        this.DoFft();
        this.spectrum.Init();

        // testing
        this.DoIfft();
    }

    public void Play()
    {
        Debug.Log("<AudioEngine> Play");
        if (this.waveOut == null)
        {

            int blockAlign = (this.importChannels * (this.importBitDepth / 8));

            NAudio.Wave.RawSourceWaveStream rws = new NAudio.Wave.RawSourceWaveStream(
                this.playbackBuffer,
                0,
                this.playbackBuffer.Length,
                NAudio.Wave.WaveFormat.CreateCustomFormat(
                    NAudio.Wave.WaveFormatEncoding.Pcm,
                    this.importSampleRate,
                    this.importChannels,
                    this.importSampleRate * this.importBitDepth * this.importChannels,
                    blockAlign,
                    this.importBitDepth
                    )
                );
            this.loopStream = new LoopStream(rws);
            this.waveOut = new NAudio.Wave.WaveOut(); //TODO: find fix. Waveout will try to use Windows.Forms for error dialog boxes - this is not supported under Unity Mono (will display "could not register the window class, win 32 error 0")
            this.waveOut.PlaybackStopped += OnPlaybackStopped;
            this.waveOut.Init(this.loopStream);
            this.waveOut.Play();
        } else {
            this.waveOut.Play();
        }
        this.isPlaying = true;
    }

    public void Stop() 
    {
        this.waveOut.Stop();
        this.isPlaying = false;
        this.spectrum.ResetMeshColors();
        
        Debug.Log("<AudioEngine> Stop");
    }

    private void OnPlaybackStopped(object sender, System.EventArgs e)
    {
        Debug.Log("<AudioEngine> " +e.ToString());
    }

    public double GetPositionInMs()
    {
        long bytePos = this.loopStream.Position;
        double ms = bytePos * 1000.0 / this.importBitDepth / 1 * 8 / this.importSampleRate; //1 for mono, TODO multichannel

        return ms;
    }

    public void SetAudioLooping(bool loop)
    {
        this.loopStream.EnableLooping = loop;
    }

    public void Rewind()
    {
        Debug.Log("<AudioEngine> Rewind");
        if (this.waveOut != null)
        {
            this.memoryStream.Position = 0;
            this.spectrum.ResetMeshColors();
        }
    }

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


    public void DoFft()
    {
        if (this.audioData != null && this.audioData.Length > 0)
        {
            this.fftSize = (int)(this.importSampleRate * 0.0232199546485261); // magic number taken from 44.100 Hz / 1024
            this.fftBinCount = fftSize / 2;

            this.fft = new Fft();

            // Calculate size of chunk that will be sent to FFT routine
            this.fftNumOfChunks = (this.audioData.Length + this.fftSize - 1) / this.fftSize; // integer round up

            Debug.Log("<AudioEngine> numOfSamples: " + this.audioData.Length.ToString() + " fftSize: " + this.fftSize.ToString() + " numOfChunks: " + this.fftNumOfChunks.ToString() + " binResolution: " + (this.importSampleRate / 2 / this.fftSize).ToString() + "Hz");

            // Create map of frequencies for the bins
            this.fftFrequencies = new double[this.fftBinCount];
            for (int i = 0; i < this.fftBinCount; i++)
                this.fftFrequencies[i] = (double)i / this.fftBinCount * this.importSampleRate / 2;

            // Do FFT per chunk
            double[][] input = new double[this.fftNumOfChunks][];
            double[][] result = new double[this.fftNumOfChunks][];
            double[][] magnitudes = new double[this.fftNumOfChunks][];
            double[][] dBs = new double[this.fftNumOfChunks][];

            for (int i = 0; i < this.fftNumOfChunks; i++)
            {
                input[i] = new double[this.fftSize];
                result[i] = new double[this.fftSize];
                magnitudes[i] = new double[this.fftSize];
                dBs[i] = new double[this.fftSize];

                for (int j = 0; j < this.fftSize; j++)
                {
                    // last chunk might be smaller than fftSize
                    if (this.audioData.Length > i * this.fftSize + j)
                    {
                        input[i][j] = this.audioData[i * this.fftSize + j];
                    } else {
                        // fill with zeros
                        input[i][j] = 0;
                    }
                }
                fft.RunFft(input[i], result[i], true);

                magnitudes[i] = Fft.Magnitudes(result[i]);
                dBs[i] = Fft.dB(magnitudes[i]);
            }
            this.fftData = result;
            this.fftDataMagnitudes = magnitudes;
            this.fftDatadBs = dBs;
        }
    }

    public void DoIfft()
    {
        if (this.fftData != null && this.fftData.Length > 0)
        {
            // Do IFFT per chunk
            double[][] result = new double[this.fftNumOfChunks][];

            for (int i = 0; i < this.fftNumOfChunks; i++)
            {
                result[i] = new double[this.fftSize];
                fft.RunIfft(this.fftData[i], result[i]);
            }
            this.ifftData = result;
            //this.audioCompare();
            this.FillPlaybackBuffer();
        }
    }

    private void FillPlaybackBuffer()
    {
        this.playbackBuffer = new byte[this.ifftData.Length * this.fftSize * 8];
        int pos = 0;
        for (int i = 0; i < this.ifftData.Length; i++)
        {          
            for (int j = 0; j < this.ifftData[i].Length; j++)
            {
                byte[] buffer = System.BitConverter.GetBytes(this.ifftData[i][j]);
                System.Array.Copy(buffer, 0, this.playbackBuffer, pos, 8);
                pos += 8;
            }
        }
    }

    private void audioCompare()
    {
        int pos = 0;
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < this.ifftData[i].Length; j++)
            {
            Debug.Log(this.audioData[pos].ToString() + " / " + this.ifftData[i][j].ToString());
            Debug.Log("= " + (this.audioData[pos] / this.ifftData[i][j]).ToString());
            pos++;
            }
        }
    }

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

    void OnApplicationQuit()
    {
        Debug.Log("Exit");
        this.fft.Dispose();
        this.StopAudioEngine();
    }
}
