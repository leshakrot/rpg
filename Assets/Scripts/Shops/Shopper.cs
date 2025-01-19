using System;
using UnityEngine;

namespace RPG.Shops
{
    public class Shopper : MonoBehaviour
    {
        Shop activeShop = null;

        public event Action activeShopChange;

        public void SetActiveShop(Shop shop)
        {
            activeShop = shop;
            if(activeShopChange != null)
            {
                activeShopChange();
            }
        }

        internal Shop GetActiveShop()
        {
            return activeShop;
        }
    }
}
