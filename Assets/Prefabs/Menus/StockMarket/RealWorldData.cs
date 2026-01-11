using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CommodityPriceResponse
{
    public string exchange;
    public string name;
    public float price;
    public long updated;
}

public class RealWorldData : MonoBehaviour
{
    public float costLumber { get; private set; }
    public float lastLumberPrice { get; private set; }
    public float avgLumberPrice { get; private set; }

    [Header("Market Parameters")]
    [SerializeField] private float baseLumberPrice = 100f;
    [SerializeField] private float referenceLumberPrice = 600f;  // Average real lumber price baseline
    [SerializeField] private float sensitivity = 6f;             // How strongly the game reacts to real-world price changes
    [SerializeField] private float volatility = 0.05f;           // Random short-term price movement range 
    [SerializeField] private float cycleSpeed = 0.1f;            // Speed of sinusoidal market cycle
    [SerializeField] private float cycleAmplitude = 0.3f;        
    public string marketMood { get; private set; }

    private const string API_KEY = "RkDmb6E5A+2W4WQZz0pBZQ==PO8NzoDX91iGM0ji";
    private const string API_URL = "https://api.api-ninjas.com/v1/commodityprice?name=lumber";
    private const string SAVE_KEY = "LastLumberPrice";
    private const int AVG_WINDOW = 5;

    private readonly Queue<float> priceHistory = new Queue<float>();
    private float timeSinceStart;
    private float marketTrend;

    private void Start()
    {
        LoadLastSavedPrice();
        StartCoroutine(FetchLumberPrice());
    }

    private void Update()
    {
        timeSinceStart += Time.deltaTime;
        marketTrend = Mathf.Sin(timeSinceStart * cycleSpeed) * cycleAmplitude;
    }

    private IEnumerator FetchLumberPrice()
    {
        Debug.Log("Fetching latest lumber price...");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(API_URL))
        {
            webRequest.SetRequestHeader("X-Api-Key", API_KEY);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("Failed to fetch lumber price. Using saved value.");
                yield break;
            }

            string json = webRequest.downloadHandler.text;
            CommodityPriceResponse response = JsonUtility.FromJson<CommodityPriceResponse>(json);

            if (response == null)
            {
                Debug.LogWarning("Invalid API response. Keeping saved value.");
                yield break;
            }

            lastLumberPrice = response.price;
            SaveLastPrice();

            UpdateAverage(lastLumberPrice);
            CalculateInGamePrice();

            Debug.Log($"Lumber price updated. Real: {lastLumberPrice}, In-Game: {costLumber}, Mood: {marketMood}");
        }
    }

    private void UpdateAverage(float newValue)
    {
        priceHistory.Enqueue(newValue);
        if (priceHistory.Count > AVG_WINDOW)
            priceHistory.Dequeue();

        float sum = 0f;
        foreach (float val in priceHistory)
            sum += val;

        avgLumberPrice = sum / priceHistory.Count;
    }

    private void CalculateInGamePrice()
    {
        float diff;
        if (avgLumberPrice > 0)
            diff = (lastLumberPrice - avgLumberPrice) / avgLumberPrice;
        else
            diff = (lastLumberPrice - referenceLumberPrice) / referenceLumberPrice;

        float randomFluctuation = Random.Range(-volatility, volatility);
        float totalEffect = diff * sensitivity + marketTrend + randomFluctuation;

        costLumber = baseLumberPrice * (1 + totalEffect);
        costLumber = Mathf.Clamp(costLumber, baseLumberPrice * 0.6f, baseLumberPrice * 1.8f);
        costLumber = Mathf.Round(costLumber * 100f) / 100f;

        SetMarketMood();
    }

    private void SetMarketMood()
    {
        if (marketTrend > 0.2f)
            marketMood = "Bullish - Lumber prices are climbing.";//people should know not to buy 
        else if (marketTrend < -0.2f)
            marketMood = "Bearish - Lumber prices are dropping.";//should  buy
        else
            marketMood = "Stable - Market remains balanced.";//can buy
    }

    private void SaveLastPrice()
    {
        PlayerPrefs.SetFloat(SAVE_KEY, lastLumberPrice);
        PlayerPrefs.Save();
    }

    private void LoadLastSavedPrice()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            lastLumberPrice = PlayerPrefs.GetFloat(SAVE_KEY);
            UpdateAverage(lastLumberPrice);
            CalculateInGamePrice();
            Debug.Log($"Loaded saved lumber price: {lastLumberPrice}");
        }
        else
        {
            costLumber = baseLumberPrice;
            Debug.Log("No saved lumber price found. Using base value.");
        }
    }
}
