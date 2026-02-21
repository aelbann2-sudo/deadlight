using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Deadlight.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Image backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private ShopItem item;
        private Action<ShopItem> onPurchase;

        public void Setup(ShopItem shopItem, Action<ShopItem> purchaseCallback)
        {
            item = shopItem;
            onPurchase = purchaseCallback;

            UpdateDisplay();

            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
        }

        private void UpdateDisplay()
        {
            if (item == null) return;

            if (iconImage != null && item.icon != null)
                iconImage.sprite = item.icon;

            if (nameText != null)
                nameText.text = item.itemName;

            if (descriptionText != null)
                descriptionText.text = item.description;

            if (costText != null)
                costText.text = $"{item.cost} pts";

            UpdateAffordability();
        }

        private void UpdateAffordability()
        {
            bool canAfford = Systems.PointsSystem.Instance != null && 
                             Systems.PointsSystem.Instance.CanAfford(item.cost);

            if (purchaseButton != null)
                purchaseButton.interactable = canAfford;

            if (backgroundImage != null)
                backgroundImage.color = canAfford ? affordableColor : unaffordableColor;

            if (costText != null)
                costText.color = canAfford ? Color.green : Color.red;
        }

        private void OnPurchaseClicked()
        {
            onPurchase?.Invoke(item);
            UpdateAffordability();
        }

        private void Update()
        {
            UpdateAffordability();
        }
    }
}
