using Unity.VisualScripting;
using UnityEngine;

public class SimulatedRealWorldDataSet : MonoBehaviour
{
    /* 
    Generate a value between 500-700
    randomly add or subtract between 0-50 dollars
    Store these as the open and close values of a 1hr candlestick in a 2D array
    Add a date and time
    take the close value of this candlestick as the open for the next one
    Loop through this process to generate 30 days worth of data
    */

    public static float[,] tradeData = new float[720, 3]; // 30 days worth of 1hr data, 3 values (open, close, date) 


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        tradeData[0, 0] = Random.Range(500, 700);
        tradeData[0, 1] = tradeData[0, 0] + Random.Range(-10, 10);

        for (int i = 1; i < tradeData.GetLength(0); i++)
        {
            tradeData[i, 0] = tradeData[i - 1, 1];
            tradeData[i, 1] = Mathf.Clamp(tradeData[i, 0] + Random.Range(-10, 10), 0, 5000);
        }
    }

}
