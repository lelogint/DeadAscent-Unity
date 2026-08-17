using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
public class UiHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private Canvas winCanvas;
    [SerializeField] private Canvas getItemCanvas;
    [SerializeField] private Canvas menuUi;
    [SerializeField] private Canvas settingsUi;
    [SerializeField] private TMP_Text itemText;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private TMP_Text coinCount;
    [SerializeField] private TMP_Text fpsCount;
    [SerializeField] private TMP_Text selectFps;
    [SerializeField] private TMP_Text selectResolutionText;
    [SerializeField] private TMP_Text selectFullscreenText;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text maxHealthText;
    [SerializeField] private RawImage healthBar;
    [SerializeField] private RawImage soulBar;
    [SerializeField] private GameObject shopSelectionBox;
    [SerializeField] private GameObject[] shopItems;
    [SerializeField] private CinemachineCamera shopCamera;
    [SerializeField] private Transform boughtItemSpawn;
    private int currentShopIndex = 0;
    public bool isShopOpen = false;

    private SoundHandler playerSoundHandler;
    private float baseHealthBarScaleX;
    private float baseSoulBarScaleX;
    public string[] fpsOptions = { "60", "30", "75", "100", "125", "175", "V-SYNC" };
    public string[] resolutionOptions = { "1920x1080", "2560x1440", "1280x720" };
    private char[] splitDelimiters = { 'x', ' ' };
    private float currentFps;
    public int fpsCurrentIndex = 0;
    public int resolutionIndex = 0;
    public bool fullScreen = false;

    private Dictionary<string, ItemUiInfo> itemUiInfo = new Dictionary<string, ItemUiInfo>() // Item names and their respective tooltips
    {
        {"Sword", new ItemUiInfo("OLD SWORD", "USE (X)/MOUSE 1 TO ATTACK. HOLD (LT)/C MID-AIR AND ATTACK TO POGO SLASH")},
        {"Bash", new ItemUiInfo("SPIKED SPAULDER", "HOLD (RT)/SHIFT TO SPRINT AND SHOULDER BASH INTO ENEMIES")},
        {"WallJump", new ItemUiInfo("WALL JUMP", "JUMP AGAINST A WALL TO PERFORM THE WALL JUMP")},
        {"DoubleJump", new ItemUiInfo("WINGED BOOTS", "JUMP AGAIN WHILE AIRBORNE TO DOUBLE JUMP")},
        {"SwordUpgrade", new ItemUiInfo("ENCHANTED SWORD", "ALL SLASH DAMAGE DEALT IS MULTIPLIED BY 1.25x")},
        {"Key1", new ItemUiInfo("SKULL KEY", "OPEN THE SILVER LOCKED DOOR")},
        {"Key2", new ItemUiInfo("GOLD KEY", "OPEN THE FINAL LOCKED DOOR")},
        {"HeartCrystal", new ItemUiInfo("LIFE CRYSTAL", "IT INCREASES YOUR CURRENT MAX HP")},

    };

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 175;
    }

    void Start()
    {
        playerSoundHandler = GetComponent<SoundHandler>();
        baseHealthBarScaleX = healthBar.rectTransform.sizeDelta.x;
        baseSoulBarScaleX = soulBar.rectTransform.sizeDelta.x;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        ResolutionSettings(resolutionOptions[0]);
    }

    void Update()
    {
        currentFps = (int)(1f / Time.unscaledDeltaTime);
        fpsCount.text = currentFps.ToString();
    }

    public bool IsMenuOpen()
    {
        bool isOpen = (menuUi.enabled || settingsUi.enabled) ? true : false;
        return isOpen;
    }

    public void SetMaxFps() // Allow player to cycle through FPS options
    {
        playerSoundHandler.PlaySound("UiSelect", 1f);
        fpsCurrentIndex = (fpsCurrentIndex += 1) % fpsOptions.Length;
        string selectedOption = fpsOptions[fpsCurrentIndex];
        FpsSettings(selectedOption);
    }

    public void SetResolution()
    {
        playerSoundHandler.PlaySound("UiSelect", 1f);
        resolutionIndex = (resolutionIndex += 1) % resolutionOptions.Length;
        string selectedOption = resolutionOptions[resolutionIndex];
        ResolutionSettings(selectedOption);
    }
    public void SetFullscreen()
    {
        playerSoundHandler.PlaySound("UiSelect", 1f);
        fullScreen = (fullScreen == true) ? false : true;
        Screen.SetResolution(Screen.width, Screen.height, fullScreen);
        selectFullscreenText.text = fullScreen.ToString();
    }
    
    public void FullscreenSettings(bool fullScreened)
    {
        fullScreen = fullScreened;
        Screen.SetResolution(Screen.width, Screen.height, fullScreen);
        selectFullscreenText.text = fullScreen.ToString();
    }
    public void ResolutionSettings(string selectedOption)
    {
        selectResolutionText.text = selectedOption;
        string[] resolutionDimensions = selectedOption.Split(splitDelimiters);
        Screen.SetResolution(Convert.ToInt32(resolutionDimensions[0]), Convert.ToInt32(resolutionDimensions[1]), fullScreen);
    }

    public void FpsSettings(string selectedOption) // Set the fps to either v-sync (to monitor refresh) or desired frames
    {
        if (selectedOption == "V-SYNC")
        {
            QualitySettings.vSyncCount = 1; // Turn on v-sync
            Application.targetFrameRate = 1000;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Convert.ToInt32(selectedOption); // Convert string to int
        }
        selectFps.text = selectedOption;
    }

    public void OnMenu()
    {
        playerSoundHandler.PlaySound("UiBack", 1f);
        if (menuUi.enabled == true)
        {
            CloseMenu();
        }
        else
        {
            menuUi.enabled = true;
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }

    public void CloseMenu()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        CloseSettings();
        Time.timeScale = 1f;
        menuUi.enabled = false;
    }

    public void OnSettings()
    {
        playerSoundHandler.PlaySound("UiBack", 1f);
        settingsUi.enabled = true;
        menuUi.enabled = false;
    }

    public void CloseSettings()
    {
        playerSoundHandler.PlaySound("UiBack", 1f);
        settingsUi.enabled = false;
        menuUi.enabled = true;
    }

    public void UpdateCoinCount(int coins)
    {
        coinCount.text = "$" + coins.ToString("D3");
    }

    public void UiItemOpen(string tagKey) // Get class through tag containing the items tooltip and name
    {
        itemText.text = itemUiInfo[tagKey].name;
        tooltipText.text = itemUiInfo[tagKey].tooltip;
        getItemCanvas.enabled = true;
        if(tagKey == "Key1" || tagKey == "Key2")
        {
            victoryText.enabled = true;
        }
    }

    public void UiItemClose()
    {
        getItemCanvas.enabled = false;
    }

    public void OnWin()
    {
        winCanvas.enabled = true;
    }

    public void OnDeath()
    {
        gameOverCanvas.enabled = true;
    }

    public void ShopOpen()
    {
        shopCamera.enabled = true;
        isShopOpen = true;
        shopSelectionBox.SetActive(true);
        shopSelectionBox.transform.position = shopItems[currentShopIndex].transform.position;
    }

    public void ShopClose()
    {
        shopCamera.enabled = false;
        isShopOpen = false;
        shopSelectionBox.SetActive(false);
    }

    int WrapIndex(int index, int length)
    {
        return (index % length + length) % length;
    }

    public void NavigateShop(float inputXAxis) // Horizontally scroll across available items
    {
        int floorX = (inputXAxis < 0) ? Mathf.FloorToInt(inputXAxis) : Mathf.CeilToInt(inputXAxis);
        currentShopIndex = (currentShopIndex + floorX);
        currentShopIndex = WrapIndex(currentShopIndex, shopItems.Length);
        shopSelectionBox.transform.position = shopItems[currentShopIndex].transform.position;
    }
    public int ShopPurchase(int coins)
    {
        if (shopItems[currentShopIndex].transform.childCount > 0)
        {
            ItemCost costScript = shopItems[currentShopIndex].GetComponent<ItemCost>();
            int purchaseCost = costScript.cost;
            print(coins - purchaseCost);
            if ((coins - purchaseCost) >= 0)
            {
                Transform itemObj = shopItems[currentShopIndex].transform.GetChild(0);
                itemObj.parent = null;
                itemObj.position = boughtItemSpawn.transform.position;
                return (coins - purchaseCost);
            }
        }
        return coins;
    }

    public void UpdateHealth(float health, float maxHealth)
    {
        maxHealthText.text = "/" + maxHealth.ToString();
        HealthUi(health, maxHealth);
    }
    public void HealthUi(float health, float maxHealth)
    {
        float healthBarFormula = (baseHealthBarScaleX/maxHealth) * health; // (healthbar size x /100) * health, this is my formula made for calculating the size of the healthbar
        healthBar.rectTransform.sizeDelta = new Vector2(healthBarFormula, healthBar.rectTransform.sizeDelta.y); // Set new healthbar size.
    }

    public void SoulUi(float soul, float maxSoul)
    {
        float soulBarFormula = (baseHealthBarScaleX/maxSoul) * soul; // (healthbar size x /100) * health, this is my formula made for calculating the size of the healthbar
        soulBar.rectTransform.sizeDelta = new Vector2(soulBarFormula, soulBar.rectTransform.sizeDelta.y); // Set new healthbar size.
    }
}
