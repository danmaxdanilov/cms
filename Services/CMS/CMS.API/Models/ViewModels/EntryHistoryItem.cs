using System;

namespace CMS.API.Models.ViewModels;

public class EntryHistoryItem
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime EventDate { get; set; }
}