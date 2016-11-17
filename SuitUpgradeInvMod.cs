using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuitUpgradeInvMod : FortressCraftMod
{
    //public GameObject SuitUpgradePanel;
    //public MethodInfo methoddata;

    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.OnVerifySuitUpgrades = new ModVerifySuitUpgradesDelegate(this.HandleSuitUpgrades);

        GameObject Sync = new GameObject("SuitUpgradePanel");
        Sync.AddComponent<SuitUpgradePanel>();
        Sync.SetActive(true);
        Sync.GetComponent<SuitUpgradePanel>().enabled = true;

        Debug.Log("Suit Upgrade Inventory Mod v1 registered");

        return modRegistrationData;
    }

    public void HandleSuitUpgrades(ModVerifySuitUpgradeParameters parameters)
    {
        if (SuitUpgradePanel.instance != null && SuitUpgradePanel.instance.SuitInventory != null)
            SuitUpgradePanel.instance.SuitInventory.VerifySuitUpgrades();
    }

    //void Update()
    //{
    //    if (this.SuitUpgradePanel == null)
    //    {
    //        this.SuitUpgradePanel = GameObject.Find("UI Root (2D)").transform.Search("Suit_Upgrade_Panel").gameObject;
    //        //foreach (Object x in this.SuitUpgradePanel.GetComponentsInChildren<Object>())
    //        //  Debug.Log("Object: " + x.name);
    //        //foreach (Object x in InventoryPanelScript.instance.GetComponentsInChildren<Object>())
    //        //    Debug.Log("Inv panel Object: " + x.name);

    //        //Instantiate all of the inventory slots
    //        //GameObject hoveredObject = UICamera.hoveredObject;
    //    }
    //    if (Input.GetKeyDown(KeyCode.U) && UIManager.instance.mInventoryPanel.isActiveAndEnabled && this.SuitUpgradePanel != null && !GenericMachinePanelScript.instance.isActiveAndEnabled)
    //    {
    //        if (!this.SuitUpgradePanel.activeSelf)
    //        {
    //            //this.SuitUpgradePanel.transform.position = UIManager.instance.mInventoryPanel.transform.position + new Vector3(0.5f, 0, 0);
    //            this.SuitUpgradePanel.SetActive(true);
    //            if (this.methoddata == null)
    //                this.methoddata = InventoryPanelScript.instance.GetType().GetMethod("RepositionInventory", BindingFlags.NonPublic | BindingFlags.Instance);
    //            methoddata.Invoke(InventoryPanelScript.instance, new object[] { true });
    //        }
    //        else
    //        {
    //            this.SuitUpgradePanel.SetActive(false);
    //            methoddata.Invoke(InventoryPanelScript.instance, new object[] { false });
    //        }
    //    }
    //}


    public void WriteInventory()
    {
        //WorldScript.instance.mDiskThread.RegisterManagedFile(WriteMethod, ReadMethod, FullFilePath)
    }
}


