using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject textPanel;
    private TMP_Text tutorialText;
    private GameObject taskPanel;

    public bool tutorialTextActive = false;
    private string currentRoom = "Workshop";

    [SerializeField] private bool tutorialCompleted = false;
    [SerializeField] private int currentStage = 1;
    [SerializeField] private int dialogueIndex = 1;

    [Header("Door tutorial arrows")]
    [SerializeField] private GameObject doorLoadingBay;
    [SerializeField] private GameObject doorLumberYard;
    [SerializeField] private GameObject doorStorageRoom;

    [Header("Workshop re-entry Doors")]
    [SerializeField] private GameObject doorFromLoadingBay;
    [SerializeField] private GameObject doorFromLumberYard;
    [SerializeField] private GameObject doorFromStorageRoom;

    [Header("Customer tutorial arrows")]
    [SerializeField] private GameObject customerUI;
    [SerializeField] private ItemSO chairItemSO;

    [Header("Job Board tutorial arrows")]
    [SerializeField] private GameObject jobBoard;
    [SerializeField] private GameObject jobBoardUI;

    [Header("Computer/Shop tutorial arrows")]
    [SerializeField] private GameObject computer;
    [SerializeField] private GameObject computerUI;
    [Header("Shop tutorial purchase ")]
    [SerializeField] private string[] requiredShopItemIds = new string[] { "Chair", "TableSaw", "LaserCutter", "Assembly" };
    private bool waitingForShopPurchases = false;
    private bool shopInstructionReadyToHide = false;

    private bool stage1IntroDialogueComplete = false;
    private bool visitedLoadingBay = false;
    private bool visitedLumberYard = false;
    private bool visitedStorageRoom = false;
    private bool stage1Complete = false;

    private bool stage2IntroDialogueComplete = false;
    private bool visitedCustomer = false;
    private bool jobBoardIntroComplete = false;
    private bool visitedJobBoard = false;

    private bool computerIntroComplete = false;

    private bool shopIntroComplete = false;
    private bool visitedShop = false;

    private bool stockIntroComplete = false;
    private bool visitedStockMarket = false;

    private bool visitedStorageForWood = false;
    private bool visitedTableSaw = false;
    private bool visitedLaserCutter = false;
    private bool visitedAssemblyStation = false;
    private bool visitedLoadingBayForDelivery = false;
    [SerializeField] private GameObject storageCrate;
    [SerializeField] private ItemSO woodItemSO;
    [SerializeField] private int woodNeededForChair = 1;

    private StorageBuilding storageBuilding;
    private bool waitingForStorageOpen = false;
    private bool waitingForWoodInHotbar = false;

    private bool waitingForTutorialJobComplete = false;
    private bool showingTutorialEndMessage = false;

    private JobManager jm;

    private bool waitingForShopOpen = false;
    private bool shopUIOpened = false;

    private bool waitingForStockOpen = false;
    private bool stockUIOpened = false;

    private WorkshopComputer wc;
    private CanvasGroup cg;

    void Start()
    {
        if (tutorialCompleted)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        tutorialText = textPanel != null ? textPanel.GetComponentInChildren<TMP_Text>(true) : null;

        var rt = GetComponentInChildren<RectTransform>(true);
        var t = rt != null ? rt.Find("Panel_Tasks") : null;
        taskPanel = t != null ? t.gameObject : null;
        if (taskPanel != null) taskPanel.SetActive(false);

        jm = FindFirstObjectByType<JobManager>();
        if (jm != null)
            jm.BeginTutorialMode();

        if (jobBoard == null)
        {
            JobBoard foundBoard = FindFirstObjectByType<JobBoard>();
            if (foundBoard != null) jobBoard = foundBoard.gameObject;
        }

        if (jobBoardUI == null)
        {
            JobBoardUI foundBoardUI = FindFirstObjectByType<JobBoardUI>();
            if (foundBoardUI != null) jobBoardUI = foundBoardUI.gameObject;
        }

        if (doorLoadingBay != null) doorLoadingBay.GetComponentInChildren<Button>(true).onClick.AddListener(LoadingBayEnter);
        if (doorLumberYard != null) doorLumberYard.GetComponentInChildren<Button>(true).onClick.AddListener(LumberYardEnter);
        if (doorStorageRoom != null) doorStorageRoom.GetComponentInChildren<Button>(true).onClick.AddListener(StorageRoomEnter);

        if (doorFromLoadingBay != null) doorFromLoadingBay.GetComponentInChildren<Button>(true).onClick.AddListener(() => { currentRoom = "Workshop"; if (!stage1Complete) Stage1CompleteCheck(); });
        if (doorFromLumberYard != null) doorFromLumberYard.GetComponentInChildren<Button>(true).onClick.AddListener(() => { currentRoom = "Workshop"; if (!stage1Complete) Stage1CompleteCheck(); });
        if (doorFromStorageRoom != null) doorFromStorageRoom.GetComponentInChildren<Button>(true).onClick.AddListener(() => { currentRoom = "Workshop"; if (!stage1Complete) Stage1CompleteCheck(); });

        if (customerUI != null)
        {
            CustomerCardUI card = customerUI.GetComponent<CustomerCardUI>();
            if (card == null) card = customerUI.GetComponentInChildren<CustomerCardUI>(true);

            if (card != null)
            {
                card.OnShown.RemoveListener(CustomerTutorialTrigger);
                card.OnAccepted.RemoveListener(JobBoardIntroTrigger);
                card.OnShown.AddListener(CustomerTutorialTrigger);
                card.OnAccepted.AddListener(JobBoardIntroTrigger);
            }
        }

        if (jobBoardUI != null)
        {
            var jb = jobBoardUI.GetComponent<JobBoardUI>();
            if (jb != null) jb.Opened.AddListener(JobBoardTutorialTrigger);
        }

        if (computer != null)
        {
            WorldShopBuilding wb = computer.GetComponent<WorldShopBuilding>();
            if (wb != null)
            {
                wb.Opened.RemoveListener(OnComputerOpened);
                wb.Opened.AddListener(OnComputerOpened);
            }
        }

        wc = null;

        if (computer != null)
            wc = computer.GetComponent<WorkshopComputer>();

        if (wc == null && computerUI != null)
            wc = computerUI.GetComponentInChildren<WorkshopComputer>(true);

        if (computerUI == null && wc != null && wc.computerPanel != null)
            computerUI = wc.computerPanel;

        if (wc != null)
        {
            wc.ComputerOpened.RemoveListener(OnComputerOpened);
            wc.ShopOpened.RemoveListener(OnShopOpened);
            wc.StockMarketOpened.RemoveListener(OnStockMarketOpened);

            wc.ComputerOpened.AddListener(OnComputerOpened);
            wc.ShopOpened.AddListener(OnShopOpened);
            wc.StockMarketOpened.AddListener(OnStockMarketOpened);

            if (wc.shopPanel != null)
            {
                wc.shopPanel.Purchased.RemoveListener(OnShopPurchased);
                wc.shopPanel.Purchased.AddListener(OnShopPurchased);
            }
        }

        if (computerUI != null)
        {
            SetArrowClickThrough(FindDeepByName(computerUI.transform, "Tutorial_Arrow"));
            SetArrowClickThrough(FindDeepByName(computerUI.transform, "Tutorial_Arrow_Stock"));
        }

        dialogueIndex = 1;
        SetDialogue(true);
        Stage1Intro();
        if (jm == null) jm = FindFirstObjectByType<JobManager>();

        if (storageCrate != null)
        {
            storageBuilding = storageCrate.GetComponent<StorageBuilding>();
            if (storageBuilding != null)
            {
                storageBuilding.Opened.RemoveListener(OnStorageOpened);
                storageBuilding.Opened.AddListener(OnStorageOpened);
            }
        }

    }
    void OnStorageOpened()
    {
        if (currentStage != 3) return;
        if (!waitingForStorageOpen) return;

        waitingForStorageOpen = false;
        waitingForWoodInHotbar = true;

        dialogueIndex = 2;
        SetDialogue(true);
        StorageUITutorial();
    }
    bool HasEnoughWoodInHotbar()
    {
        if (woodItemSO == null) return false;

        var bars = FindObjectsOfType<HotBar>(true);
        for (int i = 0; i < bars.Length; i++)
        {
            var s = bars[i].Peek();
            if (s.item == woodItemSO && s.count >= woodNeededForChair)
                return true;
        }
        return false;
    }

    bool TutorialJobDelivered()
    {
        if (jm == null) jm = FindFirstObjectByType<JobManager>();
        if (jm == null) return false;
        return jm.TutorialJobCompleted;
    }

    void SetTutorialRaycast(bool on)
    {
        if (cg == null) return;
        cg.blocksRaycasts = on;
        cg.interactable = on;
    }
    bool RequiredShopBuysComplete()
    {
        string[] ids = requiredShopItemIds;

        if (ids == null || ids.Length == 0)
            ids = new string[] { "Chair", "TableSaw", "LaserCutter", "Assembly" };

        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id)) continue;

            if (!ShopRequirementComplete(id))
                return false;
        }

        return true;
    }

    bool ShopRequirementComplete(string id)
    {
        if (PlayerPrefs.GetInt("ShopOwned_" + id, 0) == 1) return true;
        if (PlayerPrefs.GetInt("RecipeUnlocked_" + id, 0) == 1) return true;
        if (PlayerPrefs.GetInt("MachineOwned_" + id, 0) == 1) return true;
        return false;
    }

    void OnShopPurchased(ShopItemSO item)
    {
        if (!waitingForShopPurchases) return;
        if (!RequiredShopBuysComplete()) return;
        CompleteRequiredShopPurchases();
    }

    void CompleteRequiredShopPurchases()
    {
        if (!waitingForShopPurchases) return;

        waitingForShopPurchases = false;
        shopInstructionReadyToHide = false;
        SetTutorialRaycast(true);
        SetDialogue(false);

        if (wc != null && wc.shopPanel != null)
            wc.shopPanel.Close();

        visitedShop = true;
        shopUIOpened = false;
        dialogueIndex = 1;

        stockIntroComplete = false;
        waitingForStockOpen = false;
        stockUIOpened = false;

        StockIntro();
    }


    void Update()
    {
        if (waitingForShopPurchases && RequiredShopBuysComplete())
        {
            CompleteRequiredShopPurchases();
            return;
        }

        bool pressed = false;

        if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began) pressed = true;
        }

        if (!pressed && Input.GetMouseButtonDown(0)) pressed = true;
        if (!pressed) return;

        if (shopInstructionReadyToHide)
        {
            shopInstructionReadyToHide = false;
            waitingForShopPurchases = true;
            SetDialogue(false, false);
            SetTutorialRaycast(false);
            dialogueIndex = 5;

            if (RequiredShopBuysComplete())
                CompleteRequiredShopPurchases();

            return;
        }

        if (!tutorialTextActive && !waitingForStorageOpen && !waitingForWoodInHotbar && !waitingForTutorialJobComplete && !showingTutorialEndMessage && !waitingForShopPurchases) return;

        if (waitingForShopPurchases) return;

        dialogueIndex++;

        if (currentStage == 1)
        {
            if (!stage1IntroDialogueComplete) { Stage1Intro(); return; }
            if (currentRoom == "RoomLoadingBay" && !visitedLoadingBay) { LoadingBayTutorial(); return; }
            if (currentRoom == "RoomLumberYard" && !visitedLumberYard) { LumberYardTutorial(); return; }
            if (currentRoom == "RoomStorage" && !visitedStorageRoom) { StorageRoomTutorial(); return; }
            if (currentRoom != "Workshop" && !stage1Complete) { Stage1CompleteCheck(); return; }
        }

        if (currentStage == 2)
        {
            if (!stage2IntroDialogueComplete) { Stage2Intro(); return; }
            if (!visitedCustomer) { CustomerTutorial(); return; }
            if (!jobBoardIntroComplete) { JobBoardIntro(); return; }
            if (!visitedJobBoard) { JobBoardTutorial(); return; }
            if (!computerIntroComplete) { ComputerIntro(); return; }

            if (!shopIntroComplete) { ShopIntro(); return; }

            if (!visitedShop)
            {
                if (waitingForShopOpen) return;
                if (!shopUIOpened) return;
                ShopTutorial();
                return;
            }

            if (!stockIntroComplete) { StockIntro(); return; }

            if (!visitedStockMarket)
            {
                if (waitingForStockOpen) return;
                if (!stockUIOpened) return;
                StockMarketTutorial();
                return;
            }
        }

        if (currentStage == 3)
        {
            if (waitingForTutorialJobComplete)
            {
                if (!TutorialJobDelivered()) return;

                waitingForTutorialJobComplete = false;
                showingTutorialEndMessage = true;
                dialogueIndex = 1;
                SetDialogue(true);
                TutorialEndMessage();
                return;
            }
            if (!visitedStorageForWood)
            {
                if (dialogueIndex == 2 && !waitingForStorageOpen && !waitingForWoodInHotbar)
                {
                    waitingForStorageOpen = true;
                    SetDialogue(false);
                    return;
                }

                StorageUITutorial();
                return;
            }


            if (showingTutorialEndMessage)
            {
                TutorialEndMessage();
                return;
            }
        }
    }

    void OnComputerOpened()
    {
        if (currentStage != 2) return;
        if (!computerIntroComplete) return;

        if (!visitedShop)
        {
            if (!shopIntroComplete)
            {
                dialogueIndex = 1;
                SetDialogue(false);
                Invoke(nameof(DelayedShopIntro), 0.05f);
                return;
            }

            if (waitingForShopOpen)
            {
                SetUIArrow("Tutorial_Arrow", true);
                SetUIArrow("Tutorial_Arrow_Stock", false);
                return;
            }
        }

        if (visitedShop && !visitedStockMarket)
        {
            if (!stockIntroComplete)
            {
                dialogueIndex = 1;
                SetDialogue(false);
                Invoke(nameof(DelayedStockIntro), 0.05f);
                return;
            }

            if (waitingForStockOpen)
            {
                SetUIArrow("Tutorial_Arrow", false);
                SetUIArrow("Tutorial_Arrow_Stock", true);
                return;
            }
        }
    }

    void DelayedShopIntro()
    {
        dialogueIndex = 1;
        ShopIntro();
    }

    void DelayedStockIntro()
    {
        dialogueIndex = 1;
        StockIntro();
    }

    void OnShopOpened()
    {
        if (currentStage != 2) return;
        if (visitedShop) return;

        if (!waitingForShopOpen && !shopIntroComplete && computerIntroComplete)
        {
            shopIntroComplete = true;
            waitingForShopOpen = true;
            dialogueIndex = 1;
        }

        if (!waitingForShopOpen) return;

        waitingForShopOpen = false;
        shopUIOpened = true;

        SetUIArrow("Tutorial_Arrow", false);
        SetUIArrow("Tutorial_Arrow_Stock", false);

        dialogueIndex = 1;
        SetDialogue(false);
        Invoke(nameof(DelayedShopTutorialStart), 0.12f);
    }

    void DelayedShopTutorialStart()
    {
        dialogueIndex = 1;
        SetDialogue(true, false);
        ShopTutorial();
    }

    void OnStockMarketOpened()
    {
        if (currentStage != 2) return;
        if (visitedStockMarket) return;

        if (!waitingForStockOpen && !stockIntroComplete && visitedShop)
        {
            stockIntroComplete = true;
            waitingForStockOpen = true;
            dialogueIndex = 1;
        }

        if (!waitingForStockOpen) return;

        waitingForStockOpen = false;
        stockUIOpened = true;

        SetUIArrow("Tutorial_Arrow", false);
        SetUIArrow("Tutorial_Arrow_Stock", false);

        dialogueIndex = 1;
        SetDialogue(false);
        Invoke(nameof(DelayedStockTutorialStart), 0.12f);
    }

    void DelayedStockTutorialStart()
    {
        dialogueIndex = 1;
        SetDialogue(true, false);
        StockMarketTutorial();
    }

    void SetDialogue(bool on)
    {
        SetDialogue(on, on);
    }

    void SetDialogue(bool on, bool blockRaycasts)
    {
        if (textPanel != null) textPanel.SetActive(on);
        tutorialTextActive = on;

        if (cg != null)
        {
            cg.blocksRaycasts = on && blockRaycasts;
            cg.interactable = on && blockRaycasts;
            cg.alpha = 1f;
        }
    }

    void Stage1Intro()
    {
        SetDialogue(true);

        if (!stage1IntroDialogueComplete)
        {
            if (dialogueIndex == 1)
            {
                if (tutorialText != null)
                    tutorialText.text = "Welcome to LumberJill's Carpenter Shop! Explore the workshop by clicking around, to go to another room walk over the yellow boxes on the floor.";
                return;
            }

            SetDialogue(false);
            stage1IntroDialogueComplete = true;
            dialogueIndex = 1;

            SetWorldArrow(doorLoadingBay, true);
            SetWorldArrow(doorLumberYard, true);
            SetWorldArrow(doorStorageRoom, true);
        }
    }

    void LoadingBayEnter()
    {
        if (currentStage != 1) return;
        currentRoom = "RoomLoadingBay";
        SetWorldArrow(doorLoadingBay, false);
        dialogueIndex = 1;
        LoadingBayTutorial();
    }

    void LoadingBayTutorial()
    {
        if (currentStage == 1 && currentRoom == "RoomLoadingBay")
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "This is the Loading Bay, where all the finished products are shipped out to customers.";
                return;
            }

            SetDialogue(false);
            visitedLoadingBay = true;
            dialogueIndex = 1;
            if (doorLoadingBay != null) doorLoadingBay.GetComponentInChildren<Button>(true).onClick.RemoveListener(LoadingBayEnter);
            Stage1CompleteCheck();
        }
    }

    void LumberYardEnter()
    {
        if (currentStage != 1) return;
        currentRoom = "RoomLumberYard";
        SetWorldArrow(doorLumberYard, false);
        dialogueIndex = 1;
        LumberYardTutorial();
    }

    void LumberYardTutorial()
    {
        if (currentStage == 1 && currentRoom == "RoomLumberYard")
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "This is the Lumber Yard, it used to be full of trees but Jack cut them all down in a rush trying to make too many products at once.";
                return;
            }
            if (dialogueIndex == 2) { if (tutorialText != null) tutorialText.text = "whilst we wait for our trees to grow back, I've brought my computer from my old investment banking job so we can buy wood from the stock market!"; return; }
            if (dialogueIndex == 3) { if (tutorialText != null) tutorialText.text = "Once we start growing enough trees we can sell the excess wood on the stock market for a profit!"; return; }

            SetDialogue(false);
            visitedLumberYard = true;
            if (doorLumberYard != null) doorLumberYard.GetComponentInChildren<Button>(true).onClick.RemoveListener(LumberYardEnter);
            dialogueIndex = 1;
            Stage1CompleteCheck();
        }
    }

    void StorageRoomEnter()
    {
        if (currentStage != 1) return;
        currentRoom = "RoomStorage";
        SetWorldArrow(doorStorageRoom, false);
        dialogueIndex = 1;
        StorageRoomTutorial();
    }

    void StorageRoomTutorial()
    {
        if (currentStage == 1 && currentRoom == "RoomStorage")
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "This is the Storage Room, where we keep all our inventory of raw materials and finished products.";
                return;
            }

            SetDialogue(false);
            visitedStorageRoom = true;
            if (doorStorageRoom != null) doorStorageRoom.GetComponentInChildren<Button>(true).onClick.RemoveListener(StorageRoomEnter);
            dialogueIndex = 1;
            Stage1CompleteCheck();
        }
    }

    void Stage1CompleteCheck()
    {
        if (!(stage1IntroDialogueComplete && visitedLoadingBay && visitedLumberYard && visitedStorageRoom)) return;

        if (dialogueIndex == 1 && currentRoom != "Workshop")
        {
            SetDialogue(true);
            if (tutorialText != null)
                tutorialText.text = "Let's head back to the Workshop, now that we've seen all the rooms.";
            return;
        }

        if (dialogueIndex > 1)
        {
            SetDialogue(false);
            dialogueIndex = 1;
        }

        if (currentRoom == "Workshop")
        {
            stage1Complete = true;
            currentStage = 2;
            NPCSpawn();
            dialogueIndex = 1;
            Stage2Intro();
        }
    }

    void NPCSpawn()
    {
        if (jm == null) jm = FindFirstObjectByType<JobManager>();
        if (jm != null)
            jm.StartTutorialJob(chairItemSO);
    }

    void Stage2Intro()
    {
        if (currentStage == 2)
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "Oh look! A customer, let's talk to them and see what they want us to make for them.";
                return;
            }

            SetDialogue(false);
            stage2IntroDialogueComplete = true;
            dialogueIndex = 1;
        }
    }

    void CustomerTutorialTrigger()
    {
        if (currentStage == 2)
        {
            CustomerCardUI card = customerUI != null ? customerUI.GetComponentInChildren<CustomerCardUI>(true) : null;
            if (card != null) card.OnShown.RemoveListener(CustomerTutorialTrigger);

            Transform a = customerUI != null ? FindDeepByName(customerUI.transform, "Tutorial_Arrow") : null;
            if (a != null)
            {
                a.gameObject.SetActive(true);
                SetArrowClickThrough(a);
            }

            dialogueIndex = 1;
            CustomerTutorial();
        }
    }

    void CustomerTutorial()
    {
        if (currentStage == 2)
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null) tutorialText.text = "This is the customer's order.";
                return;
            }
            if (dialogueIndex == 2) { if (tutorialText != null) tutorialText.text = "This is the customer's order, here you can see what they want, how much they are willing to pay and the deadline for when they need it by."; return; }
            if (dialogueIndex == 3) { if (tutorialText != null) tutorialText.text = "Let's accept their order so we can get started!"; return; }

            SetDialogue(false);
            visitedCustomer = true;
            dialogueIndex = 1;
        }
    }

    void JobBoardIntroTrigger()
    {
        if (currentStage == 2)
        {
            if (jobBoard == null)
            {
                JobBoard foundBoard = FindFirstObjectByType<JobBoard>();
                if (foundBoard != null) jobBoard = foundBoard.gameObject;
            }

            SetWorldArrow(jobBoard, true);

            CustomerCardUI card = customerUI != null ? customerUI.GetComponentInChildren<CustomerCardUI>(true) : null;
            if (card != null) card.OnAccepted.RemoveListener(JobBoardIntroTrigger);

            dialogueIndex = 1;
            JobBoardIntro();
        }
    }

    void JobBoardIntro()
    {
        if (currentStage == 2)
        {
            if (dialogueIndex == 1)
            {
                Transform a = customerUI != null ? FindDeepByName(customerUI.transform, "Tutorial_Arrow") : null;
                if (a != null) a.gameObject.SetActive(false);

                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "Great! Now that we've talked to the customer, let's check the job board to get the full details of the item we need to make for them.";
                return;
            }

            SetDialogue(false);
            jobBoardIntroComplete = true;
            dialogueIndex = 1;
        }
    }

    void JobBoardTutorialTrigger()
    {
        if (currentStage == 2 && !visitedJobBoard)
        {
            SetWorldArrow(jobBoard, false);
            dialogueIndex = 1;
            JobBoardTutorial();
        }
    }

    void JobBoardTutorial()
    {
        if (currentStage == 2)
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "Here we can see the same information as the customer's order, but we can also see what star rating they will give us based on our current progress.";
                return;
            }
            if (dialogueIndex == 2) { if (tutorialText != null) tutorialText.text = "As we get more customers we can see all their orders on the job board and prioritise which ones to complete first based on their deadlines and payment amounts."; return; }

            SetDialogue(false);
            visitedJobBoard = true;
            dialogueIndex = 1;
            ComputerIntro();
        }
    }

    void ComputerIntro()
    {
        if (currentStage == 2)
        {
            if (dialogueIndex == 1)
            {
                SetDialogue(true);
                if (tutorialText != null)
                    tutorialText.text = "Now that we know what item we need to make, let's head to the shop on the computer to buy the blueprints and machines we need.";
                return;
            }

            SetDialogue(false);
            computerIntroComplete = true;
            dialogueIndex = 1;
            SetWorldArrow(computer, true);
        }
    }

    void ShopIntro()
    {
        if (currentStage != 2) return;

        if (dialogueIndex == 1)
        {
            SetWorldArrow(computer, false);

            SetUIArrow("Tutorial_Arrow", true);
            SetUIArrow("Tutorial_Arrow_Stock", false);

            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Click the shop icon once to open the shop.";
            return;
        }

        SetDialogue(false, false);
        shopIntroComplete = true;
        waitingForShopOpen = true;
        dialogueIndex = 1;
    }

    void ShopTutorial()
    {
        if (currentStage != 2) return;

        if (dialogueIndex == 1)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "There are two tabs. Open the blueprints tab first and buy the chair blueprint.";
            return;
        }

        if (dialogueIndex == 2)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Then open the machines tab and buy the machines needed for the chair.";
            return;
        }

        if (dialogueIndex == 3)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "You need the Table Saw, Laser Cutter and Assembly Station.";
            return;
        }

        if (dialogueIndex == 4)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Later customers will need harder items, so you will come back here for more blueprints and upgrades.";
            return;
        }

        if (dialogueIndex == 5)
        {
            SetDialogue(true, false);
            shopInstructionReadyToHide = true;
            if (tutorialText != null)
                tutorialText.text = "Now buy the chair blueprint and all needed machines. Tap anywhere once to hide this message, then finish buying them.";
            return;
        }

        shopInstructionReadyToHide = false;
        waitingForShopPurchases = true;
        SetDialogue(false, false);
        SetTutorialRaycast(false);
        dialogueIndex = 5;

        if (RequiredShopBuysComplete())
            CompleteRequiredShopPurchases();
    }


    void StockIntro()
    {
        if (currentStage != 2) return;
        if (!visitedShop) return;
        if (visitedStockMarket) return;

        if (dialogueIndex == 1)
        {
            SetUIArrow("Tutorial_Arrow", false);
            SetUIArrow("Tutorial_Arrow_Stock", true);

            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Click the stock market icon once to open the stock market.";
            return;
        }

        SetDialogue(false, false);
        stockIntroComplete = true;
        waitingForStockOpen = true;
        dialogueIndex = 1;
    }

    void StockMarketTutorial()
    {
        if (currentStage != 2) return;

        if (dialogueIndex == 1)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Use the stock market to buy lumber for the chair.";
            return;
        }

        if (dialogueIndex == 2)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "The graph shows the lumber price from the past 24 hours.";
            return;
        }

        if (dialogueIndex == 3)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Green bars mean the price went up. Red bars mean the price went down.";
            return;
        }

        if (dialogueIndex == 4)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Enter the amount of lumber you want, then click buy.";
            return;
        }

        if (dialogueIndex == 5)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "Buy enough lumber for the chair. It will go to storage.";
            return;
        }

        if (dialogueIndex == 6)
        {
            SetDialogue(true, false);
            if (tutorialText != null)
                tutorialText.text = "You can also sell lumber later if the price goes higher.";
            return;
        }

        SetDialogue(false, false);
        visitedStockMarket = true;
        stockUIOpened = false;
        dialogueIndex = 1;
        currentStage = 3;

        SetDialogue(true);
        StorageUITutorial();
    }

    void StorageUITutorial()
    {
        if (currentStage != 3) return;
        if (visitedStorageForWood) return;

        if (waitingForStorageOpen)
        {
            SetDialogue(false);
            return;
        }

        if (waitingForWoodInHotbar)
        {
            dialogueIndex = 2;
            SetDialogue(true);
            SetTutorialRaycast(false);

            if (tutorialText != null)
                tutorialText.text = "Drag and drop the wood into your hot bar to collect it.";

            if (!HasEnoughWoodInHotbar())
                return;

            waitingForWoodInHotbar = false;
            SetTutorialRaycast(true);

            dialogueIndex = 3;
            SetDialogue(true);
        }

        if (dialogueIndex == 1)
        {
            SetDialogue(true);
            SetTutorialRaycast(true);

            if (tutorialText != null)
                tutorialText.text = "Go to the storage crate and open it.";

            SetWorldArrow(storageCrate, true);
            return;
        }

        if (dialogueIndex == 2)
        {
            SetDialogue(true);
            SetTutorialRaycast(false);

            if (tutorialText != null)
                tutorialText.text = "Drag and drop the wood into your hot bar to collect it.";

            return;
        }

        if (dialogueIndex == 3)
        {
            SetDialogue(true);
            SetTutorialRaycast(true);

            if (tutorialText != null)
                tutorialText.text = "Now that we have the wood, let's head to the table saw to start making the chair. Finish Charlie's job and deliver it to end the tutorial.";

            return;
        }

        SetDialogue(false);
        visitedStorageForWood = true;
        SetWorldArrow(storageCrate, false);
        dialogueIndex = 1;

        waitingForTutorialJobComplete = true;
        showingTutorialEndMessage = false;
    }
    void RestoreNormalCustomers()
    {
        if (jm == null) jm = FindFirstObjectByType<JobManager>();
        if (jm == null) return;
        jm.EndTutorialAndStartRealGame();
    }

    void TutorialEndMessage()
    {
        if (!showingTutorialEndMessage) return;

        if (dialogueIndex == 1)
        {
            SetDialogue(true);
            if (tutorialText != null)
                tutorialText.text = "Tutorial complete. Keep taking jobs, keep delivering, and try not to bankrupt the shop.";
            return;
        }

        SetDialogue(false);
        tutorialCompleted = true;
        RestoreNormalCustomers();
        gameObject.SetActive(false);
    }



    void SetWorldArrow(GameObject root, bool on)
    {
        if (root == null) return;

        Transform a = root.transform.Find("Canvas/Tutorial_Arrow");
        if (a == null) a = FindDeepByName(root.transform, "Tutorial_Arrow");
        if (a == null) return;

        a.gameObject.SetActive(on);
        SetArrowClickThrough(a);
    }

    void SetUIArrow(string name, bool on)
    {
        if (computerUI == null) return;
        var t = FindDeepByName(computerUI.transform, name);
        if (t == null) return;
        t.gameObject.SetActive(on);
        SetArrowClickThrough(t);
    }

    void SetArrowClickThrough(Transform arrowRoot)
    {
        if (arrowRoot == null) return;

        var graphics = arrowRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        var acg = arrowRoot.GetComponent<CanvasGroup>();
        if (acg == null) acg = arrowRoot.gameObject.AddComponent<CanvasGroup>();
        acg.blocksRaycasts = false;
        acg.interactable = false;
    }

    Transform FindDeepByName(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindDeepByName(root.GetChild(i), targetName);
            if (r != null) return r;
        }

        return null;
    }
}
