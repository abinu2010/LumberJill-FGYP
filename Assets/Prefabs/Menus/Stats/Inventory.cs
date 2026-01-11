using UnityEngine;
using TMPro;

public class Inventory : MonoBehaviour
{
    public float money { get; set; } = 5000f;
    public int xp { get; set; }
    public int lumber { get; set; }
    public int gold { get; set; }
    public int copper { get; set; }

    [SerializeField] TMP_Text moneyUI;
    [SerializeField] TMP_Text lumberUI;
    [SerializeField] TMP_Text xpUI;

    void Awake()
    {
        money = PlayerPrefs.GetFloat("Money", 5000f);
        xp = PlayerPrefs.GetInt("Xp", 50);
        lumber = PlayerPrefs.GetInt("Lumber", 20);
        gold = PlayerPrefs.GetInt("Gold", 1000);
        copper = PlayerPrefs.GetInt("Copper", 0);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (moneyUI) moneyUI.text = Mathf.RoundToInt(money).ToString();
        if (lumberUI) lumberUI.text = lumber.ToString();
        if (xpUI) xpUI.text = xp.ToString();
    }

    public void AddMoney(float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;
        money += amount;
        PlayerPrefs.SetFloat("Money", money);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (money < amount) return false;
        money -= amount;
        PlayerPrefs.SetFloat("Money", money);
        PlayerPrefs.Save();
        RefreshUI();
        return true;
    }

    public void AddXp(int amount)
    {
        if (amount == 0) return;
        xp = Mathf.Max(0, xp + amount); // no going below 0
        PlayerPrefs.SetInt("Xp", xp);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void ChangeLumber(int delta)
    {
        lumber = Mathf.Max(0, lumber + delta);
        PlayerPrefs.SetInt("Lumber", lumber);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void ChangeGold(int delta)
    {
        gold = Mathf.Max(0, gold + delta);
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.Save();
    }

    public void ChangeCopper(int delta)
    {
        copper = Mathf.Max(0, copper + delta);
        PlayerPrefs.SetInt("Copper", copper);
        PlayerPrefs.Save();
    }
}
