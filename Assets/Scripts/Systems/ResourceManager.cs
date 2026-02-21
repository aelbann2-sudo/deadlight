using UnityEngine;
using Deadlight.Core;
using System;
using System.Collections.Generic;

namespace Deadlight.Systems
{
    public enum ResourceType
    {
        Scrap,
        Wood,
        Chemicals,
        Electronics,
        Ammo,
        Health
    }

    [Serializable]
    public class ResourceAmount
    {
        public ResourceType type;
        public int amount;

        public ResourceAmount(ResourceType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }

    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Starting Resources")]
        [SerializeField] private int startingScrap = 0;
        [SerializeField] private int startingWood = 0;
        [SerializeField] private int startingChemicals = 0;
        [SerializeField] private int startingElectronics = 0;

        [Header("Current Inventory")]
        [SerializeField] private Dictionary<ResourceType, int> inventory = new Dictionary<ResourceType, int>();

        public event Action<ResourceType, int> OnResourceChanged;
        public event Action OnInventoryUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            inventory[ResourceType.Scrap] = startingScrap;
            inventory[ResourceType.Wood] = startingWood;
            inventory[ResourceType.Chemicals] = startingChemicals;
            inventory[ResourceType.Electronics] = startingElectronics;
            inventory[ResourceType.Ammo] = 0;
            inventory[ResourceType.Health] = 0;
        }

        public int GetResource(ResourceType type)
        {
            return inventory.ContainsKey(type) ? inventory[type] : 0;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (!inventory.ContainsKey(type))
            {
                inventory[type] = 0;
            }

            inventory[type] += amount;
            
            OnResourceChanged?.Invoke(type, inventory[type]);
            OnInventoryUpdated?.Invoke();

            Debug.Log($"[ResourceManager] Added {amount} {type}. Total: {inventory[type]}");
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            if (!HasResource(type, amount))
            {
                Debug.Log($"[ResourceManager] Not enough {type}. Have: {GetResource(type)}, Need: {amount}");
                return false;
            }

            inventory[type] -= amount;
            
            OnResourceChanged?.Invoke(type, inventory[type]);
            OnInventoryUpdated?.Invoke();

            Debug.Log($"[ResourceManager] Spent {amount} {type}. Remaining: {inventory[type]}");
            return true;
        }

        public bool HasResource(ResourceType type, int amount)
        {
            return GetResource(type) >= amount;
        }

        public bool HasResources(List<ResourceAmount> requirements)
        {
            foreach (var req in requirements)
            {
                if (!HasResource(req.type, req.amount))
                {
                    return false;
                }
            }
            return true;
        }

        public bool SpendResources(List<ResourceAmount> costs)
        {
            if (!HasResources(costs)) return false;

            foreach (var cost in costs)
            {
                SpendResource(cost.type, cost.amount);
            }

            return true;
        }

        public Dictionary<ResourceType, int> GetAllResources()
        {
            return new Dictionary<ResourceType, int>(inventory);
        }

        public void ResetInventory()
        {
            InitializeInventory();
            OnInventoryUpdated?.Invoke();
        }

        public void SetResource(ResourceType type, int amount)
        {
            inventory[type] = Mathf.Max(0, amount);
            OnResourceChanged?.Invoke(type, inventory[type]);
            OnInventoryUpdated?.Invoke();
        }
    }
}
