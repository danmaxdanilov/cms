using System;
using System.Collections.Generic;
using System.Linq;

namespace CMS.API.Models.ViewModels;

public class EntryItem
{
    public EntryItem()
    {
        HistoryItems = new List<EntryHistoryItem>();
    }
    
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    
    public string CurrentStatus => HistoryItems.LastOrDefault()?.Status;
    public List<EntryHistoryItem> HistoryItems { get; set; }
}