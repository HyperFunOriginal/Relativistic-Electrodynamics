using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulateEM : MonoBehaviour
{
    public enum FieldType
    {
        Electric = 0, Magnetic = 1
    }

    [Header("Simulation Parameters")]
    public Vector3Int resolution;
    public float lengthScale;
    public float timestep;

    [Header("Visualization")]
    public float slice;
    [Range(0f, 1f)]
    public float zoom;
    [Range(-1f, 1f)]
    public float vectorScale;
    public FieldType renderType;
    [Header("Recording")]
    [Range(1, 50)]
    public int recordInterval;
    [Range(0, 2000)]
    public int recordCutoff;

    [Header("Internal Data")]
    public ComputeShader shader;
    public int DiffrCalcE => shader.FindKernel("DiffrCalcE");
    public int DiffrCalcB => shader.FindKernel("DiffrCalcB");
    public int Sommerfeld => shader.FindKernel("Sommerfeld");
    public int UpdateCrFl => shader.FindKernel("UpdateCrFl");
    public int UpdateEPhi => shader.FindKernel("UpdateEPhi");
    public int UpdateBPsi => shader.FindKernel("UpdateBPsi");
    public int Initialize => shader.FindKernel("Initialize");
    public int PrintImage => shader.FindKernel("PrintImage");
    public int AddVtField => shader.FindKernel("AddVtField");
    public int KrOlDerivs => shader.FindKernel("KrOlDerivs");

    public int elements => resolution.x * resolution.y * resolution.z;
    Vector3Int dispatchSize => new Vector3Int(Mathf.CeilToInt(resolution.x / 10f), Mathf.CeilToInt(resolution.y / 10f), Mathf.CeilToInt(resolution.z / 10f));

    ComputeBuffer E, B, phi, psi, Ja;
    ComputeBuffer dE, dB;

    public RenderTexture screen;
    [HideInInspector()]
    public float simTime;
    [HideInInspector()]
    public int simulationFrameIndex, frameIndex;

    // Start is called before the first frame update
    void Start()
    {
        dB = new ComputeBuffer(elements, sizeof(float) * 9);
        dE = new ComputeBuffer(elements, sizeof(float) * 9);

        Ja = new ComputeBuffer(elements, sizeof(float) * 4);
        E = new ComputeBuffer(elements, sizeof(float) * 3);
        B = new ComputeBuffer(elements, sizeof(float) * 3);
        phi = new ComputeBuffer(elements, sizeof(float));
        psi = new ComputeBuffer(elements, sizeof(float));

        screen = new RenderTexture(resolution.x * 4, resolution.y * 4, 0) { enableRandomWrite = true };
        screen.Create();

        Init();
    }

    void Init()
    {
        simulationFrameIndex = 0;
        frameIndex = 0;
        simTime = 0f;

        SetConst();
        shader.SetBuffer(Initialize, "E", E);
        shader.SetBuffer(Initialize, "B", B);
        shader.SetBuffer(Initialize, "Ja", Ja);
        shader.SetBuffer(Initialize, "psi", psi);
        shader.SetBuffer(Initialize, "phi", phi);
        shader.Dispatch(Initialize, dispatchSize.x, dispatchSize.y, dispatchSize.z);
    }
    void SetConst()
    {
        shader.SetFloat("time", simTime);
        shader.SetFloat("zoom", Mathf.Pow(10f, zoom));
        shader.SetFloat("vectorScale", Mathf.Pow(10f, vectorScale));
        shader.SetFloat("slice", slice);
        shader.SetFloat("dampCoeff", .02f);
        shader.SetFloat("lengthScale", lengthScale);
        shader.SetFloat("timestep", timestep);
        shader.SetInts("resolution", resolution.x, resolution.y, resolution.z);
    }
    public void SaveScreen()
    {
        SaveImage.SaveImageToFile(screen, Application.dataPath + "\\Frames\\", "Image_" + frameIndex.ToString());
        frameIndex++;
    }
    void PrintScreen()
    {
        switch (renderType)
        {
            case FieldType.Electric:
                shader.SetBuffer(PrintImage, "field", E);
                shader.SetBuffer(PrintImage, "derivs", dE);
                break;
            case FieldType.Magnetic:
                shader.SetBuffer(PrintImage, "field", B);
                shader.SetBuffer(PrintImage, "derivs", dB);
                break;
        }
        shader.SetTexture(PrintImage, "Result", screen);
        shader.Dispatch(PrintImage, Mathf.CeilToInt(resolution.x / 8f), Mathf.CeilToInt(resolution.y / 8f), 1);
    }
    void AddVectors()
    {
        shader.SetBuffer(AddVtField, "B", B);
        shader.SetBuffer(AddVtField, "E", E);
        shader.SetTexture(AddVtField, "Result", screen);
        shader.Dispatch(AddVtField, Mathf.CeilToInt(resolution.x / 128f), Mathf.CeilToInt(resolution.y / 128f), 1);
    }

    private void OnDestroy()
    {
        dE.Dispose();
        dB.Dispose();

        Ja.Dispose();
        E.Dispose();
        B.Dispose();
        phi.Dispose();
        psi.Dispose();

        screen.Release();
        DestroyImmediate(screen, true);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(screen, destination);
    }

    void EvolveEM()
    {
        SetConst();

        shader.SetBuffer(DiffrCalcB, "B", B);
        shader.SetBuffer(DiffrCalcB, "dB", dB);
        shader.Dispatch(DiffrCalcB, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        shader.SetBuffer(UpdateEPhi, "Ja", Ja);
        shader.SetBuffer(UpdateEPhi, "E", E);
        shader.SetBuffer(UpdateEPhi, "phi", phi);
        shader.SetBuffer(UpdateEPhi, "dB", dB);
        shader.SetBuffer(UpdateEPhi, "psi", psi);
        shader.Dispatch(UpdateEPhi, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        shader.SetBuffer(DiffrCalcE, "E", E);
        shader.SetBuffer(DiffrCalcE, "dE", dE);
        shader.Dispatch(DiffrCalcE, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        shader.SetBuffer(UpdateBPsi, "Ja", Ja);
        shader.SetBuffer(UpdateBPsi, "dE", dE);
        shader.SetBuffer(UpdateBPsi, "phi", phi);
        shader.SetBuffer(UpdateBPsi, "B", B);
        shader.SetBuffer(UpdateBPsi, "psi", psi);
        shader.Dispatch(UpdateBPsi, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        shader.SetBuffer(Sommerfeld, "dE", dE);
        shader.SetBuffer(Sommerfeld, "dB", dB);
        shader.SetBuffer(Sommerfeld, "B", B);
        shader.SetBuffer(Sommerfeld, "E", E);
        shader.Dispatch(Sommerfeld, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        simulationFrameIndex++;
        simTime += timestep;
    }
    // Update is called once per frame
    void Update()
    {
        shader.SetBuffer(UpdateCrFl, "Ja", Ja);
        shader.Dispatch(UpdateCrFl, dispatchSize.x, dispatchSize.y, dispatchSize.z);

        EvolveEM();
        PrintScreen();
        AddVectors();

        if (frameIndex >= recordCutoff)
            return;

        if (simulationFrameIndex % recordInterval == 0)
            SaveScreen();
    }
}
