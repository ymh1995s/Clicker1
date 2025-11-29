using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Legacy.Core.BuyingConsumables
{
    public class PurchaseManager : Singleton<SaveManager>
    {
        StoreController m_StoreController; // The Unity Purchasing system.

        //Your products IDs. They should match the ids of your products in your store.
        public string crystal2000ProductId = "com.overture.asl.crystal2000";
        public string crystal10000ProductId = "com.overture.asl.crystal10000";

        int m_GoldCount;
        int m_DiamondCount;

        void Awake()
        {
            InitializeIAP();
        }

        async void InitializeIAP()
        {
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

            m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
            Debug.Log("Connecting to store.");
            await m_StoreController.Connect();

            m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
            m_StoreController.OnProductsFetched += OnProductsFetched;
            FetchProducts();
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(crystal2000ProductId, ProductType.Consumable),
                new(crystal10000ProductId, ProductType.Consumable)
            };

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        public void OnClick200Crystal()
        {
            m_StoreController.PurchaseProduct(crystal2000ProductId);
        }

        public void OnClick1000Crystal()
        {
            m_StoreController.PurchaseProduct(crystal10000ProductId);
        }

        void OnPurchaseFailed(FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in failed order.");
            }

            Debug.Log($"Purchase failed - Product: '{product?.definition.id}'," +
                      $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                      + $"Purchase Failure Details: {order.Details}");
        }

        void OnPurchasePending(PendingOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product is null)
            {
                Debug.Log("Could not find product in order.");
                return;
            }

            //Add the purchased product to the players inventory
            if (product.definition.id == crystal2000ProductId)
            {
                Add2000Crystal();
            }
            else if (product.definition.id == crystal10000ProductId)
            {
                Add10000Crystal();
            }

            Debug.Log($"Purchase complete - Product: {product.definition.id}");

            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
                case FailedOrder failedOrder:
                    OnPurchaseConfirmationFailed(failedOrder);
                    break;
                default:
                    Debug.Log("Unknown OnPurchaseConfirmed result.");
                    break;
            }
        }

        void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in purchase confirmation.");
            }

            Debug.Log($"Purchase confirmed- Product: {product?.definition.id}");
        }

        void OnPurchaseConfirmationFailed(FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in failed confirmation.");
            }

            Debug.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                      $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                      + $"Confirmation Failure Details: {order.Details}");
        }

        Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().First()?.Product;
        }

        // Calling StoreController.Connect without a listener on the StoreController.OnStoreDisconnected event will result in warnings.
        void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.Log($"Store disconnected details: {description.message}");
        }

        // Calling StoreController.Connect without listeners on StoreController.OnProductsFetched and StoreController.OnProductsFetchedFailed will result in warnings.
        void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"Products fetched successfully for {products.Count} products.");
        }

        void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            Debug.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
        }

        void Add2000Crystal()
        {
            GameManager.Instance.Crystal += 2000;
            SaveManager.Instance?.Save();
            Debug.Log("PurchaseManager: Added 2000 crystals.");
        }

        void Add10000Crystal()
        {
            GameManager.Instance.Crystal += 10000;
            SaveManager.Instance?.Save();
            Debug.Log("PurchaseManager: Added 10000 crystals.");
        }
    }
}
