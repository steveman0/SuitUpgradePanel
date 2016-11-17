using System;
using UnityEngine;

public class SuitPanelIconScript : MonoBehaviour
{
    public SuitUpgradePanel target = SuitUpgradePanel.instance;

    private bool mbHover;

    private GameObject cachedGameObject;

    private void Start()
    {
        this.cachedGameObject = base.gameObject;
    }

    private void OnRelease()
    {
        this.OnPress(false);
    }

    private bool AttemptToStackIntoCrate(StorageCrate lCrate, ItemBase lItem)
    {
        for (int i = 0; i < lCrate.mItems.Length; i++)
        {
            if (lCrate.mItems[i] != null)
            {
                if (ItemManager.AreItemsTheSame(lItem, lCrate.mItems[i]))
                {
                    ItemBase itemBase = ItemManager.CloneItem(lItem);
                    if (ItemManager.StackWholeItems(itemBase, lCrate.mItems[i], true))
                    {
                        int currentStackSize = ItemManager.GetCurrentStackSize(lCrate.mItems[i]);
                        int currentStackSize2 = ItemManager.GetCurrentStackSize(itemBase);
                        StorageCrateWindow.SetSlotAndSendNetworkUpdate(WorldScript.mLocalPlayer, lCrate, i, itemBase);
                        int num = currentStackSize2 - currentStackSize;
                        this.RemoveItem(lItem, null);
                        UIManager.instance.SetInfoText(string.Concat(new object[]
                        {
                            "Stacked ",
                            num,
                            "x ",
                            ItemManager.GetItemName(itemBase)
                        }), 3f, true);
                        return true;
                    }
                    return false;
                }
            }
        }
        return false;
    }

    private bool AddItemToEmptySlotInCrate(StorageCrate lCrate, ItemBase lItem)
    {
        int i = 0;
        while (i < lCrate.mItems.Length)
        {
            if (lCrate.mItems[i] == null)
            {
                int maxStackSize = ItemManager.GetMaxStackSize(lItem);
                int currentStackSize = ItemManager.GetCurrentStackSize(lItem);
                if (currentStackSize > maxStackSize)
                {
                    ItemBase item = ItemManager.CloneItem(lItem);
                    ItemManager.SetItemCount(lItem, currentStackSize - maxStackSize);
                    ItemManager.SetItemCount(item, maxStackSize);
                    StorageCrateWindow.SetSlotAndSendNetworkUpdate(WorldScript.mLocalPlayer, lCrate, i, item);
                    return true;
                }
                StorageCrateWindow.SetSlotAndSendNetworkUpdate(WorldScript.mLocalPlayer, lCrate, i, lItem);
                this.RemoveItem(lItem, null);
                return true;
            }
            else
            {
                i++;
            }
        }
        return false;
    }

    private bool DoCtrlClick(bool isPressed)
    {
        if (!isPressed)
        {
            return false;
        }
        ItemBase item = this.target.GetItem(this.cachedGameObject.name);
        if (item == null)
        {
            Debug.LogWarning("User shift-clicked on empty inventory slot!");
            return false;
        }
        Debug.LogWarning("User is attempting to auto-remove  " + item.ToString());
        if (UIManager.instance.mGenericMachinePanel.currentWindow == null)
        {
            Debug.LogWarning("No window open for shift-click action!");
            return false;
        }
        if (!(UIManager.instance.mGenericMachinePanel.currentWindow is StorageCrateWindow))
        {
            Debug.LogWarning("We couldn't convert this to a StorageCrateWindow" + UIManager.instance.mGenericMachinePanel.currentWindow.ToString());
            return false;
        }
        StorageCrateWindow storageCrateWindow = UIManager.instance.mGenericMachinePanel.currentWindow as StorageCrateWindow;
        Debug.LogWarning("Got an SCW ok!");
        if (item.mType == ItemType.ItemCubeStack || item.mType == ItemType.ItemStack)
        {
            if (this.AttemptToStackIntoCrate(storageCrateWindow.mCrateCentre, item))
            {
                return true;
            }
            for (int i = 0; i < storageCrateWindow.mCrateCentre.mConnectedCrates.Count; i++)
            {
                StorageCrate lCrate = storageCrateWindow.mCrateCentre.mConnectedCrates[i];
                if (this.AttemptToStackIntoCrate(lCrate, item))
                {
                    return true;
                }
            }
            if (this.AddItemToEmptySlotInCrate(storageCrateWindow.mCrateCentre, item))
            {
                UIManager.instance.SetInfoText("Stored " + item.ToString() + " in empty slot", 3f, true);
                return true;
            }
            for (int j = 0; j < storageCrateWindow.mCrateCentre.mConnectedCrates.Count; j++)
            {
                StorageCrate lCrate2 = storageCrateWindow.mCrateCentre.mConnectedCrates[j];
                if (this.AddItemToEmptySlotInCrate(lCrate2, item))
                {
                    UIManager.instance.SetInfoText("Stored " + item.ToString() + " in empty slot", 3f, true);
                    return true;
                }
            }
        }
        else
        {
            if (this.AddItemToEmptySlotInCrate(storageCrateWindow.mCrateCentre, item))
            {
                UIManager.instance.SetInfoText("Stored " + item.ToString() + " in empty slot", 3f, true);
                return true;
            }
            for (int k = 0; k < storageCrateWindow.mCrateCentre.mConnectedCrates.Count; k++)
            {
                StorageCrate lCrate3 = storageCrateWindow.mCrateCentre.mConnectedCrates[k];
                if (this.AddItemToEmptySlotInCrate(lCrate3, item))
                {
                    UIManager.instance.SetInfoText("Stored " + item.ToString() + " in empty slot", 3f, true);
                    return true;
                }
            }
        }
        return false;
    }

    private void OnPress(bool isPressed)
    {
        if (UIManager.instance.mSplitPanel.gameObject.activeSelf)
        {
            return;
        }
        if (this.mbHover)
        {
            if (isPressed && Input.GetKey(KeyCode.LeftControl) && !DragAndDropManager.mbDragging && this.DoCtrlClick(isPressed))
            {
                InventoryPanelScript.mbDirty = true;
                return;
            }
            if (isPressed)
            {
                if (!DragAndDropManager.mbDragging)
                {
                    ItemBase item = this.target.GetItem(this.cachedGameObject.name);
                    if (item != null)
                    {
                        DragAndDropManager.instance.StartDrag(this.cachedGameObject.name, item, new DragAndDropManager.DragRemoveItem(this.RemoveItem));
                    }
                }
            }
            else
            {
                if (DragAndDropManager.instance.CheckClick(this.cachedGameObject.name))
                {
                    bool flag = false;
                    if (UICamera.currentTouchID == -1)
                    {
                        flag = this.target.ButtonClicked(this.cachedGameObject.name);
                    }
                    if (UICamera.currentTouchID == -3)
                    {
                        flag = this.target.ButtonMiddleClicked(this.cachedGameObject.name);
                    }
                    if (flag)
                    {
                        DragAndDropManager.instance.CancelDrag();
                    }
                }
                ItemBase draggedItem;
                DragAndDropManager.DragRemoveItem dragDelegate;
                if (DragAndDropManager.instance.CheckDragEnd(this.cachedGameObject.name, out draggedItem, out dragDelegate))
                {
                    this.target.HandleItemDrag(this.cachedGameObject.name, draggedItem, dragDelegate);
                }
            }
        }
    }

    private bool RemoveItem(ItemBase originalItem, ItemBase swapitem)
    {
        return this.target.RemoveItem(this.cachedGameObject.name, originalItem, swapitem);
    }

    private void OnHover(bool isOver)
    {
        this.mbHover = isOver;
        if (isOver)
        {
            this.target.ButtonEnter(this.cachedGameObject.name);
        }
        else
        {
            this.target.ButtonExit(this.cachedGameObject.name);
        }
    }
}
