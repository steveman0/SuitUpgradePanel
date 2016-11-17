using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using UnityEngine;

public class PlayerSuitInventory
{
    public const string InterfaceName = "SuitInventoryWindow";
    public const string InterfaceAddItem= "AddItem";
    public const string InterfaceRemoveItem = "RemoveItem";

    public const string INVENTORY_ITEM_FILE = "SuitInventory_Items.dat";
    public const string INVENTORY_OVERFLOW_FILE = "SuitInventory_Overflow.dat";
    public const int INVENTORY_VERSION = 1;
    public const int OVERFLOW_VERSION = 0;
    public const int MAX_INVENTORY_WRITE_FAILS = 10;
    public const int mnInventoryX = 10;
    public const int mnInventoryY = 8;
    public bool mbFirstTimeInSurvival;
    public Player mPlayer;
    public ItemBase[,] maItemInventory;
    public List<ItemBase> maSuitUpgrades;
    public List<ItemBase> maArtherUpgrades;
    public List<ItemBase> maItemOverflow;
    public static bool mbPlayerHasMK2BuildGun;
    public static bool mbPlayerHasMK3BuildGun;
    public bool mbReady;
    public IFCFileHandler mFile;
    public int FreeSpace = -1;
    private int mnUpdates;
    private bool JustLoaded;
    public List<int> ValidItems = new List<int>();

    public PlayerSuitInventory(Player player)
    {
        this.mPlayer = player;
        this.maItemInventory = new ItemBase[4, 4];
        this.maItemOverflow = new List<ItemBase>();
        if (WorldScript.mbIsServer)
        {
            string baseFileName = this.mPlayer.mSettings.GetPlayerDirectory() + Path.DirectorySeparatorChar + INVENTORY_ITEM_FILE;
            //UnityEngine.Debug.Log("PlayerSuitInventory file name: " + baseFileName);
            if (WorldScript.mbIsServer)
            {
                this.mFile = WorldScript.instance.mDiskThread.RegisterManagedFile(new ManagedFileSaveMethod(this.WriteFileData), new ManagedFileLoadMethod(this.ReadFileData), new ManagedFileConversionMethod(this.ConvertPlayerDirectory), baseFileName);
            }
        }
        for (int n = 0; n < ItemEntry.mEntries.Length; n++)
        {
            if (ItemEntry.mEntries[n] == null) continue;
            if (ItemEntry.mEntries[n].Category == MaterialCategories.SuitUpgrade || ItemEntry.mEntries[n].Category == MaterialCategories.ArtherUpgrade) 
                this.ValidItems.Add(ItemEntry.mEntries[n].ItemID);
        }

        UnityEngine.Debug.Log("Player Suit Inventory initialised");
    }

    public int CountFreeSlots()
    {
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (this.maItemInventory[i, j] == null)
                {
                    num++;
                }
            }
        }
        this.FreeSpace = num;
        return this.FreeSpace;
    }

    public void MarkDirty()
    {
        if (this.mFile != null)
        {
            this.mFile.MarkDirty();
            UnityEngine.Debug.Log("PlayerSuitInventory file was marked dirty " + this.mFile.LoadStatus.ToString());
        }
    }

    public void MarkDirtyDelayed(int delaySeconds)
    {
        if (this.mFile != null)
        {
            this.mFile.MarkDirty(delaySeconds);
        }
    }

    public void MarkClientReady()
    {
        if (WorldScript.mbIsServer)
        {
            throw new AssertException("PlayerSuitInventory MarkClientReady should not be called on the server!");
        }
        this.mbReady = true;
    }

    private void ConvertPlayerDirectory()
    {
        this.mPlayer.mSettings.CheckAndCreatePlayerDirectory();
    }

    public void RequestLoad()
    {
        if (!WorldScript.mbIsServer)
        {
            throw new AssertException("PlayerSuitInventory RequestLoad should not be called on a client!");
        }
        string baseFileName = this.mPlayer.mSettings.GetPlayerDirectory() + Path.DirectorySeparatorChar + INVENTORY_ITEM_FILE;
        if (File.Exists(baseFileName))
        {
            this.mFile.RequestLoad();
            this.JustLoaded = true;
        }
        else
            this.mFile.MarkReady();
    }

    public bool CheckReady()
    {
        if (!WorldScript.mbIsServer)
        {
            return this.mbReady;
        }
        if (this.mbReady)
        {
            return true;
        }
        this.mbReady = (this.mFile.LoadStatus != FCFileLoadStatus.NotLoaded);
        if (this.mbReady)
        {
            this.mFile.MarkReady();
        }
        return this.mbReady;
    }

    private FCFileLoadAttemptResult ReadFileData(BinaryReader reader, bool isBackup)
    {
        int num = reader.ReadInt32();
        if (num != 1)
        {
            throw new AssertException("Inventory file version does not match");
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                this.maItemInventory[i, j] = ItemFile.DeserialiseItem(reader);
            }
        }
        for (int k = 0; k < 4; k++)
        {
            for (int l = 0; l < 4; l++)
            {
                if (this.maItemInventory[k, l] != null)
                {
                    if (this.maItemInventory[k, l].mType == ItemType.ItemCubeStack && (this.maItemInventory[k, l] as ItemCubeStack).mCubeType == 24)
                    {
                        (this.maItemInventory[k, l] as ItemCubeStack).mCubeType = 199;
                        (this.maItemInventory[k, l] as ItemCubeStack).mCubeValue = global::TerrainData.GetDefaultValue(199);
                    }
                }
            }
        }
        if (num == 2)
        {
        }
        return FCFileLoadAttemptResult.Successful;
    }

    private bool WriteFileData(BinaryWriter writer)
    {
        writer.Write(1);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemFile.SerialiseItem(this.maItemInventory[i, j], writer);
            }
        }
        return true;
    }

    public void CleanUp()
    {
        if (WorldScript.mbIsServer)
        {
            this.mFile.MarkDirty();
            this.mFile.FlagForDeregistration();
        }
    }

    public void VerifySuitUpgrades()
    {
        if (!GameState.GameStarted)
        {
            return;
        }
        if (WorldScript.instance == null)
        {
            return;
        }
        if (WorldScript.instance.localPlayerInstance == null)
        {
            return;
        }
        if (!this.mPlayer.mbIsLocalPlayer)
        {
            return;
        }
        //SurvivalPowerPanel.ResetUpgrades();   // Overrides player inventory!!
        if (this.GetItemCount(1000) > 0 || this.GetItemCount(1050) > 0)
        {
            SurvivalPowerPanel.mrSolarEfficiency = 2f;
        }
        if (this.GetItemCount(1002) > 0 || this.GetItemCount(1050) > 0)
        {
            SurvivalHazardPanel.instance.mrSuitHeaterEfficiency = 6f;
            SurvivalHazardPanel.instance.mrSuitHeaterPower = 3f;
            SurvivalHazardPanel.instance.HasSuitHeater = true;
        }
        //else //Overriding player inventory
        //{
        //    SurvivalHazardPanel.instance.HasSuitHeater = false;
        //}
        if (this.GetItemCount(1003) > 0 || this.GetItemCount(1050) > 0)
        {
            SurvivalPowerPanel.mrHeadLightEfficiency = 2f;
        }
        float mrMaxSuitPower = SurvivalPowerPanel.mrMaxSuitPower;
        if (this.GetItemCount(1004) > 0 && mrMaxSuitPower <= 512)
        {
            mrMaxSuitPower = 512f;
        }
        //else
        //{
        //    mrMaxSuitPower = 256f;
        //}
        if (this.GetItemCount(1051) > 0 || this.GetItemCount(1050) > 0)
        {
            mrMaxSuitPower = 1024f;
        }
        SurvivalPowerPanel.mrMaxSuitPower = mrMaxSuitPower;
        if (this.JustLoaded)
        {
            SurvivalPowerPanel.mrSuitPower = mrMaxSuitPower;
            this.JustLoaded = false;
        }
        if (this.GetItemCount(1005) > 0)
        {
        }
        if (this.GetItemCount(1006) > 0)
        {
        }
        if (this.GetItemCount(1007) > 0)
        {
            SurvivalHazardPanel.instance.mrSuitInsulation = 40f;
        }
        //As before, overriding local upgrades as necessary
        //ARTHERPetSurvival.instance.mbHatEquipped = false;
        //ARTHERPetSurvival.instance.mbMonocleEquipped = false;
        //ARTHERPetSurvival.instance.mbMoustacheEquipped = false;
        //ARTHERPetSurvival.instance.mbSolarModuleEquipped = false;
        bool mbMovementModuleFound = false;
        if (this.GetItemCount(2000) > 0)
        {
            mbMovementModuleFound = true;
        }
        if (this.GetItemCount(2001) > 0)
        {
            ARTHERPetSurvival.instance.mbSolarModuleEquipped = true;
        }
        if (this.GetItemCount(2002) > 0)
        {
            ARTHERPetSurvival.instance.AttachBattery();
        }
        if (this.GetItemCount(2003) > 0)
        {
            mbMovementModuleFound = true;
            ARTHERPetSurvival.instance.mbSolarModuleEquipped = true;
            ARTHERPetSurvival.instance.AttachBattery();
        }
        if (DLCOwnership.HasAmpuTea() || DLCOwnership.HasDapper())
        {
            if (this.GetItemCount(2004) > 0)
            {
                ARTHERPetSurvival.instance.mbHatEquipped = true;
            }
            if (this.GetItemCount(2005) > 0)
            {
                ARTHERPetSurvival.instance.mbMonocleEquipped = true;
            }
            if (this.GetItemCount(2006) > 0)
            {
                ARTHERPetSurvival.instance.mbMoustacheEquipped = true;
            }
        }
        if (this.GetItemCount(2007) > 0)
        {
            ARTHERPetSurvival.instance.mbHatEquipped = true;
            ARTHERPetSurvival.instance.mbMonocleEquipped = true;
            ARTHERPetSurvival.instance.mbMoustacheEquipped = true;
        }
        if (this.GetItemCount(2008) > 0)
        {
            ARTHERPetSurvival.instance.mbHatEquipped = true;
            ARTHERPetSurvival.instance.mbMonocleEquipped = true;
            ARTHERPetSurvival.instance.mbMoustacheEquipped = true;
            mbMovementModuleFound = true;
            ARTHERPetSurvival.instance.mbSolarModuleEquipped = true;
            ARTHERPetSurvival.instance.AttachBattery();
        }
        if (!ARTHERPetSurvival.instance.mbMovementModuleFound)
            ARTHERPetSurvival.instance.mbMovementModuleFound = mbMovementModuleFound;
        int num = 0;
        if (this.GetItemCount(1008) > 0)
        {
            num = 1;
        }
        if (this.GetItemCount(1011) > 0)
        {
            num = 2;
        }
        bool flag = PlayerInventory.mbPlayerHasMK2BuildGun;
        bool flag2 = PlayerInventory.mbPlayerHasMK3BuildGun;
        switch (num)
        {
            case 0:
                SurvivalDigScript.mnHardnessLimit = 150;
                if (!PlayerInventory.mbPlayerHasMK2BuildGun && !PlayerInventory.mbPlayerHasMK3BuildGun)
                {
                    PlayerInventory.mbPlayerHasMK2BuildGun = false;
                    PlayerInventory.mbPlayerHasMK3BuildGun = false;
                }
                break;
            case 1:
                SurvivalDigScript.mnHardnessLimit = 250;
                if (!PlayerInventory.mbPlayerHasMK3BuildGun)
                {
                    PlayerInventory.mbPlayerHasMK2BuildGun = true;
                    PlayerInventory.mbPlayerHasMK3BuildGun = false;
                }
                break;
            case 2:
                SurvivalDigScript.mnHardnessLimit = 451;
                PlayerInventory.mbPlayerHasMK2BuildGun = false;
                PlayerInventory.mbPlayerHasMK3BuildGun = true;
                break;
        }
        if (!WorldScript.mbIsServer && (flag != PlayerSuitInventory.mbPlayerHasMK2BuildGun || flag2 != PlayerSuitInventory.mbPlayerHasMK3BuildGun))
        {
            NetworkManager.instance.mClientThread.mbSendSelectionChange = true;
        }
        if (this.GetItemCount(1009) > 0)
        {
            WorldScript.instance.localPlayerInstance.mbHaveJetPack = true;
        }
        //else
        //{
        //    WorldScript.instance.localPlayerInstance.mbHaveJetPack = false;
        //}
        if (this.GetItemCount(1010) > 0 || this.GetItemCount(1050) > 0)
        {
            WorldScript.instance.localPlayerInstance.mbHasToxicFilter = true;
        }
        //else
        //{
        //    WorldScript.instance.localPlayerInstance.mbHasToxicFilter = false;
        //}
        //ModManager.instance.VerifySuitUpgrades(this.mPlayer); //We're here because it was already called... this would cause an infinite loop!
    }

    public ItemBase ReduceCubeStackByExample(ItemBase lItem)
    {
        ItemCubeStack itemCubeStack = lItem as ItemCubeStack;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null && itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack2 = itemBase as ItemCubeStack;
                    if (itemCubeStack2.mnAmount > 0)
                    {
                        if (itemCubeStack.mCubeType == itemCubeStack2.mCubeType && itemCubeStack.mCubeValue == itemCubeStack2.mCubeValue)
                        {
                            itemCubeStack2.mnAmount--;
                            ItemBase itemBase2;
                            if (itemCubeStack2.mnAmount <= 0)
                            {
                                this.maItemInventory[i, j] = null;
                                itemCubeStack2.mnAmount = 1;
                                itemBase2 = itemCubeStack2;
                            }
                            else
                            {
                                itemBase2 = ItemManager.CloneItem(itemBase);
                                (itemBase2 as ItemCubeStack).mnAmount = 1;
                            }
                            this.MarkEverythingDirty();
                            return itemBase2;
                        }
                    }
                }
            }
        }
        UnityEngine.Debug.Log("Did NOT find CubeStack " + ItemManager.GetItemName(lItem));
        return null;
    }

    public ItemBase TryAndGetItem(int lnItemID)
    {
        for (int i = 0; i < 4; i++)
        {
            int j = 0;
            while (j < 4)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase == null || itemBase.mnItemID != lnItemID)
                {
                    j++;
                }
                else
                {
                    if (itemBase.mType == ItemType.ItemCubeStack)
                    {
                        UnityEngine.Debug.Log("Do not attempt to drop itemcubes yet please!");
                        return null;
                    }
                    if (itemBase.mType == ItemType.ItemStack)
                    {
                        ItemStack itemStack = itemBase as ItemStack;
                        itemStack.mnAmount--;
                        if (itemStack.mnAmount <= 0)
                        {
                            this.maItemInventory[i, j] = null;
                        }
                        ItemBase result = ItemManager.SpawnItem(itemBase.mnItemID);
                        this.MarkEverythingDirty();
                        return result;
                    }
                    ItemBase result2 = itemBase;
                    this.MarkEverythingDirty();
                    return result2;
                }
            }
        }
        UnityEngine.Debug.Log("Error, asked to drop item, but item didn't exist in inventory");
        return null;
    }

    private void WriteItemOverflow()
    {
        string text = this.mPlayer.mSettings.CheckAndCreatePlayerDirectory();
        text = text + Path.DirectorySeparatorChar + INVENTORY_OVERFLOW_FILE;
        BinaryWriter binaryWriter = new BinaryWriter(new FileStream(text, FileMode.OpenOrCreate, FileAccess.Write));
        binaryWriter.Write(0);
        binaryWriter.Write(this.maItemOverflow.Count);
        for (int i = 0; i < this.maItemOverflow.Count; i++)
        {
            ItemFile.SerialiseItem(this.maItemOverflow[i], binaryWriter);
        }
        binaryWriter.Close();
    }

    //private void ReadOldCubes()
    //{
    //    string text = this.mPlayer.mSettings.CheckAndCreatePlayerDirectory();
    //    text = text + Path.DirectorySeparatorChar + "SurvivalInventory.dat";
    //    if (!File.Exists(text))
    //    {
    //        return;
    //    }
    //    UnityEngine.Debug.Log("Converting old cube inventory");
    //    BinaryReader binaryReader = new BinaryReader(new FileStream(text, FileMode.Open, FileAccess.Read));
    //    for (int i = 0; i < 10; i++)
    //    {
    //        for (int j = 0; j < 8; j++)
    //        {
    //            ushort cubeType = binaryReader.ReadUInt16();
    //            ushort cubeValue = binaryReader.ReadUInt16();
    //            int num = binaryReader.ReadInt32();
    //            if (num > 0)
    //            {
    //                ItemCubeStack itemCubeStack = ItemManager.SpawnCubeStack(cubeType, cubeValue, num);
    //                int num2 = this.FitItem(itemCubeStack);
    //                if (num2 < itemCubeStack.mnAmount)
    //                {
    //                    itemCubeStack.mnAmount -= num2;
    //                    this.maItemOverflow.Add(itemCubeStack);
    //                }
    //            }
    //        }
    //    }
    //    binaryReader.Close();
    //    UnityEngine.Debug.Log("Deleting old cube inventory file");
    //    File.Delete(text);
    //    if (this.maItemOverflow.Count > 0)
    //    {
    //        this.MarkDirty();
    //    }
    //}

    public bool AttemptToRemove(ushort lType, ushort lValue, int lnCount)
    {
        if (this.ContainsValue(lType, lValue, lnCount))
        {
            this.RemoveValue(lType, lValue, lnCount);
            return true;
        }
        return false;
    }

    public bool ContainsValue(ushort lType, ushort lValue, int lnCount)
    {
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null && itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                    if (itemCubeStack.mCubeType == lType && itemCubeStack.mCubeValue == lValue)
                    {
                        num += itemCubeStack.mnAmount;
                    }
                }
            }
        }
        return num >= lnCount;
    }

    public bool RemoveValue(ushort lType, ushort lValue, int lnCount)
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (lnCount == 0)
                {
                    break;
                }
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null && itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                    if (itemCubeStack.mCubeType == lType && itemCubeStack.mCubeValue == lValue)
                    {
                        if (itemCubeStack.mnAmount > 0)
                        {
                            int num = lnCount;
                            if (num > itemCubeStack.mnAmount)
                            {
                                num = itemCubeStack.mnAmount;
                            }
                            itemCubeStack.mnAmount -= num;
                            if (itemCubeStack.mnAmount == 0)
                            {
                                this.maItemInventory[i, j] = null;
                            }
                            lnCount -= num;
                        }
                        else
                        {
                            this.maItemInventory[i, j] = null;
                        }
                    }
                }
            }
        }
        this.MarkEverythingDirty();
        return lnCount == 0;
    }

    public void MarkEverythingDirty()
    {
        this.MarkDirty();
        HotBarManager.MarkAsDirty();
        SurvivalHotBarManager.MarkAsDirty();
        SurvivalHotBarManager.MarkContentDirty();
        SuitUpgradePanel.instance.dirty = true;
        WorldScript.mLocalPlayer.mInventory.VerifySuitUpgrades();
    }

    public bool CanFitCube(ushort cubeType, ushort cubeValue, int amount)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase == null)
                {
                    return true;
                }
                if (itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                    if (itemCubeStack.mCubeType == cubeType && itemCubeStack.mCubeValue == cubeValue)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool CanFit(ItemBase lItemToAdd)
    {
        if (lItemToAdd.mType == ItemType.ItemCubeStack)
        {
            ItemCubeStack itemCubeStack = lItemToAdd as ItemCubeStack;
            return this.CanFitCube(itemCubeStack.mCubeType, itemCubeStack.mCubeValue, itemCubeStack.mnAmount);
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase == null)
                {
                    return true;
                }
                if (itemBase.mnItemID == lItemToAdd.mnItemID)
                {
                    if (itemBase.mType == ItemType.ItemStack)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public int FitItem(ItemBase lItemToAdd)
    {
        if (lItemToAdd == null)
        {
            UnityEngine.Debug.Log("Error, attempting to add null ItemBase to Inventory?");
        }
        int num = 0;
        if (this.ValidItems.Contains(lItemToAdd.mnItemID))
        {
            if (lItemToAdd.mType == ItemType.ItemCubeStack)
            {
                ItemCubeStack itemCubeStack = lItemToAdd as ItemCubeStack;
                if (this.AddToCubeStack(itemCubeStack))
                {
                    num = itemCubeStack.mnAmount;
                }
                else if (this.StartNewStack(itemCubeStack))
                {
                    num = itemCubeStack.mnAmount;
                    this.AddToHotbar(itemCubeStack.mCubeType, itemCubeStack.mCubeValue);
                }
            }
            else if (lItemToAdd.mType == ItemType.ItemStack)
            {
                if (this.AddToStack(lItemToAdd))
                {
                    num = (lItemToAdd as ItemStack).mnAmount;
                }
                else if (this.StartNewStack(lItemToAdd))
                {
                    num = (lItemToAdd as ItemStack).mnAmount;
                    this.AddToHotbar(lItemToAdd.mnItemID);
                }
            }
            else if (this.StartNewStack(lItemToAdd))
            {
                num = 1;
                this.AddToHotbar(lItemToAdd.mnItemID);
            }
            if (num > 0)
            {
                this.MarkEverythingDirty();
            }
        }
        return num;
    }

    public bool AddItem(ItemBase lItemToAdd)
    {
        if (lItemToAdd == null)
        {
            UnityEngine.Debug.Log("Error, attempting to add null ItemBase to Inventory?");
        }
        if (this.ValidItems.Contains(lItemToAdd.mnItemID))
        {
            this.MarkDirtyDelayed(5);
            if (lItemToAdd.mType != ItemType.ItemCubeStack)
            {
                if (lItemToAdd.mType == ItemType.ItemStack)
                {
                    if (this.AddToStack(lItemToAdd))
                    {
                        this.MarkEverythingDirty();
                        return true;
                    }
                    if (!this.StartNewStack(lItemToAdd))
                    {
                        return false;
                    }
                    this.AddToHotbar(lItemToAdd.mnItemID);
                }
                else
                {
                    if (!this.StartNewStack(lItemToAdd))
                    {
                        return false;
                    }
                    this.AddToHotbar(lItemToAdd.mnItemID);
                }
                this.MarkEverythingDirty();
                return true;
            }
            ItemCubeStack itemCubeStack = lItemToAdd as ItemCubeStack;
            if (this.CollectValue(itemCubeStack.mCubeType, itemCubeStack.mCubeValue, itemCubeStack.mnAmount))
            {
                this.MarkEverythingDirty();
                return true;
            }
        }
        return false;
    }

    public void AddItemAt(int x, int y, ItemBase lItemToAdd)
    {
        //UnityEngine.Debug.Log("Attempting to add item at specific position!");
        PlayerSuitInventory.NetworkAddItem(WorldScript.mLocalPlayer, lItemToAdd, x + "," + y);
        if (lItemToAdd == null)
        {
            UnityEngine.Debug.Log("Error, attempting to add null ItemBase to Inventory?");
        }
        if (this.ValidItems.Contains(lItemToAdd.mnItemID))
        {
            this.MarkDirtyDelayed(5);
            ItemBase itemBase = this.maItemInventory[x, y];
            if (itemBase == null || this.FitItem(itemBase) == 0)
            {
            }
            this.maItemInventory[x, y] = lItemToAdd;
            SuitUpgradePanel.instance.dirty = true;
        }
    }

    public int GetCubeTypeCountValue(ushort lType, ushort lVal)
    {
        return this.GetCubeTypeCountValue(lType, lVal, false, false);
    }

    public int GetCubeTypeCountValue(ushort lType, ushort lVal, bool reverseSearch, bool oneStackOnly)
    {
        int num = 0;
        for (int i = 0; i < 10; i++)
        {
            int num2 = (!reverseSearch) ? i : (10 - i - 1);
            for (int j = 0; j < 8; j++)
            {
                int num3 = (!reverseSearch) ? j : (8 - j - 1);
                ItemBase itemBase = this.maItemInventory[num2, num3];
                if (itemBase != null && itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                    if (itemCubeStack.mCubeType == lType && itemCubeStack.mCubeValue == lVal)
                    {
                        num += itemCubeStack.mnAmount;
                    }
                    if (num > 0 && oneStackOnly)
                    {
                        return num;
                    }
                }
            }
        }
        return num;
    }

    public int GetCubeTypeCount(ushort lType)
    {
        ushort defaultValue = global::TerrainData.GetDefaultValue(lType);
        return this.GetCubeTypeCountValue(lType, defaultValue);
    }

    public int GetItemCount(int lnItemId)
    {
        return this.GetItemCount(lnItemId, false, false);
    }

    public int GetItemCount(int lnItemId, bool reverseSearch, bool oneStackOnly)
    {
        if (this.maItemInventory == null)
        {
            UnityEngine.Debug.LogError("Inventory is null during GetItemCount?");
        }
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            int num2 = (!reverseSearch) ? i : (4 - i - 1);
            for (int j = 0; j < 4; j++)
            {
                int num3 = (!reverseSearch) ? j : (4 - j - 1);
                ItemBase itemBase = this.maItemInventory[num2, num3];
                if (itemBase != null)
                {
                    if (itemBase.mnItemID == lnItemId)
                    {
                        if (itemBase.mType == ItemType.ItemStack)
                        {
                            num += (itemBase as ItemStack).mnAmount;
                        }
                        else if (itemBase.mType == ItemType.ItemCubeStack)
                        {
                            num += (itemBase as ItemCubeStack).mnAmount;
                        }
                        else
                        {
                            num++;
                        }
                    }
                    if (num > 0 && oneStackOnly)
                    {
                        return num;
                    }
                }
            }
        }
        return num;
    }

    public ItemBase FindFirstItem(int lnItemId)
    {
        if (this.maItemInventory == null)
        {
            UnityEngine.Debug.LogError("Inventory is null during FindFirstItem?");
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null)
                {
                    if (itemBase.mnItemID == lnItemId)
                    {
                        return itemBase;
                    }
                }
            }
        }
        return null;
    }

    public ItemBase FindFirstItem(ItemType type)
    {
        if (this.maItemInventory == null)
        {
            UnityEngine.Debug.LogError("Inventory is null during FindFirstItem?");
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null)
                {
                    if (itemBase.mType == type)
                    {
                        return itemBase;
                    }
                }
            }
        }
        return null;
    }

    public ItemBase GetItemAt(int x, int y)
    {
        if (this.maItemInventory == null)
        {
            UnityEngine.Debug.LogError("Inventory is null during GetItemAt?");
        }
        return this.maItemInventory[x, y];
    }

    public bool RemoveItemCubeStackByExample(ItemCubeStack item)
    {
        return this.RemoveItemCubeStackByExample(item, false);
    }

    public bool RemoveItemCubeStackByExample(ItemCubeStack item, bool reverseSearch)
    {
        int mnAmount = item.mnAmount;
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            int num2 = (!reverseSearch) ? i : (4 - i - 1);
            for (int j = 0; j < 4; j++)
            {
                int num3 = (!reverseSearch) ? j : (4 - j - 1);
                if (num == mnAmount)
                {
                    return true;
                }
                ItemBase itemBase = this.maItemInventory[num2, num3];
                if (itemBase != null)
                {
                    if (itemBase.mType == ItemType.ItemCubeStack)
                    {
                        ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                        if (itemCubeStack.mCubeType == item.mCubeType && itemCubeStack.mCubeValue == item.mCubeValue)
                        {
                            int num4 = mnAmount - num;
                            if (num4 > itemCubeStack.mnAmount)
                            {
                                num4 = itemCubeStack.mnAmount;
                            }
                            itemCubeStack.mnAmount -= num4;
                            if (itemCubeStack.mnAmount == 0)
                            {
                                this.maItemInventory[num2, num3] = null;
                            }
                            num += num4;
                        }
                    }
                }
            }
        }
        this.MarkEverythingDirty();
        return num == mnAmount;
    }

    public bool RemoveItemByExample(ItemBase item)
    {
        return this.RemoveItemByExample(item, false);
    }

    public bool RemoveItemByExample(ItemBase item, bool reverseSearch)
    {
        if (item.mType == ItemType.ItemCubeStack)
        {
            return this.RemoveItemCubeStackByExample(item as ItemCubeStack, reverseSearch);
        }
        int currentStackSize = ItemManager.GetCurrentStackSize(item);
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            int num2 = (!reverseSearch) ? i : (4 - i - 1);
            for (int j = 0; j < 4; j++)
            {
                int num3 = (!reverseSearch) ? j : (4 - j - 1);
                if (num == currentStackSize)
                {
                    return true;
                }
                ItemBase itemBase = this.maItemInventory[num2, num3];
                if (itemBase != null)
                {
                    if (itemBase.mnItemID == item.mnItemID)
                    {
                        if (itemBase.mType == ItemType.ItemStack)
                        {
                            ItemStack itemStack = itemBase as ItemStack;
                            int num4 = currentStackSize - num;
                            if (num4 > itemStack.mnAmount)
                            {
                                num4 = itemStack.mnAmount;
                            }
                            itemStack.mnAmount -= num4;
                            if (itemStack.mnAmount == 0)
                            {
                                this.maItemInventory[num2, num3] = null;
                            }
                            num += num4;
                        }
                        else
                        {
                            this.maItemInventory[num2, num3] = null;
                            num++;
                        }
                    }
                }
            }
        }
        this.MarkEverythingDirty();
        return num == currentStackSize;
    }

    public bool RemoveSpecificItem(ItemBase item)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (this.maItemInventory[i, j] == item)
                {
                    this.maItemInventory[i, j] = null;
                    this.MarkEverythingDirty();
                    return true;
                }
            }
        }
        return false;
    }

    public int RemoveItem(int lnItemId, int lnCount)
    {
        int num = lnCount;
        try
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    ItemBase itemBase = this.maItemInventory[i, j];
                    if (itemBase != null)
                    {
                        if (itemBase.mnItemID == lnItemId)
                        {
                            if (itemBase.mType == ItemType.ItemStack)
                            {
                                ItemStack itemStack = itemBase as ItemStack;
                                if (itemStack.mnAmount >= num)
                                {
                                    itemStack.mnAmount -= num;
                                    if (itemStack.mnAmount == 0)
                                    {
                                        this.maItemInventory[i, j] = null;
                                    }
                                    int result = lnCount;
                                    return result;
                                }
                                if (itemStack.mnAmount > 0)
                                {
                                    int num2 = num;
                                    if (num2 > itemStack.mnAmount)
                                    {
                                        num2 = itemStack.mnAmount;
                                    }
                                    num -= num2;
                                    itemStack.mnAmount -= num2;
                                    if (itemStack.mnAmount == 0)
                                    {
                                        this.maItemInventory[i, j] = null;
                                    }
                                    if (num == 0)
                                    {
                                        int result = lnCount;
                                        return result;
                                    }
                                }
                            }
                            else
                            {
                                this.maItemInventory[i, j] = null;
                                num--;
                                if (num == 0)
                                {
                                    int result = lnCount;
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            if (num < lnCount)
            {
                this.MarkDirty();
                SurvivalHotBarManager.MarkContentDirty();
                InventoryPanelScript.MarkDirty();
                SuitUpgradePanel.instance.dirty = true;
            }
        }
        if (num > 0)
        {
            UnityEngine.Debug.LogError(string.Concat(new object[]
            {
                "Error! Attempted to remove items from suit inventory, but had ",
                num,
                " of item ID ",
                lnItemId,
                " left to search for!"
            }));
        }
        return lnCount - num;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                this.RemoveItemAt(i, j);
            }
        }
        this.MarkEverythingDirty();
    }

    public void RemoveItemAt(int x, int y)
    {
        PlayerSuitInventory.NetworkRemoveItem(WorldScript.mLocalPlayer, x + "," + y);
        if (this.maItemInventory[x, y] != null)
        {
            this.maItemInventory[x, y] = null;
        }
        this.MarkEverythingDirty();
    }

    private bool AddToCubeStack(ItemCubeStack item)
    {
        if (item == null)
        {
            return false;
        }
        bool flag = false;
        int maxStackSize = global::TerrainData.GetMaxStackSize(item.mCubeType);
        if (maxStackSize != 100)
        {
            flag = true;
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null)
                {
                    if (itemBase.mType == ItemType.ItemCubeStack)
                    {
                        ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                        if (itemCubeStack.mCubeType == item.mCubeType && itemCubeStack.mCubeValue == item.mCubeValue)
                        {
                            if (flag && itemCubeStack.mnAmount + item.mnAmount > maxStackSize)
                            {
                                return false;
                            }
                            itemCubeStack.mnAmount += item.mnAmount;
                            this.MarkEverythingDirty();
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool AddToStack(ItemBase lItemToAdd)
    {
        if (lItemToAdd.mType != ItemType.ItemStack)
        {
            return false;
        }
        if (this.ValidItems.Contains(lItemToAdd.mnItemID))
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (this.maItemInventory[i, j] != null)
                    {
                        if (this.maItemInventory[i, j].mnItemID == lItemToAdd.mnItemID)
                        {
                            (this.maItemInventory[i, j] as ItemStack).mnAmount += (lItemToAdd as ItemStack).mnAmount;
                            this.MarkEverythingDirty();
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool StartNewStack(ItemBase lItemToAdd)
    {
        if (this.ValidItems.Contains(lItemToAdd.mnItemID))
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (this.maItemInventory[j, i] == null)
                    {
                        this.maItemInventory[j, i] = lItemToAdd;
                        this.MarkEverythingDirty();
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool CollectValue(ushort lType, ushort lnValue, int lnCount)
    {
        TerrainDataEntry terrainDataEntry = global::TerrainData.mEntries[(int)lType];
        if (terrainDataEntry != null && terrainDataEntry.GetValue(lnValue) == null)
        {
            lnValue = terrainDataEntry.DefaultValue;
        }
        this.MarkDirtyDelayed(5);
        if (lnCount == 0)
        {
            UnityEngine.Debug.Log("Adding zero material to inventory?" + lType.ToString() + "(Probably currency)");
        }
        if (!this.AddToStack(lType, lnCount, lnValue))
        {
            if (!this.StartNewStack(lType, lnCount, lnValue))
            {
                return false;
            }
            if (!WorldScript.mLocalPlayer.mResearch.IsScanned(lType, lnValue))
            {
                WorldScript.mLocalPlayer.mResearch.Scan(lType, lnValue);
            }
            this.AddToHotbar(lType, lnValue);
        }
        this.MarkEverythingDirty();
        return true;
    }

    public void AddToHotbar(ushort lType, ushort lValue)
    {
        if (SurvivalHotBarManager.mbInited)
        {
            SurvivalHotBarManager.instance.AttemptAdd(lType, lValue);
            this.MarkEverythingDirty();
        }
    }

    public void AddToHotbar(int itemType)
    {
        if (SurvivalHotBarManager.mbInited)
        {
            SurvivalHotBarManager.instance.AttemptAdd(itemType);
            this.MarkEverythingDirty();
        }
    }

    private bool AddToStack(ushort lType, int lnCount, ushort lnValue)
    {
        bool flag = false;
        int maxStackSize = global::TerrainData.GetMaxStackSize(lType);
        if (maxStackSize != 100)
        {
            flag = true;
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                ItemBase itemBase = this.maItemInventory[i, j];
                if (itemBase != null && itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                    if (itemCubeStack.mCubeType == lType && itemCubeStack.mCubeValue == lnValue)
                    {
                        if (!flag || itemCubeStack.mnAmount + lnCount <= maxStackSize)
                        {
                            itemCubeStack.mnAmount += lnCount;
                            this.MarkEverythingDirty();
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool StartNewStack(ushort lType, int lnCount, ushort lnValue)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (this.maItemInventory[j, i] == null)
                {
                    this.MarkEverythingDirty();
                    this.maItemInventory[j, i] = new ItemCubeStack(lType, lnValue, lnCount);
                    UnityEngine.Debug.Log(string.Concat(new object[]
                    {
                        "New stack ",
                        lType,
                        " at X:",
                        j,
                        "Y:",
                        i
                    }));
                    return true;
                }
            }
        }
        UIManager.instance.SetInfoTextCached("Inventory full! Can't collect!", 1.6f);
        return false;
    }

    //[DebuggerHidden]
    //public IEnumerator<ItemBase> GetEnumerator()
    //{
    //    PlayerInventory.< GetEnumerator > c__Iterator15 < GetEnumerator > c__Iterator = new PlayerInventory.< GetEnumerator > c__Iterator15();

    //    < GetEnumerator > c__Iterator.<> f__this = this;
    //    return < GetEnumerator > c__Iterator;
    //}

    public void SortByName()
    {
        int num = 80;
        int num2 = 65536;
        bool flag = true;
        for (int i = 0; i < num2; i++)
        {
            flag = true;
            for (int j = 0; j < num - 1; j++)
            {
                int num3 = j + 1;
                ItemBase itemBase = this.maItemInventory[j % 10, j / 10];
                ItemBase itemBase2 = this.maItemInventory[num3 % 10, num3 / 10];
                if (itemBase != null || itemBase2 != null)
                {
                    if (itemBase == null || itemBase2 != null)
                    {
                        if (itemBase == null && itemBase2 != null)
                        {
                            flag = false;
                            this.SwapInventory(j, num3);
                        }
                        else
                        {
                            string itemName = ItemManager.GetItemName(itemBase);
                            string itemName2 = ItemManager.GetItemName(itemBase2);
                            if (string.CompareOrdinal(itemName, itemName2) > 0)
                            {
                                flag = false;
                                this.SwapInventory(j, num3);
                            }
                        }
                    }
                }
            }
            if (flag)
            {
                break;
            }
        }
        if (!flag)
        {
            UnityEngine.Debug.LogWarning("Failed to sort!");
        }
        this.MarkEverythingDirty();
    }

    public void SortByType()
    {
        int num = 80;
        int num2 = 65536;
        bool flag = true;
        for (int i = 0; i < num2; i++)
        {
            flag = true;
            for (int j = 0; j < num - 1; j++)
            {
                int num3 = j + 1;
                ItemBase itemBase = this.maItemInventory[j % 10, j / 10];
                ItemBase itemBase2 = this.maItemInventory[num3 % 10, num3 / 10];
                if (itemBase != null || itemBase2 != null)
                {
                    if (itemBase == null || itemBase2 != null)
                    {
                        if (itemBase == null && itemBase2 != null)
                        {
                            flag = false;
                            this.SwapInventory(j, num3);
                        }
                        else
                        {
                            int num4;
                            if (itemBase.mType == ItemType.ItemCubeStack)
                            {
                                ushort mCubeType = (itemBase as ItemCubeStack).mCubeType;
                                num4 = (int)CubeHelper.GetCategory(mCubeType);
                                if (CubeHelper.IsGarbage(mCubeType))
                                {
                                    num4 -= 50;
                                }
                                if (CubeHelper.IsIngottableOre(mCubeType))
                                {
                                    num4 -= 25;
                                }
                            }
                            else
                            {
                                num4 = (int)ItemEntry.mEntries[itemBase.mnItemID].Category;
                                if (ItemEntry.mEntries[itemBase.mnItemID].Category == MaterialCategories.ArtherUpgrade)
                                {
                                    num4 += 50;
                                }
                                if (ItemEntry.mEntries[itemBase.mnItemID].Category == MaterialCategories.SuitUpgrade)
                                {
                                    num4 += 150;
                                }
                                if (ItemEntries.IsBar(itemBase))
                                {
                                    num4 += 100;
                                }
                            }
                            int num5;
                            if (itemBase2.mType == ItemType.ItemCubeStack)
                            {
                                ushort mCubeType2 = (itemBase2 as ItemCubeStack).mCubeType;
                                num5 = (int)CubeHelper.GetCategory(mCubeType2);
                                if (CubeHelper.IsGarbage(mCubeType2))
                                {
                                    num5 -= 50;
                                }
                                if (CubeHelper.IsIngottableOre(mCubeType2))
                                {
                                    num5 -= 25;
                                }
                            }
                            else
                            {
                                num5 = (int)ItemEntry.mEntries[itemBase2.mnItemID].Category;
                                if (ItemEntry.mEntries[itemBase2.mnItemID].Category == MaterialCategories.ArtherUpgrade)
                                {
                                    num5 += 50;
                                }
                                if (ItemEntry.mEntries[itemBase2.mnItemID].Category == MaterialCategories.SuitUpgrade)
                                {
                                    num5 += 150;
                                }
                                if (ItemEntries.IsBar(itemBase))
                                {
                                    num5 += 100;
                                }
                            }
                            if (num4 == num5)
                            {
                                string itemName = ItemManager.GetItemName(itemBase);
                                string itemName2 = ItemManager.GetItemName(itemBase2);
                                if (string.CompareOrdinal(itemName, itemName2) > 0)
                                {
                                    flag = false;
                                    this.SwapInventory(j, num3);
                                }
                            }
                            else if (num4 < num5)
                            {
                                flag = false;
                                this.SwapInventory(j, num3);
                            }
                        }
                    }
                }
            }
            if (flag)
            {
                break;
            }
        }
        if (!flag)
        {
            UnityEngine.Debug.LogWarning("Failed to sort!");
        }
        this.MarkEverythingDirty();
    }

    public void SortByQuantity()
    {
        int num = 80;
        int num2 = 65536;
        bool flag = true;
        for (int i = 0; i < num2; i++)
        {
            flag = true;
            for (int j = 0; j < num - 1; j++)
            {
                int num3 = j + 1;
                ItemBase itemBase = this.maItemInventory[j % 10, j / 10];
                ItemBase itemBase2 = this.maItemInventory[num3 % 10, num3 / 10];
                if (itemBase != null || itemBase2 != null)
                {
                    if (itemBase == null || itemBase2 != null)
                    {
                        if (itemBase == null && itemBase2 != null)
                        {
                            flag = false;
                            this.SwapInventory(j, num3);
                        }
                        else if ((itemBase.mType != ItemType.ItemCubeStack && itemBase.mType != ItemType.ItemStack) || itemBase2.mType == ItemType.ItemCubeStack || itemBase2.mType == ItemType.ItemStack)
                        {
                            if (itemBase.mType != ItemType.ItemCubeStack && itemBase.mType != ItemType.ItemStack && (itemBase2.mType == ItemType.ItemCubeStack || itemBase2.mType == ItemType.ItemStack))
                            {
                                this.SwapInventory(j, num3);
                                flag = false;
                            }
                            else if ((itemBase.mType == ItemType.ItemCubeStack || itemBase.mType == ItemType.ItemStack) && (itemBase2.mType == ItemType.ItemCubeStack || itemBase2.mType == ItemType.ItemStack))
                            {
                                if (ItemManager.GetCurrentStackSize(itemBase) < ItemManager.GetCurrentStackSize(itemBase2))
                                {
                                    this.SwapInventory(j, num3);
                                    flag = false;
                                }
                            }
                            else if ((itemBase.mType == ItemType.ItemDurability || itemBase.mType == ItemType.ItemSingle) && (itemBase2.mType == ItemType.ItemStack || itemBase2.mType == ItemType.ItemCubeStack))
                            {
                                this.SwapInventory(j, num3);
                                flag = false;
                            }
                            else if ((itemBase2.mType == ItemType.ItemDurability || itemBase2.mType == ItemType.ItemSingle) && (itemBase.mType == ItemType.ItemStack || itemBase.mType == ItemType.ItemCubeStack))
                            {
                                this.SwapInventory(j, num3);
                                flag = false;
                            }
                            else if ((itemBase.mType != ItemType.ItemDurability && itemBase.mType != ItemType.ItemSingle) || (itemBase2.mType != ItemType.ItemDurability && itemBase2.mType != ItemType.ItemSingle))
                            {
                                UnityEngine.Debug.LogWarning(string.Concat(new string[]
                                {
                                    "Failed to swap ",
                                    itemBase.ToString(),
                                    " with ",
                                    itemBase2.ToString(),
                                    " - why?"
                                }));
                            }
                        }
                    }
                }
            }
            if (flag)
            {
                break;
            }
        }
        if (!flag)
        {
            UnityEngine.Debug.LogWarning("Failed to sort!");
        }
        this.MarkEverythingDirty();
    }

    private void SwapInventory(int lnOffset1, int lnOffset2)
    {
        ItemBase itemBase = this.maItemInventory[lnOffset1 % 10, lnOffset1 / 10];
        ItemBase itemBase2 = this.maItemInventory[lnOffset2 % 10, lnOffset2 / 10];
        ItemBase itemBase3 = itemBase;
        this.maItemInventory[lnOffset1 % 10, lnOffset1 / 10] = itemBase2;
        this.maItemInventory[lnOffset2 % 10, lnOffset2 / 10] = itemBase3;
    }



    public static bool NetworkAddItem(Player player, ItemBase item, string slot)
    {
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("SuitInventoryWindow", "AddItem", slot, item, null, 0.0f);
        else
        {

        }
        return true;
    }

    public static bool NetworkRemoveItem(Player player, string slot)
    {
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("SuitInventoryWindow", "RemoveItem", slot, null, null, 0.0f);
        else
        {
            
        }
        return true;
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        string key = nic.command;
        if (key != null)
        {
            if (key == "AddItem")
            {
                PlayerSuitInventory.NetworkAddItem(player, nic.itemContext, nic.payload);
            }
            else if (key == "RemoveItem")
            {
                PlayerSuitInventory.NetworkRemoveItem(player, nic.payload);
            }
        }
        return new NetworkInterfaceResponse()
        {
            entity = (SegmentEntity)null,
            inventory = player.mInventory
        };
    }
}

