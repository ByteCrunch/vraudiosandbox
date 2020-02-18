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

    private NAudio.Wave.WaveFileReader waveReader;
    private NAudio.Wave.WaveOut waveOut;
    private LoopStream loopStream;

    private double[] importDataAsSamples;
    public int importSampleRate;
    public int importBitDepth;
    public double[] fftFrequencies;
    public int fftBinCount;

    public bool isPlaying;

    // Make sure audio engine is loaded before Start() routines of other GameObjects
    void Awake()
    {
        if (!Application.isEditor)
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio", ".wav", ".aiff", ".mp3", ".m4a", ".ogg"));
            FileBrowser.AddQuickLink("Examples", Application.dataPath + "/Resources/Audio/", null);
            StartCoroutine(ShowLoadDialogCoroutine());
        } else
        {
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
        // only for testing in Unity editor
        if (Application.isEditor)
        {
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
        }

        // Read in wav file and convert into an array of samples
        this.waveReader = new NAudio.Wave.WaveFileReader(this.filePath);
        int numOfBytes = (int)this.waveReader.Length;
        this.importSampleRate = this.waveReader.WaveFormat.SampleRate;
        this.importBitDepth = this.waveReader.WaveFormat.BitsPerSample;

        byte[] importDataAsBytes = new byte[numOfBytes];
        this.waveReader.Read(importDataAsBytes, 0, numOfBytes);


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

        this.DoFft();

        SpectrumMeshGenerator spectrum = GameObject.Find("SpectrumMesh").GetComponent<SpectrumMeshGenerator>();
        spectrum.Init();
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
        Debug.Log("<AudioEngine> Stop");
    }

    private void OnPlaybackStopped(object sender, System.EventArgs e)
    {
        Debug.Log("<AudioEngine> " +e.ToString());
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

            Debug.Log("<AudioEngine> numOfSamples: " + this.importDataAsSamples.Length.ToString() + " chunkSize: " + chunkSize.ToString() + " numOfChunks: " + numOfChunks.ToString() + " binResolution: " + (this.importSampleRate / fftSize).ToString() + "Hz");

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
        this.StopAudioEngine();
    }
}
