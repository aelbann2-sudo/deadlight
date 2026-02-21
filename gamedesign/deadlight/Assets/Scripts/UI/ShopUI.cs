using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deadlight.Core;
using Deadlight.Systems;
using Deadlight.Data;
using System.Collections.Generic;

namespace Deadlight.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Shop Panel")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI pointsText;

        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI nightSurvivedText;
        [SerializeField] private TextMeshProUGUI enemiesKilledText;
        [SerializeField] private TextMeshProUGUI pointsEarnedText;

        [Header("Shop Items Container")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject shopItemPrefab;

        [Header("Categories")]
        [SerializeField] private Button weaponsTabButton;
        [SerializeField] private Button suppliesTabButton;
        [SerializeField] private Button upgradesTabButton;

        [Header("Continue Button")]
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI nextNightPreviewText;

        [Header("Shop Items Data")]
        [SerializeField] private List<ShopItem> weaponItems = new List<ShopItem>();
        [SerializeField] private List<ShopItem> supplyItems = new List<ShopItem>();
        [SerializeField] private List<ShopItem> upgradeItems = new List<ShopItem>();

        private ShopCategory currentCategory = ShopCategory.Weapons;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            SetupButtons();
            InitializeDefaultItems();
            HideShop();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void SetupButtons()
        {
            if (weaponsTabButton != null)
                weaponsTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Weapons));

            if (suppliesTabButton != null)
                suppliesTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Supplies));

            if (upgradesTabButton != null)
                upgradesTabButton.onClick.AddListener(() => ShowCategory(ShopCategory.Upgrades));

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void InitializeDefaultItems()
        {
            if (supplyItems.Count == 0)
            {
                supplyItems = new List<ShopItem>
                {
                    new ShopItem { itemName = "Ammo Pack", description = "Restore 50 ammo", cost = 50, itemType = ShopItemType.Ammo, amount = 50 },
                    new ShopItem { itemName = "Health Kit", description = "Restore 50 health", cost = 75, itemType = ShopItemType.Health, amount = 50 },
                    new ShopItem { itemName = "Large Ammo Pack", description = "Restore 100 ammo", cost = 90, itemType = ShopItemType.Ammo, amount = 100 },
                    new ShopItem { itemName = "Large Health Kit", description = "Restore full health", cost = 150, itemType = ShopItemType.Health, amount = 100 }
                };
            }

            if (upgradeItems.Count == 0)
            {
                upgradeItems = new List<ShopItem>
                {
                    new ShopItem { itemName = "Speed Boost", description = "Move 20% faster next night", cost = 100, itemType = ShopItemType.SpeedBoost, amount = 20 },
                    new ShopItem { itemName = "Damage Boost", description = "Deal 15% more damage next night", cost = 120, itemType = ShopItemType.DamageBoost, amount = 15 },
                    new ShopItem { itemName = "Armor", description = "Take 15% less damage next night", cost = 120, itemType = ShopItemType.Armor, amount = 15 }
                };
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.DawnPhase)
            {
                ShowShop();
            }
            else
            {
                HideShop();
            }
        }

        public void ShowShop()
        {
            if (shopPanel != null)
                shopPanel.SetActive(true);

            UpdateStatistics();
            UpdatePointsDisplay();
            ShowCategory(ShopCategory.Supplies);
            UpdateNextNightPreview();

            Time.timeScale = 0f;
        }

        public void HideShop()
        {
            if (shopPanel != null)
                shopPanel.SetActive(false);

            Time.timeScale = 1f;
        }

        private void UpdateStatistics()
        {
            if (GameManager.Instance != null && nightSurvivedText != null)
                nightSurvivedText.text = $"Night {GameManager.Instance.CurrentNight} Survived!";

            if (PointsSystem.Instance != null)
            {
                if (enemiesKilledText != null)
                    enemiesKilledText.text = $"Enemies Killed: {PointsSystem.Instance.EnemiesKilled}";

                if (pointsEarnedText != null)
                    pointsEarnedText.text = $"Points Earned: {PointsSystem.Instance.TotalEarned}";
            }
        }

        private void UpdatePointsDisplay()
        {
            if (pointsText != null && PointsSystem.Instance != null)
                pointsText.text = $"Points: {PointsSystem.Instance.CurrentPoints}";
        }

        private void ShowCategory(ShopCategory category)
        {
            currentCategory = category;
            ClearItems();

            List<ShopItem> items = category switch
            {
                ShopCategory.Weapons => weaponItems,
                ShopCategory.Supplies => supplyItems,
                ShopCategory.Upgrades => upgradeItems,
                _ => supplyItems
            };

            foreach (var item in items)
            {
                CreateShopItemUI(item);
            }
        }

        private void ClearItems()
        {
            if (itemsContainer == null) return;

            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateShopItemUI(ShopItem item)
        {
            if (itemsContainer == null || shopItemPrefab == null) return;

            GameObject itemObj = Instantiate(shopItemPrefab, itemsContainer);
            var itemUI = itemObj.GetComponent<ShopItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(item, OnItemPurchased);
            }
            else
            {
                var nameText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = $"{item.itemName} - {item.cost} pts";

                var button = itemObj.GetComponent<Button>();
                if (button != null)
                {
                    var capturedItem = item;
                    button.onClick.AddListener(() => OnItemPurchased(capturedItem));
                }
            }
        }

        private void OnItemPurchased(ShopItem item)
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(item.cost))
            {
                Debug.Log("[ShopUI] Cannot afford item");
                return;
            }

            if (PointsSystem.Instance.SpendPoints(item.cost, item.itemName))
            {
                ApplyItemEffect(item);
                UpdatePointsDisplay();
                Debug.Log($"[ShopUI] Purchased: {item.itemName}");
            }
        }

        private void ApplyItemEffect(ShopItem item)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            switch (item.itemType)
            {
                case ShopItemType.Ammo:
                    var shooting = player.GetComponent<Player.PlayerShooting>();
                    shooting?.AddAmmo(item.amount);
                    break;

                case ShopItemType.Health:
                    var health = player.GetComponent<Player.PlayerHealth>();
                    health?.Heal(item.amount);
                    break;

                case ShopItemType.SpeedBoost:
                    var controller = player.GetComponent<Player.PlayerController>();
                    controller?.ApplySpeedModifier(1f + item.amount / 100f, 210f);
                    break;
            }
        }

        private void UpdateNextNightPreview()
        {
            if (nextNightPreviewText != null && GameManager.Instance != null)
            {
                int nextNight = GameManager.Instance.CurrentNight + 1;
                if (nextNight <= GameManager.Instance.MaxNights)
                {
                    nextNightPreviewText.text = $"Next: Night {nextNight}\nEnemies will be stronger!";
                }
                else
                {
                    nextNightPreviewText.text = "Final Night Complete!\nVictory is yours!";
                }
            }
        }

        private void OnContinueClicked()
        {
            HideShop();
            GameManager.Instance?.AdvanceToNextNight();
        }

        public void AddWeaponItem(ShopItem item)
        {
            weaponItems.Add(item);
        }

        public void AddSupplyItem(ShopItem item)
        {
            supplyItems.Add(item);
        }

        public void AddUpgradeItem(ShopItem item)
        {
            upgradeItems.Add(item);
        }
    }

    public enum ShopCategory
    {
        Weapons,
        Supplies,
        Upgrades
    }

    public enum ShopItemType
    {
        Weapon,
        Ammo,
        Health,
        SpeedBoost,
        DamageBoost,
        Armor,
        Resource
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public string description;
        public int cost;
        public Sprite icon;
        public ShopItemType itemType;
        public int amount;
        public WeaponData weaponData;
    }
}
