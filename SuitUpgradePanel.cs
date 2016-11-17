using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SuitUpgradePanel : MonoBehaviour
{
    public GameObject Panel;
    public MethodInfo methoddata;
    public GameObject[,] Suit_Slot_Colliders;
    public UISprite[,] Suit_Slots_Blocks;
    public UISprite[,] Suit_Slots_Highlights;
    public UILabel SuitPowerLabel;
    public Vector3 mStartPos;
    public bool dirty = false;
    private int dragSourceX = -1;
    private int dragSourceY = -1;
    private string mHoverButton;
    public static SuitUpgradePanel instance;
    public PlayerSuitInventory SuitInventory;

    void Start()
    {
        instance = this;
        this.Panel = GameObject.Find("UI Root (2D)").transform.Search("Suit_Upgrade_Panel").gameObject;
        this.methoddata = InventoryPanelScript.instance.GetType().GetMethod("RepositionInventory", BindingFlags.NonPublic | BindingFlags.Instance);
        this.Suit_Slot_Colliders = new GameObject[4, 4];
        this.Suit_Slots_Blocks = new UISprite[4, 4];
        this.Suit_Slots_Highlights = new UISprite[4, 4];
        GameObject gameObject = FindChildren.FindChild(this.Panel, "Suit_Slot1_Collider");
        gameObject.AddComponent<SuitPanelIconScript>();
        UISprite component = FindChildren.FindChild(this.Panel, "Suit_Slot1_Block").GetComponent<UISprite>();
        UISprite component2 = FindChildren.FindChild(this.Panel, "Suit_Slot1_Highlight").GetComponent<UISprite>();

        UILabel component3 = FindChildren.FindChild(this.Panel, "Suit_Power_Label").GetComponent<UILabel>();
        UILabel component6 = FindChildren.FindChild(this.Panel, "Suit_Radius_Label").GetComponent<UILabel>();

        component6.gameObject.SetActive(false);
        this.SuitPowerLabel = component3;

        this.Suit_Slot_Colliders[0, 0] = gameObject;
        this.Suit_Slots_Blocks[0, 0] = component;
        this.Suit_Slots_Highlights[0, 0] = component2;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (i != 0 || j != 0)
                {
                    int num = 1 + i + j * 10;
                    Vector3 b = new Vector3((float)(82 * i), (float)(-79 * j), 0f);
                    GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(gameObject, gameObject.transform.position + b, gameObject.transform.rotation);
                    gameObject2.transform.parent = gameObject.transform.parent;
                    gameObject2.transform.localScale = gameObject.transform.localScale;
                    gameObject2.transform.localPosition = gameObject.transform.localPosition + b;
                    gameObject2.name = string.Format("Suit_Slot{0}_Collider", num);
                    gameObject2.AddComponent<SuitPanelIconScript>();
                    this.Suit_Slot_Colliders[i, j] = gameObject2;
                    UISprite component4 = ((GameObject)UnityEngine.Object.Instantiate(component.gameObject, component.transform.position + b, component.transform.rotation)).GetComponent<UISprite>();
                    component4.transform.parent = component.transform.parent;
                    component4.transform.localScale = component.transform.localScale;
                    component4.transform.localPosition = component.transform.localPosition + b;
                    component4.name = string.Format("Suit_Slot{0}_Block", num);
                    this.Suit_Slots_Blocks[i, j] = component4;
                    UISprite component5 = ((GameObject)UnityEngine.Object.Instantiate(component2.gameObject, component2.transform.position + b, component2.transform.rotation)).GetComponent<UISprite>();
                    component5.transform.parent = component2.transform.parent;
                    component5.transform.localScale = component2.transform.localScale;
                    component5.transform.localPosition = component2.transform.localPosition + b;
                    component5.name = string.Format("Suit_Slot{0}_Highlight", num);
                    this.Suit_Slots_Highlights[i, j] = component5;
                }
            }
        }
        this.mStartPos = base.gameObject.transform.localPosition;
        this.Panel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O) && UIManager.instance.mInventoryPanel.isActiveAndEnabled && this.Panel != null && !GenericMachinePanelScript.instance.isActiveAndEnabled)
        {
            if (!this.Panel.activeSelf)
            {
                this.Show();
                this.dirty = true;
            }
            else
                this.Hide();
        }
        else if (GenericMachinePanelScript.instance.isActiveAndEnabled && UIManager.instance.mInventoryPanel.isActiveAndEnabled)
            this.Panel.SetActive(false);

        if (this.dirty)
            this.UpdateDisplay();
        if (this.SuitInventory == null && SurvivalHotBarManager.instance != null)
        {
            this.SuitInventory = new PlayerSuitInventory(WorldScript.mLocalPlayer);
            this.SuitInventory.RequestLoad();
        }
        if (this.SuitInventory != null && this.SuitInventory.mFile.LoadStatus == FCFileLoadStatus.DoesNotExist)
        {
            this.SuitInventory.RequestLoad();
        }
        else if (this.SuitInventory != null && this.SuitInventory.mFile.LoadStatus == FCFileLoadStatus.Loaded)
        {
            this.SuitInventory.mFile.MarkReady();
            WorldScript.mLocalPlayer.mInventory.VerifySuitUpgrades();
        }
    }

    private void Show()
    {
        this.Panel.SetActive(true);
        methoddata.Invoke(InventoryPanelScript.instance, new object[] { true });
        Debug.Log("SuitUpgradePanel Inventory file load status: " + this.SuitInventory.mFile.LoadStatus.ToString());
    }

    private void Hide()
    {
        this.Panel.SetActive(false);
        methoddata.Invoke(InventoryPanelScript.instance, new object[] { false });
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemAt = this.SuitInventory.GetItemAt(i, j);
                UISprite uISprite = this.Suit_Slots_Blocks[i, j];
                UISprite uISprite2 = this.Suit_Slots_Highlights[i, j];
                if (itemAt == null)
                {
                    uISprite.gameObject.SetActive(false);
                    uISprite2.gameObject.SetActive(false);
                }
                else
                {
                    string spriteName;
                    int num;
                    Color color;
                    Util.GetIconAndCount(itemAt, out spriteName, out num, out color);
                    uISprite.gameObject.SetActive(true);
                    uISprite.color = color;
                    Util.SetNGUIIcon(uISprite, spriteName);
                    uISprite2.gameObject.SetActive(false);
                }
            }
        }
        this.SuitPowerLabel.text = SurvivalPowerPanel.mrSuitPower.ToString() + "/" + SurvivalPowerPanel.mrMaxSuitPower.ToString();

        //if (UICamera.hoveredObject != null)
        //    this.SuitPowerLabel.text = UICamera.hoveredObject.name;
        //this.dirty = false;
    }

    public ItemBase GetItem(string name)
    {
        if (name.StartsWith("Suit_Slot"))
        {
            int num = int.Parse(name.Substring("Suit_Slot".Length, name.Length - "Suit_Slot".Length - "_Collider".Length)) - 1;
            int x = num % 10;
            int y = num / 10;
            ItemBase itemAt = this.SuitInventory.GetItemAt(x, y);
            this.dragSourceX = x;
            this.dragSourceY = y;
            return itemAt;
        }
        return null;
    }

    public bool RemoveItem(string name, ItemBase originalItem, ItemBase swapitem)
    {
        //if (swapitem != null)
        //{
        //    Debug.Log("Swap Item: " + ItemManager.GetItemName(swapitem));  //Remove?
        //}
        if (name.StartsWith("Suit_Slot"))
        {
            int num = int.Parse(name.Substring("Suit_Slot".Length, name.Length - "Suit_Slot".Length - "_Collider".Length)) - 1;
            int x = num % 10;
            int y = num / 10;
            this.SuitInventory.RemoveItemAt(x, y);
            if (swapitem != null)
            {
                this.SuitInventory.AddItemAt(x, y, swapitem);
            }
            this.dragSourceX = -1;
            this.dragSourceY = -1;
            this.dirty = true;
            this.SuitInventory.MarkDirty();
            return true;
        }
        Debug.Log("Failed to remove item, target name '" + name + "' unknown!");
        return false;
    }

    public void HandleItemDrag(string name, ItemBase draggedItem, DragAndDropManager.DragRemoveItem dragDelegate)
    {
        //Debug.Log("Handle Item Drag: " + ItemManager.GetItemName(draggedItem));
        if (this.SuitInventory.ValidItems.Contains(draggedItem.mnItemID))
        {
            if (name.StartsWith("Suit_Slot"))
            {
                int num = int.Parse(name.Substring("Suit_Slot".Length, name.Length - "Suit_Slot".Length - "_Collider".Length)) - 1;
                int num2 = num % 10;
                int num3 = num / 10;
                ItemBase itemAt = this.SuitInventory.GetItemAt(num2, num3);
                if (this.dragSourceX >= 0 && this.dragSourceY >= 0 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    bool flag = true;
                    int currentStackSize = ItemManager.GetCurrentStackSize(draggedItem);
                    if (currentStackSize < 2)
                    {
                        flag = false;
                    }
                    if (itemAt != null)
                    {
                        if (itemAt.mnItemID != draggedItem.mnItemID)
                        {
                            flag = false;
                        }
                        else if (itemAt.mType == ItemType.ItemCubeStack)
                        {
                            ItemCubeStack itemCubeStack = itemAt as ItemCubeStack;
                            ItemCubeStack itemCubeStack2 = draggedItem as ItemCubeStack;
                            if (itemCubeStack.mCubeType != itemCubeStack2.mCubeType || itemCubeStack.mCubeValue != itemCubeStack2.mCubeValue)
                            {
                                flag = false;
                            }
                        }
                    }
                    if (flag)
                    {
                        UIManager.instance.mSplitPanel.Show(draggedItem, this.dragSourceX, this.dragSourceY, itemAt, num2, num3);
                        this.SuitInventory.MarkDirtyDelayed(5);
                        return;
                    }
                }
                if (itemAt != null)
                {
                    if (draggedItem.mType == ItemType.ItemCubeStack && itemAt.mType == ItemType.ItemCubeStack)
                    {
                        ItemCubeStack itemCubeStack3 = draggedItem as ItemCubeStack;
                        ItemCubeStack itemCubeStack4 = itemAt as ItemCubeStack;
                        if (itemCubeStack3.mCubeType == itemCubeStack4.mCubeType && itemCubeStack3.mCubeValue == itemCubeStack4.mCubeValue)
                        {
                            int maxStackSize = global::TerrainData.GetMaxStackSize(itemCubeStack4.mCubeType);
                            if (maxStackSize != 100)
                            {
                                if (itemCubeStack3.mnAmount + itemCubeStack4.mnAmount > maxStackSize)
                                {
                                    int num4 = maxStackSize - itemCubeStack4.mnAmount;
                                    itemCubeStack3.mnAmount -= num4;
                                    itemCubeStack4.mnAmount += num4;
                                    this.SuitInventory.MarkDirty();
                                    this.dirty = true;
                                    return;
                                }
                            }
                            if (dragDelegate(draggedItem, null))
                            {
                                itemCubeStack4.mnAmount += itemCubeStack3.mnAmount;
                                this.SuitInventory.MarkDirty();
                                this.dirty = true;
                                return;
                            }
                            return;
                        }
                    }
                    if (draggedItem.mType == ItemType.ItemStack && itemAt.mnItemID == draggedItem.mnItemID)
                    {
                        ItemStack itemStack = draggedItem as ItemStack;
                        ItemStack itemStack2 = itemAt as ItemStack;
                        if (dragDelegate(draggedItem, null))
                        {
                            itemStack2.mnAmount += itemStack.mnAmount;
                            this.SuitInventory.MarkEverythingDirty();
                            this.dirty = true;
                            return;
                        }
                        return;
                    }
                }
                if (dragDelegate(draggedItem, itemAt))
                {
                    if (itemAt != null)
                    {
                        this.SuitInventory.RemoveItemAt(num2, num3);
                    }
                    this.SuitInventory.AddItemAt(num2, num3, draggedItem);
                    this.SuitInventory.MarkEverythingDirty();
                    this.dirty = true;
                }
                return;
            }
        }
    }

    public bool ButtonClicked(string name)
    {
        return false;
    }

    public bool ButtonMiddleClicked(string name)
    {
        return false;
    }

    public void ButtonEnter(string name)
    {
        this.mHoverButton = name;
        if (name.StartsWith("Suit_Slot"))
        {
            int num = int.Parse(name.Substring("Suit_Slot".Length, name.Length - "Suit_Slot".Length - "_Collider".Length)) - 1;
            int x = num % 10;
            int y = num / 10;
            string text = string.Empty;
            ItemBase itemAt = this.SuitInventory.GetItemAt(x, y);
            if (itemAt != null)
            {
                if (HotBarManager.mbInited)
                {
                    text = ItemManager.GetItemName(itemAt);
                    HotBarManager.SetCurrentBlockLabel(text);
                }
                else if (SurvivalHotBarManager.mbInited)
                {
                    if (WorldScript.mLocalPlayer.mResearch.IsKnown(itemAt))
                    {
                        text = ItemManager.GetItemName(itemAt);
                    }
                    else
                    {
                        text = "Unknown Material";
                    }
                    int currentStackSize = ItemManager.GetCurrentStackSize(itemAt);
                    if (currentStackSize > 1)
                    {
                        SurvivalHotBarManager.SetCurrentBlockLabel(string.Format("{0} {1}", currentStackSize, text));
                    }
                    else
                    {
                        SurvivalHotBarManager.SetCurrentBlockLabel(text);
                    }
                }
            }
        }
    }

    public void ButtonExit(string name)
    {
        if (this.mHoverButton == name)
        {
            this.mHoverButton = null;
        }
    }
}
