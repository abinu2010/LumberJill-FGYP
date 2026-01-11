using UnityEngine;
using System;

public class Tree : MonoBehaviour
{
    public int growthState { get; private set; } //0 = sapling, 1 = young tree, 2 = mature tree
    public int hitsToChop { get; private set; }
    public int currentHits { get; private set; }
    public int lumberYield { get; private set; }
    private bool choppable = false;
    [SerializeField] public Inventory inventory;
    [SerializeField] private Sprite saplingSprite;
    [SerializeField] private Sprite youngTreeSprite;
    [SerializeField] private Sprite matureTreeSprite;
    [SerializeField] private Sprite choppedTreeSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (currentHits == hitsToChop && choppable)
        {
            ChopDown();
        }
    }

    private void ChopDown()
    {
        inventory.lumber += lumberYield;
        growthState = 0;
        currentHits = 0;
        choppable = false;
        UpdateTreeSprite(choppedTreeSprite);
    }
    
    private void UpdateTreeSprite(Sprite newSprite)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }
}
