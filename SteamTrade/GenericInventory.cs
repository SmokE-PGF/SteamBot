﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade.TradeWebAPI;

namespace SteamTrade
{
    
    /// <summary>
    /// Generic Steam Backpack Interface
    /// </summary>
    public class GenericInventory
    {
        public Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        public Dictionary<ulong, ItemDescription> descriptions = new Dictionary<ulong, ItemDescription>();

        public bool isLoaded = false;
        public List<string> errors = new List<string>();

        public class Item : TradeUserAssets
        {
            public ulong classid { get; set; }
        }

        public class ItemDescription
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool tradable { get; set; }
            public bool marketable { get; set; }

            public dynamic app_data { get; set; }
        }

        public ItemDescription getInfo(ulong id)
        {
            try
            {
                return descriptions[items[id].classid];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errors.Add("getInfo(" + id + "): " + e.Message);
                return new ItemDescription();
            }
        }

        public bool itemExists(ulong id)
        {
            try
            {
                GenericInventory.Item tmpItem = items[id];
                return true;
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
                return false;
            }
        }

        public bool load(int appid,List<int> contextIds, SteamID steamid)
        {
            dynamic invResponse;
            isLoaded = false;

            try
            {
                for (int i = 0; i < contextIds.Count; i++)
                {
                    string response = SteamWeb.Fetch(string.Format("http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/?trading=1", steamid.ConvertToUInt64(),appid, contextIds[i]), "GET", null, null, true);
                    invResponse = JsonConvert.DeserializeObject(response);

                    if (invResponse.success == false)
                    {
                        errors.Add("Fail to open backpack: " + invResponse.Error);
                        continue;
                    }

                    //rgInventory = Items on Steam Inventory 
                    foreach (var item in invResponse.rgInventory)
                    {

                        foreach (var itemId in item)
                        {
                            items.Add((ulong)itemId.id, new Item()
                            {
                                appid = appid,
                                contextid = contextIds[i],
                                assetid = itemId.id,
                                classid = itemId.classid
                            });
                            break;
                        }
                    }

                    // rgDescriptions = Item Schema (sort of)
                    foreach (var description in invResponse.rgDescriptions)
                    {
                        foreach (var classid_instanceid in description)// classid + '_' + instenceid 
                        {
                            if (!descriptions.ContainsKey((ulong)classid_instanceid.classid))
                            descriptions.Add((ulong)classid_instanceid.classid, new ItemDescription(){
                                name = classid_instanceid.name,
                                type = classid_instanceid.type,
                                marketable = (bool) classid_instanceid.marketable,
                                tradable = (bool)classid_instanceid.tradable,
                                app_data = classid_instanceid.app_data
                            });
                            break;
                        }
                    }

                    if (errors.Count > 0)
                        return false;
                    
                }//end for (contextId)
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errors.Add("Exception: " + e.Message);
                return false;
            }
            isLoaded = true;
            return true;
        }
    }
}
