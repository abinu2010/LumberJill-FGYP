using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    //attach to Machine Panels in shop
    
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject machine;
    [SerializeField] private int costToUpgrade = 100;
    private int machineLevel = 0;

    private GameObject gameManager;
    private Inventory inventory;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = GameObject.FindWithTag("GameController");
        inventory = gameManager.GetComponent<Inventory>();
        upgradeButtonText.text = "$" + costToUpgrade.ToString();
        levelText.text = "BUY";
    }

    public void UpgradeMachine()
    {
        if(machineLevel < 1)
        {
            PlaceMachine();

        }
        else
        {
            machineLevel++;
        }
        levelText.text = "LEVEL: " + machineLevel.ToString();
        costToUpgrade = (int)(costToUpgrade * 1.5);
        upgradeButtonText.text = "$" + costToUpgrade.ToString();
    }

    public void PlaceMachine()
    {
        Instantiate(machine);
        machineLevel++;
    }
}
