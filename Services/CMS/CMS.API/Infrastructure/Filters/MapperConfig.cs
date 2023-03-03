using System;
using System.Collections.Generic;
using AutoMapper;
using CMS.API.Models.DomainModels;
using CMS.API.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace CMS.API.Infrastructure.Filters;

public static class MapperConfig
{
    public static MapperConfiguration CreateConfig()
    {
        return new MapperConfiguration(
            cfg =>
            {
                cfg.CreateMap<Command, EntryHistoryItem>();
                cfg.CreateMap<Entry, EntryItem>()
                    .ForMember(dest => dest.HistoryItems, 
                        opt => opt.MapFrom(src => src.Commands));
                cfg.CreateMap<Entry, EntryItemDetailed>()
                    .ForMember(dest => dest.HistoryItems, 
                        opt => opt.MapFrom(src => src.Commands));
                
                cfg.CreateMap<EntryItem, Entry>();
            });
    } 
}