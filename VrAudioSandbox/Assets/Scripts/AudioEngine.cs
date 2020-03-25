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
    public float[][] fftData;

    private NAudio.Wave.WaveFileReader waveReader;
    private NAudio.Wave.WaveOut waveOut;
    private LoopStream loopStream;

    private float[] audioData;
    private byte[] playbackBuffer;

    public int importSampleRate;
    public int importBitDepth;
    public double importDurationInMs;
    public int importChannels;
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
                Debug.Log("<AudioEngine> multi-channel audio files are not supported right now - so only first channel is used, number of channels: " + this.importChannels.ToString());
            }
            this.audioData[pos] = sample[0];
            pos++;
        }

        this.DoFft();
        this.spectrum.Init();
    }

    public void Play()
    {
        Debug.Log("<AudioEngine> Play");
        if (this.waveOut == null)
        {
            this.loopStream = new LoopStream(this.waveReader);
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
            this.waveReader.Position = 0;
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
            int fftSize = (int)(this.importSampleRate * 0.0232199546485261); // magic number taken from 44.100 Hz / 1024
            this.fftBinCount = fftSize / 2;

            // Calculate size of chunk that will be sent to FFT routine
            int numOfChunks = (this.audioData.Length + fftSize - 1) / fftSize; // integer round up

            Debug.Log("<AudioEngine> numOfSamples: " + this.audioData.Length.ToString() + " fftSize: " + fftSize.ToString() + " numOfChunks: " + numOfChunks.ToString() + " binResolution: " + (this.importSampleRate / 2 / fftSize).ToString() + "Hz");

            // Create map of frequencies for the bins
            this.fftFrequencies = new double[this.fftBinCount];
            for (int i = 0; i < this.fftBinCount; i++)
                this.fftFrequencies[i] = (double)i / this.fftBinCount * this.importSampleRate / 2;

            // Do FFT per chunk
            float[][] input = new float[numOfChunks][];
            float[][] result = new float[numOfChunks][];
            this.fft = new Fft(fftSize);
            for (int i = 0; i < numOfChunks; i++)
            {
                input[i] = new float[fftSize];
                result[i] = new float[fftSize];

                for (int j = 0; j < fftSize; j++)
                {
                    // last chunk might be smaller than fftSize
                    if (this.audioData.Length > i * fftSize + j)
                    {
                        input[i][j] = this.audioData[i * fftSize + j];
                    } else {
                        // fill with zeros
                        input[i][j] = 0f;
                    }
                }
                fft.RunFft(input[i], result[i]);
            }
            this.fftData = result;
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
