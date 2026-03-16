using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CarSelector : MonoBehaviour
{
    public static CarSelector Instance { get; private set; }

    [Header("Settings")]
    public bool useImportedModels = true;
    public int currentCarIndex = 0;
    public float carScale = 1.32f; // Kenney cars are small, scale them up (10% increase)

    private string[] carNames = { "SportsCar", "SportsCar2", "SUV", "Cop", "Taxi", "NormalCar1", "NormalCar2" };
    private GameObject currentCarModel;
    private CarController carController;
    private Transform carBody;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        carController = FindFirstObjectByType<CarController>();
        if (carController != null)
        {
            carBody = carController.transform.Find("Body");
        }

        if (useImportedModels && carBody != null)
        {
            // Hide procedural car body
            SetProceduralBodyVisible(false);
            LoadCarModel(currentCarIndex);
        }
    }

    void Update()
    {
        // Press C to cycle through cars
        if (Input.GetKeyDown(KeyCode.C))
        {
            NextCar();
        }
        // Press V to toggle between procedural and imported
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleCarType();
        }
    }

    public void NextCar()
    {
        if (!useImportedModels) return;

        currentCarIndex = (currentCarIndex + 1) % carNames.Length;
        LoadCarModel(currentCarIndex);
        Debug.Log($"Switched to: {carNames[currentCarIndex]}");
    }

    public void PreviousCar()
    {
        if (!useImportedModels) return;

        currentCarIndex--;
        if (currentCarIndex < 0) currentCarIndex = carNames.Length - 1;
        LoadCarModel(currentCarIndex);
    }

    void ToggleCarType()
    {
        useImportedModels = !useImportedModels;

        if (useImportedModels)
        {
            SetProceduralBodyVisible(false);
            LoadCarModel(currentCarIndex);
        }
        else
        {
            if (currentCarModel != null)
            {
                Destroy(currentCarModel);
            }
            SetProceduralBodyVisible(true);
        }

        Debug.Log($"Car type: {(useImportedModels ? "Imported" : "Procedural")}");
    }

    void SetProceduralBodyVisible(bool visible)
    {
        if (carBody == null) return;

        foreach (Renderer r in carBody.GetComponentsInChildren<Renderer>())
        {
            r.enabled = visible;
        }
    }

    void LoadCarModel(int index)
    {
        if (carController == null) return;

        // Destroy previous model
        if (currentCarModel != null)
        {
            Destroy(currentCarModel);
        }

        // Load new model from Resources
        string carName = carNames[index];
        GameObject carPrefab = Resources.Load<GameObject>($"Cars/{carName}");

        if (carPrefab == null)
        {
            Debug.LogWarning($"Car model not found: Cars/{carName}. Make sure FBX is in Assets/Resources/Cars/");
            return;
        }

        // Instantiate and parent to car controller
        currentCarModel = Instantiate(carPrefab, carController.transform);
        currentCarModel.name = $"CarModel_{carName}";
        currentCarModel.transform.localPosition = new Vector3(0, 0.1f, 0); // Slight offset
        currentCarModel.transform.localRotation = Quaternion.identity;
        currentCarModel.transform.localScale = Vector3.one * carScale;

        // Remove any colliders from the model (car controller has its own)
        foreach (Collider col in currentCarModel.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }

        // Apply a nice material to the car
        ApplyCarMaterial(currentCarModel);
    }

    void ApplyCarMaterial(GameObject car)
    {
        // Create a nice car paint material
        Material carPaint = new Material(Shader.Find("Standard"));

        // Different colors for different cars
        Color[] carColors = {
            new Color(0.9f, 0.1f, 0.1f),    // SportsCar - Red
            new Color(0.1f, 0.1f, 0.9f),    // SportsCar2 - Blue
            new Color(0.2f, 0.2f, 0.2f),    // SUV - Dark gray
            new Color(0.1f, 0.1f, 0.1f),    // Cop - Black
            new Color(0.95f, 0.8f, 0.2f),   // Taxi - Yellow
            new Color(0.8f, 0.8f, 0.85f),   // NormalCar1 - Silver
            new Color(0.0f, 0.3f, 0.15f),   // NormalCar2 - Green
        };

        Color paintColor = carColors[currentCarIndex % carColors.Length];
        carPaint.color = paintColor;
        carPaint.SetFloat("_Metallic", 0.7f);
        carPaint.SetFloat("_Glossiness", 0.8f);

        // Apply to all renderers
        foreach (Renderer r in car.GetComponentsInChildren<Renderer>())
        {
            // Keep windows/glass transparent-ish
            if (r.gameObject.name.ToLower().Contains("window") ||
                r.gameObject.name.ToLower().Contains("glass"))
            {
                Material glassMat = new Material(Shader.Find("Standard"));
                glassMat.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);
                glassMat.SetFloat("_Mode", 3);
                glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                glassMat.EnableKeyword("_ALPHABLEND_ON");
                glassMat.renderQueue = 3000;
                r.material = glassMat;
            }
            else
            {
                r.material = carPaint;
            }
        }
    }

    public string GetCurrentCarName()
    {
        return carNames[currentCarIndex];
    }
}
