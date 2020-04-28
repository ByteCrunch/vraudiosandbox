using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrumFreqLegend : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetFreqLegend(double[] freq, float edgeLength)
    {
        int bins = freq.Length;
        Vector3 spectrumPos = GameObject.Find("SpectrumMesh").transform.position;

        GameObject[] textObj = new GameObject[bins];
        MeshRenderer[] textRenderer = new MeshRenderer[bins];
        TextMesh[] textMesh = new TextMesh[bins];

        for (int f = 0; f < bins; f++)
        {
            textObj[f] = new GameObject("Freq" + f.ToString());
            textObj[f].transform.parent = this.transform;
            textObj[f].transform.position = new Vector3(spectrumPos.x + f * edgeLength, spectrumPos.y, spectrumPos.z);
            textObj[f].transform.localEulerAngles = new Vector3(90f, 90f, 0f);
            textObj[f].transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            textRenderer[f] = textObj[f].AddComponent<MeshRenderer>();
            textMesh[f] = textObj[f].AddComponent<TextMesh>();

            textMesh[f].text = System.Math.Round(freq[f], 2).ToString() + " Hz\n";
            textMesh[f].alignment = TextAlignment.Right;
            textMesh[f].fontSize = 70;
            textMesh[f].color = Color.white;
        }
    }
}
