using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CMS.Shared.Domain;

public enum CommandStatus
{
    [Display(Name = "Выполнено")]
    Done = 1,
    
    [Display(Name = "Ошибка")]
    Error = 2,
    
    [Display(Name = "В процессе")]
    InProgress = 3,
    
    [Display(Name = "Помечено как удаленное")]
    MarkDeleted = 10
}

public static class EnumExtentions
{
    public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
        where TAttribute: Attribute
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<TAttribute>();
    }
}