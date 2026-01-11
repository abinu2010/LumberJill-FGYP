using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class StockMarketChart : MonoBehaviour
{
    /*
    Get array size of chosen data set then - timePeriod and store as startTime
    use this to generate the last 12 hours of candlesticks
    Take the abs(open-close) to get the height of the candlestick
    take open+(open-close)/2 to position to generate the candlestick
    Repeat 11 times and add width each time
    (some extra calculations are done to get these sizes relative to the trade window size)
    */

    [SerializeField] List<Image> candleSticks;
    [SerializeField] private int timePeriod = 12;
    [SerializeField] private GameObject tradeWindow;
    private float windowHeight;
    private float min, max;
    private float windowUnit;
    private int startTime;
    private float candleStickWidth;
    private float height;
    private float position;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startTime = SimulatedRealWorldDataSet.tradeData.GetLength(0) - timePeriod;
        windowHeight = tradeWindow.GetComponent<RectTransform>().rect.height*0.8f;
        candleStickWidth = tradeWindow.GetComponent<RectTransform>().rect.width*0.9f/timePeriod;
        FindRange();
        GenerateCandleSticks();
    }

    void FindRange()
    {
        min = float.MaxValue;
        max = float.MinValue;

        //find min max
        for (int i=0;i<timePeriod;i++)
        {
            for(int j=0;j<2;j++)
            {
                float val = SimulatedRealWorldDataSet.tradeData[startTime+i, j];

                if (val < min) min = val;
                if (val > max) max = val;
            }
        }
        float range = max-min;
        windowUnit = windowHeight/range;
    }

    void GenerateCandleSticks()
    {   
        for(int i=0;i<timePeriod;i++)
        {
            GameObject candleStick = new GameObject("CandleStick");    
            candleStick.transform.SetParent(this.transform);
            candleStick.transform.localScale = Vector3.one;
            candleStick.transform.localPosition = Vector3.zero;
            Image image = candleStick.AddComponent<Image>();
            image.rectTransform.anchorMin = new Vector2(0f, 0f);
            image.rectTransform.anchorMax = new Vector2(0f, 0f);
            candleSticks.Add(image);

            height = (SimulatedRealWorldDataSet.tradeData[startTime+i, 1] - SimulatedRealWorldDataSet.tradeData[startTime+i, 0])*windowUnit;
            position = (SimulatedRealWorldDataSet.tradeData[startTime+i, 0]-min)*windowUnit + height/2;
            candleSticks[i].rectTransform.sizeDelta = new Vector2(candleStickWidth, Mathf.Abs(height));
            candleSticks[i].rectTransform.anchoredPosition = new Vector2(i * candleStickWidth + candleStickWidth/2, position+tradeWindow.GetComponent<RectTransform>().rect.height*0.1f);
            
            //red or green candlestick based on increase or decrease in value
            if (height>0)
            {
                candleSticks[i].color = Color.green;
            }
            else
            {
                candleSticks[i].color = Color.red;
            }
        }
    }
}