using System;

namespace SoundpadLightFINAL.Models;

public class SoundItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? PlaylistId { get; set; }
}


