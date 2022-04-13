﻿using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Domain.Entities;
[Index(nameof(Name), IsUnique=true)]
public class Country
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double KilometerPrice { get; set; }
    public ICollection<Depot> Depots { get; set; }
}