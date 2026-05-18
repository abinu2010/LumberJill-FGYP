using UnityEngine;
using TMPro;

public class Inventory : MonoBehaviour
{
    public float money { get; set; } = 2000f;
    public int xp { get; set; }
    public int lumber { get; set; }
    public int gold { get; set; }
    public int copper { get; set; }

    [SerializeField] private TMP_Text moneyUI;
    [SerializeField] private TMP_Text lumberUI;
    [SerializeField] private TMP_Text xpUI;

    private void Awake()
    {
        LoadFromPrefs();
        RefreshUI();
    }

    public void LoadFromPrefs()
    {
        money = PlayerPrefs.GetFloat("Money", 2000f);
        xp = PlayerPrefs.GetInt("Xp", 0);   
        lumber = PlayerPrefs.GetInt("Lumber", 20);
        gold = PlayerPrefs.GetInt("Gold", 1000);
        copper = PlayerPrefs.GetInt("Copper", 0);
    }

    public void SaveToPrefs()
    {
        PlayerPrefs.SetFloat("Money", money);
        PlayerPrefs.SetInt("Xp", xp);
        PlayerPrefs.SetInt("Lumber", lumber);
        PlayerPrefs.SetInt("Gold", gold);
        PlayerPrefs.SetInt("Copper", copper);
        PlayerPrefs.Save();
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
        money = Mathf.Max(0f, money + amount);
        SaveToPrefs();
        RefreshUI();
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (money < amount) return false;
        money -= amount;
        SaveToPrefs();
        RefreshUI();
        return true;
    }

    public void AddXp(int amount)
    {
        if (amount == 0) return;
        xp = Mathf.Max(0, xp + amount);
        SaveToPrefs();
        RefreshUI();
    }

    public void SetXp(int value)
    {
        xp = Mathf.Max(0, value);
        SaveToPrefs();
        RefreshUI();
    }

    public void ChangeLumber(int delta)
    {
        lumber = Mathf.Max(0, lumber + delta);
        SaveToPrefs();
        RefreshUI();
    }

    public void SetLumber(int value)
    {
        lumber = Mathf.Max(0, value);
        SaveToPrefs();
        RefreshUI();
    }

    public void ChangeGold(int delta)
    {
        gold = Mathf.Max(0, gold + delta);
        SaveToPrefs();
    }

    public void ChangeCopper(int delta)
    {
        copper = Mathf.Max(0, copper + delta);
        SaveToPrefs();
    }
}
