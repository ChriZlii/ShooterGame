﻿using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponholderScript : NetworkBehaviour
{

    // Publics------------------------------------------------------------------------------------------------------
    public GameObject Weaponholder;

    public bool EnableManualSwitching = true;

    public WeaponTypes StartWeaponType = WeaponTypes.PISTOL;

    public GameObject[] Weapons;



    [HideInInspector]
    public GameObject SelectedWeapon;

    [SyncVar]
    [HideInInspector]
    public int selectedWeaponNum = (int)WeaponTypes.PISTOL;

    // Privates------------------------------------------------------------------------------------------------------
    private InputController controls;
    private int selectedWeaponPrevious = (int)WeaponTypes.PISTOL;

    // Fields--------------------------------------------------------------------------------------------------------


    private void Awake() => controls = new InputController();

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();


    void Start()
    {
        // add Prefabs to Player
        GameObject[] newWeapon = new GameObject[Weapons.Length];
        foreach (GameObject weapon in Weapons)
        {
            WeaponTypes type = weapon.GetComponent<WeaponData>().weaponType;
            newWeapon[(int)type] = Instantiate(weapon, Weaponholder.transform);
        }
        Weapons = newWeapon;


        // remove all weapons from local player inventory.
        if (isLocalPlayer)
        {
            foreach (GameObject weapon in Weapons)
            {
                WeaponTypes type = weapon.GetComponent<WeaponData>().weaponType;
                GiveWeapon((int)type, false);
            }
        }

        //Give player one weapon
        if (isServer) RpcGiveWeapon((int)StartWeaponType, true);
        else GiveWeapon((int)StartWeaponType, true);
        SelectWeapon((int)StartWeaponType);
    }


    void Update()
    {
        if (EnableManualSwitching)
        {
            // every weaponholder--------------------------------------------------------------------------------------------
            // is weapon changed, change the wepon in hand
            if (selectedWeaponNum != selectedWeaponPrevious)
            {
                SelectWeapon(selectedWeaponNum);
                selectedWeaponPrevious = selectedWeaponNum;
            }



            if (!isLocalPlayer) return;
            // only weaponholder----------------------------------------------------------------------------------------------

            int weaponNumber = selectedWeaponNum;
            float scrollwheelInput = controls.Player.SwitchWeapons.ReadValue<Vector2>().y;

            if (scrollwheelInput > 0)
            {
                IncrementWeaponNumber(ref weaponNumber);
            }
            else if (scrollwheelInput < 0)
            {
                DecrementWeaponNumber(ref weaponNumber);
            }


            // if weapon changed Publish to server
            if (weaponNumber != selectedWeaponNum)
            {
                selectedWeaponNum = weaponNumber;
                CmdSelectetWeapon(weaponNumber);
            }
        }
    }


    // increments the weaponnumber and check if the player has the weapon
    private void IncrementWeaponNumber(ref int weaponNumber)
    {
        //incrementWeaponNumber weapon in circle
        weaponNumber++;
        if (weaponNumber == Weaponholder.transform.childCount)
        {
            weaponNumber = 0;
        }

        //check if play has the weapon
        if (!this.HasWeapon(weaponNumber))
        {
            IncrementWeaponNumber(ref weaponNumber);
        }
    }

    // decrements the weaponnumber and check if the player has the weapon
    private void DecrementWeaponNumber(ref int weaponNumber)
    {
        //decrementWeaponNumber weapon in circle
        weaponNumber--;
        if (weaponNumber == -1)
        {
            weaponNumber = Weaponholder.transform.childCount - 1;
        }

        //check if play has the weapon
        if (!this.HasWeapon(weaponNumber))
        {
            DecrementWeaponNumber(ref weaponNumber);
        }
    }

    // returns true if the player has the weapon with number
    private bool HasWeapon(int weaponNumber)
    {
        GameObject weapon = Weapons[weaponNumber];

        if (weapon.activeInHierarchy)
        {
            //Debug.Log("Has weapon");
            return true;
        }
        else
        {
            //Debug.Log("Has not weapon");
            return false;
        }

        throw new System.ArgumentException("Some Error in Weaponswitching occours!!", "Weapon");
    }

    // return true if successfull, flase if playser hasnt weapon.
    public bool SelectWeapon(int weaponNumber)
    {
        if (!HasWeapon(weaponNumber)) return false;

        int count = 0;

        foreach (GameObject weapon in Weapons)
        {
            if (count++ == weaponNumber)
            {
                weapon.GetComponent<WeaponData>().Weapon.SetActive(true);
                SelectedWeapon = weapon;
            }
            else
            {
                weapon.GetComponent<WeaponData>().Weapon.SetActive(false);
            }
        }
        return true;
    }

    // True gives player weapon, false takes is away
    // returns true if successfull, falls if player has allredy
    [ClientRpc]
    public void RpcGiveWeapon(int weaponNumber, bool state)
    {
        GameObject weapon = Weapons[weaponNumber];
        weapon.SetActive(state);
    }

    public void GiveWeapon(int weaponNumber, bool state)
    {
        if (isServer) { RpcGiveWeapon(weaponNumber, state); }
        else
        {
            GameObject weapon = Weapons[weaponNumber];
            weapon.SetActive(state);
        }
    }

    [Command]
    public void CmdGiveWeapon(int weaponNumber, bool state) => GiveWeapon(weaponNumber, state);

    public void Msg_GiveWeapon(List<object> messageData)
    {
        CmdGiveWeapon((int)messageData[0], (bool)messageData[1]);

        // if argument 2 is set, the item schould be despawnd. if not ther is an exeption and is will stay alife
        try { CmdDespawnNetID((uint)messageData[2]); }
        catch (ArgumentOutOfRangeException) { }
        //else throw new UnityException("Call from Client, only enabled for Server/Host");
    }




    // publishes the weapon change to the server. The server Syncs it to the rest of holders
    [Command]
    public void CmdSelectetWeapon(int seletion)
    {
        this.selectedWeaponNum = seletion;
    }

    [Command]
    public void CmdDespawnNetID(uint playerNetID)
    {
        GameObject obj = NetworkIdentity.spawned[(uint)playerNetID].gameObject;
        NetworkServer.Destroy(obj);
    }

}
